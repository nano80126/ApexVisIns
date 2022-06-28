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
using LockPlate.Driver;

namespace LockPlate
{
    public class ShihlinSDE : SerialPortBase
    {
        #region Varibles
        private int _motorNumber;
        private bool _servoReady;

        #endregion


        #region private
        private Task _pollingTask;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        private Timer _pollingTimer;
        private int _cmdPos;
        private int _resPos;
        #endregion


        #region Private
        public enum DIFunctions : byte
        {
            NONE = 0x00,
            SON = 0x01,
            RES = 0x02,
            PC = 0x03,
            TL = 0x04,
            TL1 = 0x05,
            SP1 = 0x06,
            SP2 = 0x07,
            SP3 = 0x08,
            ST1_RS2 = 0x09,
            ST2_RS1 = 0x0A,
            ORGP = 0x0B,
            SHOM = 0x0C,
            CM1 = 0x0D,
            CM2 = 0x0E,
            CR = 0x0F,
            CDP = 0x10,
            LOP = 0x11,
            EMG = 0x12,
            POS1 = 0x13,
            POS2 = 0x14,
            POS3 = 0x15,
            CTRG = 0x16,
            LSP = 0x18,
            LSN = 0x19,
            POS4 = 0x1A,
            POS5 = 0x1B,
            POS6 = 0x1C,
            INHP = 0x1D,
            EV1 = 0x1E,
            EV2 = 0x1F,
            EV3 = 0x20,
            EV4 = 0x21,
            ABSE = 0x22,
            ABSC = 0x23,
            STOP = 0x24,
            MD1 = 0x28,
            MD2 = 0x29,
            MPD1 = 0x2A,
            MPD2 = 0x2B,
            SPS = 0x2C
        }

        public enum DOFunctions
        {
            NONE = 0x00,
            RD = 0x01,
            ALM = 0x02,
            INP_SA = 0x03,
            HOME = 0x04,
            TLC_VLC = 0x05,
            MBR = 0x06,
            WNG = 0x07,
            ZSP = 0x08,
            CMDOK = 0x09,
            OLW = 0x0A,
            MC_OK = 0x0B,
            OVF = 0x0C,
            SWPL = 0x0D,
            SWNL = 0x0E,
            ABSW = 0x0F,
            POS1 = 0x11,
            POS2 = 0x12,
            POS3 = 0x13,
            POS4 = 0x14,
            POS5 = 0x15,
            POS6 = 0x16,
        }
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

        public int BytesInBuf => _serialPort.BytesToRead;

        public bool ServoReady
        {
            get => _servoReady;
            set
            {
                if (value != _servoReady)
                {
                    _servoReady = value;
                    OnPropertyChanged();
                }
            }
        }


        public int CmdPos
        {
            get => _cmdPos;
            set
            {
                if (value != _cmdPos)
                {
                    _cmdPos = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ResPos
        {
            get => _resPos;
            set
            {
                if (value != _resPos)
                {
                    _resPos = value;
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
        public ObservableCollection<IOChannel> DIs { get; set; } = new();

        /// <summary>
        /// 數位輸出
        /// </summary>
        public ObservableCollection<IOChannel> DOs { get; set; } = new();


        #region Private Methods

        /// <summary>
        /// 讀取 SERVO ON/OFF, 控制模式
        /// </summary>
        public void ReadServo(byte station)
        {
            byte[] cmd = new byte[] { station, 0x03, 0x02, 0x00, 0x00, 0x02 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            Write(cmd);
            Debug.WriteLine($"data: {string.Join(",", cmd)}");
            Debug.WriteLine($"{_serialPort.BytesToRead}");
            _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + 2 * 0x02 + 2, 1000);
            Debug.WriteLine($"{_serialPort.BytesToRead}");

            byte[] response = Read();

            string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
            Debug.WriteLine($"ret: {string.Join(",", retStr)}");

            if (response[0] == 0x01 && response[1] == 0x03 && response[2] == 0x04)
            {
                Debug.WriteLine($"SERVO: {response[4]}, MODE: {response[6]}");
            }
        }

        /// <summary>
        /// 讀取 IO 設定，通常讀取一次
        /// </summary>
        /// <param name="station"></param>
        public void ReadIO(byte station)
        {
            DIs.Clear();
            DOs.Clear();

            byte[] cmd = new byte[] { station, 0x03, 0x02, 0x06, 0x00, 0x08 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            Write(cmd);
            Debug.WriteLine($"data: {string.Join(",", cmd)}");

            Debug.WriteLine($"{DateTime.Now:mm:ss.fff} {BytesInBuf}");
            // 站號、功能碼、長度(3 bytes) + 2 * 讀取長度 + CRC(2 bytes)
            _ = SpinWait.SpinUntil(() => BytesInBuf == 3 + (2 * 0x08) + 2, 1000);
            Debug.WriteLine($"{DateTime.Now:mm:ss.fff} {BytesInBuf}");

            byte[] response = Read();

            string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
            Debug.WriteLine($"ret: {string.Join(",", retStr)}");

            Debug.WriteLine($"{(response[0] << 8) + (response[1] << 4) + response[2]}");
            if ((response[0] << 8) + (response[1] << 4) + response[2] == ((station << 8) + 0b01000000))
            {
                for (int i = 3; i < response.Length - 6; i++)
                {
                    //Debug.WriteLine($"{i} {response[i]} {(DIFunctions)response[i]}");
                    DIs.Add(new IOChannel()
                    {
                        Function = ((DIFunctions)response[i]).ToString(),
                        Number = i - 2,
                        On = false,
                        Input = true
                    });
                }

                short[] DO_temp = new short[] {
                    (short)((response[^6] << 8) + response[^5]),
                    (short)((response[^4] << 8) + response[^3])
                };

                for (int i = 0; i < DO_temp.Length; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        DOs.Add(new IOChannel()
                        {
                            Function = $"{(DOFunctions)((DO_temp[i] >> (5 * j)) & 0b11111)}",
                            Number = (3 * i) + j + 1,
                            On = false,
                            Input = false
                        });
                    }
                }


                foreach (IOChannel item in DIs)
                {
                    Debug.WriteLine($"{item.Name} {item.Function}");
                }

                Debug.WriteLine($"-------------------------------");
                foreach (IOChannel item in DOs)
                {
                    Debug.WriteLine($"{item.Name} {item.Function}");
                }
            }
        }

        /// <summary>
        /// 讀取 IO 狀態
        /// </summary>
        /// <param name="station"></param>
        public void ReadIOStatus(byte station)
        {
            byte[] cmd = new byte[] { station, 0x03, 0x02, 0x04, 0x00, 0x02 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            Write(cmd);
            Debug.WriteLine($"data: {string.Join(",", cmd)}");

            _ = SpinWait.SpinUntil(() => BytesInBuf == 3 + (2 * 0x02) + 2, 1000);

            byte[] response = Read();

            string[] resStr = Array.ConvertAll(response, (a) => $"{a:X2}");
            Debug.WriteLine($"ret: {string.Join(",", resStr)}");

            if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110100)
            {
                short temp = (short)((response[3] << 8) + response[4]);

                Debug.WriteLine($"{temp}");
                for (int i = 0; i < DIs.Count; i++)
                {
                    DIs[i].On = ((temp >> i) & 0b01) == 0b01;
                }

                temp = (short)((response[5] << 8) + response[6]);

                Debug.WriteLine($"{temp}");
                for (int i = 0; i < DOs.Count; i++)
                {
                    DOs[i].On = ((temp >> i) & 0b01) == 0b01;
                }
            }
        }

        /// <summary>
        /// 讀取位置 (命令&回授)
        /// </summary>
        /// <param name="station"></param>
        public void ReadPos(byte station)
        {
            // 讀取 馬達迴授脈波數(電子齒輪比前)
            byte[] cmd = new byte[] { station, 0x03, 0x00, 0x02, 0x00, 0x02 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            Write(cmd);
            _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + (2 * 0x02) + 2, 1000);
            byte[] response = Read();


            if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110100)
            {
                int cmdpl = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                CmdPos = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
            }

            // 讀取 馬達迴授脈波數(電子齒輪比前)
            cmd = new byte[] { station, 0x03, 0x00, 0x24, 0x00, 0x02 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            Write(cmd);
            _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + (2 * 0x02) + 2, 1000);
            response = Read();

            if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110100)
            {
                int pospl = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                ResPos = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
            }
        }
        #endregion

        #region Public Methods
        public void EnableTask()
        {
            //Task.Factory.StartNew(() =>
            //{
            //    Debug.WriteLine("123");

            //}, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
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
            byte[] buffer = new byte[32];

            int count = _serialPort.Read(buffer, 0, buffer.Length);
            Array.Resize(ref buffer, count);

            return buffer;
        }

        /// <summary>
        /// IO 通道
        /// </summary>
        public class IOChannel : INotifyPropertyChanged 
        {
            #region Private
            private bool _on;
            #endregion

            public IOChannel() { }

            public IOChannel(bool input, int number, string function, bool on)
            {
                Input = input;
                Number = number;
                Function = function;
                On = on;
            }

            public string Name => Input ? $"DI{Number}" : $"DO{Number}";

            public bool Input { get; set; }

            public int Number { get; set; }

            public string Function { get; set; }

            public bool On {
                get => _on;
                set
                {
                    if (value != _on)
                    {
                        _on = value;
                        OnPropertyChanged();
                    }
                }
            }

            #region PropertyChanged
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }
    }


}


