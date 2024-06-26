﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JudgeSearcher.Utility
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && ((Visibility)value).Equals(Visibility.Visible);
        }
    }
}
