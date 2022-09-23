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
using MCAJawIns.Tab;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// LightPanel.xaml 的互動邏輯
    /// </summary>
    public partial class LightPanel : Control.CustomCard
    {
        /// <summary>，
        /// MainWindow
        /// </summary>
        public MainWindow MainWindow { get; } = (MainWindow)Application.Current.MainWindow;

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

            // Set value when init
            // MainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (LightSerial == null)
            {
                LightSerial = FindResource(nameof(LightSerial)) as LightSerial;
            }
        }

        private void ComPortConnect_Click(object sender, RoutedEventArgs e)
        {
            string comPort = ComPortSelector.SelectedValue as string;

            if (!LightSerial.IsComOpen)
            {
                try
                {
                    // 開啟 COM
                    LightSerial.ComOpen(comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    // 歸零所有通道
                    LightSerial.ResetAllChannel();
                    // 暫停 LightEnumer
                    MainWindow.SerialEnumer.WorkerPause();
                }
                catch (Exception ex)
                {
                    // 待新增光源 Error Code
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.LIGHT, ex.Message);
                    // 例外產生，關閉通訊
                    LightSerial.ComClose();
                }
            }
            else
            {
                // 關閉 COM
                LightSerial.ComClose();
                // 啟動 Serial Enumer
                MainWindow.SerialEnumer.WorkerResume();
            }
        }

        private void BulbOnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LightSerial.IsComOpen)
            {
                LightSerial.SetAllChannelValue();
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, "未與光源控制器連線或已斷線");
            }
        }

        private void BulbOffBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LightSerial.IsComOpen)
            {
                LightSerial.ResetAllChannel();
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.LIGHT, "未與光源控制器連線或已斷線");
            }
        }
    }
}
