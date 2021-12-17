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

namespace ApexVisIns.content
{
    /// <summary>
    /// MotionTab.xaml 的互動邏輯
    /// </summary>
    public partial class MotionTab : StackPanel
    {
        #region Variables
        [Obsolete]
        private uint boardCount;
        [Obsolete]
        private DEV_LIST[] BoardList = new DEV_LIST[10];

        private bool DllIsValid;
        #endregion

        public MotionTab()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Motion Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region 確認驅動安裝
            DllIsValid = ServoMotion.CheckDllVersion();
            if (DllIsValid)
            {
                GetAvaiDevs();
            }
            else
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, "Motion 控制驅動未安裝或版本不符");
            }
            #endregion
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
        public void GetAvaiDevs()
        {
            uint count = MainWindow.ServoMotion.GetAvailableDevices();

            if (count > 0)
            {
                // 選擇 第一個 Device
                DeviceSelector.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 取得可用之 Devices (包進 Servo Motion)
        /// </summary>
        [Obsolete("此方法已加入Servo motion中")]
        public void GetAvailableDevices()
        {
            int result = Motion.mAcm_GetAvailableDevs(BoardList, 10, ref boardCount);

            if (result != (int)ErrorCode.SUCCESS)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, $"列舉 EtherCAT Card 失敗 : {result}");
            }

            MainWindow.ServoMotion.BoardList.Clear();
            for (int i = 0; i < boardCount; i++)
            {
                MainWindow.ServoMotion.BoardList.Add(new ServoMotion.DeviceList(BoardList[i]));
            }

            if (boardCount > 0)
            {
                DeviceSelector.SelectedIndex = 0;
                // Debug.WriteLine($"{BoardList[0].DeviceName} {BoardList[0].DeviceNum} {BoardList[0].NumofSubDevice}");
            }
        }

        /// <summary>
        /// 開始軸卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!MainWindow.ServoMotion.DeviceOpened)
                {
                    MainWindow.ServoMotion.OpenDevice((DeviceSelector.SelectedItem as ServoMotion.DeviceList).DeviceNumber);
                    MainWindow.ServoMotion.EnableTimer(100);
                    //Debug.WriteLine($"Opened: {MainWindow.ServoMotion.DeviceOpened}");
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
        /// 選擇 Device 變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem is ServoMotion.DeviceList deviceList)
            {
                Debug.WriteLine($"BoardSelector_SelectionChanged {deviceList.DeviceName} {deviceList.DeviceNumber} {deviceList.NumOfSubDevice}");
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

            if (comboBox.SelectedItem is MotionAxis axis)
            {
                MainWindow.ServoMotion.SelectedAxis = comboBox.SelectedIndex;

                MainWindow.ServoMotion.SltMotionAxis.GetGearRatio();
                MainWindow.ServoMotion.SltMotionAxis.GetAxisVelParam();
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

                Debug.WriteLine($"{motionAxis.AxisIndex} {motionAxis.AxisName}");

                Debug.WriteLine($"{motionAxis.PosCommand} {motionAxis.PosActual} {motionAxis.CurrentStatus}");

                //Debug.WriteLine($"{}")

                if (!MainWindow.ServoMotion.SltMotionAxis.ServoOn)
                {
                    Debug.WriteLine("Servo On");
                    MainWindow.ServoMotion.SltMotionAxis.SetServoOn();
                }
                else
                {
                    Debug.WriteLine("Servo Off");
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
        /// 寫入速度參數
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VelParamSetBtn_Click(object sender, RoutedEventArgs e)
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

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
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

        private void PtToPtBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SltMotionAxis.PosMove(false);
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
                // throw;
            }
        }

        private void ChangePtBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SltMotionAxis.ChangePos();
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void ChangeVelBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.ServoMotion.SltMotionAxis.ChangeVel();
            }
            catch (InvalidOperationException ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

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
    }
}
