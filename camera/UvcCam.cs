using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCAJawIns
{
    public class UvcCam : CustomCam
    {
        private bool _isGrabbing;

        public UvcCam()
        {
            //
        }

        public UvcCam(int cameraIndex)
        {
            CameraIndex = cameraIndex;
            Camera = new VideoCapture(cameraIndex);
        }

        /// <summary>
        /// UVC 相機
        /// </summary>
        public VideoCapture Camera { get; set; }

        /// <summary>
        /// 相機是否開啟
        /// </summary>
        public override bool IsOpen => Camera != null && Camera.IsOpened();

        /// <summary>
        /// 相機是否 抓取中
        /// </summary>
        public bool IsGrabbing => Camera != null && _isGrabbing;

        /// <summary>
        /// UVC Camera Index
        /// </summary>
        public int CameraIndex { get; set; }

        /// <summary>
        /// Create camera object, call this function before open camera
        /// </summary>
        /// <param name="cameraIndex">Camera index</param>
        public override void CreateCam(int cameraIndex)
        {
            CameraIndex = cameraIndex;
            // Camera = new VideoCapture(cameraIndex);
            Camera = new VideoCapture();    // 帶參數會直接開啟相機
        }

        public override void Open()
        {
            _ = Camera == null
                ? throw new ArgumentException("Camera is a null object, initialize it before calling this function")
                : Camera.Open(CameraIndex, VideoCaptureAPIs.DSHOW);
        }

        public override void Close()
        {
            Camera.Release();
            Camera.Dispose();
            Camera = null;
        }

        /// <summary>
        /// 啟動 Grabber
        /// </summary>
        public void GrabberStart()
        {
            _isGrabbing = true;
            OnPropertyChanged("IsGrabbing");
        }

        /// <summary>
        /// 停止 Grabber
        /// </summary>
        public void GrabberStop()
        {
            _isGrabbing = false;
            OnPropertyChanged("IsGrabbing");
        }
    }

    public class UvcConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
