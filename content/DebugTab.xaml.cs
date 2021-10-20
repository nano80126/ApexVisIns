using System;
using System.Collections.Generic;
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

namespace ApexVisIns.content
{
    /// <summary>
    /// Programming.xaml 的互動邏輯
    /// </summary>
    public partial class DebugTab : StackPanel
    {

        #region Resources
        public static Crosshair Crosshair { set; get; }
        public static AssistRect AssistRect { set; get; }
        public static Indicator Indicator { set; get; }
        #endregion

        public DebugTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region Find Resource
            if (Crosshair == null)
            {
                Crosshair = TryFindResource("Crosshair2") as Crosshair;
            }

            if (AssistRect == null)
            {
                AssistRect = TryFindResource("AssistRect2") as AssistRect;
            }

            if (Indicator == null)
            {
                Indicator = TryFindResource("Indicator2") as Indicator;
            }
            #endregion
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
