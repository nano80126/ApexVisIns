using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Timers;

namespace ApexVisIns
{
    //public class ShilinSDE : INotifyPropertyChanged, IDisposable
    //{
    //    #region private field
    //    private bool _dispossed;

    //    private SerialPort _serialPort;

    //    private Timer _pollingTimer;
    //    private double _interval = 100;

    //    #endregion



    //    public event PropertyChangedEventHandler PropertyChanged;

    //    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    //    {
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }


    //    public void Dispose()
    //    {
    //        // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!_dispossed)
    //        {
    //            if (disposing)
    //            {
    //                // TODO: 處置受控狀態 (受控物件)
    //            }

    //            // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
    //            // TODO: 將大型欄位設為 Null
    //            _dispossed = true;
    //        }
    //    }
    //}
}
