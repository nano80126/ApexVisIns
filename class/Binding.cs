using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;

namespace ApexVisIns
{
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    /// This file is for binding data of application (public bindings) 
    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// 
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private ImageSource _imgSrc;

        /// <summary>
        /// DEBUG TAB
        /// </summary>
        private int _onTabIndex = 2;

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
                    OnPropertyChanged(nameof(OnTabIndex));
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
        /// 主影像 Source
        /// </summary>
        public ImageSource ImageSource
        {
            get => _imgSrc;
            set
            {
                _imgSrc = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
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
        private int _posX;
        private int _posY;

        public Mat Image
        {
            get => _img;
            set
            {
                _img = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// R 像素
        /// </summary>
        public byte R => Image != null ? Image.At<Vec3b>(Y, X)[2] : (byte)0;
        /// <summary>
        /// G 像素
        /// </summary>
        public byte G => Image != null ? Image.At<Vec3b>(Y, X)[1] : (byte)0;
        /// <summary>
        /// B 像素
        /// </summary>
        public byte B => Image != null ? Image.At<Vec3b>(Y, X)[0] : (byte)0;
        
        public int X
        {
            get => _posX;
            //set
            //{
            //    if (value != _posX)
            //    {
            //        _posX = value;
            //        OnPropertyChanged(nameof(X));
            //    }
            //}
        }
        public int Y
        {
            get => _posY;
            //set
            //{
            //    if (value != _posY)
            //    {
            //        _posY = value;
            //        OnPropertyChanged(nameof(Y));
            //    }
            //}
        }

        public void SetPoint(int x, int y)
        {
            _posX = x;
            _posY = y;
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
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
        public void AssignPoint(double x, double y, OpenCvSharp.Point offset)
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
        public int NewError { get; private set; }

        public int ErrorCount => ErrSource.Count;


        /// <summary>
        /// 新增 Error
        /// </summary>
        /// <param name="msg">Message, 若 type 為 info, 自動改為 warning </param>
        public void AddError(Message msg)
        {
            if (msg.MsgType == Message.MessageType.Info)
            {
                msg.MsgType = Message.MessageType.Warning;
            }
            ErrSource.Push(msg);
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
        public void AddError(Message.MsgCode code, string description, Message.MessageType type)
        {
            ErrSource.Push(new Message
            {
                Code = code,
                Description = description,
                MsgType = type == Message.MessageType.Info ? Message.MessageType.Warning : type,
            });
            NewError++;
            OnPropertyChanged(nameof(NewError));
            OnPropertyChanged(nameof(ErrorCount));
        }

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
        public void AddInfo(Message msg)
        {
            msg.MsgType = Message.MessageType.Info;
            InfoSource.Push(msg);
            NewInfo++;
            OnPropertyChanged(nameof(NewInfo));
            OnPropertyChanged(nameof(InfoCount));
        }

        /// <summary>
        /// 新增 Information (type 為 info)
        /// </summary>
        /// <param name="code">Message Code</param>
        /// <param name="description">Description</param>
        public void AddInfo(Message.MsgCode code, string description)
        {
            InfoSource.Push(new Message
            {
                Code = code,
                Description = description,
                MsgType = Message.MessageType.Info
            });
            NewInfo++;
            OnPropertyChanged(nameof(NewInfo));
            OnPropertyChanged(nameof(InfoCount));
        }

        public void ClearInfo()
        {
            InfoSource.Clear();
            NewInfo = 0;
            OnPropertyChanged(nameof(NewInfo));
        }

        public void ResetInfoCount()
        {
            NewInfo = 0;
            OnPropertyChanged(nameof(NewInfo));
        }

        //public ObservableCollection<Message> MessageSource { get; set; } = new ObservableCollection<Message>();

        /// <summary>
        /// Stack Source of Message
        /// </summary>
        public ObserableStack<Message> InfoSource { get; set; } = new ObserableStack<Message>();

        public ObserableStack<Message> ErrSource { get; set; } = new ObserableStack<Message>();
        
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
                /// BFR Testing Error Code
                /// Start Failed or Records Error
                /// </summary>
                BFR,
                /// <summary>
                /// Camera Error Code
                /// </summary>
                C,
                /// <summary>
                /// 
                /// </summary>
                D,
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
                /// OpenCv Process Error Code
                /// </summary>
                OPENCV,
                /// <summary>
                /// OpenCvSharp Process Error Code
                /// </summary>
                OPENCVS,
                /// <summary>
                /// Info & Error for Plot
                /// </summary>
                CHART,
                /// <summary>
                /// IO Exception Code
                /// </summary>
                IO,
            }

            /// <summary>
            /// 
            /// </summary>
            public enum MessageType
            {
                Info = 0,
                Warning = 1,
                Error = 2,
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Observable Stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObserableStack<T> : Stack<T>, ICollection<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public ObserableStack() : base() { }

        public ObserableStack(IEnumerable<T> collection) : base(collection) { }

        public ObserableStack(int capacity) : base(capacity) { }

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

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, item == null ? -1 : 0));
            OnPropertyChanged(nameof(Count));
        }

        public bool IsReadOnly => throw new NotImplementedException();


        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
    }
}
