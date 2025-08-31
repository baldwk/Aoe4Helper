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

      private DispatcherTimer tcProducer;
      private List<DispatcherTimer> stableProducer, archeryProducer, barracksProducer;

      private Dictionary<string, CivConfig> civConfigs;

      [DllImport("user32.dll")]
      public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

      [DllImport("user32.dll")]
      public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
      [DllImport("user32.dll")]
      public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

      Random rnd = new Random();

      private double intervalMultiplier = 1.0;

      const int WM_SYSKEYDOWN = 0x0104;
      const int WM_SYSKEYUP = 0x0105;
      public const Int32 WM_CHAR = 0x0102;
      private const int WM_KEYDOWN = 0x100;
      private const int WM_KEYUP = 0x0101;
      const int WM_LBUTTONDOWN = 0x0201;
      const int WM_LBUTTONUP = 0x0202;
      private const int VK_LBUTTON = 0X1; //Left mouse button. 
      private const int VK_RBUTTON = 0X2; //Right mouse button. 
      private const int VK_CANCEL = 0X3; //Used for control-break processing. 
      private const int VK_MBUTTON = 0X4; //''Middle mouse button (3-button mouse). 
      private const int KEYEVENTF_KEYUP = 0X2; // Release key 
      private const int VK_OEM_PERIOD = 0XBE; //.
      private const int KEYEVENTF_EXTENDEDKEY = 0X1;
      private const int VK_STARTKEY = 0X5B; //Start Menu key 
      private const int VK_OEM_COMMA = 0XBC; //, comma 
      public const int VK_0 = 0x30;
      public const int VK_1 = 0x31;
      public const int VK_2 = 0x32;
      public const int VK_3 = 0x33;
      public const int VK_4 = 0x34;
      public const int VK_5 = 0x35;
      public const int VK_6 = 0x36;
      public const int VK_7 = 0x37;
      public const int VK_8 = 0x38;
      public const int VK_9 = 0x39;
      public const int VK_A = 0x41;
      public const int VK_B = 0x42;
      public const int VK_C = 0x43;
      public const int VK_D = 0x44;
      public const int VK_E = 0x45;
      public const int VK_F = 0x46;
      public const int VK_G = 0x47;
      public const int VK_H = 0x48;
      public const int VK_I = 0x49;
      public const int VK_J = 0x4A;
      public const int VK_K = 0x4B;
      public const int VK_L = 0x4C;
      public const int VK_M = 0x4D;
      public const int VK_N = 0x4E;
      public const int VK_O = 0x4F;
      public const int VK_P = 0x50;
      public const int VK_Q = 0x51;
      public const int VK_R = 0x52;
      public const int VK_S = 0x53;
      public const int VK_T = 0x54;
      public const int VK_U = 0x55;
      public const int VK_V = 0x56;
      public const int VK_W = 0x57;
      public const int VK_X = 0x58;
      public const int VK_Y = 0x59;
      public const int VK_Z = 0x5A;
      public const int VK_BACK = 0x08;
      public const int VK_TAB = 0x09;
      public const int VK_CLEAR = 0x0C;
      public const int VK_RETURN = 0x0D;
      public const int VK_SHIFT = 0x10;
      public const int VK_CONTROL = 0x11;
      public const int VK_MENU = 0x12;
      public const int VK_PAUSE = 0x13;
      public const int VK_CAPITAL = 0x14;
      public const int VK_KANA = 0x15;
      public const int VK_HANGEUL = 0x15;
      public const int VK_HANGUL = 0x15;
      public const int VK_JUNJA = 0x17;
      public const int VK_FINAL = 0x18;
      public const int VK_HANJA = 0x19;
      public const int VK_KANJI = 0x19;
      public const int VK_ESCAPE = 0x1B;
      public const int VK_CONVERT = 0x1C;
      public const int VK_NONCONVERT = 0x1D;
      public const int VK_ACCEPT = 0x1E;
      public const int VK_MODECHANGE = 0x1F;
      public const int VK_SPACE = 0x20;
      public const int VK_PRIOR = 0x21;
      public const int VK_NEXT = 0x22;
      public const int VK_END = 0x23;
      public const int VK_HOME = 0x24;
      public const int VK_LEFT = 0x25;
      public const int VK_UP = 0x26;
      public const int VK_RIGHT = 0x27;
      public const int VK_DOWN = 0x28;
      public const int VK_SELECT = 0x29;
      public const int VK_PRINT = 0x2A;
      public const int VK_EXECUTE = 0x2B;
      public const int VK_SNAPSHOT = 0x2C;
      public const int VK_INSERT = 0x2D;
      public const int VK_DELETE = 0x2E;
      public const int VK_HELP = 0x2F;
      public const int VK_LWIN = 0x5B;
      public const int VK_RWIN = 0x5C;
      public const int VK_APPS = 0x5D;
      public const int VK_SLEEP = 0x5F;
      public const int VK_NUMPAD0 = 0x60;
      public const int VK_NUMPAD1 = 0x61;
      public const int VK_NUMPAD2 = 0x62;
      public const int VK_NUMPAD3 = 0x63;
      public const int VK_NUMPAD4 = 0x64;
      public const int VK_NUMPAD5 = 0x65;
      public const int VK_NUMPAD6 = 0x66;
      public const int VK_NUMPAD7 = 0x67;
      public const int VK_NUMPAD8 = 0x68;
      public const int VK_NUMPAD9 = 0x69;
      public const int VK_MULTIPLY = 0x6A;
      public const int VK_ADD = 0x6B;
      public const int VK_SEPARATOR = 0x6C;
      public const int VK_SUBTRACT = 0x6D;
      public const int VK_DECIMAL = 0x6E;
      public const int VK_DIVIDE = 0x6F;
      public const int VK_F1 = 0x70;
      public const int VK_F2 = 0x71;
      public const int VK_F3 = 0x72;
      public const int VK_F4 = 0x73;
      public const int VK_F5 = 0x74;
      public const int VK_F6 = 0x75;
      public const int VK_F7 = 0x76;
      public const int VK_F8 = 0x77;
      public const int VK_F9 = 0x78;
      public const int VK_F10 = 0x79;

      private int tcNumber;
      private int[] stableProd = new int[] { 0, 0, 0, 0 };

      private int[] archeryProd = new int[] { 0, 0, 0, 0 };
      private int[] barracksProd = new int[] { 0, 0, 0, 0 };

      private Boolean tcEnabled = false, stableEnabled = false, archeryEnabled = false, barracksEnabled = false;
      private string civ = "rus";

      public MainWindow(MainViewModel viewModel)
      {
         loadCivConfig();
         InitializeComponent();
         this.DataContext = viewModel;
         this.Closed += delegate { Application.Current.Shutdown(); };
         initTimer();
      }

      private void loadCivConfig()
      {
         var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
         var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

         var ymlFile = "./prod.tab";
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

      private void initTimer()
      {
         // Initialize tcProducer  
         tcProducer = new DispatcherTimer
         {
            Interval = TimeSpan.FromSeconds(0)
         };
         tcProducer.Tick += tcProduce;

         // Initialize stableProducer  
         stableProducer = new List<DispatcherTimer>();
         for (int i = 0; i < 4; i++)
         {
            DispatcherTimer prod = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0) };
            stableProducer.Add(prod);
         }
         stableProducer[0].Tick += s_q_produce;
         stableProducer[1].Tick += s_w_produce;
         stableProducer[2].Tick += s_e_produce;
         stableProducer[3].Tick += s_r_produce;
         // Initialize barracksProducer  
         barracksProducer = new List<DispatcherTimer>();
         for (int i = 0; i < 4; i++)
         {
            DispatcherTimer prod = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0) };
            barracksProducer.Add(prod);
         }
         barracksProducer[0].Tick += b_q_produce;
         barracksProducer[1].Tick += b_w_produce;
         barracksProducer[2].Tick += b_e_produce;
         barracksProducer[3].Tick += b_r_produce;

         // Initialize archeryProducer  
         archeryProducer = new List<DispatcherTimer>();
         for (int i = 0; i < 4; i++)
         {
            DispatcherTimer prod = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0) };
            archeryProducer.Add(prod);
         }
         archeryProducer[0].Tick += a_q_produce;
         archeryProducer[1].Tick += a_w_produce;
         archeryProducer[2].Tick += a_e_produce;
         archeryProducer[3].Tick += a_r_produce;
      }

      private void updateInterval()
      {
         CivConfig config = civConfigs[civ];
         TimeSpan diff = TimeSpan.FromMicroseconds(-100);
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
         updateInterval();
         calcFarmerCount();
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
            tcProduce(null, null);
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
         calcFarmerCount();
      }

      private int MAKELPARAM(int p, int p_2)
      {
         return ((p_2 << 16) | (p & 0xFFFF));
      }

      private void mouseClickAfter(IntPtr window)
      {
         int x = 1000;
         int y = 800;
         PostMessage(window, WM_LBUTTONDOWN, 0, MAKELPARAM(x, y));
         PostMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(x, y));
      }

      private void mouseClickBefore(IntPtr window)
      {
         int x = 500;
         int y = 800;
         SendMessage(window, WM_LBUTTONDOWN, 0, MAKELPARAM(x, y));
         SendMessage(window, WM_LBUTTONUP, 0, MAKELPARAM(x, y));
      }

      private void tcProduce(object sender, EventArgs e)
      {
         if (tcNumber <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel

         mouseClickBefore(WindowToFind);
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_SPACE, 0);
         //PostMessage(WindowToFind, WM_KEYDOWN, VK_SPACE, 0);

         for (int i = 0; i < tcNumber; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_Q, 0);
            // PostMessage(WindowToFind, WM_SYSKEYUP, VK_Q, 0);

         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void Archery_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            archeryEnabled = true;
            a_q_produce(null, null);
            a_w_produce(null, null);
            a_e_produce(null, null);
            a_r_produce(null, null);

            archeryProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0)
               {
                  t.Start();
               }
            });
         }
         else
         {
            archeryEnabled = false;
            archeryProducer.ForEach(t => t.Stop());
         }
         calcFarmerCount();

      }

      private void Barracks_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            barracksEnabled = true;
            b_q_produce(null, null);
            b_w_produce(null, null);
            b_e_produce(null, null);
            b_r_produce(null, null);
            barracksProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0)
               {
                  t.Start();
               }
            });
         }
         else
         {
            barracksEnabled = false;
            barracksProducer.ForEach(t => t.Stop());
         }
         calcFarmerCount();
      }

      private void Stable_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            stableEnabled = true;
            s_q_produce(null, null);
            s_w_produce(null, null);
            s_e_produce(null, null);
            s_r_produce(null, null);
            stableProducer.ForEach(t =>
            {
               if (t.Interval.TotalSeconds > 0)
               {
                  t.Start();
               }
            });
         }
         else
         {
            stableEnabled = false;
            stableProducer.ForEach(t => t.Stop());
         }
         calcFarmerCount();
      }

      private void selectStable(IntPtr WindowToFind)
      {
         SendMessage(WindowToFind, WM_KEYDOWN, VK_L, 0x20000000);
      }

      private void s_q_produce(object sender, EventArgs e)
      {
         int num = stableProd[0];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectStable(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_Q, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void s_w_produce(object sender, EventArgs e)
      {
         int num = stableProd[1];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectStable(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_W, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void s_e_produce(object sender, EventArgs e)
      {
         int num = stableProd[2];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectStable(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_E, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void s_r_produce(object sender, EventArgs e)
      {
         int num = stableProd[3];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectStable(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_R, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }


      private void selectArchery(IntPtr WindowToFind)
      {
         SendMessage(WindowToFind, WM_KEYDOWN, VK_K, 0);

      }
      private void a_q_produce(object sender, EventArgs e)
      {
         int num = archeryProd[0];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectArchery(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_Q, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void a_w_produce(object sender, EventArgs e)
      {
         int num = archeryProd[1];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectArchery(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_W, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void a_e_produce(object sender, EventArgs e)
      {
         int num = archeryProd[2];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectArchery(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_E, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void a_r_produce(object sender, EventArgs e)
      {
         int num = archeryProd[3];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectArchery(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_R, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }


      private void selectBarracks(IntPtr WindowToFind)
      {
         SendMessage(WindowToFind, WM_KEYDOWN, VK_J, 0);

      }
      private void b_q_produce(object sender, EventArgs e)
      {
         int num = barracksProd[0];

         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectBarracks(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_Q, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void b_w_produce(object sender, EventArgs e)
      {
         int num = barracksProd[1];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectBarracks(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_W, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void b_e_produce(object sender, EventArgs e)
      {
         int num = barracksProd[2];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectBarracks(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_E, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }

      private void b_r_produce(object sender, EventArgs e)
      {
         int num = barracksProd[3];
         if (num <= 0)
         {
            return;
         }
         IntPtr WindowToFind = FindWindow(null, "Age of Empires IV "); // Window Titel
         mouseClickBefore(WindowToFind);
         selectBarracks(WindowToFind);
         for (int i = 0; i < num; i++)
         {
            SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_R, 0);
         }
         SendMessage(WindowToFind, WM_SYSKEYDOWN, VK_ESCAPE, 0);
      }


      private void a_q_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         archeryProd[0] = num;
         calcFarmerCount();
      }

      private void a_w_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         archeryProd[1] = num;
         calcFarmerCount();
      }

      private void a_e_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         archeryProd[2] = num;
         calcFarmerCount();
      }

      private void a_r_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         archeryProd[3] = num;
         calcFarmerCount();
      }

      private void s_q_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         stableProd[0] = num;
         calcFarmerCount();
      }

      private void s_w_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         stableProd[1] = num;
         calcFarmerCount();
      }

      private void s_e_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         stableProd[2] = num;
         calcFarmerCount();
      }

      private void s_r_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         stableProd[3] = num;
         calcFarmerCount();
      }

      private void b_q_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         barracksProd[0] = num;
         calcFarmerCount();
      }

      private void b_w_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         barracksProd[1] = num;
         calcFarmerCount();
      }

      private void b_e_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         barracksProd[2] = num;
         calcFarmerCount();
      }

      private void b_r_Changed(object sender, EventArgs e)
      {
         int num = int.Parse(((AduIntegerUpDown)sender).Text);
         barracksProd[3] = num;
         calcFarmerCount();
      }

      private void calcFarmerCount()
      {
         if (foodFarmer != null)
         {
            foodFarmer.Text = Convert.ToDouble(calcFarmerFood()).ToString("0.00");

         }
         if (woodFarmer != null)
         {
            woodFarmer.Text = Convert.ToDouble(calcFarmerWood()).ToString("0.00");
         }
         if (goldFarmer != null)
         {
            goldFarmer.Text = Convert.ToDouble(calcFarmerGold()).ToString("0.00");
         }
      }

      private double calcFarmerFood()
      {
         double cost = 0;
         if (tcEnabled)
         {
            cost += double.Parse(civConfigs[civ].tc.Split(',').Last()) > 0 ? tcNumber * double.Parse(civConfigs[civ].tc.Split(',')[0]) / double.Parse(civConfigs[civ].tc.Split(',').Last()) * 60 : 0;
         }
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[0]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[0]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[0]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[0]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * 60 : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[0]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[0]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[0]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[0]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * 60 : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[0]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[0]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[0]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[0]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * 60 : 0;
         }
         double pd = double.Parse(civConfigs[civ].food);
         return cost / pd;
      }

      private double calcFarmerWood()
      {
         double cost = 0;
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[1]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[1]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[1]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[1]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * 60 : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[1]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[1]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[1]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[1]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * 60 : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[1]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[1]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[1]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[1]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * 60 : 0;
         }
         double pd = double.Parse(civConfigs[civ].wood);
         return cost / pd;
      }

      private double calcFarmerGold()
      {
         double cost = 0;
         if (stableEnabled)
         {
            cost += double.Parse(civConfigs[civ].s_q.Split(',').Last()) > 0 ? stableProd[0] * double.Parse(civConfigs[civ].s_q.Split(',')[2]) / double.Parse(civConfigs[civ].s_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_w.Split(',').Last()) > 0 ? stableProd[1] * double.Parse(civConfigs[civ].s_w.Split(',')[2]) / double.Parse(civConfigs[civ].s_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_e.Split(',').Last()) > 0 ? stableProd[2] * double.Parse(civConfigs[civ].s_e.Split(',')[2]) / double.Parse(civConfigs[civ].s_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].s_r.Split(',').Last()) > 0 ? stableProd[3] * double.Parse(civConfigs[civ].s_r.Split(',')[2]) / double.Parse(civConfigs[civ].s_r.Split(',').Last()) * 60 : 0;
         }
         if (archeryEnabled)
         {
            cost += double.Parse(civConfigs[civ].a_q.Split(',').Last()) > 0 ? archeryProd[0] * double.Parse(civConfigs[civ].a_q.Split(',')[2]) / double.Parse(civConfigs[civ].a_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_w.Split(',').Last()) > 0 ? archeryProd[1] * double.Parse(civConfigs[civ].a_w.Split(',')[2]) / double.Parse(civConfigs[civ].a_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_e.Split(',').Last()) > 0 ? archeryProd[2] * double.Parse(civConfigs[civ].a_e.Split(',')[2]) / double.Parse(civConfigs[civ].a_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].a_r.Split(',').Last()) > 0 ? archeryProd[3] * double.Parse(civConfigs[civ].a_r.Split(',')[2]) / double.Parse(civConfigs[civ].a_r.Split(',').Last()) * 60 : 0;
         }
         if (barracksEnabled)
         {
            cost += double.Parse(civConfigs[civ].b_q.Split(',').Last()) > 0 ? barracksProd[0] * double.Parse(civConfigs[civ].b_q.Split(',')[2]) / double.Parse(civConfigs[civ].b_q.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_w.Split(',').Last()) > 0 ? barracksProd[1] * double.Parse(civConfigs[civ].b_w.Split(',')[2]) / double.Parse(civConfigs[civ].b_w.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_e.Split(',').Last()) > 0 ? barracksProd[2] * double.Parse(civConfigs[civ].b_e.Split(',')[2]) / double.Parse(civConfigs[civ].b_e.Split(',').Last()) * 60 : 0;
            cost += double.Parse(civConfigs[civ].b_r.Split(',').Last()) > 0 ? barracksProd[3] * double.Parse(civConfigs[civ].b_r.Split(',')[2]) / double.Parse(civConfigs[civ].b_r.Split(',').Last()) * 60 : 0;
         }
         double pd = double.Parse(civConfigs[civ].gold);
         return cost / pd;
      }

      private void Ma_Checked(object sender, RoutedEventArgs e)
      {
         if (((AduSkin.Controls.Metro.MetroSwitch)sender).IsChecked == true)
         {
            intervalMultiplier *= 0.66;
         }
         else
         {
            intervalMultiplier /= 0.66;
         }
         updateInterval();
      }
   }
}