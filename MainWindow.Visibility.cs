using Aoe4Helper.Interop;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace Aoe4Helper
{
   public partial class MainWindow
   {
      private GameOverlayHotkey visibilityHotkey;
      private Forms.NotifyIcon trayIcon;
      private Icon trayImage;
      private bool overlayClosed;

      protected override void OnSourceInitialized(EventArgs e)
      {
         base.OnSourceInitialized(e);
         trayImage = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
         var menu = new Forms.ContextMenuStrip();
         menu.Items.Add("显示助手", null, (_, _) => Dispatcher.BeginInvoke(new Action(ShowFromTray)));
         menu.Items.Add("隐藏助手", null, (_, _) => Dispatcher.BeginInvoke(new Action(Hide)));
         menu.Items.Add("退出", null, (_, _) => Dispatcher.BeginInvoke(new Action(Close)));
         trayIcon = new Forms.NotifyIcon
         {
            Text = "Aoe4Helper · 游戏内按 P 显示/隐藏",
            Icon = trayImage ?? SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
         };
         trayIcon.DoubleClick += (_, _) => Dispatcher.BeginInvoke(new Action(ShowFromTray));
         try
         {
            visibilityHotkey = new GameOverlayHotkey(window =>
               Dispatcher.BeginInvoke(new Action(() => ToggleOverlay(window))));
         }
         catch (Exception ex)
         {
            ShowProductionError("P 快捷键初始化失败：" + ex.Message);
         }
      }

      private void ToggleOverlay(IntPtr previousForeground)
      {
         if (overlayClosed) return;
         if (IsVisible && WindowState != WindowState.Minimized)
         {
            bool helperHadFocus = previousForeground == new WindowInteropHelper(this).Handle || IsActive;
            Hide();
            if (helperHadFocus) ReturnFocusToGame();
         }
         else
         {
            WindowState = WindowState.Normal;
            Show(); // ShowActivated=False：P 唤出面板时保持游戏焦点。
         }
      }

      private void ShowFromTray()
      {
         if (overlayClosed) return;
         WindowState = WindowState.Normal;
         Show();
         Activate();
      }

      private static void ReturnFocusToGame()
      {
         var games = Process.GetProcessesByName("RelicCardinal");
         try
         {
            foreach (var game in games)
            {
               try
               {
                  if (game.MainWindowHandle == IntPtr.Zero) continue;
                  SetForegroundWindow(game.MainWindowHandle);
                  break;
               }
               catch (InvalidOperationException) { }
               catch (System.ComponentModel.Win32Exception) { }
            }
         }
         finally { foreach (var game in games) game.Dispose(); }
      }

      protected override void OnClosed(EventArgs e)
      {
         overlayClosed = true;
         visibilityHotkey?.Dispose();
         if (trayIcon != null)
         {
            trayIcon.Visible = false;
            trayIcon.ContextMenuStrip?.Dispose();
            trayIcon.Dispose();
         }
         trayImage?.Dispose();
         base.OnClosed(e);
      }

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetForegroundWindow(IntPtr window);
   }
}
