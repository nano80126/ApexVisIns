using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace ApexVisIns
{
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    /// This file is for binding data of application (public bindings) 
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
#if DEBUG
        private readonly bool _debugMode = true;
#else
        private readonly bool _debugMode = false;
#endif

        private ImageSource _imgSrc;

        private readonly ImageSource[] _imgSrcArray = new ImageSource[4];

        private ImageSource _chartSource;

        private int _onTabIndex;
        private bool _loginFlag;

        /// <summary>
        /// Tab active index
        /// </summary>
        public int OnTabIndex
        {
            get => _onTabIndex;
            set
            {
                if (value != _onTabIndex)
                {
                    _onTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        //public double ZoomRatio
        //{
        //    //get => ImageViewbox.Width / ImageCanvas.Width * 100;
        //    //set
        //    //{
        //    //    int v = (int)Math.Floor(value);

        //    //    if (20 > v)
        //    //    {
        //    //        ImageViewbox.Width = 0.2 * ImageCanvas.Width;
        //    //    }
        //    //    else if (v > 200)
        //    //    {
        //    //        ImageViewbox.Width = 2 * ImageCanvas.Width;
        //    //    }
        //    //    else
        //    //    {
        //    //        double ratio = value / 100;
        //    //        ImageViewbox.Width = ratio * ImageCanvas.Width;
        //    //    }
        //    //    OnPropertyChanged(nameof(ZoomRatio));
        //    //}
        //}

        /// <summary>
        /// Debug 模式
        /// </summary>
        public bool DebugMode => _debugMode;

        #region Admin Password
        public string Password { get; } = "admin";

        /// <summary>
        /// 是否已登入
        /// </summary>
        public bool LoginFlag
        {
            get => _loginFlag;
            set
            {
                if (value != _loginFlag)
                {
                    _loginFlag = value;
                    OnPropertyChanged();
                }
            }
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
            SET_CAMERA_TRIGGER_MODE = 2
        }


        /// <summary>
        /// 檢驗狀態旗標
        /// </summary>
        public enum InsStatus
        {
            [Description("初始化")]
            INIT = 0,
            [Description("準備完成")]
            READY = 1,
            [Description("閒置")]
            IDLE = 2,
            [Description("檢驗中")]
            RUNNING = 3,
            [Description("完成")]
            DONE = 4,
            [Description("錯誤")]
            ERROR = 5,
        }
        #endregion


        /// <summary>
        /// 主影像 Source
        /// </summary>
        [Obsolete("轉移到 Indicator")]
        public ImageSource ImageSource
        {
            get => _imgSrc;
            set
            {
                _imgSrc = value;
                OnPropertyChanged();
            }
        }

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

        /// <summary>
        /// 主影像 Source
        /// </summary>
        public ImageSource ImageSource => _img?.ToImageSource();

        /// <summary>
        /// R 像素
        /// </summary>
        public byte R => _img != null ? _img.At<Vec3b>(Y, X)[2] : (byte)0;
        /// <summary>
        /// G 像素
        /// </summary>
        public byte G => _img != null ? _img.At<Vec3b>(Y, X)[1] : (byte)0;
        /// <summary>
        /// B 像素
        /// </summary>
        public byte B => _img != null ? _img.At<Vec3b>(Y, X)[0] : (byte)0;

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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        public OpenCvSharp.Rect GetRect()
        {
            return new OpenCvSharp.Rect((int)_x, (int)_y, (int)_width, (int)_height);
        }
        // // // // // //
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
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
        //private double _strokeThickness;

        //private bool _mouseDown;
        // //
        public double TempX { get; set; }    // Temp Pos of Mousedown
        public double TempY { get; set; }    // Temp Pos of Mousedown
        public double OftX { get; set; }     // Offset Pos of Mousedown
        public double OftY { get; set; }     // Offset Pos of Mousedown

        /// <summary>
        /// Is Mouse Down
        /// </summary>
        public bool MouseDown { get; set; }
        #endregion

        /// <summary>
        /// 是否開啟
        /// </summary>
        public bool Enable
        {
            get => _enable;
            set
            {
                if (value != _enable)
                {
                    _enable = value;
                    OnPropertyChanged("Enable");
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
    }


    /// <summary>
    /// 輔助用 Point
    /// </summary>
    public class AssistPoint : INotifyPropertyChanged
    {
        #region Basic Variable
        private double _x;
        private double _y;
        // //
        private SolidColorBrush _stroke = new(Colors.Black);
        private double _strokeThickness = 1;
        private bool _enable;
        #endregion

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
        /// Point 顏色
        /// </summary>
        public SolidColorBrush Stroke
        {
            get => _stroke;
            set
            {
                if (value != _stroke)
                {
                    _stroke = value;
                    OnPropertyChanged(nameof(Stroke));
                }
            }
        }

        public double StrokeThickness
        {
            get => _strokeThickness;
            set
            {
                if (value != _strokeThickness)
                {
                    _strokeThickness = value;
                    OnPropertyChanged(nameof(StrokeThickness));
                }
            }
        }

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
                    OnPropertyChanged(nameof(Enable));
                }
            }
        }

        /// <summary>
        /// 設置 point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AssignPoint(double x, double y)
        {
            _x = x;
            _y = y;
            OnPropertyChanged();
        }

        /// <summary>
        /// 設置 Point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="offset"></param>
        public void AssignPoint(double x, double y, Point offset)
        {
            _x = x + offset.X;
            _y = y + offset.Y;
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
        private int _targetProgressValue;

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
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// 目標 Progress Value
        /// </summary>
        public int TargetProgressValue
        {
            get => _targetProgressValue;
            set
            {
                if (value > _targetProgressValue)
                {
                    // 1 % = 100 ms
                    TimeSpan timeSpan = TimeSpan.FromMilliseconds((value - _targetProgressValue) * 45);
                    ProgressAnimation.Add(() =>
                    {
                        OnProgressValueChanged(_progress, value, timeSpan);
                        _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, timeSpan);
                        // 等待動畫結束後更新
                        ProgressValue = value;
                    });
                }
                // 即時更新
                _targetProgressValue = value;
                OnPropertyChanged();
            }
        }

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
        /// 新增 Information
        /// </summary>
        /// <param name="msg">Message, 強制 type 為 info</param>
        [Obsolete("待廢")]
        public void AddInfo(Message msg)
        {
            msg.MsgType = Message.MessageType.Info;
            InfoSource.Push(msg);
            NewInfo++;
            OnPropertyChanged(nameof(NewInfo));
            OnPropertyChanged(nameof(InfoCount));
        }

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
                /// OpenCv Process Error Code
                /// </summary>
                OPENCV,
                /// <summary>
                /// OpenCvSharp Process Error Code
                /// </summary>
                OPENCVS,
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
        private int _index;

        public ObservableDictionary() : base()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, TKey key, TValue value)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, new KeyValuePair<TKey, TValue>(key, value)));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Count));
        }

        public void Add(TKey key,TValue value)
        {
            this.Add(key, value);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, key, value);
        }


        public new KeyCollection Keys
        {
            get { return base.Keys; }
        }


        public new ValueCollection Values
        {
            get { return base.Values; }
        }


        public new int Count
        {
            get { return base.Count; }
        }

        //public new TValue this[TKey key]
        //{
        //get { return this.GetValue(); }
        //get { return this.GetValueOrDefault(key); }
        //set { this}
        //}
    }
}
