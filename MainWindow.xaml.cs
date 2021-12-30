﻿using Basler.Pylon;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace ApexVisIns
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region Long life worker
        /// <summary>
        /// Camera 列舉器
        /// 綁在 MainWindow
        /// </summary>
        public CameraEnumer CameraEnumer;
        //public Thermometer Thermometer;
        #endregion

        #region Cameras
        public static BaslerCam BaslerCam { get; set; }
        public static BaslerCam[] BaslerCams { get; set; }

        //public UvcCam UvcCam;
        #endregion

        #region Light Controller
        /// <summary>
        /// Com Port 列舉器，
        /// 綁在 MainWindow
        /// </summary>
        public LightEnumer LightEnumer;

        /// <summary>
        /// 光源控制器
        /// </summary>
        public static LightController LightController { get; set; }

        /// <summary>
        /// 光源控制器陣列
        /// </summary>
        public static LightController[] LightCtrls { get; set; }
        #endregion

        #region I/O Controller
        public static IOController IOController { get; set; }
        #endregion

        #region Devices
        /// <summary>
        /// 相機裝置列表
        /// </summary>
        public static ObservableCollection<DeviceConfig> DeviceConfigs { get; set; }
        #endregion

        #region EtherCAT Motion
        public static ServoMotion ServoMotion { get; set; }
        #endregion

        #region Resources
        //public Crosshair Crosshair;         // 待刪
        //public AssistRect AssistRect;       // 待刪
        //public AssistPoint[] AssistPoints;  // 待刪
        //public Indicator Indicator;         // 待刪
        /// <summary>
        /// 訊息通知器
        /// </summary>
        public static MsgInformer MsgInformer { get; set; }
        #endregion

        #region Varibles
        /// <summary>
        /// 影像處理中
        /// </summary>
        public static bool IsProcessing { get; set; }

        private readonly PixelDataConverter pxConverter = new()
        {
            OutputPixelFormat = PixelType.Mono8
        };
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializePanels();

            //GetSystemPath();
        }

        /// <summary>
        /// 初始化 Panel's MainWindow
        /// </summary>
        private void InitializePanels()
        {
            #region Set Tabs's and Panels's MainWindows ref
            // MainTab
            MainTab.MainWindow = this;
            // Config Tab
            DeviceTab.MainWindow = this;
            // Engineer Tab
            EngineerTab.MainWindow = this;
            EngineerTab.ConfigPanel.MainWindow = this;
            EngineerTab.LightPanel.MainWindow = this;
            EngineerTab.DigitalIOPanel.MainWindow = this;
            #endregion
        }

        /// <summary>
        /// 主視窗載入，
        /// 資源尋找等初始化在此
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Cameras
            CameraEnumer = TryFindResource(nameof(CameraEnumer)) as CameraEnumer;
            CameraEnumer?.WorkerStart();

#if DEBUG
            BaslerCam = FindResource(nameof(BaslerCam)) as BaslerCam;
#endif
            BaslerCams = FindResource(nameof(BaslerCams)) as BaslerCam[];
            #endregion

            #region Light Controller
            LightEnumer = TryFindResource(nameof(LightEnumer)) as LightEnumer;
            LightEnumer?.WorkerStart();

#if DEBUG
            LightController = FindResource(nameof(LightController)) as LightController;
#endif
            LightCtrls = FindResource(nameof(LightCtrls)) as LightController[];
            #endregion

            #region IO Controller
            IOController = FindResource(nameof(IOController)) as IOController;
            #endregion

            #region Device
            DeviceConfigs = FindResource(nameof(DeviceConfigs)) as ObservableCollection<DeviceConfig>;
            #endregion

            #region EtherCAT Motion
            ServoMotion = FindResource(nameof(ServoMotion)) as ServoMotion;
            ServoMotion.EnableCollectionBinding();
            #endregion

            #region Find Resource
            // Crosshair = FindResource(nameof(Crosshair)) as Crosshair;
            // AssistRect = FindResource(nameof(AssistRect)) as AssistRect;
            // Indicator = FindResource(nameof(Indicator)) as Indicator;
            // AssistPoints = FindResource(nameof(AssistPoints)) as AssistPoint[];
            MsgInformer = FindResource(nameof(ApexVisIns.MsgInformer)) as MsgInformer;
            MsgInformer.EnableProgressBar();
            MsgInformer.EnableCollectionBinding();
            #endregion

            // 載入後, focus 視窗
            _ = Focus();
        }

        /// <summary>
        /// 主視窗關閉，
        /// 停止 LongLifeWorker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MsgInformer.DisableCollectionBinding();
            MsgInformer.DisposeProgressTask();

            CameraEnumer.WorkerEnd();
            LightEnumer.WorkerEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            #region 保留
            //MsgInformer.CollectionDebinding();
            //MsgInformer.DisposeProgressTask();
            //CameraEnumer?.WorkerEnd();
            //LightEnumer?.WorkerEnd(); 
            #endregion
        }

        #region User Login
        private void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            // 1. 管理者登入
            // 2. 彈出視窗輸入操作密碼
            // 3. 登入後變更ICON
        }
        #endregion

        #region Footer Message
        private void ErrPopupBox_Opened(object sender, RoutedEventArgs e)
        {
            MsgInformer.ResetErrorCount();
        }

        private void MessageClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MsgInformer.ClearError();
        }

        private void InfoPopupbox_Opened(object sender, RoutedEventArgs e)
        {
            MsgInformer.ResetInfoCount();
        }

        private void InfoClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MsgInformer.ClearInfo();
        }

        private void PopupBox_Closed(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = TitleGrid.Focus();
        }
        #endregion


        /// <summary>
        /// 讓 TextBox 失去 Focus用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Panels_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = TitleGrid.Focus();
        }
    }


    /// <summary>
    /// ImageSource 轉換器
    /// </summary>
    public static class ImageSourceConverter
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern void DeleteObject(IntPtr o);

        #region 效能低下
        /// <summary>
        /// Low performace
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns> (盡量壓到 10ms 左右)
        public static BitmapSource ToBitmapSource(this Bitmap src)
        {
            BitmapSource bs;
            IntPtr ptr = src.GetHbitmap();
            //long imageSize = src.Width * src.Height * 3;
            //GC.AddMemoryPressure(imageSize);
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ptr);
                //GC.RemoveMemoryPressure(imageSize);
                src.Dispose();
            }
            return bs;
        }
        /// <summary>
        /// Medium performance
        /// </summary>
        /// <param name="src">Source bitmap</param>
        /// <returns></returns>
        public static ImageSource ToBitmapSource2(this Bitmap src)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                src.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        #endregion

        #region Bitmap & Mat convert to ImageSource (High performace)
        /// <summary>
        /// Convert Bitmap to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static ImageSource ToImageSource(this Bitmap src)
        {
            BitmapData bitmapData = src.LockBits(new System.Drawing.Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapSource bitmapSource = BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, bitmapData.Scan0, src.Width * src.Height * 3, src.Width * 3);
            src.UnlockBits(bitmapData);
            return bitmapSource;
        }

        /// <summary>
        /// Convert Mat to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static unsafe ImageSource ToImageSource(this Mat src)
        {
            switch (src.Type().Channels)
            {
                case 1:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Gray8, null, (IntPtr)src.DataPointer, src.Width * src.Height, src.Width);
                case 3:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, (IntPtr)src.DataPointer, src.Width * src.Height * 3, src.Width * 3);
                default:
                    throw new Exception("Unsupported type");
            }
        }
        #endregion
    }
}


namespace ApexVisIns.CustomProperty
{
    public class StatusHelper : DependencyObject
    {
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.RegisterAttached(
          "Connected", typeof(bool), typeof(StatusHelper), new PropertyMetadata(false));

        public static void SetConnected(DependencyObject target, bool value)
        {
            target.SetValue(ConnectedProperty, value);
        }

        public static bool GetConnected(DependencyObject target)
        {
            return (bool)target.GetValue(ConnectedProperty);
        }
    }
}

