using AduSkin.Demo.Models;
using AduSkin.Demo.ViewModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;

namespace AduSkin.Demo.UserControls
{
   /// <summary>
   /// SortGroup.xaml 的交互逻辑
   /// </summary>
   public partial class SortGroup : UserControl
   {
      public SortGroup()
      {
         InitializeComponent();
      }

      /// <summary>
      /// 命令Command
      /// </summary>
      [RelayCommand]
      public void ToGroup(ChatUserModel e)
      {
         if (e == null || DataContext is not SortGroupViewModel vm) return;
         vm.IsOpenSortList = false;
         ListBoxContact.UpdateLayout();
         ListBoxContact.AnimateScrollIntoView(e);
      }
   }
}
