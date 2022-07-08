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
using MCAJawIns.content;
using Automation.BDaq;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Globalization;

namespace MCAJawIns.module
{
    /// <summary>
    /// DigitalIOPanel.xaml 的互動邏輯
    /// </summary>
    public partial class DigitalIOPanel : Card
    {
        #region Variables
        /// <summary>
        /// IO 控制器
        /// </summary>
        private IOController Controller { get; set; }

        private bool DllIsValid;
        
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
            DllIsValid = IOController.CheckDllVersion();    // 確認驅動安裝

            if (DataContext != null)
            {
                Controller = DataContext as IOController;
            }
#if false // Use button to initialize
            if (DllIsValid)
            {
                Controller = DataContext as IOController;
                ////return; // 初始化在MAINTAB

                if (!Controller.DiCtrlCreated)
                {
                    Controller.DigitalInputChanged += Controller_DigitalInputChanged;
                    Controller.InitializeDiCtrl();
                }

                if (!Controller.DoCtrlCreated)
                {
                    Controller.InitializeDoCtrl();
                }
            }
            else
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.IO, "IO 控制驅動未安裝或版本不符");
            } 
#endif
        }

        private void InitializeIO_Click(object sender, RoutedEventArgs e)
        {
            InitializeIOModule();
        }

        private void InitializeIOModule()
        {
            try
            {
                if (DllIsValid)
                {
                    Controller = DataContext as IOController;
                    ////return; // 初始化在MAINTAB

                    if (!Controller.DiCtrlCreated)
                    {
                        Controller.DigitalInputChanged += Controller_DigitalInputChanged;
                        Controller.InitializeDiCtrl();
                    }

                    if (!Controller.DoCtrlCreated)
                    {
                        Controller.InitializeDoCtrl();
                    }
                }
                else
                {
                    //MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.IO, "IO 控制驅動未安裝或版本不符");
                    throw new DllNotFoundException("IO 控制驅動未安裝或版本不符");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, $"IO 控制初始化失敗: {ex.Message}");
            }
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
            int channel = Convert.ToInt32(checkBox.CommandParameter, CultureInfo.CurrentCulture);

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
            int channel = Convert.ToInt32(checkBox.CommandParameter, CultureInfo.CurrentCulture);

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
            // DI 變更事件
            Debug.WriteLine($"Port{e.Port}, Bit{e.Bit}, {e.Data}");
        }

        /// <summary>
        /// 讀取 DI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadDIButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Controller.DiArrayColl.Count; i++)
            {
                ErrorCode err = Controller.ReadDI(i);
                Debug.WriteLine($"ErrorCode: {err}");
            }
        }

        /// <summary>
        /// 鎖定 DO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LockDOButton_Click(object sender, RoutedEventArgs e)
        {
            if (Controller.DOLocked)
            {
                Controller.UnlockDO();
            }
            else
            {
                Controller.LockDO();
            }

            Debug.WriteLine(Controller.DOLocked);
        }

        /// <summary>
        /// DO 變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DOSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Controller.DOLocked)
            {
                Button button = sender as Button;
                byte[] objs = button.CommandParameter as byte[];
                // 當前值: (bool)button.Tag
                // 目標 Port: objs[0] 
                // 目標 Bit: objs[1]
                _ = Controller.WriteDOBit(objs[0], objs[1], !(bool)button.Tag);
            }
        }

        #region 測試用 控制項，只有 DI 讀取要用到
        private void DiRead_Click(object sender, RoutedEventArgs e)
        {
            ErrorCode err = Controller.ReadDI(0);
            Debug.WriteLine($"ErrorCode: {err}");
            err = Controller.ReadDI(1);
            Debug.WriteLine($"ErrorCode: {err}");
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
        }
        #endregion

    }
}
