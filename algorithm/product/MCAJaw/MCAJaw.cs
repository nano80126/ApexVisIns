﻿using ApexVisIns.Product;
using Basler.Pylon;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApexVisIns
{

    public partial class MainWindow : System.Windows.Window
    {
        #region 單位換算
        private readonly double Cam1PixelSize = 2.2 * 1e-3;
        private readonly double Cam2PixelSize = 2.2 * 1e-3;
        private readonly double Cam3PixelSize = 4.5 * 1e-3;

        private readonly double cam1Mag = 0.218;
        private readonly double cam2Mag = 0.255;
        private readonly double cam3Mag = 0.11;

        private double Cam1Unit => Cam1PixelSize / 25.4 / cam1Mag;
        private double Cam2Unit => Cam2PixelSize / 25.4 / cam2Mag;
        private double Cam3Unit => Cam3PixelSize / 25.4 / cam3Mag;
        #endregion

        private readonly Dictionary<string, Rect> JawROIs = new()
        {
            { "粗定位左", new Rect(310, 260, 230, 200) },
            { "粗定位右", new Rect(540, 260, 230, 200) },
            { "治具定位", new Rect(460, 900, 160, 100) },
            { "後開位置", new Rect(320, 570, 440, 60) },
            { "側面定位", new Rect(460, 110, 240, 130) }
        };

        public void ListJawParam()
        {
            Debug.WriteLine($"Camera 1 Unit: 1px = {Cam1Unit} inch");
            Debug.WriteLine($"Camera 2 Unit: 1px = {Cam2Unit} inch");
            Debug.WriteLine($"Camera 3 Unit: 1px = {Cam3Unit} inch");
        }

        public async void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3)
        {
            // 1. 擷取影像 
            // 2. 取得 Canny
            // 3. 取得基準點
            // 4. 依據基準點計算各尺寸
            // 5. 依據 SpecList 決定要不要判定 OK / NG
            // 6. 重複以上 4 次
            // 7. 計算平均結果並新增

            List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            double d_front = 0;
            double d_back = 0;

            List<Task> task1 = new();
            List<Task> task2 = new();
            List<Task> task3 = new();

            #region results
            Dictionary<string, List<double>> cam1results = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> cam2results = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> cam3results = new Dictionary<string, List<double>>();
            #endregion

            for (int i = 0; i < 2; i++)
            {
                #region CAMERA 3
                // COM2 光源控制器 (24V, 2CH)
                LightCtrls[1].SetAllChannelValue(96, 0);
                // 等待光源
                _ = SpinWait.SpinUntil(() => false, 100);

                int count = 0;
                // 拍照要 Dispacker
                Dispatcher.Invoke(() =>
                {
                    while (count < 2)
                    {
                        cam1.Camera.ExecuteSoftwareTrigger();
                        using IGrabResult grabResult = cam1.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                        if (grabResult != null && grabResult.GrabSucceeded)
                        {
                            Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                            task1.Add(Task.Run(() => JawInsSequenceCam1(mat, specList, cam1results)));

                            ImageSource1 = mat.ToImageSource();
                            count++;
                        }
                    }
                });

                //DateTime t1 = DateTime.Now;
                //foreach (string key in cam1results.Keys)
                //{
                //    double avg = cam1results[key].Average();
                //    JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                //    MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                //    if (key == "前開") { d_front = avg; }
                //}
                //Debug.WriteLine($"t1 takes: {(DateTime.Now - t1).TotalMilliseconds}");
                #endregion


                #region CAMERA 2 
                // COM2 光源控制器 (24V, 2CH)
                LightCtrls[1].SetAllChannelValue(0, 128);
                // 等待光源
                _ = SpinWait.SpinUntil(() => false, 100);

                count = 0;
                // 拍照要 Dispacker
                Dispatcher.Invoke(() =>
                {
                    while (count < 2)
                    {
                        cam2.Camera.ExecuteSoftwareTrigger();
                        using IGrabResult grabResult = cam2.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                        if (grabResult != null && grabResult.GrabSucceeded)
                        {
                            Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                            task2.Add(Task.Run(() => JawInsSequenceCam2(mat, specList, cam2results)));
                            //JawInsSequenceCam2(mat, specList, cam2results);

                            ImageSource2 = mat.ToImageSource();
                            count++;
                        }
                    }
                });
                //DateTime t2 = DateTime.Now;
                //foreach (string key in cam2results.Keys)
                //{
                //    double avg = cam2results[key].Average();
                //    JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                //    MCAJaw.JawSpecGroup.Collection2.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                //    if (key == "後開") { d_back = avg; }
                //}
                //Debug.WriteLine($"t2 takes: {(DateTime.Now - t2).TotalMilliseconds}");
                #endregion
            }

            #region CAMERA 3 平面度
            // COM2 光源控制器 (24V, 2CH)
            LightCtrls[1].SetAllChannelValue(128, 256);
            // 等待光源
            _ = SpinWait.SpinUntil(() => false, 100);

            int count2 = 0;
            // 拍照要 Dispacker
            Dispatcher.Invoke(() =>
            {
                while (count2 < 4)
                {
                    cam3.Camera.ExecuteSoftwareTrigger();
                    IGrabResult grabResult = cam3.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                    if (grabResult != null && grabResult.GrabSucceeded)
                    {
                        Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                        task3.Add(Task.Run(() => JawInsSequenceCam3(mat, specList, cam3results)));
                        //JawInsSequenceCam3(mat, specList, cam3results);

                        ImageSource3 = mat.ToImageSource();

                        count2++;
                    }
                }
            });
            #endregion

            LightCtrls[1].SetAllChannelValue(0, 0);
            await Task.WhenAll(task1.Concat(task2).Concat(task3));

            // Camera 1
            DateTime t1 = DateTime.Now;
            foreach (string key in cam1results.Keys)
            {
                //Debug.WriteLine($"{key} {cam1results[key].Count}");
                double avg = cam1results[key].Average();
                JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                if (key == "前開") { d_front = avg; }
            }
            Debug.WriteLine($"t1 takes: {(DateTime.Now - t1).TotalMilliseconds} ms");

            // Camera 2
            DateTime t2 = DateTime.Now;
            foreach (string key in cam2results.Keys)
            {
                //Debug.WriteLine($"{key} {cam2results[key].Count}");
                double avg = cam2results[key].Average();
                JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                MCAJaw.JawSpecGroup.Collection2.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                if (key == "後開") { d_back = avg; }
            }
            Debug.WriteLine($"t2 takes: {(DateTime.Now - t2).TotalMilliseconds} ms");


            #region 開度差
            JawSpecSetting spec2 = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == "開度差");
            if (spec2.Enable)
            {
                MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(spec2.Item, spec2.CenterSpec, spec2.LowerCtrlLimit, spec2.UpperCtrlLimit, Math.Abs(d_front - d_back)));
            }
            #endregion

            // Camera 3
            DateTime t3 = DateTime.Now;
            foreach (string key in cam3results.Keys)
            {
                //Debug.WriteLine($"{key} {cam3results[key].Count}");
                double avg = cam3results[key].Average();
                JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                MCAJaw.JawSpecGroup.Collection3.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
            }
            Debug.WriteLine($"t3 takes: {(DateTime.Now - t3).TotalMilliseconds} ms");
            Debug.WriteLine($"Total takes {(DateTime.Now - t1).TotalMilliseconds} ms");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        public void JawInsSequenceCam1(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting spec;

            OpenCvSharp.Point baseL;
            OpenCvSharp.Point baseR;

            //Mat canny;
            double CenterX;

            // 1. 取得 Canny 影像
            // 2. 取得基準點 (左爪右下 & 右爪左下)

            GetCoarsePos(src, out baseL, out baseR);
            CenterX = (baseL.X + baseR.X) / 2;

            // Debug.WriteLine($"{baseL} {baseR} {CenterX}");
            // Methods.GetCanny();

            #region 計算輪廓度 // LCY、RCY 輪廓度基準，後面會用到
            spec = specList?[11];
            CalContourValue(src, baseL, baseR, out double LCY, out double RCY, out double d_005Max, spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_005Max);
                }
            }
            // Debug.WriteLine($"d005MAX: {d_005Max}");
            #endregion

            //return;

            #region 計算前開 // LX、RX 前開基準，後面會用到
            spec = specList?[9];
            CalFrontDistanceValue(src, baseL, baseR, out double LX, out double RX, out double d_front, spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_front);
                }
            }
            #endregion

            #region 計算 0.008 左 (實際上是右)
            spec = specList?[2];
            if (spec != null && spec.Enable && results != null)
            {
                Cal008DistanceValue(src, baseL, LX, out double LTX, out double d_008R, spec.Correction);
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_008R);
                }
            }
            #endregion

            #region 計算 0.008 右 (實際上是左)
            spec = specList?[3];
            if (spec != null && spec.Enable && results != null)
            {
                Cal008DistanceValue(src, baseR, RX, out double RTX, out double d_008L, spec.Correction);
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_008L);
                }
            }
            #endregion

            #region 計算 0.013 左 (實際上是右)
            spec = specList?[4];
            Cal013DistanceValue(src, baseL, 0, out double LtopY, out double LbotY, out double d_013R, spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_013R);
                }
            }
            #endregion

            #region 計算 0.013 右 (實際上是左)
            spec = specList?[5];
            Cal013DistanceValue(src, baseR, 1, out double RtopY, out double RbotY, out double d_013L, spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_013L);
                }
            }
            #endregion

            #region 計算 0.024 左 (實際上是右)
            spec = specList?[6];
            double d_024R = (Math.Abs(LCY - LtopY) * Cam1Unit) + (spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_024R);
                }
            }
            #endregion

            #region 計算 0.024 右 (實際上是左)
            spec = specList?[7];
            double d_024L = (Math.Abs(RCY - RtopY) * Cam1Unit) + (spec != null ? spec.Correction : 0);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_024L);
                }
            }
            #endregion
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
        }

        public void JawInsSequenceCam2(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting spec;

            // 取得基準線
            GetJigPos(src, out double JigPosY);
            Debug.WriteLine($"JigPos: {JigPosY}");

            #region 計算後開
            spec = specList?[8];
            CalBackDistanceValue(src, out double LX, out double RX, out double d_back, spec != null ? spec.Correction : 0);
            if (spec != null)
            {
                if (spec.Enable && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_back);
                    }
                }
            }
            #endregion

            #region 計算 0.088-R
            spec = specList?[0];
            if (spec != null && spec.Enable && results != null)
            {
                Cal088DistanceValue(src, JigPosY, RX, 1, out double d_088R, spec.Correction);
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_088R);
                }
            }
            #endregion

            #region 計算 0.088-L
            spec = specList?[1];
            if (spec != null && spec.Enable && results != null)
            {
                Cal088DistanceValue(src, JigPosY, LX, 0, out double d_088L, spec.Correction);
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(d_088L);
                }
            }
            #endregion

            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
        }

        public void JawInsSequenceCam3(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            JawSpecSetting spec;

            // 取得 背景 POM 基準 Y
            GetPomDatum(src, out double datumY);

            #region 計算 平面度
            spec = specList?[12];
            //Cal007FlatnessValue(src, datumY, out double f_007, spec.Correction);
            Cal007FlatnessValue(src, datumY, out double f_007);
            if (spec != null && spec.Enable && results != null)
            {
                lock (results)
                {
                    if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                    results[spec.Item].Add(f_007);
                }
            }
            #endregion

            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
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

            Mat LeftMat = new(src, LeftRoi);
            Mat RightMat = new(src, RightROi);

            Methods.GetContours(LeftMat, LeftRoi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] LeftCon);
            Methods.GetContours(RightMat, RightROi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] RightCon);

            // Cv2.ImShow($"leftMat", LeftMat);
            // Cv2.ImShow($"rightMat", RightMat);
            // Debug.WriteLine($"LeftCon Length: {LeftCon.Length}");
            // Debug.WriteLine($"RightCon Length: {RightCon.Length}");

            int maxX_L = LeftCon.Max(c => c.X);
            int maxY_L = LeftCon.Max(c => c.Y);

            int minX_R = RightCon.Min(c => c.X);
            int maxY_R = RightCon.Max(c => c.Y);

            // Debug.WriteLine($"{minX_R} { maxY_R}");
            LeftPoint = new OpenCvSharp.Point(maxX_L, maxY_L);
            RightPoint = new OpenCvSharp.Point(minX_R, maxY_R);
            // LeftPoint = new OpenCvSharp.Point(0, 0);
            // RightPoint = new OpenCvSharp.Point(0, 0);

            LeftMat.Dispose();
            RightMat.Dispose();
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
        public bool CalContourValue(Mat src, OpenCvSharp.Point leftPt, OpenCvSharp.Point rightPt, out double LeftY, out double RightY, out double d_005max, double correction = 0, double upperLimit = 0.005)
        {
            // 計算 roi
            Rect left = new(leftPt.X - 19, leftPt.Y - 150, 18, 70);
            Rect right = new(rightPt.X + 2, rightPt.Y - 150, 18, 70);

            double sumLength = 0;

            LineSegmentPoint[] lineH;
            double min, max, center;

            // using Mat leftMat = new(src, left);
            // using Mat rightMat = new(src, right);

            Methods.GetRoiCanny(src, left, 75, 150, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 75, 150, out Mat rightCanny);

            // Cv2.ImShow("Left Canny", leftCanny);
            // Cv2.ImShow("Right Canny", rightCanny);

            #region 左邊
            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 3, 0, 3);
            // sumLength = lineH.Sum(line => line.Length());
            // LeftY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = (min + max) / 2;

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH_L = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 > center);
            // 計算 maxH 總長
            sumLength = maxH_L.Sum(line => line.Length());
            // 計算平均 Y 座標
            LeftY = maxH_L.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

#if false
            //foreach (LineSegmentPoint l in lineH)
            //{
            //    Debug.WriteLine($"{l.P1} {l.P2} {l.Length()}");
            //}
            //Debug.WriteLine($"------------------------------");
            Debug.WriteLine($"---------------L---------------");
            foreach (LineSegmentPoint l in maxH_L)
            {
                Debug.WriteLine($"{l.P1} {l.P2} {l.Length()}");
            }
#endif
            #endregion

            #region 右邊
            Methods.GetHoughLinesHFromCanny(rightCanny, right.Location, out lineH, 3, 0, 3);
            // sumLength = lineH.Sum(line => line.Length());
            // RightY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = (min + max) / 2;

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH_R = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 > center);
            // 計算 maxH 總長
            sumLength = maxH_R.Sum(line => line.Length());
            // 計算平均 Y 座標
            RightY = maxH_R.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

#if false
            //foreach (LineSegmentPoint l in lineH)
            //{
            //    Debug.WriteLine($"{l.P1} {l.P2} {l.Length()}");
            //}
            //Debug.WriteLine($"------------------------------");
            Debug.WriteLine($"---------------R---------------");
            foreach (LineSegmentPoint l in maxH_R)
            {
                Debug.WriteLine($"{l.P1} {l.P2} {l.Length()}");
            }
#endif
            #endregion

            // 計算 輪廓度
            d_005max = (Math.Abs(LeftY - RightY) * Cam1Unit) + correction;
            //Debug.WriteLine($"005MAX PT: {LeftY - RightY}");

            #region Dispose
            leftCanny.Dispose();
            rightCanny.Dispose();
            #endregion

            // 確認 OK / NG
            return d_005max <= upperLimit;
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
            distance = (Math.Abs(leftX - rightX) * Cam1Unit) + correction;
            Debug.WriteLine($"前開: {Math.Abs(leftX - rightX)}");

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
            distance = (Math.Abs(toothX - compareX) * Cam1Unit) + correction;
            // 銷毀 canny
            canny.Dispose();

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// 計算 0.013 距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="leftRight">(deprecate) 暫保留</param>
        /// <param name="topY">上邊緣</param>
        /// <param name="botY">下邊緣</param>
        /// <param name="distance">(out) 0.013 距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        public bool Cal013DistanceValue(Mat src, OpenCvSharp.Point basePoint, int leftRight, out double topY, out double botY, out double distance, double correction = 0, double limitL = 0.011, double limitU = 0.015)
        {
            // 計算 roi
            Rect roi = new(basePoint.X - 20, basePoint.Y - 70, 40, 90);

            double sumLength = 0;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 5, 0);

            //foreach (LineSegmentPoint item in lineH)
            //{
            //    Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
            //}
            //Debug.WriteLine("----------------------------------------");

            double min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            double max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            double center = (min + max) / 2;

            //Cv2.ImShow($"canny{leftRight}", canny);

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
            distance = (Math.Abs(topY - botY) * Cam1Unit) + correction;
            // 銷毀 canny
            canny.Dispose();
            //Debug.WriteLine(distance);

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// Camera 2 取得治具基準線
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="JigPosY">治具基準線</param>
        public void GetJigPos(Mat src, out double JigPosY)
        {
            Rect JigRoi = JawROIs["治具定位"];

            Methods.GetRoiCanny(src, JigRoi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, JigRoi.Location, out LineSegmentPoint[] lineH, 25, 10, 3);
            canny.Dispose();

            double sumLength = lineH.Sum(line => line.Length());
            JigPosY = lineH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);
        }

        /// <summary>
        ///  計算後開
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="leftX">開度左 X</param>
        /// <param name="rightX">開度右 X</param>
        /// <param name="distance">後開距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        /// <returns></returns>
        public bool CalBackDistanceValue(Mat src, out double leftX, out double rightX, out double distance, double correction = 0, double limitL = 0.098, double limitU = 0.101)
        {
            // roi
            Rect roi = JawROIs["後開位置"];

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesVFromCanny(canny, roi.Location, out LineSegmentPoint[] lineV, 5, 3, 5);

            int l = lineV.Min(line => (line.P1.X + line.P2.X) / 2);
            int r = lineV.Max(line => (line.P1.X + line.P2.X) / 2);
            double c = (l + r) / 2;

            // 開度左
            IEnumerable<LineSegmentPoint> lineL = lineV.Where(line => line.P1.X < c);

            double sumL = lineL.Sum(line => line.Length());
            leftX = lineL.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumL);

            //開度右
            IEnumerable<LineSegmentPoint> lineR = lineV.Where(line => line.P1.X > c);

            double sumR = lineR.Sum(line => line.Length());
            rightX = lineR.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumR);

            //Dispatcher.Invoke(() =>
            //{
            //    Cv2.ImShow($"src", new Mat(src, roi));
            //    Cv2.ImShow($"canny", canny);
            //});

            // 計算 後開距離
            distance = (Math.Abs(rightX - leftX) * Cam2Unit) + correction;
            // 銷毀 canny
            //canny.Dispose();

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// 計算0.088 距離 (左右分開)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="baseJigY">治具邊緣</param>
        /// <param name="compareX">比較基準 (量測後開取得)</param>
        /// <param name="side">左: 0, 右: 1</param>
        /// <param name="distance">(out) 0.088 距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        /// <returns></returns>
        public bool Cal088DistanceValue(Mat src, double baseJigY, double compareX, int side, out double distance, double correction = 0, double limitL = 0.0855, double limitU = 0.0905)
        {
            // roi
            Rect roi = side == 0 ? new Rect(100, (int)(baseJigY - 70), 60, 60) : new Rect(920, (int)(baseJigY - 70), 60, 60);


            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesVFromCanny(canny, roi.Location, out LineSegmentPoint[] lineV, 20, 10, 3);

            double sumLength = lineV.Sum(line => line.Length());
            double X = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);

            // Cv2.ImShow($"mat", new Mat(src, roi));
            // Cv2.ImShow($"canny", canny);
            //foreach (LineSegmentPoint item in lineV)
            //{
            //    Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
            //}
            //Debug.WriteLine($"{X}");

            // 計算 0.088 距離
            distance = (Math.Abs(compareX - X) * Cam2Unit) + correction;
            // 銷毀 canny
            canny.Dispose();

            return limitL <= distance && distance <= limitU;
        }


        /// <summary>
        /// Camera 3 取得 POM 基準
        /// </summary>
        public void GetPomDatum(Mat src, out double datumY)
        {
            Rect roi = JawROIs["側面定位"];

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 25, 10, 3);
            datumY = lineH.Min(line => (line.P1.Y + line.P2.Y) / 2);

            canny.Dispose();
        }


        /// <summary>
        /// 計算平面度
        /// </summary>
        /// <returns></returns>
        public bool Cal007FlatnessValue(Mat src, double baseDatumY, out double flatValue, double correction = 0, double limitU = 0.007)
        {
            // roi
            Rect roi1 = new(270, (int)(baseDatumY + 30), 460, 40);
            Rect roi2 = new(800, (int)(baseDatumY + 30), 100, 40);


            Methods.GetRoiCanny(src, roi1, 25, 50, out Mat canny1);
            Methods.GetHoughLinesHFromCanny(canny1, roi1.Location, out LineSegmentPoint[] lineH1, 5);

            Methods.GetRoiCanny(src, roi2, 25, 50, out Mat canny2);
            Methods.GetHoughLinesHFromCanny(canny2, roi2.Location, out LineSegmentPoint[] lineH2, 5);

            LineSegmentPoint[] line = lineH1.Concat(lineH2).OrderBy(line => line.P1.Y + line.P2.Y).OrderBy(line => line.P1.X).ToArray();

            List<LineSegmentPoint> lineList = new();

            //Cv2.ImShow("mat1", m);
            //Cv2.ImShow("mat2", new Mat(src, roi2));
            //Cv2.ImShow("canny1", canny1);
            //Cv2.ImShow("canny2", canny2);

            // 過濾重複點
            for (int i = 0; i < line.Length; i++)
            {
                //Debug.WriteLine($"{line[i].P1} {line[i].P2} {line[i].Length()}");
                Cv2.Line(src, line[i].P1, line[i].P2, Scalar.Black, 1);

                // 第一條線直接新增
                if (i == 0)
                {
                    lineList.Add(line[i]);
                    continue;
                }

                //lineList.All(l => l.P2.X < line[i].P1.X);

                // 線二P1.X > 列表所有線P2.X : 直接新增 
                if (lineList.All(l => l.P2.X < line[i].P1.X))
                {
                    // 新增
                    lineList.Add(line[i]);
                }
                // 線二 p1 介於 線一中間
                else if (line[i].P1.X >= lineList[^1].P1.X && line[i].P1.X <= lineList[^1].P2.X)
                {
                    // 兩線接近 : 直接新增
                    if (Math.Abs(line[i].P2.Y - lineList[^1].P1.Y) <= 1)
                    {
                        lineList.Add(line[i]);
                        continue;
                    }

                    if (line[i].P1.Y < lineList[^1].P1.Y)
                    {
                        // 取代
                        lineList[^1] = line[i];
                    }
                }
            }

            /*
            Debug.WriteLine($"---------------------------------------"); // foreach (LineSegmentPoint item in lineList)
            foreach (LineSegmentPoint item in lineList)
            {
                Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
            }
            Debug.WriteLine($"---------------------------------------"); // foreach (LineSegmentPoint item in lineList)
            */

            flatValue = (lineList.Max(l => Math.Max(l.P1.Y, l.P2.Y)) - lineList.Min(l => Math.Min(l.P1.Y, l.P2.Y))) * Cam3Unit + correction;

            canny1.Dispose();
            canny2.Dispose();

            return flatValue <= limitU;
        }

    }
}
