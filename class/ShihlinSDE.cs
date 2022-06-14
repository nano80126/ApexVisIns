using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using ApexVisIns.Driver;

namespace ApexVisIns
{
    public class ShihlinSDE : SerialPortBase
    {
        #region Varibles
        private int _motorNumber;


        #endregion


        #region Properties
        public int MotorNumber
        {
            get => _motorNumber;
            set
            {
                if (value != _motorNumber)
                {
                    _motorNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public ShihlinSDE()
        {


        }

        public ShihlinSDE(int motors)
        {
            MotorNumber = motors;
        }

        /// <summary>
        /// 數位輸入
        /// </summary>
        public ObservableCollection<IOChannel> DIs = new();

        /// <summary>
        /// 數位輸出
        /// </summary>
        public ObservableCollection<IOChannel> DOs = new();


        #region Private Methods



        private void ReadIO()
        {

        }

        #endregion


        /// <summary>
        /// 寫入命令
        /// </summary>
        /// <param name="data"></param>
        protected override void Write(byte[] data)
        {
            base.Write(data);
        }

        /// <summary>
        /// 讀取回傳
        /// </summary>
        /// <returns></returns>
        protected byte[] Read()
        {
            byte[] buffer = new byte[16];

            int count = _serialPort.Read(buffer, 0, buffer.Length);
            Array.Resize(ref buffer, count);

            return buffer;
        }








        public class IOChannel
        {
            // string Name => get

            public string Name => Input ? $"DI{Number}" : $"DO{Number}";

            public bool Input { get; set; }

            public int Number { get; set; }

            public string Function { get; set; }

            public bool On { get; set; }

            #region PropertyChanged
            //public event PropertyChangedEventHandler PropertyChanged;
            //private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            //{
            //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //} 
            #endregion
        }
    }


}


