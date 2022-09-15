using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCvSharp;


namespace MCAJawIns
{
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    /// This file is for binding data of application (public bindings) 
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
#if DEBUG || debug
        private readonly bool _debugMode = true;
#else
        private readonly bool _debugMode = false;
#endif

        #region private
        private readonly ImageSource[] _imgSrcArray = new ImageSource[4];
        private ImageSource _chartSource;
        private int _onNavIndex;
        private int _authLevel;
        #endregion

        /// <summary>
        /// Nav active index
        /// </summary>
        public int OnNavIndex
        {
            get => _onNavIndex;
            set
            {
                if (value != _onNavIndex)
                {
                    _onNavIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Debug 模式
        /// </summary>
        public bool DebugMode => _debugMode;

        #region Admin Password
        /// <summary>
        /// Admin Password
        /// </summary>
        private string Password { get; } = "admin0000";
        /// <summary>
        /// 密碼字典表 { "密碼", 等級 }
        /// </summary>
        internal Dictionary<string, int> PasswordDict { get; } = new Dictionary<string, int>();

        /// <summary>
        /// 權限等級
        /// 0: 無權限；
        /// 1: 操作員；
        /// 2: 品管員；
        /// 5: 工程師；
        /// 9: 開發者；
        /// </summary>
        public int AuthLevel {
            get => _authLevel; 
            set
            {
                if (value != _authLevel)
                {
                    _authLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LoginFlag));
                }
            }
        }

        /// <summary>
        /// 是否已登入
        /// </summary>
        public bool LoginFlag
        {
            get => _authLevel > 0;
        }
        #endregion

        #region Flags
        /// <summary>
        /// 硬體初始化用旗標
        /// </summary>
        public enum InitFlags
        {
            OK = 0,
            INIT_HARDWARE_FAILED = 1,
            SET_CAMERA_TRIGGER_MODE_FAILED = 2
        }
        #endregion

        /// <summary>
        /// 相機影像 1
        /// </summary>
        public ImageSource ImageSource1
        {
            get => _imgSrcArray[0];
            set
            {
                _imgSrcArray[0] = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 相機影像 2
        /// </summary>
        public ImageSource ImageSource2
        {
            get => _imgSrcArray[1];
            set
            {
                _imgSrcArray[1] = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 相機影像 3
        /// </summary>
        public ImageSource ImageSource3
        {
            get => _imgSrcArray[2];
            set
            {
                _imgSrcArray[2] = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 相機影像 4
        /// </summary>
        public ImageSource ImageSource4
        {
            get => _imgSrcArray[3];
            set
            {
                _imgSrcArray[3] = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 灰階 Chart
        /// </summary>
        public ImageSource ChartSource
        {
            get => _chartSource;
            set
            {
                _chartSource = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// 權限等級
    /// </summary>
    public class AuthLevel1
    {
        [BsonId]
        public ObjectId ObjID { get; set; }

        [BsonElement(nameof(Password))]
        public string Password { get; set; }

        [BsonElement(nameof(Level))]
        public int Level { get; set; }
    }


    public class SystemInfo : INotifyPropertyChanged, IDisposable
    {
        #region Private
        private System.Timers.Timer _timer;
        private bool _disposed;

        private bool _x64;
        private bool _auto;
        private string _mongoVer = null;
        private DateTime _startTime;
        /// <summary>
        /// 總自動模式時間，每次啟動自動模式時從資料庫讀取
        /// </summary>
        private int _totalAutoTime;
        /// <summary>
        /// 閒置時間計時器
        /// </summary>
        private Stopwatch _stopwatch;
        /// <summary>
        /// 量測總次數
        /// </summary>
        private int _totalParts = 0;
        #endregion

        #region Properties
        //[BsonId]
        //public ObjectId ObjID { get; set; } 

        /// <summary>
        /// 作業系統
        /// </summary>
        [BsonElement(nameof(OS))]
        public string OS { get; set; }
        /// <summary>
        /// 64 / 32 位元
        /// </summary>
        [BsonElement(nameof(Plateform))]
        public string Plateform => _x64 ? "64位元" : "32位元";
        /// <summary>
        /// Program ID
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public int PID { get; set; }
        /// <summary>
        /// .NET 版本
        /// </summary>
        [BsonElement(nameof(DotNetVer))]
        public string DotNetVer { get; set; }
        /// <summary>
        /// MongoDB 版本
        /// </summary>
        [BsonElement(nameof(MongoVer))]
        public string MongoVer => _mongoVer ?? "未連線";
        /// <summary>
        /// Basler Pylon API Versnio Number
        /// </summary>
        public string PylonVer
        {
            get
            {
                Debug.WriteLine($"Get Pylon Version");
                return FileVersionInfo.GetVersionInfo("Basler.Pylon.dll").FileVersion;
            }
        }
        /// <summary>
        /// 系統時間
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string SystemTime => $"{DateTime.Now:HH:mm:ss}";

        /// <summary>
        /// 軟體版本
        /// </summary>
        [BsonElement(nameof(SoftVer))]
        public string SoftVer { get; set; } = "2.0.0";
        /// <summary>
        /// 模式
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string Mode => _auto ? "自動模式" : "編輯模式";
        /// <summary>
        /// 自動運行時間
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public string AutoTime
        {
            get
            {
                if (_auto)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds((DateTime.Now - _startTime).TotalSeconds - (_stopwatch?.Elapsed.TotalSeconds ?? 0));

                    Debug.WriteLine($"{timeSpan} {(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}");
                    Debug.WriteLine($"{timeSpan.Seconds} {timeSpan.TotalSeconds}");


                    return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
                }
                else
                {
                    return "00:00:00";
                }
            }
        }
        /// <summary>
        /// 自動運行時間 (累計)
        /// </summary>
        [BsonElement(nameof(TotalAutoTime))]
        public string TotalAutoTime
        {
            get
            {
                if (_auto)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds((DateTime.Now - _startTime).TotalSeconds - (_stopwatch?.Elapsed.TotalSeconds ?? 0) + _totalAutoTime);
                    return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
                }
                else
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(_totalAutoTime);
                    return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
                }
            }
        }
        /// <summary>
        /// 自動運行時間 (小時)，(TotalAutoTime 超過 9999時，由這邊紀錄)
        /// </summary>
        [BsonElement(nameof(TotalHours))]
        public int TotalHours
        {
            get
            {
                if (_auto)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds((DateTime.Now - _startTime).TotalSeconds - (_stopwatch?.Elapsed.TotalSeconds ?? 0) + _totalAutoTime);
                    return (int)timeSpan.TotalHours;
                }
                else
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(_totalAutoTime);
                    return (int)timeSpan.TotalHours;
                }
            }
        }
        /// <summary>
        /// 總計檢驗數量
        /// </summary>
        [BsonElement(nameof(TotalParts))]
        public int TotalParts
        {
            get => _totalParts;
            set
            {
                if (value != _totalParts)
                {
                    _totalParts = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// 閒置時間 (seconds) (無條件捨棄)
        /// </summary>
        [BsonIgnore]
        [JsonIgnore]
        public int IdleTime => _stopwatch != null ? (int)(_stopwatch.ElapsedMilliseconds / 1000.0) : 0;
        #endregion

        #region Methods
        /// <summary>
        /// 設定 32/64 位元
        /// </summary>
        /// <param name="is64bit">是否為 64 bits</param>
        public void SetPlateform(bool is64bit)
        {
            _x64 = is64bit;
        }
        /// <summary>
        /// 設定 自動/編輯 模式
        /// </summary>
        public void SetMode(bool auto)
        {
            _auto = auto;
        }
        /// <summary>
        /// 設定 Mongo 版本
        /// </summary>
        /// <param name="version">版本</param>
        public void SetMongoVersion(string version = null)
        {
            _mongoVer = version;
        }

        /// <summary>
        /// 設定啟動時間
        /// </summary>
        /// <param name="dateTime">啟動時間</param>
        public void SetStartTime()
        {
            _startTime = DateTime.Now;
        }
        /// <summary>
        /// 設定 Total Auto Time
        /// </summary>
        public void SetTotalAutoTime(int seconds)
        {
            _totalAutoTime = seconds;
        }
        /// <summary>
        /// 開始計算閒置時間
        /// </summary>
        public void StartIdleWatch()
        {
            if (_stopwatch == null)
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }
            else if (_stopwatch?.IsRunning is false)
            {
                _stopwatch.Start();
            }
        }
        /// <summary>
        /// 停止計算閒置時間
        /// </summary>
        public void StopIdleWatch()
        {
            if (_stopwatch?.IsRunning is true)
            {
                _stopwatch.Stop();
            }
        }
        /// <summary>
        /// 取得 自動運行時間 (秒)
        /// </summary>
        /// <returns></returns>
        public int GetAutoTimeInSeconds()
        {
            if (_auto)
            {
                return (int)((DateTime.Now - _startTime).TotalSeconds - _stopwatch.Elapsed.TotalSeconds);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 取得 累計自動運行時間 (秒)
        /// </summary>
        /// <returns></returns>
        public int GetTotalAutoTimeTnSeconds()
        {
            if (_auto)
            {
                return (int)((DateTime.Now - _startTime).TotalSeconds - _stopwatch.Elapsed.TotalSeconds + _totalAutoTime);
            }
            else
            {
                return _totalAutoTime;
            }
        }
        #endregion

        #region 定時執行 timer
        /// <summary>
        /// 啟動 Timer (刷新UI用)
        /// </summary>
        public void EnableTimer()
        {
            if (_timer == null)
            {
                _timer = new System.Timers.Timer()
                {
                    Interval = 1000,
                    AutoReset = true,
                };

                _timer.Elapsed += Timer_Elapsed;
                _timer.Start();
            }
            else if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// 計時器 Elapsed 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            OnPropertyChanged(nameof(SystemTime));

            Debug.WriteLine(_auto);
            if (_auto)
            {
                OnPropertyChanged(nameof(AutoTime));
                OnPropertyChanged(nameof(TotalAutoTime));
            }
            OnPropertyChanged(nameof(IdleTime)); // 之後需移除
                                                 // Debug.WriteLine($"Binding.cs line 443 {DateTime.Now:HH:mm:ss}");
        }

        /// <summary>
        /// 停止 Timer (刷新UI用)
        /// </summary>
        public void DisableTimer()
        {
            if (_timer?.Enabled == true)
            {
                _timer.Stop();
            }
        }
        #endregion

        #region Property Changed Event
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dispose
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
                _timer.Stop();
                _timer.Dispose();
            }
            _disposed = true;
        }
        #endregion
    }

    /// <summary>
    /// Crosshair
    /// </summary>
    public class Crosshair : INotifyPropertyChanged
    {
        private bool _enable;

        /// <summary>
        /// Is Crosshair visiable
        /// </summary>
        public bool Enable
        {
            get => _enable;
            set
            {
                if (value != _enable)
                {
                    _enable = value;
                    OnPropertyChanged(nameof(Enable));
                }
            }
        }

        /// <summary>
        /// Color
        /// </summary>
        public SolidColorBrush Stroke { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// 標示器
    /// </summary>
    public class Indicator : INotifyPropertyChanged
    {
        private Mat _img;

        private Mat _oriImg;

        public Mat Image
        {
            get => _img;
            set
            {
                _img = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImageSource));
                OnPropertyChanged(nameof(R));
                OnPropertyChanged(nameof(G));
                OnPropertyChanged(nameof(B));
            }
        }

        public Mat OriImage
        {
            get => _oriImg;
            set
            {
                _oriImg = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 主影像 Source
        /// </summary>
        public ImageSource ImageSource => _img?.ToImageSource();

        /// <summary>
        /// R 像素
        /// </summary>
        public byte R => _img != null ? _img.Channels() == 1 ? _img.At<byte>(Y, X) : _img.At<Vec3b>(Y, X)[2] : (byte)0;
        /// <summary>
        /// G 像素
        /// </summary>
        public byte G => _img != null ? _img.Channels() == 1 ? _img.At<byte>(Y, X) : _img.At<Vec3b>(Y, X)[1] : (byte)0;
        /// <summary>
        /// B 像素
        /// </summary>
        public byte B => _img != null ? _img.Channels() == 1 ? _img.At<byte>(Y, X) : _img.At<Vec3b>(Y, X)[0] : (byte)0;

        /// <summary>
        /// X pos
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// Y pos
        /// </summary>
        public int Y { get; private set; }

        public void SetPoint(int x, int y)
        {
            X = x;
            Y = y;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));
            //OnPropertyChanged();
        }

        public void GetRGB(int x, int y, out byte R, out byte G, out byte B)
        {
            int chs = _img.Channels();

            if (chs == 1)
            {
                R = _img.At<byte>(y, x);
                G = _img.At<byte>(y, x);
                B = _img.At<byte>(y, x);
            }
            else
            {
                R = _img.At<Vec3b>(y, x)[2];
                G = _img.At<Vec3b>(y, x)[1];
                B = _img.At<Vec3b>(y, x)[0];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange()
        {

        }
    }

    /// <summary>
    /// 基本 Rect
    /// </summary>
    public class BasicRect : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        public double X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }
        public double Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    _y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }
        public double Width
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
        public double Height
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
        public Rect GetRect()
        {
            return new Rect((int)_x, (int)_y, (int)_width, (int)_height);
        }
        // // // // // //
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 輔助用矩形
    /// </summary>
    public class AssistRect : BasicRect
    {
        #region Basic Variable
        private bool _enable;
        private bool _isLeftMouseDown;
        private bool _isMiddleMouseDown;

        //private double _strokeThickness;

        //private bool _mouseDown;
        // //
        public double TempX { get; set; }    // Temp Pos of Mousedown
        public double TempY { get; set; }    // Temp Pos of Mousedown
        public double OftX { get; set; }     // Offset Pos of Mousedown
        public double OftY { get; set; }     // Offset Pos of Mousedown
        #endregion

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool Enable
        {
            get => _enable;
            set
            {
                if (value != _enable)
                {
                    _enable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 滑鼠按鍵是否按下
        /// </summary>
        public bool IsMouseDown => IsLeftMouseDown || IsMiddleMouseDown;

        /// <summary>
        /// 滑鼠左鍵是否按下
        /// </summary>
        public bool IsLeftMouseDown
        {
            get => _isLeftMouseDown;
            set
            {
                if (value != _isLeftMouseDown)
                {
                    _isLeftMouseDown = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsMouseDown));
                }
            }
        }

        /// <summary>
        /// 滑鼠中鍵是否按下
        /// </summary>
        public bool IsMiddleMouseDown
        {
            get => _isMiddleMouseDown;
            set
            {
                if (value != _isMiddleMouseDown)
                {
                    _isMiddleMouseDown = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsMouseDown));
                }
            }
        }

        /// <summary>
        /// Stroke Color
        /// </summary>
        public SolidColorBrush Stroke { get; set; }
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Rect 面積
        /// </summary>
        public double Area => Width * Height;

        /// <summary>
        /// Reset temp point
        /// </summary>
        public void ResetTemp()
        {
            TempX = TempY = 0;
        }

        /// <summary>
        /// 重置滑鼠按鍵
        /// </summary>
        public void ResetMouse()
        {
            IsLeftMouseDown = false;
            IsMiddleMouseDown = false;
        }
    }

    public class AssistPoints : INotifyPropertyChanged
    {
        #region Private
        private bool _enable;
        private bool _isMouseDown; 
        #endregion

        public AssistPoints()
        {
            Source = new ObservableCollection<AssistPoint>();
        }

        /// <summary>
        /// 點集合
        /// </summary>
        public ObservableCollection<AssistPoint> Source { get; set; }
            
        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool Enable
        {
            get => _enable;
            set
            {
                if (value != _enable)
                {
                    _enable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 滑鼠按鍵是否按下
        /// </summary>
        public bool IsMouseDown
        {
            get => _isMouseDown;
            set
            {
                if (value != _isMouseDown)
                {
                    _isMouseDown = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 重置滑鼠按鍵
        /// </summary>
        public void ResetMouse()
        {
            IsMouseDown = false;
        }

        #region Property Change Event
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// 輔助用 Point
    /// </summary>
    public class AssistPoint : INotifyPropertyChanged
    {
        #region Basic Variable
        private double _x;
        private double _y;
        #endregion

        public AssistPoint(double x, double y, byte r, byte g, byte b, double strokeThickness = 1)
        {
            X = x;
            Y = y;
            // Stroke = stroke;
            R = r;
            G = g;
            B = b;

            StrokeThickness = strokeThickness;
        }

        /// <summary>
        /// Point X
        /// </summary>
        public double X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }
        /// <summary>
        /// Point Y
        /// </summary>
        public double Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    _y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }

        /// <summary>
        /// R channel
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// G channel
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// B channel
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Point 顏色
        /// </summary>
        public SolidColorBrush Stroke
        {
            get
            {
                byte r = Math.Abs(127 - R) < 51 ? (byte)(R < 127 ? 255 : 0) : (byte)(255 - R);
                byte g = Math.Abs(127 - G) < 51 ? (byte)(G < 127 ? 255 : 0) : (byte)(255 - G);
                byte b = Math.Abs(127 - B) < 51 ? (byte)(B < 127 ? 255 : 0) : (byte)(255 - B);

                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
        }

        public double StrokeThickness { get; set; }

        /// <summary>
        /// 設置 point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetPoint(double x, double y)
        {
            _x = x;
            _y = y;
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MsgInformer : INotifyPropertyChanged
    {
        #region LockObject
        /// <summary>
        /// Info Collection Lock
        /// </summary>
        private readonly object _infoCollLock = new();
        /// <summary>
        /// Error Collection Lock
        /// </summary>
        private readonly object _errCollLock = new();
        #endregion

        #region Varibles
        private int _progress;
        private int _lastProgressValue;

        private Task progressTask;
        private CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        /// <summary>
        /// AnimationList
        /// </summary>
        private readonly List<Action> ProgressAnimation = new();
        #endregion


        /// <summary>
        /// 當前 Progress Value
        /// </summary>
        public int ProgressValue
        {
            get => _progress;
            set
            {
                if (value != _progress)
                {
                    _progress = value > 100 ? 100 : value;
                    OnPropertyChanged();
                }
            }
        }

#if false // Deprecated
        /// <summary>
        /// 目標 Progress Value
        /// </summary>
        public int TargetProgressValue
        {
            get => _targetProgressValue;
            set
            {
                Debug.WriteLine($"TargetProgressChanged  Value: {value} TargetValue: {_targetProgressValue}      Binding.cs line 641 {DateTime.Now:mm:ss.fff}");

                if (value > _targetProgressValue)
                {
                    // 計算時間 1 % = 25 ms
                    TimeSpan timeSpan = TimeSpan.FromMilliseconds((value - _targetProgressValue) * 25);
                    // 更新 TargetProgressValue
                    _targetProgressValue = value;
                    //
                    // 插入工作序列
                    ProgressAnimation.Add(() =>
                    {
                        OnProgressValueChanged(_progress, value, timeSpan);
                        _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, timeSpan);
                        // 等待動畫結束後更新
                        ProgressValue = value;
                    });
                }
                OnPropertyChanged();
            }
        } 
#endif

        /// <summary>
        /// 啟用 ProgressBar
        /// </summary>
        public void EnableProgressBar()
        {
            progressTask = Task.Run(() =>
            {
                while (ProgressValue < 100 && !CancellationTokenSource.IsCancellationRequested)
                {
                    if (ProgressAnimation.Count > 0)
                    {
                        ProgressAnimation[0]();
                        ProgressAnimation.RemoveAt(0);
                        continue;
                    }
                    _ = SpinWait.SpinUntil(() => false, 50);
                }
            });
        }

        /// <summary>
        /// 取消 ProgressBar
        /// </summary>
        public void DisposeProgressTask()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }

            if (progressTask != null)
            {
                progressTask.Wait();
                progressTask.Dispose();
                ProgressAnimation.Clear();
            }
        }

        /// <summary>
        /// 推進 ProgressBarValue
        /// </summary>
        /// <param name="value"></param>
        public void AdvanceProgressValue(int value)
        {
            // 下個進度值
            int toValue = _lastProgressValue + value;
            _lastProgressValue = toValue;
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(value * 25);

            ProgressAnimation.Add(() =>
            {
                OnProgressValueChanged(_progress, toValue, timeSpan);
                // 等待動畫結束後更新
                _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, timeSpan);
                ProgressValue = toValue;
                //Debug.WriteLine($"ProgressValue: {ProgressValue}");
            });
        }

        public void EnableCollectionBinding()
        {
            BindingOperations.EnableCollectionSynchronization(InfoSource, _infoCollLock);
            BindingOperations.EnableCollectionSynchronization(ErrSource, _errCollLock);
        }

        public void DisableCollectionBinding()
        {
            BindingOperations.DisableCollectionSynchronization(InfoSource);
            BindingOperations.DisableCollectionSynchronization(ErrSource);
        }

        public int NewError { get; private set; }

        public int ErrorCount => ErrSource.Count;

        /// <summary>
        /// 新增 Warnging
        /// </summary>
        /// <param name="code"></param>
        /// <param name="description"></param>
        public void AddWarning(Message.MsgCode code, string description)
        {
            lock (_errCollLock)
            {
                ErrSource.Push(new Message
                {
                    Code = code,
                    Description = description,
                    MsgType = Message.MessageType.Warning
                });
            }
            NewError++;
            OnPropertyChanged(nameof(NewError));
            OnPropertyChanged(nameof(ErrorCount));
        }

        /// <summary>
        /// 新增 Error
        /// </summary>
        /// <param name="code">Message Code</param>
        /// <param name="description">Description</param>
        /// <param name="type">Message Type, 若為 info, 自動改為 warngin</param>
        public void AddError(Message.MsgCode code, string description)
        {
            lock (_errCollLock)
            {
                ErrSource.Push(new Message
                {
                    Code = code,
                    Description = description,
                    MsgType = Message.MessageType.Error
                });
            }
            NewError++;
            OnPropertyChanged(nameof(NewError));
            OnPropertyChanged(nameof(ErrorCount));
        }

        /// <summary>
        /// 清空 Error
        /// </summary>
        public void ClearError()
        {
            ErrSource.Clear();
            NewError = 0;
            OnPropertyChanged(nameof(NewError));
        }

        public void ResetErrorCount()
        {
            NewError = 0;
            OnPropertyChanged(nameof(NewError));
        }

        public int NewInfo { get; private set; }

        public int InfoCount => InfoSource.Count;

        /// <summary>
        /// 新增 Success 訊息
        /// </summary>
        /// <param name="code">Message Code</param>
        /// <param name="description">Description</param>
        public void AddSuccess(Message.MsgCode code, string description)
        {
            lock (_infoCollLock)
            {
                InfoSource.Push(new Message
                {
                    Code = code,
                    Description = description,
                    MsgType = Message.MessageType.Success
                });
            }
            NewInfo++;
            OnPropertyChanged(nameof(NewInfo));
            OnPropertyChanged(nameof(InfoCount));
        }

        /// <summary>
        /// 新增 Info 訊息
        /// </summary>
        /// <param name="code">Message Code</param>
        /// <param name="description">Description</param>
        public void AddInfo(Message.MsgCode code, string description)
        {
            lock (_infoCollLock)
            {
                InfoSource.Push(new Message
                {
                    Code = code,
                    Description = description,
                    MsgType = Message.MessageType.Info
                });
            }
            NewInfo++;
            OnPropertyChanged(nameof(NewInfo));
            OnPropertyChanged(nameof(InfoCount));
        }

        /// <summary>
        /// 清空 Info
        /// </summary>
        public void ClearInfo()
        {
            InfoSource.Clear();
            NewInfo = 0;
            OnPropertyChanged(nameof(NewInfo));
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetInfoCount()
        {
            NewInfo = 0;
            OnPropertyChanged(nameof(NewInfo));
        }

        /// <summary>
        /// Stack Source of Message
        /// </summary>
        public ObservableStack<Message> InfoSource { get; set; } = new ObservableStack<Message>();

        public ObservableStack<Message> ErrSource { get; set; } = new ObservableStack<Message>();

        /// <summary>
        /// Message 訊息
        /// </summary>
        public class Message
        {
            /// <summary>
            /// Info / Warning / Error Code
            /// </summary>
            public enum MsgCode
            {
                /// <summary>
                /// Application Error Code
                /// </summary>
                APP,
                /// <summary>
                /// Info & Error for Plot
                /// </summary>
                CHART,
                /// <summary>
                /// Camera Error Code
                /// </summary>
                CAMERA,
                /// <summary>
                /// Database Exception
                /// </summary>
                DATABASE,
                /// <summary>
                /// DLL Exception 
                /// </summary>
                DLL,
                /// <summary>
                /// General Exception
                /// </summary>
                EX,
                /// <summary>
                /// 
                /// </summary>
                F,
                /// <summary>
                /// 
                /// </summary>
                G,
                /// <summary>
                /// Light Control Error Code 
                /// </summary>
                LIGHT,
                /// <summary>
                /// JAW Inspection Error
                /// </summary>
                JAW, 
                /// <summary>
                /// OpenCv Process Error Code
                /// </summary>
                OPENCV,
                /// <summary>
                /// OpenCvSharp Process Error Code
                /// </summary>
                OPENCVSHARP,
                /// <summary>
                /// I/O Control Exception Code
                /// </summary>
                IO,
                /// <summary>
                /// EtherCAT Motion Exception Code
                /// </summary>
                MOTION
            }

            /// <summary>
            /// 
            /// </summary>
            public enum MessageType
            {
                Success = 0,
                Info = 1,
                Warning = 2,
                Error = 3,
            }

            public MsgCode Code { get; set; }
            public string Description { get; set; }
            public MessageType MsgType { get; set; }
            public SolidColorBrush MsgColor
            {
                get
                {
                    switch (MsgType)
                    {
                        case MessageType.Success:
                            return new SolidColorBrush(Color.FromArgb(255, 46, 175, 80));
                        case MessageType.Info:
                            return new SolidColorBrush(Color.FromArgb(255, 33, 150, 243));
                        case MessageType.Warning:
                            return new SolidColorBrush(Color.FromArgb(255, 255, 152, 0));
                        case MessageType.Error:
                            return new SolidColorBrush(Color.FromArgb(255, 255, 82, 82));
                        default:
                            return new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }

        #region ProgressValueChanged
        /// <summary>
        /// 進度表 ChangedEventHandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ProgressValueChangedEventHandler(object sender, ProgressValueChangedEventArgs e);
        /// <summary>
        /// 進度表 ChangedEvent
        /// </summary>
        public event ProgressValueChangedEventHandler ProgressValueChanged;

        private void OnProgressValueChanged(int oldValue, int newValue, TimeSpan duration)
        {
            ProgressValueChanged?.Invoke(this, new ProgressValueChangedEventArgs(oldValue, newValue, duration));
        }

        public class ProgressValueChangedEventArgs : EventArgs
        {
            public ProgressValueChangedEventArgs(int a, int b, TimeSpan duration)
            {
                OldValue = a;
                NewValue = b;
                Duration = duration;
            }

            public int OldValue { get; }

            public int NewValue { get; }

            public TimeSpan Duration { get; }
        }
        #endregion

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }


    /// <summary>
    /// Observable Stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableStack<T> : Stack<T>, ICollection<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {

        public ObservableStack() : base() { }

        public ObservableStack(IEnumerable<T> collection) : base(collection) { }

        public ObservableStack(int capacity) : base(capacity) { }
        public bool IsReadOnly => throw new NotImplementedException();

        public new virtual T Pop()
        {
            T item = base.Pop();
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            return item;
        }

        public new virtual void Push(T item)
        {
            base.Push(item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        public new virtual void Clear()
        {
            base.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset, default);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, item == null ? -1 : 0));
            OnPropertyChanged(nameof(Count));
        }

        [Obsolete("don't use this")]
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        [Obsolete("don't use this")]
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
    }


    public class ObservablaQueue<T> : Queue<T>, ICollection<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public ObservablaQueue() : base() { }

        public ObservablaQueue(IEnumerable<T> collection) : base(collection) { }

        public ObservablaQueue(int capacity) : base(capacity) { }

        public bool IsReadOnly => throw new NotImplementedException();

        public new virtual T Dequeue()
        {
            T item = base.Dequeue();
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            return item;
        }

        public new virtual void Enqueue(T item)
        {
            base.Enqueue(item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        public new virtual void Clear()
        {
            base.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset, default);
        }


        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, item == null ? -1 : 0));
            OnPropertyChanged(nameof(Count));
        }

        [Obsolete("don't use this")]
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        [Obsolete("don't use this")]
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
    }

    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        //private IDictionary<TKey, TValue> dictionary;

        //public ObservableDictionary() : this(new Dictionary<TKey, TValue>())
        //{

        //}

        public ObservableDictionary() : base() { }

        //public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        //{
        //    //this.dictionary = dictionary;
        //}

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Used by Add & Remove
        /// </summary>
        /// <param name="action"></param>
        /// <param name="newItem"></param>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, base.Keys.ToList().IndexOf(newItem.Key)));

            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
        }

        /// <summary>
        /// Used by Update
        /// </summary>
        /// <param name="action"></param>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, base.Keys.ToList().IndexOf(newItem.Key)));

            OnPropertyChanged(nameof(Values));
            OnPropertyChanged("Item[]");
        }

        /// <summary>
        /// Used By Clear
        /// </summary>
        /// <param name="action"></param>
        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action));

            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
        }

        public new bool ContainsKey(TKey key)
        {
            return base.ContainsKey(key);
        }

        public new ICollection<TKey> Keys => base.Keys;

        public new ICollection<TValue> Values => base.Values;

        public new TValue this[TKey key]
        {
            get => ContainsKey(key) ? base[key] : default;
            //get => base[key];
            set => Update(key, value);
        }

        //public new void Add(TKey key, TValue value)
        //{
        //    KeyValuePair<TKey, TValue> item = new(key, value);
        //    dictionary.Add(item);
        //    OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        //    //OnPropertyChanged("Item[]");
        //}
        public new void Add(TKey key, TValue value)
        {
            //if (!base.ContainsKey(key))
            KeyValuePair<TKey, TValue> item = new(key, value);
            base.Add(key, value);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
            OnPropertyChanged(nameof(Count));
        }

        public new bool Remove(TKey key)
        {
            TValue value;

            if (base.TryGetValue(key, out value) && base.Remove(key))
            {
                KeyValuePair<TKey, TValue> item = new(key, value);
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public new void Clear()
        {
            base.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        private void Update(TKey key, TValue value)
        {
            if (base.TryGetValue(key, out TValue existing))
            {
                base[key] = value;
                //OnCollectionChanged(NotifyCollectionChangedAction.Replace, key, value, existing);
                KeyValuePair<TKey, TValue> newItem = new(key, value);
                KeyValuePair<TKey, TValue> oldItem = new(key, existing);
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, newItem, oldItem);
            }
            else
            {
                Add(key, value);
            }
        }

        public new bool TryGetValue(TKey key, out TValue value)
        {
            return base.TryGetValue(key, out value);
        }

        public new int Count => base.Count;
    }
}
