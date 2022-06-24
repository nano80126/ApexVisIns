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

        private readonly double cam1Mag = 0.21772;
        private readonly double cam2Mag = 0.2532;
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
                    _ = SpinWait.SpinUntil(() => false, 120);

                    // count = 0;
                    // 拍照要 Dispacker
                    Dispatcher.Invoke(() =>
                    {
                        //while (count < (i == 0 ? 2 : 3))
                        //{
                        for (int j = 0; j < (i == 0 ? 2 : 3); j++)
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

                                if (partExist)
                                {
                                    task1.Add(Task.Run(() => JawInsSequenceCam1(mat, specList, cam1results)));
                                }
                                else { count += 999; }  // 跳出迴圈

                                ImageSource1 = mat.ToImageSource();
                                //count++;
                                //Debug.WriteLine($"111: {DateTime.Now:mm:ss.fff}");
                            }
                            else { j--; }
                        }
                        //}
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
                    _ = SpinWait.SpinUntil(() => false, 120);

                    count = 0;
                    // 拍照要 Dispacker
                    Dispatcher.Invoke(() =>
                    {
                        // while (count < 2)
                        // {
                        for (int j = 0; j < 2; j++)
                        {
                            cam2.Camera.ExecuteSoftwareTrigger();
                            using IGrabResult grabResult = cam2.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                            if (grabResult != null && grabResult.GrabSucceeded)
                            {
                                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                                if (partExist) { task2.Add(Task.Run(() => JawInsSequenceCam2(mat, specList, cam2results))); }
                                else { count += 999; }  // 跳出迴圈
                                                        // JawInsSequenceCam2(mat, specList, cam2results);

                                ImageSource2 = mat.ToImageSource();
                                count++;
                            }
                            else { j--; }
                        }
                        //}
                    });

                    #endregion
                    // 跳出迴圈
                    if (!partExist) { break; }
                }

                #region CAMERA 3 平面度
                // COM2 光源控制器 (24V, 2CH)
                LightCtrls[1].SetAllChannelValue(128, 256);
                // 等待光源
                _ = SpinWait.SpinUntil(() => false, 120);

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
                            // JawInsSequenceCam3(mat, specList, cam3results);

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
                //try
                //{
                Task.WhenAll(task1.Concat(task2).Concat(task3)).Wait();
                //}
                //catch (Exception ex)
                //{
                //    Dispatcher.Invoke(() => MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, ex.Message));
                //    return;
                //}
                //Task.WhenAll(task1.Concat(task2).Concat(task3)).Wait();

                Debug.WriteLine($"end: {DateTime.Now:mm:ss.fff}");

                // Camera 1 結果
                // DateTime stTime = DateTime.Now;
                foreach (string key in cam1results.Keys)
                {
                    Debug.WriteLine($"{key} {cam1results[key].Count}");
                    //
                    double avg = 0;
                    if (key is "輪廓度R" or "輪廓度L")
                    {
                        avg = cam1results[key].Max();
                    }
                    else
                    {
                        avg = cam1results[key].Average();
                    }
                    spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                    MCAJaw.JawSpecGroup.Collection1.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
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

                    if (key == "前開") { d_front = avg; }
                }

                // Camera 2 結果
                foreach (string key in cam2results.Keys)
                {
                    //Debug.WriteLine($"{key} {cam2results[key].Count}");
                    //
                    double avg = cam2results[key].Min();
                    spec = MCAJaw.JawSpecGroup.SpecList.First(s => s.Item == key);
                    MCAJaw.JawSpecGroup.Collection2.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
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

                    if (key == "後開") { d_back = avg; }
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
                    Debug.WriteLine($"平面度 {string.Join(",", cam3results[item])}");
                    double avg = cam3results[item].Average();
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

                    Debug.WriteLine($"1: {DateTime.Now:mm:ss.fff}");
                    // 資料庫物件新增  key, value
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }
                    Debug.WriteLine($"2: {DateTime.Now:mm:ss.fff}");
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
            // SIP
            // 1. 取得基準點 2. 1 x 前開 3. 2 x 輪廓度
            // 全尺寸
            // 4. 2 x 0.008 5. 2 x 0.013 6. 2 x 0.024 

            JawSpecSetting spec;
            double CenterX;
            Point cPt1L = new();
            Point cPt2L = new();
            Point cPt1R = new();
            Point cPt2R = new();

            try
            {
                GetCoarsePos(src, out Point baseL, out Point baseR);
                CenterX = (baseL.X + baseR.X) / 2;

                #region 計算輪廓度 // LCY、RCY 輪廓度基準，後面會用到
                //spec = specList?[12];
                //CalContourValue(src, baseL, baseR, out double LCY, out double RCY, out double d_005Max, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                //if (spec != null && spec.Enable && results != null)
                //{
                //    lock (results)
                //    {
                //        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                //        if (d_005Max != -1) { results[spec.Item].Add(d_005Max); }   // -1 表示抓取輪廓失敗
                //    }
                //}
                #endregion

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

                #region 計算輪廓度 2 左 (實際上是右) 
                spec = specList?[12];
                if (spec != null && spec.Enable && results != null)
                {
                    CalContourValue2(src, baseL, LX, JawPos.Left, out cPt1L, out cPt2L, out double c_005R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(c_005R);
                    }
                }
                else if (results == null)
                {
                    CalContourValue2(src, baseL, LX, JawPos.Left, out cPt1L, out cPt2L, out double c_005R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    Debug.WriteLine($"c_005R: {c_005R}");
                }
                #endregion

                #region 計算輪廓度 2 右 (實際上是左)
                spec = specList?[13];
                if (spec != null && spec.Enable && results != null)
                {
                    CalContourValue2(src, baseR, RX, JawPos.Right, out cPt1R, out cPt2R, out double c_005L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(c_005L);
                    }
                }
                else if (results == null)
                {
                    CalContourValue2(src, baseR, RX, JawPos.Right, out cPt1R, out cPt2R, out double c_005L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    Debug.WriteLine($"c_005L: {c_005L}");
                }
                #endregion

                #region 左右輪廓度高低差
                Debug.WriteLine($"輪廓度高低差: {Math.Abs(cPt1L.Y - cPt1R.Y) * Cam1Unit}");
                Debug.WriteLine($"輪廓度高低差: {Math.Abs(cPt2L.Y - cPt2R.Y) * Cam1Unit}");
                spec = specList?[14];   // 平面度
                // Cal007FlatnessValue(src, datumY, out double f_007, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    Debug.WriteLine($"Cos(30) {Math.Cos(Math.PI * 30 / 180)}");

                    double f_007 = Math.Abs(cPt1L.Y - cPt1R.Y) * Cam1Unit / Math.Cos(Math.PI * 30 / 180);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(f_007);
                    }
                }
                #endregion

                #region 計算 0.008 左 (實際上是右)
                spec = specList?[3];
                if (spec != null && spec.Enable && results != null)
                {
                    Cal008DistanceValue(src, baseL, LX, out double LX008, out double d_008R, spec.Correction + spec.CorrectionSecret);
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
                    Cal008DistanceValue(src, baseR, RX, out double RX008, out double d_008L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_008L);
                    }
                }
                #endregion

                #region 計算 0.013 左 (實際上是右)
                spec = specList?[5];
                Cal013DistanceValue(src, baseL, JawPos.Left, LX, out double LtopY013, out double LbotY013, out double d_013R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        if (d_013R != -1) { results[spec.Item].Add(d_013R); }
                    }
                }
                #endregion

                #region 計算 0.013 右 (實際上是左)
                spec = specList?[6];
                Cal013DistanceValue(src, baseR, JawPos.Right, RX, out double RtopY013, out double RbotY013, out double d_013L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        if (d_013L != -1) { results[spec.Item].Add(d_013L); }
                    }
                }
                #endregion

                #region 計算 0.024 左 (實際上是右) (重寫)
                spec = specList?[7];
                // double d_024R = (Math.Abs(LCY - LtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                // Debug.WriteLine($"RbotY024 {LbotY024}, RtopY013 {LtopY013}");
                if (spec != null && spec.Enable && results != null)
                {
                    Cal024DistanceValue(src, baseL, JawPos.Left, LX, LtopY013, out double LbotY024, out double d_024R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024R);
                    }
                }
                #endregion

                #region 計算 0.024 右 (實際上是左) (重寫)
                spec = specList?[8];
                // double d_024L = (Math.Abs(RCY - RtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                // Debug.WriteLine($"RbotY024 {RbotY024}, RtopY013 {RtopY013}");
                if (spec != null && spec.Enable && results != null)
                {
                    Cal024DistanceValue(src, baseR, JawPos.Right, RX, RtopY013, out double RbotY024, out double d_024L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024L);
                    }
                }
                #endregion
                spec = null;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddWarning(MsgInformer.Message.MsgCode.JAW, $"Jaw 檢驗過程發生錯誤, {ex.Message}");
                });
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
            // SIP
            // 1. 後開 (2. 計算開度差)
            // 全尺寸
            // 3. 2 x 0.088 4. 0.088 和 

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

                #region 計算 0.088 和
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
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddWarning(MsgInformer.Message.MsgCode.JAW, $"Jaw 檢驗過程發生錯誤, {ex.Message}");
                });
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
                // Debug.WriteLine($"datumY: {datumY}");

                #region 計算 平面度
                spec = specList?[14];
                // Cal007FlatnessValue(src, datumY, out double f_007, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    Cal007FlatnessValue(src, datumY, out double[] arrayY, out double f_007, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(f_007);
                    }
                }
                else if (results == null)
                {
                    Cal007FlatnessValue(src, datumY, out double[] arrayY, out double f_007);
                    Debug.WriteLine($"f007: {f_007} {string.Join(",", arrayY)} {arrayY.Length}");
                }
                #endregion
                spec = null;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddWarning(MsgInformer.Message.MsgCode.JAW, $"Jaw 檢驗過程發生錯誤, {ex.Message}");
                });
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

            Methods.GetContours(LeftMat, LeftRoi.Location, 75, 150, out Point[][] _, out Point[] LeftCon);
            Methods.GetContours(RightMat, RightROi.Location, 75, 150, out Point[][] _, out Point[] RightCon);

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
            Rect left = new(leftPt.X - 22, leftPt.Y - 50, 20, 70);
            Rect right = new(rightPt.X + 2, rightPt.Y - 50, 20, 70);

            double sumLength = 0;

            LineSegmentPoint[] lineH;
            double min, max, center;

            // using Mat leftMat = new(src, left);
            // using Mat rightMat = new(src, right);

            Methods.GetRoiCanny(src, left, 50, 120, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 50, 120, out Mat rightCanny);

            // Cv2.Rectangle(src, left, Scalar.Black, 2);
            // Cv2.Rectangle(src, right, Scalar.Black, 2);

            //Dispatcher.Invoke(() =>
            //{
            //  Cv2.ImShow("Left Canny", leftCanny);
            //  Cv2.ImShow("Right Canny", rightCanny);
            //});

            #region 左邊
            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 2, 2, 5);
            // sumLength = lineH.Sum(line => line.Length());
            // LeftY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : 0; // 先判斷有辨識到最小值
            // Debug.WriteLine($"center: {center} {min} {max}");

            // 小於中心值
            IEnumerable<LineSegmentPoint> maxH_L = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 <= center);
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
            //foreach (LineSegmentPoint l in lineH)
            //{
            //    Debug.WriteLine($"{l.P1} {l.P2} {l.Length()}");
            //}

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : 0; // 先判斷有辨識到最小值
            //Debug.WriteLine($"center: {center} {min} {max}");

            // 小於中心值
            IEnumerable<LineSegmentPoint> maxH_R = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 <= center);
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

            // Debug.WriteLine($"L:{LeftY} R:{RightY}");

            // 計算 輪廓度
            d_005max = LeftY != 0 && RightY != 0 ? (Math.Abs(LeftY - RightY) * Cam1Unit) + correction : -1;
            // Debug.WriteLine($"R: {RightY} L: {LeftY} {d_005max}");

            #region Dispose
            leftCanny.Dispose();
            rightCanny.Dispose();
            #endregion

            // 確認 OK / NG
            return d_005max <= upperLimit;
        }

        /// <summary>
        /// 計算輪廓度
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePt">基準點</param>
        /// <param name="baseX">roi 基準 X</param>
        /// <param name="leftRight">Jaw 左、右</param>
        /// <param name="p1">(out) 輪廓度基準點</param>
        /// <param name="c_005max">(out) 輪廓度</param>
        /// <param name="correction">校正值</param>
        /// <param name="upperLimit">管制上限 (default: 0.005)</param>
        /// <returns></returns>
        public bool CalContourValue2(Mat src, Point basePt, double baseX, JawPos leftRight, out Point p1, out Point p2,  out double c_005max, double correction = 0, double upperLimit = 0.005)
        {
            Rect roi;

            switch (leftRight)
            {
                case JawPos.Left:
                    roi = new((int)baseX + 2, basePt.Y - 50, 20, 70);
                    break;
                case JawPos.Right:
                    roi = new((int)baseX - 22, basePt.Y - 50, 20, 70);
                    break;
                default:
                    roi = new();
                    break;
            }

            LineSegmentPoint[] lineH;
            double min, max, center;

            // 取得 Canny
            Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
            // 取得輪廓點
            Cv2.FindContours(canny, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, roi.Location);

            //Cv2.Rectangle(src, roi, Scalar.Gray, 1);
            //Cv2.ImShow($"canny{leftRight}", canny);

            #region 計算中心值
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out lineH, 2, 2, 5);

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : max;    // 先判斷有辨識到最小值
            #endregion

            #region 測試區
            // 連接輪廓點
            Point[] pts = contours.SelectMany(pts => pts).ToArray();
            // > baseX && < center

            Point[] filter = Array.Empty<Point>();
            // 點 1, 2
            //Point p1;
            p2 = new();
            switch (leftRight)
            {
                case JawPos.Left:
                    filter = pts.Where(pt => pt.X > baseX && pt.Y < center).Distinct().OrderBy(pt => pt.X).ToArray();
                    // 尋找點 2
                    for (int i = 1; i <= filter.Length; i++)
                    {
                        if (i == 1) { continue; }

                        if (filter[^i].X < filter[^(i - 1)].X && filter[^i].Y == filter[^(i - 1)].Y)
                        {
                            p2 = filter[^(i - 1)];
                            break;
                        }
                    }
                    break;
                case JawPos.Right:
                    filter = pts.Where(pt => pt.X < baseX && pt.Y < center).Distinct().OrderByDescending(pt => pt.X).ToArray();
                    // 尋找點 2
                    for (int i = 1; i <= filter.Length; i++)
                    {
                        if (i == 1) { continue; }

                        if (filter[^i].X > filter[^(i - 1)].X && filter[^i].Y == filter[^(i - 1)].Y)
                        {
                            p2 = filter[^(i - 1)];
                            break;
                        }
                    }
                    break;
                default:
                    break;
            }
            // 點 1
            p1 = filter[0];

#if DEBUG
            Cv2.Circle(src, p1, 5, Scalar.Gray, 2);
            Cv2.Circle(src, p2, 5, Scalar.Gray, 2);
#endif

            // foreach (Point pt in filter)
            // {
            //     Debug.WriteLine($"{pt}");
            // }
            Debug.WriteLine($"{leftRight} p1 : {p1}");
            #endregion

            c_005max = (Math.Abs(p1.Y - p2.Y) * Cam1Unit) + correction;
            return c_005max < upperLimit;
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
            Rect leftRoi = new(leftPt.X - 35, leftPt.Y - 73, 26, 30);
            Rect rightRoi = new(rightPt.X + 9, rightPt.Y - 73, 26, 30);

            double sumLength = 0;
            LineSegmentPoint[] lineV;

            Methods.GetRoiCanny(src, leftRoi, 75, 150, out Mat leftCanny);
            Methods.GetRoiCanny(src, rightRoi, 75, 150, out Mat rightCanny);

            //Cv2.Rectangle(src, leftRoi, Scalar.Black, 2);
            //Cv2.Rectangle(src, rightRoi, Scalar.Black, 2);

            // 左
            Methods.GetHoughLinesVFromCanny(leftCanny, leftRoi.Location, out lineV, 5, 2, 3);
            sumLength = lineV.Sum(line => line.Length());
            leftX = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);
            leftX = lineV.Max(x => Math.Max(x.P1.X, x.P2.X));

            // 右
            Methods.GetHoughLinesVFromCanny(rightCanny, rightRoi.Location, out lineV, 5, 2, 3);
            sumLength = lineV.Sum(line => line.Length());
            rightX = lineV.Aggregate(0.0, (sum, next) => sum + (next.P1.X + next.P2.X) / 2 * next.Length() / sumLength);
            rightX = lineV.Min(x => Math.Min(x.P1.X, x.P2.X));

            // 計算前開距離
            distance = (Math.Abs(leftX - rightX) * Cam1Unit) + correction;
            //Debug.WriteLine($"前開: {Math.Abs(leftX - rightX)} px, Distance: {distance}");

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
        /// <param name="leftRight">Jaw 左、右</param>
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
                    roi = new((int)X + 1, basePoint.Y - 50, (int)(basePoint.X - X - 2), 70);
                    break;
                case JawPos.Right:
                    roi = new(basePoint.X + 1, basePoint.Y - 50, (int)(X - basePoint.X - 2), 70);
                    break;
                default:
                    break;
            }

            double sumLength = 0;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 1, 5);

            //Cv2.Rectangle(src, roi, Scalar.Black, 2);

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
            distance = botY - topY > 10 ? (Math.Abs(topY - botY) * Cam1Unit) + correction : -1;

            //Debug.WriteLine($"{leftRight} TopY: {topY} BotY: {botY}");

            if (distance != -1)
            {
                // 計算 offset
                double offset = correction / Cam1Unit;
                double topY_Offset = offset * (topY - (src.Height / 2)) / (topY + botY - src.Height);
                double botY_Offset = offset * (botY - (src.Height / 2)) / (topY + botY - src.Height);
                topY += topY_Offset;
                botY += botY_Offset;
            }
            // 銷毀 canny
            canny.Dispose();
            //Debug.WriteLine(distance);

            //Debug.WriteLine($"{leftRight} TopY: {topY} BotY: {botY}, distance: {distance}");
            //Debug.WriteLine($"------------------------------------------------");
            //Debug.WriteLine($"{leftRight} :013 Value {distance}");

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// 計算 0.024距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="leftRight"></param>
        /// <param name="X">ROI X (從前開取得)</param>
        /// <param name="refY">參考 Y (從 013 或輪廓取得)</param>
        /// <param name="botY">上邊緣</param>
        /// <param name="distance">(out) 0.024距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU"></param>
        /// <returns></returns>
        public bool Cal024DistanceValue(Mat src, Point basePoint, JawPos leftRight, double X, double refY, out double botY, out double distance, double correction, double limitL = 0.0225, double limitU = 0.0255)
        {
            Rect roi;

            switch (leftRight)
            {
                case JawPos.Left:
                    roi = new((int)X + 1, basePoint.Y - 150, (int)(basePoint.X - X - 2), 70);
                    break;
                case JawPos.Right:
                    roi = new(basePoint.X + 1, basePoint.Y - 150, (int)(X - basePoint.X - 2), 70);
                    break;
                default:
                    roi = new();
                    break;
            }

            double sumLength = 0;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 1, 5);

            #region 約略計算中心值
            double min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            double max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            double center = (min + max) / 2;
            #endregion

            // 大於中心值
            IEnumerable<LineSegmentPoint> maxH = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 > center);
            // 計算 maxH 總長 
            sumLength = maxH.Sum(line => line.Length());
            // 計算平均 Y 座標
            botY = maxH.Aggregate(0.0, (sum, next) => sum + (next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength);
            // distance
            distance = (Math.Abs(refY - botY) * Cam1Unit) + correction;

            // 銷毀 canny
            canny.Dispose();

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

            // Dispatcher.Invoke(() =>
            // {
            //     Cv2.Rectangle(src, roi, Scalar.Gray, 2);
            //     Cv2.ImShow($"srcBack", new Mat(src, roi));
            //     Cv2.ImShow($"cannyBack", canny);
            // });

            // 計算 後開距離
            distance = (Math.Abs(rightX - leftX) * Cam2Unit) + correction;
            //Debug.WriteLine($"Right: {rightX} Left: {leftX}, {rightX - leftX}, {distance} {distance:0.00000}
            // 銷毀 canny");
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
        public bool Cal007FlatnessValue(Mat src, double baseDatumY, out double[] arrayY,out double flatValue, double correction = 0, double limitU = 0.007)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // ROIs
            Rect roi = new(140, (int)(baseDatumY + 50), 860, 40);

            Rect[] rois = new Rect[] {
                // PIN 前
                new(140, (int)(baseDatumY + 50), 20, 40),
                new(170, (int)(baseDatumY + 50), 20, 40),
                // 中間區
                new(260, (int)(baseDatumY + 50), 20, 40),
                new(290, (int)(baseDatumY + 50), 20, 40),
                new(320, (int)(baseDatumY + 50), 20, 40),
                new(350, (int)(baseDatumY + 50), 20, 40),
                new(380, (int)(baseDatumY + 50), 20, 40),
                new(410, (int)(baseDatumY + 50), 20, 40),
                new(440, (int)(baseDatumY + 50), 20, 40),
                new(470, (int)(baseDatumY + 50), 20, 40),
                new(500, (int)(baseDatumY + 50), 20, 40),
                new(530, (int)(baseDatumY + 50), 20, 40),
                new(560, (int)(baseDatumY + 50), 20, 40),
                new(590, (int)(baseDatumY + 50), 20, 40),
                new(620, (int)(baseDatumY + 50), 20, 40),
                new(650, (int)(baseDatumY + 50), 20, 40),
                new(680, (int)(baseDatumY + 50), 20, 40),
                // 最後區
                new(800, (int)(baseDatumY + 50), 20, 40),
                new(830, (int)(baseDatumY + 50), 20, 40),
                new(860, (int)(baseDatumY + 50), 20, 40),
                new(890, (int)(baseDatumY + 50), 20, 40),
                new(920, (int)(baseDatumY + 50), 20, 40),
                new(950, (int)(baseDatumY + 50), 20, 40),
                new(980, (int)(baseDatumY + 50), 20, 40),
            };

            //
            // LineSegmentPoint[] lineH1 = new LineSegmentPoint[0];
            // LineSegmentPoint[] lineH2 = new LineSegmentPoint[0];
            // LineSegmentPoint[] lineH3 = new LineSegmentPoint[0];
            // 

            Methods.GetRoiCanny(src, roi, 25, 60, out Mat canny);
            Cv2.Rectangle(src, roi, Scalar.Gray, 2);


            List<double> minLineY = new();
            for (int i = 0; i < rois.Length; i++)
            {
                Cv2.Rectangle(src, rois[i], Scalar.Black, 1);
                Mat c = new(canny, rois[i].Subtract(new Point(roi.X, roi.Y)));
                // Mat c2 = new(canny, rois[i]);
                Methods.GetHoughLinesHFromCanny(c, rois[i].Location, out LineSegmentPoint[] lineH, 5, 5, 3);

                // Methods.GetRoiOtsu(src, rois[i].Subtract(new Point(roi.X, roi.Y)), 0, 255, out Mat Otsu, out byte threshhold);
                // Cv2.ImShow($"otsu{i}", Otsu);
                // Cv2.Rectangle(canny, rois[i].Subtract(new Point(roi.X, roi.Y)), Scalar.Gray, 2);

                if (lineH.Length > 0)
                {
                    if (i == 0)
                    {
                        minLineY.Add(lineH.Min(l => (l.P1.Y + l.P2.Y) / 2));
                    }
                    else
                    {
                        double y = lineH.Min(l => (l.P1.Y + l.P2.Y) / 2);
                        if (y - minLineY[i - 1] > 5) { continue; }
                        minLineY.Add(y);
                    }
                    foreach (LineSegmentPoint line in lineH)
                    {
                        Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1);
                    }
                }
            }
            // 
            // Mat c1 = new Mat(canny, roi1.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c2 = new Mat(canny, roi2.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c3 = new Mat(canny, roi3.Subtract(new Point(roi.Left, roi.Top)));
            //Debug.WriteLine($"{string.Join(",", minLineY)}, count: {minLineY.Count}, {rois.Length}");
            // 

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
            // canny1.Dispose();
            // canny2.Dispose();

            arrayY = minLineY.ToArray();
            flatValue = ((minLineY.Max() - minLineY.Min()) * Cam3Unit) + correction;
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            return flatValue <= limitU;
        }
    }
}
