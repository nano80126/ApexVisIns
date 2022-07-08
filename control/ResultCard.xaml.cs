using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace MCAJawIns.Control
{
    /// <summary>
    /// ResultCard.xaml 的互動邏輯
    /// </summary>
    public partial class ResultCard : Card
    {
        public ResultCard()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.RegisterAttached(nameof(Result), typeof(bool), typeof(ResultCard), new PropertyMetadata(false));


        public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached(nameof(Duration), typeof(TimeSpan), typeof(ResultCard), new PropertyMetadata(TimeSpan.Zero));


        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.RegisterAttached(nameof(EndTime), typeof(DateTime), typeof(ResultCard), new PropertyMetadata(DateTime.Now));


        /// <summary>
        /// 檢驗結果
        /// </summary>
        [Description("檢驗結果")]
        public bool Result
        {
            get => (bool)GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        /// <summary>
        /// 檢驗花費時間
        /// </summary>
        [Description("檢驗花費時間")]
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        /// <summary>
        /// 完成時間
        /// </summary>
        [Description("完成時間")]
        public DateTime EndTime
        {
            get => (DateTime)GetValue(EndTimeProperty);
            set => SetValue(EndTimeProperty, value);
        }

    }

    /// <summary>
    /// Result Card 輔助功能
    /// </summary>
    public class RCHelper : DependencyObject
    {
        public static readonly DependencyProperty OKProperty = DependencyProperty.RegisterAttached("OK", typeof(bool), typeof(RCHelper), new PropertyMetadata(false));

        public static void SetOK(DependencyObject target, bool value)
        {
            target.SetValue(OKProperty, value);
        }

        public static bool GetOK(DependencyObject target)
        {
            return (bool)target.GetValue(OKProperty);
        }
    }
}
