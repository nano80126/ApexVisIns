using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexVisIns
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
                //using Mat clone = new(src, roi);

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
        /// <param name="threshHold">Otsu 閾值</param>
        public static void GetRoiOtsu(Mat src, Rect roi, byte th, byte max, out Mat otsu, out byte threshHold)
        {
            try
            {
                otsu = new Mat();
                using Mat clone = new(src, roi);

                threshHold = (byte)Cv2.Threshold(clone, otsu, th, max, ThresholdTypes.Otsu);
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
        public static void GetHorizonalFilter2D(Mat src, double centerValue, double compesation,out Mat filter)
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
        /// 取得輪廓點陣列
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="offset">Offset 位移</param>
        /// <param name="th1">Canny 閾值 1</param>
        /// <param name="th2">Canny 閾值 2</param>
        /// <param name="con">輪廓陣列</param>
        /// <param name="cons">一維化輪廓陣列</param>
        /// <param name="conLength">輪廓長度閾值，過濾小於此長度值的輪廓</param>
        public static void GetContours(Mat src, Point offset, byte th1, byte th2, out Point[][] con, out Point[] cons, int conLength = 0)
        {
            con = null;
            cons = null;

            try
            {
                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(src, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3, true);

                Cv2.FindContours(canny, out con, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, offset);

                IEnumerable<Point[]> filter = con.Where(c => c.Length > conLength);

                con = filter.ToArray();
                cons = filter.SelectMany(pts => pts).ToArray();
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
        /// 計算水平 Hough Lines
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
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => line.Length() > lineLength && Math.Abs(line.P2.X - line.P1.X) < Xgap);
                    IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.X).GroupBy(line => Math.Floor((double)(line.P1.X * line.P2.X) / 10000)).ToArray();

                    if (groupings.Length == 4)
                    {
                        lineCount = groupings.Length;
                        width = groupings[2].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2) - groupings[1].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2);

                        //if (dir == 5)
                        //{
                        //    // 這邊確認旋轉方向
                        //    double L = groupings[1].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2) - groupings[0].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2);
                        //    double R = groupings[3].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2) - groupings[2].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2);
                        //    double abs = Math.Abs(L - R);

                        //    Debug.WriteLine($"L: {L} R: {R} ABS: {abs}");

                        //    if (3 < abs && abs < 50)
                        //    {
                        //        dir = L > R ? (byte)0 : (byte)1;
                        //    }
                        //    else if (50 <= abs)
                        //    {
                        //        dir = L > R ? (byte)2 : (byte)3;
                        //    }
                        //    else
                        //    {
                        //        dir = 4;
                        //    }
                        //}

                        if (width < minWindowWidth)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        lineCount = groupings.Length;
                        return false;
                    }
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
    }
}
