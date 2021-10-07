using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ApexVisIns.BFR
{
    /// <summary>
    /// 彎曲自由恢復試驗條件
    /// </summary>
    public class Trail : INotifyPropertyChanged
    {
        private bool _tempEnable;
        private bool _finished;
        private bool _unrestricted;

        public double Temperature { get; set; }

        public bool TemperatureEnable
        {
            get => _tempEnable;
            set
            {
                if (value != _tempEnable)
                {
                    _tempEnable = value;
                    OnPropertyChanged(nameof(TemperatureEnable));
                    OnPropertyChanged(nameof(Runnable));
                }
            }
        }

        public bool Unrestricted
        {
            get => _unrestricted;
            set
            {
                if (value != _unrestricted)
                {
                    _unrestricted = value;
                    OnPropertyChanged(nameof(Unrestricted));
                    OnPropertyChanged(nameof(Runnable));
                }
            }
        }

        public bool Running { get; private set; }

        public bool Finished => !Running && _finished;

        public bool Runnable => TemperatureEnable || Unrestricted;

        /// <summary>
        /// 開始時間
        /// </summary>
        public DateTime StartMoment { get; private set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        public DateTime EndMoment { get; private set; }

        public ObservableCollection<Record> ResultSource { get; } = new ObservableCollection<Record>();

        /// <summary>
        /// 開始檢測，清除舊有的 Records
        /// </summary>
        public void Start()
        {
            if (Runnable)
            {
                Running = true;
                _finished = false;
                StartMoment = DateTime.Now;
                ResultSource.Clear();
                OnPropertyChanged(nameof(Running));
                OnPropertyChanged(nameof(Finished));
            }
            else
            {
                throw new InvalidOperationException("Unable to start, check test conditions");
            }
        }

        /// <summary>
        /// 停止測試
        /// </summary>
        public void Stop()
        {
            Running = false;
            _finished = false;
            EndMoment = DateTime.Now;
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(Finished));
        }

        /// <summary>
        /// 完成測試 (達成條件後停止)
        /// </summary>
        public void End()
        {
            Running = false;
            _finished = true;
            EndMoment = DateTime.Now;
            OnPropertyChanged(nameof(Running));
            OnPropertyChanged(nameof(Finished));
        }

        public void AddRecord(int posX1, int posX2, double temperture)
        {
            ResultSource.Add(new(StartMoment)
            {
                ID = ResultSource.Count + 1, // 自動加算 ID
                PosX1 = posX1,
                PosX2 = posX2,
                Temperature = temperture
            });
        }

        public void AddRecordNewTemperature(int posX1, int posX2, double temperture)
        {
            if (ResultSource.Count == 0 || temperture > ResultSource.Last().Temperature)
            {
                ResultSource.Add(new(StartMoment)
                {
                    ID = ResultSource.Count + 1, // 自動加算 ID
                    PosX1 = posX1,
                    PosX2 = posX2,
                    Temperature = temperture
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Record
    {
        private readonly TimeSpan _timeSpan;
        private readonly DateTime _dateTime;

        public Record(DateTime startTime)
        {
            _timeSpan = DateTime.Now - startTime;
            _dateTime = DateTime.Now;
        }

        [Description("ID")]
        public int ID { get; set; }

        [Description("溫度")]
        public double Temperature { get; set; }

        /// <summary>
        /// 變形量 X1
        /// </summary>
        [Description("位置 X1")]
        public int PosX1 { get; set; }

        /// <summary>
        /// 變形量 X2
        /// </summary>
        [Description("位置 X2")]
        public int PosX2 { get; set; }

        /// <summary>
        /// 變形量 Avg
        /// </summary>
        [Description("平均 X")]
        public double AvgX => (PosX1 + PosX2) / 2.0;

        /// <summary>
        /// 經過時間
        /// </summary>
        [Description("經過時間")]
        public string Timespan => $"{_timeSpan.Hours:D2}:{_timeSpan.Minutes:D2}:{_timeSpan.Seconds:D2}";

        /// <summary>
        /// 紀錄時間
        /// </summary>
        [Description("紀錄時間")]
        public string Time => $"{_dateTime:HH:mm:ss}";
    }
}
