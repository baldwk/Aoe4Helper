using AduSkin.Controls.Metro;
using AduSkin.Demo.ViewModel;
using Aoe4Helper.Servers.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Shape = System.Windows.Shapes.Shape;
using System.Windows.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aoe4Helper
{
   public enum BuildingType { Stable, Barracks, Archery, TownCenter }

   public partial class MainWindow : IWindow
   {
      private const double MilitaryAcademySpeed = 1.33;
      private readonly Dictionary<BuildingType, BuildingState> buildings = new();
      private Dictionary<string, CivilizationProductionConfig> civConfigs;
      private string civ = string.Empty;
      private double intervalMultiplier = 1;
      private bool updatingControls = true;

      public MainWindow(MainViewModel viewModel)
      {
         LoadCivConfig();
         foreach (BuildingType type in Enum.GetValues<BuildingType>())
         {
            var state = new BuildingState(type switch
            {
               BuildingType.TownCenter => 1,
               BuildingType.Barracks => 5,
               _ => 4
            });
            buildings.Add(type, state);
            for (int i = 0; i < state.Timers.Length; i++)
            {
               int slot = i;
               state.Timers[i].Tick += async (_, _) => await ProduceUnitsAsync(type, slot);
            }
         }
         InitializeComponent();
         DataContext = viewModel;
         buildings[BuildingType.Archery].Ring = ArcheryRing;
         buildings[BuildingType.Stable].Ring = StableRing;
         buildings[BuildingType.Barracks].Ring = BarracksRing;
         updatingControls = false;
         UpdateInterval();
         Closed += (_, _) =>
         {
            StopProducerTimers();
            CancelProductionRequests();
            productionCancellation.Dispose();
            Application.Current.Shutdown();
         };
      }

      private void LoadCivConfig()
      {
         string path = Path.Combine(AppContext.BaseDirectory, "prod.tab");
         var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
         var configs = deserializer.Deserialize<Dictionary<string, CivConfig>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("prod.tab 中没有文明配置。");
         civConfigs = configs.ToDictionary(pair => pair.Key, pair => pair.Value.Parse(pair.Key));
      }

      private CivilizationProductionConfig CurrentConfig =>
         civConfigs.TryGetValue(civ, out var config) ? config : null;

      private Task ProduceUnitsAsync(BuildingType type, int slot, bool activateGame = false)
      {
         var state = buildings[type];
         return state.Enabled && state.Quantities[slot] > 0 && state.Timers[slot].Interval > TimeSpan.Zero
            ? ProduceWithKeyboardAsync(type.ToString(), slot, state.Quantities[slot], activateGame)
            : Task.CompletedTask;
      }

      private void UpdateInterval(bool restart = false)
      {
         if (updatingControls) return;
         var config = CurrentConfig;
         updatingControls = true;
         try { UpdateProductionSlotVisibility(config); }
         finally { updatingControls = false; }
         foreach (var (type, state) in buildings)
         {
            for (int i = 0; i < state.Timers.Length; i++)
            {
               double seconds = config?.Slots[type][i].TrainingSeconds ?? 0;
               if (type != BuildingType.TownCenter) seconds *= intervalMultiplier;
               var interval = TimeSpan.FromSeconds(seconds);
               var timer = state.Timers[i];
               if (restart || timer.Interval != interval)
               {
                  timer.Stop();
                  timer.Interval = interval;
               }
            }
            UpdateTimerState(state);
         }
         CalcFarmerCount();
      }

      private static void UpdateTimerState(BuildingState state)
      {
         for (int i = 0; i < state.Timers.Length; i++)
         {
            var timer = state.Timers[i];
            bool shouldRun = state.Enabled && state.Quantities[i] > 0 && timer.Interval > TimeSpan.Zero;
            if (timer.IsEnabled == shouldRun) continue;
            if (shouldRun) timer.Start();
            else timer.Stop();
         }
      }

      private void StopProducerTimers()
      {
         foreach (var timer in buildings.Values.SelectMany(state => state.Timers)) timer.Stop();
      }

      private void Civ_Changed(object sender, SelectionChangedEventArgs e)
      {
         string selected = ((AduComboBox)sender).SelectedItem is ComboBoxItem item ? item.Name : string.Empty;
         if (selected == civ) return;
         CancelProductionRequests();
         civ = selected;
         UpdateInterval(restart: true);
         if (civ.Length > 0 && CurrentConfig == null) ShowProductionError("找不到所选文明的生产配置。");
      }

      private async void Tc_Checked(object sender, RoutedEventArgs e) =>
         await SetBuildingEnabledAsync(BuildingType.TownCenter, ((MetroSwitch)sender).IsChecked == true);

      private async void Archery_Clicked(object sender, MouseButtonEventArgs e) =>
         await SetBuildingEnabledAsync(BuildingType.Archery, !buildings[BuildingType.Archery].Enabled);

      private async void Stable_Clicked(object sender, MouseButtonEventArgs e) =>
         await SetBuildingEnabledAsync(BuildingType.Stable, !buildings[BuildingType.Stable].Enabled);

      private async void Barracks_Clicked(object sender, MouseButtonEventArgs e) =>
         await SetBuildingEnabledAsync(BuildingType.Barracks, !buildings[BuildingType.Barracks].Enabled);

      private async Task SetBuildingEnabledAsync(BuildingType type, bool enabled)
      {
         var state = buildings[type];
         if (updatingControls || state.Enabled == enabled) return;
         CancelProductionRequests(type.ToString());
         state.Enabled = enabled;
         UpdateRing(state);
         UpdateTimerState(state);
         CalcFarmerCount();
         if (enabled)
         {
            // 同一轮的槽位一起入队，由按键客户端合并建筑选择操作。
            await Task.WhenAll(Enumerable.Range(0, state.Quantities.Length)
               .Select(slot => ProduceUnitsAsync(type, slot, activateGame: true)));
         }
      }

      private static void UpdateRing(BuildingState state)
      {
         Shape ring = state.Ring;
         if (ring == null) return;
         if (!state.Enabled)
         {
            ring.BeginAnimation(Shape.StrokeDashOffsetProperty, null);
            ring.BeginAnimation(Shape.StrokeThicknessProperty, null);
            ring.Visibility = Visibility.Collapsed;
            return;
         }
         ring.Visibility = Visibility.Visible;
         ring.BeginAnimation(Shape.StrokeThicknessProperty, new DoubleAnimation
         {
            To = 3.5, Duration = TimeSpan.FromSeconds(0.1), AutoReverse = true,
            FillBehavior = FillBehavior.Stop
         });
         ring.BeginAnimation(Shape.StrokeDashOffsetProperty, new DoubleAnimation
         {
            From = 0, To = 15, Duration = TimeSpan.FromSeconds(1.4), RepeatBehavior = RepeatBehavior.Forever
         });
      }

      private void tcNumChanged(object sender, TextChangedEventArgs e) => UpdateProductionCount(BuildingType.TownCenter, 0, sender);
      private void a_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 0, sender);
      private void a_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 1, sender);
      private void a_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 2, sender);
      private void a_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 3, sender);
      private void s_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 0, sender);
      private void s_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 1, sender);
      private void s_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 2, sender);
      private void s_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 3, sender);
      private void b_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 0, sender);
      private void b_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 1, sender);
      private void b_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 2, sender);
      private void b_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 3, sender);
      private void b_t_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 4, sender);

      private void UpdateProductionCount(BuildingType type, int slot, object sender)
      {
         var input = (AduIntegerUpDown)sender;
         int quantity = int.TryParse(input.Text, out int value)
            ? Math.Clamp(value, input.Minimum, input.Maximum) : 0;
         var state = buildings[type];
         if (state.Quantities[slot] == quantity) return;
         state.Quantities[slot] = quantity;
         if (updatingControls) return;
         CancelProductionRequests(type.ToString());
         UpdateTimerState(state);
         CalcFarmerCount();
      }

      private void CalcFarmerCount()
      {
         if (updatingControls) return;
         var config = CurrentConfig;
         var costs = new double[3];
         if (config != null)
         {
            foreach (var (type, state) in buildings.Where(pair => pair.Value.Enabled))
            {
               for (int i = 0; i < state.Quantities.Length; i++)
               {
                  double seconds = state.Timers[i].Interval.TotalSeconds;
                  if (seconds <= 0) continue;
                  var unit = config.Slots[type][i];
                  for (int resource = 0; resource < costs.Length; resource++)
                     costs[resource] += state.Quantities[i] * unit.Costs[resource] * 60 / seconds;
               }
            }
            for (int resource = 0; resource < costs.Length; resource++)
               costs[resource] /= config.GatheringRates[resource];
         }
         FoodFarmers.Text = costs[0].ToString("0.00", CultureInfo.InvariantCulture);
         WoodFarmers.Text = costs[1].ToString("0.00", CultureInfo.InvariantCulture);
         GoldFarmers.Text = costs[2].ToString("0.00", CultureInfo.InvariantCulture);
      }

      private void Ma_Checked(object sender, RoutedEventArgs e)
      {
         intervalMultiplier = ((MetroSwitch)sender).IsChecked == true ? 1 / MilitaryAcademySpeed : 1;
         UpdateInterval();
      }

      private void Reset_Click(object sender, RoutedEventArgs e)
      {
         CancelProductionRequests();
         StopProducerTimers();
         updatingControls = true;
         try
         {
            foreach (var state in buildings.Values)
            {
               state.Enabled = false;
               Array.Clear(state.Quantities);
               UpdateRing(state);
            }
            // 逻辑树包含尚未展开的控件，避免依赖控件模板的视觉子树。
            ResetInputControls(this);
            intervalMultiplier = 1;
         }
         finally { updatingControls = false; }
         ProductionStatus.Text = string.Empty;
         ProductionStatus.Visibility = Visibility.Collapsed;
         UpdateInterval(restart: true);
      }

      private static void ResetInputControls(DependencyObject parent)
      {
         foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
         {
            if (child is MetroSwitch toggle) toggle.IsChecked = false;
            else if (child is AduIntegerUpDown input) input.Value = 0;
            else ResetInputControls(child);
         }
      }

      private sealed class BuildingState
      {
         public bool Enabled { get; set; }
         public int[] Quantities { get; }
         public DispatcherTimer[] Timers { get; }
         public Shape Ring { get; set; }

         public BuildingState(int slots)
         {
            Quantities = new int[slots];
            Timers = Enumerable.Range(0, slots).Select(_ => new DispatcherTimer()).ToArray();
         }
      }
   }
}
