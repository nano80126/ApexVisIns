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

namespace ApexVisIns.content
{
    /// <summary>
    /// MotionTab.xaml 的互動邏輯
    /// </summary>
    public partial class MotionTab : StackPanel
    {
        #region Variables
        /// <summary>
        /// Dll 是否正確安裝
        /// </summary>
        private bool DllIsValid;
        private string MotionDirectory { get; } = @"./motions";

        #endregion

        public MotionTab()
        {
            InitializeComponent();
        }

        #region Load & Unload
        /// <summary>
        /// Motion Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region 確認驅動安裝 => 移動到 MotionEnumer 處理
            //DllIsValid = ServoMotion.CheckDllVersion();
            //if (DllIsValid)
            //{
            //    //GetAvaiDevs();
            //}
            //else
            //{
            //    MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "MOTION 控制驅動未安裝或版本不符");
            //}
            #endregion
            InitMotionsConfigsRoot();

            // 若D eviceOpened，始能 Timer
            if (MainWindow.ServoMotion.DeviceOpened == true)
            {
                MainWindow.ServoMotion.EnableTimer(100);
            }

            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "運動頁面已載入");
        }

        /// <summary>
        /// Motion Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ServoMotion.DisableTimer();
        }
        #endregion

        /// <summary>
        /// 初始化 Motion Config 路徑，
        /// 
        /// </summary>
        private void InitMotionsConfigsRoot()
        {
            // Directory 不存在則新增
            if (!Directory.Exists(MotionDirectory))
            {
                _ = Directory.CreateDirectory(MotionDirectory);
            }

#if false
            string path = $@"{MotionDirectory}/motion.json";

            if (!Directory.Exists(MotionDirectory))
            {
                // 新增路徑
                _ = Directory.CreateDirectory(MotionDirectory);
                //// 新增檔案
                //_ = File.CreateText(path);
            }
            else if (!File.Exists(path))
            {
                //_ = File.CreateText(path);
            }
            else
            {
                //using StreamReader reader = File.OpenText(path);
                //string jsonStr = reader.ReadToEnd();
                //if (jsonStr != string.Empty)
                //{
                //    // 反序列化
                //}
            } 
#endif
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
        /// 取得可用之 Device (EtherCAT卡)
        /// </summary>
        //public void GetAvaiDevs()
        //{
        //    // Board Count == 0 時才尋找
        //    // 重新尋找會導致 Handle 參考出問題

        //    if (MainWindow.ServoMotion.BoardCount == 0)
        //    {
        //        uint count = MainWindow.ServoMotion.GetAvailableDevices();

        //        if (count > 0)
        //        {
        //            // 選擇 第一個 Device
        //            DeviceSelector.SelectedIndex = 0;
        //        }
        //    }
        //}


        /// <summary>
        /// 選擇 Device 變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [Obsolete("待刪除，正常用不到")]
        private void BoardSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem is ServoMotion.MotionDevice deviceList)
            {
                Debug.WriteLine($"BoardSelector_SelectionChanged {deviceList.DeviceName} {deviceList.DeviceNumber} {deviceList.NumOfSubDevice}");
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
                    MainWindow.ServoMotion.OpenDevice((DeviceSelector.SelectedItem as ServoMotion.MotionDevice).DeviceNumber);
                    // // // 
                    // MainWindow.ServoMotion.EnableTimer(100);
                    // Debug.WriteLine($"Opened: {MainWindow.ServoMotion.DeviceOpened}");
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
                    MainWindow.ServoMotion.SltMotionAxis.GetGearRatio();
                    MainWindow.ServoMotion.SltMotionAxis.GetJogVelParam();
                    MainWindow.ServoMotion.SltMotionAxis.GetHomeVelParam();
                    MainWindow.ServoMotion.SltMotionAxis.GetAxisVelParam();
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

                if (!MainWindow.ServoMotion.SltMotionAxis.ServoOn)
                {
                    MainWindow.ServoMotion.SltMotionAxis.SetServoOn();
                }
                else
                {
                    MainWindow.ServoMotion.SltMotionAxis.SetServoOff();
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
                MainWindow.ServoMotion.SltMotionAxis.ResetPos();
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
                MainWindow.ServoMotion.SltMotionAxis.ResetError();
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        /// <summary>
        /// 寫入電子齒輪比
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GearRationSetBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SltMotionAxis.SetGearRatio();
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
                    MainWindow.ServoMotion.SltMotionAxis.SetJogVelParam();
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
            if (!MainWindow.ServoMotion.SltMotionAxis.JogOn)
            {
                try
                {
                    MainWindow.ServoMotion.SltMotionAxis.JogStart();
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
            if (MainWindow.ServoMotion.SltMotionAxis.JogOn)
            {
                try
                {
                    MainWindow.ServoMotion.SltMotionAxis.JogStop();
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
                MainWindow.ServoMotion.SltMotionAxis.JogCtClock();
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
                MainWindow.ServoMotion.SltMotionAxis.JogDecAction();
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
                MainWindow.ServoMotion.SltMotionAxis.JogClock();
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
                MainWindow.ServoMotion.SltMotionAxis.JogDecAction();
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
                    MainWindow.ServoMotion.SltMotionAxis.SetHomeVelParam();
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
                    await MainWindow.ServoMotion.SltMotionAxis.NegativeWayHomeMove();
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
                    MainWindow.ServoMotion.SltMotionAxis.SetAxisVelParam();
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
                    bool abs = MainWindow.ServoMotion.SltMotionAxis.Absolute;
                    MainWindow.ServoMotion.SltMotionAxis.PosMove(abs);
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
                    MainWindow.ServoMotion.SltMotionAxis.ChangePos();
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
                //MainWindow.ServoMotion.SltMotionAxis.ChangeVel();
                if (MainWindow.ServoMotion.DeviceOpened && MainWindow.ServoMotion.SelectedAxis != -1)
                {
                    MainWindow.ServoMotion.SltMotionAxis.ChangeVel();
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
        /// 運動停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotionStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SltMotionAxis.StopMove();
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
                MainWindow.ServoMotion.SltMotionAxis.StopEmg();
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
                InitialDirectory = Environment.CurrentDirectory + @"\motions"
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
#if false
                            axis.SetGearRatio();
                            axis.SetJogVelParam();
                            axis.SetHomeVelParam();
                            axis.SetAxisVelParam(); 
#endif
                        }
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
                InitialDirectory = Environment.CurrentDirectory + @"\motions"
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

        [Obsolete("測試完刪除")]
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            bool abs = MainWindow.ServoMotion.SltMotionAxis.Absolute;
            Debug.WriteLine(abs);
        }
    }
}
