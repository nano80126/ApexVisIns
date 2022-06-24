using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.IO.Ports;

namespace LockPlate.content
{
    /// <summary>
    /// ModbusMoterTab.xaml 的互動邏輯
    /// </summary>
    public partial class ModbusMotorTab : StackPanel
    {
        #region Resources

        #endregion

        #region Varibles

        #endregion

        #region Properties
        public MainWindow MainWindow { get; set; }
        #endregion


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
            Debug.WriteLine(MainWindow.ShihlinSDE.IsComOpen);
            if (!MainWindow.ShihlinSDE.IsComOpen)
            {
                string[] protocols = (ProtocolSelector.SelectedItem as string).Split(",", StringSplitOptions.RemoveEmptyEntries);

                Parity parity = protocols[1] == "N" ? Parity.None : protocols[1] == "E" ? Parity.Even : Parity.Odd;
                int dataBit = Convert.ToInt32(protocols[0], System.Globalization.CultureInfo.CurrentCulture);
                StopBits stopBits = protocols[2] == "1" ? StopBits.One : protocols[2] == "2" ? StopBits.Two : StopBits.None;

                MainWindow.ShihlinSDE.ComOpen(ComportSelector.SelectedItem as string, (int)BaudSelector.SelectedItem, parity, dataBit, stopBits);
            }
            else
            {
                MainWindow.ShihlinSDE.ComClose();
            }
            Debug.WriteLine(MainWindow.ShihlinSDE.IsComOpen);
        }

        private void ReadMotorInfo_Click(object sender, RoutedEventArgs e)
        {
            //MainWindow.ShihlinSDE.
            //MainWindow.ShihlinSDE.ReadServo((byte)StationSelector.SelectedItem);
            MainWindow.ShihlinSDE.ReadIO((byte)StationSelector.SelectedItem);
            //MainWindow.ShihlinSDE.ReadPos((byte)StationSelector.SelectedItem);

            Debug.WriteLine($"------------------------------------------");
        }
    }
}
