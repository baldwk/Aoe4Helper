using AduSkin.Demo.Data.Enum;
using AduSkin.Demo.Models;
using AduSkin.Demo.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace AduSkin.Demo.ViewModel
{
   public class PracticalCaseViewModel : ObservableObject
   {
      public PracticalCaseViewModel()
      {

         AllControl = new List<ControlModel>()
         {
            new ControlModel("Win10菜单", typeof(SortGroup)),
            new ControlModel("表单校验", typeof(FormVerificationDemo)),
            new ControlModel("步骤条", typeof(StepBarDemo)),
            new ControlModel("图片上传", typeof(UploadPic)),
            new ControlModel("视频控件", typeof(VideoPlayer)),
            new ControlModel("折叠菜单", typeof(ExpanderMenu)),
            new ControlModel("导航容器", typeof(NavigationPanel)),
            new ControlModel("轮播容器", typeof(CarouselContainer)),
            new ControlModel("封面流", typeof(CoverFlowDemo), DemoType.Demo, ControlState.New),
            new ControlModel("时间轴", typeof(TimeLine)),
            new ControlModel("时间线", typeof(TimeBarDemo), DemoType.Demo, ControlState.New),
            new ControlModel("树形菜单", typeof(TreeMenu)),
            new ControlModel("多功能Tab", typeof(MultiFunctionTabControl)),
            new ControlModel("右键菜单", typeof(ContextMenuDemo), DemoType.Demo),
            new ControlModel("右侧弹框", typeof(NoticeDemo)),
            new ControlModel("过渡容器", typeof(TransitioningContentControlDemo), DemoType.Demo),
            new ControlModel("消息弹框", typeof(MessageBoxDemo), DemoType.Demo, ControlState.New),
            new ControlModel("滚动字幕", typeof(RunningBlockDemo)),
         };

         SearchControl.Source = AllControl;
         SearchControl.View.Culture = CultureInfo.GetCultureInfo("zh-CN");
         SearchControl.View.Filter = obj => obj is ControlModel model &&
            ((model.Title ?? "") + (model.TitlePinyin ?? "")).Contains(SearchKey, StringComparison.OrdinalIgnoreCase);
         SearchControl.View.SortDescriptions.Add(new SortDescription(nameof(ControlModel.Title), ListSortDirection.Ascending));
      }

      public IReadOnlyList<ControlModel> AllControl { get; }
      public CollectionViewSource SearchControl { get; } = new();
      public bool IsShowCode => CurrentShowControl?.Type == DemoType.Demo;
      public double ShowCodeHeight => IsShowCode ? 40 : 0;
      public string Title => CurrentShowControl?.Title;
      public string CurrentShowCode => (ShowCodeTypeIndex == 0 ? CurrentShowControl?.XAML : CurrentShowControl?.Code) ?? "";

      private ControlModel currentShowControl;
      public ControlModel CurrentShowControl
      {
         get => currentShowControl;
         set
         {
            if (!SetProperty(ref currentShowControl, value)) return;
            Content = value?.Content == null ? null : (UserControl)Activator.CreateInstance(value.Content);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(IsShowCode));
            OnPropertyChanged(nameof(ShowCodeHeight));
            OnPropertyChanged(nameof(CurrentShowCode));
         }
      }

      private int showCodeTypeIndex;
      public int ShowCodeTypeIndex
      {
         get => showCodeTypeIndex;
         set
         {
            if (SetProperty(ref showCodeTypeIndex, value)) OnPropertyChanged(nameof(CurrentShowCode));
         }
      }

      private UserControl content;
      public UserControl Content
      {
         get => content;
         private set => SetProperty(ref content, value);
      }

      private string searchKey = "";
      public string SearchKey
      {
         get => searchKey;
         set
         {
            if (SetProperty(ref searchKey, value ?? "")) SearchControl.View.Refresh();
         }
      }
   }
}
