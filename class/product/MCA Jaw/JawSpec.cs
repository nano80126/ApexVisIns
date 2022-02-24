using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
            CenterLine = cl;
            LowerSpecLimit = lsl;
            UpperSpecLimit = usl;
            LowerCtrlLimit = lcl;
            UpperCtrlLimit = ucl;
        }

        /// <summary>
        /// 檢測數值
        /// </summary>
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
        public bool OK => LowerCtrlLimit <= Result && Result <= UpperCtrlLimit;
    }

    public class JawSpecGroup
    {
        /// <summary>
        /// 規格尺寸列表，從 Json file 載入
        /// </summary>
        public List<SpecBase> SpecList { get; set; } = new List<SpecBase>();

        /// <summary>
        /// 規格集合
        /// </summary>
        public ObservableCollection<JawSpec> SpecCollection { get; set; } = new ObservableCollection<JawSpec>();

        /// <summary>
        /// 規格集合 1
        /// </summary>
        public ObservableCollection<JawSpec> Collection1 { get; set; }
        /// <summary>
        /// 規格集合 2
        /// </summary>
        public ObservableCollection<JawSpec> Collection2 { get; set; }
        /// <summary>
        /// 規格集合 3
        /// </summary>
        public ObservableCollection<JawSpec> Collection3 { get; set; }

        /// <summary>
        /// 集合 1 結果
        /// </summary>
        public bool Col1Result { get; set; }
        /// <summary>
        /// 集合 2 結果
        /// </summary>
        public bool Col2Result { get; set; }
        /// <summary>
        /// 集合 3 結果
        /// </summary>
        public bool Col3Result { get; set; }
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
