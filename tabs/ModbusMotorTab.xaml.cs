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
    /// ModbusMoterTab.xaml 的互動邏輯
    /// </summary>
    public partial class ModbusMotorTab : StackPanel
    {

        public ModbusMotorTab()
        {
            InitializeComponent();
        }

        private void Comport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ComboBox).SelectedIndex = -1;
        }

        private void SerialPortConnect_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
