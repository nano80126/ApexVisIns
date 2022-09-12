using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MCAJawIns.Product
{
    /// <summary>
    /// MCA Jaw 檢驗結果輸出
    /// </summary>
    public class JawSpec : SpecBase
    {
        private double _result;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item">項目</param>
        /// <param name="cl">規格中心</param>
        /// <param name="lcl">管制下限</param>
        /// <param name="ucl">管制上限</param>
        public JawSpec(string item, double cl, double lcl, double ucl)
        {
            Item = item;
            CenterSpec = cl;
            //LowerSpecLimit = lsl;
            //UpperSpecLimit = usl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item">項目</param>
        /// <param name="cl">規格中心</param>
        /// <param name="lcl">管制下限</param>
        /// <param name="ucl">管制上限</param>
        /// <param name="result">量測值</param>
        public JawSpec(string item, double cl, double lcl, double ucl, double result)
        {
            Item = item;
            CenterSpec = cl;
            // LowerSpecLimit = lsl;
            // UpperSpecLimit = usl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
            Result = result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item">項目</param>
        /// <param name="cl">規格中心</param>
        /// <param name="lsl">規格下限</param>
        /// <param name="usl">規格上限</param>
        /// <param name="lcl">管制下限</param>
        /// <param name="ucl">管制上限</param>
        public JawSpec(string item, double cl, double lsl, double usl, double lcl, double ucl)
        {
            Item = item;
            CenterSpec = cl;
            LowerSpecLimit = lsl;
            UpperSpecLimit = usl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
        }

        /// <summary>
        /// 檢測數值
        /// </summary>
        [Description("檢驗數值")]
        public double Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OK));
            }
        }

        /// <summary>
        /// 檢測結果
        /// </summary>
        [Description("檢驗結果")]
        public bool OK => (double.IsNaN(LowerCtrlLimit) && double.IsNaN(UpperCtrlLimit)) || (LowerCtrlLimit <= Result && Result <= UpperCtrlLimit);
    }

    /// <summary>
    /// MCA Jaw 尺寸規格設定，規格設定列表用
    /// </summary>
    public class JawSpecSetting : SpecBase
    {
        private string _note;
        private double _correction;
        private bool _enable;
        private double _correctionSecret;
        private SpecInsGroup _group = SpecInsGroup.NONE;

        public JawSpecSetting() { }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="enable">是否啟用</param>
        /// <param name="key">Dictionary Key</param>
        /// <param name="item">Item Name</param>
        /// <param name="cl">規格中心</param>
        /// <param name="lcl">管制下限</param>
        /// <param name="ucl">管制上限</param>
        /// <param name="correction">校正值 1</param>
        /// <param name="correction2">校正值 2 (開發者專用)</param>
        /// <param name="note">備註</param>
        public JawSpecSetting(int id, bool enable, string key, string item, double cl, double lcl, double ucl, double correction, double correction2 = 0, string note = null)
        {
            ID = id;
            Enable = enable;

            Key = key;
            Item = item;
            CenterSpec = cl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
            Correction = correction;
            CorrectionSecret = correction2;
            Note = note ?? string.Empty;
        }

        /// <summary>
        /// 檢驗群組，同一群組
        /// </summary>
        public enum SpecInsGroup {
            [Description("未分組")]
            NONE = 0,
            [Description("群組 1")]
            Group1 = 1,
            [Description("群組 2")]
            Group2 = 2,
            [Description("群組 3")]
            Group3 = 3,
            [Description("群組 4")]
            Group4 = 4,
            [Description("群組 5")]
            Group5 = 5
        };

        [Description("群組")]
        public SpecInsGroup Group
        {
            get => _group;
            set
            {
                if (value != _group)
                {
                    _group = value;
                    OnPropertyChanged();

                }
            }
        }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Description("啟用")]
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
        /// 校正值
        /// </summary>
        [Description("校正值")]
        public double Correction
        {
            get => _correction;
            set
            {
                if (value != _correction)
                {
                    _correction = value;
                    OnPropertyChanged();
                }
            }
        }

        [Description("校正值(開發)")]
        public double CorrectionSecret
        {
            get => _correctionSecret;
            set
            {
                if (value != _correctionSecret)
                {
                    _correctionSecret = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 備註
        /// </summary>
        [Description("備註")]
        public string Note
        {
            get => _note;
            set
            {
                if (value != _note)
                {
                    _note = value;
                    OnPropertyChanged();
                }
            }
        }
    }


    /// <summary>
    /// MCA Jaw 檢驗結果集合，即時顯示用
    /// </summary>
    public class JawResultGroup
    {
        #region Private
        private readonly object _c1lock = new();
        private readonly object _c2lock = new();
        private readonly object _c3lock = new();
        #endregion

        #region Public
        public bool SyncBinding { get; private set; }
        #endregion

        public void EnableCollectionBinding()
        {
            BindingOperations.EnableCollectionSynchronization(Collection1, _c1lock);
            BindingOperations.EnableCollectionSynchronization(Collection2, _c2lock);
            BindingOperations.EnableCollectionSynchronization(Collection3, _c3lock);
            SyncBinding = true;
        }

        public void DisableCollectionBinding()
        {
            BindingOperations.DisableCollectionSynchronization(Collection1);
            BindingOperations.DisableCollectionSynchronization(Collection2);
            BindingOperations.DisableCollectionSynchronization(Collection3);
            SyncBinding = false;
        }

        /// <summary>
        /// 尺寸規格列表
        /// </summary>
        /// [Obsolete("deprecated, use JawSizeSpecList instead")]
        /// public ObservableCollection<JawSpecSetting> SizeSpecList { get; set; } = new ObservableCollection<JawSpecSetting>();

#if false
        /// <summary>
        /// 規格集合
        /// </summary>
        public ObservableCollection<JawSpec> SpecCollection { get; set; } = new ObservableCollection<JawSpec>(); 
#endif

        /// <summary>
        /// 規格集合 1
        /// </summary>
        public ObservableCollection<JawSpec> Collection1 { get; set; } = new ObservableCollection<JawSpec>();
        /// <summary>
        /// 規格集合 2
        /// </summary>
        public ObservableCollection<JawSpec> Collection2 { get; set; } = new ObservableCollection<JawSpec>();
        /// <summary>
        /// 規格集合 3
        /// </summary>
        public ObservableCollection<JawSpec> Collection3 { get; set; } = new ObservableCollection<JawSpec>();

        /// <summary>
        /// 集合 1 結果
        /// </summary>
        public bool Col1Result => Collection1.All(item => item.OK);
        /// <summary>
        /// 集合 2 結果
        /// </summary>
        public bool Col2Result => Collection2.All(item => item.OK);
        /// <summary>
        /// 集合 3 結果
        /// </summary>
        public bool Col3Result => Collection3.All(item => item.OK);
    }

    public class JawSizeSpecList : INotifyPropertyChanged
    {
        #region private
        // private readonly object _srcLock = new object();
        private bool _saved;
        #endregion

        public JawSizeSpecList()
        {
            //BindingOperations.EnableCollectionSynchronization(Source, _srcLock);
            Source.CollectionChanged += Source_CollectionChanged;
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"act {e.Action}");
            //System.Diagnostics.Debug.WriteLine($"new {e.NewItems}");
            //System.Diagnostics.Debug.WriteLine($"old {e.OldItems}");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    JawSpecSetting newItem = e.NewItems[0] as JawSpecSetting;
                    newItem.PropertyChanged += SpecChange_PropertyChanged;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    JawSpecSetting oldItem = e.OldItems[0] as JawSpecSetting;
                    oldItem.PropertyChanged -= SpecChange_PropertyChanged;
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:

                default:
                    break;
            }
        }

        private void SpecChange_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Saved = false;
            //System.Diagnostics.Debug.WriteLine($"{e.PropertyName}");

            System.Diagnostics.Debug.WriteLine($"{e.PropertyName}");
        }

        public ObservableCollection<JawSpecSetting> Source { get; set; } = new ObservableCollection<JawSpecSetting>();

        /// <summary>
        /// Source 新增物件 (自動增加 ID)
        /// </summary>
        public void AddNew(JawSpecSetting item)
        {
            int id = Source.Count + 1;
            item.ID = id;
            //lock (_srcLock)
            //{
            Source.Add(item);
            //}
        }

        /// <summary>
        /// 是否已儲存
        /// </summary>
        public bool Saved
        {
            get => _saved;
            private set
            {
                if (value != _saved)
                {
                    _saved = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 列表儲存 (變更 Saved Flags)
        /// </summary>
        public void Save()
        {
            _saved = true;
            OnPropertyChanged(nameof(Saved));
        }

        #region Property Changed
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// MAC Jaw 檢驗主物件，
    /// 狀態、計數等功能，
    /// 存入 (Lots) 資料庫用
    /// </summary>
    public class JawInspection : INotifyPropertyChanged
    {
        #region private
        private string _lotNumber = string.Empty;
        private bool _lotNumberChecked;
        private bool _lotInserted;
        #endregion

        /// <summary>
        /// Mongo ID
        /// </summary>
        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 檢驗批號輸入
        /// </summary>
        [BsonElement(nameof(LotNumber))]
        public string LotNumber
        {
            get => _lotNumber;
            set
            {
                if (value != _lotNumber)
                {
                    _lotNumber = value;
                    _lotNumberChecked = false;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LotNumberChecked));
                }
            }
        }

        /// <summary>
        /// 批號是否已確認，用於確認該批號是否已確認，始可檢驗 (不會插入 MongoDB)
        /// </summary>
        [BsonIgnore]
        public bool LotNumberChecked
        {
            get => _lotNumberChecked;
            private set
            {
                if (value != _lotNumberChecked)
                {
                    _lotNumberChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 批號是否已確認，用於確認該批號是否已插入資料庫 (不會插入 MongoDB)
        /// </summary>
        [BsonIgnore]
        public bool LotInserted
        {
            get => _lotInserted;
            private set
            {
                if (value != _lotInserted)
                {
                    _lotInserted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 該批檢驗結果紀錄
        /// </summary>
        [BsonElement(nameof(LotResults))]
        public ObservableDictionary<string, ResultElement> LotResults { get; set; } = new ObservableDictionary<string, ResultElement>();

        /// <summary>
        /// 資料插入時間
        /// </summary>
        [BsonElement(nameof(DateTime))]
        public DateTime DateTime { get; set; }

        public void CheckLotNumber()
        {
            LotNumberChecked = true;
        }

        public void SetLotInserted(bool inserted)
        {
            LotInserted = inserted;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// MCA Jaw 尺寸 NG 數
        /// </summary>
        public class ResultElement : INotifyPropertyChanged
        {
            private int _count;

            public ResultElement() { }

            public ResultElement(string name, string note, int count, bool enable)
            {
                Name = name;
                Note = note;
                Count = count;
                Enable = enable;
            }

            /// <summary>
            /// 規格名稱
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 規格 Note
            /// </summary>
            [BsonIgnore]
            public string Note { get; set; }

            /// <summary>
            /// 檢驗數量
            /// </summary>
            public int Count
            {
                get => _count;
                set
                {
                    if (value != _count)
                    {
                        _count = value;
                        OnPropertyChanged();
                    }
                }
            }

            /// <summary>
            /// 是否啟用
            /// </summary>
            [BsonIgnore]
            public bool Enable { get; set; }

            #region Property Changed
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            } 
            #endregion
        }
    }

    /// <summary>
    /// Jaw 全尺寸量測結果物件物件，存入 (Measurements) 資料庫用
    /// </summary>
    public class JawMeasurements
    {
        public JawMeasurements()
        {
            Results = new Dictionary<string, double>();
        }

        public JawMeasurements(string lotNumber)
        {
            LotNumber = lotNumber;
            Results = new Dictionary<string, double>();
        }

        [BsonId]
        public ObjectId ObjID { get; set; }

        /// <summary>
        /// 批號
        /// </summary>
        [BsonElement(nameof(LotNumber))]
        public string LotNumber { get; set; } = string.Empty;

        /// <summary>
        /// 各尺寸檢驗結果
        /// </summary>
        [BsonElement(nameof(Results))]
        public Dictionary<string, double> Results { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 是否良品
        /// </summary>
        [BsonElement(nameof(OK))]
        public bool OK { get; set; } = false;

        /// <summary>
        /// 檢驗完成時間
        /// </summary>
        [BsonElement(nameof(DateTime))]
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
