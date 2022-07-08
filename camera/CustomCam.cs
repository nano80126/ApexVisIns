using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MCAJawIns
{
    interface ICustomCam
    {
        public bool IsOpen { get; }
        public string ConfigName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double ExposureTime { get; set; }
        public double FPS { get; set; }

        public int Frames { get; set; }
        public void CreateCam(string argument);
        public void CreateCam(int argument);
        public void Open();
        public void Close();
    }


    public abstract class CustomCam : ICustomCam, INotifyPropertyChanged
    {
        private int _frames;

        // // // // // // // 
        // 相機物件 ()
        // // // // // // // 

        /// <summary>
        /// If camera is opened
        /// </summary>
        public virtual bool IsOpen { get; }
        /// <summary>
        /// Config Name
        /// </summary>
        public string ConfigName { get; set; } = "Default";
        /// <summary>
        /// Image Width
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Image Height
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Exposure Time
        /// </summary>
        public double ExposureTime { get; set; }
        /// <summary>
        /// FPS
        /// </summary>
        public double FPS { get; set; }
        /// <summary>
        /// Frames
        /// </summary>
        public int Frames
        {
            get => _frames;
            set
            {
                if (value != _frames)
                {
                    _frames = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Create camera function (must be override)
        /// </summary>
        /// <param name="argument">輸入參數(必須被型態轉換)</param>
        public virtual void CreateCam(string argument)
        {
            try
            {
                throw new Exception("This method must be reimplemented");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public virtual void CreateCam(int argument)
        {
            try
            {
                throw new Exception("This method must be reimplemented");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Open camera
        /// </summary>
        public abstract void Open();
        /// <summary>
        /// Close Camera
        /// </summary>
        public abstract void Close();

        // <summary>
        // 手動觸發 Property Changed
        // </summary>
        //public virtual void PropertyChange()
        //{
        //    OnPropertyChanged();
        //}

        /// <summary>
        /// 外部觸發 Property Changed
        /// </summary>
        public virtual void PropertyChange(string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 內部觸發
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
