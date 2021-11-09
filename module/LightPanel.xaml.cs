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
using MaterialDesignThemes.Wpf;
using ApexVisIns.content;

namespace ApexVisIns.module
{
    /// <summary>
    /// LightPanel.xaml 的互動邏輯
    /// </summary>
    public partial class LightPanel : Card
    {
        public MainWindow MainWindow { get; set; }

        public EngineerTab EngineerTab { get; set; }

        public LightPanel()
        {
            InitializeComponent();
        }


        private void ComPortConnect_Click(object sender, RoutedEventArgs e)
        {
            string comPort = ComPortSelector.SelectedValue as string;

            Debug.WriteLine($"{comPort} : {MainWindow.SerialPort.IsOpen}");
            // MainWindow.SerialPort

            if (!MainWindow.SerialPort.IsOpen)
            {
                MainWindow.SerialPort = new System.IO.Ports.SerialPort(comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                MainWindow.SerialPort.Open();
            }
            else
            {
                MainWindow.SerialPort.Close();
            }

            Debug.WriteLine($"{comPort} : {MainWindow.SerialPort.IsOpen}");
        }

        private void ChannelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            //Debug.WriteLine($"{listBox.SelectedItem}");
            //Debug.WriteLine($"{listBox.SelectedIndex}");

            Debug.WriteLine($"{this.MainWindow}");
            Debug.WriteLine($"{MainWindow.SerialPort}");
            //Debug.WriteLine($"{MainWindow.SerialPort.IsOpen}");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string s = $"1,{LightSlider.Value}\r\n";

            MainWindow.SerialPort.Write(s);

            string str = MainWindow.SerialPort.ReadLine();

            Debug.WriteLine($"{str}");
            Debug.WriteLine($"{s} {str}");
        }
    }
}
