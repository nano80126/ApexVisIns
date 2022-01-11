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
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using System.Runtime.CompilerServices;

namespace ApexVisIns
{
    public class MotionEnumer : LongLifeWorker
    {
        private readonly object _ColleciotnLock = new();

        private DEV_LIST[] DEV_LISTs = new DEV_LIST[10];
        private uint DEV_Count;

        public ObservableCollection<ServoMotion.MotionDevice> MotionDevices { get; } = new();

        /// <summary>
        /// 確認 DLL 已安裝且版本符合
        /// </summary>
        /// <returns>DLL是否安裝正確</returns>
        public static bool CheckDllVersion()
        {
            string fileName = Environment.SystemDirectory + @"\ADVMOT.dll"; // SystemDirectory : System32

            if (File.Exists(fileName))
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(fileName).FileVersion;

                string[] strSplit = fileVersion.Split(',');

                if (Convert.ToUInt16(strSplit[0], CultureInfo.CurrentCulture) < 2)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }


        private void DevicesAdd(ServoMotion.MotionDevice device)
        {
            lock (_ColleciotnLock)
            {
                MotionDevices.Add(device);
            }
        }

        private void DevicesClear()
        {
            lock (_ColleciotnLock)
            {
                MotionDevices.Clear();
            }
        }

        public void Interrupt()
        {
            InitFlag = InitFlags.Interrupt;
        }

        /// <summary>
        /// 工作者啟動，
        /// 先確認DLL是否已正確安裝
        /// </summary>
        public override void WorkerStart()
        {
            // bool dllIsValid = ServoMotion.CheckDllVersion();
            // if (dllIsValid)
            // {
            //     BindingOperations.EnableCollectionSynchronization(MotionDevices, _ColleciotnLock);
            //     base.WorkerStart();
            // }
            // else
            // {
            //     InitFlag = InitFlags.Interrupt;
            //     throw new DllNotFoundException("MOTION 控制驅動未安裝或版本不符");
            // }
            BindingOperations.EnableCollectionSynchronization(MotionDevices, _ColleciotnLock);
            base.WorkerStart();
        }

        /// <summary>
        /// 工作者停止
        /// </summary>
        public override void WorkerEnd()
        {
            BindingOperations.DisableCollectionSynchronization(MotionDevices);
            base.WorkerEnd();
        }

        /// <summary>
        /// 作業迴圈
        /// </summary>
        public override void DoWork()
        {
            try
            {
                int result = Motion.mAcm_GetAvailableDevs(DEV_LISTs, 10, ref DEV_Count);

                if (result != (int)ErrorCode.SUCCESS)
                {
                    throw new InvalidOperationException($"取得 EtherCAT Cards 失敗: Code[0x{result:X}]");
                }

                if (DEV_Count == 0)
                {
                    DevicesClear();
                    InitFlag = InitFlags.Finished;
                }

                for (int i = 0; i < DEV_Count; i++)
                {
                    // 先確認集合內不包含
                    if (!MotionDevices.ToList().Exists(x => x.DeviceNumber == DEV_LISTs[i].DeviceNum))
                    {
                        DevicesAdd(new ServoMotion.MotionDevice(DEV_LISTs[i]));
                    }
                }

                InitFlag = InitFlags.Finished;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Motion Devices 數量
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return MotionDevices.Count;
        }

        /// <summary>
        /// 取得第一張卡號碼
        /// </summary>
        /// <returns></returns>
        public uint GetFirstDeivceNum()
        {
            return MotionDevices[0].DeviceNumber;
        }
    }


    public class ServoMotion : INotifyPropertyChanged, IDisposable
    {
        #region Variables
        //private DEV_LIST[] DEV_LISTs = new DEV_LIST[10];
        //private uint DEV_Count;

        // private uint Result;
        private IntPtr DeviceHandle = IntPtr.Zero;
        private readonly IntPtr[] AxisHandles = new IntPtr[4];

        //private double _posCmd;
        //private double _posAct;
        //private string _currentStatus;
        private bool _deviceOpened;

        private System.Timers.Timer statusTimer;
        private int _sltAxis = -1;
        private bool _disposed;

        //private bool _servoOn;

        // private int _sltAxisIndex;

        //private readonly object _deviceColltionLock = new();
        private readonly object _axisColltionLock = new();
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
        public ushort[] SlaveIDArray { get; private set; } = new ushort[4];

        //public ObservableCollection<DeviceList> BoardList { get; } = new ObservableCollection<DeviceList>();

        //public int BoardCount => BoardList.Count;

        /// <summary>
        /// 軸列表，
        /// 預設四軸 null，
        /// OpenDevice 時會重置
        /// </summary>
        public ObservableCollection<MotionAxis> Axes { get; } = new ObservableCollection<MotionAxis>() { null, null, null, null };

        /// <summary>
        /// 原點復歸模式列表
        /// </summary>
        public List<HomeMode> HomeModes { get; } = new List<HomeMode>();

        /// <summary>
        /// 選擇軸
        /// </summary>
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

                if (_sltAxis != -1)
                {
                    EnableTimer(100);
                }
                else
                {
                    DisableTimer();
                }
            }
        }

        /// <summary>
        /// 選擇軸
        /// </summary>
        public MotionAxis SltMotionAxis => 0 <= _sltAxis && _sltAxis < Axes.Count ? Axes[_sltAxis] : null;

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
        /// 確認 DLL 已安裝且版本符合
        /// </summary>
        /// <returns>DLL是否安裝正確</returns>
        public static bool CheckDllVersion()
        {
            string fileName = Environment.SystemDirectory + @"\ADVMOT.dll"; // SystemDirectory : System32

            if (File.Exists(fileName))
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(fileName).FileVersion;

                string[] strSplit = fileVersion.Split(',');

                if (Convert.ToUInt16(strSplit[0], CultureInfo.CurrentCulture) < 2)
                {
                    return false;
                }
            }
            else
            {
                //throw new DllNotFoundException("Motion 控制驅動未安裝");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 啟用 Collecion Binding
        /// </summary>
        public void EnableCollectionBinding()
        {
            //BindingOperations.EnableCollectionSynchronization(BoardList, _deviceColltionLock);
            BindingOperations.EnableCollectionSynchronization(Axes, _axisColltionLock);
        }

        /// <summary>
        /// 停用 Collection Binding
        /// </summary>
        public void DisableCollectionBinding()
        {
            BindingOperations.DisableCollectionSynchronization(Axes);
        }


#if false
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

            lock (_deviceColltionLock)
            {
                BoardList.Clear();
                for (int i = 0; i < DEV_Count; i++)
                {
                    BoardList.Add(new DeviceList(DEV_LISTs[i]));
                }
            }

            return DEV_Count;
        } 
#endif

        /// <summary>
        /// 開啟 Board
        /// </summary>
        public void OpenDevice(uint deviceNum)
        {
            uint result;
            int retry = 0;          // 重試次數
            uint AxesCount = 0;     // 裝置軸數 
            // uint DiChCount = 0;     // 裝置DI數，用不到

            // ushort ringNo = 0;      // 
            // ushort[] slaveIPArr = new ushort[10];   
            // uint slaveCount = 0;    // 從站數量

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

                Axes.Clear();
                for (int i = 0; i < MaxAxisCount; i++)
                {
                    //result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)i, ref AxisHandles[i]);

                    Axes.Add(new MotionAxis(DeviceHandle, i));
                    result = Axes[i].AxisOpen(out AxisHandles[i]);

                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"開啟軸失敗: Code[0x{result:X}]");
                    }
                }

                #region 可略過，不知道能幹嘛
                // result = Motion.mAcm_DevGetMasInfo(DeviceHandle, ref ringNo, slaveIPArr, ref slaveCount);
                // if (result != (int)ErrorCode.SUCCESS)
                // {
                //     throw new Exception($"讀取主站資訊失敗: Code[0x{result:X}]");
                // }
                // Debug.WriteLine($"RingNO: {ringNo} {string.Join(",", slaveIPArr)} {slaveCount}");

#if true
                ADV_SLAVE_INFO info = new();
                ushort j = 0x01;    // 從站站號
                int slvIndex = 0;   // 從站數量
                do
                {
                    if (slvIndex < MaxAxisCount)
                    {
                        result = Motion.mAcm_DevGetSlaveInfo(DeviceHandle, 0, j, ref info); // argu 2 : 0 => Motion ring
                        if (result != (int)ErrorCode.SUCCESS)
                        {
                            // Debug.WriteLine($"result: {result:X} j:{j:X}");
                        }
                        else
                        {
                            Axes[slvIndex].SlaveNumber = info.SlaveID;
                            slvIndex++;
                            Debug.WriteLine($"{info.SlaveID} {info.Name} {info.RevisionNo}");
                        }
                        j++;
                    }
                    else
                    {
                        break;
                    }
                } while (j < 0xFF);
#endif
                #endregion


                if (HomeModes.Count == 0)
                {
                    foreach (Advantech.Motion.HomeMode mode in Enum.GetValues(typeof(Advantech.Motion.HomeMode)))
                    {
                        HomeModes.Add(new HomeMode(mode.ToString(), (uint)mode));
                    }

                    foreach (Cia402HomeMode mode in Enum.GetValues(typeof(Cia402HomeMode)))
                    {
                        HomeModes.Add(new HomeMode(mode.ToString(), (uint)mode));
                    }
                }

                // Get Di maximum number of this channel // 目前不知道要做啥    // 大概沒用
                //result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DaqDiMaxChan, ref DiChCount);
                //if (result != (uint)ErrorCode.SUCCESS)
                //{
                //    throw new Exception($"讀取屬性 FT_DaqDiMaxChan 失敗: Code[0x{result:X}]");
                //}

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
            //uint result;
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
                    //Motion.mAcm_AxClose(ref AxisHandles[i]);
                    _ = Axes[i].AxisClose(out AxisHandles[i]);
                }
                MaxAxisCount = 0;
                // Close Device
                _ = Motion.mAcm_DevClose(ref DeviceHandle);

                //ResetMotionIOStatus();
                //UpdateIO();
                //CurrentStatus = string.Empty;

                DeviceHandle = IntPtr.Zero; // 重置裝置 Handle
                DeviceOpened = false;       // 重置開啟旗標
            }
        }

        /// <summary>
        /// 設定全部軸 Servo On
        /// </summary>
        public void SetAllServoOn()
        {
            if (!DeviceOpened)
            {
                return;
            }

            #region 待刪除
            //for (int i = 0; i < MaxAxisCount; i++)
            //{
            //    // Servo On augu 2 => 1
            //    result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 1);

            //    if (result != (uint)ErrorCode.SUCCESS)
            //    {
            //        throw new Exception($"{i}-Axis Servo On 失敗: Code[0x{result:X}]");
            //    }
            //}
            //SltMotionAxis.ServoOn = true; 
            #endregion

            try
            {
                for (int i = 0; i < Axes.Count; i++)
                {
                    Axes[i].SetServoOn();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 設定全部軸 Servo Off
        /// </summary>
        public void SetAllServoOff()
        {
            if (!DeviceOpened)
            {
                return;
            }

            //for (int i = 0; i < MaxAxisCount; i++)
            //{
            //    Debug.WriteLine($"Axis: {i} {AxisHandles[i]}");

            //    // Servo Off augu 2 => 0
            //    result = Motion.mAcm_AxSetSvOn(AxisHandles[i], 0);

            //    if (result != (uint)ErrorCode.SUCCESS)
            //    {
            //        throw new Exception($"{i}-Axis Servo Off 失敗: Code[0x{result:X}]");
            //    }
            //}

            try
            {
                for (int i = 0; i < Axes.Count; i++)
                {
                    Axes[i].SetServoOff();
                }
            }
            catch (Exception)
            {
                throw;
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
                statusTimer = new System.Timers.Timer(interval)
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
            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            GetMotionInfo();
        }

        /// <summary>
        /// 關閉 Timer
        /// </summary>
        public void DisableTimer()
        {
            statusTimer?.Stop();
        }

        /// <summary>
        /// 取得 Motion Information
        /// </summary>
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

                //Debug.WriteLine($"{(IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_DIR) == (uint)Ax_Motion_IO.AX_MOTION_IO_DIR}");
                result = Motion.mAcm_AxGetMotionIO(AxisHandles[_sltAxis], ref IOStatus);
                if (result == (uint)ErrorCode.SUCCESS)
                {
                    SetMotionIOStatus(IOStatus);
                    // UpdateIO();
                    SltMotionAxis.UpdateIO();
                }

                // Get Axis current state
                Motion.mAcm_AxGetState(AxisHandles[_sltAxis], ref axState);
                // CurrentStatus = $"{(AxisState)axState}";
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
            SltMotionAxis.IO_ORG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ORG) == (uint)Ax_Motion_IO.AX_MOTION_IO_ORG;
        }


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
        /// 原點復歸模式
        /// </summary>
        public class HomeMode
        {
            public HomeMode(string modeName, uint modeCode)
            {
                ModeName = modeName;
                ModeCode = modeCode;
            }

            public string ModeName { get; set; }

            public uint ModeCode { get; set; }
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
        public class MotionDevice
        {
            /// <summary>
            /// 建構子
            /// </summary>
            /// <param name="dev"></param>
            public MotionDevice(DEV_LIST dev)
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
            public MotionDevice(string deviceName, uint deviceNnumber, short numOfSubDevice)
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
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
            //throw new NotImplementedException();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Axes.Clear();
                DisableCollectionBinding();
                // homemo

                if (statusTimer != null)
                {
                    statusTimer.Stop();
                    statusTimer.Dispose();
                    statusTimer = null;
                }
            }

            _disposed = true;
        }

        //public void Dispose()
        //{
        //    statusTimer.Dispose();
        //    //throw new NotImplementedException();
        //}
    }


    /// <summary>
    /// Motion 軸參數
    /// </summary>
    public class MotionVelParam : INotifyPropertyChanged
    {
        #region Varibles

        #region Gear Ratio
        private uint _gearN1;
        private uint _gearM;
        #endregion
        
        #region JOG
        private double _jogVelLow;
        private double _jogVelHigh;
        private double _jogAcc;
        private double _jogDec;
        private uint _jogVLTime;
        #endregion

        #region HOME
        private double _homeVelLow;
        private double _homeVelHigh;
        private double _homeAcc;
        private double _homeDec;
        #endregion

        #region Velocity
        private bool _absolute;
        private double _velLow;
        private double _velHigh;
        private double _acc;
        private double _dec; 
        #endregion

        // public MotionVelParam(MotionAxis axis)
        // {
        //     SlaveNumber = axis.SlaveNumber;
        //     GearN1 = axis.GearN1;
        //     GearM = axis.GearM;
        //     JogVelLow = axis.JogVelLow;
        //     JogVelHigh = axis.JogVelHigh;
        //     JogAcc = axis.JogAcc;
        //     JogDec = axis.JogDec;
        //     JogVLTime = axis.JogVLTime;
        // }
        #endregion

        /// <summary>
        /// 從站編號
        /// </summary>
        public uint SlaveNumber { get; set; }

        /// <summary>
        /// 齒輪比分子
        /// </summary>
        public uint GearN1
        {
            get => _gearN1;
            set
            {
                if (value != _gearN1)
                {
                    _gearN1 = value;
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
        public double JogVelLow
        {
            get => _jogVelLow;
            set
            {
                if (value != _jogVelLow)
                {
                    _jogVelLow = value;
                    OnPropertyChanged(nameof(JogVelLow));
                }
            }
        }

        /// <summary>
        /// Jog 目標速度
        /// </summary>
        public double JogVelHigh
        {
            get => _jogVelHigh;
            set
            {
                if (value != _jogVelHigh)
                {
                    _jogVelHigh = value;
                    OnPropertyChanged(nameof(JogVelHigh));
                }
            }

        }

        /// <summary>
        /// Jog 加速度
        /// </summary>
        public double JogAcc
        {
            get => _jogAcc;
            set
            {
                if (value != _jogAcc)
                {
                    _jogAcc = value;
                    OnPropertyChanged(nameof(JogAcc));
                }
            }
        }

        /// <summary>
        /// Jog 減速度
        /// </summary>
        public double JogDec
        {
            get => _jogDec;
            set
            {
                if (value != _jogDec)
                {
                    _jogDec = value;
                    OnPropertyChanged(nameof(JogDec));
                }
            }
        }

        /// <summary>
        /// Jog 初速時間
        /// </summary>
        public uint JogVLTime
        {
            get => _jogVLTime;
            set
            {
                if (value != _jogVLTime)
                {
                    _jogVLTime = value;
                    OnPropertyChanged(nameof(JogVLTime));
                }
            }
        }

        /// <summary>
        /// 回原點
        /// </summary>
        public double HomeVelLow
        {
            get => _homeVelLow;
            set
            {
                if (value != _homeVelLow)
                {
                    _homeVelLow = value;
                    OnPropertyChanged(nameof(HomeVelLow));
                }
            }
        }

        /// <summary>
        /// 回原點 目標速度
        /// </summary>
        public double HomeVelHigh
        {
            get => _homeVelHigh;
            set
            {
                if (value != _homeVelHigh)
                {
                    _homeVelHigh = value;
                    OnPropertyChanged(nameof(HomeVelHigh));
                }
            }
        }

        /// <summary>
        /// 回原點 加速度
        /// </summary>
        public double HomeAcc
        {
            get => _homeAcc;
            set
            {
                if (value != _homeAcc)
                {
                    _homeAcc = value;
                    OnPropertyChanged(nameof(HomeAcc));
                }
            }
        }

        /// <summary>
        /// 回原點 減速度
        /// </summary>
        public double HomeDec
        {
            get => _homeDec;
            set
            {
                if (value != _homeDec)
                {
                    _homeDec = value;
                    OnPropertyChanged(nameof(HomeDec));
                }
            }
        }

        /// <summary>
        /// Move Mode Absolute or Relative
        /// </summary>
        public bool Absolute
        {
            get => _absolute;
            set
            {
                if (value != _absolute)
                {
                    _absolute = value;
                    OnPropertyChanged(nameof(Absolute));
                }
            }
        }

        /// <summary>
        /// PAR_AxVelLow
        /// </summary>
        public double VelLow
        {
            get => _velLow;
            set
            {
                if (value != _velLow)
                {
                    _velLow = value;
                    OnPropertyChanged(nameof(VelLow));
                }
            }
        }

        /// <summary>
        /// PAR_AxVelHigh
        /// </summary>
        public double VelHigh
        {
            get => _velHigh;
            set
            {
                if (value != _velHigh)
                {
                    _velHigh = value;
                    OnPropertyChanged(nameof(VelHigh));
                }
            }
        }

        /// <summary>
        /// PAR_AxAcc
        /// </summary>
        public double Acc
        {
            get => _acc;
            set
            {
                if (value != _acc)
                {
                    _acc = value;
                    OnPropertyChanged(nameof(Acc));
                }
            }
        }

        /// <summary>
        /// PAR_AxDec
        /// </summary>
        public double Dec
        {
            get => _dec;
            set
            {
                if (value != _dec)
                {
                    _dec = value;
                    OnPropertyChanged(nameof(Dec));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 觸發 Property Change
        /// </summary>
        /// <param name="propertyName"></param>
        public void PropertyChange(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// Motion 軸狀態、參數等
    /// </summary>
    public class MotionAxis : MotionVelParam
    {
        private string _currentStatus;
        private double _posCmd;
        private double _posAct;

        /// <summary>
        /// 軸 Handle
        /// </summary>
        private IntPtr AxisHandle = IntPtr.Zero;
        private readonly IntPtr DeviceHandle = IntPtr.Zero;
        private bool _servoOn;

        private bool _jogOn;
        private bool _isAxisOpen;

        /// <summary>
        /// xaml 用建構子
        /// </summary>
        public MotionAxis() {}

        /// <summary>
        /// MotionAxis 建構子
        /// </summary>
        /// <param name="axisHandle">軸 Handle</param>
        /// <param name="axisIndex">軸 Index</param>
        public MotionAxis(IntPtr deviceHandle, int axisIndex)
        {
            DeviceHandle = deviceHandle;
            //AxisHandle = axisHandle;
            AxisIndex = axisIndex;
        }

        /// <summary>
        /// MotionAxis 建構子
        /// </summary>
        /// <param name="axisHandle">軸 Handle</param>
        /// <param name="axisIndex">軸 Index</param>
        public MotionAxis(IntPtr deviceHandle, int axisIndex, ushort slaveNumber)
        {
            DeviceHandle = deviceHandle;
            //AxisHandle = axisHandle;
            AxisIndex = axisIndex;
            SlaveNumber = slaveNumber;
        }

        /// <summary>
        /// 軸 Index
        /// </summary>
        public int AxisIndex { get; set; }

        /// <summary>
        /// 軸名稱
        /// </summary>
        public string AxisName { get => $"{AxisIndex}-Axis"; }

        // public uint SlaveNumber { get; set; }

        /// <summary>
        /// 從 Json 載入 Param
        /// </summary>
        /// <param name="param"></param>
        public void LoadFromVelParam(MotionVelParam param)
        {
            GearN1 = param.GearN1;
            GearM = param.GearM;
            // JOG
            JogVelLow = param.JogVelLow;
            JogVelHigh = param.JogVelHigh;
            JogAcc = param.JogAcc;
            JogDec = param.JogDec;
            JogVLTime = param.JogVLTime;
            // HOME
            HomeVelLow = param.HomeVelLow;
            HomeVelHigh = param.HomeVelHigh;
            HomeAcc = param.HomeAcc;
            HomeDec = param.HomeDec;
            // AXIS
            Absolute = param.Absolute;
            VelHigh = param.VelHigh;
            VelLow = param.VelLow;
            Acc = param.Acc;
            Dec = param.Dec;
        }

        /// <summary>
        /// 是否開啟軸
        /// </summary>
        public bool IsAxisOpen
        {
            get => _isAxisOpen;
            set
            {
                if (value != _isAxisOpen)
                {
                    _isAxisOpen = value;
                    OnPropertyChanged(nameof(IsAxisOpen));
                }
            }
        }

        /// <summary>
        /// 開啟軸
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public uint AxisOpen(out IntPtr handle)
        {
            handle = IntPtr.Zero;
            uint result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)AxisIndex, ref AxisHandle);

            if (result == (int)ErrorCode.SUCCESS)
            {
                //throw new Exception($"{AxisIndex}-Axis 開啟失敗: Code[0x{result:X}]");
                IsAxisOpen = true;
                handle = AxisHandle;
            }
            return result;
        }

        /// <summary>
        /// 關閉軸
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public uint AxisClose(out IntPtr handle)
        {
            handle = IntPtr.Zero;
            uint result = Motion.mAcm_AxClose(ref AxisHandle);

            if (result == (uint)ErrorCode.SUCCESS)
            {
                IsAxisOpen = false;
                handle = AxisHandle;
                //throw new Exception($"{AxisIndex}-Axis 關閉失敗: Code[0x{result:X}]");
            }
            return result;
        }

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
        /// Origin Flag
        /// </summary>
        public AxisSignal IO_ORG { get; set; } = new AxisSignal("ORG");

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

#if false
        /// <summary>
        /// 齒輪比分子
        /// </summary>
        public uint GearN1
        {
            get => _gearN1;
            set
            {
                if (value != _gearN1)
                {
                    _gearN1 = value;
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
#endif

        /// <summary>
        /// 目標位置
        /// </summary>
        public double TargetPos { get; set; }

        /// <summary>
        /// 變更位置脈波
        /// </summary>
        public double ChangePosPulse { get; set; }

        /// <summary>
        /// 變更速度脈波
        /// </summary>
        public double ChangeVelPulse { get; set; }

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
            OnPropertyChanged(nameof(IO_ORG));
        }

        /// <summary>
        /// 讀取軸資訊
        /// </summary>
        [Obsolete]
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

        /// <summary>
        /// 設定 Servo Off
        /// </summary>
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
                    throw new InvalidOperationException($"重置位置命令失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"重置錯誤失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 寫入齒輪比
        /// </summary>
        public void SetGearRatio()
        {
            uint result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPU, GearN1);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入電子齒輪比 N1 失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPUDenominator, GearM);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入電子齒輪比 N1 失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"讀取電子齒輪比 N1 失敗: Code[0x{result:X}]");
            }

            GearN1 = ppu;

            result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxPPUDenominator, ref ppum);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取電子齒輪比 M 失敗: Code[0x{result:X}]");
            }
            GearM = ppum;
        }

        /// <summary>
        ///寫入速度參數
        /// </summary>
        public void SetAxisVelParam()
        {
            uint result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelLow, VelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入初始速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelHigh, VelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入目標速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxAcc, Acc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入加速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxDec, Dec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入減速度失敗: Code[0x{result:X}]");
            }
            GetAxisVelParam();
        }

        /// <summary>
        /// 讀取速度參數
        /// </summary>
        public void GetAxisVelParam()
        {
            double axVelLow = 0;
            double axVelHigh = 0;
            double axAcc = 0;
            double axDec = 0;

            uint result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelLow, ref axVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG初始速度失敗: Code[0x{result:X}]");
            }
            VelLow = axVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelHigh, ref axVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG目標速度失敗: Code[0x{result:X}]");
            }
            VelHigh = axVelHigh;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxAcc, ref axAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG加速度失敗: Code[0x{result:X}]");
            }
            Acc = axAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxDec, ref axDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG減速度失敗: Code[0x{result:X}]");
            }
            Dec = axDec;
        }

        /// <summary>
        /// 寫入JOG速度參數
        /// </summary>
        public void SetJogVelParam()
        {
            uint result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelLow, JogVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 JOG 初始速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelHigh, JogVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 JOG 目標速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogAcc, JogAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 JOG 加速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogDec, JogDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 JOG 減速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxJogVLTime, JogVLTime);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 JOG 初速時間失敗: Code[0x{result:X}]");
            }

            GetJogVelParam();
        }

        /// <summary>
        /// 讀取JOG速度參數
        /// </summary>
        public void GetJogVelParam()
        {
            double axJogVelLow = 0;
            double axJogVelHigh = 0;
            double axJogAcc = 0;
            double axJogDec = 0;
            uint axJogVLTime = 0;

            // double axParJogVelLow = 0;
            // double axParJogVelHigh = 0;

            uint result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelLow, ref axJogVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG初始速度失敗: Code[0x{result:X}]");
            }
            JogVelLow = axJogVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelHigh, ref axJogVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG目標速度失敗: Code[0x{result:X}]");
            }
            JogVelHigh = axJogVelHigh;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogAcc, ref axJogAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG加速度失敗: Code[0x{result:X}]");
            }
            JogAcc = axJogAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogDec, ref axJogDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG減速度失敗: Code[0x{result:X}]");
            }
            JogDec = axJogDec;

            result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxJogVLTime, ref axJogVLTime);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG初速時間失敗: Code[0x{result:X}]");
            }
            JogVLTime = axJogVLTime;
        }

        /// <summary>
        /// 設定 HOME 速度參數
        /// </summary>
        public void SetHomeVelParam()
        {
            uint result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeVelLow, HomeVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 HOME 初始速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeVelHigh, HomeVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 HOME 目標速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeAcc, HomeAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 HOME 加速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeDec, HomeDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入 HOME 減速度失敗: Code[0x{result:X}]");
            }

            /// 不支援
            //result = Motion.mAcm_SetU32Property(AxisHandle, (uint)PropertyID.CFG_AxHomeResetEnable, 1);
            //if (result != (uint)ErrorCode.SUCCESS)
            //{
            //    throw new InvalidOperationException($"寫入 HOME Auto Reset 失敗: Code[0x{result:X}]");
            //}

            GetHomeVelParam();
        }

        /// <summary>
        /// 讀取 HOME  速度參數
        /// </summary>
        public void GetHomeVelParam()
        {
            double axHomeVelLow = 0;
            double axHomeVelHigh = 0;
            double axHomeAcc = 0;
            double axHomeDec = 0;
            //uint orgReact = 0;
            //uint homeResetEnable = 0;

            uint result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeVelLow, ref axHomeVelLow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG初始速度失敗: Code[0x{result:X}]");
            }
            HomeVelLow = axHomeVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeVelHigh, ref axHomeVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG目標速度失敗: Code[0x{result:X}]");
            }
            HomeVelHigh = axHomeVelHigh;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeAcc, ref axHomeAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG加速度失敗: Code[0x{result:X}]");
            }
            HomeAcc = axHomeAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxHomeDec, ref axHomeDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取JOG減速度失敗: Code[0x{result:X}]");
            }
            HomeDec = axHomeDec;
        }

        /// <summary>
        /// Jog 開始
        /// </summary>
        public void JogStart()
        {
            uint result = Motion.mAcm_AxSetExtDrive(AxisHandle, 1);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"啟動 Jog 模式失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"停止 Jog 模式失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"觸發順時針 JOG 失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"觸發逆時針 JOG 失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// JOG 停止
        /// </summary>
        public void JogDecAction()
        {
            uint result = Motion.mAcm_AxStopDec(AxisHandle);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"觸發馬達停止失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 原點復歸
        /// </summary>
        /// <param name="homeMode">復歸模式</param>
        public void HomeMove(uint homeMode)
        {
            // Dir: 0 => 正方向, 1 => 負方向
            uint result = Motion.mAcm_AxMoveHome(AxisHandle, homeMode, 0);

            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"啟動原點復歸失敗: Code:[0x{result:X}]");
            }
        }

        /// <summary>
        /// 往負方向找 HOME
        /// </summary>
        public async Task NegativeWayHomeMove()
        {
            if (CurrentStatus == "READY")
            {
                uint result = await Task.Run(() =>
                {
                    uint result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE7_AbsSearch, 1);    // 1 => 先往復方向找 HOME
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    /// 等待碰到 ORG 或 LMTN
                    SpinWait.SpinUntil(() => IO_ORG.BitOn || IO_LMTN.BitOn);
                    if (IO_ORG.BitOn)
                    {
                        return result;
                    }

                    result = Motion.mAcm_AxResetError(AxisHandle);
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE7_AbsSearch, 1);
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    // 等待碰到 ORG
                    SpinWait.SpinUntil(() => IO_ORG.BitOn);
                    return result;
                });


                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"原點復歸過程中發生錯誤: Code[0x{result:X}]");
                }
            }
            else
            {
                throw new InvalidOperationException("伺服軸狀態不允許此操作");
            }
        }

        /// <summary>
        /// 往正方向找 HOME
        /// </summary>
        public async Task PositiveWayHomeMove()
        {
            if (CurrentStatus == "READY")
            {
                uint result = await Task.Run(() =>
                {
                    uint result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE7_AbsSearch, 0);    // 0 => 先往正方向找 HOME
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    /// 等待碰到 ORG 或 LMTN
                    SpinWait.SpinUntil(() => IO_ORG.BitOn || IO_LMTP.BitOn);
                    if (IO_ORG.BitOn)
                    {
                        return result;
                    }

                    result = Motion.mAcm_AxResetError(AxisHandle);
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE7_AbsSearch, 0);
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    // 等待碰到 ORG
                    SpinWait.SpinUntil(() => IO_ORG.BitOn);
                    return result;
                });


                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"原點復歸過程中發生錯誤: Code[0x{result:X}]");
                }
            }
            else
            {
                throw new InvalidOperationException("伺服軸狀態不允許此操作");
            }
        }

        /// <summary>
        /// 觸發位置控制
        /// </summary>
        /// <param name="absolute"></param>
        public void PosMove()
        {
            uint result = Absolute ? Motion.mAcm_AxMoveAbs(AxisHandle, TargetPos) : Motion.mAcm_AxMoveRel(AxisHandle, TargetPos);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達控制位置失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 觸發位置控制
        /// </summary>
        /// <param name="position">目標位置</param>
        /// <param name="absolute">絕對定位</param>
        public void PosMove(bool absolute = false)
        {
            uint result = absolute ? Motion.mAcm_AxMoveAbs(AxisHandle, TargetPos) : Motion.mAcm_AxMoveRel(AxisHandle, TargetPos);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達控制位置失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 變更目標位置
        /// </summary>
        public void ChangePos()
        {
            uint result = Motion.mAcm_AxChangePos(AxisHandle, ChangePosPulse);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"位置控制變更目標位置失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 變更運轉速度
        /// </summary>
        public void ChangeVel()
        {
            uint result = Motion.mAcm_AxChangeVel(AxisHandle, ChangeVelPulse);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"位置控制變更運轉速度失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 馬達停止
        /// </summary>
        public void StopMove()
        {
            uint result = Motion.mAcm_AxStopDec(AxisHandle);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達停止失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// 馬達急停
        /// </summary>
        public void StopEmg()
        {
            uint result = Motion.mAcm_AxStopEmg(AxisHandle);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達緊急停止失敗: Code[0x{result:X}]");
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

        //public event PropertyChangedEventHandler PropertyChanged;

        //private void OnPropertyChanged(string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //public void PropertyChange(string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}
