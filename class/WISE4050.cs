using System;
using MCAJawIns.Driver;

namespace MCAJawIns
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

                // 確認數值有變更
                if (data[bytes - 1] != Value)
                {
                    byte nV = data[bytes - 1];
                    byte oV = Value;
                    // 防彈跳，先變更舊值
                    Value = data[bytes - 1];   
                    OnIOChanged(nV, oV);
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
           private readonly byte _changedDI;

#if false
            #region MyRegion
            /// <summary>
            /// DI0 狀態
            /// </summary>
            public bool DI0 => (NewValue & 0b01) == 0b01;
            /// <summary>
            /// DI 狀態變更
            /// </summary>
            public bool DI0Changed => (_changedDI & 0b01) == 0b01;
            /// <summary>
            /// DI1 狀態
            /// </summary>
            public bool DI1 => ((NewValue >> 1) & 0b01) == 0b01;
            public bool DI1Changed => ((_changedDI >> 1) & 0b01) == 0b01;
            /// <summary>
            /// DI2 狀態
            /// </summary>
            public bool DI2 => ((NewValue >> 2) & 0b01) == 0b01;
            public bool DI2Changed => ((_changedDI >> 2) & 0b01) == 0b01;
            /// <summary>
            /// DI3 狀態
            /// </summary>
            public bool DI3 => ((NewValue >> 3) & 0b01) == 0b01;
            public bool DI3Changed => ((_changedDI >> 3) & 0b01) == 0b01;
            #endregion  
#endif

            #region Raise & Fall
            /// <summary>
            /// DI0 上升
            /// </summary>
            public bool DI0Raising => (NewValue & 0b01) == 0b01 && (_changedDI & 0b01) == 0b01;
            /// <summary>
            /// DI0 下降
            /// </summary>
            public bool DI0Falling => (NewValue & 0b01) == 0b00 && (_changedDI & 0b01) == 0b01;
            /// <summary>
            /// DI1 上升
            /// </summary>
            public bool DI1Raising => ((NewValue >> 1) & 0b01) == 0b01 && ((_changedDI >> 1) & 0b01) == 0b01;
            /// <summary>
            /// DI1 下降
            /// </summary>
            public bool DI1Falling => ((NewValue >> 1) & 0b01) == 0b00 && ((_changedDI >> 1) & 0b01) == 0b01;
            /// <summary>
            /// DI2 上升
            /// </summary>
            public bool DI2Raising => ((NewValue >> 2) & 0b01) == 0b01 && ((_changedDI >> 2) & 0b01) == 0b01;
            /// <summary>
            /// DI2 下降
            /// </summary>
            public bool DI2Falling => ((NewValue >> 2) & 0b01) == 0b00 && ((_changedDI >> 2) & 0b01) == 0b01;
            /// <summary>
            /// DI3 上升
            /// </summary>
            public bool DI3Raising => ((NewValue >> 3) & 0b01) == 0b01 && ((_changedDI >> 3) & 0b01) == 0b01;
            /// <summary>
            /// DI3 下降
            /// </summary>
            public bool DI3Falling => ((NewValue >> 3) & 0b01) == 0b00 && ((_changedDI >> 3) & 0b01) == 0b01;
            #endregion


            public byte NewValue { get; }

            public byte OldValue { get; }

            public IOChangedEventArgs(byte newValue, byte oldValue)
            {
                _changedDI = (byte)(newValue ^ oldValue);
                NewValue = newValue;
                OldValue = oldValue;
            }
        }

        protected void OnIOChanged(byte nValue, byte oValue)
        {
            IOChanged?.Invoke(this, new IOChangedEventArgs(nValue, oValue));
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
