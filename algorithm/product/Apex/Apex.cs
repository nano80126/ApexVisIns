using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using System.Drawing;
using OpenCvSharp;
using System.Diagnostics;

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// Apex 處理,
        /// 保留做為參考
        /// </summary>
        /// <param name="mat">來源影像</param>
        public void ProcessApex(Mat mat)
        {
            int matWidth = mat.Width;
            int matHeight = mat.Height;

            Algorithm.Apex img = new(mat);

            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (OnTabIndex == 0)
                    {
                        ImageSource = img.GetMat().ToImageSource();
                    }
                });
            }
            catch (OpenCVException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCV, ex.Message);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            catch (OpenCvSharpException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCVS, ex.Message);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.EX, ex.Message);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            finally
            {
                img.Dispose();
            }
        }
    }
}


namespace ApexVisIns.Algorithm
{
    public class Apex : Algorithm
    {
        public Apex() { }

        public Apex(Bitmap bmp) : base(bmp) { }

        public Apex(Mat mat) : base(mat) { }

        public Apex(string path) : base(path) { }

        /// <summary>
        /// 取得 ROI 銳化影像
        /// </summary>
        /// <param name="roi"></param>
        public void GetSharpROI(Rect roi)
        {
            try
            {
                using Mat clone = new(img, roi);
                using Mat sharp = new();

                InputArray kernel = InputArray.Create(new int[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } });
                Cv2.Filter2D(clone, sharp, MatType.CV_8U, kernel, new Point(-1, -1), 0);

                Cv2.ImShow("sharp", sharp);
            }
            catch (OpenCVException)
            {
                throw;
            }
            catch (OpenCvSharpException)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得ROI Canny影像
        /// </summary>
        /// <param name="roi">ROI</param>
        /// <param name="th1">閾值 1</param>
        /// <param name="th2">閾值 2</param>
        public void GetCannyROI(Rect roi, byte th1, byte th2)
        {
            try
            {
                using Mat clone = new(img, roi);
                using Mat blur = new();
                Mat canny = new();

                Cv2.BilateralFilter(clone, blur, 5, 50, 100);
                Cv2.Canny(blur, canny, th1, th2, 3);

                //Cv2.ImShow("blur", blur);
                Cv2.ImShow("canny", canny);
            }
            catch (OpenCVException)
            {
                throw;
            }
            catch (OpenCvSharpException)
            {
                throw;
            }
        }

        public Mat GetBackgroundMogROI(Rect roi, int blurSize)
        {
            try
            {
                using Mat clone = new(img, roi);

                BackgroundSubtractorMOG2 mog = BackgroundSubtractorMOG2.Create();
                Mat mask = new(roi.Height, roi.Width, MatType.CV_8UC1, Scalar.Black);
                using Mat blur = new(roi.Height, roi.Width, MatType.CV_8UC1, Scalar.Black);

                Cv2.MedianBlur(clone, blur, blurSize);

                mog.Apply(clone, mask);
                mog.Apply(blur, mask);

                return mask;
            }
            catch (OpenCVException)
            {
                throw;
            }
            catch (OpenCvSharpException)
            {
                throw;
            }
        }

        public Mat Threshbold_mog(Mat mat, OpenCvSharp.Size window)
        {
            try
            {
                BackgroundSubtractorMOG2 bgModel = BackgroundSubtractorMOG2.Create();
                Mat fgMask = new Mat();
                Mat output = new Mat(mat.Rows, mat.Cols, MatType.CV_8UC1);

                for (int y = 0; y < mat.Rows - window.Height; y++)
                {
                    for (int x = 0; x < mat.Cols - window.Width; x++)
                    {
                        Mat m = new Mat(mat, new Rect(x, y, window.Width, window.Height));
                        bgModel.Apply(m, fgMask);
                    }
                }

                for (int y = 0; y < mat.Rows - window.Height; y++)
                {
                    for (int x = 0; x < mat.Cols - window.Width; x++)
                    {
                        Mat region = new Mat(mat, new Rect(x, y, window.Width, window.Height));

                        bgModel.Apply(region, fgMask, 0);

                        fgMask.CopyTo(output);
                    }
                }

                return output;
            }
            catch (OpenCVException)
            {
                throw;
            }
            catch (OpenCvSharpException)
            {
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                srcImg.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Apex 處理方法
    /// </summary>
    public class ApexProcess
    {

        public static bool CheckTubeAnglePos(Mat src, out double x1Gap, out double x2Gap, out double x1Center, out double x2Center)
        {
            x1Gap = x2Gap = -1;
            x1Center = x2Center = -1;

            #region 上半
            Rect roi1 = new(100, 580, 1000, 120);

            Methods.GetRoiCanny(src, roi1, 75, 150, out Mat canny);

            Methods.GetHoughVerticalXPos(canny, out int count, out double[] pos, 3);

            if (count == 4)
            {
                x1Gap = Math.Abs((pos[0] + pos[3] - pos[1] - pos[2]) / 2);
                x1Center = (pos[0] + pos[3]) / 2;
            }
            else
            {
                return false;
            }
            #endregion

            #region 下半
            OpenCvSharp.Rect roi2 = new OpenCvSharp.Rect(100, 1260, 1000, 120);

            Methods.GetRoiCanny(src, roi2, 75, 150, out Mat canny2);

            Methods.GetHoughVerticalXPos(canny2, out count, out pos, 3);

            if (count == 4)
            {
                x2Gap = Math.Abs((pos[0] + pos[3] - pos[1] - pos[2]) / 2);
                x2Center = (pos[0] + pos[3]) / 2;
            }
            else
            {
                return false;
            }
            #endregion

            return true;
        }

        /// <summary>
        /// 取得窗戶寬度
        /// </summary>
        /// <param name="src">源影像</param>
        public static bool GetWindowWidth(Mat src, out double width1, out double width2)
        {
            width1 = 0;
            width2 = 0;
            #region 上半
            Rect roi1 = new(100, 580, 1000, 120);
            Methods.GetRoiCanny(src, roi1, 75, 150, out Mat canny);

            if (!Methods.GetVertialWindowWidth(canny, out width1))
            {
                return false;
            }
            #endregion

            #region 下半
            Rect roi2 = new(100, 580, 1000, 120);
            Methods.GetRoiCanny(src, roi2, 75, 150, out Mat canny2);

            if (!Methods.GetVertialWindowWidth(canny2, out width2))
            {
                return false;
            }
            return true;
            #endregion
        }
    }
}