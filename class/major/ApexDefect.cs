using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ApexVisIns
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
            [Description("閒置")]
            Idle = 1,       // 閒置
            [Description("準備")]
            Ready = 2,      // 準備完成
            [Description("檢驗中")]
            Running = 3,    // 運轉中
            [Description("等待")]
            Waiting = 4,    // 等待 (上下料)
            [Description("完成")]
            Finish = 5,     // 完成
            [Description("錯誤")]
            Error = 6       // 報警
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
    /// Apex Defect Testing Status to Color
    /// </summary>
    [ValueConversion(typeof(ApexDefect.StatusType), typeof(SolidColorBrush))]
    public class StatusColorConverter : IValueConverter
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
                ApexDefect.StatusType.Init => new SolidColorBrush(Color.FromArgb(0xff, 0x21, 0x96, 0xf3)),
                ApexDefect.StatusType.Idle => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xc1, 0x07)),
                ApexDefect.StatusType.Ready => new SolidColorBrush(Colors.Black),
                ApexDefect.StatusType.Running => new SolidColorBrush(Color.FromArgb(0xff, 0x4c, 0xaf, 0x50)),
                ApexDefect.StatusType.Waiting => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xc1, 0x07)),
                ApexDefect.StatusType.Finish => new SolidColorBrush(Color.FromArgb(0x88, 0x4c, 0xaf, 0x50)),
                ApexDefect.StatusType.Error => new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x52, 0x52)),
                _ => new SolidColorBrush(Colors.Red),
            };
            //return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
