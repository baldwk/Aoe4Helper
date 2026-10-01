using AduSkin.Controls;
using AduSkin.Demo.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AduSkin.Demo.ViewModel
{
   public class SortGroupViewModel : ObservableObject
   {
      public SortGroupViewModel()
      {
         var tempContactList = new List<ChatUserModel>();
         ChatUserModel group = new ChatUserModel();
         group.ContactType = ContactType.Group;
         group.UserName = "Flutter开发组";
         group.Header = "pack://application:,,,/Resources/Img/Header/头像1.png";
         group.Describe = "寻找适合你的UI";
         tempContactList.Add(group);

         ChatUserModel Group1 = new ChatUserModel();
         Group1.ContactType = ContactType.Group;
         Group1.UserName = "AduSkin开发组";
         Group1.Header = "pack://application:,,,/Resources/Img/Header/头像2.png";
         Group1.Describe = "开发者论坛";
         tempContactList.Add(Group1);

         ChatUserModel item = new ChatUserModel();
         item.UserName = "AduSkin";
         item.Header = "pack://application:,,,/Resources/Img/Header/头像3.png";
         item.Describe = "追求极致，永臻完美";
         tempContactList.Add(item);

         ChatUserModel item1 = new ChatUserModel();
         item1.UserName = "千百度";
         item1.Header = "pack://application:,,,/Resources/Img/Header/头像4.png";
         item1.Describe = "追求极致，永臻完美";
         tempContactList.Add(item1);

         ChatUserModel item2 = new ChatUserModel();
         item2.UserName = "万里独行";
         item2.Header = "pack://application:,,,/Resources/Img/Header/头像5.png";
         item2.Describe = "追求极致，永臻完美";
         tempContactList.Add(item2);

         ChatUserModel item3 = new ChatUserModel();
         item3.UserName = "一个人";
         item3.Header = "pack://application:,,,/Resources/Img/Header/头像6.png";
         item3.Describe = "追求极致，永臻完美";
         tempContactList.Add(item3);

         ChatUserModel item4 = new ChatUserModel();
         item4.UserName = "AduMusic";
         item4.Header = "pack://application:,,,/Resources/Img/Header/头像1.png";
         item4.Describe = "追求极致，永臻完美";
         tempContactList.Add(item4);

         ChatUserModel item6 = new ChatUserModel();
         item6.UserName = "往事如风";
         item6.Header = "pack://application:,,,/Resources/Img/Header/头像2.png";
         item6.Describe = "美滋滋";
         tempContactList.Add(item6);

         ChatUserModel item7 = new ChatUserModel();
         item7.UserName = "美滋滋";
         item7.Header = "pack://application:,,,/Resources/Img/Header/头像3.png";
         item7.Describe = "不需要太多";
         tempContactList.Add(item7);

         ContactList = new ObservableCollection<ChatUserModel>(Sort(tempContactList));
         SortID = new ObservableCollection<ChatUserModel>(ContactList.Where(item => item.ContactType == ContactType.SerialNumber));
      }

      public List<ChatUserModel> Sort(List<ChatUserModel> contacts)
      {
         var order = new[] { "群组" }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString())).Append("#");
         var groups = contacts.Where(item => item.ContactType != ContactType.SerialNumber)
            .GroupBy(item => item.ContactType == ContactType.Group ? "群组" : GetSortKey(item.UserName))
            .ToDictionary(group => group.Key, group => group.ToList());
         var result = new List<ChatUserModel>();
         foreach (string key in order)
         {
            if (!groups.TryGetValue(key, out var members)) continue;
            result.Add(new ChatUserModel { SortID = key, ContactType = ContactType.SerialNumber });
            foreach (var member in members)
            {
               member.SortID = key;
               result.Add(member);
            }
         }
         return result;
      }

      private static string GetSortKey(string name)
      {
         if (string.IsNullOrWhiteSpace(name)) return "#";
         char first = char.ToUpperInvariant(name[0]);
         if (first >= 'A' && first <= 'Z') return first.ToString();
         // 拼音工具要求双字节汉字；其他字符归入 #，避免丢失联系人。
         if (first < 0x4E00 || first > 0x9FFF ||
             System.Text.Encoding.GetEncoding("GB2312").GetByteCount(first.ToString()) != 2) return "#";
         string key = AduSkin.Utility.Extend.StringExtend.GetFirstPinyin(name).ToUpperInvariant();
         return key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z' ? key : "#";
      }

      public ObservableCollection<ChatUserModel> ContactList { get; }
      public ObservableCollection<ChatUserModel> SortID { get; }

      private ChatUserModel _CurrentChatUserModel;
      /// <summary>
      /// 当前选中的用户/群组/序号
      /// </summary>
      public ChatUserModel CurrentChatUserModel
      {
         get { return _CurrentChatUserModel; }
         set
         {
            IsOpenSortList = value?.ContactType == ContactType.SerialNumber;
            SetProperty(ref _CurrentChatUserModel, value);
         }
      }

      private bool _IsOpenSortList = false;
      /// <summary>
      /// 显示序号
      /// </summary>
      public bool IsOpenSortList
      {
         get { return _IsOpenSortList; }
         set { SetProperty(ref _IsOpenSortList, value); }
      }
   }
}
