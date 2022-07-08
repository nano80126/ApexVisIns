using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading;
using System.Windows.Data;

namespace MCAJawIns
{
    public class SerialEnumer : LongLifeWorker
    {
        private readonly object _CollectionLock = new();

        public ObservableCollection<string> ComPortSource { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 初始化旗標
        /// </summary>
        //public LongLifeWorker.InitFlags InitFlag { get; private set; } = InitFlags.Starting;

        private void ComPortSourceAdd(string comPort)
        {
            lock (_CollectionLock)
            {
                ComPortSource.Add(comPort);
            }
        }

        private void ComPortSourceClear()
        {
            lock (_CollectionLock)
            {
                ComPortSource.Clear();
            }
        }

        /// <summary>
        /// 工作開始，且 Collection 同步綁定
        /// </summary>
        public override void WorkerStart()
        {
            BindingOperations.EnableCollectionSynchronization(ComPortSource, _CollectionLock);
            base.WorkerStart();
        }

        /// <summary>
        /// 工作結束，且 Collection 取消綁定
        /// </summary>
        public override void WorkerEnd()
        {
            BindingOperations.DisableCollectionSynchronization(ComPortSource);
            base.WorkerEnd();
        }

        public override void DoWork()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();

                if (portNames.Length == 0)
                {
                    ComPortSourceClear();
                    InitFlag = InitFlags.Finished;
                    _ = SpinWait.SpinUntil(() => CancellationTokenSource.IsCancellationRequested, 3000);
                }

                foreach (string com in portNames)
                {
                    if (!ComPortSource.Contains(com))
                    {
                        ComPortSourceAdd(com);
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
        /// ComPort 數量
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ComPortSource.Count;
        }

        protected override void Dispose(bool disposing)
        {
            ComPortSource.Clear();

            base.Dispose(disposing);
        }
    }
}
