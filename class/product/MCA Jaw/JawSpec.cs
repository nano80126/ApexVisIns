using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;


namespace ApexVisIns.Product
{
    public class JawSpec : SpecBase
    {
        private double _result;

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
        public bool OK => LowerCtrlLimit <= Result && Result <= UpperCtrlLimit;
    }


    public class JawSpecSetting : SpecBase
    {
        private string _note;
        private double _correction;
        private bool _enable;

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

    public class JawSpecGroup
    {

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
    public class JawInspection
    {
        /// <summary>
        /// 批號輸入
        /// </summary>
        public string LotNumber { get; set; }
    }
}
