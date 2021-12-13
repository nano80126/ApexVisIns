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
        private uint boardCount;
        private DEV_LIST[] BoardList = new DEV_LIST[10];
        #endregion

        public MotionTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region 保留
            //else
            //{
            //    Timer.Start();
            //}
            //Timer.Start();

            //GetAvailableDevices();
            GetAvaiDevs();

            #endregion
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "運動頁面已載入");
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ServoMotion.DisableTimer();
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
                Debug.WriteLine("BoardSelector_SelectionChanged");
                Debug.WriteLine($"{deviceList.DeviceName} {deviceList.DeviceNumber} {deviceList.NumOfSubDevice}");
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
                MainWindow.ServoMotion.ResetPos();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddWarning(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void ParaWriteBtn_Click(object sender, RoutedEventArgs e)
        {

            MainWindow.ServoMotion.WriteParameter();


        }

        private void JogLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
        private void JogLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void JogRight_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void JogRight_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

    }
}
