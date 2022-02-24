using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

namespace ApexVisIns.Product
{
    public interface ISpecBase
    {
        public string Item { get; set; }
        public double CenterLine { get; set; }
        public double LowerSpecLimit { get; set; }
        public double UpperSpecLimit { get; set; }
        public double LowerCtrlLimit { get; set; }
        public double UpperCtrlLimit { get; set; }
    }

    public class SpecBase : ISpecBase, INotifyPropertyChanged
    {
        private double _cl;
        private double _lsl;
        private double _usl;
        private double _lcl;
        private double _ucl;

        public string Item { get; set; }

        /// <summary>
        /// 規格中心
        /// </summary>
        public double CenterLine
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
