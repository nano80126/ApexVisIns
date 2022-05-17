using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ApexVisIns.Converter
{
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

    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanAndToVisibility : BooleanAndGate
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //bool b = values.All(value =>
            //{
            //    return value != DependencyProperty.UnsetValue && (bool)value;
            //});
            return (bool)base.Convert(values, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
            //return b ? Visibility.Visible : (object)Visibility.Collapsed;
        }

        public new object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool[]), typeof(Visibility))]
    public class BooleanAndToVisibilityInverse : BooleanAndGate
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

    /// <summary>
    /// 布林 OR 轉換器
    /// </summary>
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
            return (int)value != System.Convert.ToInt32(parameter, CultureInfo.CurrentCulture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 字串比較
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.ToString().Equals(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 字串比較(反向)
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || !value.ToString().Equals(parameter.ToString());
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) / System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
    /// 數學小於 轉換器
    /// </summary>
    public class NumberLessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)value == "N/A")
            {
                return "N/A";
            }
            else
            {
                return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) < System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 數學大於 轉換器
    /// </summary>
    public class NumberGreaterConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)value == "N/A")
            {
                return "N/A";
            }
            else
            {
                return System.Convert.ToDouble(value, CultureInfo.CurrentCulture) > System.Convert.ToDouble(parameter, CultureInfo.CurrentCulture);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布林轉Visibility 轉換器
    /// </summary>
    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字串陣列比較器 (每個元素相等則傳回true)
    /// </summary>
    public class StringCompareConverter : IMultiValueConverter
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
    /// 數字陣列比較器 (每個元素相等則傳回true)
    /// </summary>
    public class NumberAllEqualConverter : IMultiValueConverter
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

    public class BooleanNotNullOrFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Enum 轉 Description
    /// </summary>
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
            return string.Empty;
        }
    }




    /// <summary>
    /// Multiple value 轉陣列
    /// </summary>
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

    //public class GetListElementConvert : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        ushort idx = System.Convert.ToUInt16(parameter);
    //        ObservableCollection<MotionAxis> coll = value as ObservableCollection<MotionAxis>;

    //        Debug.WriteLine(value);
    //        Debug.WriteLine(coll.Count);


    //        return false;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    //public class GetIndexConvertor : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        ListCollectionView collection = (ListCollectionView)values[1];
    //        int itemIndex = collection.IndexOf(values[0]);

    //        return itemIndex;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
