using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexVisIns
{
    public class ServoMotion : INotifyPropertyChanged
    {
        private int _posCmd;
        private int _posFdbk;
        private string _currentStatus;


        public List<string> CardList = new List<string>();

        public int PosCommand
        {
            get => _posCmd;
            set
            {
                if (value != _posCmd)
                {
                    _posCmd = value;
                    OnPropertyChanged(nameof(PosCommand));
                }
            }
        }

        public int PosFeedback
        {
            get => _posFdbk;
            set
            {
                if (value != _posFdbk)
                {
                    _posFdbk = value;
                    OnPropertyChanged(nameof(PosFeedback));
                }
            }
        }

        public AxisSignal ServoReady { get; set; } = new AxisSignal("SRDY",false);

        public AxisSignal ServoAlm { get; set; } = new AxisSignal("ALM", false);

        public AxisSignal LMTP { get; set; } = new AxisSignal("LMT+", false);

        public AxisSignal LMTN { get; set; } = new AxisSignal("LMT-", false);

        public AxisSignal SVON { get; set; } = new AxisSignal("SVON", false);

        public AxisSignal EMG { get; set; } = new AxisSignal("EMG", false);

        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (value != _currentStatus)
                {
                    _currentStatus = value;
                    OnPropertyChanged(nameof(CurrentStatus));
                }
            }
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        public void ResetPos()
        {


        }

        /// <summary>
        /// 重置錯誤
        /// </summary>
        public void ResetError()
        {

        }


        /// <summary>
        /// 軸 IO 狀態
        /// </summary>
        public struct AxisSignal
        {
            public AxisSignal(string name, bool bitOn)
            {
                this.Name = name;
                this.BitOn = bitOn;
            }

            /// <summary>
            /// 信號名稱
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 信號 On / Off
            /// </summary>
            public bool BitOn { get; set; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
