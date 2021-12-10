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

            GetAvailableDevices();
            #endregion
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "運動頁面已載入");
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ServoMotion.DisableTimer();
        }

        /// <summary>
        /// 取得可用之 Devices
        /// </summary>
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
                    MainWindow.ServoMotion.CloseDevice();
                    //Debug.WriteLine($"Opened: {MainWindow.ServoMotion.DeviceOpened}");
                }
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.MOTION, ex.Message);
            }
        }

        private void BoardSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem is ServoMotion.DeviceList deviceList)
            {
                Debug.WriteLine($"{deviceList.DeviceName} {deviceList.DeviceNumber} {deviceList.NumOfSubDevice}");
            }
        }

        private void BoardSelector_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as ComboBox).SelectedIndex = -1;
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
                MainWindow.ServoMotion.ServoOnSwitch();
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
                MainWindow.ServoMotion.ResetPos();
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
