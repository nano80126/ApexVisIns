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
using MCAJawIns.content;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// LightPanel.xaml 的互動邏輯
    /// </summary>
    public partial class LightPanel : Control.CustomCard
    {
        /// <summary>
        /// 
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Parent Tab
        /// </summary>
        public EngineerTab EngineerTab { get; set; }

        /// <summary>
        /// 光源控器器
        /// </summary>
        public LightSerial LightSerial { get; set; }

        public LightPanel()
        {
            InitializeComponent();
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ComPortConnect_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BulbOnBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BulbOffBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
