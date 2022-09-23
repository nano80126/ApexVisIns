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
using Advantech.Motion;
using System.Runtime.InteropServices;
using System.Timers;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using System.Threading;

namespace ApexVisIns.content
{
    /// <summary>
    /// MotionTab.xaml 的互動邏輯
    /// </summary>
    public partial class MotionTab : StackPanel
    {
        #region Resources

        #endregion

        #region Variables
        /// <summary>
        /// Motion Setting 路徑
        /// </summary>
        private string MotionDirectory { get; } = @"motions";
        //private string MotionPath { get; } = @"motion.json";
        #endregion

        #region Properties
        /// <summary>
        /// 主視窗物件
        /// </summary>
        public MainWindow MainWindow { get; set; }
        #endregion

        #region Flags
        /// <summary>
        /// 已載入旗標
        /// </summary>
        private bool loaded;
        #endregion

        public MotionTab()
        {
            InitializeComponent();

            MainWindow = (MainWindow)Application.Current.MainWindow;
            // 初始化路徑
            InitMotionsConfigsPath();
        }

        #region Load & Unload
        /// <summary>
        /// Motion Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // 確認軸卡連線狀態
            CheckMotionCardStatus();

            // 若 DeviceOpened，始能 Timer
            if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedMotionAxis != null)
            {
                MainWindow.ServoMotion.EnableTimer(100);
            }

            if (!loaded)
            {
                MainWindow.MsgInformer?.AddInfo(MsgInformer.Message.MsgCode.APP, "運動控制頁面已載入");
                loaded = true;
            }
        }
        /// <summary>
        /// Motion Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            //MainWindow.ServoMotion.DisableTimer();
        }
        #endregion

        /// <summary>
        /// 初始化 Motion Config 路徑
        /// </summary>
        private void InitMotionsConfigsPath()
        {
            string directory = $@"{Directory.GetCurrentDirectory()}\{MotionDirectory}";
            // Directory 不存在則新增
            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// 確認軸卡連線狀態
        /// </summary>
        private void CheckMotionCardStatus()
        {
            // 若 DeviceSelector，選擇第一個 Device
            if (DeviceSelector.SelectedIndex == -1)
            {
                if (MainWindow.ServoMotion.MotionDevices.Count > 0)
                {
                    DeviceSelector.SelectedIndex = 0;
                }
            }

            // 若 AxisSelector，選擇第一個 Axis
            if (AxisSelector.SelectedIndex == -1)
            {
                if (MainWindow.ServoMotion.Axes.Count > 0)
                {
                    AxisSelector.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Clear Focus 用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Window.GetWindow(this) as MainWindow).TitleGrid.Focus();
        }

        /// <summary>
        /// 重載入 EtherCAT Card
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReloadMotionDevicesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ServoMotion.CheckDllVersion())
            {
                if (!MainWindow.ServoMotion.DeviceOpened)
                {
                    MainWindow.ServoMotion.ListAvailableDevices();
                    if (DeviceSelector.Items.Count > 0)
                    {
                        DeviceSelector.SelectedIndex = 0;
                    }
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "Device 開啟時不允許此操作");
                }
            }
            else
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "MOTION 控制驅動未安裝或版本不符");
            }
        }

        /// <summary>
        /// 開啟軸卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!MainWindow.ServoMotion.DeviceOpened)
                {
                    // 開啟裝置
                    MainWindow.ServoMotion.OpenDevice((DeviceSelector.SelectedItem as ServoMotion.MotionDevice).DeviceNumber);
                    // 重置各軸錯誤
                    MainWindow.ServoMotion.ResetAllError();
                    // 軸數量 > 0，選擇第一軸
                    if (AxisSelector.Items.Count > 0) AxisSelector.SelectedIndex = 0;
                }
                else
                {
                    // 全部軸 Servo Off
                    MainWindow.ServoMotion.SetAllServoOff();
                    // 關閉 Timer
                    MainWindow.ServoMotion.DisableTimer();
                    // 重置選擇軸
                    MainWindow.ServoMotion.SelectedAxis = AxisSelector.SelectedIndex = -1;
                    // 關閉裝置
                    MainWindow.ServoMotion.CloseDevice();
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 重置選擇 Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardSelector_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ComboBox).SelectedIndex = -1;
        }

        /// <summary>
        /// 選擇軸變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem is MotionAxis)
            {
                MainWindow.ServoMotion.SelectedAxis = comboBox.SelectedIndex;

                try
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.GetGearRatio();
                    MainWindow.ServoMotion.SelectedMotionAxis.GetJogVelParam();
                    MainWindow.ServoMotion.SelectedMotionAxis.GetHomeVelParam();
                    MainWindow.ServoMotion.SelectedMotionAxis.GetAxisVelParam();
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                }
            }
        }

        /// <summary>
        /// 重置選擇軸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisSelector_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.ServoMotion.SelectedAxis = (sender as ComboBox).SelectedIndex = -1;
        }

        /// <summary>
        /// 切換 Servo On/Off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServoOnBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MotionAxis motionAxis = AxisSelector.SelectedItem as MotionAxis;

                if (!MainWindow.ServoMotion.SelectedMotionAxis.IO_SVON.BitOn)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetServoOn();
                }
                else
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetServoOff();
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }


        private void AllServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!MainWindow.ServoMotion.AllServoOn)
                {
                    MainWindow.ServoMotion.SetAllServoOn();
                } else
                {
                    MainWindow.ServoMotion.SetAllServoOff();
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }


        /// <summary>
        /// 重置命令位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetCmdPos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.ResetPos();
                //MainWindow.ServoMotion.ResetPos();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 重置軸錯誤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetAxisError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.ResetError();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
            e.Handled = true;
        }

        /// <summary>
        /// 寫入電子齒輪比
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GearRatioSetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetGearRatio();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 寫入寸動模式(JOG)速度參數
        /// (CFG_AxJogXXX)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JogVelParamSetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetJogVelParam();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogStartPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.ServoMotion.SelectedMotionAxis.JogOn)
            {
                try
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.JogStart();
                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.MOTION, "JOG 開始");
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                }

            }
        }

        private void JogStartPopupBox_Closed(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ServoMotion.SelectedMotionAxis.JogOn)
            {
                try
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.JogStop();
                    MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.MOTION, "JOG 停止");
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                }
            }
        }

        private void JogLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.JogCtClock();
                //MainWindow.ServoMotion.SltMotionAxis.JogClock();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.JogDecAction();
                //Debug.WriteLine("DecAction");
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.JogClock();
                //MainWindow.ServoMotion.SltMotionAxis.JogCtClock();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void JogRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SelectedMotionAxis.JogDecAction();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 寫入原點復歸(HOME)速度參數
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HomeVelParamSetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetHomeVelParam();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 執行原點復歸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void HomeStartBtn_Click(object sender, RoutedEventArgs e)
        {
            ServoMotion.HomeMode homeMode = HomeModeSelector.SelectedItem as ServoMotion.HomeMode;

            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    //await MainWindow.ServoMotion.SelectedMotionAxis.NegativeWayHomeMove(true);
                    if (homeMode is ServoMotion.HomeMode)
                    {
                        switch (homeMode.ModeCode)
                        {
                            case 0:
                                await MainWindow.ServoMotion.SelectedMotionAxis.PositiveWayHomeMove(true);
                                break;
                            case 1:
                                await MainWindow.ServoMotion.SelectedMotionAxis.NegativeWayHomeMove(true);
                                break;
                            default:

                                break;
                        }
                    }
                    else
                    {
                        MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"原點復歸模式未選擇");
                    }
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 寫入速度參數
        /// (PAR_AxVelXXX)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisVelParamSetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.SetAxisVelParam();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 點到點移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PtToPtBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //MainWindow.ServoMotion.SltMotionAxis.PosMove(false);
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    bool abs = MainWindow.ServoMotion.SelectedMotionAxis.Absolute;
                    MainWindow.ServoMotion.SelectedMotionAxis.PosMove(abs);
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                //Debug.WriteLine($"{ex.Message}");
            }
        }

        /// <summary>
        /// 變更目標位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangePtBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //MainWindow.ServoMotion.SltMotionAxis.ChangePos();
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.ChangePos();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 變更速度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeVelBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // MainWindow.ServoMotion.SltMotionAxis.ChangeVel();
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.ChangeVel();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }


        private void VelMoveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //MainWindow.ServoMotion.SltMotionAxis.PosMove(false);
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    if (MainWindow.ServoMotion.SelectedMotionAxis.IO_SVON.BitOn)
                    {
                        ushort dir = Convert.ToUInt16((sender as Button).CommandParameter);
                        MainWindow.ServoMotion.SelectedMotionAxis.VelMove(dir);
                    } else
                    {
                        MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"伺服軸狀態為 Servo Off");
                    }
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 運動停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotionStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.StopMove();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// 運動緊急停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotionEmgStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SelectedMotionAxis.StopEmg();
                }
                else
                {
                    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"裝置未開啟或未選擇可用軸");
                }
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        /// <summary>
        /// Motion 參數載入按鈕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotionConfigLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                FileName = string.Empty,
                Filter = "JSON File(*.json)|*.json",
                Title = "載入 json 檔",
                //InitialDirectory = Environment.CurrentDirectory + @"\motions"
                InitialDirectory = $@"{Directory.GetCurrentDirectory()}\{MotionDirectory}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using StreamReader reader = File.OpenText(openFileDialog.FileName);
                    string jsonStr = reader.ReadToEnd();

                    if (jsonStr != string.Empty)
                    {
                        MotionVelParam[] velParams = JsonSerializer.Deserialize<MotionVelParam[]>(jsonStr);

                        foreach (MotionVelParam item in velParams)
                        {
                            MotionAxis axis = MainWindow.ServoMotion.Axes.First(axis => axis.SlaveNumber == item.SlaveNumber);

                            axis.LoadFromVelParam(item);

                            // 先不要寫入
#if true
                            axis.SetGearRatio();
                            axis.SetJogVelParam();
                            axis.SetHomeVelParam();
                            axis.SetAxisVelParam();
#endif
                        }
                    }
                    else
                    {
                        MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, $"Motion 設定為空");
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"載入 Motion 設定失敗: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Motion 參數儲存按鈕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotionConfigSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                FileName = string.Empty,
                Filter = "JSON File(*.json)|*.json",
                //InitialDirectory = Environment.CurrentDirectory + @"\motions"
                InitialDirectory = $@"{Directory.GetCurrentDirectory()}\{MotionDirectory}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {

                try
                {
                    // 這邊要儲存所有參數
                    MotionVelParam[] AxisArray = MainWindow.ServoMotion.Axes.Select(axis => new MotionVelParam()
                    {
                        SlaveNumber = axis.SlaveNumber,
                        GearN1 = axis.GearN1,
                        GearM = axis.GearM,
                        JogVelLow = axis.JogVelLow,
                        JogVelHigh = axis.JogVelHigh,
                        JogAcc = axis.JogAcc,
                        JogDec = axis.JogDec,
                        JogVLTime = axis.JogVLTime,
                        HomeVelLow = axis.HomeVelLow,
                        HomeVelHigh = axis.HomeVelHigh,
                        HomeAcc = axis.HomeAcc,
                        HomeDec = axis.HomeDec,
                        Absolute = axis.Absolute,
                        VelLow = axis.VelLow,
                        VelHigh = axis.VelHigh,
                        Acc = axis.Acc,
                        Dec = axis.Dec
                    }).ToArray();
                    string jsonStr = JsonSerializer.Serialize(AxisArray, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(saveFileDialog.FileName, jsonStr);
                }
                catch (Exception ex)
                {
                    MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"儲存 Motion 設定失敗: {ex.Message}");
                }
            }
        }


        private void PackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"123");
        }

        //private void TextBox_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    (sender as TextBox).SelectAll();
        //}

        private void TextBox_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.NewValue);
            Debug.WriteLine(e.OldValue);
        }
    }
}
