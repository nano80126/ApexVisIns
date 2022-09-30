using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Reflection;

namespace MCAJawIns.Control
{
    /// <summary>
    /// SpecListView.xaml 的互動邏輯
    /// </summary>
    public partial class InsResultsView : StackPanel
    {
        public InsResultsView()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Actual Height: {this.ActualHeight}");
        }

        private void Scroller_Loaded(object sender, RoutedEventArgs e)
        {
            // 這邊 Binding 錯誤
            //_ = Scroller.SetBinding(HeightProperty, new Binding(nameof(ScrollViewerHeight)));
        }


        public static readonly DependencyProperty AutoCreateHeaderProperty = 
            DependencyProperty.RegisterAttached(nameof(AutoCreateHeader), typeof(bool), typeof(InsResultsView), new PropertyMetadata(false));

        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.RegisterAttached(nameof(ItemsSource), typeof(IEnumerable), typeof(InsResultsView), new PropertyMetadata(null));

        public static readonly DependencyProperty ScrollViewerStyleProperty = 
            DependencyProperty.RegisterAttached(nameof(ScrollViewerStyle), typeof(Style), typeof(InsResultsView), new PropertyMetadata(default(Style)));

        public static readonly DependencyProperty ScrollViewerHeightProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollViewerHeight), typeof(double), typeof(InsResultsView), new PropertyMetadata(double.NaN));


        [Description("自動生成 Header")]
        public bool AutoCreateHeader
        {
            get => (bool)GetValue(AutoCreateHeaderProperty);
            set => SetValue(AutoCreateHeaderProperty, value);
        }

        [Description("資料集合")]
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        [Description("滾輪 Style")]
        public Style ScrollViewerStyle
        {
            get => (Style)GetValue(ScrollViewerStyleProperty);
            set => SetValue(ScrollViewerStyleProperty, value);
        }

        [Description("滾輪高度")]
        public double ScrollViewerHeight
        {
            get => (double)GetValue(ScrollViewerHeightProperty);
            set => SetValue(ScrollViewerHeightProperty, value);
        }
    }

    public class InsResultsViewAssist : DependencyObject
    {
        public static readonly DependencyProperty HideTextProperty =
            DependencyProperty.RegisterAttached("HideText", typeof(bool), typeof(InsResultsViewAssist), new PropertyMetadata(false));

        public static void SetHideText(DependencyObject target, bool value)
        {
            target.SetValue(HideTextProperty, value);
        }

        public static bool GetHideText(DependencyObject target)
        {
            return (bool)target.GetValue(HideTextProperty);
        }
    }
}


