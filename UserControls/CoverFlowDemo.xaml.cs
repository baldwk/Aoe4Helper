using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace AduSkin.Demo.UserControls
{
   /// <summary>
   /// CoverFlowDemo.xaml 的交互逻辑
   /// </summary>
   public partial class CoverFlowDemo : UserControl
   {
      public CoverFlowDemo()
      {
         InitializeComponent();
         
         CoverFlowMain.ItemsSource = AduSkin.Demo.Data.CarouselSamples.Create();
         CoverFlowMain.JumpTo(2);
      }
   }
}
