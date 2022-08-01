using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
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


        private int _cmdPos;
        private int _resPos;
        private int _jogRPM = 200;
        private int _jogAccDecTime = 1000;

        private int _posMoveRPM = 200;
        private int _posMoveAccDecTime = 1000;
        private int _posMovePulse = 4194304; // 22 位元

        private bool _canTest = false;
        private AlarmCode _alarmCode = AlarmCode.NONE;      // 0xff : 無警報
        #endregion


        #region private
        private Task _pollingTask;
        private List<Action> _pollingAct = new List<Action>();
        private CancellationTokenSource _cancellationTokenSource;
        
        private Timer _pollingTimer;
        #endregion

        #region enum
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

        public enum DOFunctions : byte
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

        public enum AlarmCode
        {
            [Description("過電壓")]
            AL01 = 0x01,
            [Description("低電壓")]
            AL02 = 0x02,
            [Description("過電流")]
            AL03 = 0x03,
            [Description("回生異常")]
            AL04 = 0x04,
            [Description("過負載 1")]
            AL05 = 0x05,
            [Description("過速度")]
            AL06 = 0x06,
            [Description("異常脈波控制命令")]
            AL07 = 0x07,
            [Description("位置控制誤差過大")]
            AL08 = 0x08,
            [Description("串列通訊異常")]
            AL09 = 0x09,
            [Description("串列通訊逾時")]
            AL0A = 0x0A,
            [Description("位置檢出器異常1")]
            AL0B = 0x0B,
            [Description("風扇異常")]
            AL0D = 0x0D,
            [Description("IGBT異常")]
            AL0E = 0x0E,
            [Description("記憶體異常")]
            AL0F = 0x0F,
            [Description("過負載2")]
            AL10 = 0x10,
            [Description("馬達匹配異常")]
            AL11 = 0x11,
            [Description("馬達碰撞錯誤")]
            AL20 = 0x20,
            [Description("馬達UVW斷線")]
            AL21 = 0x21,
            [Description("編碼器通訊異常")]
            AL22 = 0x22,
            [Description("馬達編碼器種類錯誤")]
            AL24 = 0x24,
            [Description("位置檢出器異常3")]
            AL26 = 0x26,
            [Description("位置檢出器異常4")]
            AL27 = 0x27,
            [Description("位置檢出器過熱")]
            AL28 = 0x28,
            [Description("位置檢出器異常5(溢位)")]
            AL29 = 0x29,
            [Description("絕對型編碼器異常1")]
            AL2A = 0x2A,
            [Description("絕對型編碼器異常2")]
            AL2B = 0x2B,
            [Description("控制迴路異常")]
            AL2E = 0x2E,
            [Description("回升能量異常")]
            AL2F = 0x2F,
            [Description("脈波輸出檢出器頻率過高")]
            AL30 = 0x30,
            [Description("過電流2")]
            AL31 = 0x31,
            [Description("控制迴路異常2")]
            AL32 = 0x32,
            [Description("記憶體異常2")]
            AL33 = 0x33,
            [Description("過負載4")]
            AL34 = 0x34,
            [Description("緊急停止")]
            AL12 = 0x12,
            [Description("正反轉極限異常")]
            AL13 = 0x13,
            [Description("軟體正向極限")]
            AL14 = 0x14,
            [Description("軟體負向極限")]
            AL15 = 0x15,
            [Description("預先過負載警告")]
            AL16 = 0x16,
            [Description("ABS逾時警告")]
            AL17 = 0x17,
            [Description("預備")]
            AL18 = 0x18,
            [Description("Pr命令異常")]
            AL19 = 0x19,
            [Description("分度座標未定義")]
            AL1A = 0x1A,
            [Description("位置偏移警告")]
            AL1B = 0x1B,
            [Description("來源參數群組超出範圍")]
            AL61 = 0x61,
            [Description("預先過負載4")]
            AL1C = 0x1C,
            [Description("絕對型編碼器異常3")]
            AL2C = 0x2C,
            [Description("編碼器電池低電壓")]
            AL2D = 0x2D,
            [Description("來源參數編號超出範圍")]
            AL62 = 0x62,
            [Description("PR程序寫入參數超出範圍")]
            AL63 = 0x63,
            [Description("PR程序寫入參數錯誤")]
            AL64 = 0x64,
            [Description("無異常")]
            NONE = 0xFF
        }

        public enum PollingType : byte
        {
            CAN_TEST = 0,   // 讀取是否啟動測試     0x0900
            Alarm = 1,      // 讀取警報　　　　     0x0100
            IO = 2,         // 讀取ＩＯ　　　　     0x0204 0x0205　
            Position = 3,   // 讀取位置　　　　     0x0002 0x0024
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

        /// <summary>
        /// 馬達是否開啟
        /// </summary>
        public bool IsOpen
        {
            get => _serialPort?.IsOpen == true;
        }

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

        /// <summary>
        /// 命令位置
        /// </summary>
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

        /// <summary>
        /// 回授位置
        /// </summary>
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

        /// <summary>
        /// 是否可以啟動測試模式
        /// </summary>
        public bool CanTest
        {
            get => _canTest;
            set
            {
                if (value != _canTest)
                {
                    _canTest = value;
                    OnPropertyChanged();
                }
            }
        }

        public AlarmCode Alarm
        {
            get => _alarmCode;
            set
            {
                if (value != _alarmCode)
                {
                    _alarmCode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AlarmString));
                }
            }
        }

        public string AlarmString
        {
            get
            {
                FieldInfo info = _alarmCode.GetType().GetField(_alarmCode.ToString());

                DescriptionAttribute[] attributes = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);
                return attributes.Length > 0 ? $"{_alarmCode}. {attributes[0].Description}" : $"{_alarmCode}";
            }
        }

        /// <summary>
        /// Jog 模式 轉速
        /// </summary>
        public int JogRPM
        {
            get => _jogRPM;
            set
            {
                if (value != _jogRPM)
                {
                    _jogRPM = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Jog 模式 加減速時間
        /// </summary>
        public int JogAccDecTime
        {
            get => _jogAccDecTime;
            set
            {
                if (value != _jogAccDecTime)
                {
                    _jogAccDecTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 定位移動 轉速
        /// </summary>
        public int PosMoveRPM
        {
            get => _posMoveRPM;
            set
            {
                if (value != _posMoveRPM)
                {
                    _posMoveRPM = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///定位移動 加減速 時間
        /// </summary>
        public int PosMoveAccDecTime
        {
            get => _posMoveAccDecTime;
            set
            {
                if (value !=  _posMoveAccDecTime)
                {
                    _posMoveAccDecTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 定位移動命令 pulse (不受設定之電子齒輪比影響)
        /// </summary>
        public int PosMovePulse 
        {
            get => _posMovePulse;
            set
            {
                if (value != _posMovePulse)
                {
                    _posMovePulse = value;
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


        #region Public Methods
        public void ChangeTimeout(int ms)
        {
            Timeout = ms;
            _serialPort.ReadTimeout = ms;
        }

        /// <summary>
        /// 讀取 SERVO ON/OFF, 控制模式
        /// </summary>
        public void ReadServo(byte station)
        {
            byte[] cmd = new byte[] { station, 0x03, 0x02, 0x00, 0x00, 0x02 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            if (Write(cmd))
            {
                //Debug.WriteLine($"data: {string.Join(",", cmd)}");
                //Debug.WriteLine($"{_serialPort.BytesToRead}");
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + (2 * 0x02) + 2, Timeout);        // 站號(1) + 功能碼(1) + 長度(1) + 資料(2 * 2) + CRC(2)
                //Debug.WriteLine($"{_serialPort.BytesToRead}");
                byte[] response = Read();

                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"ret: {string.Join(",", retStr)}");

                if (response[0] == 0x01 && response[1] == 0x03 && response[2] == 0x04)
                {
                    Debug.WriteLine($"SERVO: {response[4]}, MODE: {response[6]}");
                }
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
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

            if (Write(cmd))
            {
                _ = SpinWait.SpinUntil(() => BytesInBuf == 3 + (2 * 0x08) + 2, Timeout);    // 站號(1) + 功能碼(1) + 長度(1) + 資料(2 * 8) + CRC(2)
                byte[] response = Read();

                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"ret: {string.Join(",", retStr)}");

                //Debug.WriteLine($"{(response[0] << 8) + (response[1] << 4) + response[2]}");
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

#if false
                    foreach (IOChannel item in DIs)
                    {
                        Debug.WriteLine($"{item.Name} {item.Function}");
                    }

                    Debug.WriteLine($"-------------------------------");
                    foreach (IOChannel item in DOs)
                    {
                        Debug.WriteLine($"{item.Name} {item.Function}");
                    } 
#endif
                }
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
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

            if (Write(cmd))
            {
                Debug.WriteLine($"data: {string.Join(",", cmd)}");

                _ = SpinWait.SpinUntil(() => BytesInBuf == 3 + (2 * 0x02) + 2, Timeout);

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
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
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

            if (Write(cmd))
            {
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + (2 * 0x02) + 2, Timeout);
                byte[] response = Read();

                if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110100)
                {
                    int cmdpl = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                    CmdPos = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                }

                // 讀取 馬達迴授脈波數(電子齒輪比前)
                cmd = new byte[] { station, 0x03, 0x00, 0x24, 0x00, 0x02 };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                _ = Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 3 + (2 * 0x02) + 2, 1000);
                response = Read();

                if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110100)
                {
                    int pospl = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                    ResPos = (response[3] << 8) + response[4] + (response[5] << 24) + (response[6] << 16);
                }
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");

            }
        }

        [Obsolete("deprecated")]
        public Task<List<byte>> ListStations()
        {
            List<byte> stations = new List<byte>();

            Task.Run(() =>
            {
                for (byte i = 0; i < 0x10; i++)
                {
                    bool b = CheckStat(i);

                    if (CheckStat(i))
                    {
                        stations.Add(i);
                    }
                }
            }).Wait();

            return Task.FromResult(stations);
        }

        /// <summary>
        /// 確認馬達資訊
        /// </summary>
        /// <param name="station">站號</param>
        /// <returns></returns>
        public bool CheckStat(byte station)
        {
            // 讀取 馬達狀態
            byte[] cmd = new byte[] { station, 0x03, 0x09, 0x00, 0x00, 0x01 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            if (Write(cmd))
            {
                //Debug.WriteLine($"a: {DateTime.Now:ss.fff}");
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 7, Timeout);   // 站號(1) + 功能碼(1) + 長度(1) + 資料(2) + CRC(2)
                                                                          //Debug.WriteLine($"b: {DateTime.Now:ss.fff}");
                byte[] response = Read();

                //string[] cmdStr = Array.ConvertAll(cmd, (a) => $"{a:X2}");
                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                //Debug.WriteLine($"{string.Join(",", cmdStr)}");
                Debug.WriteLine($"{string.Join(",", retStr)}");

                if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110010)
                {
                    return (response[3] << 8) + response[4] == 0x0FF0;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
            }
        }

        public void ReadAlarm(byte station)
        {
            // 讀取 警報
            byte[] cmd = new byte[] { station, 0x03, 0x01, 0x00, 0x00, 0x01 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            if (Write(cmd))
            {
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 7, Timeout);     // 站號(1) + 功能碼(1) + 長度
                byte[] response = Read();

                //string[] cmdStr = Array.ConvertAll(cmd, (a) => $"{a:X2}");
                //string[] resStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                if ((response[0] << 8) + (response[1] << 4) + response[2] == (station << 8) + 0b00110010)
                {
                    Alarm = (AlarmCode)((response[3] << 4) + response[4]);
                }
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
            }
        }

        public void ResetAlarm(byte station)
        {

            // 0x0130 寫入 0x1EA5：清除目前異警
            byte[] cmd = new byte[] { station, 0x06, 0x01, 0x30, 0x1E, 0xA5 };
            cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

            if (Write(cmd))
            {
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);       // 站號(1) + 功能碼(1) + 起始位置(2) + 資料(2) + CRC(2)
                byte[] response = Read();

                //string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                //Debug.WriteLine($"清除目前異警: {string.Join(",", retStr)}");

                // 重新讀取一次
                ReadAlarm(station);
            }
            else
            {
                throw new ShihlinSDEException("通訊 SerialPort 為 null 或未開啟");
            }
        }

        /// <summary>
        /// 啟動 Polling 工作
        /// </summary>
        /// <param name="station">站號</param>
        public void EnablePollingTask(byte station, PollingType type = PollingType.CAN_TEST)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                _pollingTask = Task.Run(() =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        if (_pollingAct.Count > 0)
                        {
                            _pollingAct[0]();
                            _pollingAct.RemoveAt(0);
                            continue;
                        }

                        switch (type)
                        {
                            case PollingType.CAN_TEST:
                                CanTest = CheckStat(station);
                                break;
                            case PollingType.Alarm:
                                ReadAlarm(station);
                                break;
                            case PollingType.IO:
                                ReadIOStatus(station);
                                break;
                            case PollingType.Position:
                                ReadPos(station);
                                break;
                            default:
                                break;
                        }

                        _ = SpinWait.SpinUntil(() => _pollingAct.Count > 0 || _cancellationTokenSource.IsCancellationRequested, 250);
                    }
                }, _cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 停用 Polling 工作
        /// </summary>
        public void DisablePollingTask()
        {
            try
            {
                _cancellationTokenSource.Cancel();

                _pollingTask.Wait();
                _pollingTask.Dispose();
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region JOG
        /// <summary>
        /// 啟動 JOG
        /// </summary>
        /// <param name="station">站號</param>
        public void JogEnable(byte station)
        {
            try
            {
                // 0x0901 寫入 0x0003：啟動 Jog
                byte[] cmd = new byte[] { station, 0x06, 0x09, 0x01, 0x00, 0x03 };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);
                byte[] response = Read();

                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                Debug.WriteLine($"{string.Join(",", retStr)}");

                // 加減速時間 換算
                byte hTime = (byte)((JogAccDecTime >> 8) & 0xFF);
                byte lTime = (byte)(JogAccDecTime & 0xFF);

                Debug.WriteLine($"TIME {hTime} {lTime}");

                // RPM 換算
                byte hRPM = (byte)((JogRPM >> 8) & 0xFF);
                byte lRPM = (byte)(JogRPM & 0xFF);

                Debug.WriteLine($"RPM {hRPM} {lRPM}");

                // 0x0902 寫入 加減速 命令
                cmd = new byte[] { station, 0x06, 0x09, 0x02, hTime, lTime };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);
                response = Read();

                retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"{string.Join(",", retStr)}");

                // 0x0903 寫入 RPM 命令
                cmd = new byte[] { station, 0x06, 0x09, 0x03, hRPM, lRPM };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);
                response = Read();

                retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"{string.Join(",", retStr)}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Jog 正轉
        /// </summary>
        public void JogClock(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0001：Jog 正轉
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x04, 0x00, 0x01 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 2 + 4 + 2, 1000);
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"{string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Jog 逆轉
        /// </summary>
        public void JogCClock(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0000：Jog 逆轉
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x04, 0x00, 0x02 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 2 + 4 + 2, 1000);
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"{string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Jog 停止
        /// </summary>
        public void JogStop(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0000：停止轉動
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x04, 0x00, 0x00 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 2 + 4 + 2, 1000);
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"{string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 停止 JOG
        /// </summary>
        /// <param name="station">站號</param>
        public void JogDisable(byte station)
        {
            try
            {
                // 0x0901 寫入 0x0000：停止 Jog
                byte[] cmd = new byte[] { station, 0x06, 0x09, 0x01, 0x00, 0x00 };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 位址(2) + 資料(2) + CRC(2)
                byte[] response = Read();

                string[] cmdStr = Array.ConvertAll(cmd, (a) => $"{a:X2}");
                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                //Debug.WriteLine($"{string.Join(",", cmdStr)}");
                Debug.WriteLine($"{string.Join(",", retStr)}");
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region PosMove
        /// <summary>
        /// 定位測試啟動
        /// </summary>
        /// <param name="station">站號</param>
        public void PosMoveEnable(byte station)
        {
            try
            {
                // 0x0901 寫入 0x0004：啟動定位控制
                byte[] cmd = new byte[] { station, 0x06, 0x09, 0x01, 0x00, 0x04 };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);       // 站號(1) + 功能碼(1) + 起始位置(2) + 資料(2) + CRC(2)
                byte[] response = Read();

                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                Debug.WriteLine($"啟動定位: {string.Join(",", retStr)}");

                // 加減速時間 換算
                byte hTime = (byte)((PosMoveAccDecTime >> 8) & 0xFF);
                byte lTime = (byte)(PosMoveAccDecTime & 0xFF);

                // RPM 換算
                byte hRPM = (byte)((PosMoveRPM >> 8) & 0xFF);
                byte lRPM = (byte)(PosMoveRPM & 0xFF);

                // 目標派波數 換算
                byte hhPul = (byte)((PosMovePulse >> 24) & 0xFF);
                byte hlPul = (byte)((PosMovePulse >> 16) & 0xFF);
                byte lhPul = (byte)((PosMovePulse >> 8) & 0xFF);
                byte llPul = (byte)(PosMovePulse & 0xFF);

                Debug.WriteLine($"{hhPul} {hlPul} {lhPul} {llPul}");

                // 0x0902 寫入 加減入時間
                cmd = new byte[] { station, 0x06, 0x09, 0x02, hTime, lTime };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 2 + 4 + 2, 1000);
                response = Read();

                retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"寫入加減速 {string.Join(",", retStr)}");

                // 0x0903 寫入 RPM 命令
                cmd = new byte[] { station, 0x06, 0x09, 0x03, hRPM, lRPM };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 2 + 4 + 2, 1000);
                response = Read();

                retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                Debug.WriteLine($"寫入轉速 {string.Join(",", retStr)}");

                cmd = new byte[] { station, 0x10, 0x09, 0x05, 0x00, 0x02, 0x04, lhPul, llPul, hhPul, hlPul };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 起始位置(2) + 長度(2) + CRC(2)
                response = Read();

                string[] cmdStr = Array.ConvertAll(cmd, (a) => $"{a:X2}");
                retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                // Debug.WriteLine($"寫入 {string.Join(",", cmdStr)}");
                Debug.WriteLine($"寫入脈波數 {string.Join(",", retStr)}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 定位測試正轉
        /// </summary>
        /// <param name="station">站號</param>
        public void PosMoveClock(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0001：Jog 正轉
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x07, 0x00, 0x01 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 位址(2) + 資料(2)
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"正轉 {string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 定位測試逆轉
        /// </summary>
        /// <param name="station">站號</param>
        public void PosMoveCClock(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0001：Jog 正轉
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x07, 0x00, 0x02 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 位址(2) + 資料(2)
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"逆轉 {string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 定位測試暫停/停止
        /// </summary>
        /// <param name="station"></param>
        public void PosMovePause(byte station)
        {
            try
            {
                _pollingAct.Add(() =>
                {
                    // 0x0904 寫入 0x0001：Jog 正轉
                    byte[] cmd = new byte[] { station, 0x06, 0x09, 0x07, 0x00, 0x00 };
                    cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                    Write(cmd);
                    _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 位址(2) + 資料(2)
                    byte[] response = Read();

                    string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");
                    Debug.WriteLine($"暫停 {string.Join(",", retStr)}");
                });
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// 定位測試停止
        /// </summary>
        /// <param name="station">站號</param>
        public void PosMoveDisable(byte station)
        {
            try
            {
                // 0x0901 寫入 0x0000：停止 Jog
                byte[] cmd = new byte[] { station, 0x06, 0x09, 0x01, 0x00, 0x00 };
                cmd = cmd.Concat(CRC16LH(cmd)).ToArray();

                Write(cmd);
                _ = SpinWait.SpinUntil(() => BytesInBuf >= 8, 1000);    // 站號(1) + 功能碼(1) + 位址(2) + 資料(2) + CRC(2)
                byte[] response = Read();

                string[] cmdStr = Array.ConvertAll(cmd, (a) => $"{a:X2}");
                string[] retStr = Array.ConvertAll(response, (a) => $"{a:X2}");

                //Debug.WriteLine($"{string.Join(",", cmdStr)}");
                Debug.WriteLine($"{string.Join(",", retStr)}");
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #endregion


        /// <summary>
        /// 寫入命令
        /// </summary>
        /// <param name="data"></param>
        protected override bool Write(byte[] data)
        {
            return base.Write(data);
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


