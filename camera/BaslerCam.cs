using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;

namespace ApexVisIns
{

    /// <summary>
    /// Basler Camera Enumerator
    /// Basler 相機枚舉器
    /// </summary>
    public class CameraEnumer : LongLifeWorker
    {
        private readonly object _CollectionLock = new();

        /// <summary>
        /// Camera source list
        /// </summary>
        public ObservableCollection<BaslerCamInfo> CamsSource { get; set; } = new ObservableCollection<BaslerCamInfo>();

        private void CamsSourceAdd(BaslerCamInfo info)
        {
            lock (_CollectionLock)
            {
                CamsSource.Add(info);
            }
        }

        private void CamsSourceClear()
        {
            lock (_CollectionLock)
            {
                CamsSource.Clear();
            }
        }

        public override void WorkerStart()
        {
            BindingOperations.EnableCollectionSynchronization(CamsSource, _CollectionLock);
            base.WorkerStart();
        }

        /// <summary>
        /// 工作內容
        /// </summary>
        public override void DoWork()
        {
            try
            {
                List<ICameraInfo> cams = CameraFinder.Enumerate();

                if (cams.Count == 0)
                {
                    CamsSourceClear(); // <= use this
                                       // Dispatcher.Invoke(() => CamsSource.Clear());
                                       // CamsSource.Clear();
                    _ = SpinWait.SpinUntil(() => false, 500);
                }

                foreach (ICameraInfo info in cams)
                {
                    if (!CamsSource.Any(item => item.SerialNumber == info[CameraInfoKey.SerialNumber]))
                    {
                        CamsSourceAdd(new BaslerCamInfo(
                                info[CameraInfoKey.FriendlyName],
                                info[CameraInfoKey.ModelName],
                                info[CameraInfoKey.DeviceIpAddress],
                                info[CameraInfoKey.DeviceMacAddress],
                                info[CameraInfoKey.SerialNumber]
                            ));
                    }
                }
            }
            catch (Exception ex)
            {
                // Display in message list
                Console.WriteLine(ex.Message);
            }
        }
    }


    /// <summary>
    /// Basler Camera Information, for camera enumerator
    /// Basler 相機資訊，相機枚舉器使用
    /// </summary>
    public class BaslerCamInfo
    {
        /// <summary>
        /// 建構式
        /// </summary>
        /// <param name="fullName">相機全名</param>
        /// <param name="model">相機 model</param>
        /// <param name="ip">相機 IP</param>
        /// <param name="mac">相機 mac</param>
        /// <param name="serialNumber">相機 S/N</param>
        public BaslerCamInfo(string fullName, string model, string ip, string mac, string serialNumber)
        {
            FullName = fullName;
            Model = model;
            IP = ip;
            MAC = mac;
            SerialNumber = serialNumber;
        }

        /// <summary>
        /// 相機全名
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// 相機 Model
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// 相機 IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// 相機 mac
        /// </summary>
        public string MAC { get; set; }
        /// <summary>
        /// 相機 S/N
        /// </summary>
        public string SerialNumber { get; set; }
    }


    /// <summary>
    /// Basler Camera Basic setting
    /// Basler 相機基本設定
    /// </summary>
    public class BaslerCam : CustomCam
    {
        //private int _frames = 0;

        public BaslerCam()
        {
            //
        }

        /// <summary>
        /// 相機建構子
        /// </summary>
        /// <param name="serialNumber">S/N</param>
        public BaslerCam(string serialNumber)
        {
            SerialNumber = serialNumber;
            Camera = new Camera(serialNumber);
        }

        /// <summary>
        /// Basler 相機
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// 相機是否連線
        /// </summary>
        public bool IsConnected => Camera != null && Camera.IsConnected;
        /// <summary>
        /// 相機是否開啟
        /// </summary>
        public override bool IsOpen => Camera != null && Camera.IsOpen;
        /// <summary>
        /// Grabber 是否開啟
        /// </summary>
        public bool IsGrabbing => Camera != null && Camera.StreamGrabber.IsGrabbing;

        /// <summary>
        /// 組態名稱
        /// </summary>
        //public string ConfigName { get; set; } = "Default";
        /// <summary>
        /// S / N
        /// </summary>
        public string SerialNumber { get; set; }
        /// <summary>
        /// 最大寬度
        /// </summary>
        public int WidthMax { get; set; }
        /// <summary>
        /// 最大高度
        /// </summary>
        public int HeightMax { get; set; }
        /// <summary>
        /// 最大 X 偏移
        /// </summary>
        public int OffsetXMax { get; set; }
        /// <summary>
        /// 最大 Y 偏移
        /// </summary>
        public int OffsetYMax { get; set; }
        /// <summary>
        /// X 偏移
        /// </summary>
        public int OffsetX { get; set; }
        /// <summary>
        /// Y 偏移
        /// </summary>
        public int OffsetY { get; set; }

        /// <summary>
        /// 當前套用之組態
        /// </summary>
        public BaslerConfig Config { get; set; }

        /// <summary>
        /// 組態列表
        /// </summary>
        public ObservableCollection<string> ConfigList { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Create camera object, call this function before open camera
        /// </summary>
        /// <param name="argument">serial number</param>
        public override void CreateCam(string argument)
        {
            SerialNumber = argument;
            Camera = new Camera(argument);
        }

        public override void Open()
        {
            _ = Camera == null
                ? throw new ArgumentNullException("Camera is a null object, initialize it before calling this function")
                : Camera.Open();
        }

        public override void Close()
        {
            Camera.Close();
            Camera.Dispose();
            Camera = null;
        }

        // 手動觸發 Property Change
        //public void PropertyChange()
        //{
        //    OnPropertyChanged();
        //}

        //public void PropertyChange(string propertyName)
        //{
        //    OnPropertyChanged(propertyName);
        //}

        //public event PropertyChangedEventHandler PropertyChanged;

        //private void OnPropertyChanged(string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }




    public class BaslerConfig : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _width;
        private int _height;
        private double _fps;
        private double _exposureTimeAbs;
        private bool _saved;
        /// <summary>
        /// 組態列表
        /// </summary>
        //public ObservableCollection<string> ConfigList { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 組態名稱
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    Saved = false;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        /// <summary>
        /// Resolution Width
        /// </summary>
        public int Width
        {
            get => _width;
            set
            {
                if (value != _width)
                {
                    _width = value;
                    Saved = false;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }
        /// <summary>
        /// Resolution Height
        /// </summary>
        public int Height
        {
            get => _height;
            set
            {
                if (value != _height)
                {
                    _height = value;
                    Saved = false;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }
        /// <summary>
        /// Camera FPS
        /// </summary>
        public double FPS
        {
            get => _fps;
            set
            {
                if (Math.Round(value, 1) != _fps)
                {
                    _fps = Math.Round(value, 1);
                    Saved = false;
                    OnPropertyChanged(nameof(FPS));
                }
            }
        }
        /// <summary>
        /// Camera Exposure Time
        /// </summary>
        public double ExposureTime
        {
            get => _exposureTimeAbs;
            set
            {
                if (value != _exposureTimeAbs)
                {
                    _exposureTimeAbs = value;
                    Saved = false;
                    OnPropertyChanged(nameof(ExposureTime));
                }
            }
        }
        /// <summary>
        /// 是否已儲存 (json)
        /// </summary>
        public bool Saved
        {
            get => _saved;
            set
            {
                if (value != _saved)
                {
                    _saved = value;
                    OnPropertyChanged(nameof(Saved));
                }
            }
        }
        /// <summary>
        /// 變更儲存狀態為已儲存
        /// </summary>
        public void Save()
        {
            Saved = true;
        }

        //public void PropertyChange(string propertyName)
        //{
        //    OnPropertyChanged(propertyName);
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}