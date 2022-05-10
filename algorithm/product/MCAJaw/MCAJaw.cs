using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenCvSharp;
using System.Diagnostics;
using Basler.Pylon;
using System.Threading;
using System.Runtime.InteropServices;


namespace ApexVisIns
{

    public partial class MainWindow : System.Windows.Window
    {

        private readonly Dictionary<string, Rect> JawROIs = new()
        {
            { "粗定位左", new Rect(310, 260, 230, 200) },
            { "粗定位右", new Rect(540, 260, 230, 200) },
        };


        public void JawInvSequence(Mat src)
        {
            OpenCvSharp.Point baseP1;
            OpenCvSharp.Point baseP2;

            Mat canny;
            OpenCvSharp.Point centerPoint;


            // 1. 取得 Canny 影像
            // 2. 取得基準點
            // 3. 
            
            GetCoarsePos(src, out baseP1, out baseP2);

            Debug.WriteLine($"{baseP1} {baseP2}");

            //Methods.GetCanny();

            CalContourValue(src, baseP1, baseP2, out double LY, out double RY);

        }

        /// <summary>
        /// 取得左右兩邊基準點 (極端點)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="LeftPoint">左半邊極端點</param>
        /// <param name="RightPoint">右半邊極端點</param>
        public void GetCoarsePos(Mat src, out OpenCvSharp.Point LeftPoint, out OpenCvSharp.Point RightPoint)
        {
            Rect LeftRoi = JawROIs["粗定位左"];
            Rect RightROi = JawROIs["粗定位右"];

            using Mat LeftMat = new(src, LeftRoi);
            using Mat RightMat = new(src, RightROi);

            Methods.GetContours(LeftMat, LeftRoi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] LeftCon);
            Methods.GetContours(RightMat, RightROi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] RightCon);

            int maxX_L = LeftCon.Max(c => c.X);
            int maxY_L = LeftCon.Max(c => c.Y);

            int minX_R = RightCon.Min(c => c.X);
            int maxY_R = RightCon.Max(c => c.Y);

            Debug.WriteLine($"{minX_R} { maxY_R}");

            LeftPoint = new OpenCvSharp.Point(maxX_L, maxY_L);
            RightPoint = new OpenCvSharp.Point(minX_R, maxY_R);
        }

        /// <summary>
        /// 計算輪廓度 (0.005MAX)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="leftPt">左邊基準</param>
        /// <param name="rightPt">右邊</param>
        /// <param name="LeftY">左邊 Y 座標</param>
        /// <param name="RightY">右邊 Y 座標</param>
        /// <param name="upperLimit">輪廓度上限 (default: 0.005)</param>
        /// <returns></returns>
        public bool CalContourValue(Mat src, OpenCvSharp.Point leftPt, OpenCvSharp.Point rightPt, out double LeftY, out double RightY, double upperLimit = 0.005)
        {
            Rect left = new(leftPt.X - 20, leftPt.Y - 40, 20, 20);
            Rect right = new(rightPt.X + 1, rightPt.Y - 40, 20, 20);

            double sumLength = 0;
            LineSegmentPoint[] lineH;

            using Mat leftMat = new(src, left);
            using Mat rightMat = new(src, right);

            Methods.GetRoiCanny(src, left, 75, 150, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 75, 150, out Mat rightCanny);

            //Methods.GetCanny(leftMat, 75, 150, out Mat leftCanny);
            //Methods.GetCanny(rightMat, 75, 150, out Mat rightCanny);

            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 5, 0);
            sumLength = lineH.Sum(line => line.Length());
            LeftY = lineH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);

            foreach (LineSegmentPoint item in lineH)
            {
                LeftY += (item.P1.Y + item.P2.Y) / 2 * item.Length() / sumLength;
            }
            Debug.WriteLine($"Left Y: {LeftY} {sumLength}");
            Debug.WriteLine("");

            Methods.GetHoughLinesHFromCanny(rightCanny, right.Location, out lineH, 5, 0);
            sumLength = lineH.Sum(line => line.Length());
            RightY = lineH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);

            RightY = 0;
            foreach (LineSegmentPoint item in lineH)
            {
                RightY += (item.P1.Y + item.P2.Y) / 2 * item.Length() / sumLength;
            }
            Debug.WriteLine($"Right Y: {RightY} {sumLength}");
            Debug.WriteLine("");

            // 確認 OK / NG
            return Math.Abs(LeftY - RightY) * 0.249 < upperLimit;
        }


        public void GetYPos()
        {

        }

    }
}
