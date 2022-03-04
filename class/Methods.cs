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
        /// <param name="src"></param>
        /// <param name="th1"></param>
        /// <param name="th2"></param>
        /// <param name="canny"></param>
        public static void GetCanny(Mat src, byte th1, byte th2, out Mat canny)
        {
            try
            {
                canny = new Mat();

                using Mat blur = new();
                Cv2.BilateralFilter(src, blur, 15, 1100, 5);
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
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="th1"></param>
        /// <param name="th2"></param>
        /// <param name="canny"></param>
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


        public static void GetContours(Mat src, Point offset, byte th1, byte th2, out Point[][] con, out Point[] connectedCon, int contourLength = 0)
        {
            con = null;
            connectedCon = null;

            try
            {
                using Mat blur = new();
                using Mat canny = new();

                Cv2.BilateralFilter(src, blur, 15, 100, 5);
                Cv2.Canny(blur, canny, th1, th2, 3, true);

                Cv2.FindContours(canny, out con, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, offset);

                IEnumerable<Point[]> filter = con.Where(c => c.Length > contourLength);

                con = filter.ToArray();
                connectedCon = filter.SelectMany(pts => pts).ToArray();
                //con = con.Where(c => c.Length > contourLength).ToArray();
                //for (int i = 0; i < con.Length; i++)
                //{
                //    connectedCon = connectedCon.Concat(con[i].ToArray()).ToArray();
                //}
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
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="th1"></param>
        /// <param name="th2"></param>
        /// <param name="lineSegH"></param>
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
        public static void GetHoughLinesCenterV(Mat src, Rect roi, byte th1, byte th2, out LineSegmentPoint[] leftLineSegV, out LineSegmentPoint[] rightLineSegV, out double CenterX, int Xgap = 3)
        {
            leftLineSegV = Array.Empty<LineSegmentPoint>();
            rightLineSegV = Array.Empty<LineSegmentPoint>();
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
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => Math.Abs(line.P2.X - line.P1.X) < Xgap);

                    double max = filter.Max(line => Math.Max(line.P1.X, line.P2.X));
                    double min = filter.Min(line => Math.Min(line.P1.X, line.P2.X));
                    double mean = (max + min) / 2.0;

                    // 過濾 + 平移
                    leftLineSegV = filter.Where(line => line.P1.X < mean).Select(line =>
                    {
                        line.Offset(roi.X, roi.Y);
                        return line;
                    }).ToArray();
                    // 過濾 + 平移
                    rightLineSegV = filter.Where(line => line.P1.X > mean).Select(line =>
                    {
                        line.Offset(roi.X, roi.Y);
                        return line;
                    }).ToArray();

                    min = leftLineSegV.Average(line => (line.P1.X + line.P2.X) / 2);
                    max = rightLineSegV.Max(line => (line.P1.X + line.P2.X) / 2);
                    CenterX = (min + max) / 2;
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
        /// <param name="XPosCount">X 座標數</param>
        /// <param name="Xpos">X 座標</param>
        public static void GetHoughVerticalXPos(Mat src, int offset, out int XPosCount, out double[] Xpos, int Xgap = 3)
        {
            XPosCount = 0;
            Xpos = new double[0];

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => Math.Abs(line.P2.X - line.P1.X) < Xgap);

                    IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.X).GroupBy(line => Math.Floor((double)(line.P1.X * line.P2.X) / 10000)).ToArray();

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
        /// <param name="width"></param>
        /// <returns>是否檢測到窗戶</returns>
        public static bool GetVertialWindowWidth(Mat src, out int lineCount, out double width, int Xgap = 3)
        {
            lineCount = 0;
            width = 0;

            try
            {
                LineSegmentPoint[] lineSeg = Cv2.HoughLinesP(src, 1, Cv2.PI / 180, 25, 10, 5);

                if (lineSeg != null && lineSeg.Length > 0)
                {
                    IEnumerable<LineSegmentPoint> filter = lineSeg.Where(line => Math.Abs(line.P2.X - line.P1.X) < Xgap);

                    IGrouping<double, LineSegmentPoint>[] groupings = filter.OrderBy(line => line.P1.X).GroupBy(line => Math.Floor((double)(line.P1.X * line.P2.X) / 10000)).ToArray();

                    if (groupings.Length == 4)
                    {
                        lineCount = groupings.Length;
                        width = groupings[2].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2) - groupings[1].Average(a => Math.Round((double)a.P1.X + a.P2.X) / 2);
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
