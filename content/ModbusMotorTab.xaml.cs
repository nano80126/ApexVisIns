using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        private void Comport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ComboBox).SelectedIndex = -1;
        }

        private void SerialPortConnect_Click(object sender, RoutedEventArgs e)
        {
            // Debug.WriteLine(MainWindow.ShihlinSDE.IsComOpen);
            try
            {

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
                    MainWindow.ShihlinSDE?.DisablePollingTask();

                    MainWindow.ShihlinSDE?.ResetInfo();

                    MainWindow.ShihlinSDE?.ComClose();
                }

            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                //throw;
            }
            // Debug.WriteLine(MainWindow.ShihlinSDE.IsComOpen);
        }

        private void RefreshStationn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ShihlinSDE.ChangeTimeout(50);

            MainWindow.ShihlinSDE.ChangeTimeout(200);
        }

        private void StationSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 關閉 Polling Task
            MainWindow.ShihlinSDE?.DisablePollingTask();
            // 重置 Info
            MainWindow.ShihlinSDE?.ResetInfo();
        }

        private void ReadMotorInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //讀取 IO 資訊
                //啟動 Task 讀取 IO

                MainWindow.ShihlinSDE.ReadServoOn((byte)StationSelector.SelectedItem);
                MainWindow.ShihlinSDE.ReadIO((byte)StationSelector.SelectedItem);
                MainWindow.ShihlinSDE.ReadIOStatus((byte)StationSelector.SelectedItem);
                MainWindow.ShihlinSDE.ReadPos((byte)StationSelector.SelectedItem);
                MainWindow.ShihlinSDE.ReadAlarm((byte)StationSelector.SelectedItem);
                // // // 
                //MainWindow.ShihlinSDE.CheckStat((byte)StationSelector.SelectedItem);
                //MainWindow.ShihlinSDE.ReadPrPath((byte)StationSelector.SelectedItem);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void ResetMotorAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ShihlinSDE.ResetAlarm((byte)StationSelector.SelectedItem);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        #region JOG
        private void JogPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            try
            {
                bool b = MainWindow.ShihlinSDE.CheckStat((byte)StationSelector.SelectedItem);

                if (b)
                {
                    // 啟動 JOG //，寫入轉速、加減速時間
                    MainWindow.ShihlinSDE.JogEnable((byte)StationSelector.SelectedItem);
                    // 啟動 Polling Task
                    MainWindow.ShihlinSDE.EnablePollingTask((byte)StationSelector.SelectedItem);
                }
                else
                {
                    // 新增 Informer
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "當前馬達狀態不允許此操作");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogMove_GotMouseCapture(object sender, MouseEventArgs e)
        {
            try
            {
                int dir = (ushort)(sender as Button).CommandParameter;
                switch (dir)
                {
                    case 1:
                        MainWindow.ShihlinSDE.JogClock((byte)StationSelector.SelectedItem);
                        break;
                    case 2:
                        MainWindow.ShihlinSDE.JogCClock((byte)StationSelector.SelectedItem);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogStop_LostMouseCapture(object sender, MouseEventArgs e)
        {
            try
            {
                MainWindow.ShihlinSDE.JogStop((byte)StationSelector.SelectedItem);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            try
            {
                // 關閉 Polling Task
                MainWindow.ShihlinSDE.DisablePollingTask();
                // 停止 JOG
                MainWindow.ShihlinSDE.JogDisable((byte)StationSelector.SelectedItem);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }
        #endregion

        #region 定位移動
        private void PosMovePopupBox_Opened(object sender, RoutedEventArgs e)
        {
            try
            {
                bool b = MainWindow.ShihlinSDE.CheckStat((byte)StationSelector.SelectedItem);

                if (b)
                {
                    // 啟動 定位測試 //，寫入轉速、加減速時間、目標脈波
                    MainWindow.ShihlinSDE.PosMoveEnable((byte)StationSelector.SelectedItem);
                    // 啟動 Polling Task
                    MainWindow.ShihlinSDE.EnablePollingTask((byte)StationSelector.SelectedItem);
                }
                else
                {
                    // 新增 Informer
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "當前馬達狀態不允許此操作");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void PosMovePopupBox_Closed(object sender, RoutedEventArgs e)
        {
            try
            {
                // 關閉 Polling Task
                MainWindow.ShihlinSDE.DisablePollingTask();
                // 停止 定位測試
                MainWindow.ShihlinSDE.PosMoveDisable((byte)StationSelector.SelectedItem);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void PosMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int dir = (ushort)(sender as Button).CommandParameter;

                switch (dir)
                {
                    case 1:
                        MainWindow.ShihlinSDE.PosMoveClock((byte)StationSelector.SelectedItem);
                        break;
                    case 2:
                        MainWindow.ShihlinSDE.PosMoveCClock((byte)StationSelector.SelectedItem);
                        break;
                    case 0:
                        MainWindow.ShihlinSDE.PosMovePause((byte)StationSelector.SelectedItem);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }
        #endregion

        #region PR 模式
        private void SyncPrPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ShihlinSDE.SyncPrPath((byte)StationSelector.SelectedItem);
                // MainWindow.ShihlinSDE.EnablePollingTask((byte)StationSelector.SelectedItem, ShihlinSDE.PollingType.PrPath);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }


        private void RunZeroReturn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // path = 0, 執行原點復歸
                MainWindow.ShihlinSDE.RunPrPath((byte)StationSelector.SelectedItem, 0);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void RunPrPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 執行指定 Path, 
                MainWindow.ShihlinSDE.RunPrPath((byte)StationSelector.SelectedItem, MainWindow.ShihlinSDE.PF82);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void StopPrPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 停止執行
                MainWindow.ShihlinSDE.RunPrPath((byte)StationSelector.SelectedItem, 1000);
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }
        #endregion

    }
}
