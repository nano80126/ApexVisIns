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
using System.Threading;

namespace ApexVisIns.module
{
    /// <summary>
    /// LightPanel.xaml 的互動邏輯
    /// </summary>
    public partial class LightPanel : Card
    {
        /// <summary>
        /// App Element
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Parent Element
        /// </summary>
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
                    // #region TEST
                    // Debug.WriteLine($"{MainWindow}: {MainWindow == null} : MainWindow");
                    // Debug.WriteLine($"{MainWindow.LightEnumer} {MainWindow?.LightEnumer == null} : LightPanel");
                    // #endregion

                    // 開啟 COM
                    MainWindow.LightController.ComOpen(comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 歸零所有通道
                    MainWindow.LightController.ResetAllValue();
                    // 暫停 LightEnummer
                    MainWindow.LightEnumer.WorkerPause();
                }
                catch (Exception ex)
                {
                    // 待新增光源 Error Code
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.LIGHT, ex.Message);
                    // 例外產生，關閉通訊
                    MainWindow.LightController.ComClose();
                }
            }
            else
            {
                // 關閉 COM
                MainWindow.LightController.ComClose();
                // 啟動 LightEnumer
                MainWindow.LightEnumer.WorkerResume();
            }
        }

        private void BulbOffBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.LightController.IsComOpen)
            {
                MainWindow.LightController.ResetAllValue();
            }
            else
            {

            }
        }

        private void CmdSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.LightController.IsComOpen)
            {
                string cmd = string.Empty;
                for (int i = 0; i < MainWindow.LightController.ChannelNumber; i++)
                {
                    LightChannel ch = MainWindow.LightController.Channels[i];
                    cmd += $"{i + 1},{ch.Value},";
                }
                cmd = $"{cmd.TrimEnd(',')}\r\n";

                string ret = MainWindow.LightController.Write(cmd);

                // 新增至 MsgInformer 
                Debug.WriteLine(cmd);
                Debug.WriteLine(ret);

                #region 自動化測試
                //Task.Run(() =>
                //{
                //    for (int j = 0; j < 60; j++)
                //    {
                //        MainWindow.LightController.SetCannelValue(j % 4 + 1, 192);
                //        MainWindow.LightController.SetCannelValue((j + 1) % 4 + 1, 0);
                //        MainWindow.LightController.SetCannelValue((j + 2) % 4 + 1, 0);
                //        MainWindow.LightController.SetCannelValue((j + 3) % 4 + 1, 0);

                //        string cmd = string.Empty;
                //        for (int i = 0; i < MainWindow.LightController.ChannelNumber; i++)
                //        {
                //            LightChannel ch = MainWindow.LightController.Channels[i];
                //            cmd += $"{i + 1},{ch.Value},";
                //        }
                //        cmd = $"{cmd.TrimEnd(',')}\r\n";

                //        string ret = MainWindow.LightController.Write(cmd);

                //        // 新增 msg
                //        Debug.WriteLine(cmd);
                //        Debug.WriteLine(ret);

                //        SpinWait.SpinUntil(() => false, 500);
                //    }
                //}); 
                #endregion
            }
            else
            {
                //MainWindow.LightController.ResetValue();
            }
        }
        

        //private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    Debug.WriteLine($"{e.OldValue} {e.NewValue}");

        //     foreach (LightChannel channel in MainWindow.LightController.Channels)
        //    {
        //        Debug.WriteLine($"{channel.Channel} {channel.Value}");
        //    }
        //}
    }
}
