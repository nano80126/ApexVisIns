using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

namespace ApexVisIns.Product
{

    /// <summary>
    /// MCA Jaw 規格輸出
    /// </summary>
    public class JawSpec : SpecBase
    {
        private double _result;

        public JawSpec(string item, double cl, double lcl, double ucl)
        {
            Item = item;
            CenterSpec = cl;
            //LowerSpecLimit = lsl;
            //UpperSpecLimit = usl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
        }

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
    /// MCA Jaw 規格設定
    /// </summary>
    public class JawSpecSetting : SpecBase
    {
        private string _note;
        private double _correction;
        private bool _enable;

        public JawSpecSetting() { }

        public JawSpecSetting(int key, bool enable, string item, double centerSpec, double lowerCtrlLimit, double upperCtrlLimit, double correction, string note = null)
        {
            Key = key;
            Enable = enable;

            Item = item;
            CenterSpec = centerSpec;
            LowerCtrlLimit = lowerCtrlLimit;
            UpperCtrlLimit = upperCtrlLimit;
            Correction = correction;
            Note = note ?? string.Empty;
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
    /// MCA Jaw 規格群組
    /// </summary>
    public class JawSpecGroup
    {
        #region Private
        private readonly object _c1lock = new();
        private readonly object _c2lock = new();
        private readonly object _c3lock = new();
        //private readonly object _lock = new();
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
        public ObservableCollection<JawSpecSetting> SpecList { get; set; } = new ObservableCollection<JawSpecSetting>();

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


    /// <summary>
    /// MAC Jaw 檢驗主物件，
    /// 狀態、計數等功能
    /// </summary>
    public class JawInspection : INotifyPropertyChanged
    {
        #region private
        private string _lotNumber;
        #endregion

        /// <summary>
        /// 批號輸入
        /// </summary>
        public string LotNumber
        {
            get => _lotNumber;
            set
            {
                if (value != _lotNumber)
                {
                    _lotNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableDictionary<string, ResultElement> LotResult { get; } = new ObservableDictionary<string, ResultElement>();

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

            public ResultElement(string name, string note, int count)
            {
                Name = name;
                Note = note;
                Count = count;
            }

            public string Name { get; set; }
            public string Note { get; set; }
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

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
