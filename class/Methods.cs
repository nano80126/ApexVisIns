using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCAJawIns
{
    public class Methods
    {
        /// <summary>
        /// 取得 Canny 影像
        /// </summary>
        /// <param name="roi"></param>
        /// <param name="th1">閾值 1</param>
        /// <param name="th2">閾值 2</param>
        /// <param name="canny">(out) canny 圖像</param>
        public static void GetCanny(Mat roi, byte th1, byte th2, out Mat canny)
        {
            try
            {
                canny = new Mat();

                using Mat blur = new();
                Cv2.BilateralFilter(roi, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3);
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
        /// 取得 ROI Canny 影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">roi 方形區域</param>
        /// <param name="th1">閾值 1</param>
        /// <param name="th2">閾值 2</param>
        /// <param name="canny">(out) canny 圖像</param>
        public static void GetRoiCanny(Mat src, Rect roi, byte th1, byte th2, out Mat canny)
        {
            try
            {
                canny = new Mat();

                using Mat clone = new(src, roi);
                using Mat blur = new();

                Cv2.BilateralFilter(clone, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3);
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
        /// 取得二值影像
        /// </summary>
        public static void GetBinarization(Mat src, byte th, byte max, out Mat binar)
        {
            try
            {
                binar = new Mat();

                Cv2.Threshold(src, binar, 0, 255, ThresholdTypes.Binary);
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
        /// 取得 ROI 二值化影像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="th"></param>
        /// <param name="max"></param>
        /// <param name="binar"></param>
        public static void GetRoiBinarization(Mat src, Rect roi, byte th, byte max, out Mat binar)
        {
            try
            {
                binar = new Mat();
                using Mat clone = new(src, roi);

                Cv2.Threshold(clone, binar, th, max, ThresholdTypes.Binary);
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
        /// 取得 Otsu 影像、閾值
        /// </summary>
        /// <param name="src"></param>
        /// <param name="th"></param>
        /// <param name="max"></param>
        /// <param name="otsu"></param>
        /// <param name="threshHold"></param>
        public static void GetOtsu(Mat src, byte th, byte max, out Mat otsu, out byte threshHold)
        {
            try
            {
                otsu = new Mat();
                // using Mat clone = new(src, roi);
                threshHold = (byte)Cv2.Threshold(src, otsu, th, max, ThresholdTypes.Otsu);
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
        /// 取得 ROI Otsu 影像、閾值
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">roi 方形區域</param>
        /// <param name="th">閾值</param>
        /// <param name="max">最大值</param>
        /// <param name="otsu">Otsu 影像</param>
        /// <param name="threshold">Otsu 閾值</param>
        public static void GetRoiOtsu(Mat src, Rect roi, byte th, byte max, out Mat otsu, out byte threshold)
        {
            try
            {
                otsu = new Mat();
                using Mat clone = new(src, roi);

                threshold = (byte)Cv2.Threshold(clone, otsu, th, max, ThresholdTypes.Otsu);
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
        /// 取得 ROI 高斯
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="kernelSize"></param>
        /// <param name="sigmaX"></param>
        /// <param name="sigmaY"></param>
        /// <param name="blur"></param>
        public static void GetRoiGaussianBlur(Mat src, Rect roi, Size kernelSize, double sigmaX, double sigmaY, out Mat blur)
        {
            try
            {
                blur = new Mat();
                using Mat clone = new(src, roi);

                Cv2.GaussianBlur(clone, blur, kernelSize, sigmaX, sigmaY);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 取得輪廓點 X 座標
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">ROI</param>
        /// <param name="avg">平均 X</param>
        /// <param name="minX">最小 X</param>
        /// <param name="maxX">最大 X</param>
        public static void GetContoursX(Mat src, Rect roi, out double avg, out int minX, out int maxX)
        {
            try
            {
                using Mat clone = new(src, roi);

                Cv2.FindContours(clone, out Point[][] cons, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, roi.Location);
                Point[] concatCon = cons.SelectMany(pts => pts).ToArray();

                // Cv2.CvtColor(src, src, ColorConversionCodes.GRAY2BGR);
                for (int i = 0; i < cons.Length; i++)
                {
                    Debug.WriteLine($"cons{i}: {cons[i].Length}");
                    //Cv2.DrawContours(src, cons, i, Scalar.Red, 2);
                }

                avg = concatCon.Average(pt => pt.X);
                minX = concatCon.Min(pt => pt.X);
                maxX = concatCon.Max(pt => pt.X);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得濾波影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="kernelCenterValue">中心值</param>
        /// <param name="compesation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetFilter2D(Mat src, double kernelCenterValue, double compesation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                double s = kernelCenterValue / 9 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, s, s},
                    { s, kernelCenterValue - s + compesation, s },
                    { s, s, s }
                });
                Cv2.Filter2D(src, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 取得 ROI 濾波影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">roi 區域</param>
        /// <param name="kernelCenterValue">中心值</param>
        /// <param name="compensation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetRoiFilter2D(Mat src, Rect roi, double kernelCenterValue, double compensation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                using Mat clone = new(src, roi);
                double s = kernelCenterValue / 9 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, s, s},
                    { s, kernelCenterValue - s + compensation, s },
                    { s, s, s }
                });
                Cv2.Filter2D(clone, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 取得垂直方向濾波影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="centerValue">中心值</param>
        /// <param name="compesation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetVerticalFilter2D(Mat src, double centerValue, double compesation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                double s = centerValue / 3 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, centerValue - s + compesation, s },
                    { s, centerValue - s + compesation, s },
                    { s, centerValue - s + compesation, s }
                });
                Cv2.Filter2D(src, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 取得 ROI 垂直方向濾波影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">roi 區域</param>
        /// <param name="centerValue">中心值</param>
        /// <param name="compesation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetRoiVerticalFilter2D(Mat src, Rect roi, double centerValue, double compesation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                using Mat clone = new(src, roi);
                double s = centerValue / 3 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, centerValue - s + compesation, s },
                    { s, centerValue - s + compesation, s },
                    { s, centerValue - s + compesation, s }
                });
                Cv2.Filter2D(clone, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 取得水平方向濾波影像 
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="centerValue">中心值</param>
        /// <param name="compesation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetHorizonalFilter2D(Mat src, double centerValue, double compesation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                double s = centerValue / 3 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, s, s },
                    { centerValue - s + compesation, centerValue - s + compesation, centerValue - s + compesation },
                    { s, s, s }
                });
                Cv2.Filter2D(src, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 取得 ROI 水平方向濾波影像
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">roi 區域</param>
        /// <param name="centerValue">中心值</param>
        /// <param name="compesation">中心補償值</param>
        /// <param name="filter">(out) 濾波影像</param>
        public static void GetRoiHorizonalFilter2D(Mat src, Rect roi, double centerValue, double compesation, out Mat filter)
        {
            try
            {
                filter = new Mat();

                using Mat clone = new(src, roi);
                double s = centerValue / 3 * -1;

                InputArray kernel = InputArray.Create(new double[3, 3] {
                    { s, s, s },
                    { centerValue - s + compesation, centerValue - s + compesation, centerValue - s + compesation },
                    { s, s, s }
                });
                Cv2.Filter2D(clone, filter, MatType.CV_8U, kernel, new Point(-1, -1), 0);
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
        /// 繪製 Gray Scale Chart (X座標對灰度值曲線)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="width"></param>
        /// <param name="createChart"></param>
        /// <param name="chart"></param>
        public static unsafe void GetHorizontalGrayScale(Mat src, out byte[] grayArr, out short[] grayArrDiff, bool createChart, out Mat chart, Scalar scalar)
        {
            chart = createChart ? new Mat(new Size(src.Width, 300), MatType.CV_8UC3, Scalar.White) : new Mat();
            byte* b = src.DataPointer;

            grayArr = new byte[src.Width];
            grayArrDiff = new short[src.Width];
            for (int i = 0; i < src.Width; i++)
            {
                ushort gray = 0;
                for (int j = 0; j < src.Height; j++)
                {
                    gray += b[src.Width * j];
                }

                // 計算平均
                grayArr[i] = (byte)(gray / src.Height);
                grayArrDiff[i] = (short)(i > 0 ? grayArr[i] - grayArr[i - 1] : grayArr[i]);

                if (createChart)
                {
                    if (i != 0)
                    {
                        Cv2.Line(chart, i - 1, chart.Height - grayArr[i - 1], i, chart.Height - grayArr[i], scalar, 1);
                    }
                }
                b++;
            }
        }

        /// <summary>
        /// 繪製 Gray Scale Chart (X座標對灰度值曲線)，
        /// 並標示超出標準差的點
        /// </summary>
        /// <param name="src"></param>
        /// <param name="width"></param>
        /// <param name="createChart"></param>
        /// <param name="chart"></param>
        public static unsafe void GetHorizontalGrayScale(Mat src, out byte[] grayArr, out double mean, bool createChart, out Mat chart, Scalar scalar)
        {
            chart = createChart ? new Mat(new Size(src.Width, 300), MatType.CV_8UC3, Scalar.White) : new Mat();
            //ptOutOfR = 0;
            byte* b = src.DataPointer;

            grayArr = new byte[src.Width];
            //grayArrDiff = new short[src.Width];
            for (int i = 0; i < src.Width; i++)
            {
                ushort gray = 0;
                for (int j = 0; j < src.Height; j++)
                {
                    gray += b[src.Width * j];
                }

                // 計算平均
                grayArr[i] = (byte)(gray / src.Height);
                // grayArrDiff[i] = (short)(i > 0 ? grayArr[i] - grayArr[i - 1] : grayArr[i]);

                if (createChart)
                {
                    if (i != 0)
                    {
                        Cv2.Line(chart, i - 1, chart.Height - grayArr[i - 1], i, chart.Height - grayArr[i], scalar, 1);
                    }

                    //if (Math.Abs(mean - grayArr[i]) > sigma * n_sigma)
                    //{
                    //    Cv2.Circle(chart, i, chart.Height - grayArr[i], 3, Scalar.Red, 1);
                    //    ptOutOfR++;
                    //}
                }
                b++;
            }
            mean = grayArr.Average(x => x);
        }

        /// <summary>
        /// 計算陣列局部離群點 (local outliers)
        /// </summary>
        /// <param name="img">Chart Mat</param>
        /// <param name="array">來源陣列</param>
        /// <param name="regionPoints">每個區域點數量</param>
        /// <param name="slope">平均值與峰/谷值差異門檻</param>
        /// <param name="mask">遮罩點，這些點附近會被填入平均值不做計算</param>
        /// <param name="peaks">峰值陣列</param>
        /// <param name="valleys">谷值陣列</param>
        public static void CalLocalOutliers(Mat img, byte[] array, int regionPoints, byte slope, double std, out Point[] peaks, out Point[] valleys)
        {
            List<Point> ptsP = new();
            List<Point> ptsV = new();

            // int offset = array.Length / regionPoints;
            for (int i = 0; i < array.Length; i += regionPoints)
            {
                byte[] arr = i + regionPoints < array.Length
                    ? new ArraySegment<byte>(array, i, regionPoints).ToArray()
                    : new ArraySegment<byte>(array, i, array.Length - i).ToArray();
                double avg = arr.Average(x => x);
                byte max = arr.Max();
                byte min = arr.Min();

                if (max - avg > slope || max - avg > std)
                {
                    Point pt = new(Array.IndexOf(arr, max) + i, img.Height - max);
                    Cv2.Circle(img, pt, 5, Scalar.DarkRed, 2);
                    ptsP.Add(pt);
                }
                else if (avg - min > slope || avg - min > std)
                {
                    Point pt = new(Array.IndexOf(arr, min) + i, img.Height - min);
                    Cv2.Circle(img, pt, 5, Scalar.DarkBlue, 2);
                    ptsV.Add(pt);
                }
                Cv2.Line(img, i, 0, i, img.Height, Scalar.Gray, 1);

                if (i + (regionPoints * 1.5) < array.Length)
                {
                    arr = new ArraySegment<byte>(array, i + (regionPoints / 2), regionPoints).ToArray();
                    avg = arr.Average(x => x);
                    max = arr.Max();
                    min = arr.Min();

                    if (max - avg > slope)
                    {
                        Point pt = new(Array.IndexOf(arr, max) + i + (regionPoints / 2), img.Height - max);
                        Cv2.Circle(img, pt, 5, Scalar.DarkRed, 2);
                        ptsP.Add(pt);
                    }
                    else if (avg - min > slope)
                    {
                        Point pt = new(Array.IndexOf(arr, min) + i + (regionPoints / 2), img.Height - min);
                        Cv2.Circle(img, pt, 5, Scalar.DarkBlue, 2);
                        ptsV.Add(pt);
                    }
                    Cv2.Line(img, i + (regionPoints / 2), 0, i + (regionPoints / 2), img.Height, Scalar.Gray, 1);
                }
                else
                {
                    Cv2.Line(img, i + (regionPoints / 2), 0, i + (regionPoints / 2), img.Height, Scalar.Gray, 1);
                }
            }

            peaks = ptsP.ToArray();
            valleys = ptsV.ToArray();
            ptsP.Clear();
            ptsV.Clear();
        }

        /// <summary>
        /// 計算陣列局部離群點 (local outliers)
        /// </summary>
        /// <param name="img">Chart Mat</param>
        /// <param name="array">來源陣列</param>
        /// <param name="regionPoints">每個區域點數量</param>
        /// <param name="slope">平均值與峰/谷值差異門檻</param>
        /// <param name="peaks">峰值數量</param>
        /// <param name="valleys">谷值數量</param>
        public static void CalLocalOutliers(Mat img, byte[] array, int regionPoints, byte slope, double std, out int peaks, out int valleys)
        {
            //List<Point> ptsP = new();
            //List<Point> ptsV = new();
            peaks = 0;
            valleys = 0;

            // int offset = array.Length / regionPoints;
            for (int i = 0; i < array.Length; i += regionPoints)
            {
                byte[] arr = i + regionPoints < array.Length
                    ? new ArraySegment<byte>(array, i, regionPoints).ToArray()
                    : new ArraySegment<byte>(array, i, array.Length - i).ToArray();

                double avg = arr.Average(x => x);
                byte max = arr.Max();
                byte min = arr.Min();

                // 大於門檻斜率 or 大於標準差
                if (max - avg > slope || max - avg > std)
                {
                    Point pt = new(Array.IndexOf(arr, max) + i, img.Height - max);
                    Cv2.Circle(img, pt, 5, Scalar.DarkRed, 2);
                    //ptsP.Add(pt);
                    peaks++;
                }
                // 大於門檻斜率 or 小於標準差
                else if (avg - min > slope || avg - min > std)
                {
                    Point pt = new(Array.IndexOf(arr, min) + i, img.Height - min);
                    Cv2.Circle(img, pt, 5, Scalar.DarkBlue, 2);
                    //ptsV.Add(pt);
                    valleys++;
                }
                Cv2.Line(img, i, 0, i, img.Height, Scalar.Gray, 1);

                if (i + (regionPoints * 1.5) < array.Length)
                {
                    arr = new ArraySegment<byte>(array, i + (regionPoints / 2), regionPoints).ToArray();
                    avg = arr.Average(x => x);
                    max = arr.Max();
                    min = arr.Min();

                    // 大於門檻斜率 or 大於標準差
                    if (max - avg > slope || max - avg > std)
                    {
                        Point pt = new(Array.IndexOf(arr, max) + i + (regionPoints / 2), img.Height - max);
                        Cv2.Circle(img, pt, 5, Scalar.DarkRed, 2);
                        //ptsP.Add(pt);
                        peaks++;
                    }
                    else if (avg - min > slope)
                    {
                        Point pt = new(Array.IndexOf(arr, min) + i + (regionPoints / 2), img.Height - min);
                        Cv2.Circle(img, pt, 5, Scalar.DarkBlue, 2);
                        //ptsV.Add(pt);
                        valleys++;
                    }
                    Cv2.Line(img, i + (regionPoints / 2), 0, i + (regionPoints / 2), img.Height, Scalar.Gray, 1);
                }
                else
                {
                    Cv2.Line(img, i + (regionPoints / 2), 0, i + (regionPoints / 2), img.Height, Scalar.Gray, 1);
                }
            }

            //peaks = ptsP.ToArray();
            //valleys = ptsV.ToArray();
            //ptsP.Clear();
            //ptsV.Clear();
        }

        /// <summary>
        /// 取得輪廓點陣列
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="th1">Canny 閾值 1</param>
        /// <param name="th2">Canny 閾值 2</param>
        /// <param name="cons">輪廓陣列</param>
        /// <param name="con">一維化輪廓陣列</param>
        /// <param name="conLength">輪廓長度閾值，過濾小於此長度值的輪廓</param>
        public static void GetContours(Mat src, Point offset, byte th1, byte th2, out Point[][] cons, out Point[] con, int conLength = 0)
        {
            cons = null;
            con = null;

            try
            {
                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(src, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3, true);

                Cv2.FindContours(canny, out cons, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, offset);

                IEnumerable<Point[]> filter = cons.Where(c => c.Length > conLength);

                cons = filter.ToArray();
                con = filter.SelectMany(pts => pts).ToArray();
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
        /// 從 Canny 取得輪廓點陣列
        /// </summary>
        /// <param name="canny">來源影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="cons">輪廓陣列</param>
        /// <param name="con">一維化輪廓陣列</param>
        /// <param name="conLength"></param>
        public static void GetContoursFromCanny(Mat canny, Point offset, out Point[][] cons, out Point[] con, int conLength = 0)
        {
            cons = null;
            con = null;

            try
            {
                Cv2.FindContours(canny, out cons, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, offset);

                if (conLength > 0)
                {
                    IEnumerable<Point[]> filter = cons.Where(c => c.Length > conLength);

                    cons = filter.ToArray();
                    con = filter.SelectMany(pts => pts).ToArray();
                }
                else
                {
                    con = cons.SelectMany(pts => pts).ToArray();
                }
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
        /// 取得霍夫 Lines
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">ROI 方形區域</param>
        /// <param name="th1">Canny 閾值 1</param>
        /// <param name="th2">Canny 閾值 2</param>
        /// <param name="lineSegments">霍夫直線</param>
        /// <param name="lineLength">直線長度閾值，過濾小於指長度的直線</param>
        public static void GetHoughLines(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] lineSegments, int lineLength = 0)
        {
            lineSegments = Array.Empty<LineSegmentPoint>();

            try
            {
                using Mat blur = new();
                using Mat canny = new();

                //Cv2.BilateralFilter(src, blur, 15, 100, 5);
                Cv2.BilateralFilter(src, blur, 5, 50, 100);
                Cv2.Canny(blur, canny, th1, th2, 3);

                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 25, 10, 5);

                // 過濾 line 長度大於 Length
                lineSegments = lineSeg.Where(line => line.Length() > lineLength).ToArray();
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
        /// 取得霍夫水平 / 垂直線
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">ROI 方形區域</param>
        /// <param name="th1">Canny 閾值 1</param>
        /// <param name="th2">Canny 閾值 1</param>
        /// <param name="lineSegH">水平線</param>
        /// <param name="lineSegV">垂直線</param>
        public static void GetHoughLines(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] lineSegH, out LineSegmentPoint[] lineSegV)
        {
            lineSegH = lineSegV = Array.Empty<LineSegmentPoint>();

            try
            {
                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(src, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3);

                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 25, 10, 5);

                for (int i = 0; i < lineSeg.Length; i++)
                {
                    if (Math.Abs(lineSeg[i].P2.X - lineSeg[i].P1.X) < 3)
                    {
                        //垂直線 (gap < 3)
                        lineSeg[i].Offset(roi.Location);
                        lineSegV = lineSegV.Concat(new LineSegmentPoint[] { lineSeg[i] }).ToArray();
                    }
                    else if (Math.Abs(lineSeg[i].P2.Y - lineSeg[i].P1.Y) < 3)
                    {
                        //水平線 (gap < 3)
                        lineSeg[i].Offset(roi.Location);
                        lineSegH = lineSegH.Concat(new LineSegmentPoint[] { lineSeg[i] }).ToArray();
                    }
                }
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
        /// 計算水平 Hough Lines (General)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="roi">ROI 方形區域</param>
        /// <param name="th1">Canny 閾值 1</param>
        /// <param name="th2">Canny 閾值 2</param>
        /// <param name="lineSegH">水平線</param>
        /// <param name="Ygap"></param>
        public static void GetHoughLinesH(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] lineSegH, int Ygap = 3)
        {
            lineSegH = Array.Empty<LineSegmentPoint>();

            try
            {
                using Mat clone = new Mat(src, roi);

                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(src, blur, 5, 50, 100);
                Cv2.Canny(blur, canny, th1, th2, 3);

                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 25, 10, 5);

                // 1. 保留 Ygap < 3 的線 2. 平移 roi.X roi.Y
                lineSegH = lineSeg.Where(line => Math.Abs(line.P2.Y - line.P1.Y) < Ygap).Select(line =>
                {
                    line.Offset(roi.Location);
                    return line;
                }).ToArray();
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
        /// 從 Canny 計算水平 Hough Lines
        /// </summary>
        /// <param name="src">來源 Canny 影像</param>
        /// <param name="th1"></param>
        /// <param name="th2"></param>
        /// <param name="lineSegH"></param>
        /// <param name="Ygap"></param>
        public static void GetHoughLinesHFromCanny(Mat src, Point offset, out LineSegmentPoint[] lineSegH, int Ygap = 3)
        {
            lineSegH = Array.Empty<LineSegmentPoint>();

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                // 1. 保留 Ygap < 3 的線 2. 平移 roi.X roi.Y
                lineSegH = lineSeg.Where(line => Math.Abs(line.P2.Y - line.P1.Y) < Ygap).Select(line =>
                {
                    line.Offset(offset);
                    return line;
                }).ToArray();
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
        /// 計算垂直Hough Lines
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="th1"></param>
        /// <param name="th2"></param>
        /// <param name="lineSegV"></param>
        /// <param name="Xgap"></param>
        public static void GetHoughLinesV(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] lineSegV, int Xgap = 3)
        {
            lineSegV = Array.Empty<LineSegmentPoint>();

            try
            {
                using Mat clone = new(src, roi);

                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(clone, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3);

                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 25, 10, 5);

                // 1. 保留 Xgap < 3 的線 2. 平移 roi.X roi.Y
                lineSegV = lineSeg.Where(line => Math.Abs(line.P2.X - line.P1.X) < Xgap).Select(line =>
                {
                    line.Offset(roi.X, roi.Y);
                    return line;
                }).ToArray();

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
        /// 計算 Center 垂直線
        /// </summary>
        public static void GetHoughLinesCenterV(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] LineSegVLeft, out LineSegmentPoint[] LineSegVRight, out double CenterX, int Xgap = 3)
        {
            LineSegVLeft = Array.Empty<LineSegmentPoint>();
            LineSegVRight = Array.Empty<LineSegmentPoint>();
            CenterX = 0;

            try
            {
                using Mat clone = new(src, roi);

                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(clone, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3);

                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    // 保留近垂直線
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => Math.Abs(line.P2.X - line.P1.X) < Xgap);

                    // 計算中心
                    double max = filter.Max(line => Math.Max(line.P1.X, line.P2.X));
                    double min = filter.Min(line => Math.Min(line.P1.X, line.P2.X));
                    double mean = (max + min) / 2.0;

                    // 過濾 + 平移
                    LineSegVLeft = filter.Where(line => line.P1.X < mean).Select(line =>
                    {
                        line.Offset(roi.Location);
                        return line;
                    }).ToArray();
                    // 過濾 + 平移
                    LineSegVRight = filter.Where(line => line.P1.X > mean).Select(line =>
                    {
                        line.Offset(roi.Location);
                        return line;
                    }).ToArray();

                    min = LineSegVLeft.Min(line => (line.P1.X + line.P2.X) / 2);
                    max = LineSegVRight.Max(line => (line.P1.X + line.P2.X) / 2);
                    CenterX = (min + max) / 2;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得方框內部 Y 座標，
        /// </summary>
        /// <param name="src">canny 影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="Ypos">(out) Y 座標；長度必為 2</param>
        public static void GetHoughWindowYPos(Mat src, int offset, out double y1, out double y2, int Ygap = 3, int lineLength = 0)
        {
            y1 = y2 = 0;

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    //Debug.WriteLine($"filter.length {lineSeg.Where(line => line.Length() > lineLength && Math.Abs(line.P2.Y - line.P1.Y) < Ygap).Count()}");

                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => line.Length() > lineLength && Math.Abs(line.P2.Y - line.P1.Y) < Ygap).OrderBy(line => line.P1.Y + line.P2.Y);

                    // Debug.WriteLine($"filter.length {filter.Count()}");
                    // Debug.WriteLine($"{string.Join(" , ", filter)}");

                    LineSegmentPoint pt1 = filter.Last(line => (line.P1.Y + line.P2.Y) / 2 < 960);
                    LineSegmentPoint pt2 = filter.First(line => (line.P1.Y + line.P2.Y) / 2 > 960);

                    y1 = (pt1.P1.Y + pt1.P2.Y) / 2 + offset;    // 上緣
                    y2 = (pt2.P1.Y + pt2.P2.Y) / 2 + offset;    // 下緣
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得分組後垂直線 Y 座標位置，
        /// </summary>
        /// <param name="src">canny 影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="XPosCount">(out) Y 座標數</param>
        /// <param name="Xpos">(out) Y 座標</param>
        /// <param name="Xgap">Y 座標 Gap</param>
        public static void GetHoughHorizonalYPos(Mat src, int offset, out int YPosCount, out double[] Ypos, int Ygap = 3, int lineLengh = 0)
        {
            YPosCount = 0;
            Ypos = Array.Empty<double>();

            try
            {
                //Debug.WriteLine($"offset: {offset}");
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => line.Length() > lineLengh && Math.Abs(line.P2.Y - line.P1.Y) < Ygap);
                    IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.Y + line.P2.Y).GroupBy(line => Math.Floor((double)(line.P1.Y * line.P2.Y) / 10000)).ToArray();

                    YPosCount = groupings.Length;
                    Ypos = new double[groupings.Length];
                    for (int j = 0; j < groupings.Length; j++)
                    {
                        Ypos[j] = groupings[j].Average(a => Math.Round((double)(a.P1.Y + a.P2.Y) / 2)) + offset;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得分組後垂直線 X 座標位置，
        /// </summary>
        /// <param name="src">canny 影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="XPosCount">(out) X 座標數</param>
        /// <param name="Xpos">(out) X 座標</param>
        /// <param name="Xgap">X 座標 Gap</param>
        public static void GetHoughVerticalXPos(Mat src, int offset, out int XPosCount, out double[] Xpos, int Xgap = 3, int lineLength = 0)
        {
            XPosCount = 0;
            Xpos = Array.Empty<double>();

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => line.Length() > lineLength && Math.Abs(line.P2.X - line.P1.X) < Xgap);
                    IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.X + line.P2.X).GroupBy(line => Math.Floor((double)(line.P1.X * line.P2.X) / 10000)).ToArray();

                    XPosCount = groupings.Length;
                    Xpos = new double[groupings.Length];
                    for (int i = 0; i < groupings.Length; i++)
                    {
                        Xpos[i] = groupings[i].Average(a => Math.Round((double)(a.P1.X + a.P2.X) / 2)) + offset;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 取得窗戶 Width
        /// </summary>
        /// <param name="src">canny 影</param>
        /// <param name="lineCount">(out) 線數量</param>
        /// <param name="width">(out) 窗戶 Width</param>
        /// <param name="dir">(ref) 方向</param>
        /// <param name="Xgap">垂直線 X 跨度</param>
        /// <param name="lineLength">線長度閾值</param>
        /// <param name="minWindowWidth">最小窗戶 Width</param>
        /// <returns>是否檢測到窗戶</returns>
        public static bool GetVertialWindowWidth(Mat src, out int lineCount, out double width, int Xgap = 5, int lineLength = 0, int minWindowWidth = 100)
        {
            lineCount = 0;
            width = 0;

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    //foreach (LineSegmentPoint line in lineSeg)
                    //{
                    //    Debug.WriteLine($"{line.P1} {line.P2} {line.Length()}");
                    //}
                    //Debug.WriteLine($"{DateTime.Now:ss.fff}");

                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => line.Length() > lineLength && Math.Abs(line.P2.X - line.P1.X) <= Xgap);

                    // 排序 => 取出 X => 過濾重複 => ToArray()
                    double[] distinct = filter.OrderBy(line => line.P1.X + line.P2.X).Select(l => (double)((l.P1.X + l.P2.X) / 2)).Distinct().ToArray();
                    Debug.WriteLine($"distinct: {string.Join(",", distinct)}");

                    if (distinct.Length >= 4)
                    {
                        double center = (distinct[0] + distinct[^1]) / 2;

                        double winL = distinct.Last(x => x < center);
                        double winR = distinct.First(x => x > center);
                        lineCount = distinct.Length;
                        width = winR - winL;
                    }
                    else
                    {
                        lineCount = distinct.Length;
                        return false;
                    }
#if false
                    // List<double> a1 = new();
                    // List<double> a2 = new();

                    //for (int i = 0; i < distinct.Length; i++)
                    //{
                    //    if (i == 0)
                    //    {
                    //        a1.Add(distinct[i]);
                    //        a2.Add(distinct[^(i + 1)]);
                    //    }
                    //    else
                    //    {
                    //        if (distinct[i] > distinct[i - 1] + 5 && a1.Count < 2)
                    //        {
                    //            a1.Add(distinct[i]);
                    //        }

                    //        if (distinct[^(i + 1)] < distinct[^(i)] - 5 && a2.Count < 2)
                    //        {
                    //            a2.Add(distinct[^(i + 1)]);
                    //        }
                    //    }
                    //}
                    //groupings = a1.Concat(a2.Reverse());

                    //Debug.WriteLine($"arr: {string.Join(",", arr)}");
                    //Debug.WriteLine($"a1: {string.Join(",", a1)}");
                    //Debug.WriteLine($"a2: {string.Join(",", a2)}");

                    //foreach (LineSegmentPoint item in filter.OrderBy(line => (line.P1.X + line.P2.X) / 2))
                    //{
                    //    Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
                    //}
                    //Debug.WriteLine($"--------------------------------------------------------------------------------------------------------------------");

                    //Debug.WriteLine($"{DateTime.Now:ss.fff}");

                    //IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.X).GroupBy(line => Math.Floor((double)(line.P1.X + line.P2.X) / 10)).ToArray();

                    //Debug.WriteLine($"{DateTime.Now:ss.fff}");

                    //foreach (IGrouping<double, LineSegmentPoint> item in groupings)
                    //{
                    //    Debug.WriteLine($"{item.Key} {item.Average(a => (a.P1.X + a.P2.X) / 2)}");
                    //}
                    //Debug.WriteLine($"====================================================================================================================");

                    //a2.Reverse(); // 反轉
                    //double[] concat = a1.Concat(a2).ToArray();

                    Debug.WriteLine($"groupings {string.Join(",", concat)}");
                    if (concat.Length == 4)
                    {
                        lineCount = concat.Length;
                        //width = groupings[2].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2) - groupings[1].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2);
                        width = concat[2] - concat[1];

                        if (width < minWindowWidth)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        lineCount = concat.Length;
                        return false;
                    } 
#endif
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 確認影像是否含有目標物
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfImageEmpty(Mat src, Rect roi, double threshhold)
        {
            try
            {
                using Mat roiImg = new(src, roi);
                using Mat roiHsv = new();
                using Mat roiS = new();
                using Mat canny = new();

                Cv2.CvtColor(roiImg, roiHsv, ColorConversionCodes.BGR2HSV);
                Cv2.Split(roiHsv, out Mat[] roiHSVs);
                Cv2.InRange(roiHSVs[1], 50, 255, roiS);

                Cv2.Canny(roiS, canny, 120, 0);
                Cv2.FindContours(canny, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                double arcLength = contours.Aggregate(0.0, (acc, e) => acc += Cv2.ArcLength(e, false));

                return arcLength > threshhold;
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

        public static Mat GetAreaGreaterThan(Mat src, Rect roi, int threshbold)
        {
            try
            {
                using Mat roiImg = new(src, roi);
                using Mat dst = new(roi.Size, MatType.CV_8UC1);

                using Mat lables = new();
                using Mat stats = new();
                using Mat cXY = new();

                int num = Cv2.ConnectedComponentsWithStats(src, lables, stats, cXY);
                Vec3b[] colors = new Vec3b[num];

                for (int i = 0; i < num; i++)
                {
                    if (stats.At<int>(i, 0) == 0) continue;

                    colors[i] = stats.At<int>(i, 4) > threshbold ? new Vec3b(255, 255, 255) : new Vec3b(0, 0, 0);
                }

                for (int y = 0; y < dst.Rows; y++)
                {
                    for (int x = 0; x < dst.Cols; x++)
                    {
                        int label = lables.At<int>(y, x);

                        dst.At<Vec3b>(y, x) = colors[label];
                    }
                }
                return dst;
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

        #region MCA JAW 專用 Methods 
        /// <summary>
        /// 從 Canny 計算水平 Hough Lines (MCA Jaw 專用)
        /// </summary>
        /// <param name="canny">來源影像 (canny)</param>
        /// <param name="lineSegH">水平 Hough Line</param>
        /// <param name="houghThreashold">HoughLinesP 方法內的 Threashold</param>
        /// <param name="houghMinLineLength">HoughLinesP 方法內的 MinLineLength</param>
        /// <param name="Ygap">同一線段 Y 座標變化</param>
        public static void GetHoughLinesHFromCanny(Mat canny, Point offset, out LineSegmentPoint[] lineSegH, int houghThreashold = 25, double houghMinLineLength = 10, int Ygap = 3)
        {
            lineSegH = Array.Empty<LineSegmentPoint>();

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, houghThreashold, houghMinLineLength, 3);
                // Debug.WriteLine($"lineSeg.Length: {lineSeg.Length}");

                // 1. 保留 Ygap < 3 的線 2. 確認 X 偏移大於 Y 偏移 3. 平移 roi.X, roi.Y
                lineSegH = lineSeg.Where(line =>
                    Math.Abs(line.P2.Y - line.P1.Y) < Ygap &&
                    Math.Abs(line.P2.X - line.P1.X) >= Math.Abs(line.P2.Y - line.P1.Y)).Select(line =>
                {
                    line.Offset(offset);
                    return line;
                }).ToArray();
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
        /// 從 Canny 計算垂直 Hough Lines (MCA Jaw 專用)
        /// </summary>
        /// <param name="canny">來源影像 (canny)</param>
        /// <param name="offset">位移</param>
        /// <param name="lineSegV">垂直 Hough Line</param>
        /// <param name="houghThreashold">HoughLinesP 方法內的 Threashold</param>
        /// <param name="houghMinLineLength">HoughLinesP 方法內的 MinLineLength</param>
        /// <param name="Xgap">同一線段 X 座雕變化</param>
        public static void GetHoughLinesVFromCanny(Mat canny, Point offset, out LineSegmentPoint[] lineSegV, int houghThreashold = 25, double houghMinLineLength = 10, int Xgap = 3)
        {
            lineSegV = Array.Empty<LineSegmentPoint>();

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, houghThreashold, houghMinLineLength, 3);

                // 1. 保留 Xgap < 3 的線 2. 確認 Y 偏移大於 X 偏移 3. 平移 roi.X, roi.Y
                lineSegV = lineSeg.Where(line =>
                    Math.Abs(line.P2.X - line.P1.X) < Xgap &&
                    Math.Abs(line.P2.Y - line.P1.Y) >= Math.Abs(line.P2.X - line.P1.X)).Select(line =>
                {
                    line.Offset(offset.X, offset.Y);
                    return line;
                }).ToArray();
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
        /// 計算水平方向平直度
        /// </summary>
        /// <param name="roiMat">ROI 影像</param>
        /// <param name="width">原圖 width</param>
        /// <param name="listY">Y 座標列表</param>
        /// <param name="listY2">Y 座標列表 2</param>
        [Obsolete("wait for testing and then start using")]
        public static unsafe void GetHorizontablFlatness(Mat roiMat, int width, out List<double> listY, out List<double> listY2)
        {
            byte* b = roiMat.DataPointer;

            listY = new List<double>();
            listY2 = new List<double>();

            double[] grayArr;
            double tmpGrayAbs = 0;
            int tmpY = 0;

            for (int i = roiMat.Width / 2, i2 = roiMat.Width / 2 - 3; i < roiMat.Width || i2 >= 0; i += 3, i2 -= 3)
            {
                // 向右，且避開 pin
                if (i is < 590 or >= 650 && (i < roiMat.Width))
                {
                    grayArr = new double[roiMat.Height];
                    tmpGrayAbs = 0; // 紀錄差值 (斜率)
                    tmpY = 0;

                    for (int j = 0; j < roiMat.Height; j++)
                    {
                        // 計算鄰近三點灰階值平均
                        double avg = (b[(width * j) + i] + b[(width * j) + i + 1] + b[(width * j) + i + 2]) / 3;
                        grayArr[j] = avg;

                        int k = j - 1;
                        if (j == 0) { continue; }

                        if (grayArr[j] < grayArr[k] && Math.Abs(grayArr[j] - grayArr[k]) > tmpGrayAbs)
                        {
                            tmpY = j;   // 紀錄 Y 座標，此為斜率最大點
                            tmpGrayAbs = Math.Abs(grayArr[j] - grayArr[k]); // 計算差值，下個迴圈使用
                        }

                        // 若灰階值突然增加，代表進入反光區，不為工件表面
                        if (grayArr[j] - grayArr[k] > 10) { break; }
                    }

                    // 判斷鄰近 Y 座標間差距小於3，否則代表 Y 座標抓取錯誤
                    if (listY.Count == 0 || Math.Abs(tmpY - listY[^1]) < 3)
                    {
                        listY.Add(tmpY);

#if DEBUG || debug
                        // 標記黑色 pin
                        b[(width * tmpY) + i] = 0;
                        b[(width * (tmpY + 1)) + i] = 0;
                        b[(width * (tmpY + 2)) + i] = 0;
                        b[(width * (tmpY - 1)) + i] = 0;
                        b[(width * (tmpY - 2)) + i] = 0;
#endif
                    }
                    else
                    {
                        // 若差距過大，插入前一筆 Y 座標
                        listY.Add(listY[^1]);
                    }

                    if (listY.Count > 4)
                    {
                        // 平滑化曲線
                        double avg = (listY[^1] + listY[^2] + listY[^3] + listY[^4] + listY[^5]) / 5;
                        listY2.Add(avg);
                    }
                }

                // 向左
                if (i2 > 0)
                {
                    grayArr = new double[roiMat.Height];
                    tmpGrayAbs = 0;
                    tmpY = 0;

                    for (int j = 0; j < roiMat.Height; j++)
                    {
                        // 計算鄰近三點灰階值平均
                        double avg = (b[(width * j) + i2] + b[(width * j) + i2 + 1] + b[(width * j) + i2 + 2]) / 3;
                        grayArr[j] = avg;
                        int k = j - 1;

                        if (j == 0) { continue; }

                        if (grayArr[j] < grayArr[k] && Math.Abs(grayArr[j] - grayArr[k]) > tmpGrayAbs)
                        {
                            tmpY = j;   // 紀錄 Y 座標，此為斜率最大點
                            tmpGrayAbs = Math.Abs(grayArr[j] - grayArr[k]); // 計算差值，下個迴圈使用
                        }

                        // 若灰階值突然增加，代表進入反光區，不為工件表面
                        if (grayArr[j] - grayArr[k] > 10) { break; }
                    }


                    // 判斷鄰近 Y 座標間差距小於3，否則代表 Y 座標抓取錯誤
                    if (Math.Abs(tmpY - listY[0]) < 3)
                    {
                        listY.Insert(0, tmpY);

#if DEBUG || debug 
                        b[(width * tmpY) + i2] = 50;
                        b[(width * (tmpY + 1)) + i2] = 50;
                        b[(width * (tmpY + 2)) + i2] = 50;
                        b[(width * (tmpY - 1)) + i2] = 50;
                        b[(width * (tmpY - 2)) + i2] = 50;
#endif
                    }
                    else
                    {
                        listY.Insert(0, listY[0]);
                    }


                    if (listY.Count > 4)
                    {
                        double avg = (listY[0] + listY[1] + listY[2] + listY[3] + listY[4]) / 5;
                        listY2.Insert(0, avg);
                    }
                }
            }
        }
        #endregion
    }
}
