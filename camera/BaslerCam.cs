﻿using Basler.Pylon;
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
                        BaslerCamInfo camInfo = new(info[CameraInfoKey.FriendlyName], info[CameraInfoKey.ModelName], info[CameraInfoKey.DeviceIpAddress], info[CameraInfoKey.DeviceMacAddress], info[CameraInfoKey.SerialNumber])
                        {
                            VendorName = info[CameraInfoKey.VendorName],
                            CameraType = info[CameraInfoKey.DeviceType],
                            //DeviceVersion = info[CameraInfoKey.DeviceVersion],
                        };

                        CamsSourceAdd(camInfo);

                        // CamsSourceAdd(new BaslerCamInfo(
                        //         info[CameraInfoKey.FriendlyName],
                        //         info[CameraInfoKey.ModelName],
                        //         info[CameraInfoKey.DeviceIpAddress],
                        //         info[CameraInfoKey.DeviceMacAddress],
                        //         info[CameraInfoKey.SerialNumber]
                        //     )
                        // {
                        // });
                    }
                }
            }
            catch (Exception)
            {
                // Display in message list
                //Console.WriteLine(ex.Message);
                throw;
            }
        }
    }


    /// <summary>
    /// Basler Camera Information, for camera enumerator
    /// Basler 相機資訊，相機枚舉器使用
    /// </summary>
    public class BaslerCamInfo
    {
        public BaslerCamInfo()
        {
        }

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
        /// 供應商名稱
        /// </summary>
        public string VendorName { get; set; }
        /// <summary>
        /// 相機全名
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// 相機 Model
        /// </summary>
        public string Model { get; set; }
        /// <summary>
        /// 相機類型
        /// </summary>
        public string CameraType { get; set; }
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

        ///// <summary>
        ///// 裝置版本
        ///// </summary>
        //public string DeviceVersion { get; set; }
        ///// <summary>
        ///// 韌體版本
        ///// </summary>
        //public string FirmwareVersion { get; set; }
    }


    /// <summary>
    /// Basler Camera Basic setting
    /// Basler 相機基本設定
    /// </summary>
    public class BaslerCam : CustomCam
    {
        // private int _frames = 0;

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
        /// 型號名稱
        /// </summary>
        public string ModelName { get; set; }
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


    /// <summary>
    /// Basler 組態
    /// 測試用
    /// </summary>
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

    /// <summary>
    /// Camera 組態, 較為 Detail, 正式流程使用
    /// </summary>
    public class DeviceConfig : BaslerCamInfo, INotifyPropertyChanged
    {
        private string _userSet;
        private string[] _userSetEnum;
        //
        private int _width;
        private int _height;
        private int _maxWidth;
        private int _maxHeight;
        private int _offsetX;
        private int _offsetY;
        //
        private bool _fixedFPS;
        private double _fps;

        private string[] _acquisitionModeEnum;
        private string _acquisitionMode;
        private string[] _triggerSelectorEnum;
        private string _triggerSelector;
        private string[] _triggerModeEnum;
        private string _triggerMode;
        private string[] _triggerSourceEnum;
        private string _triggerSource;

        private string[] _exposureModeEnum;
        private string _exposureMode;
        private string[] _exposureAutoEnum;
        private string _exposureAuto;
        private double _exposureTime;
        private string _deviceVersion;
        private string _firmwareVersion;
        private int _sensorWidth;
        private int _sensorHeight;


        /// <summary>
        /// .xaml 使用 (一般不使用)
        /// </summary>
        public DeviceConfig() { }

        /// <summary>
        /// 正式建構子
        /// </summary>
        /// <param name="fullName">相機全名</param>
        /// <param name="model">相機型號</param>
        /// <param name="ip">相機IP</param>
        /// <param name="mac">相機MAC</param>
        /// <param name="serialNumber">相機S/N</param>
        public DeviceConfig(string fullName, string model, string ip, string mac, string serialNumber) : base(fullName, model, ip, mac, serialNumber)
        {
        }

        #region 基本相機 Info
        public string DeviceVersion
        {
            get => _deviceVersion;
            set
            {
                if (value != _deviceVersion)
                {
                    _deviceVersion = value;
                    OnPropertyChanged(nameof(DeviceVersion));
                }
            }
        }

        public string FirmwareVersion
        {
            get => _firmwareVersion;
            set
            {
                if (value != _firmwareVersion)
                {
                    _firmwareVersion = value;
                    OnPropertyChanged(nameof(FirmwareVersion));
                }
            }
        }

        #endregion

        #region 基本相機 Config 
        public string UserSet
        {
            get => _userSet;
            set
            {
                if (value != _userSet)
                {
                    _userSet = value;
                    OnPropertyChanged(nameof(UserSet));
                }
            }
        }

        public string[] UserSetEnum
        {
            get => _userSetEnum;
            set
            {
                _userSetEnum = value;
                OnPropertyChanged(nameof(UserSetEnum));
            }
        }

        /// <summary>
        /// 組態名稱
        /// </summary>
        public string Name { get; set; }

        #region AOI Controls (Classify by Basler Pylon)
       
        /// <summary>
        /// Sensor 寬度
        /// </summary>
        public int SensorWidth
        {
            get => _sensorWidth;
            set
            {
                if (value != _sensorWidth)
                {
                    _sensorWidth = value;
                    OnPropertyChanged(nameof(SensorWidth));
                }
            }
        }

        /// <summary>
        /// Sensor 高度
        /// </summary>
        public int SensorHeight
        {
            get => _sensorHeight;
            set
            {
                if (value != _sensorHeight)
                {
                    _sensorHeight = value;
                    OnPropertyChanged(nameof(SensorHeight));
                }
            }
        }


        /// <summary>
        /// 寬度
        /// </summary>
        public int Width
        {
            get => _width;
            set
            {
                if (value != _width)
                {
                    _width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }

        /// <summary>
        /// 高度
        /// </summary>
        public int Height
        {
            get => _height;
            set
            {
                if (value != _height)
                {
                    _height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        /// <summary>
        /// Max Width, get value from camera parameters
        /// </summary>
        public int MaxWidth
        {
            get => _maxWidth;
            set
            {
                if (value != _maxWidth)
                {
                    _maxWidth = value;
                    OnPropertyChanged(nameof(MaxWidth));
                }
            }
        }

        /// <summary>
        /// Max Height, get value from camera parameters
        /// </summary>
        public int MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (value != _maxHeight)
                {
                    _maxHeight = value;
                    OnPropertyChanged(nameof(MaxHeight));
                }
            }
        }

        /// <summary>
        /// X 偏移
        /// </summary>
        public int OffsetX
        {
            get => _offsetX;
            set
            {
                if (value != _offsetX)
                {
                    _offsetX = value;
                    OnPropertyChanged(nameof(OffsetX));
                }
            }
        }

        /// <summary>
        /// Y 偏移
        /// </summary>
        public int OffsetY
        {
            get => _offsetY;
            set
            {
                if (value != _offsetY)
                {
                    _offsetY = value;
                    OnPropertyChanged(nameof(OffsetY));
                }
            }
        }
        #endregion

        #region Acquisition Control
        public string[] AcquisitionModeEnum
        {
            get => _acquisitionModeEnum;
            set
            {
                _acquisitionModeEnum = value;
                OnPropertyChanged(nameof(AcquisitionModeEnum));
            }
        }

        /// <summary>
        /// 擷取模式
        /// </summary>
        public string AcquisitionMode
        {
            get => _acquisitionMode;
            set
            {
                if (value != _acquisitionMode)
                {
                    _acquisitionMode = value;
                    OnPropertyChanged(nameof(AcquisitionMode));
                }
            }
        }

        /// <summary>
        /// Trigger Selector 列舉
        /// </summary>
        public string[] TriggerSelectorEnum
        {
            get => _triggerSelectorEnum;
            set
            {
                _triggerSelectorEnum = value;
                OnPropertyChanged(nameof(TriggerSelectorEnum));
            }
        }

        public string TriggerSelector
        {
            get => _triggerSelector;
            set
            {
                if (value != _triggerSelector)
                {
                    _triggerSelector = value;
                    OnPropertyChanged(nameof(TriggerSelector));
                }
            }
        }

        /// <summary>
        /// Trigger Mode 列舉
        /// </summary>
        public string[] TriggerModeEnum
        {
            get => _triggerModeEnum;
            set
            {
                _triggerModeEnum = value;
                OnPropertyChanged(nameof(TriggerModeEnum));
            }
        }

        public string TriggerMode
        {
            get => _triggerMode;
            set
            {
                if (value != _triggerMode)
                {
                    _triggerMode = value;
                    OnPropertyChanged(nameof(TriggerMode));
                }
            }
        }

        /// <summary>
        /// Trigger Source 列舉
        /// </summary>
        public string[] TriggerSourceEnum
        {
            get => _triggerSourceEnum;
            set
            {
                _triggerSourceEnum = value;
                OnPropertyChanged(nameof(TriggerSourceEnum));
            }
        }

        public string TriggerSource
        {
            get => _triggerSource;
            set
            {
                if (value != _triggerSource)
                {
                    _triggerSource = value;
                    OnPropertyChanged(nameof(TriggerSource));
                }
            }
        }

        /// <summary>
        /// 鎖定 FPS
        /// </summary>
        public bool FixedFPS
        {
            get => _fixedFPS;
            set
            {
                if (value != _fixedFPS)
                {
                    _fixedFPS = value;
                    OnPropertyChanged(nameof(FixedFPS));
                }
            }
        }

        /// <summary>
        /// 目標 FPS, FPS 鎖定後才生效
        /// </summary>
        public double FPS
        {
            get => _fps;
            set
            {
                if (value != _fps)
                {
                    _fps = value;
                    OnPropertyChanged(nameof(FPS));
                }
            }
        }

        /// <summary>
        /// 曝光模式(名稱待變更)
        /// </summary>
        public string[] ExposureModeEnum
        {
            get => _exposureModeEnum;
            set
            {
                _exposureModeEnum = value;
                OnPropertyChanged(nameof(ExposureModeEnum));
            }
        }

        public string ExposureMode
        {
            get => _exposureMode;
            set
            {
                if (value != _exposureMode)
                {
                    _exposureMode = value;
                    OnPropertyChanged(nameof(ExposureMode));
                }
            }
        }

        /// <summary>
        /// 自動曝光()
        /// </summary>
        public string[] ExposureAutoEnum
        {
            get => _exposureAutoEnum;
            set
            {
                _exposureAutoEnum = value;
                OnPropertyChanged(nameof(ExposureAutoEnum));
            }
        }

        public string ExposureAuto
        {
            get => _exposureAuto;
            set
            {
                if (value != _exposureAuto)
                {
                    _exposureAuto = value;
                    OnPropertyChanged(nameof(ExposureAuto));
                }
            }
        }

        /// <summary>
        /// 曝光時間
        /// </summary>
        public double ExposureTime
        {
            get => _exposureTime;
            set
            {
                if (value != _exposureTime)
                {
                    _exposureTime = value;
                    OnPropertyChanged(nameof(ExposureTime));
                }
            }
        }
        #endregion

#if false
        ///// <summary>
        ///// 相機全名
        ///// </summary>
        //public string CameraName { get; set; }
        ///// <summary>
        ///// 相機型號 
        ///// </summary>
        //public string Model { get; set; }
        ///// <summary>
        ///// 相機IP
        ///// </summary>
        //public string IP { get; set; }
        ///// <summary>
        ///// 相機 MAC
        ///// </summary>
        //public string MAC { get; set; }
        ///// <summary>
        ///// 相機 S / N
        ///// </summary>
        //public string SerialNumber { get; set; }  
#endif
        #endregion

        #region Application 應用
        /// <summary>
        /// 擔任角色
        /// </summary>
        public enum CharacterType
        {
            Ear = 1,
            Window = 2,
            Surface1 = 3,
            Surface2 = 4
        }

        /// <summary>
        /// 相機 Character
        /// </summary>
        public CharacterType Character { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class DeviceConfigList : ObservableCollection<DeviceConfig>
    {
        public DeviceConfigList()
        {

        }
    }
}