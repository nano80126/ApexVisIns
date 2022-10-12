using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Basler.Pylon;
//using MCAJawIns._Camera;
using Debug = System.Diagnostics.Debug;


namespace MCAJawIns
{
    /// <summary>
    /// Basler Camera 物件
    /// <para>包含 Camera, 狀態, 與基本 AOI 屬性</para>
    /// </summary>
    public class BaslerCam : CustomCam, IDisposable
    {
        #region Fields
        private bool _disposed;
        private bool _isGrabberOpened;
        private bool _isContinuousGrabbing;
        private bool _isTriggerMode;
        private int _offsetX;
        private int _offsetY;

        private string[] _userSetEnum = new string[] { "112321131", "321321322", "12313213", "UserSet1", "UserSet2", "UserSet3" };
        private string _userSet;
        #endregion

        #region Properties
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
        [Obsolete("deprecated, MCA_Jaw 不推薦使用")]
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
        [Obsolete("deprecated, MCA_Jaw 不推薦使用")]
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
        /// 是否為 Trigger Mode
        /// </summary>
        public bool IsTriggerMode
        {
            get => _isTriggerMode;
            set
            {
                if (value != _isTriggerMode)
                {
                    _isTriggerMode = value;
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
        public virtual int WidthMax { get; set; }
        /// <summary>
        /// 最大高度
        /// </summary>
        public virtual int HeightMax { get; set; }
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
        public int OffsetX
        {
            get => _offsetX;
            set
            {
                if (value != _offsetX)
                {
                    _offsetX = value;
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
                    _offsetY = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// UserSet 列舉
        /// </summary>
        public string[] UserSetEnum
        {
            get => _userSetEnum;
            set
            {
                _userSetEnum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 當前 UserSet
        /// </summary>
        public string UserSet
        {
            get => _userSet;
        }

        /// <summary>
        /// 當前套用之組態
        /// </summary>
        public BaslerConfig Config { get; set; }
        /// <summary>
        /// 組態列表
        /// </summary>
        public ObservableCollection<string> ConfigList { get; set; } = new ObservableCollection<string>();
        #endregion

        #region 建構子
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
        #endregion

        #region Methods
        /// <summary>
        /// Create camera object, call this function before open camera
        /// </summary>
        /// <param name="argument">serial number</param>
        public override void CreateCam(string argument)
        {
            SerialNumber = argument;
            Camera = new Basler.Pylon.Camera(argument);
        }

        /// <summary>
        /// 設定 UserSet
        /// </summary>
        /// <param name="userSet"></param>
        public void SetUserSet(string userSet)
        {
            _userSet = userSet;
            //OnPropertyChanged(nameof(UserSet));
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
        #endregion

        // 手動觸發 Property Change
        // public void PropertyChange()
        // {
        //     OnPropertyChanged();
        // }

        // public void PropertyChange(string propertyName)
        // {
        //     OnPropertyChanged(propertyName);
        // }

        // public event PropertyChangedEventHandler PropertyChanged;

        // private void OnPropertyChanged(string propertyName = null)
        // {
        //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // }
    }

    /// <summary>
    /// Basler 組態,
    /// <para>Config Panel 使用</para>
    /// </summary>
    public class BaslerConfig : INotifyPropertyChanged
    {
        #region Fields
        private string _name = string.Empty;
        private int _width;
        private int _height;
        private double _fps;
        private double _exposureTimeAbs;
        private bool _saved;
        #endregion

        #region Properties
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
        #endregion

        #region 建構子
        public BaslerConfig()
        {
        }

        public BaslerConfig(string name)
        {
            this.Name = name;
        }
        #endregion

        #region Methods
        /// <summary>
        /// 變更儲存狀態為已儲存
        /// </summary>
        public void Save()
        {
            Saved = true;
        }
        #endregion

        // public void PropertyChange(string propertyName)
        // {
        //     OnPropertyChanged(propertyName);
        // }

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// 相機目標特徵
    /// </summary>
    public enum TargetFeature
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
    /// Basler Camera Information, for camera enumerator
    /// Basler 相機資訊，相機枚舉器使用
    /// </summary>
    public class CameraConfigBase
    {
        #region 建構子
        public CameraConfigBase() { }

        /// <summary>
        /// 建構式
        /// </summary>
        /// <param name="fullName">相機全名</param>
        /// <param name="model">相機 model</param>
        /// <param name="ip">相機 IP</param>
        /// <param name="mac">相機 mac</param>
        /// <param name="serialNumber">相機 S/N</param>
        public CameraConfigBase(string fullName, string model, string ip, string mac, string serialNumber)
        {
            FullName = fullName;
            Model = model;
            IP = ip;
            MAC = mac;
            SerialNumber = serialNumber;
        }
        #endregion

        #region Properties
        /// <summary>
        /// 供應商名稱
        /// </summary>
        public virtual string VendorName { get; set; }
        /// <summary>
        /// 相機全名
        /// </summary>
        public virtual string FullName { get; set; }
        /// <summary>
        /// 相機 Model
        /// </summary>
        public virtual string Model { get; set; }
        /// <summary>
        /// 相機類型
        /// </summary>
        public virtual string CameraType { get; set; }
        /// <summary>
        /// 相機 IP
        /// </summary>
        public virtual string IP { get; set; }
        /// <summary>
        /// 相機 mac
        /// </summary>
        public virtual string MAC { get; set; }
        /// <summary>
        /// 相機 S/N
        /// </summary>
        public virtual string SerialNumber { get; set; }
        #endregion
    }

    /// <summary>
    /// CameraConfigs 儲存用基底
    /// <para>繼承 Camera Config Base 基本屬性，並加上 Target Feature Type 與 鏡頭參數</para>
    /// </summary>
    public class CameraConfigBaseExtension : CameraConfigBase
    {
        #region Fields
        private readonly LensConfig _lensConfig = new();
        #endregion

        #region 建構子
        public CameraConfigBaseExtension()
        {
         
        }

        public CameraConfigBaseExtension(string fullName, string model, string ip, string mac, string serialNumber) : base(fullName, model, ip, mac, serialNumber)
        {

        }
        #endregion

        #region Properties
        /// <summary>
        /// 目標特徵
        /// <para>綁定到相機 UserData</para>
        /// </summary>
        public virtual TargetFeature TargetFeature { get; set; }

        /// <summary>
        /// Sensor Pixel Size
        /// </summary>
        public virtual double PixelSize { get; set; }

        /// <summary>
        /// 鏡頭參數
        /// <para>set 存取子內建 DeepCopy</para>
        /// </summary>
        public LensConfig LensConfig
        {
            get => _lensConfig;
            set
            {
                Type t = value.GetType();
                // object o = Activator.CreateInstance(t);
                PropertyInfo[] pi = t.GetProperties();
                for (int i = 0; i < pi.Length; i++)
                {
                    PropertyInfo p = pi[i];
                    p.SetValue(_lensConfig, p.GetValue(value));
                }
            }
        }
        #endregion

        #region Property Changed Event
        public event PropertyChangedEventHandler BasicPropertyChanged;
        protected void OnBasicPropertyChanged([CallerMemberName] string propertyName = null)
        {
            BasicPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void BasicPropertyChange(string propertyName = null)
        {
            BasicPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// 鏡頭 Config
    /// </summary>
    public class LensConfig : INotifyPropertyChanged
    {
        #region Fields
        private string _model;
        private string _manufacturer;
        private double _focalLength;
        private double _magnification;
        #endregion

        #region Properties
        public string Model
        {
            get => _model;
            set
            {
                if (value != _model)
                {
                    _model = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Manufacturer {
            get => _manufacturer;
            set
            {
                if (value != _manufacturer)
                {
                    _manufacturer = value;
                    OnPropertyChanged();
                }
            } 
        }

        public double FocalLength
        {
            get => _focalLength;
            set
            {
                if (value != _focalLength)
                {
                    _focalLength = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Magnification
        {
            get => _magnification;
            set
            {
                if (value != _magnification)
                {
                    _magnification = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// Camera 組態，CameraTab 內使用
    /// <para>Basic Properties</para>
    /// <para>AOI Controls</para>
    /// <para>Acquistion Controls</para>
    /// <para>Analog Controls</para>
    /// </summary>
    public class CameraConfig : CameraConfigBaseExtension, INotifyPropertyChanged
    {
        #region Fields
        //private TargetFeature _targetFeature;
        //
        #region Basic Info
        private bool _online;
        private string _deviceVersion;
        private string _firmwareVersion;
        #endregion
        //
        #region UserSet
        private string[] _userSetEnum;
        private string _userSet;
        private string _userSetRead;
        #endregion
        //
        #region AOI Controls
        private int _sensorWidth;
        private int _sensorHeight;
        private int _width;
        private int _height;
        private int _maxWidth;
        private int _maxHeight;
        private int _offsetX;
        private int _offsetY;
        private bool _centerX;
        private bool _centerY;
        #endregion
        //
        #region Acquistion Controls
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
        //
        private bool _fixedFPS;
        private double _fps;
        #endregion
        //
        #region Analog Controls
        private string[] _gainAutoEnum;
        private string _gainAuto;
        private int _gain;
        private int _blackLevel;
        private bool _gammaEnable;
        private string[] _gammaSelectorEnum;
        private string _gammaSelector;
        private double _gamma;
        #endregion
        //  
        #endregion

        #region Base Properties
        //public override TargetFeature TargetFeature
        //{
        //    get => _targetFeature;
        //    set
        //    {
        //        if (value != _targetFeature)
        //        {
        //            _targetFeature = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}
        public override TargetFeature TargetFeature
        {
            get => base.TargetFeature;
            set
            {
                if (value != base.TargetFeature)
                {
                    base.TargetFeature = value;
                    OnBasicPropertyChanged();
                    OnPropertyChanged();
                }
            }
        }

        public override double PixelSize
        {
            get => base.PixelSize;
            set
            {
                if (value != base.PixelSize)
                {
                    base.PixelSize = value;
                    OnBasicPropertyChanged();
                    OnPropertyChanged();
                }
            }
        }

        public override string IP
        {
            get => base.IP;
            set
            {
                if (value != base.IP)
                {
                    base.IP = value;
                    OnBasicPropertyChanged();
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Properties

        #region Basic Info
        // public override string IP { get => base.IP; set => base.IP = value; }

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

        #region UserSet
        /// <summary>
        /// UserSet Enum
        /// </summary>
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
        /// 欲讀取之 UserSet
        /// </summary>
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

        /// <summary>
        /// 已讀取之 UserSet
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
        #endregion

        #region AOI Controls
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

        /// <summary>
        /// Trigger Source
        /// </summary>
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
        /// 曝光模式列舉
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

        /// <summary>
        /// 曝光模式
        /// </summary>
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
        /// 自動曝光列舉
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

        /// <summary>
        /// 自動曝光
        /// </summary>
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
        /// Gamma 選擇器列舉
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

        #endregion

        #region 建構子
        /// <summary>
        /// .xaml 使用 (一般不使用)
        /// </summary>
        public CameraConfig()
        {

        }

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
        #endregion

        #region Property Changed Event
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