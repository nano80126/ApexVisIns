using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

namespace MCAJawIns.Product
{
    public interface ISpecBase
    {
        public string Item { get; set; }
        public double CenterSpec { get; set; }
        public double LowerSpecLimit { get; set; }
        public double UpperSpecLimit { get; set; }
        public double LowerCtrlLimit { get; set; }
        public double UpperCtrlLimit { get; set; }
    }

    public class SpecBase : ISpecBase, INotifyPropertyChanged
    {
        //private int _key;
        private double _cl;
        private double _lsl;
        private double _usl;
        private double _lcl;
        private double _ucl;


        /// <summary>
        /// ID (自動遞增)
        /// </summary>
        [Description("ID")]
        public int ID { get; set; }

        /// <summary>
        /// Key (主要用於資料庫)
        /// </summary>
        [Description("Key")]
        public string Key { get; set; }

        /// <summary>
        /// 主要用於 UI 顯示
        /// </summary>
        [Description("項目")]
        public string Item { get; set; }

        /// <summary>
        /// 規格中心
        /// </summary>
        [Description("規格中心")]
        public double CenterSpec
        {
            get => _cl;
            set
            {
                if (value != _cl)
                {
                    _cl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 規格下限
        /// </summary>
        [Description("規格下限")]
        public double LowerSpecLimit
        {
            get => _lsl;
            set
            {
                if (value != _lsl)
                {
                    _lsl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 規格上限
        /// </summary>
        [Description("規格上限")]
        public double UpperSpecLimit
        {
            get => _usl;
            set
            {
                if (value != _usl)
                {
                    _usl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 管制下限
        /// </summary>
        [Description("管制下限")]
        public double LowerCtrlLimit
        {
            get => _lcl;
            set
            {
                if (value != _lcl)
                {
                    _lcl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 管制上限
        /// </summary>
        [Description("管制上限")]
        public double UpperCtrlLimit
        {
            get => _ucl;
            set
            {
                if (value != _ucl)
                {
                    _ucl = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
