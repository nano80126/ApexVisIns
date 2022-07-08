using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCAJawIns.Algorithm
{
    public abstract class Algorithm : IDisposable
    {
        /// <summary>
        /// Source Image
        /// </summary>
        public readonly Mat srcImg;

        /// <summary>
        /// Image
        /// </summary>
        public Mat img;

        /// <summary>
        /// Queue Actions
        /// </summary>
        protected Queue<Action> DrawActions { get; set; } = new Queue<Action>();

        protected bool _disposed;

        protected Algorithm()
        {
            srcImg = new(@"./Image/sample.jpg");
            img = srcImg.Clone();
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="bmp"></param>
        protected Algorithm(Bitmap bmp)
        {
            srcImg = bmp.ToMat();
            img = srcImg.Clone();
            bmp.Dispose();
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="mat"></param>
        protected Algorithm(Mat mat)
        {
            srcImg = mat;
            img = srcImg.Clone();
            //mat.Dispose();
        }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="path"></param>
        protected Algorithm(string path)
        {
            srcImg = new Mat(path);
            img = srcImg.Clone();
        }

        public Mat GetMat()
        {
            return img;
        }

        public Bitmap GetBmp()
        {
            return img.ToBitmap();
        }

        public void Restore()
        {
            img.Dispose();
            img = srcImg.Clone();
        }

        public void ActionAdd(Action act)
        {
            DrawActions.Enqueue(act);
        }

        public void DoActions()
        {
            while (DrawActions.Count > 0)
            {
                Action act = DrawActions.Dequeue();
                act();
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                throw new NotImplementedException();
            }
            _disposed = true;
        }
    }
}
