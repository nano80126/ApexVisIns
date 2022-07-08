using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

namespace MCAJawIns.Product
{
    public class ApexDefect : INotifyPropertyChanged, IDisposable
    {
        // private bool _started;
        private int _currentStep = -1;
        private bool _stepError;
        private StatusType _status;
        private DateTime _startTime;

        private System.Timers.Timer _timeUpdater;
        private bool _disposed;
        private bool _zeroReturning;
        private bool _zeroReturned;
        private bool _hardwarePrepared;

        public ApexDefect()
        {
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 檢驗步驟，
        /// -1 時方塊圖全暗 (-1 為程式剛啟動之預設狀態)
        /// </summary>
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (value != _currentStep)
                {
                    _currentStep = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StepError
        {
            get => _stepError;
            set
            {
                if (value != _stepError)
                {
                    _stepError = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 目前狀態
        /// </summary>
        public StatusType Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 硬體準備完成旗標
        /// </summary>
        public bool HardwarePrepared
        {
            get => _hardwarePrepared;
            set
            {
                if (value != _hardwarePrepared)
                {
                    _hardwarePrepared = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 原點復歸中 Flag
        /// </summary>
        public bool ZeroReturning
        {
            get => _zeroReturning;
            set
            {
                if (value != _zeroReturning)
                {
                    _zeroReturning = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 原點復歸完成 Flag
        /// </summary>
        public bool ZeroReturned
        {
            get => _zeroReturned;
            set
            {
                if (value != _zeroReturned)
                {
                    _zeroReturned = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 變更規格中 Flag
        /// </summary>
        public bool SpecChanging { get; set; }

        /// <summary>
        /// 變更規格完成 Flag
        /// </summary>
        public bool SpecChanged { get; set; }

        /// <summary>
        /// OK 數量，
        /// 檢驗完成 +1 
        /// </summary>
        public uint OK { get; private set; }

        /// <summary>
        /// NG 數量，
        /// 檢驗完成 +1
        /// </summary>
        public uint NG { get; private set; }

        /// <summary>
        /// 檢驗總數
        /// </summary>
        public uint TotalCount => OK + NG;

        /// <summary>
        /// 經過時間
        /// </summary>
        public string PassedTime
        {
            get
            {
                TimeSpan ts = DateTime.Now - _startTime;
                return ts.TotalSeconds > 0 ? $"{ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}" : "00:00:00";
            }
        }

        /// <summary>
        /// 檢驗開始
        /// </summary>
        public void Start()
        {
            _startTime = DateTime.Now;
            OnPropertyChanged(nameof(PassedTime));
            _timeUpdater = new System.Timers.Timer(1000)
            {
                AutoReset = true,
            };
            _timeUpdater.Elapsed += TimeUpdater_Elapsed;
            _timeUpdater.Start();
        }

        private void TimeUpdater_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            OnPropertyChanged(nameof(PassedTime));
        }

        /// <summary>
        /// 檢驗停止
        /// </summary>
        public void Stop()
        {
            _timeUpdater.Stop();
            OnPropertyChanged(nameof(PassedTime));
        }

        public void SetStep(int step)
        {
            CurrentStep = step;
        }

        /// <summary>
        /// OK 數量增加
        /// </summary>
        public void IncreaseOK()
        {
            OK++;
            OnPropertyChanged(nameof(OK));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 重置 OK 數量
        /// </summary>
        public void ResetOK()
        {
            OK = 0;
            OnPropertyChanged(nameof(OK));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// NG 數量增加
        /// </summary>
        public void IncreaseNG()
        {
            NG++;
            OnPropertyChanged(nameof(NG));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 重置 NG 數量
        /// </summary>
        public void ResetNG()
        {
            NG++;
            OnPropertyChanged(nameof(NG));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 運作狀態
        /// </summary>
        public enum StatusType
        {
            [Description("初始化")]
            Init = 0,       // 初始化階段
            [Description("原點復歸")]
            Returning = 1,
            [Description("準備完成")]
            Ready = 2,      // 準備完成
            [Description("移動中")]
            Moving = 3,
            [Description("閒置")]
            Idle = 4,       // 閒置 (Reset 後)
            [Description("檢驗中")]
            Running = 5,    // 運轉中
            [Description("等待")]
            Waiting = 6,    // 等待 (上下料)
            [Description("完成")]
            Finish = 7,     // 完成
            [Description("錯誤")]
            Error = 8,       // 報警
            [Description("暖機")]
            Warm = 9
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timeUpdater.Elapsed += TimeUpdater_Elapsed;
                _timeUpdater.Stop();
                _timeUpdater.Dispose();
                _timeUpdater = null;
            }

            _disposed = true;
        }
    }



    /// <summary>
    /// Apex 檢驗特徵
    /// </summary>
    public class ApexFeatures
    {
        public class DefectsBase
        {
            #region 耳朵瑕疵
            /// <summary>
            /// 耳朵內側毛邊
            /// </summary>
            public bool EarInnerBurr { get; set; }
            /// <summary>
            /// 耳朵外側毛邊
            /// </summary>
            public bool EarOuterBurr { get; set; }
            /// <summary>
            /// 耳朵孔毛邊
            /// </summary>
            public bool EarHoleBurr { get; set; }
            #endregion

            #region 窗戶瑕疵
            /// <summary>
            /// 窗戶毛邊
            /// </summary>
            public bool WindowBurr { get; set; }
            /// <summary>
            /// 窗戶撞傷
            /// </summary>
            public bool WindowBump { get; set; }
            #endregion

            #region 表面瑕疵
            /// <summary>
            /// 殘留
            /// </summary>
            public bool Residual { get; set; }
            /// <summary>
            /// 刮傷
            /// </summary>
            public bool Scraches { get; set; }
            /// <summary>
            /// 亮紋
            /// </summary>
            public bool Brights { get; set; }
            /// <summary>
            /// 表面撞傷
            /// </summary>
            public bool Bump { get; set; }
            /// <summary>
            /// 不均
            /// </summary>
            public bool Uneven { get; set; }
            /// <summary>
            /// 車刀紋
            /// </summary>
            public bool LatheToolScraches { get; set; }
            #endregion

            #region 原材瑕疵
            /// <summary>
            /// 凹洞
            /// </summary>
            public bool RawTubePit { get; set; }
            /// <summary>
            /// 原材模痕
            /// </summary>
            public bool RawTubeScar { get; set; }
            #endregion
        }
        /// /// ///
        /// /// ///
        /// /// ///

        /// <summary>
        /// 瑕疵旗標
        /// </summary>
        public DefectsBase DefectFlags { get; set; }
        /// <summary>
        /// 開始時間
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 檢驗時間
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
        /// <summary>
        /// 完成時間
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 灰度值 (Camera1)
        /// </summary>
        public byte GrayScale1 { get; set; }
        /// <summary>
        /// 灰度值 (Camera2)
        /// </summary>
        public byte GrayScale2 { get; set; }

        /// <summary>
        /// 等高圖
        /// </summary>
        public OpenCvSharp.Mat FullMat { get; set; }

        ///// <summary>
        ///// 平均灰階陣列 1
        ///// </summary>
        public double[] MeanGrayArray1 { get; set; }

        /// <summary>
        /// 標準差陣列 1
        /// </summary>
        public double[] StdGrayArray1 { get; set; }
    }

    /// <summary>
    /// Apex Defect Testing Status to Color
    /// </summary>
    [ValueConversion(typeof(ApexDefect.StatusType), typeof(SolidColorBrush))]
    public class ApexStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ApexDefect.StatusType))
            {
                throw new ArgumentException("Value is not ApexDefect Status");
            }

            ApexDefect.StatusType status = (ApexDefect.StatusType)value;
            return status switch
            {
                ApexDefect.StatusType.Init => new SolidColorBrush(Color.FromArgb(0x88, 0x21, 0x96, 0xf3)),
                ApexDefect.StatusType.Returning => new SolidColorBrush(Color.FromArgb(0xff, 0x21, 0x96, 0xf3)),
                ApexDefect.StatusType.Ready => new SolidColorBrush(Color.FromArgb(0xbb, 0x4c, 0xaf, 0x50)),
                ApexDefect.StatusType.Moving => new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x96, 0x88)),
                ApexDefect.StatusType.Idle => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xc1, 0x07)),
                ApexDefect.StatusType.Running => new SolidColorBrush(Color.FromArgb(0xff, 0x4c, 0xaf, 0x50)),
                ApexDefect.StatusType.Waiting => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xc1, 0x07)),
                ApexDefect.StatusType.Finish => new SolidColorBrush(Color.FromArgb(0x88, 0x4c, 0xaf, 0x50)),
                ApexDefect.StatusType.Error => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x52, 0x52)),
                ApexDefect.StatusType.Warm => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x52, 0x52)),
                _ => new SolidColorBrush(Colors.Red),   // Default
            };
            //return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
