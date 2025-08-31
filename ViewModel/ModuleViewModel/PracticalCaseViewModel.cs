using AduSkin.Demo.Data.Enum;
using AduSkin.Demo.Models;
using AduSkin.Demo.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace AduSkin.Demo.ViewModel
{
   public class PracticalCaseViewModel : ObservableObject
   {
      public PracticalCaseViewModel()
      {
         #region 实用控件
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
         _SearchControl.Source = _AllControl;
         _SearchControl.View.Culture = new System.Globalization.CultureInfo("zh-CN");
         _SearchControl.View.Filter = (obj) => ((obj as ControlModel).Title + (obj as ControlModel).TitlePinyin).ToLower().Contains(SearchKey.ToLower());
         _SearchControl.View.SortDescriptions.Add(new SortDescription(nameof(ControlModel.Title), ListSortDirection.Ascending));
         #endregion

         #region 工具栏
         AllTool = new List<ControlModel>()
         {
            //new ControlModel("百度翻译", typeof(BaiduTranslate),DemoType.Tool),
            new ControlModel("接口调试工具", typeof(HttpTool), DemoType.Tool, ControlState.InDev),
         };
         _SearchTool.Source = _AllTool;
         _SearchTool.View.Culture = new System.Globalization.CultureInfo("zh-CN");
         _SearchTool.View.Filter = (obj) => ((obj as ControlModel).Title + (obj as ControlModel).TitlePinyin).ToLower().Contains(SearchKey.ToLower());
         _SearchTool.View.SortDescriptions.Add(new SortDescription(nameof(ControlModel.Title), ListSortDirection.Ascending));
         #endregion
      }

      private int _SelectedDemoType;
      /// <summary>
      /// 当前列表显示类型
      /// </summary>
      public int SelectedDemoType
      {
         get { return _SelectedDemoType; }
         set
         {

            SetProperty(ref _SelectedDemoType, value);
            if (value == 0)
               CurrentShowControl = _AllControl.First();
            else if (value == 1)
               CurrentShowTool = _AllTool.FirstOrDefault();
            OnPropertyChanged("IsShowCode");
            OnPropertyChanged("SearchKey");
         }
      }

      #region 控件Demo
      private IEnumerable<ControlModel> _AllControl;
      /// <summary>
      /// 所有控件
      /// </summary>
      public IEnumerable<ControlModel> AllControl
      {
         get { return _AllControl; }
         set { SetProperty(ref _AllControl, value); }
      }

      private CollectionViewSource _SearchControl = new CollectionViewSource();
      /// <summary>
      /// 所有控件
      /// </summary>
      public CollectionViewSource SearchControl
      {
         get { return _SearchControl; }
         set
         {
            SetProperty(ref _SearchControl, value);
         }
      }
      #endregion

      #region 工具
      private IEnumerable<ControlModel> _AllTool;
      /// <summary>
      /// 所有控件
      /// </summary>
      public IEnumerable<ControlModel> AllTool
      {
         get { return _AllTool; }
         set { SetProperty(ref _AllTool, value); }
      }

      private CollectionViewSource _SearchTool = new CollectionViewSource();
      /// <summary>
      /// 所有控件
      /// </summary>
      public CollectionViewSource SearchTool
      {
         get { return _SearchTool; }
         set
         {
            SetProperty(ref _SearchTool, value);
         }
      }
      #endregion

      /// <summary>
      /// 显示代码案例栏
      /// </summary>
      public bool IsShowCode => CurrentShowControl?.Type == DemoType.Demo && SelectedDemoType == 0;

      /// <summary>
      /// 代码案例显示高度
      /// </summary>
      public double ShowCodeHeight
      {
         get
         {
            if (CurrentShowControl?.Type == DemoType.Demo && SelectedDemoType == 0)
               return 40;
            else
               return 0;
         }
      }

      private string _CurrentShowCode;
      /// <summary>
      /// 案例代码
      /// </summary>
      public string CurrentShowCode
      {
         get { return _CurrentShowCode; }
         set { SetProperty(ref _CurrentShowCode, value); }
      }

      private ControlModel _CurrentShowControl;
      /// <summary>
      /// 当前显示控件
      /// </summary>
      public ControlModel CurrentShowControl
      {
         get { return _CurrentShowControl; }
         set
         {
            SetProperty(ref _CurrentShowControl, value);
            OnPropertyChanged("Content");
            OnPropertyChanged("Title");
            OnPropertyChanged("IsShowCode");
            OnPropertyChanged("ShowCodeHeight");
         }
      }

      private ControlModel _CurrentShowTool;
      /// <summary>
      /// 当前显示工具
      /// </summary>
      public ControlModel CurrentShowTool
      {
         get { return _CurrentShowTool; }
         set
         {
            SetProperty(ref _CurrentShowTool, value);
            OnPropertyChanged("Content");
            OnPropertyChanged("Title");
            OnPropertyChanged("IsShowCode");
            OnPropertyChanged("ShowCodeHeight");
         }
      }

      private int _ShowCodeTypeIndex = 0;
      /// <summary>
      /// 显示代码类型
      /// </summary>
      public int ShowCodeTypeIndex
      {
         get { return _ShowCodeTypeIndex; }
         set
         {
            SetProperty(ref _ShowCodeTypeIndex, value);
            if (value == 0)
               CurrentShowCode = CurrentShowControl.XAML;
            else
               CurrentShowCode = CurrentShowControl.Code;
         }
      }

      /// <summary>
      /// 控件显示
      /// </summary>
      private UserControl _content;
      public UserControl Content
      {
         get
         {
            if (SelectedDemoType == 0)
            {
               if (CurrentShowControl == null)
                  return null;
               return (UserControl)Activator.CreateInstance(CurrentShowControl.Content);
            }
            else
            {
               if (CurrentShowControl == null)
                  return null;
               return (UserControl)Activator.CreateInstance(CurrentShowTool.Content);
            }
         }
         set
         {
            SetProperty(ref _content, value);
         }
      }
      /// <summary>
      /// 标题
      /// </summary>
      private string _Title;
      public string Title
      {
         get
         {

            if (SelectedDemoType == 0)
            {
               if (CurrentShowControl == null)
                  return null;
               return CurrentShowControl.Title;
            }
            else
            {
               if (CurrentShowTool == null)
                  return null;
               return CurrentShowTool.Title;
            }
         }
         set { SetProperty(ref _Title, value); }
      }
      /// <summary>
      /// 搜索关键字
      /// </summary>
      private string _SearchKey = "";
      public string SearchKey
      {
         get
         {
            return _SearchKey;
         }
         set
         {
            SetProperty(ref _SearchKey, value);
            if (SelectedDemoType == 0)
               if (_SearchControl != null)
                  _SearchControl.View.Refresh();
               else if (SelectedDemoType == 1)
                  if (_SearchTool != null)
                     _SearchTool.View.Refresh();
         }
      }
   }
}
