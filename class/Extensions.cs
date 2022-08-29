using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace MCAJawIns
{
    #region Mat & Bitmap Convert to ImageSource Extension
    public static class ImageSourceTransfer
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern void DeleteObject(IntPtr o);

        #region 效能低下 
        /// <summary>
        /// Low performace
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns> (盡量壓到 10ms 左右)
        public static BitmapSource ToBitmapSource(this Bitmap src)
        {
            BitmapSource bs;
            IntPtr ptr = src.GetHbitmap();
            //long imageSize = src.Width * src.Height * 3;
            //GC.AddMemoryPressure(imageSize);
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ptr);
                //GC.RemoveMemoryPressure(imageSize);
                src.Dispose();
            }
            return bs;
        }
        /// <summary>
        /// Medium performance
        /// </summary>
        /// <param name="src">Source bitmap</param>
        /// <returns></returns>
        public static ImageSource ToBitmapSource2(this Bitmap src)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                src.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        #endregion

        #region 效能較好 Bitmap & Mat convert to ImageSource (High performace)
        /// <summary>
        /// Convert Bitmap to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static ImageSource ToImageSource(this Bitmap src)
        {
            BitmapData bitmapData = src.LockBits(new System.Drawing.Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapSource bitmapSource = BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, bitmapData.Scan0, src.Width * src.Height * 3, src.Width * 3);
            src.UnlockBits(bitmapData);
            return bitmapSource;
        }

        /// <summary>
        /// Convert Mat to ImageSource
        /// </summary>
        /// <param name="src">source image</param>
        /// <returns>Image source</returns>
        public static unsafe ImageSource ToImageSource(this Mat src)
        {
            switch (src.Type().Channels)
            {
                case 1:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Gray8, null, (IntPtr)src.DataPointer, src.Width * src.Height, src.Width);
                case 3:
                    return BitmapSource.Create(src.Width, src.Height, 96, 96, PixelFormats.Bgr24, null, (IntPtr)src.DataPointer, src.Width * src.Height * 3, src.Width * 3);
                default:
                    throw new Exception("Unsupported type");
            }
        }
        #endregion
    }
    #endregion

    #region IEnumerable Get Last Index Extension
    public static class EnumerableExtensions
    {
        public static int LastIndex<T>(this IEnumerable<T> collection)
        {
            return collection.Count() - 1;
        }
    } 
    #endregion
}
