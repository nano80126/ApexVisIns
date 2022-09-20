using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MCAJawIns.Converter
{
    #region 字串 Equal 轉換器
    /// <summary>
    /// 字串 Equal 轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringEqualConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.ToString().Equals(parameter.ToString(), StringComparison.Ordinal);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字串 NotEqual 轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotEqualConveter : StringEqualConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)base.Convert(value, targetType, parameter, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, parameter, culture);
        }
    }

    /// <summary>
    /// 字串 Equal 轉 Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringEqualToVisibility : StringEqualConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(value, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, parameter, culture);
        }
    }

    /// <summary>
    /// 字串 Equal 轉 Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringEqualToVisibilityInverter : StringEqualConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Collapsed;

        public Visibility FalseValue { get; set; } = Visibility.Visible;

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(value, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, parameter, culture);
        }
    }

    /// <summary>
    /// 字串 Equal 反向轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotEqualConverter : StringEqualConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)base.Convert(value, targetType, parameter, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字串陣列比較器 (每個元素相等則傳回true)
    /// </summary>
    [ValueConversion(typeof(string[]), typeof(bool))]
    public class StringsAllEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string first = values[0] as string;
            return values.Length >= 2 && values.All(value => value.ToString().Equals(first, StringComparison.Ordinal));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字串不為 Null 或 Empty
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 過長字串省略轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class StringEllipsisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = int.TryParse($"{parameter}", out int leng);

            if (b)
            {
                return value.ToString().Length > leng ? $"{value.ToString().Substring(0, leng - 3)}..." : value;
            }
            else
            {   // 原封不動回傳
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
