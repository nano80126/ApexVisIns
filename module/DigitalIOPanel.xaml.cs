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
using MaterialDesignThemes.Wpf;
using ApexVisIns.content;
using Automation.BDaq;
using System.Diagnostics;
using System.Threading;
using System.Collections;

namespace ApexVisIns.module
{
    /// <summary>
    /// DigitalIOPanel.xaml 的互動邏輯
    /// </summary>
    public partial class DigitalIOPanel : Card
    {
        #region 
        private IOController Controller { get; set; }
        #endregion

        /// <summary>
        /// App Element
        /// </summary>
        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Parent Element
        /// </summary>
        public EngineerTab EngineerTab { get; set; }

        public DigitalIOPanel()
        {
            InitializeComponent();
        }

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            //Controller = DataContext as IOController;

            //if (!Controller.DiCtrlCreated)
            //{
            //    // Controller.DigitalInputChanged += Controller_DigitalInputChanged;
            //    Controller.InitializeDiCtrl();
            //}

            //if (!Controller.DoCtrlCreated)
            //{
            //    Controller.InitializeDoCtrl();
            //}
        }

        /// <summary>
        /// 啟用中斷器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterruptToggle_Checked(object sender, RoutedEventArgs e)
        {
            ErrorCode err;
            CheckBox checkBox = sender as CheckBox;
            int channel = Convert.ToInt32(checkBox.CommandParameter);

            _ = Controller.DisableInterrupt();
            err = Controller.SetInterruptChannel(channel, ActiveSignal.RisingEdge);

            if (err == ErrorCode.Success)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.IO, $"CH {channel} 中斷已啟用");
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"CH {channel} 中斷啟用失敗");
            }

            if (Controller.InterruptEnabledChannel.Length > 0)
            {
                _ = Controller.EnableInterrut();
            }
        }

        /// <summary>
        /// 關閉中斷器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InterruptToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ErrorCode err;
            CheckBox checkBox = sender as CheckBox;
            int channel = Convert.ToInt32(checkBox.CommandParameter);

            _ = Controller.DisableInterrupt();
            err = Controller.SetInterruptChannel(channel, ActiveSignal.RisingEdge, false);

            if (err == ErrorCode.Success)
            {
                MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.IO, $"CH {channel} 中斷已停用");
            }
            else
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"CH {channel} 中斷停用失敗");
            }

            if (Controller.InterruptEnabledChannel.Length > 0)
            {
                _ = Controller.EnableInterrut();
            }
        }

        /// <summary>
        /// 中斷事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_DigitalInputChanged(object sender, IOController.DigitalInputChangedEventArgs e)
        {
            Debug.WriteLine($"{e.Port} {e.Bit} {e.Data}");
        }

        /// <summary>
        /// Di 變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoSetButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            byte[] objs = button.CommandParameter as byte[];
            // 當前值: (bool)button.Tag
            // 目標 Port: objs[0] 
            // 目標 Bit: objs[1]
            _ = Controller.WriteDOBit(objs[0], objs[1], !(bool)button.Tag);
        }

        #region 測試用 控制項
        private void DiRead_Click(object sender, RoutedEventArgs e)
        {
            ErrorCode err = Controller.ReadDI(0);
            Debug.WriteLine($"ErrorCode: {err}");
            err = Controller.ReadDI(1);
            Debug.WriteLine($"ErrorCode: {err}");
        }

        /// <summary>
        /// Digital Read Function
        /// </summary>
        private void DiRead()
        {
            ErrorCode err = Controller.ReadDI(0);
            ErrorCode err2 = Controller.ReadDI(1);
            Debug.WriteLine($"{err} {err2}");
        }


        private void DoRead_Click(object sender, RoutedEventArgs e)
        {
            ErrorCode err = Controller.ReadDO(0);
            Debug.WriteLine($"ErrorCode: {err}");
            err = Controller.ReadDO(1);
            Debug.WriteLine($"ErrorCode: {err}");
        }


        private void DoWrite_Click(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //Controller.WriteDO(0, 0b10100110);
            //Controller.WriteDO(1, 0b00111001); 
            #endregion
            Controller.TriggerEvent();
        }


        private void SwitchInterrupt_Click(object sender, RoutedEventArgs e)
        {
            if (!Controller.InterruptEnabled)
            {
                //Controller.DisableInterrupt();

                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge);
                Debug.WriteLine($"{err}");

                err = Controller.EnableInterrut();
                Debug.WriteLine($"{err}");
            }
            else
            {
                Controller.DisableInterrupt();

                ErrorCode err = Controller.SetInterrutChannel(0, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");
                err = Controller.SetInterrutChannel(8, ActiveSignal.RisingEdge, false);
                Debug.WriteLine($"{err}");

                err = Controller.DisableInterrupt();
                Debug.WriteLine($"{err}");
            }
        } 
        #endregion
    }
}
