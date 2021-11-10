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

            if (!MainWindow.LightController.IsComOpen)
            {
                try
                {
                    // 開啟 COM
                    MainWindow.LightController.ComOpen(comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 歸零所有通道
                    MainWindow.LightController.ResetValue();
                }
                catch (Exception ex)
                {
                    // 待新增光源 Error Code
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, ex.Message, MsgInformer.Message.MessageType.Warning);
                    // 例外產生，關閉通訊
                    MainWindow.LightController.ComClose();
                }
            }
            else
            {
                // 關閉 COM
                MainWindow.LightController.ComClose();
            }
        }

        private void ChannelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.LightController != null)
            {
                //MainWindow.LightController.ChannelOn = listBox.SelectedIndex;
                Debug.WriteLine($"{MainWindow.LightController.ChannelOn} {MainWindow.LightController.ValueOn}");
            }
        }

        private void CmdSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.LightController.IsComOpen)
            {
                string s = $"{ChannelSelector.SelectedIndex + 1},{LightSlider.Value}\r\n";

                Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");

                string str = MainWindow.LightController.Write(s);

                Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");

                Debug.WriteLine($"{s} {str}");
            }
            else
            {
                //MainWindow.LightController.ResetValue();
            }
        }
    }
}
