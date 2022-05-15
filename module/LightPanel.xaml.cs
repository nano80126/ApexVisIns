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

        /// <summary>
        /// 光源控制器
        /// </summary>
        public LightSerial LightControl { get; set; }

        public LightPanel()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            LightControl = FindResource(nameof(LightControl)) as LightSerial;
        }

        private void ComPortConnect_Click(object sender, RoutedEventArgs e)
        {
            string comPort = ComPortSelector.SelectedValue as string;

            if (!LightControl.IsComOpen)
            {
                try
                {
                    // #region TEST
                    // Debug.WriteLine($"{MainWindow}: {MainWindow == null} : MainWindow");
                    // Debug.WriteLine($"{MainWindow.LightEnumer} {MainWindow?.LightEnumer == null} : LightPanel");
                    // #endregion

                    // 開啟 COM
                    LightControl.ComOpen(comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 歸零所有通道
                    LightControl.ResetAllChannel();
                    // 暫停 LightEnummer
                    //MainWindow.LightEnumer.WorkerPause();
                    MainWindow.SerialEnumer.WorkerPause();
                }
                catch (Exception ex)
                {
                    // 待新增光源 Error Code
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.LIGHT, ex.Message);
                    // 例外產生，關閉通訊
                    LightControl.ComClose();
                }
            }
            else
            {
                // 關閉 COM
                LightControl.ComClose();
                // 啟動 LightEnumer
                //MainWindow.LightEnumer.WorkerResume();
                MainWindow.SerialEnumer.WorkerResume();
            }
        }

        private void BulbOnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LightControl.IsComOpen)
            {
                string cmd = string.Empty;
                for (int i = 0; i < LightControl.ChannelNumber; i++)
                {
                    LightChannel ch = LightControl.Channels[i];
                    cmd += $"{i + 1},{ch.Value},";
                }
                cmd = $"{cmd.TrimEnd(',')}\r\n";

                LightControl.Write(cmd);

                //// 新增至 MsgInformer 
                //Debug.WriteLine(cmd);
                //Debug.WriteLine(ret);
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, "未與光源控制器連線或已斷線");
            }
        }

        private void BulbOffBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LightControl.IsComOpen)
            {
                LightControl.ResetAllChannel();
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, "未與光源控制器連線或已斷線");
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
