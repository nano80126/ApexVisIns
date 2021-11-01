using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ApexVisIns
{
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    /// This file will be delete (wait for comfirmation)
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    public partial class MainWindow : System.Windows.Window
    {
        #region 變數宣告
        //private double centerY = 0;
        //private double topMinY = 0;
        //private double botMaxY = 0;

        //private OpenCvSharp.Point pt1 = new OpenCvSharp.Point(0, 0);
        //private OpenCvSharp.Point pt2 = new OpenCvSharp.Point(0, 0);

        //List<OpenCvSharp.Point> pts1 = new List<OpenCvSharp.Point>();
        /// <summary>
        /// Top Points Queue1
        /// </summary>
        //private readonly Queue<OpenCvSharp.Point> ptQueue1 = new Queue<OpenCvSharp.Point>();

        //List<OpenCvSharp.Point> pts2 = new List<OpenCvSharp.Point>();
        /// <summary>
        /// Bot Points Queue2
        /// </summary>
        //private readonly Queue<OpenCvSharp.Point> ptQueue2 = new Queue<OpenCvSharp.Point>();
        /// <summary>
        /// 處理經過時間
        /// </summary>
        private readonly Stopwatch processSw = new Stopwatch();
        #endregion

//        private void CamConnect_Checked(object sender, RoutedEventArgs e)
//        {
//            ToggleButton Toggle = sender as ToggleButton;
//#if BASLER
//            BaslerCamInfo info = CamSelector.SelectedItem as BaslerCamInfo;
//            Toggle.IsChecked = Basler_Connect(info.SerialNumber);
//#elif UVC
//            Toggle.IsChecked = Uvc_Connect(0);
//#endif
//        }

//        private void CamConnect_Unchecked(object sender, RoutedEventArgs e)
//        {
//            ToggleButton Toggle = sender as ToggleButton;
//#if BASLER
//            Toggle.IsChecked = Basler_Disconnect();
//#elif UVC
//            Toggle.IsChecked = Uvc_Disconnect();
//#endif
//        }


        /// <summary>
        /// 單張擷取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SingleShot_Click(object sender, RoutedEventArgs e)
        {
#if BASLER
            Basler_SingleGrab();
#elif UVC
            Uvc_SingleGrab(5);
#endif
        }

        /// <summary>
        /// 連續擷取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContinouseShot_Click(object sender, RoutedEventArgs e)
        {
#if BASLER
            Basler_ContinouseGrab();
#elif UVC
            Uvc_ContinousGrab();
#endif
        }

        /// <summary>
        /// RatioTextBlock Click 事件，縮放率調整為 100%
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RatioTextblock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Reset Zoom Ratio
            //if (e.ClickCount >= 2 && e.LeftButton == MouseButtonState.Pressed)
            //{
            //    ZoomRatio = 100;
            //}
        }

        /// <summary>
        /// 切換 Crosshair
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleCrosshair_Click(object sender, RoutedEventArgs e)
        {
            Crosshair.Enable = !Crosshair.Enable;
        }

        /// <summary>
        /// 切換 Assist Rect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleAssistRect_Click(object sender, RoutedEventArgs e)
        {
            AssistRect.Enable = !AssistRect.Enable;
        }

        /// <summary>
        /// Switch Tab Control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
        }

        /* ================================= 以下測試用 ================================= */
    }
}
