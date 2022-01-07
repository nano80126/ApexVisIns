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

namespace ApexVisIns.control
{
    /// <summary>
    /// ProcedureBlock.xaml 的互動邏輯
    /// </summary>
    public partial class ProcedureBlock : StackPanel
    {
        public ProcedureBlock()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty BlockNameProperty = DependencyProperty.RegisterAttached("BlockName", typeof(string), typeof(ProcedureBlock), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty StepProperty = DependencyProperty.RegisterAttached("Step", typeof(int), typeof(ProcedureBlock), new PropertyMetadata(0));

        public static readonly DependencyProperty HighLightProperty = DependencyProperty.RegisterAttached("HighLight", typeof(bool), typeof(ProcedureBlock), new PropertyMetadata(false));

        public static readonly DependencyProperty EnableSubPackIconProperty = DependencyProperty.RegisterAttached("EnableSubPackIcon", typeof(bool), typeof(ProcedureBlock), new PropertyMetadata(false));
        public static readonly DependencyProperty SubPackIconProperty = DependencyProperty.RegisterAttached("SubPackIcon", typeof(PackIconKind), typeof(ProcedureBlock), new PropertyMetadata(PackIconKind.Abacus));
        public static readonly DependencyProperty SubPackIconColorProperty = DependencyProperty.RegisterAttached("SubPackIconColor", typeof(SolidColorBrush), typeof(ProcedureBlock), new PropertyMetadata(new SolidColorBrush(Colors.Black)));


        /// <summary>
        /// 方塊流程名稱
        /// </summary>
        [Description("方塊流程名稱")]
        public string BlockName
        {
            get => (string)GetValue(BlockNameProperty);
            set => SetValue(BlockNameProperty, value);
        }

        /// <summary>
        /// 步序
        /// </summary>
        [Description("步序")]
        public int Step
        {
            get => (byte)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        [Description("是否高亮")]
        /// <summary>
        /// 是否HighLight
        /// </summary>
        public bool HighLight
        {
            get => (bool)GetValue(HighLightProperty);
            set => SetValue(HighLightProperty, value);
        }

        /// <summary>
        /// 啟用副 icon
        /// </summary>
        [Description("啟用副 icon")]
        public bool EnableSubPackIcon
        {
            get => (bool)GetValue(EnableSubPackIconProperty);
            set => SetValue(EnableSubPackIconProperty, value);
        }

        /// <summary>
        /// 副 icon
        /// </summary>
        [Description("副 ICON")]
        public PackIconKind SubPackIcon
        {
            get => (PackIconKind)GetValue(SubPackIconProperty);
            set => SetValue(SubPackIconProperty, value);
        }

        /// <summary>
        /// 副 icon
        /// </summary>
        [Description(" 副 ICON")]
        public SolidColorBrush SubPackIconColor
        {
            get => (SolidColorBrush)GetValue(SubPackIconColorProperty);
            set => SetValue(SubPackIconColorProperty, value);
        }
    }

    public class PBHelper : DependencyObject
    {
        public static readonly DependencyProperty HighLightProperty = DependencyProperty.RegisterAttached(
          "HighLight", typeof(bool), typeof(PBHelper), new PropertyMetadata(true));

        public static void SetHighLight(DependencyObject target, bool value)
        {
            target.SetValue(HighLightProperty, value);
        }

        public static bool GetHighLight(DependencyObject target)
        {
            return (bool)target.GetValue(HighLightProperty);
        }
    }
}
