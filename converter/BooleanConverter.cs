using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MCAJawIns.Converter
{
    #region Bool 轉換器
    /// <summary>
    /// 布林反向轉換器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanInverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Debug.WriteLine($"ConvertBack: {value}");
            return !(bool)value;
        }
    }

    /// <summary>
    /// 布林 OR 轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(bool))]
    public class BooleanOrGate : IMultiValueConverter
    {
        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Any(value =>
            {
                return (bool)value;
            });
        }

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林 AND 轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(bool))]
    public class BooleanAndGate : IMultiValueConverter
    {
        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.All(value =>
            {
                return value != DependencyProperty.UnsetValue && (bool)value;
            });
        }

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林轉Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibility : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueValue : FalseValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林轉Visibility 反向轉換器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityInverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Collapsed;

        public Visibility FalseValue { get; set; } = Visibility.Visible;

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueValue : FalseValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林 AND To Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanAndToVisibility : BooleanAndGate
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetTypes, parameter, culture);
            //throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林 OR To Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanOrToVisibility : BooleanOrGate
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetTypes, parameter, culture);
        }
    }

    /// <summary>
    /// 布林 AND To Visibility 反向轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanAndToVisibilityInverter : BooleanAndGate
    {
        public Visibility TrueValue { get; set; } = Visibility.Collapsed;

        public Visibility FalseValue { get; set; } = Visibility.Visible;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetTypes, parameter, culture);
        }
    }

    /// <summary>
    /// 布林 OR To Visibility 反向轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanOrToVisibilityInverter : BooleanOrGate
    {
        public Visibility TrueValue { get; set; } = Visibility.Collapsed;

        public Visibility FalseValue { get; set; } = Visibility.Visible;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? TrueValue : FalseValue;
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetTypes, parameter, culture);
        }
    }
    #endregion
}
