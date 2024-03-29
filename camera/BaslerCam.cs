﻿using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Data;

namespace ApexVisIns
{
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
    }

    /// <summary>
    /// CameraConfigs 儲存用基底，
    /// 繼承 BaslerCamInfo，新增Target Feature Type
    /// </summary>
    public class CameraConfigBase : BaslerCamInfo
    {
        public CameraConfigBase()
        {
        }

        public enum TargetFeatureType
        {
            [Description("(NULL)")]
            Null = 0,
            [Description("窗戶")]
            Window = 1,
            [Description("耳朵")]
            Ear = 2,
            [Description("表面 1")]
            Surface1 = 3,
            [Description("表面 2")]
            Surface2 = 4,

            [Description("MCA Jaw 前部攝影機")]
            MCA_Front = 11,
            [Description("MCA Jaw 底部攝影機")]
            MCA_Bottom = 12,
            [Description("MCA Jaw 側部攝影機")]
            MCA_SIDE = 13,
        }

        /// <summary>
        /// 目標特徵
        /// </summary>
        public TargetFeatureType TargetFeature { get; set; }
    }


    /// <summary>
    /// Basler Camera Basic setting
    /// Basler 相機基本設定
    /// </summary>
    public class BaslerCam : CustomCam, IDisposable
    {
        private bool _disposed;
        private bool _isGrabberOpened;
        private bool _isContinuousGrabbing;

        /// <summary>
        /// 相機建構子
        /// </summary>
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
        /// 是否連續拍攝
        /// </summary>
        public bool IsContinuousGrabbing
        {
            get => _isContinuousGrabbing;
            set
            {
                if (value != _isContinuousGrabbing)
                {
                    _isContinuousGrabbing = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否只開啟Grabber不拍照，拍照使用 RetrieveResult 觸發
        /// </summary>
        public bool IsGrabberOpened
        {
            get => _isGrabberOpened;
            set
            {
                if (value != _isGrabberOpened)
                {
                    _isGrabberOpened = value;
                    OnPropertyChanged();
                }
            }
        }

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
                ? throw new InvalidOperationException("Camera is a null object, initialize it before calling this function")
                : Camera.Open();

            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsOpen));
        }

        public override void Close()
        {
            if (Camera != null)
            {
                Camera.Close();
                Camera.Dispose();
                Camera = null;

                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsOpen));
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                ConfigList.Clear();
                ConfigList = null;

                Camera.Close();
                Camera.Dispose();
                Camera = null;
            }
            _disposed = true;
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
    /// Basler 組態,
    /// ConfigPanel 使用 (EngineerTab 內)
    /// </summary>
    public class BaslerConfig : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _width;
        private int _height;
        private double _fps;
        private double _exposureTimeAbs;
        private bool _saved;

        public BaslerConfig()
        {
        }

        public BaslerConfig(string name)
        {
            this.Name = name;
        }

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
    /// Camera 組態, 較為 Detail, 
    /// CameraTab 內使用
    /// </summary>
    public class CameraConfig : BaslerCamInfo, INotifyPropertyChanged
    {
        private string _userSet;
        private string[] _userSetEnum;
        private string _userSetRead;
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
        //
        private string[] _acquisitionModeEnum;
        private string _acquisitionMode;
        private string[] _triggerSelectorEnum;
        private string _triggerSelector;
        private string[] _triggerModeEnum;
        private string _triggerMode;
        private string[] _triggerSourceEnum;
        private string _triggerSource;
        //
        private string[] _exposureModeEnum;
        private string _exposureMode;
        private string[] _exposureAutoEnum;
        private string _exposureAuto;
        private double _exposureTime;
        private string _deviceVersion;
        private string _firmwareVersion;
        private int _sensorWidth;
        private int _sensorHeight;
        private bool _centerX;
        private bool _centerY;
        private bool _online;
        //
        private string[] _gainAutoEnum;
        private string _gainAuto;
        private int _gain;
        private int _blackLevel;
        private bool _gammaEnable;
        private string[] _gammaSelectorEnum;
        private string _gammaSelector;
        private double _gamma;
        // 

        /// <summary>
        /// .xaml 使用 (一般不使用)
        /// </summary>
        public CameraConfig() { }

        /// <summary>
        /// 正式建構子
        /// </summary>
        /// <param name="fullName">相機全名</param>
        /// <param name="model">相機型號</param>
        /// <param name="ip">相機IP</param>
        /// <param name="mac">相機MAC</param>
        /// <param name="serialNumber">相機S/N</param>
        public CameraConfig(string fullName, string model, string ip, string mac, string serialNumber) : base(fullName, model, ip, mac, serialNumber)
        {
        }

        #region 是否在線
        /// <summary>
        /// 相機是否在線
        /// </summary>
        public bool Online
        {
            get => _online;
            set
            {
                if (value != _online)
                {
                    _online = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region 基本相機 Info
        public string DeviceVersion
        {
            get => _deviceVersion;
            set
            {
                if (value != _deviceVersion)
                {
                    _deviceVersion = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
        /// 已讀取
        /// </summary>
        public string UserSetRead
        {
            get => _userSetRead;
            set
            {
                if (value != _userSetRead)
                {
                    _userSetRead = value;
                    OnPropertyChanged(nameof(UserSetRead));
                }
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    // < 0 => 0, > MaxWidth => MaxWidth
                    _width = value < 0 ? 0 : value > MaxWidth ? MaxWidth : value;
                    if (CenterX)
                    {
                        int half = (_maxWidth - _width) / 2;
                        OffsetX = half % 2 == 0 ? half : half - 1;
                    }
                    else
                    {
                        // Offset > 允許最大值 => 設為最大值
                        if (OffsetX > _maxWidth - _width)
                        {
                            OffsetX = _maxWidth - _width;
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OffsetX));
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
                    // < 0 => 0, > MaxHeight => MaxHeight
                    _height = value < 0 ? 0 : value > MaxHeight ? MaxHeight : value;
                    if (CenterY)
                    {
                        int half = (_maxHeight - _height) / 2;
                        OffsetY = half % 2 == 0 ? half : half - 1;
                    }
                    else
                    {
                        // Offset > 允許最大值 => 設為最大值
                        if (OffsetY > _maxHeight - _height)
                        {
                            OffsetY = _maxHeight - _height;
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OffsetY));
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    _offsetX = value < 0 ? 0 : value > _maxWidth - _width ? _maxWidth - _width : value;
                    OnPropertyChanged();
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
                    _offsetY = value < 0 ? 0 : value > _maxHeight - _height ? _maxHeight - _height : value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// X 置中
        /// </summary>
        public bool CenterX
        {
            get => _centerX;
            set
            {
                if (value != _centerX)
                {
                    // 計算 Offset X 置中
                    if (value == true)
                    {
                        int half = (MaxWidth - Width) / 2;
                        OffsetX = half % 2 == 0 ? half : half - 1;
                    }
                    _centerX = value;


                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OffsetX));
                }
            }
        }

        /// <summary>
        /// Y 置中
        /// </summary>
        public bool CenterY
        {
            get => _centerY;
            set
            {
                if (value != _centerY)
                {
                    // 計算 Offset Y 置中
                    if (value == true)
                    {
                        int half = (MaxHeight - Height) / 2;
                        OffsetY = half % 2 == 0 ? half : half - 1;
                    }
                    _centerY = value;

                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }
        #endregion


        #region Analog Controls
        /// <summary>
        /// 自動增益列舉
        /// </summary>
        public string[] GainAutoEnum
        {
            get => _gainAutoEnum;
            set
            {
                _gainAutoEnum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 自動增益
        /// </summary>
        public string GainAuto
        {
            get => _gainAuto;
            set
            {
                if (value != _gainAuto)
                {
                    _gainAuto = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 增益
        /// </summary>
        public int Gain
        {
            get => _gain;
            set
            {
                if (value != _gain)
                {
                    _gain = value;
                    OnPropertyChanged();
                }
            }
        }


        public int BlackLevel
        {
            get => _blackLevel;
            set
            {
                if (value != _blackLevel)
                {
                    _blackLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 啟用 Gamma
        /// </summary>
        public bool GammaEnable
        {
            get => _gammaEnable;
            set
            {
                if (value != _gammaEnable)
                {
                    _gammaEnable = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Gamma 選擇列舉
        /// </summary>
        public string[] GammaSelectorEnum
        {
            get => _gammaSelectorEnum;
            set
            {
                _gammaSelectorEnum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gamma 選擇
        /// </summary>
        public string GammaSelector
        {
            get => _gammaSelector;
            set
            {
                if (value != _gammaSelector)
                {
                    _gammaSelector = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Gamma
        /// </summary>
        public double Gamma
        {
            get => _gamma;
            set
            {
                if (value != _gamma)
                {
                    _gamma = value;
                    OnPropertyChanged();
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
        //public enum TargetFeatureType
        //{
        //    [Description("耳朵")]
        //    Ear = 1,
        //    [Description("窗戶")]
        //    Window = 2,
        //    [Description("表面 1")]
        //    Surface1 = 3,
        //    [Description("表面 2")]
        //    Surface2 = 4
        //}

        /// <summary>
        /// 相機 Character (之後可能綁定到 StreamGrabber UserData)
        /// </summary>
        //[JsonConverter(typeof(DeviceConfigBase.TargetFeatureType))]
        public CameraConfigBase.TargetFeatureType TargetFeature { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 外部觸發 Property Changed
        /// </summary>
        /// <param name="propertyName"></param>
        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}