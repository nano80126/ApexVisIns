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


        protected override void Write(byte[] data)
        {
            base.Write(data);
        }


        protected byte[] Read()
        {
            byte[] buffer = new byte[16];

            int count = _serialPort.Read(buffer, 0, buffer.Length);
            Array.Resize(ref buffer, count);

            return buffer;
        }

    }
}


