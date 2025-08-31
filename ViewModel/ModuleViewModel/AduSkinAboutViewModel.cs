using AduSkin.Demo.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace AduSkin.Demo.ViewModel
{
   public partial class AduSkinAboutViewModel : ObservableObject
   {
      /// <summary>
      /// 命令Command
      /// </summary>
      [RelayCommand]
      public void OpenDemo(string e)
      {
         switch (e)
         {
            case "Reward":
               IsOpenReward = true;
               return;
         }
      }

      [ObservableProperty]
      private bool _isOpenReward;
   }
}
