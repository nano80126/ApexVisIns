using ApexVisIns.Product;
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

        private readonly double cam1Mag = 0.21745;
        private readonly double cam2Mag = 0.255;
        private readonly double cam3Mag = 0.11;

        private double Cam1Unit => Cam1PixelSize / 25.4 / cam1Mag;
        private double Cam2Unit => Cam2PixelSize / 25.4 / cam2Mag;
        private double Cam3Unit => Cam3PixelSize / 25.4 / cam3Mag;
        #endregion

        #region 
        /// <summary>
        /// Jaw 左右 enum，013、024等演算法所需 param
        /// </summary>
        public enum JawPos
        {
            Left = 1,
            Right = 2,
        }

        private readonly Dictionary<string, Rect> JawROIs = new()
        {
            { "有料檢知", new Rect(185, 345, 710, 30) },
            { "粗定位左", new Rect(310, 260, 230, 300) },
            { "粗定位右", new Rect(540, 260, 230, 300) },
            { "治具定位", new Rect(460, 900, 160, 100) },
            { "後開位置", new Rect(320, 575, 440, 40) },
            { "側面定位", new Rect(460, 90, 240, 130) }
        };
        #endregion


        public void ListJawParam()
        {
            Debug.WriteLine($"Camera 1 Unit: 1px = {Cam1Unit} inch");
            Debug.WriteLine($"Camera 2 Unit: 1px = {Cam2Unit} inch");
            Debug.WriteLine($"Camera 3 Unit: 1px = {Cam3Unit} inch");
        }

        /// <summary>
        /// Jaw 檢驗流程
        /// </summary>
        /// <param name="cam1">相機 1</param>
        /// <param name="cam2">相機 2</param>
        /// <param name="cam3">相機 3</param>
        /// <param name="jawFullSpecIns">檢驗結果物件</param>
        public void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3, JawFullSpecIns jawFullSpecIns = null)
        {
            // 1. 擷取影像 
            // 2. 取得 Canny
            // 3. 取得基準點
            // 4. 依據基準點計算各尺寸
            // 5. 依據 SpecList 決定要不要判定 OK / NG
            // 6. 重複以上 4 次
            // 7. 計算平均結果並新增

            try
            {
                // 規格列表
                List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
                JawSpecSetting spec;
                // 有無料
                bool partExist = false;
                // 是否NG (避免重複計算NG數量)
                bool isNG = false;
                // 前開
                double d_front = 0;
                // 後開
                double d_back = 0;
                // 開設張數
                int count = 0;

                List<Task> task1 = new();
                List<Task> task2 = new();
                List<Task> task3 = new();

                #region results
                Dictionary<string, List<double>> cam1results = new();
                Dictionary<string, List<double>> cam2results = new();
                Dictionary<string, List<double>> cam3results = new();
                #endregion

                for (int i = 0; i < 2; i++)
                {
                    #region CAMERA 1
                    // COM2 光源控制器 (24V, 2CH)
                    LightCtrls[1].SetAllChannelValue(96, 0);
                    // 等待光源
                    _ = SpinWait.SpinUntil(() => false, 80);

                    count = 0;
                    // 拍照要 Dispacker
                    Dispatcher.Invoke(() =>
                    {
                        while (count < 3)
                        {
                            cam1.Camera.ExecuteSoftwareTrigger();
                            using IGrabResult grabResult = cam1.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                            if (grabResult != null && grabResult.GrabSucceeded)
                            {
                                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                                if (count == 0)
                                {
                                    if (CheckPart(mat)) { partExist = true; }
                                }

                                if (partExist) { task1.Add(Task.Run(() => JawInsSequenceCam1(mat, specList, cam1results))); }
                                else { count += 999; }  // 跳出迴圈

                                ImageSource1 = mat.ToImageSource();
                                count++;
                            }
                        }
                    });

                    // DateTime t1 = DateTime.Now;
                    // foreach (string key in cam1results.Keys)
                    // {
                    //     double avg = cam1results[key].Average();
                    //     JawSpecSetting spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                    //     MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                    //     if (key == "前開") { d_front = avg; }
                    // }
                    // Debug.WriteLine($"t1 takes: {(DateTime.Now - t1).TotalMilliseconds}");
                    #endregion


                    #region CAMERA 2 
                    // COM2 光源控制器 (24V, 2CH)
                    LightCtrls[1].SetAllChannelValue(0, 128);
                    // 等待光源
                    _ = SpinWait.SpinUntil(() => false, 80);

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

                                if (partExist) { task2.Add(Task.Run(() => JawInsSequenceCam2(mat, specList, cam2results))); }
                                else { count += 999; }  // 跳出迴圈
                                //JawInsSequenceCam2(mat, specList, cam2results);

                                ImageSource2 = mat.ToImageSource();
                                count++;
                            }
                        }
                    });

                    #endregion
                    // 跳出迴圈
                    if (!partExist) { break; }
                }

                #region CAMERA 3 平面度
                // COM2 光源控制器 (24V, 2CH)
                LightCtrls[1].SetAllChannelValue(128, 256);
                // 等待光源
                _ = SpinWait.SpinUntil(() => false, 100);

                count = 0;
                // 拍照要 Dispacker
                Dispatcher.Invoke(() =>
                {
                    while (count < 4)
                    {
                        cam3.Camera.ExecuteSoftwareTrigger();
                        IGrabResult grabResult = cam3.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                        if (grabResult != null && grabResult.GrabSucceeded)
                        {
                            Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                            if (partExist) { task3.Add(Task.Run(() => JawInsSequenceCam3(mat, specList, cam3results))); }
                            else { count += 999; }
                            //JawInsSequenceCam3(mat, specList, cam3results);

                            ImageSource3 = mat.ToImageSource();
                            count++;
                        }
                    }
                });
                #endregion

                LightCtrls[1].SetAllChannelValue(0, 0);
                if (!partExist) { throw new MCAJawException("未檢測到料件"); }

                Debug.WriteLine($"st: {DateTime.Now:mm:ss.fff}");

                // 等待所有 計算完成
                Task.WhenAll(task1.Concat(task2).Concat(task3)).Wait();

                Debug.WriteLine($"end: {DateTime.Now:mm:ss.fff}");

                // Camera 1 結果
                //DateTime stTime = DateTime.Now;
                foreach (string item in cam1results.Keys)
                {
                    // Debug.WriteLine($"{key} {cam1results[key].Count}");
                    double avg = cam1results[item].Average();
                    spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == item);
                    MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
                    // MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection1[^1].OK ? 0 : 1;   // 保留

                    // 先判斷是否已為 NG
                    if (!isNG)
                    {
                        // 判斷是否 ok
                        bool ok = MCAJaw.JawSpecGroup.Collection1[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標記這 piece 為 NG品，避免重複計算NG
                        isNG = !ok;
                    }

                    // 資料庫物件新增
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }

                    if (item == "前開") { d_front = avg; }
                }

                // Camera 2 結果
                foreach (string item in cam2results.Keys)
                {
                    // Debug.WriteLine($"{key} {cam2results[key].Count}");
                    double avg = cam2results[item].Average();
                    spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == item);
                    MCAJaw.JawSpecGroup.Collection2.Add(new JawSpec(item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
                    // MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection2[^1].OK ? 0 : 1;   // 保留

                    // 先判斷是否已為 NG
                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawSpecGroup.Collection2[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }

                    if (item == "後開") { d_back = avg; }
                }

                #region 開度差 (先確認是否啟用)
                spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == "開度差");
                if (spec.Enable)
                {
                    double bfDiff = Math.Abs(d_front - d_back);
                    MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(spec.Item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, bfDiff));
                    //MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection1[^1].OK ? 0 : 1;    // 保留

                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawSpecGroup.Collection1[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增  key, value
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, bfDiff); }
                }
                #endregion

                // Camera 3 結果
                foreach (string item in cam3results.Keys)
                {
                    Debug.WriteLine($"{string.Join(",", cam3results[item])}");
                    double avg = cam3results[item].Min();
                    spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == item);
                    MCAJaw.JawSpecGroup.Collection3.Add(new JawSpec(item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
                    // MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection3[^1].OK ? 0 : 1;   // 保留

                    // 先判斷是否已為 NG
                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawSpecGroup.Collection3[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增  key, value
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }
                }

                // 判斷是否為良品
                MCAJaw.JawInspection.LotResults["good"].Count += MCAJaw.JawSpecGroup.Col1Result && MCAJaw.JawSpecGroup.Col2Result && MCAJaw.JawSpecGroup.Col3Result ? 1 : 0;
                //Debug.WriteLine($"Total takes {(DateTime.Now - stTime).TotalMilliseconds} ms");
            }
            catch (OpenCVException ex)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCV, ex.Message);
            }
            catch (OpenCvSharpException ex)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCVSHARP, ex.Message);
            }
            catch (Exception ex)
            {
                MsgInformer.AddError(MsgInformer.Message.MsgCode.EX, ex.Message);
            }
        }

        /// <summary>
        /// 相機 1 檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="specList">規格列表</param>
        /// <param name="results">檢驗結果</param>
        public void JawInsSequenceCam1(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting spec;
            JawSpecSetting spec2;
            double CenterX;

            try
            {
                GetCoarsePos(src, out Point baseL, out Point baseR);
                CenterX = (baseL.X + baseR.X) / 2;

                // Debug.WriteLine($"{baseL} {baseR} {CenterX}");
                // Methods.GetCanny();

                #region 計算輪廓度 // LCY、RCY 輪廓度基準，後面會用到
                spec = specList?[12];
                CalContourValue(src, baseL, baseR, out double LCY, out double RCY, out double d_005Max, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
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
                spec = specList?[10];
                CalFrontDistanceValue(src, baseL, baseR, out double LX, out double RX, out double d_front, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
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
                spec = specList?[3];
                if (spec != null && spec.Enable && results != null)
                {
                    Cal008DistanceValue(src, baseL, LX, out double LTX, out double d_008R, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_008R);
                    }
                }
                #endregion

                #region 計算 0.008 右 (實際上是左)
                spec = specList?[4];
                if (spec != null && spec.Enable && results != null)
                {
                    Cal008DistanceValue(src, baseR, RX, out double RTX, out double d_008L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_008L);
                    }
                }
                #endregion

                #region 計算 0.013 左 (實際上是右)
                spec = specList?[5];
                Cal013DistanceValue(src, baseL, JawPos.Left, LX, out double LtopY, out double LbotY, out double d_013R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
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
                spec = specList?[6];
                Cal013DistanceValue(src, baseR, JawPos.Right, RX, out double RtopY, out double RbotY, out double d_013L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
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
                spec = specList?[7];
                spec2 = specList?[5];
                double d_024R = (Math.Abs(LCY - LtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret - spec2.CorrectionSecret / 2 : 0);
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
                spec = specList?[8];
                spec2 = specList?[6];
                double d_024L = (Math.Abs(RCY - RtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret - spec2.CorrectionSecret / 2 : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024L);
                    }
                }
                #endregion
                spec = spec2 = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 相機 2 檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="specList">規格列表</param>
        /// <param name="results">檢驗結果</param>
        public void JawInsSequenceCam2(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // List<JawSpecSetting> specList = MCAJaw.JawSpecGroup.SpecList.ToList();
            JawSpecSetting spec;
            double d_088R = 0;
            double d_088L = 0;

            try
            {
                // 取得基準線
                GetJigPos(src, out double JigPosY);

                #region 計算後開
                spec = specList?[9];
                CalBackDistanceValue(src, out double LX, out double RX, out double d_back, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
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
                spec = specList?[0];    // 
                //Cal088DistanceValue(src, JigPosY, RX, JawPos.Right, out d_088R);
                if (spec != null && spec.Enable && results != null)
                {
                    Cal088DistanceValue(src, JigPosY, RX, JawPos.Right, out d_088R, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_088R);
                    }
                }
                #endregion

                #region 計算 0.088-L
                spec = specList?[1];    // 
                //Cal088DistanceValue(src, JigPosY, LX, JawPos.Left, out d_088L);
                if (spec != null && spec.Enable && results != null)
                {
                    Cal088DistanceValue(src, JigPosY, LX, JawPos.Left, out d_088L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_088L);
                    }
                }
                #endregion

                #region 計算 0.088 合
                spec = specList?[2];
                if (spec != null && spec.Enable && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_088R + d_088L);
                    }
                }
                #endregion
                // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
                spec = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 相機 3 檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="specList">規格列表</param>
        /// <param name="results">檢驗結果</param>
        public void JawInsSequenceCam3(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            JawSpecSetting spec;

            try
            {
                // 取得 背景 POM 基準 Y
                GetPomDatum(src, out double datumY);
                //Debug.WriteLine($"datumY: {datumY}");

                #region 計算 平面度
                spec = specList?[13];
                //Cal007FlatnessValue(src, datumY, out double f_007, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                Cal007FlatnessValue(src, datumY, out double f_007);
                if (spec != null && spec.Enable && results != null)
                {
                    //Cal007FlatnessValue(src, datumY, out double f_007, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(f_007);
                    }
                }
                //Debug.WriteLine($"DatumY: {datumY}, f007: {f_007}");
                #endregion
                spec = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CheckPart(Mat src)
        {
            // ROI
            Rect roi = JawROIs["有料檢知"];

            Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte threshHold);
            return threshHold is > 30 and < 200;
        }

        /// <summary>
        /// 取得左右兩邊基準點 (極端點)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="LeftPoint">左半邊極端點</param>
        /// <param name="RightPoint">右半邊極端點</param>
        public void GetCoarsePos(Mat src, out Point LeftPoint, out Point RightPoint)
        {
            Rect LeftRoi = JawROIs["粗定位左"];
            Rect RightROi = JawROIs["粗定位右"];

            Mat LeftMat = new(src, LeftRoi);
            Mat RightMat = new(src, RightROi);

            Methods.GetContours(LeftMat, LeftRoi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] LeftCon);
            Methods.GetContours(RightMat, RightROi.Location, 75, 150, out OpenCvSharp.Point[][] _, out OpenCvSharp.Point[] RightCon);

            //Cv2.ImShow($"leftMat", LeftMat);
            //Cv2.ImShow($"rightMat", RightMat);
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
        public bool CalContourValue(Mat src, Point leftPt, Point rightPt, out double LeftY, out double RightY, out double d_005max, double correction = 0, double upperLimit = 0.005)
        {
            // 計算 roi
            Rect left = new(leftPt.X - 25, leftPt.Y - 150, 30, 70);
            Rect right = new(rightPt.X - 5, rightPt.Y - 150, 30, 70);

            double sumLength = 0;

            LineSegmentPoint[] lineH;
            double min, max, center;

            // using Mat leftMat = new(src, left);
            // using Mat rightMat = new(src, right);

            Methods.GetRoiCanny(src, left, 50, 120, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 50, 120, out Mat rightCanny);

            //Dispatcher.Invoke(() =>
            //{
            //Cv2.ImShow("Left Canny", leftCanny);
            //Cv2.ImShow("Right Canny", rightCanny);
            //});

            #region 左邊
            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 2, 2, 5);
            // sumLength = lineH.Sum(line => line.Length());
            // LeftY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : min; // 先判斷有辨識到最小值

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH_L = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 >= center);
            // 計算 maxH 總長
            sumLength = maxH_L.Sum(line => line.Length());
            // 計算平均 Y 座標
            LeftY = maxH_L.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

#if false
            Debug.WriteLine($"L Center: {center} {min} {max}");
            Debug.WriteLine($"---------------L---------------");
            foreach (LineSegmentPoint l in lineH)
            {
                Debug.WriteLine($"Count: {lineH.Length} {l.P1} {l.P2} {l.Length()}");
            }
            Debug.WriteLine($"---------------L---------------");
            foreach (LineSegmentPoint l in maxH_L)
            {
                Debug.WriteLine($"Count: {maxH_L.Count()} {l.P1} {l.P2} {l.Length()}");
                Cv2.Line(src, l.P1, l.P2, Scalar.Black, 2);
            }
#endif
            #endregion

            #region 右邊
            Methods.GetHoughLinesHFromCanny(rightCanny, right.Location, out lineH, 2, 2, 5);
            // sumLength = lineH.Sum(line => line.Length());
            // RightY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : min; // 先判斷有辨識到最小值

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH_R = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 >= center);
            // 計算 maxH 總長
            sumLength = maxH_R.Sum(line => line.Length());
            // 計算平均 Y 座標
            RightY = maxH_R.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));


#if false
            Debug.WriteLine($"R Center: {center} {min} {max}");
            Debug.WriteLine($"---------------R---------------");
            foreach (LineSegmentPoint l in lineH)
            {
                Debug.WriteLine($"Count: {lineH.Length} {l.P1} {l.P2} {l.Length()}");
            }
            Debug.WriteLine($"---------------R---------------");
            foreach (LineSegmentPoint l in maxH_R)
            {
                Debug.WriteLine($"Count: {maxH_R.Count()} {l.P1} {l.P2} {l.Length()} {center}");
                Cv2.Line(src, l.P1, l.P2, Scalar.Black, 2);
            }
#endif
            #endregion

            //Debug.WriteLine($"L:{LeftY} R:{RightY}");

            // 計算 輪廓度
            d_005max = (Math.Abs(LeftY - RightY) * Cam1Unit) + correction;
            //Debug.WriteLine($"R: {RightY} L: {LeftY} {d_005max}");

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
        public void CalFrontDistanceValue(Mat src, Point leftPt, Point rightPt, out double leftX, out double rightX, out double distance, double correction = 0)
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
            Debug.WriteLine($"前開: {Math.Abs(leftX - rightX)} px");

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
        public bool Cal008DistanceValue(Mat src, Point basePoint, double compareX, out double toothX, out double distance, double correction = 0, double limitL = 0.006, double limitU = 0.010)
        {
            // 計算 roi
            Rect roi = new(basePoint.X - 10, basePoint.Y - 140, 20, 150);

            //double sumLength = 0;
            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesVFromCanny(canny, roi.Location, out LineSegmentPoint[] lineV, 5, 0);
            // 總長
            double sumLength = lineV.Sum(line => line.Length());
            // 計算平均 X 座標
            toothX = lineV.Aggregate(0.0, (sum, next) => sum + ((next.P1.X + next.P2.X) / 2 * next.Length() / sumLength));
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
        /// <param name="X">ROI X (從前開取得)</param>
        /// <param name="topY">上邊緣</param>
        /// <param name="botY">下邊緣</param>
        /// <param name="distance">(out) 0.013 距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        public bool Cal013DistanceValue(Mat src, Point basePoint, JawPos leftRight, double X, out double topY, out double botY, out double distance, double correction = 0, double limitL = 0.011, double limitU = 0.015)
        {
            // 計算 roi
            //Rect roi = new(basePoint.X - 20, basePoint.Y - 70, 40, 90);
            Rect roi = new();

            switch (leftRight)
            {
                case JawPos.Left:
                    roi = new((int)X + 1, basePoint.Y - 70, (int)(basePoint.X - X - 2), 90);
                    break;
                case JawPos.Right:
                    roi = new(basePoint.X + 1, basePoint.Y - 70, (int)(X - basePoint.X - 2), 90);
                    break;
                default:
                    break;
            }

            double sumLength = 0;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 5, 1);

            //Cv2.ImShow($"013canny{leftRight}", canny);

            //Debug.WriteLine("-------------------------013-------------------------");
            //foreach (LineSegmentPoint item in lineH)
            //{
            //    Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
            //}
            //Debug.WriteLine("-----------------------------------------------------");

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

            Debug.WriteLine($"TopY: {topY} BotY: {botY}");
            //Debug.WriteLine($"{leftRight} :013 Value {distance}");

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

            Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
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
            //    Cv2.Rectangle(src, roi, Scalar.Black, 1);
            //    Cv2.ImShow($"srcBack", new Mat(src, roi));
            //    Cv2.ImShow($"cannyBack", canny);
            //});

            // 計算 後開距離
            distance = (Math.Abs(rightX - leftX) * Cam2Unit) + correction;
            //Debug.WriteLine($"Right: {rightX} Left: {leftX} {rightX - leftX}");
            // 銷毀 canny
            canny.Dispose();

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
        public bool Cal088DistanceValue(Mat src, double baseJigY, double compareX, JawPos leftRight, out double distance, double correction = 0, double limitL = 0.0855, double limitU = 0.0905)
        {
            // roi
            Rect roi = leftRight == JawPos.Left ? new Rect(80, (int)(baseJigY - 150), 120, 140) : new Rect(880, (int)(baseJigY - 150), 120, 140);

            Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
            Methods.GetHoughLinesVFromCanny(canny, roi.Location, out LineSegmentPoint[] lineV, 20, 10, 3);

            double sumLength = lineV.Sum(line => line.Length());
            double X = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);

            //Dispatcher.Invoke(() =>
            //{
            //    Cv2.Rectangle(src, roi, Scalar.Black, 2);
            //    Cv2.ImShow($"mat{leftRight}", new Mat(src, roi));
            //    Cv2.MoveWindow($"mat{leftRight}", 100, (int)(100 + 100 * (int)leftRight));
            //    Cv2.ImShow($"canny{leftRight}", canny);
            //    Cv2.MoveWindow($"canny{leftRight}", 300, (int)(100 + 100 * (int)leftRight));
            //});
            //foreach (LineSegmentPoint item in lineV)
            //{
            //    Cv2.Line(src, item.P1, item.P2, Scalar.Gray, 2);

            //    Debug.WriteLine($"{item.P1} {item.P2} {item.Length()}");
            //}
            //Debug.WriteLine($"{X}");

            // 隱藏 correction
            double subCorrection = 0;
            switch (leftRight)
            {
                case JawPos.Left:
                    subCorrection = (src.Width / 2 - X - 400) * 0.00004 + 0.0014;
                    break;
                case JawPos.Right:
                    subCorrection = ((X - (src.Width / 2) - 400) * 0.00004) + 0.0014;
                    break;
                default:
                    break;
            }

            // 計算 0.088 距離
            distance = (Math.Abs(compareX - X) * Cam2Unit) + correction + subCorrection;
            //Debug.WriteLine($"088 {leftRight} : {Math.Abs(compareX - X)}, {compareX}, {X}");
            //Debug.WriteLine($"Distance: {distance}, {subCorrection}");
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

            //Cv2.Rectangle(src, roi, Scalar.Black, 1);

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
            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // ROIs
            Rect roi = new Rect(140, (int)(baseDatumY + 50), 780, 40);

            Rect[] rois = new Rect[] {
                new(140, (int)(baseDatumY + 50), 50, 40), // PIN 前
                new(260, (int)(baseDatumY + 50), 50, 40),
                new(330, (int)(baseDatumY + 50), 50, 40),
                new(400, (int)(baseDatumY + 50), 50, 40),
                new(470, (int)(baseDatumY + 50), 50, 40),
                new(540, (int)(baseDatumY + 50), 50, 40),
                new(610, (int)(baseDatumY + 50), 50, 40),
                new(680, (int)(baseDatumY + 50), 40, 40),
                new(800, (int)(baseDatumY + 50), 50, 40), // 最後區
                new(870, (int)(baseDatumY + 50), 50, 40), // 最後區
            };

            //LineSegmentPoint[] lineH1 = new LineSegmentPoint[0];
            //LineSegmentPoint[] lineH2 = new LineSegmentPoint[0];
            //LineSegmentPoint[] lineH3 = new LineSegmentPoint[0];
            // 

            //Methods.GetRoiCanny(src, roi1, 20, 50, out Mat canny1);
            //Methods.GetHoughLinesHFromCanny(canny1, roi1.Location, out lineH1, 5);

            //Methods.GetRoiCanny(src, roi2, 20, 50, out Mat canny2);
            //Methods.GetHoughLinesHFromCanny(canny2, roi2.Location, out lineH2, 5);

            //Methods.GetRoiCanny(src, roi3, 20, 50, out Mat canny3);
            //Methods.GetHoughLinesHFromCanny(canny3, roi3.Location, out lineH3, 5);

            Methods.GetRoiCanny(src, roi, 20, 50, out Mat canny);

            List<double> maxLineY = new();
            for (int i = 0; i < rois.Length; i++)
            {
                //Cv2.Rectangle(src, rois[i], Scalar.Black, 1);
                Mat c = new(canny, rois[i].Subtract(new Point(roi.X, roi.Y)));
                Methods.GetHoughLinesHFromCanny(c, rois[i].Location, out LineSegmentPoint[] lineH, 5);
                maxLineY.Add(lineH.Max(l => (l.P1.Y + l.P2.Y) / 2));
            }
            // Mat c1 = new Mat(canny, roi1.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c2 = new Mat(canny, roi2.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c3 = new Mat(canny, roi3.Subtract(new Point(roi.Left, roi.Top)));
            //Debug.WriteLine($"{string.Join(",", maxLineY)}");

#if false
            LineSegmentPoint[] line = lineH1.Concat(lineH2).Concat(lineH3).Where(line => line.Length() > 30).OrderBy(line => line.P1.Y + line.P2.Y).OrderBy(line => line.P1.X).ToArray();
            List<LineSegmentPoint> lineList = new();

            //Debug.WriteLine($"-----------------------------------------------------------------");
            // 過濾重複點
            for (int i = 0; i < line.Length; i++)
            {
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

            //LineSegmentPoint max = lineList.Max(line => (line.P1.Y + line.P2.Y));

            //flatValue = (lineList.Max(l => Math.Max(l.P1.Y, l.P2.Y)) - lineList.Min(l => Math.Min(l.P1.Y, l.P2.Y))) * Cam3Unit + correction;
            flatValue = (lineList.Max(l => (l.P1.Y + l.P2.Y) / 2) - lineList.Min(l => (l.P1.Y + l.P2.Y) / 2)) * Cam3Unit + correction;
#endif
            canny.Dispose();
            //canny1.Dispose();
            //canny2.Dispose();

            flatValue = ((maxLineY.Max() - maxLineY.Min()) * Cam3Unit) + correction;
            //Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            return flatValue <= limitU;
        }
    }
}
