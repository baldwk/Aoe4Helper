using System.Windows;

namespace Aoe4Helper.Servers.Contracts
{
   public interface IWindow
   {
      event RoutedEventHandler Loaded;

      void Show();
   }
}
