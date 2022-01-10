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
    public class ApexDefect : INotifyPropertyChanged
    {
        // private bool _started;
        private int _currentStep = -1;
        private StatusType _status;
        private DateTime _startTime;

        public ApexDefect()
        {
            _startTime = DateTime.Now;
        }


        /// <summary>
        /// 檢驗步驟，
        /// -1 時方塊圖全暗
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

            switch(status)
            {

            }

            return new SolidColorBrush(Colors.Red);
            //throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
