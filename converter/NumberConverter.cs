using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

// 轉換前是否確認 IsNumer?
// 轉換前是否確認 IsNumer?

namespace MCAJawIns.Converter
{
    #region 數字 Equal 轉換器
    /// <summary>
    /// Int32 equal 轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberEqualConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == System.Convert.ToInt32(parameter, CultureInfo.CurrentCulture);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Int32 not equal 轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberNotEqualConverter : NumberEqualConverter
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
    /// 數字陣列比較器 (每個元素相等則傳回 true)
    /// </summary>
    [ValueConversion(typeof(int[]), typeof(bool))]
    public class NumbersAllEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                int[] array = Array.ConvertAll(values, x => int.TryParse($"{x}", out int value) ? value : -2 ^ 10);
                return array.All(value => value.Equals(array[0]));
            }
            else
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數字存在在陣列中轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberInArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Array.Exists((int[])parameter, x => x == (int)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數字 Odd 轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberIsOddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsNum = int.TryParse(value?.ToString(), out int number);
            return IsNum && number % 2 == 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///  數學加法 轉換器
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class MathPlusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) + System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數學乘法 轉換器
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class MathMultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) * System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數學減法 轉換器
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class MathMinusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) - System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數學除法 轉換器
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class MathDivideConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) / System.Convert.ToDouble(parameter ?? 1, CultureInfo.CurrentCulture);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數學除法取商數轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(int))]
    public class MathGetQuotientConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value, CultureInfo.CurrentCulture) / System.Convert.ToInt32(parameter ?? 1, CultureInfo.CurrentCulture);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 樹學除法取餘數轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(int))]
    public class MathGetRemainderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value, CultureInfo.CurrentCulture) % System.Convert.ToInt32(parameter ?? 1, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 數字小於比較器轉
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberLessConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) <= System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數字小於比較器轉
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class NumberLessToVisibility : NumberLessConverter
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
    /// 數字大於比較器轉
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberGreaterConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) >= System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數字大於比較器轉 Visibility
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class NumberGreaterToVisibility : NumberGreaterConverter
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
    #endregion
}
