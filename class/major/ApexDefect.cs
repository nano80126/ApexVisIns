using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApexVisIns
{
    public class ApexDefect : INotifyPropertyChanged
    {
        // private bool _started;
        private int _step = -1;
        private StatusType _status;
        private uint _okCount;
        private uint _ngCount;
        private DateTime _startTime;


        public ApexDefect()
        {
            _startTime = DateTime.Now.AddHours(-36.26);
        }


        /// <summary>
        /// 檢驗步驟，
        /// -1 時方塊圖全暗
        /// </summary>
        public int Step
        {
            get => _step;
            set
            {
                if (value != _step)
                {
                    _step = value;
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
        public uint OK => _okCount;

        /// <summary>
        /// NG 數量，
        /// 檢驗完成 +1
        /// </summary>
        public uint NG => _ngCount;

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
                return $"{ts.TotalHours:F0}:{ts.Minutes}:{ts.Seconds}";
            }
        }

        /// <summary>
        /// OK 數量增加
        /// </summary>
        public void IncreaseOK()
        {
            _okCount++;
            OnPropertyChanged(nameof(OK));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 重置 OK 數量
        /// </summary>
        public void ResetOK()
        {
            _okCount = 0;
            OnPropertyChanged(nameof(OK));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// NG 數量增加
        /// </summary>
        public void IncreaseNG()
        {
            _ngCount++;
            OnPropertyChanged(nameof(NG));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 重置 NG 數量
        /// </summary>
        public void ResetNG()
        {
            _ngCount++;
            OnPropertyChanged(nameof(NG));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 運作狀態
        /// </summary>
        public enum StatusType
        {
            Init = 0,       // 初始化階段
            Idle = 1,       // 閒置
            Ready = 2,      // 準備完成
            Running = 3,    // 運轉中
            Waiting = 4,    // 等待 (上下料)
            Finish = 5,     // 完成
            Error = 6       // 報警
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
