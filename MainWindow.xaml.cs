using AduSkin.Controls.Metro;
using AduSkin.Demo;
using AduSkin.Demo.ViewModel;
using Aoe4Helper.Servers.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Reflection.Metadata;

namespace Aoe4Helper
{
   public enum BuildingType
   {
      Stable,   // 马厩
      Barracks, // 兵营
      Archery   // 射箭场
   }
   
   public class CivConfig
   {
      public string tc { get; set; }
      public string a_q { get; set; }
      public string a_w { get; set; }
      public string a_e { get; set; }
      public string a_r { get; set; }
      public string s_q { get; set; }
      public string s_w { get; set; }
      public string s_e { get; set; }
      public string s_r { get; set; }
      public string b_q { get; set; }
      public string b_w { get; set; }
      public string b_e { get; set; }
      public string b_r { get; set; }
      public string food { get; set; }
      public string wood { get; set; }
      public string gold { get; set; }
      public string stone { get; set; }
   }
   public partial class MainWindow : IWindow
   {
      #region Win32 API导入
      [DllImport("user32.dll")]
      public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

      [DllImport("user32.dll")]
      public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

      [DllImport("user32.dll")]
      public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
      #endregion

      #region 常量定义

      // 游戏窗口名称
      private const string GAME_WINDOW_NAME = "Age of Empires IV ";
      
      // 窗口消息常量
      private const int WM_SYSKEYDOWN = 0x0104;
      private const int WM_SYSKEYUP = 0x0105;
      private const int WM_CHAR = 0x0102;
      private const int WM_KEYDOWN = 0x100;
      private const int WM_KEYUP = 0x0101;
      private const int WM_LBUTTONDOWN = 0x0201;
      private const int WM_LBUTTONUP = 0x0202;
      
      // 鼠标按键常量
      private const int VK_LBUTTON = 0X1; // 鼠标左键
      private const int VK_RBUTTON = 0X2; // 鼠标右键
      private const int VK_MBUTTON = 0X4; // 鼠标中键
      
      // 键盘事件标志
      private const int KEYEVENTF_KEYUP = 0X2; // 释放按键
      private const int KEYEVENTF_EXTENDEDKEY = 0X1; 
      // 数字键常量
      private const int VK_0 = 0x30;
      private const int VK_1 = 0x31;
      private const int VK_2 = 0x32;
      private const int VK_3 = 0x33;
      private const int VK_4 = 0x34;
      private const int VK_5 = 0x35;
      private const int VK_6 = 0x36;
      private const int VK_7 = 0x37;
      private const int VK_8 = 0x38;
      private const int VK_9 = 0x39;
      // 字母键常量
      private const int VK_A = 0x41;
      private const int VK_B = 0x42;
      private const int VK_C = 0x43;
      private const int VK_D = 0x44;
      private const int VK_E = 0x45;
      private const int VK_F = 0x46;
      private const int VK_G = 0x47;
      private const int VK_H = 0x48;
      private const int VK_I = 0x49;
      private const int VK_J = 0x4A; // 兵营选择键
      private const int VK_K = 0x4B; // 射箭场选择键
      private const int VK_L = 0x4C; // 马厩选择键
      private const int VK_M = 0x4D;
      private const int VK_N = 0x4E;
      private const int VK_O = 0x4F;
      private const int VK_P = 0x50;
      private const int VK_Q = 0x51; // 生产键Q
      private const int VK_R = 0x52; // 生产键R
      private const int VK_S = 0x53;
      private const int VK_T = 0x54;
      private const int VK_U = 0x55;
      private const int VK_V = 0x56;
      private const int VK_W = 0x57; // 生产键W
      private const int VK_X = 0x58;
      private const int VK_Y = 0x59;
      private const int VK_Z = 0x5A;
      // 功能键常量
      private const int VK_ESCAPE = 0x1B; // ESC键
      private const int VK_SPACE = 0x20;  // 空格键
      
      // 鼠标点击位置常量
      private const int CLICK_X_BEFORE = 500;
      private const int CLICK_Y_BEFORE = 800;
      private const int CLICK_X_AFTER = 1000;
      private const int CLICK_Y_AFTER = 800;
      
      // 游戏相关常量
      private const double MA_MULTIPLIER = 0.66; // MA加速倍率
      private const int PRODUCTION_KEYS_COUNT = 4; // 生产按键数量 Q,W,E,R
      private const string CONFIG_FILE = "./prod.tab";
      private const string DEFAULT_CIV = "rus";
      
      // 时间和计算相关常量
      private const int TIMER_ADJUSTMENT_MICROSECONDS = -100; // 定时器调整微秒
      private const int SECONDS_PER_MINUTE = 60; // 每分钟秒数
      #endregion

      #region 私有字段
      private DispatcherTimer tcProducer;
      private List<DispatcherTimer> stableProducer, archeryProducer, barracksProducer;
      private Dictionary<string, CivConfig> civConfigs;
      private Random rnd = new Random();
      private double intervalMultiplier = 1.0;

      private int tcNumber;
      private int[] stableProd = new int[PRODUCTION_KEYS_COUNT];
      private int[] archeryProd = new int[PRODUCTION_KEYS_COUNT];
      private int[] barracksProd = new int[PRODUCTION_KEYS_COUNT];

      private bool tcEnabled = false;
      private bool stableEnabled = false; 
      private bool archeryEnabled = false;
      private bool barracksEnabled = false;
      private string civ = DEFAULT_CIV;
      #endregion

      #region 构造函数和初始化
      public MainWindow(MainViewModel viewModel)
      {
         LoadCivConfig();
         InitializeComponent();
         this.DataContext = viewModel;
         this.Closed += delegate { Application.Current.Shutdown(); };
         InitTimer();
      }
      #endregion

      #region 配置加载
      private void LoadCivConfig()
      {
         var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
         var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

         var ymlFile = CONFIG_FILE;
         if (File.Exists(ymlFile))
         {
            var ymlContent = File.ReadAllText(ymlFile); // 读取 yaml 文件内容
            civConfigs = deserializer.Deserialize<Dictionary<string, CivConfig>>(ymlContent); // 序列化
            if (civConfigs == null)
            {
               throw new Exception("Failed to load civ configs.");
            }
         }
      }
      #endregion

      #region 定时器初始化
      private void InitTimer()
      {
         // 初始化TC生产器
         tcProducer = new DispatcherTimer
         {
            Interval = TimeSpan.FromSeconds(0)
         };
         tcProducer.Tick += (s, e) => TcProduce();

         // 初始化马厩生产器
         stableProducer = CreateProducerTimers();
         stableProducer[0].Tick += (s, e) => ProduceUnits(BuildingType.Stable, 0, VK_Q);
         stableProducer[1].Tick += (s, e) => ProduceUnits(BuildingType.Stable, 1, VK_W);
         stableProducer[2].Tick += (s, e) => ProduceUnits(BuildingType.Stable, 2, VK_E);
         stableProducer[3].Tick += (s, e) => ProduceUnits(BuildingType.Stable, 3, VK_R);
         
         // 初始化兵营生产器  
         barracksProducer = CreateProducerTimers();
         barracksProducer[0].Tick += (s, e) => ProduceUnits(BuildingType.Barracks, 0, VK_Q);
         barracksProducer[1].Tick += (s, e) => ProduceUnits(BuildingType.Barracks, 1, VK_W);
         barracksProducer[2].Tick += (s, e) => ProduceUnits(BuildingType.Barracks, 2, VK_E);
         barracksProducer[3].Tick += (s, e) => ProduceUnits(BuildingType.Barracks, 3, VK_R);

         // 初始化射箭场生产器
         archeryProducer = CreateProducerTimers();
         archeryProducer[0].Tick += (s, e) => ProduceUnits(BuildingType.Archery, 0, VK_Q);
         archeryProducer[1].Tick += (s, e) => ProduceUnits(BuildingType.Archery, 1, VK_W);
         archeryProducer[2].Tick += (s, e) => ProduceUnits(BuildingType.Archery, 2, VK_E);
         archeryProducer[3].Tick += (s, e) => ProduceUnits(BuildingType.Archery, 3, VK_R);
      }
      
      private List<DispatcherTimer> CreateProducerTimers()
      {
         var timers = new List<DispatcherTimer>();
         for (int i = 0; i < PRODUCTION_KEYS_COUNT; i++)
         {
            timers.Add(new DispatcherTimer { Interval = TimeSpan.FromSeconds(0) });
         }
         return timers;
      }
      #endregion

      #region 通用工具方法
      /// <summary>
      /// 通用的单位生产方法
      /// </summary>
      /// <param name="buildingType">建筑类型</param>
      /// <param name="keyIndex">按键索引 (0=Q, 1=W, 2=E, 3=R)</param>
      /// <param name="productionKey">生产按键</param>
      private void ProduceUnits(BuildingType buildingType, int keyIndex, int productionKey)
      {
         var productionArray = GetProductionArray(buildingType);
         int num = productionArray[keyIndex];
         
         if (num <= 0) return;
         
         IntPtr gameWindow = FindWindow(null, GAME_WINDOW_NAME);
         MouseClickBefore(gameWindow);
         SelectBuilding(gameWindow, buildingType);
         
         for (int i = 0; i < num; i++)
         {
            SendMessage(gameWindow, WM_SYSKEYDOWN, productionKey, 0);
         }
         
         SendMessage(gameWindow, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }
      
      /// <summary>
      /// 根据建筑类型获取对应的生产数组
      /// </summary>
      private int[] GetProductionArray(BuildingType buildingType)
      {
         return buildingType switch
         {
            BuildingType.Stable => stableProd,
            BuildingType.Barracks => barracksProd,
            BuildingType.Archery => archeryProd,
            _ => throw new ArgumentException($"不支持的建筑类型: {buildingType}")
         };
      }
      
      /// <summary>
      /// 选择指定的建筑
      /// </summary>
      private void SelectBuilding(IntPtr gameWindow, BuildingType buildingType)
      {
         int selectionKey = buildingType switch
         {
            BuildingType.Stable => VK_L,
            BuildingType.Barracks => VK_J,
            BuildingType.Archery => VK_K,
            _ => throw new ArgumentException($"不支持的建筑类型: {buildingType}")
         };
         
         SendMessage(gameWindow, WM_KEYDOWN, selectionKey, buildingType == BuildingType.Stable ? 0x20000000 : 0);
      }
      #endregion
      
      #region 时间间隔更新
      private void UpdateInterval()
      {
         CivConfig config = civConfigs[civ];
         TimeSpan diff = TimeSpan.FromMicroseconds(TIMER_ADJUSTMENT_MICROSECONDS);
         tcProducer.Interval = TimeSpan.FromSeconds(int.Parse(config.tc.Split(',').Last()));
         int s_q_interval = (int)(int.Parse(config.s_q.Split(',').Last()) * intervalMultiplier);
         if (s_q_interval > 0)
         {
            stableProducer[0].Interval = TimeSpan.FromSeconds(s_q_interval).Add(diff);
         }
         int s_w_interval = (int)(int.Parse(config.s_w.Split(',').Last()) * intervalMultiplier);
         if (s_w_interval > 0)
         {
            stableProducer[1].Interval = TimeSpan.FromSeconds(s_w_interval).Add(diff);
         }
         int s_e_interval = (int)(int.Parse(config.s_e.Split(',').Last()) * intervalMultiplier);
         if (s_e_interval > 0)
         {
            stableProducer[2].Interval = TimeSpan.FromSeconds(s_e_interval).Add(diff);
         }
         int s_r_interval = (int)(int.Parse(config.s_r.Split(',').Last()) * intervalMultiplier);
         if (s_r_interval > 0)
         {
            stableProducer[3].Interval = TimeSpan.FromSeconds(s_r_interval).Add(diff);
         }
         int a_q_interval = (int)(int.Parse(config.a_q.Split(',').Last()) * intervalMultiplier);
         if (a_q_interval > 0)
         {
            archeryProducer[0].Interval = TimeSpan.FromSeconds(a_q_interval).Add(diff);
         }
         int a_w_interval = (int)(int.Parse(config.a_w.Split(',').Last()) * intervalMultiplier);
         if (a_w_interval > 0)
         {
            archeryProducer[1].Interval = TimeSpan.FromSeconds(a_w_interval).Add(diff);
         }
         int a_e_interval = (int)(int.Parse(config.a_e.Split(',').Last()) * intervalMultiplier);
         if (a_e_interval > 0)
         {
            archeryProducer[2].Interval = TimeSpan.FromSeconds(a_e_interval).Add(diff);
         }
         int a_r_interval = (int)(int.Parse(config.a_r.Split(',').Last()) * intervalMultiplier);
         if (a_r_interval > 0)
         {
            archeryProducer[3].Interval = TimeSpan.FromSeconds(a_r_interval).Add(diff);
         }
         int b_q_interval = (int)(int.Parse(config.b_q.Split(',').Last()) * intervalMultiplier);
         if (b_q_interval > 0)
         {
            barracksProducer[0].Interval = TimeSpan.FromSeconds(b_q_interval).Add(diff);
         }
         int b_w_interval = (int)(int.Parse(config.b_w.Split(',').Last()) * intervalMultiplier);
         if (b_w_interval > 0)
         {
            barracksProducer[1].Interval = TimeSpan.FromSeconds(b_w_interval).Add(diff);
         }
         int b_e_interval = (int)(int.Parse(config.b_e.Split(',').Last()) * intervalMultiplier);
         if (b_e_interval > 0)
         {
            barracksProducer[2].Interval = TimeSpan.FromSeconds(b_e_interval).Add(diff);
         }
         int b_r_interval = (int)(int.Parse(config.b_r.Split(',').Last()) * intervalMultiplier);
         if (b_r_interval > 0)
         {
            barracksProducer[3].Interval = TimeSpan.FromSeconds(b_r_interval).Add(diff);
         }
      }

      private void Civ_Changed(object sender, SelectionChangedEventArgs e)
      {
         ComboBoxItem cbx = ((sender as AduComboBox).SelectedItem as ComboBoxItem);
         civ = cbx.Name;
         UpdateInterval();
         CalcFarmerCount();
      }


      private void tcNumChanged(object sender, TextChangedEventArgs e)
      {
         tcNumber = int.Parse(((AduIntegerUpDown)sender).Text);
      }

      private void Tc_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            tcEnabled = true;
            TcProduce();
            if (tcProducer.Interval.TotalSeconds > 0)
            {
               tcProducer.Start();
            }
         }
         else
         {
            tcEnabled = false;
            tcProducer.Stop();
         }
         CalcFarmerCount();
      }

      #region 鼠标操作方法
      private int MAKELPARAM(int p, int p_2)
      {
         return ((p_2 << 16) | (p & 0xFFFF));
      }

      private void MouseClickAfter(IntPtr window)
      {
         PostMessage(window, WM_LBUTTONDOWN, 0, MAKELPARAM(CLICK_X_AFTER, CLICK_Y_AFTER));
         PostMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(CLICK_X_AFTER, CLICK_Y_AFTER));
      }

      private void MouseClickBefore(IntPtr window)
      {
         SendMessage(window, WM_LBUTTONDOWN, 0, MAKELPARAM(CLICK_X_BEFORE, CLICK_Y_BEFORE));
         SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(CLICK_X_BEFORE, CLICK_Y_BEFORE));
      }
      #endregion

      #region TC生产方法
      private void TcProduce()
      {
         if (tcNumber <= 0) return;
         
         IntPtr gameWindow = FindWindow(null, GAME_WINDOW_NAME);
         MouseClickBefore(gameWindow);
         SendMessage(gameWindow, WM_SYSKEYDOWN, VK_SPACE, 0);

         for (int i = 0; i < tcNumber; i++)
         {
            SendMessage(gameWindow, WM_SYSKEYDOWN, VK_Q, 0);
         }
         
         SendMessage(gameWindow, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }
      #endregion

      #region 建筑开关事件处理
      private void Archery_Checked(object sender, RoutedEventArgs e)
      {
         archeryEnabled = ((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true;
         
         if (archeryEnabled)
         {
            // 立即执行一次生产
            for (int i = 0; i < PRODUCTION_KEYS_COUNT; i++)
            {
               var keyValue = new[] { VK_Q, VK_W, VK_E, VK_R }[i];
               ProduceUnits(BuildingType.Archery, i, keyValue);
            }

            archeryProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0) t.Start();
            });
         }
         else
         {
            archeryProducer.ForEach(t => t.Stop());
         }
         
         CalcFarmerCount();
      }

      private void Barracks_Checked(object sender, RoutedEventArgs e)
      {
         barracksEnabled = ((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true;
         
         if (barracksEnabled)
         {
            // 立即执行一次生产
            for (int i = 0; i < PRODUCTION_KEYS_COUNT; i++)
            {
               var keyValue = new[] { VK_Q, VK_W, VK_E, VK_R }[i];
               ProduceUnits(BuildingType.Barracks, i, keyValue);
            }

            barracksProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0) t.Start();
            });
         }
         else
         {
            barracksProducer.ForEach(t => t.Stop());
         }
         
         CalcFarmerCount();
      }

      private void Stable_Checked(object sender, RoutedEventArgs e)
      {
         stableEnabled = ((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true;
         
         if (stableEnabled)
         {
            // 立即执行一次生产
            for (int i = 0; i < PRODUCTION_KEYS_COUNT; i++)
            {
               var keyValue = new[] { VK_Q, VK_W, VK_E, VK_R }[i];
               ProduceUnits(BuildingType.Stable, i, keyValue);
            }

            stableProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0) t.Start();
            });
         }
         else
         {
            stableProducer.ForEach(t => t.Stop());
         }
         
         CalcFarmerCount();
      }
      #endregion

      #region 生产数量变更事件处理
      // 射箭场生产数量变更
      private void a_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 0, sender);
      private void a_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 1, sender);
      private void a_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 2, sender);
      private void a_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Archery, 3, sender);
      
      // 马厩生产数量变更
      private void s_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 0, sender);
      private void s_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 1, sender);
      private void s_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 2, sender);
      private void s_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Stable, 3, sender);
      
      // 兵营生产数量变更
      private void b_q_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 0, sender);
      private void b_w_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 1, sender);
      private void b_e_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 2, sender);
      private void b_r_Changed(object sender, EventArgs e) => UpdateProductionCount(BuildingType.Barracks, 3, sender);
      
      /// <summary>
      /// 通用的生产数量更新方法
      /// </summary>
      private void UpdateProductionCount(BuildingType buildingType, int keyIndex, object sender)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         GetProductionArray(buildingType)[keyIndex] = num;
         CalcFarmerCount();
      }
      #endregion



      private void CalcFarmerCount()
      {
         //if (foodFarmer != null)
         //{
         //   foodFarmer.Text = Convert.ToDouble(calcFarmerFood()).ToString("0.00");

         //}
         //if (woodFarmer != null)
         //{
         //   woodFarmer.Text = Convert.ToDouble(calcFarmerWood()).ToString("0.00");
         //}
         //if (goldFarmer != null)
         //{
         //   goldFarmer.Text = Convert.ToDouble(calcFarmerGold()).ToString("0.00");
         //}
      }

      private double CalcFarmerFood()
      {
         double cost = 0;
         if (tcEnabled)
         {
            cost += double.Parse(civConfigs[civ].tc.Split(',').Last()) > 0 ? tcNumber * double.Parse(civConfigs[civ].tc.Split(',')[0]) / double.Parse(civConfigs[civ].tc.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[0]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[0]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[0]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[0]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[0]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[0]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[0]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[0]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[0]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[0]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[0]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[0]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         double pd = double.Parse(civConfigs[civ].food);
         return cost / pd;
      }

      private double CalcFarmerWood()
      {
         double cost = 0;
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[1]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[1]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[1]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[1]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[1]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[1]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[1]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[1]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[1]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[1]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[1]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[1]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         double pd = double.Parse(civConfigs[civ].wood);
         return cost / pd;
      }

      private double CalcFarmerGold()
      {
         double cost = 0;
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[2]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[2]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[2]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[2]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[2]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[2]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[2]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[2]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[2]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[2]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[2]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[2]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * SECONDS_PER_MINUTE : 0;
         }
         double pd = double.Parse(civConfigs[civ].gold);
         return cost / pd;
      }

      private void Ma_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            intervalMultiplier *= MA_MULTIPLIER;
         }
         else
         {
            intervalMultiplier /= MA_MULTIPLIER;
         }
         UpdateInterval();
      }
   }
}
#endregion