using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace MCAJawIns.Converter
{

#if false
    #region Bool 轉換器
    /// <summary>
    /// 布林 反向 轉換器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Any(value =>
            {
                return (bool)value;
            });
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
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
    public class BooleanToVisibilityInverter : BooleanToVisibility
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return (bool)value ? Visibility.Visible : Visibility.Hidden;
            return (Visibility)base.Convert(value, targetType, parameter, culture) == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
        }

        public new object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林 AND To Visibility 反向轉換器
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanAndToVisibilityInverter : BooleanAndGate
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(values, targetType, parameter, culture) ? Visibility.Collapsed : Visibility.Visible;
        }

        public new object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

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
            //return (int)value == System.Convert.ToInt32(parameter, CultureInfo.CurrentCulture);
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
            //return (int)value != System.Convert.ToInt32(parameter, CultureInfo.CurrentCulture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, parameter, culture);
        }
    }


    /// <summary>
    /// 數字陣列比較器 (每個元素相等則傳回true)
    /// </summary>
    public class NumbersAllEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object first = values[0];
            return values.Length >= 2 && values.All(value => value.Equals(first));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            //if (string.IsNullOrEmpty(value as )) return false;
            throw new NotImplementedException();
        }
    }
    #endregion
#endif
#if false
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
    /// 字串 Equal 轉 Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringEqualToVisibility : StringEqualConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(value, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
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
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(value, targetType, parameter, culture) ? Visibility.Collapsed : Visibility.Visible;
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
    #endregion
#endif
#if false
    #region 數學運算轉換器
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) / System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 數字比較轉換器
    /// <summary>
    /// 數字 Odd 轉換器
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class NumberIsOddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value, CultureInfo.CurrentCulture) % 2 == 1;
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) <= System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)base.Convert(value, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
            //return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) >= System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion
#endif

    #region DateTime 轉換器
    [ValueConversion(typeof(DateTime), typeof(DateTime))]
    public class DateTimeToLocalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime)value).ToLocalTime();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region NotNull 轉換器
    /// <summary>
    /// 物件不為 Null 轉換器
    /// </summary>
    [ValueConversion(typeof(object), typeof(bool))]
    public class ObjectNotNullConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Debug.WriteLine($"ConvertBack: {value}");
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字串 Equal 轉 Visibility 轉換器
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class ObjectNotNullToVisibility : ObjectNotNullConverter
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

    /// <summary>
    /// Enum 轉 Description
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(string))]
    public class EnumDescriptionConverter : IValueConverter
    {
        private static string GetEnumDescription(Enum @enum)
        {
            FieldInfo fieldInfo = @enum.GetType().GetField(@enum.ToString());

            DescriptionAttribute[] attrArray = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrArray.Length == 0)
            {
                return @enum.ToString();
            }
            else
            {
                //DescriptionAttribute descriptionAttribute = attrArray[0] as DescriptionAttribute;
                return attrArray[0].Description;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum @enum = (Enum)value;
            if (@enum == null)
            {
                return null;
            }

            string description = GetEnumDescription(@enum);
            return !string.IsNullOrEmpty(description) ? description : @enum.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "123";
        }
    }

    /// <summary>
    /// Dictionary 取值轉換器
    /// </summary>
    [ValueConversion(typeof(Dictionary<string, double>), typeof(double))]
    public class DictionaryGetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as Dictionary<string, double>).TryGetValue(parameter as string, out double v) ? v : "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(object), typeof(object))]
    public class ObjectGetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"value {value} {parameter} {value.GetType()}");

            if (value != null)
            {
                PropertyInfo propertyInfo = value.GetType().GetProperty($"{parameter}");

                System.Diagnostics.Debug.WriteLine($"value {propertyInfo}");


                return null;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Multiple value 轉陣列
    /// </summary>
    [Obsolete("確認功能中")]
    public class CombineValueConvert : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Select(value => System.Convert.ToByte(value, CultureInfo.CurrentCulture)).ToArray();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
