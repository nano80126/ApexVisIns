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
using System.Data;
using System.Drawing;
using System.IO;
using MCAJawIns.content;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// PointsPanel.xaml 的互動邏輯
    /// </summary>
    public partial class PointsPanel : Control.CustomCard
    {
        public EngineerTab EngineerTab { get; set; }


        public AssistPoints AssistPoints { get; set; }

        public PointsPanel()
        {
            InitializeComponent();

        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            AssistPoints = DataContext as AssistPoints;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AssistPoints.Source.Clear();
        }
    }
}
