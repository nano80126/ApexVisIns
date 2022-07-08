using Advantech.Motion;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Data;

namespace MCAJawIns
{

    [Obsolete("因 PCI Card不會常常插拔，故不需要開 LongLifeWorker 來處理")]
    public class MotionEnumer : LongLifeWorker
    {
        private readonly object _ColleciotnLock = new();

        private readonly DEV_LIST[] DEV_LISTs = new DEV_LIST[10];
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
        /// 作業迴圈，
        /// 待優化？
        /// </summary>
        public override void DoWork()
        {
            try
            {
                if (MotionDevices.Count > 0)
                {
                    // WorkerPause();
                    return;
                }

                // 此函數會導致 handle 遺失
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


    /// <summary>
    /// 伺服運動控制
    /// </summary>
    public class ServoMotion : INotifyPropertyChanged, IDisposable
    {
        #region Variables
        private readonly DEV_LIST[] DEV_LISTs = new DEV_LIST[10];
        //private uint DEV_Count;

        // private uint Result;
        private IntPtr DeviceHandle = IntPtr.Zero;
        private readonly IntPtr[] AxisHandles = new IntPtr[4];

        //private double _posCmd;
        //private double _posAct;
        //private string _currentStatus;
        private bool _deviceOpened;

        private System.Timers.Timer _statusTimer;
        private System.Timers.Timer _allAxisTimer;
        private int _sltAxis = -1;
        private bool _disposed;

        private readonly object _deviceColltionLock = new();
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
        public bool StatusTimerOn => _statusTimer.Enabled;

        /// <summary>
        /// 全軸 Status Timer
        /// </summary>
        public bool AllAxisTimerOn => _allAxisTimer.Enabled;

        /// <summary>
        /// 從站 ID 陣列
        /// </summary>
        public ushort[] SlaveIDArray { get; private set; } = new ushort[4];

        /// <summary>
        /// 軸卡 Devices 列表，
        /// 呼叫 ListAvailableDevices 更新
        /// </summary>
        public ObservableCollection<MotionDevice> MotionDevices { get; } = new();
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
                    OnPropertyChanged(nameof(SelectedMotionAxis));
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
        public MotionAxis SelectedMotionAxis => 0 <= _sltAxis && _sltAxis < Axes.Count ? Axes[_sltAxis] : null;

        /// <summary>
        /// 最大軸數
        /// </summary>
        public uint MaxAxisCount { get; private set; }

        /// <summary>
        /// 是否所有軸 ServoOn
        /// </summary>
        public bool AllServoOn => Axes.All(axis => axis != null && axis.IO_SVON.BitOn);

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

        /// <summary>
        /// 啟用 Collecion Binding
        /// </summary>
        public void EnableCollectionBinding()
        {
            BindingOperations.EnableCollectionSynchronization(MotionDevices, _deviceColltionLock);
            BindingOperations.EnableCollectionSynchronization(Axes, _axisColltionLock);
            //Axes.CollectionChanged += Axes_CollectionChanged;
        }

        /// <summary>
        /// 停用 Collection Binding
        /// </summary>
        public void DisableCollectionBinding()
        {
            BindingOperations.DisableCollectionSynchronization(MotionDevices);
            BindingOperations.DisableCollectionSynchronization(Axes);
            //Axes.CollectionChanged -= Axes_CollectionChanged;
        }

        //private void Axes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    // if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        //    // {
        //    //     return;
        //    // }
        //}

        /// <summary>
        /// 列出可用的 Devices (Motion Cards)
        /// </summary>
        public void ListAvailableDevices(bool checkDll = false)
        {
            bool dllValid = checkDll ? CheckDllVersion() : true;

            if (dllValid)
            {
                if (!_deviceOpened)
                {
                    uint devCount = 0;
                    int result = Motion.mAcm_GetAvailableDevs(DEV_LISTs, 10, ref devCount);

                    if (result != (int)ErrorCode.SUCCESS)
                    {
                        throw new Exception($"取得 EtherCAT Cards 失敗: Code[0x{result:X}]");
                    }

                    lock (_deviceColltionLock)
                    {
                        MotionDevices.Clear();
                        for (int i = 0; i < devCount; i++)
                        {
                            MotionDevices.Add(new MotionDevice(DEV_LISTs[i]));
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Device 開啟時不允許此操作");
                }
            }
            else
            {
                throw new DllNotFoundException("MOTION 控制驅動未安裝或版本不符");
            }
        }

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

                lock (_axisColltionLock)
                {
                    Axes.Clear();
                    for (int i = 0; i < MaxAxisCount; i++)
                    {
                        // result = Motion.mAcm_AxOpen(DeviceHandle, (ushort)i, ref AxisHandles[i]);
                        Axes.Add(new MotionAxis(DeviceHandle, i));
                        result = Axes[i].AxisOpen(out AxisHandles[i]);

                        if (result != (int)ErrorCode.SUCCESS)
                        {
                            throw new Exception($"開啟軸失敗: Code[0x{result:X}]");
                        }
                    }
                }

                // 重置所有軸 Error // 另外開 Method
                //foreach (MotionAxis axis in Axes)
                //{
                //    axis.ResetError();
                //}

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
                            // Debug.WriteLine($"{info.SlaveID} {info.Name} {info.RevisionNo}");
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
                    HomeModes.Add(new HomeMode("Positive Direction", 0));
                    HomeModes.Add(new HomeMode("Negative Direction", 1));

#if false
                    //foreach (Advantech.Motion.HomeMode mode in Enum.GetValues(typeof(Advantech.Motion.HomeMode)))
                    //{
                    //    HomeModes.Add(new HomeMode(mode.ToString(), (uint)mode));
                    //}

                    //foreach (Cia402HomeMode mode in Enum.GetValues(typeof(Cia402HomeMode)))
                    //{
                    //    HomeModes.Add(new HomeMode(mode.ToString(), (uint)mode));
                    //}  
#endif
                }

#if false
                // Get Di maximum number of this channel // 目前不知道要做啥    // 大概沒用
                // result = Motion.mAcm_GetU32Property(DeviceHandle, (uint)PropertyID.FT_DaqDiMaxChan, ref DiChCount);
                // if (result != (uint)ErrorCode.SUCCESS)
                // {
                //     throw new Exception($"讀取屬性 FT_DaqDiMaxChan 失敗: Code[0x{result:X}]");
                // }

                //// 這要幹麻? 
                //for (int i = 0; i < DiChCount; i++)
                //{
                //    Debug.WriteLine($"{i}");
                //}
                //Debug.WriteLine(DiChCount);  
#endif

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

#if false
                    // 讀取軸狀態
                    _ = Motion.mAcm_AxGetState(AxisHandles[i], ref AxisState[i]);

                    if (AxisState[i] == (uint)Advantech.Motion.AxisState.STA_AX_ERROR_STOP)
                    {
                        // 若軸狀態為Error，重置軸狀態
                        _ = Motion.mAcm_AxResetError(AxisHandles[i]);
                    } 
#endif

                    // 命令軸減速至停止
                    _ = Motion.mAcm_AxStopDec(AxisHandles[i]);
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
        /// 重置全部軸錯誤
        /// </summary>
        public void ResetAllError()
        {
            if (!DeviceOpened)
            {
                return;
            }

            try
            {
                for (int i = 0; i < Axes.Count; i++)
                {
                    Axes[i].ResetError();
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
            if (_statusTimer == null)
            {
                _statusTimer = new System.Timers.Timer(interval)
                {
                    AutoReset = true
                };

                _statusTimer.Elapsed += StatusTimer_Elapsed;
                _statusTimer.Start();
            }
            else
            {
                _statusTimer.Interval = interval;
                _statusTimer.Start();
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
            //OnPropertyChanged(nameof(AllServoOn));
        }

        /// <summary>
        /// 關閉 Timer
        /// </summary>
        public void DisableTimer()
        {
            _statusTimer?.Stop();
        }

        /// <summary>
        /// 啟用全軸 Timer
        /// </summary>
        public void EnableAllTimer(int interval)
        {
            if (_allAxisTimer == null)
            {
                _allAxisTimer = new System.Timers.Timer(interval)
                {
                    AutoReset = true
                };

                _allAxisTimer.Elapsed += AllAxisTimer_Elapsed;
                _allAxisTimer.Start();
            }
            else
            {
                _allAxisTimer.Interval = interval;
                _allAxisTimer.Start();
            }
        }

        /// <summary>
        /// All Timer Tick Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllAxisTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //throw new NotImplementedException() 
            for (int i = 0; i < Axes.Count; i++)
            {
                //Debug.WriteLine($"Axis-{i}");
                GetMotionState(i);
                GetMotionPos(i);
                GetMotionIO(i);
            }
        }

        /// <summary>
        /// 停用全軸 Timer
        /// </summary>
        public void DisableAllTimer()
        {
            _allAxisTimer?.Stop();
        }

        public void GetMotionState(int axis)
        {
            ushort axState = 0;
            uint result = Motion.mAcm_AxGetState(AxisHandles[axis], ref axState);

            if (result == (uint)ErrorCode.SUCCESS)
            {
                Axes[axis].CurrentStatus = $"{(AxisState)axState}";
                Axes[axis].PropertyChange("CurrentStatus");
            }
        }

        /// <summary>
        /// 取得 Motion IO 狀態
        /// </summary>
        /// <param name="axis"></param>
        public void GetMotionIO(int axis)
        {
            uint IOStatus = 0;
            uint result = Motion.mAcm_AxGetMotionIO(AxisHandles[axis], ref IOStatus);
            //Debug.WriteLine($"{result:X} {IOStatus}");

            if (result == (uint)ErrorCode.SUCCESS)
            {
                Axes[axis].IO_SRDY.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == (uint)Ax_Motion_IO.AX_MOTION_IO_RDY;
                Axes[axis].IO_ALM.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ALM) == (uint)Ax_Motion_IO.AX_MOTION_IO_ALM;
                Axes[axis].IO_LMTP.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP;
                Axes[axis].IO_LMTN.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN;
                Axes[axis].IO_SVON.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) == (uint)Ax_Motion_IO.AX_MOTION_IO_SVON;
                Axes[axis].IO_EMG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_EMG) == (uint)Ax_Motion_IO.AX_MOTION_IO_EMG;
                Axes[axis].IO_ORG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ORG) == (uint)Ax_Motion_IO.AX_MOTION_IO_ORG;
                Axes[axis].PropertyChange("IO_SRDY");
                Axes[axis].PropertyChange("IO_ALM");
                Axes[axis].PropertyChange("IO_LMTP");
                Axes[axis].PropertyChange("IO_LMTN");
                Axes[axis].PropertyChange("IO_EMG");
                Axes[axis].PropertyChange("IO_ORG");
            }
        }

        /// <summary>
        /// 取得 Motion Actual Position
        /// </summary>
        /// <param name="axis"></param>
        public void GetMotionPos(int axis)
        {
            //double cmd = 0;
            double pos = 0;

            uint result = Motion.mAcm_AxGetActualPosition(AxisHandles[axis], ref pos);

            if (result == (uint)ErrorCode.SUCCESS)
            {
                Axes[axis].PosActual = pos;
            }
        }

        /// <summary>
        /// 取得 Motion Information，
        /// MotionTab 使用
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

                SelectedMotionAxis.PosCommand = cmd;
                SelectedMotionAxis.PosActual = pos;

                //Debug.WriteLine($"{(IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_DIR) == (uint)Ax_Motion_IO.AX_MOTION_IO_DIR}");
                result = Motion.mAcm_AxGetMotionIO(AxisHandles[_sltAxis], ref IOStatus);
                if (result == (uint)ErrorCode.SUCCESS)
                {
                    SetMotionIOStatus(IOStatus);
                    SelectedMotionAxis.UpdateIO(); // 觸發 PropertyUpdate
                    OnPropertyChanged(nameof(AllServoOn));  // 觸發 PropertyUpdate
                }

                // Get Axis current state
                Motion.mAcm_AxGetState(AxisHandles[_sltAxis], ref axState);
                // CurrentStatus = $"{(AxisState)axState}";
                SelectedMotionAxis.CurrentStatus = $"{(AxisState)axState}";
                OnPropertyChanged(nameof(SelectedMotionAxis));
            }
        }

        /// <summary>
        /// 更新選擇軸 Servo IO Status
        /// </summary>
        /// <param name="IOStatus"></param>
        private void SetMotionIOStatus(uint IOStatus)
        {
            SelectedMotionAxis.IO_SRDY.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_RDY) == (uint)Ax_Motion_IO.AX_MOTION_IO_RDY;
            SelectedMotionAxis.IO_ALM.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ALM) == (uint)Ax_Motion_IO.AX_MOTION_IO_ALM;
            SelectedMotionAxis.IO_LMTP.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTP;
            SelectedMotionAxis.IO_LMTN.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN) == (uint)Ax_Motion_IO.AX_MOTION_IO_LMTN;
            // SVON Bit & ServoOn Flag
            //SelectedMotionAxis.IO_SVON.BitOn = SelectedMotionAxis.ServoOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) == (uint)Ax_Motion_IO.AX_MOTION_IO_SVON;
            SelectedMotionAxis.IO_SVON.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_SVON) == (uint)Ax_Motion_IO.AX_MOTION_IO_SVON;
            SelectedMotionAxis.IO_EMG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_EMG) == (uint)Ax_Motion_IO.AX_MOTION_IO_EMG;
            SelectedMotionAxis.IO_ORG.BitOn = (IOStatus & (uint)Ax_Motion_IO.AX_MOTION_IO_ORG) == (uint)Ax_Motion_IO.AX_MOTION_IO_ORG;
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

                if (_statusTimer != null)
                {
                    _statusTimer.Stop();
                    _statusTimer.Dispose();
                    _statusTimer = null;
                }
            }

            _disposed = true;
        }
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
        private bool _zeroReturning;
        private bool _zeroReturned;

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
        public string AxisName => $"{AxisIndex}-Axis";

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
        /// 原點復歸中
        /// </summary>
        public bool ZeroReturning
        {
            get => _zeroReturning;
            private set
            {
                if (value != _zeroReturning)
                {
                    _zeroReturning = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 原點復歸完成
        /// </summary>
        public bool ZeroReturned
        {
            get => _zeroReturned;
            private set
            {
                if (value != _zeroReturned)
                {
                    _zeroReturned = value;
                    OnPropertyChanged();
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
                // 重置所有IO
                IO_SRDY.BitOn = IO_SVON.BitOn = IO_LMTP.BitOn = IO_LMTN.BitOn = IO_ALM.BitOn = IO_ORG.BitOn = false;
                UpdateIO();
                IsAxisOpen = false;
                handle = AxisHandle;
            }
            return result;
        }

        /// <summary>
        /// Servo Oo Flag
        /// </summary>
        [Obsolete("待確認是否使用")]
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
        /// 更新 Servo IO Status
        /// </summary>
        /// <param name="IOStatus"></param>
        [Obsolete("待測試")]
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
            //if (!ServoOn)
            if (!IO_SVON.BitOn)
            {
                // Servo On augu 2 => 1
                result = Motion.mAcm_AxSetSvOn(AxisHandle, 1);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"{AxisIndex}-Axis Servo On 失敗: Code[0x{result:X}]");
                }
                //ServoOn = true;
                IO_SVON.BitOn = true;
            }
        }

        /// <summary>
        /// 設定 Servo Off
        /// </summary>
        public void SetServoOff()
        {
            uint result;
            if (IO_SVON.BitOn)
            {
                // Servo Off augu 2 => 0
                result = Motion.mAcm_AxSetSvOn(AxisHandle, 0);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new Exception($"{AxisIndex}-Axis Servo Off 失敗: Code[0x{result:X}]");
                }
                IO_SVON.BitOn = false;
            }
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        public void ResetPos()
        {
            double cmdPos = 0;
            // 先確認 Servo On
            //if (ServoOn)
            if (IO_SVON.BitOn)
                {
                uint result = Motion.mAcm_AxSetCmdPosition(AxisHandle, cmdPos);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new InvalidOperationException($"重置位置命令失敗: Code[0x{result:X}]");
                }
            }
        }

        /// <summary>
        /// 嘗試 Reset Servo Position 
        /// </summary>
        /// <returns>Return Error Code or 1 (不為Servo On 狀態)</returns>
        public uint TryResetPos()
        {
            double cmdPos = 0;
            return IO_SVON.BitOn ? Motion.mAcm_AxSetCmdPosition(AxisHandle, cmdPos) : 1;
        }

        /// <summary>
        /// 重置錯誤
        /// </summary>
        public void ResetError()
        {
            uint result = Motion.mAcm_AxResetError(AxisHandle);
            //Debug.WriteLine($"{AxisHandle} {result}");
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
        /// 寫入速度參數 (With Argument)
        /// </summary>
        /// <param name="Vlow">初始速度</param>
        /// <param name="Vhigh">目標速度</param>
        /// <param name="acc">加速度</param>
        /// <param name="dec">減速度</param>
        public void SetAxisVelParam(double Vlow, double Vhigh, double acc, double dec)
        {
            uint result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelLow, Vlow);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入初始速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelHigh, Vhigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入目標速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxAcc, acc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"寫入加速度失敗: Code[0x{result:X}]");
            }

            result = Motion.mAcm_SetF64Property(AxisHandle, (uint)PropertyID.PAR_AxDec, dec);
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
                throw new InvalidOperationException($"讀取初始速度失敗: Code[0x{result:X}]");
            }
            VelLow = axVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxVelHigh, ref axVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取目標速度失敗: Code[0x{result:X}]");
            }
            VelHigh = axVelHigh;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxAcc, ref axAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取加速度失敗: Code[0x{result:X}]");
            }
            Acc = axAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.PAR_AxDec, ref axDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取減速度失敗: Code[0x{result:X}]");
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
                throw new InvalidOperationException($"讀取 JOG 初始速度失敗: Code[0x{result:X}]");
            }
            JogVelLow = axJogVelLow;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogVelHigh, ref axJogVelHigh);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取 JOG 目標速度失敗: Code[0x{result:X}]");
            }
            JogVelHigh = axJogVelHigh;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogAcc, ref axJogAcc);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取 JOG 加速度失敗: Code[0x{result:X}]");
            }
            JogAcc = axJogAcc;

            result = Motion.mAcm_GetF64Property(AxisHandle, (uint)PropertyID.CFG_AxJogDec, ref axJogDec);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取 JOG 減速度失敗: Code[0x{result:X}]");
            }
            JogDec = axJogDec;

            result = Motion.mAcm_GetU32Property(AxisHandle, (uint)PropertyID.CFG_AxJogVLTime, ref axJogVLTime);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"讀取 JOG 初速時間失敗: Code[0x{result:X}]");
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

        public void ChangeZeroReturned(bool zeroReturned)
        {
            ZeroReturned = zeroReturned;
        }

        /// <summary>
        /// 往負方向找 HOME
        /// </summary>
        public async Task NegativeWayHomeMove(bool setPosZero = false)
        {
            if (CurrentStatus == "READY")
            {
                uint result = await Task.Run(() =>
                {
                    ZeroReturning = true;
                    ZeroReturned = false;

                    // MODE9，往負方向找HOME， 過ORG 後停在 EZ
                    uint result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE9_AbsSearch_Ref, 1);    // 1 => 先往負方向找 HOME
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    // 等待一次 Timer Tick
                    SpinWait.SpinUntil(() => false, 150);
                    /// 等待碰到 ORG 或 LMTN
                    SpinWait.SpinUntil(() => IO_ORG.BitOn || IO_LMTN.BitOn);
                    if (IO_LMTN.BitOn)
                    {
                        // 重置錯誤
                        result = Motion.mAcm_AxResetError(AxisHandle);
                        if (result != (uint)ErrorCode.SUCCESS)
                        {
                            return result;
                        }

                        // 啟動復歸
                        result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE9_AbsSearch_Ref, 1);     // 1 => 負方向
                        if (result != (uint)ErrorCode.SUCCESS)
                        {
                            return result;
                        }

                        // 等待碰到 ORG && 軸狀態為READY
                        SpinWait.SpinUntil(() => IO_ORG.BitOn);
                    }

                    return result;
                }).ContinueWith(t =>
                {
                    if (t.Result == (uint)ErrorCode.SUCCESS)
                    {
                        uint result = (uint)ErrorCode.SUCCESS;
                        if (setPosZero)
                        {
                            SpinWait.SpinUntil(() => CurrentStatus == "READY");
                            double cmdPos = 0;
                            result = Motion.mAcm_AxSetCmdPosition(AxisHandle, cmdPos);

                            // 可有可無
                            // if (result != (uint)ErrorCode.SUCCESS)
                            // {
                            //     return result;
                            // }
                        }
                        return result;
                    }
                    return t.Result;
                });


                if (result != (uint)ErrorCode.SUCCESS)
                {
                    ZeroReturning = false;
                    throw new Exception($"原點復歸過程中發生錯誤: Code[0x{result:X}]");
                }
                else
                {
                    ZeroReturning = false;
                    ZeroReturned = true;
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
        public async Task PositiveWayHomeMove(bool setPosZero = false)
        {
            if (CurrentStatus == "READY")
            {
                uint result = await Task.Run(() =>
                {
                    ZeroReturned = false;
                    ZeroReturning = true;

                    // MODE9，往正方向找HOME， 過ORG 後停在 EZ
                    uint result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE9_AbsSearch_Ref, 0);    // 0 => 先往正方向找 HOME
                    if (result != (uint)ErrorCode.SUCCESS)
                    {
                        return result;
                    }

                    // 等待一次 Timer Tick
                    SpinWait.SpinUntil(() => false, 150);
                    // 等待碰到 ORG 或 LMTP
                    SpinWait.SpinUntil(() => IO_ORG.BitOn || IO_LMTP.BitOn);
                    if (IO_LMTP.BitOn)
                    {
                        // 重置錯誤
                        result = Motion.mAcm_AxResetError(AxisHandle);
                        if (result != (uint)ErrorCode.SUCCESS)
                        {
                            return result;
                        }

                        // 啟動復歸
                        result = Motion.mAcm_AxMoveHome(AxisHandle, (uint)HomeMode.MODE9_AbsSearch_Ref, 0);
                        if (result != (uint)ErrorCode.SUCCESS)
                        {
                            return result;
                        }
                        // 等待碰到 ORG && 軸狀態為READY
                        SpinWait.SpinUntil(() => IO_ORG.BitOn);
                    }

                    return result;
                }).ContinueWith(t =>
                {
                    if (t.Result == (uint)ErrorCode.SUCCESS)
                    {
                        uint result = (uint)ErrorCode.SUCCESS;
                        if (setPosZero)
                        {
                            SpinWait.SpinUntil(() => CurrentStatus == "READY");
                            double cmdPos = 0;
                            result = Motion.mAcm_AxSetCmdPosition(AxisHandle, cmdPos);

                            // 可有可無
                            // if (result != (uint)ErrorCode.SUCCESS)
                            // {
                            //     return result;
                            // }
                        }
                        return result;
                    }
                    return t.Result;
                });

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    ZeroReturning = false;
                    throw new Exception($"原點復歸過程中發生錯誤: Code[0x{result:X}]");
                }
                else
                {
                    ZeroReturning = false;
                    ZeroReturned = true;
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
        [Obsolete("待刪除")]
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
        /// 直接觸發位置控制
        /// </summary>
        /// <param name="targetPos">(double)目標位置</param>
        /// <param name="absolute">(bool)絕對位置</param>
        public void PosMove(double targetPos, bool absolute = false)
        {
            uint result = absolute ? Motion.mAcm_AxMoveAbs(AxisHandle, targetPos) : Motion.mAcm_AxMoveRel(AxisHandle, targetPos);

            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達控制位置失敗: Code[0x{result:X}]");
            }
            // Motion.macm_axmove
        }

        /// <summary>
        /// (try) 觸發位置控制
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="absolute"></param>
        /// <returns></returns>
        public uint TryPosMove(double targetPos, bool absolute = false)
        {
            //TargetPos = targetPos;
            return absolute ? Motion.mAcm_AxMoveAbs(AxisHandle, targetPos) : Motion.mAcm_AxMoveRel(AxisHandle, targetPos);
        }

        /// <summary>
        /// 位置移動 (可等候)
        /// </summary>
        /// <param name="targetPos">(double)目標位置</param>
        /// <param name="absolute">(bool)絕對位置</param>
        /// <returns></returns>
        public async Task PosMoveAsync(double targetPos, bool absolute = false)
        {
            await Task.Run(() =>
            {
                //TargetPos = targetPos;
                uint result = absolute ? Motion.mAcm_AxMoveAbs(AxisHandle, targetPos) : Motion.mAcm_AxMoveRel(AxisHandle, targetPos);

                if (result != (uint)ErrorCode.SUCCESS)
                {
                    throw new InvalidOperationException($"伺服馬達控制位置失敗: Code[0x{result:X}]");
                }

                // 等待一次 Timer Tick
                SpinWait.SpinUntil(() => false, 120);

                // 等待軸狀態變為READY
                SpinWait.SpinUntil(() => CurrentStatus == "READY");
            });
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
        /// 變更運轉速度
        /// </summary>
        public void ChangeVel(double vel)
        {
            uint result = Motion.mAcm_AxChangeVel(AxisHandle, vel);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"位置控制變更運轉速度失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// (try) 變更運轉速度
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public uint TryChangeVel(double vel)
        {
            return Motion.mAcm_AxChangeVel(AxisHandle, vel);
        }


        /// <summary>
        /// 速度控制
        /// </summary>
        /// <param name="direction">0: 正, 1: 反</param>
        public void VelMove(ushort direction)
        {
            uint result = Motion.mAcm_AxMoveVel(AxisHandle, direction);
            if (result != (uint)ErrorCode.SUCCESS)
            {
                throw new InvalidOperationException($"伺服馬達速度控制失敗: Code[0x{result:X}]");
            }
        }

        /// <summary>
        /// (try) 定速控制
        /// </summary>
        /// <param name="direction">0: 正, 1: 反</param>
        /// <returns></returns>
        public uint TryVelMove(ushort direction)
        {
            return Motion.mAcm_AxMoveVel(AxisHandle, direction);
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
