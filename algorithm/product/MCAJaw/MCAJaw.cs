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
using ApexVisIns.Product;

namespace ApexVisIns
{

    public partial class MainWindow : System.Windows.Window
    {

        private readonly Dictionary<string, Rect> JawROIs = new()
        {
            { "粗定位左", new Rect(310, 260, 230, 200) },
            { "粗定位右", new Rect(540, 260, 230, 200) },
        };


        public void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3)
        {
            // 1. 擷取影像 
            // 2. 取得 Canny
            // 3. 取得基準點
            // 4. 依據基準點計算各尺寸
            // 5. 依據 SpecList 決定要不要判定 OK / NG
            // 6. 重複以上 4 次
            // 7. 計算平均結果並新增


        }


        public void JawInsSequenceCam1(Mat src)
        {
            List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting TargetSpec;

            OpenCvSharp.Point baseL;
            OpenCvSharp.Point baseR;

            Mat canny;
            double CenterX;

            // 1. 取得 Canny 影像
            // 2. 取得基準點 (左爪右下 & 右爪左下)

            GetCoarsePos(src, out baseL, out baseR);
            CenterX = (baseL.X + baseR.X) / 2;

            Debug.WriteLine($"{baseL} {baseR} {CenterX}");

            // Methods.GetCanny();

            TargetSpec = specList.Find(s => s.Item == "0.005MAX");
            // 計算輪廓度 // LY、RY 輪廓度基準，後面會用到
            CalContourValue(src, baseL, baseR, out double LCY, out double RCY);
             // 計算前開 // LCX、LCR 前開基準，後面會用到
            CalFrontDistanceValue(src, baseL, baseR, out double LX, out double RX, out double d_front);

            // 計算 0.008 左
            Cal008DistanceValue(src, baseL, LX, out double LTX, out double d_008L);
            // 計算 0.008 右
            Cal008DistanceValue(src, baseR, RX, out double RTX, out double d_008R);

            // 計算 0.013 左
            Cal013DistanceValue(src, baseL, 0, out double LtopY, out double LbotY, out double d_013L);
            // 計算 0.013 左
            Cal013DistanceValue(src, baseR, 0, out double RtopY, out double RbotY, out double d_013R);

            // 計算 0.024 左
            double d_024L = Math.Abs(LCY - LbotY);
            // 計算 0.024 右
            double d_024R = Math.Abs(RCY - RbotY);
        }

        public void JawInsSequenceCam2(Mat src)
        {
            List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting TargetSpec;


            TargetSpec = specList.Find(s => s.Item == "0.088-R");
            // 

            TargetSpec = specList.Find(s => s.Item == "0.088-L");




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
        /// <param name="leftPt">左邊基準點</param>
        /// <param name="rightPt">右邊基準點</param>
        /// <param name="LeftY">(out) 輪廓度左邊 Y 座標</param>
        /// <param name="RightY">(out) 輪廓度右邊 Y 座標</param>
        /// <param name="correction">校正值 (inch)</param>
        /// <param name="upperLimit">管制上限 (default: 0.005)</param>
        /// <returns>OK / NG</returns>
        public bool CalContourValue(Mat src, OpenCvSharp.Point leftPt, OpenCvSharp.Point rightPt, out double LeftY, out double RightY, double correction = 0, double upperLimit = 0.005)
        {
            // 計算 roi
            Rect left = new(leftPt.X - 20, leftPt.Y - 40, 20, 20);
            Rect right = new(rightPt.X + 1, rightPt.Y - 40, 20, 20);

            double sumLength = 0;
            LineSegmentPoint[] lineH;

            //using Mat leftMat = new(src, left);
            //using Mat rightMat = new(src, right);

            Methods.GetRoiCanny(src, left, 75, 150, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 75, 150, out Mat rightCanny);

            //Methods.GetCanny(leftMat, 75, 150, out Mat leftCanny);
            //Methods.GetCanny(rightMat, 75, 150, out Mat rightCanny);

            // 左邊
            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 5, 0);
            sumLength = lineH.Sum(line => line.Length());
            LeftY = lineH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);

            Debug.WriteLine($"Left Y: {LeftY} {sumLength}");

            // 右邊
            Methods.GetHoughLinesHFromCanny(rightCanny, right.Location, out lineH, 5, 0);
            sumLength = lineH.Sum(line => line.Length());
            RightY = lineH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);

            Debug.WriteLine($"Right Y: {RightY} {sumLength}");

            leftCanny.Dispose();
            rightCanny.Dispose();

            // 確認 OK / NG
            return Math.Abs(LeftY - RightY) * 0.249 < upperLimit;
        }


        /// <summary>
        /// 計算前開 (計算開度差用)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="leftPt">左邊基準點</param>
        /// <param name="rightPt">右邊基準點</param>
        /// <param name="distance">(out) 前開距離</param>
        /// <param name="correction">校正值 (inch)</param>
        public void CalFrontDistanceValue(Mat src, OpenCvSharp.Point leftPt, OpenCvSharp.Point rightPt, out double leftX, out double rightX, out double distance, double correction = 0)
        {
            // 計算 roi
            Rect leftRoi = new(leftPt.X - 35, leftPt.Y - 85, 26, 40);
            Rect rightRoi = new(rightPt.X + 9, rightPt.Y - 85, 26, 40);

            double sumLength = 0;
            LineSegmentPoint[] lineV;

            Methods.GetRoiCanny(src, leftRoi, 75, 150, out Mat leftCanny);
            Methods.GetRoiCanny(src, rightRoi, 75, 150, out Mat rightCanny);

            // 左
            Methods.GetHoughLinesVFromCanny(leftCanny, leftRoi.Location, out lineV, 5, 0);
            sumLength = lineV.Sum(line => line.Length());
            leftX = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);

            // 右
            Methods.GetHoughLinesVFromCanny(rightCanny, rightRoi.Location, out lineV, 5, 0);
            sumLength = lineV.Sum(line => line.Length());
            rightX = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);

            // 計算前開距離
            distance = Math.Abs(leftX - rightX) + correction;

            leftCanny.Dispose();
            rightCanny.Dispose();
        }


        /// <summary>
        /// 計算 0.008 距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="compareX">比較基準 (量測前開時取得)</param>
        /// <param name="toothX">牙齒 X 座標</param>
        /// <param name="distance">0.008 量測值</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        /// <returns>OK / NG</returns>
        public bool Cal008DistanceValue(Mat src, OpenCvSharp.Point basePoint, double compareX, out double toothX, out double distance, double correction = 0, double limitL = 0.006, double limitU = 0.010)
        {
            // 計算 roi
            Rect roi = new(basePoint.X - 10, basePoint.Y - 140, 20, 150);

            double sumLength = 0;
            LineSegmentPoint[] lineV;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesVFromCanny(canny, roi.Location, out lineV, 5, 0);

            // 總長
            sumLength = lineV.Sum(line => line.Length());
            // 計算平均 X 座標
            toothX = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);
            // 計算 0.008距離
            distance = Math.Abs(toothX - compareX) + correction;
            // 銷毀 canny
            canny.Dispose();

            return limitL < distance && distance < limitU;
        }


        /// <summary>
        /// 計算 0.013 距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="leftRight">(deprecate) 暫保留</param>
        /// <param name="topY">上邊緣</param>
        /// <param name="botY">下邊緣</param>
        /// <param name="distance">0.013 </param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        public bool Cal013DistanceValue(Mat src, OpenCvSharp.Point basePoint, int leftRight, out double topY, out double botY, out double distance, double correction = 0, double limitL = 0.011, double limitU = 0.015)
        {
            // 計算 roi
            Rect roi = new(basePoint.X - 20, basePoint.Y - 140, 40, 60);

            double sumLength = 0;
            LineSegmentPoint[] lineH;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out lineH, 5, 0);

            double min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            double max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            double center = (min + max) / 2;

            // 小於中心值
            IEnumerable<LineSegmentPoint> minH = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 < center);
            // minH 總長
            sumLength = minH.Sum(line => line.Length());
            // 計算平均 Y 座標
            topY = minH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 > center);
            // minH 總長
            sumLength = maxH.Sum(line => line.Length());
            // 計算平均 Y 座標
            botY = maxH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);
            // 計算 0.013 距離
            distance = Math.Abs(topY - botY) + correction;
            // 銷毀 canny
            canny.Dispose();

            return limitL < distance && distance < limitU;
        }


        public void GetYPos()
        {

        }

    }
}
