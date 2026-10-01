using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace AduSkin.Demo.Converter
{
    public class IndentConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double colunwidth = 10;
            double left = 0.0;


            DependencyObject element = value as TreeViewItem;
            while (element != null && element is not TreeView)
            {
                element = VisualTreeHelper.GetParent(element);
                if (element is TreeViewItem)
                    left += colunwidth;
            }
            return new Thickness(left, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class BoolToVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool visible) return DependencyProperty.UnsetValue;
            if (visible)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibleToReverse : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility visibility) return DependencyProperty.UnsetValue;
            if (visibility == Visibility.Visible)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class DateConvertToColor : IValueConverter
    {
        public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not CalendarDayButton calendarDayButton || calendarDayButton.DataContext is not DateTime dateTime)
                return DependencyProperty.UnsetValue;
            if (!calendarDayButton.IsMouseOver && !calendarDayButton.IsSelected && !calendarDayButton.IsBlackedOut && (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday))
                return WeekendBrush;
            else
                return NormalBrush;
        }

        private static readonly SolidColorBrush WeekendBrush = CreateBrush(Color.FromRgb(255, 47, 47));
        private static readonly SolidColorBrush NormalBrush = CreateBrush(Color.FromRgb(51, 51, 51));

        private static SolidColorBrush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
