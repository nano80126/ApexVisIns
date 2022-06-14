using LockPlate.Driver;
using System;

namespace LockPlate
{
    public class WISE4050 : TCPIPBase
    {
        #region private fields
        //private bool _disposed;

        //private TcpClient _tcpClient;
        //private NetworkStream _networkStream;

        //private Timer _pollingTimer;
        //private double _interval = 100;
        private byte _value;
        #endregion

        public WISE4050() { }

        public WISE4050(string iP, int port, double interval)
        {
            IP = iP;
            Port = port;
            Interval = interval;
        }

        #region Properties

        public byte Value
        {
            get => _value;
            private set
            {
                _value = value;
            }
        }
        #endregion

        /// <summary>
        /// 輪詢 Timer Elapsed 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ReadIO();
        }

        /// <summary>
        /// 讀取 IO
        /// </summary>
        private void ReadIO()
        {
            byte[] msg = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x01, 0x02, 0x00, 0x00, 0x00, 0x04 }; // 站號 1；讀取 Coil；0x00 0x00 開始；長度 0x04；

            if (_networkStream.CanWrite)
            {
                _networkStream.Write(msg, 0, msg.Length);

                byte[] data = new byte[16];
                int bytes = _networkStream.Read(data, 0, data.Length);

                if (data[bytes - 1] != Value)
                {
                    Value = data[bytes - 1];
                    OnIOChanged(Value);
                }
            }
            else
            {
                throw new WISE4050Exception("Network Stream 無法寫入");
            }
        }

        public delegate void IOChangedEventHandler(object sender, IOChangedEventArgs e);

        public event IOChangedEventHandler IOChanged;

        public class IOChangedEventArgs : EventArgs
        {
            public bool DI0 => (Value & 0b01) == 0b01;
            public bool DI1 => ((Value >> 1) & 0b01) == 0b01;
            public bool DI2 => ((Value >> 2) & 0b01) == 0b01;
            public bool DI3 => ((Value >> 3) & 0b01) == 0b01;

            public byte Value { get; }
            public IOChangedEventArgs(byte value)
            {
                Value = value;
            }
        }

        protected void OnIOChanged(byte value)
        {
            IOChanged?.Invoke(this, new IOChangedEventArgs(value));
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
