using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Advantech.Motion;

namespace ApexVisIns
{
    public class ServoMotion : INotifyPropertyChanged
    {
        #region Variables
        private DEV_LIST[] DEV_LISTs = new DEV_LIST[10];
        private uint DEV_Count;

        // private uint Result;
        private IntPtr DeviceHandle = IntPtr.Zero;
        private readonly IntPtr[] AxisHandles = new IntPtr[8];

        private double _posCmd;
        private double _posAct;
        private string _currentStatus;
        private bool _deviceOpened;

        private Timer statusTimer;
        private int _sltAxis = -1;
        private bool _servoOn;

        // private int _sltAxisIndex;
        #endregion

        /// <summary>
        /// EtherCAT board 開啟旗標
        /// </summary>
        public bool DeviceOpened
        {
            get => _deviceOpened;
            set
            {
                if (value != _deviceOpened)
                {
                    _deviceOpened = value;
                    OnPropertyChanged(nameof(DeviceOpened));
                }
            }
        }

        /// <summary>
        /// Status Timer 啟用旗標
        /// </summary>
        public bool StatusTimerOn => statusTimer.Enabled;

        /// <summary>
        /// 從站 ID 陣列
        /// </summary>
        public ushort[] SlaveIDArray { get; private set; } = new ushort[10]; 

        /// <summary>
        /// Servo Oo Flag
        /// </summary>
        public bool ServoOn
        {
            get => _servoOn;
            private set
            {
                if (value != _servoOn)
                {
                    _servoOn = value;
                    OnPropertyChanged(nameof(ServoOn));
                }
            }
        }

       

        public ObservableCollection<DeviceList> BoardList { get; } = new ObservableCollection<DeviceList>();

        /// <summary>
        /// 軸列表
        /// </summary>
        public List<MotionAxis> AxisList { get; } = new List<MotionAxis>();

        public int SelectedAxis
        {
            get => _sltAxis;
            set
            {
                if (value != _sltAxis)
                {
                    _sltAxis = value;
                    OnPropertyChanged(nameof(SelectedAxis));
                    OnPropertyChanged(nameof(SltMotionAxis));
                }
            }
        }

        public MotionAxis SltMotionAxis => 0 <= _sltAxis && _sltAxis < AxisList.Count ? AxisList[_sltAxis] : null;

#if false
        public double PosCommand
        {
            get => _posCmd;
            set
            {
                if (value != _posCmd)
                {
                    _posCmd = value;
                    OnPropertyChanged(nameof(PosCommand));
                }
            }
        }

        public double PosActual
        {
            get => _posAct;
            set
            {
                if (value != _posAct)
                {
                    _posAct = value;
                    OnPropertyChanged(nameof(PosActual));
                }
            }
        } 
#endif

#if false
        /// <summary>
        /// 伺服 Ready
        /// </summary>
        public AxisSignal IO_SRDY { get; set; } = new AxisSignal("SRDY");

        /// <summary>
        /// 伺服警報
        /// </summary>
        public AxisSignal IO_ALM { get; set; } = new AxisSignal("ALM");

        /// <summary>
        /// Positive Limit Flag
        /// </summary>
        public AxisSignal IO_LMTP { get; set; } = new AxisSignal("LMT+");

        /// <summary>
        /// Native Limit Flag
        /// </summary>
        public AxisSignal IO_LMTN { get; set; } = new AxisSignal("LMT-");

        /// <summary>
        /// Servo On Flag
        /// </summary>
        public AxisSignal IO_SVON { get; set; } = new AxisSignal("SVON");

        /// <summary>
        /// Servo Emergency Flag
        /// </summary>
        public AxisSignal IO_EMG { get; set; } = new AxisSignal("EMG");
#endif

        /// <summary>
        /// 更新 IO
        /// </summary>
#if false
        public void UpdateIO()
        {
            OnPropertyChanged(nameof(IO_SRDY));
            OnPropertyChanged(nameof(IO_ALM));
            OnPropertyChanged(nameof(IO_LMTP));
            OnPropertyChanged(nameof(IO_LMTN));
            OnPropertyChanged(nameof(IO_SVON));
            OnPropertyChanged(nameof(IO_EMG));
        } 
#endif

        ///// <summary>
        ///// 當前軸狀態
        ///// </summary>
        //public string CurrentStatus
        //{
        //    get => !string.IsNullOrEmpty(_currentStatus) ? _currentStatus?.Remove(0, 7) : string.Empty;
        //    set
        //    {
        //        if (value != _currentStatus)
        //        {
        //            _currentStatus = value;
        //            OnPropertyChanged(nameof(CurrentStatus));
        //        }
        //    }
        //}

        /// <summary>
        /// 最大軸數
        /// </summary>
        public uint MaxAxisCount { get; private set; }

        /// <summary>
        /// 運動軸 (待刪除)
        /// </summary>
        [Obsolete("待移除")]
        public ObservableCollection<string> Axes { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint GetAvailableDevices()
        {
            int result = Motion.mAcm_GetAvailableDevs(DEV_LISTs, 10, ref DEV_Count);

            if (result != (int)ErrorCode.SUCCESS)
            {
                throw new Exception($"列舉 EtherCAT Card 失敗: Code[0x{result:X}]");
            }

            BoardList.Clear();
            for (int i = 0; i < DEV_Count; i++)
            {
                BoardList.Add(new DeviceList(DEV_LISTs[i]));
            }

            return DEV_Count;
        }

        /// <summary>
        /// 開啟 Board
        /// </summary>
        public void OpenDevice(uint deviceNum)
        {
            uint result;
            int retry = 0;          // 重試次數
            uint AxesCount = 0;     // 裝置軸數 
            uint DiChCount = 0;     // 裝置DI數

            //ushort ringNo = 0;
            //ushort[] slaveIPArr = new ushort[6];
            //uint slaveCount = 0;    // 從站數量

            if (!DeviceOpened)
            {
                bool rescanFlag;    // 重試 Flag
                do
                {
                    result = Motion.mAcm_DevOpen(deviceNum, ref DeviceHandle);
                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        retry++;
                        rescanFlag = true;  // 重試Flag
                        if (retry > 5)
                        {
                            throw new Exception($"開啟 EtherCAT 卡失敗: Code[0x{result:X}]");
                        }
                    }
                    else
                    {
                        rescanFlag = false;
                    }
                } while (rescanFlag);

                // Get axis number of this device // 讀取最大軸數
                result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesCount);
                if (result != (int)ErrorCode.SUCCESS)
                {
                    throw new Exception($"讀取軸數量失敗: Code[0x{result:X}]");
                }
                MaxAxisCount = AxesCount;

                #region 可略過
#if false
                result = Motion.mAcm_DevGetMasInfo(DeviceHandle, ref ringNo, slaveIPArr, ref slaveCount);
                if (result != (int)ErrorCode.SUCCESS)
                {
                    throw new Exception($"讀取主站資訊失敗: Code[0x{result:X}]");
                }
                Debug.WriteLine($"{ringNo} {string.Join(",", slaveIPArr)} {slaveCount}");

                ADV_SLAVE_INFO info = new();
                result = Motion.mAcm_DevGetSlaveInfo(DeviceHandle, 0, 0, ref info); // argu 2 : 0 => Motion ring
                if (result != (int)ErrorCode.SUCCESS)
                {
                    throw new Exception($"讀取從站資訊失敗: Code[0x{result:X}]");
                }
                Debug.WriteLine($"{info.SlaveID} {info.Name} {info.RevisionNo}"); 
#endif
                #endregion

                Axes.Clear();   // 清空軸數
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)i, ref AxisHandles[i]);

                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"開啟軸失敗: Code[0x{result:X}]");
                    }
                    Axes.Add($"{i}-Axis");
                    AxisList.Add(new MotionAxis(AxisHandles[i], i));

                    // Set Commnad pos to 0
                    Motion.mAcm_AxSetCmdPosition(AxisHandles[i], 0);
                }

                // Get Di maximum number of this channel // 目前不知道要做啥
                result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DaqDiMaxChan, ref DiChCount);
                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"讀取屬性 FT_DaqDiMaxChan 失敗: Code[0x{result:X}]");
                }

                //// 這要幹麻? 
                //for (int i = 0; i < DiChCount; i++)
                //{
                //    Debug.WriteLine($"{i}");
                //}
                //Debug.WriteLine(DiChCount);

                // 
                DeviceOpened = true;    // 裝置開啟旗標
                                        // 啟動 Timer 
            }
        }

        /// <summary>
        /// 關閉 Board
        /// </summary>
        public void CloseDevice()
        {
            // 紀錄軸狀態
            ushort[] AxisState = new ushort[MaxAxisCount];

            if (DeviceOpened)
            {
                // Get the axis's current state
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    // 讀取軸狀態
                    Motion.mAcm_AxGetState(AxisHandles[i], ref AxisState[i]);

                    if (AxisState[i] == (uint)Advantech.Motion.AxisState.STA_AX_ERROR_STOP)
                    {
                        // 若軸狀態為Error，重置軸狀態
                        Motion.mAcm_AxResetError(AxisHandles[i]);
                    }
                    // 命令軸減速至停止
                    Motion.mAcm_AxStopDec(AxisHandles[i]);
                }

                // Close Axes
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    Motion.mAcm_AxClose(ref AxisHandles[i]);
                }
                MaxAxisCount = 0;
                // Close Device
                Motion.mAcm_DevClose(ref DeviceHandle);

                //ResetMotionIOStatus();
                //UpdateIO();
                //CurrentStatus = string.Empty;

                DeviceHandle = IntPtr.Zero; // 重置裝置 Handle
                DeviceOpened = false;       // 重置開啟旗標
            }
        }

        /// <summary>
        /// 切換 Servo On
        /// </summary>
        public void ServoOnSwitch()
        {
            uint result;

            if (!DeviceOpened)
            {
                return;
            }

            if (!ServoOn)
            {
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    // Servo On, augu 2 => 1
                    result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 1);

                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"{i}-Axis Servo On 失敗: Code: [0x{result:X}]");
                    }
                    ServoOn = true;
                }
            }
            else
            {
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    // Servo On, augu 2 => 0
                    result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 0);
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"{i}-Axis Servo Off 失敗: Code: [0x{result:X}]");
                    }
                    ServoOn = false;
                }
            }
        }

        /// <summary>
        /// 設定 Servo On
        /// </summary>
        public void SetAllServoOn()
        {
            uint result;

            if (!DeviceOpened)
            {
                return;
            }

            for (int i = 0; i < MaxAxisCount; i++)
            {
                // Servo On augu 2 => 1
                result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 1);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"{i}-Axis Servo On 失敗: Code[0x{result:X}]");
                }
            }
            SltMotionAxis.ServoOn = true;
        }

        /// <summary>
        /// 設定全部軸 Servo Off
        /// </summary>
        public void SetAllServoOff()
        {
            uint result;

            if (!DeviceOpened)
            {
                return;
            }

            for (int i = 0; i < MaxAxisCount; i++)
            {
                // Servo Off augu 2 => 0
                result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 0);

                if (result != (uint)ErrorCode.SUCCESS)
                {

                    throw new Exception($"{i}-Axis Servo Off 失敗: Code[0x{result:X}]");
                }
            }

            if (SltMotionAxis != null)
            {
                SltMotionAxis.ServoOn = false;
            }
        }

        /// <summary>
        /// 啟用 Timer
        /// </summary>
        /// <param name="interval"></param>
        public void EnableTimer(int interval)
        {
            if (statusTimer == null)
            {
                statusTimer = new Timer(interval)
                {
                    AutoReset = true
                };

                statusTimer.Elapsed += StatusTimer_Elapsed;
                statusTimer.Start();
            }
            else
            {
                statusTimer.Interval = interval;
                statusTimer.Start();
            }
        }

        /// <summary>
        /// Timer tick event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetMotionInfo();
        }

        /// <summary>
        /// 關閉 Timer
        /// </summary>
        public void DisableTimer()
        {
            statusTimer?.Stop();
        }

        public void GetMotionInfo()
        {
            double cmd = 0;
            double pos = 0;
            ushort axState = 0;
            uint result;
            uint IOStatus = 0;

            if (DeviceOpened && SelectedAxis != -1)
            {
                // Get current command position
                Motion.mAcm_AxGetCmdPosition(AxisHandles[_sltAxis], ref cmd);
                // Get current actual position
                Motion.mAcm_AxGetActualPosition(AxisHandles[_sltAxis], ref pos);

                SltMotionAxis.PosCommand = cmd;
                SltMotionAxis.PosActual = pos;

                //PosCommand = cmd;
                //PosActual = pos;

                result = Motion.mAcm_AxGetMotionIO(AxisHandles[_sltAxis], ref IOStatus);

                if (result == (uint)ErrorCode.SUCCESS)
                {
                    SetMotionIOStatus(IOStatus);
                    // UpdateIO();
                    SltMotionAxis.UpdateIO();
                }

                // Get Axis current state
                Motion.mAcm_AxGetState(AxisHandles[_sltAxis], ref axState);
                //CurrentStatus = $"{(AxisState)axState}";
                SltMotionAxis.CurrentStatus = $"{(AxisState)axState}";
                OnPropertyChanged(nameof(SltMotionAxis));
            }
        }

        /// <summary>
        /// 更新選擇軸 Servo IO Status
        /// </summary>
        /// <param name="IOStatus"></param>
        private void SetMotionIOStatus(uint IOStatus)
        {
            SltMotionAxis.IO_SRDY.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == (uint)Ax_Motion_IO.AX_MOTION_IO_RDY;
            SltMotionAxis.IO_ALM.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ALM) == (uint)Ax_Motion_IO.AX_MOTION_IO_ALM;
            SltMotionAxis.IO_LMTP.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP;
            SltMotionAxis.IO_LMTN.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN;
            SltMotionAxis.IO_SVON.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) == (uint)Ax_Motion_IO.AX_MOTION_IO_SVON;
            SltMotionAxis.IO_EMG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_EMG) == (uint)Ax_Motion_IO.AX_MOTION_IO_EMG;
        }

        /// <summary>
        ///重置 Servo IO 狀態
        /// </summary>
#if false
        private void ResetMotionIOStatus()
        {
            IO_SRDY.BitOn = false;
            IO_ALM.BitOn = false;
            IO_LMTP.BitOn = false;
            IO_LMTN.BitOn = false;
            IO_SVON.BitOn = false;
            IO_EMG.BitOn = false;
        } 
#endif

        /// <summary>
        /// 重置位置
        /// </summary>
        public void ResetPos(int axis = 0)
        {
            double cmdPos = 0;
            if (DeviceOpened && ServoOn)
            {
                uint result = Motion.mAcm_AxSetCmdPosition(AxisHandles[axis], cmdPos);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"重置位置命令失敗: Code[0x{result:X}]");
                }
            }
        }

        /// <summary>
        /// 重置錯誤
        /// </summary>
        public void ResetError(int axis = 0)
        {
            if (DeviceOpened)
            {
                uint result = Motion.mAcm_AxResetError(AxisHandles[axis]);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"重置錯誤失敗: Code[0x{result:X}]");
                }
            }
        }

        /// <summary>
        /// 參數寫入
        /// </summary>
        public void WriteParameter()
        {
            uint ID = 0;


            ADV_SLAVE_INFO aDV_SLAVE_INFO = new ADV_SLAVE_INFO();
            uint result = Motion.mAcm_DevGetSlaveInfo(DeviceHandle, 0, 0x2, ref aDV_SLAVE_INFO);
            Debug.WriteLine($"{result:X} ID:{aDV_SLAVE_INFO.SlaveID} {aDV_SLAVE_INFO.Name} {aDV_SLAVE_INFO.RevisionNo}");


            uint ppu = 0;
            uint ppum = 0;
            result = Motion.mAcm_GetU32Property(AxisHandles[_sltAxis], (uint)PropertyID.CFG_AxPPU, ref ppu);
            Debug.WriteLine($"{result:X} PPU:{ppu}");

            result = Motion.mAcm_GetU32Property(AxisHandles[_sltAxis], (uint)PropertyID.CFG_AxPPU, ref ppum);
            Debug.WriteLine($"{result:X} PPUM:{ppum}");
        }

        /// <summary>
        /// 軸 IO 狀態
        /// </summary>
        public class AxisSignal
        {
            /// <summary>
            /// 建構子
            /// </summary>
            /// <param name="name"></param>
            public AxisSignal(string name)
            {
                Name = name;
                BitOn = false;
            }

            /// <summary>
            /// 信號名稱
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 信號 On / Off
            /// </summary>
            public bool BitOn { get; set; }
        }

        /// <summary>
        /// 裝置列表
        /// </summary>
        public class DeviceList
        {
            /// <summary>
            /// 建構子
            /// </summary>
            /// <param name="dev"></param>
            public DeviceList(DEV_LIST dev)
            {
                DeviceName = dev.DeviceName;
                DeviceNumber = dev.DeviceNum;
                NumOfSubDevice = dev.NumofSubDevice;
            }

            /// <summary>
            /// 建構子
            /// </summary>
            /// <param name="deviceName"></param>
            /// <param name="deviceNnumber"></param>
            /// <param name="numOfSubDevice"></param>
            public DeviceList(string deviceName, uint deviceNnumber, short numOfSubDevice)
            {
                DeviceName = deviceName;
                DeviceNumber = deviceNnumber;
                NumOfSubDevice = numOfSubDevice;
            }

            public string DeviceName { get; }

            public uint DeviceNumber { get; }

            public short NumOfSubDevice { get; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //public void Dispose()
        //{
        //    statusTimer.Dispose();
        //    //throw new NotImplementedException();
        //}
    }

    public class MotionAxis : INotifyPropertyChanged
    {
        private string _currentStatus;
        private double _posCmd;
        private double _posAct;

        /// <summary>
        /// 軸 Handle
        /// </summary>
        private readonly IntPtr AxisHandle = IntPtr.Zero;
        private bool _servoOn;
        private uint _geatrN1;
        private uint _gearM;
        private bool _jogOn;

        /// <summary>
        /// xaml 用建構子
        /// </summary>
        public MotionAxis() { }

        /// <summary>
        /// MotionAxis 建構子
        /// </summary>
        /// <param name="axisHandle">軸 Handle</param>
        /// <param name="axisIndex">軸 Index</param>
        public MotionAxis(IntPtr axisHandle, int axisIndex)
        {
            AxisHandle = axisHandle;
            AxisIndex = axisIndex;
        }

        /// <summary>
        /// 軸 Index
        /// </summary>
        public int AxisIndex { get; set; }

        /// <summary>
        /// 軸名稱
        /// </summary>
        public string AxisName { get => $"{AxisIndex}-Axis"; }

        /// <summary>
        /// Servo Oo Flag
        /// </summary>
        public bool ServoOn
        {
            get => _servoOn;
            set
            {
                if (value != _servoOn)
                {
                    _servoOn = value;
                    OnPropertyChanged(nameof(ServoOn));
                }
            }
        }

        /// <summary>
        /// Jog On Flag
        /// </summary>
        public bool JogOn
        {
            get => _jogOn;
            private set
            {
                if (value != _jogOn)
                {
                    _jogOn = value;
                    OnPropertyChanged(nameof(JogOn));
                }
            }
        }

        /// <summary>
        /// 伺服 Ready
        /// </summary>
        public AxisSignal IO_SRDY { get; set; } = new AxisSignal("SRDY");

        /// <summary>
        /// 伺服警報
        /// </summary>
        public AxisSignal IO_ALM { get; set; } = new AxisSignal("ALM");

        /// <summary>
        /// Positive Limit Flag
        /// </summary>
        public AxisSignal IO_LMTP { get; set; } = new AxisSignal("LMT+");

        /// <summary>
        /// Native Limit Flag
        /// </summary>
        public AxisSignal IO_LMTN { get; set; } = new AxisSignal("LMT-");

        /// <summary>
        /// Servo On Flag
        /// </summary>
        public AxisSignal IO_SVON { get; set; } = new AxisSignal("SVON");

        /// <summary>
        /// Servo Emergency Flag
        /// </summary>
        public AxisSignal IO_EMG { get; set; } = new AxisSignal("EMG");

        /// <summary>
        /// 命令位置
        /// </summary>
        public double PosCommand
        {
            get => _posCmd;
            set
            {
                if (value != _posCmd)
                {
                    _posCmd = value;
                    OnPropertyChanged(nameof(PosCommand));
                }
            }
        }

        /// <summary>
        /// 實際位置
        /// </summary>
        public double PosActual
        {
            get => _posAct;
            set
            {
                if (value != _posAct)
                {
                    _posAct = value;
                    OnPropertyChanged(nameof(PosActual));
                }
            }
        }

        /// <summary>
        /// 齒輪比分子
        /// </summary>
        public uint GearN1
        {
            get => _geatrN1;
            set
            {
                if (value != _geatrN1)
                {
                    _geatrN1 = value;
                    OnPropertyChanged(nameof(GearN1));
                }
            }
        }

        /// <summary>
        /// 齒輪比分母
        /// </summary>
        public uint GearM
        {
            get => _gearM;
            set
            {
                if (value != _gearM)
                {
                    _gearM = value;
                    OnPropertyChanged(nameof(GearM));
                }
            }
        }

        /// <summary>
        /// Jog 初速度
        /// </summary>
        public double JogVelLow { get; set; }

        /// <summary>
        /// Jog 目標速度
        /// </summary>
        public double JogVelHigh { get; set; }

        /// <summary>
        /// Jog 加速度
        /// </summary>
        public double JogAcc { get; set; }
        
        /// <summary>
        /// Jog 減速度
        /// </summary>
        public double JogDec { get; set; }

        /// <summary>
        /// Jog 初速時間
        /// </summary>
        public uint JogVLTime { get; set; }

        /// <summary>
        /// 更新 IO
        /// </summary>
        public void UpdateIO()
        {
            OnPropertyChanged(nameof(IO_SRDY));
            OnPropertyChanged(nameof(IO_ALM));
            OnPropertyChanged(nameof(IO_LMTP));
            OnPropertyChanged(nameof(IO_LMTN));
            OnPropertyChanged(nameof(IO_SVON));
            OnPropertyChanged(nameof(IO_EMG));
        }

        /// <summary>
        /// 讀取軸資訊
        /// </summary>
        public void GetAxisInfo()
        {
            double cmd = 0;
            double pos = 0;
            ushort axState = 0;
            uint result;
            uint IOStatus = 0;

            // Get current command position
            Motion.mAcm_AxGetCmdPosition(AxisHandle, ref cmd);
            // Get current actual position
            Motion.mAcm_AxGetActualPosition(AxisHandle, ref pos);

            PosCommand = cmd;
            PosActual = pos;

            result = Motion.mAcm_AxGetMotionIO(AxisHandle, ref IOStatus);

            if (result == (uint)ErrorCode.SUCCESS)
            {
                SetMotionIOStatus(IOStatus);
                // UpdateIO(); // 更新 IO (trigger property)
            }

            // Get Axis current state
            Motion.mAcm_AxGetState(AxisHandle, ref axState);
            CurrentStatus = $"{(AxisState)axState}";
        }

        /// <summary>
        /// 更新 Servo IO Status
        /// </summary>
        /// <param name="IOStatus"></param>
        private void SetMotionIOStatus(uint IOStatus)
        {
            IO_SRDY.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == (uint)Ax_Motion_IO.AX_MOTION_IO_RDY;
            IO_ALM.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ALM) == (uint)Ax_Motion_IO.AX_MOTION_IO_ALM;
            IO_LMTP.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP;
            IO_LMTN.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN;
            IO_SVON.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) == (uint)Ax_Motion_IO.AX_MOTION_IO_SVON;
            IO_EMG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_EMG) == (uint)Ax_Motion_IO.AX_MOTION_IO_EMG;
        }

        /// <summary>
        /// 當前軸狀態
        /// </summary>
        public string CurrentStatus
        {
            get => !string.IsNullOrEmpty(_currentStatus) ? _currentStatus?.Remove(0, 7) : string.Empty;
            set
            {
                if (value != _currentStatus)
                {
                    _currentStatus = value;
                    OnPropertyChanged(nameof(CurrentStatus));
                }
            }
        }

        /// <summary>
        /// 設定 Servo On
        /// </summary>
        public void SetServoOn()
        {
            uint result;

            if (!ServoOn)
            {
                // Servo On augu 2 => 1
                result = Motion.mAcm_AxSetSvOn(AxisHandle, 1);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"{AxisIndex}-Axis Servo On 失敗: Code[0x{result:X}]");
                }
                ServoOn = true;
            }
        }

        public void SetServoOff()
        {
            uint result;

            if (ServoOn)
            {
                // Servo Off augu 2 => 0
                result = Motion.mAcm_AxSetSvOn(AxisHandle, 0);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"{AxisIndex}-Axis Servo Off 失敗: Code[0x{result:X}]");
                }
                ServoOn = false;
            }
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        public void ResetPos()
        {
            double cmdPos = 0;
            // 先確認 Servo On
            if (ServoOn)
            {
                uint result = Motion.mAcm_AxSetCmdPosition(AxisHandle, cmdPos);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"重置位置命令失敗: Code[0x{result:X}]");
                }
            }
        }

        /// <summary>
        /// 重置錯誤
        /// </summary>
        public void ResetError()
        {
            uint result = Motion.mAcm_AxResetError(AxisHandle);

            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"重置錯誤失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 寫入齒輪比
        /// </summary>
        public void SetGearRatio()
        {
            uint result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPU,  GearN1);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入電子齒輪比 N1 失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPUDenominator, GearM);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入電子齒輪比 N1 失敗: Code[0x{result:X}]");
            }

            GetGearRatio();
        }

        /// <summary>
        /// 讀取齒輪比
        /// </summary>
        public void GetGearRatio()
        {
            uint ppu = 0;
            uint ppum = 0;

            uint result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPU, ref ppu);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取電子齒輪比 N1 失敗: Code[0x{result:X}]");
            }

            GearN1 = ppu;

            result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPUDenominator, ref ppum);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取電子齒輪比 M 失敗: Code[0x{result:X}]");
            }
            GearM = ppum;

            OnPropertyChanged(nameof(GearN1));
            OnPropertyChanged(nameof(GearM));
        }

        /// <summary>
        /// 寫入速度參數
        /// </summary>
        public void SetAxisVelParam()
        {
            Debug.WriteLine($"{JogVelLow} {JogVelHigh}");
            Debug.WriteLine($"{JogAcc} {JogDec}");
            Debug.WriteLine($"{JogVLTime}");

            uint result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelLow, JogVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入初始速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelHigh, JogVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入目標速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogAcc, JogAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入加速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogDec, JogDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入減速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxJogVLTime, JogVLTime);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"寫入初速時間失敗: Code[0x{result:X}]");
            }

            GetAxisVelParam();
        }

        /// <summary>
        /// 讀取速度參數
        /// </summary>
        public void GetAxisVelParam()
        {
            double axJogVelLow = 0;
            double axJogVelHigh = 0;
            double axJogAcc = 0;
            double axJogDec = 0;
            uint axJogVLTime = 0;


            double axParJogVelLow = 0;
            double axParJogVelHigh = 0;

            uint result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelLow, ref axJogVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取初始速度失敗: Code[0x{result:X}]");
            }
            JogVelLow = axJogVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelHigh, ref axJogVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取目標速度失敗: Code[0x{result:X}]");
            }
            JogVelHigh = axJogVelHigh;


            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxJogVelLow, ref axParJogVelLow);

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxJogVelHigh, ref axParJogVelHigh);

            Debug.WriteLine($"Par: {axParJogVelHigh} {axParJogVelLow}");

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogAcc, ref axJogAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取加速度失敗: Code[0x{result:X}]");
            }
            JogAcc = axJogAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogDec, ref axJogDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取減速度失敗: Code[0x{result:X}]");
            }
            JogDec = axJogDec;

            result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxJogVLTime, ref axJogVLTime);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"讀取初速時間失敗: Code[0x{result:X}]");
            }
            JogVLTime = axJogVLTime;

            OnPropertyChanged(nameof(JogVelLow));
            OnPropertyChanged(nameof(JogVelHigh));
            OnPropertyChanged(nameof(JogAcc));
            OnPropertyChanged(nameof(JogDec));
            OnPropertyChanged(nameof(JogVLTime));
        }

        /// <summary>
        /// Jog 開始
        /// </summary>
        public void JogStart()
        {
            uint result = Motion.mAcm_AxSetExtDrive(AxisHandle, 1);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"啟動 Jog 模式失敗: Code[0x{result:X}]");
                //throw new Exception($"寫入初始速度失敗: Code[0x{result:X}]");
            }
            JogOn = true;
        }

        /// <summary>
        /// Jog 停止
        /// </summary>
        public void JogStop()
        {
            uint result = Motion.mAcm_AxSetExtDrive(AxisHandle, 0);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"停止 Jog 模式失敗: Code[0x{result:X}]");
                //throw new Exception($"寫入初始速度失敗: Code[0x{result:X}]");
            }
            JogOn = false;
        }

        /// <summary>
        /// 順時針旋轉
        /// </summary>
        public void JogClock()
        {
            uint result = Motion.mAcm_AxJog(AxisHandle, 1);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"Jog 模式失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 逆時針旋轉
        /// </summary>
        public void JogCtClock()
        {
            uint result = Motion.mAcm_AxJog(AxisHandle, 0);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"Jog 模式失敗: Code[0x{result:X}]");
            }
        }


        /// <summary>
        /// Jog 停止
        /// </summary>
        public void JogDecAction()
        {
            uint result = Motion.mAcm_AxStopDec(AxisHandle);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new Exception($"停止 Jog 模式失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 軸 IO 狀態
        /// </summary>
        public class AxisSignal
        {
            public AxisSignal(string name)
            {
                Name = name;
                BitOn = false;
            }

            /// <summary>
            /// 信號名稱
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 信號 On / Off
            /// </summary>
            public bool BitOn { get; set; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
