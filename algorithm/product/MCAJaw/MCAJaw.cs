using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using Basler.Pylon;
using MCAJawIns.Product;
using OpenCvSharp;

namespace MCAJawIns.Algorithm
{
    /// <summary>
    /// Jaw 左、右邊，演算法參數需要
    /// </summary>
    public enum JawPos
    {
        Left = 1,
        Right = 2,
    }

    public interface IMCAJawAlgorithm
    {
        #region Properties
        public double Cam1PixelSize { get; set; }
        public double Cam2PixelSize { get; set; }
        public double Cam3PixelSize { get; set; }

        public double Cam1Mag { get; set; }
        public double Cam2Mag { get; set; }
        public double Cam3Mag { get; set; }

        public MainWindow MainWindow { get; set; }
        #endregion

        #region Methods
        public void ListVisionParam();

        public void CaptureImage(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3);

        public void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3, JawMeasurements jawFullSpecIns = null);
        #endregion
    }

    public abstract class MCAJawAlgorithm : IMCAJawAlgorithm
    {
        #region Properties
        public MainWindow MainWindow { get; set; } = (MainWindow)System.Windows.Application.Current.MainWindow;
        public double Cam1PixelSize { get; set; } = 2.2 * 1e-3;
        public double Cam2PixelSize { get; set; } = 2.2 * 1e-3;
        public double Cam3PixelSize { get; set; } = 3.45 * 1e-3;
        public virtual double Cam1Mag { get; set; } = 1;
        public virtual double Cam2Mag { get; set; } = 1;
        public virtual double Cam3Mag { get; set; } = 1;
        #endregion

        #region Methods
        public virtual void ListVisionParam()
        {
            Debug.WriteLine($"Camera 1 Unit: 1px = {Cam1PixelSize / 25.4 / Cam1Mag} inch");
            Debug.WriteLine($"Camera 2 Unit: 1px = {Cam2PixelSize / 25.4 / Cam2Mag} inch");
            Debug.WriteLine($"Camera 3 Unit: 1px = {Cam3PixelSize / 25.4 / Cam3Mag} inch");
        }

        public virtual void SetVisionParam(int cam, double pxSize, double mag)
        {
            switch (cam)
            {
                case 1:
                    Cam1PixelSize = pxSize * 1e-3;
                    Cam1Mag = mag;
                    break;
                case 2:
                    Cam2PixelSize = pxSize * 1e-3;
                    Cam2Mag = mag;
                    break;
                case 3:
                    Cam3PixelSize = pxSize * 1e-3;
                    Cam3Mag = mag;
                    break;
                default:
                    break;
            }
        }

        public abstract void CaptureImage(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3);

        public abstract void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3, JawMeasurements jawFullSpecIns = null);

        public abstract void JawInsSequenceCam1(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null);

        public abstract void JawInsSequenceCam2(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null);

        public abstract void JawInsSequenceCam3(Mat src, List<JawSpecSetting> specList = null, Dictionary<string, List<double>> results = null);

        public abstract bool CheckPartCam1(Mat src, out byte threshold);

        public abstract bool CheckPartCam2(Mat src, out byte threshold);

        public abstract bool CheckPartCam3(Mat src, out byte threshold);
        #endregion
    }

    [Obsolete("deprecated")]
    public class VisionUnitTransfer
    {
        #region Properties
        /// <summary>
        /// Pixel Size
        /// </summary>
        public double PixelSize = 2.2 * 1e-3;
        /// <summary>
        /// 放大倍率
        /// </summary>
        public double Magnification = 0.2;
        #endregion

        public VisionUnitTransfer(double pixelSize, double magnification)
        {
            PixelSize = pixelSize;
            Magnification = magnification;
        }
    }
}


namespace MCAJawIns
{
#if deprecated || true
    public partial class MainWindow : System.Windows.Window
    {
        #region 單位換算
        private readonly double Cam1PixelSize = 2.2 * 1e-3;
        private readonly double Cam2PixelSize = 2.2 * 1e-3;
        private readonly double Cam3PixelSize = 4.5 * 1e-3;

        // private readonly double cam1Mag = 0.21867;
        // private readonly double cam2Mag = 0.25461;
        private readonly double cam1Mag = 0.21839;
        private readonly double cam2Mag = 0.25431;
        private readonly double cam3Mag = 0.1063;

        private double Cam1Unit => Cam1PixelSize / 25.4 / cam1Mag;
        private double Cam2Unit => Cam2PixelSize / 25.4 / cam2Mag;
        private double Cam3Unit => Cam3PixelSize / 25.4 / cam3Mag;
        #endregion

        #region private
        /// <summary>
        /// 警告音效
        /// </summary>
        private readonly SoundPlayer SoundAlarm = new SoundPlayer(@".\sound\Alarm.wav");    // 4 極短音
        #endregion

        #region 演算法使用
        /// <summary>
        /// Jaw 左右 enum，013、024等演算法所需 param
        /// </summary>
        public enum JawPos
        {
            Left = 1,
            Right = 2,
        }

        /// <summary>
        /// ROIs
        /// </summary>
        private readonly Dictionary<string, Rect> JawROIs = new()
        {
            { "有料檢知", new Rect(185, 345, 710, 30) },
            { "有料檢知2", new Rect(185, 345, 710, 30) },
            { "有料檢知3", new Rect(280, 170, 420, 40) },
            { "粗定位左", new Rect(310, 260, 230, 300) },
            { "粗定位右", new Rect(540, 260, 230, 300) },
            { "治具定位", new Rect(460, 900, 160, 100) },
            { "後開位置", new Rect(320, 575, 440, 40) },
            { "側面定位", new Rect(460, 90, 240, 130) }
        };
        #endregion

        #region 測試用
        /// <summary>
        /// 顯示換算單位
        /// </summary>
        public void ListJawParam()
        {
            Debug.WriteLine($"Camera 1 Unit: 1px = {Cam1Unit} inch");
            Debug.WriteLine($"Camera 2 Unit: 1px = {Cam2Unit} inch");
            Debug.WriteLine($"Camera 3 Unit: 1px = {Cam3Unit} inch");
        }
        #endregion

        #region 主要進入點
        /// <summary>
        /// Jaw 檢驗流程
        /// </summary>
        /// <param name="cam1">相機 1</param>
        /// <param name="cam2">相機 2</param>
        /// <param name="cam3">相機 3</param>
        /// <param name="jawFullSpecIns">檢驗結果物件</param>
        [Obsolete]
        public void JawInsSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3, JawMeasurements jawFullSpecIns = null)
        {
            // 0. 各項物件、變數初始化
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
                //List<JawSpecSetting> specList = MCAJaw.JawResultGroup.SizeSpecList.ToList();
                List<JawSpecSetting> specList = MCAJaw.JawSizeSpecList.Source.ToList();
                JawSpecSetting spec;
                // 有無料
                bool partExist = false;
                // 是否NG (避免重複計算NG數量)
                bool isNG = false;
                // 前開
                double d_front = 0;
                // 後開
                double d_back = 0;

                #region 平行處理 Task 初始化
                List<Task> task1 = new();
                List<Task> task2 = new();
                List<Task> task3 = new();
                #endregion

                #region results 記錄用物件初始化
                Dictionary<string, List<double>> cam1results = new();
                Dictionary<string, List<double>> cam2results = new();
                Dictionary<string, List<double>> cam3results = new();
                #endregion

                // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 以下觸發拍照且計算各項量測值

                for (int i = 0; i < 2; i++)
                {
                    #region CAMERA 1
                    // COM2 光源控制器 (24V, 2CH)
                    LightCtrls[1].SetAllChannelValue(96, 0);
                    // 等待光源
                    _ = SpinWait.SpinUntil(() => false, 30);

                    // count = 0;
                    // 拍照要 Dispacker
                    Dispatcher.Invoke(() =>
                    {
                        for (int j = 0; j < (i == 0 ? 2 : 3); j++)
                        {
                            Debug.WriteLine($"camera1: count: {j}");
                            // 等待 Trigger Ready
                            bool ready = cam1.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);
                            if (!ready)
                            {
                                j--;
                                continue;
                            }

                            cam1.Camera.ExecuteSoftwareTrigger();

                            using IGrabResult grabResult = cam1.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                            //if (grabResult != null && grabResult.GrabSucceeded)
                            if (grabResult?.GrabSucceeded == true)
                            {
                                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                                // 第一張確認有工件
                                if (i + j == 0)
                                {
                                    //if (CheckPartCam1(mat)) { partExist = true; }
                                    partExist = CheckPartCam1(mat);
                                }

                                if (partExist) { task1.Add(Task.Run(() => JawInsSequenceCam1(mat, specList, cam1results))); }
                                else { j += 999; }  // 跳出迴圈

                                ImageSource1 = mat.ToImageSource();
                            }
                            else { j--; }
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
                    _ = SpinWait.SpinUntil(() => false, 30);

                    //count = 0;
                    // 拍照要 Dispacker
                    Dispatcher.Invoke(() =>
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            Debug.WriteLine($"camera2: count: {j}");
                            // 等待 Trigger Ready
                            bool ready = cam2.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);
                            if (!ready)
                            {
                                j--;
                                continue;
                            }

                            cam2.Camera.ExecuteSoftwareTrigger();
                            using IGrabResult grabResult = cam2.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                            //if (grabResult != null && grabResult.GrabSucceeded)
                            if (grabResult?.GrabSucceeded == true)
                            {
                                Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                                // 補上有料檢知 // 補上有料檢知 // 補上有料檢知 // 補上有料檢知 // 補上有料檢知 // 補上有料檢知
                                if (i + j == 0 && partExist)
                                {
                                    partExist = CheckPartCam2(mat);
                                }

                                if (partExist) { task2.Add(Task.Run(() => JawInsSequenceCam2(mat, specList, cam2results))); }
                                else { j += 999; }  // 跳出迴圈

                                ImageSource2 = mat.ToImageSource();
                            }
                            else { j--; }
                        }
                    });

                    #endregion
                    // 跳出迴圈
                    if (!partExist) { break; }
                }

                #region CAMERA 3 平直度
                // COM2 光源控制器 (24V, 2CH)
                LightCtrls[1].SetAllChannelValue(256, 96);
                // 等待光源
                _ = SpinWait.SpinUntil(() => false, 30);

                //count = 0;
                // 拍照要 Dispacker
                Dispatcher.Invoke(() =>
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Debug.WriteLine($"camera3: count: {j}");
                        bool ready = cam3.Camera.WaitForFrameTriggerReady(100, TimeoutHandling.Return);
                        //Debug.WriteLine($"{ready}");
                        if (!ready)
                        {
                            j--;
                            continue;
                        }

                        cam3.Camera.ExecuteSoftwareTrigger();
                        IGrabResult grabResult = cam3.Camera.StreamGrabber.RetrieveResult(125, TimeoutHandling.Return);

                        //if (grabResult != null && grabResult.GrabSucceeded)
                        if (grabResult?.GrabSucceeded == true)
                        {
                            Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

                            if (j == 0 && partExist)
                            {
                                partExist = CheckPartCam3(mat);
                            }

                            if (partExist) { task3.Add(Task.Run(() => JawInsSequenceCam3(mat, specList, cam3results))); }
                            else { j += 999; }

                            ImageSource3 = mat.ToImageSource();
                        }
                        else { j--; }
                    }
                });

                #endregion

                LightCtrls[1].SetAllChannelValue(0, 0);

                if (!partExist)
                {
                    SoundAlarm.Play();
                    throw new MCAJawException("未檢測到料件");
                }

                Task.WhenAll(task1.Concat(task2).Concat(task3)).Wait();

                #region 計算不良數量

                #region Camera 1 結果 (前開)
                foreach (string key in cam1results.Keys)
                {
                    //Debug.WriteLine($"{key} {cam1results[key].Count}");
                    double avg = 0;

                    // 過濾輪廓度極值
                    if (key is "輪廓度R" or "輪廓度L" or "輪廓度")
                    {
                        double max = cam1results[key].Max();
                        double min = cam1results[key].Min();
                        avg = cam1results[key].Average();

                        int count = cam1results[key].Count;

                        // 過濾極端值
                        if (max >= avg + Cam1Unit)
                        {
                            cam1results[key].RemoveAll(x => x >= max);
                        }
                        else if (avg >= min + Cam1Unit)
                        {
                            cam1results[key].RemoveAll(x => x <= min);
                        }
                        avg = cam1results[key].Average();

                        // Debug.WriteLine($"{string.Join(",", cam1results[key])}, max: {max} min: {min}");
                        // Debug.WriteLine($"Count: {count - cam1results[key].Count} ");
                    }
                    else
                    {
                        avg = cam1results[key].Average();
                    }
                    //spec = MCAJaw.JawResultGroup.SizeSpecList.First(s => s.Item == key);
                    spec = MCAJaw.JawSizeSpecList.Source.First(s => s.Item == key);
                    MCAJaw.JawResultGroup.Collection1.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));


                    // 先判斷是否已為 NG，若已計為NG則數量不再 +1
                    if (!isNG)
                    {
                        // 判斷是否 ok
                        bool ok = MCAJaw.JawResultGroup.Collection1[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標記這 piece 為 NG品，避免重複計算NG
                        isNG = !ok;
                    }

                    // 資料庫物件新增
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }

                    if (key == "前開") { d_front = avg; }
                }
                #endregion

                #region Camera 2 結果 (後開)
                foreach (string key in cam2results.Keys)
                {
                    //Debug.WriteLine($"{key} {cam2results[key].Count}");
                    //
                    double avg = cam2results[key].Min();
                    //spec = MCAJaw.JawResultGroup.SizeSpecList.First(s => s.Item == key);
                    spec = MCAJaw.JawSizeSpecList.Source.First(s => s.Item == key);
                    MCAJaw.JawResultGroup.Collection2.Add(new JawSpec(key, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));

                    // 先判斷是否已為 NG，若已計為NG則數量不再 +1
                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawResultGroup.Collection2[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, avg); }

                    if (key == "後開") { d_back = avg; }
                }
                #endregion

                #region 開度差 (先確認是否啟用)
                //spec = MCAJaw.JawResultGroup.SizeSpecList.First(s => s.Item == "開度差");
                spec = MCAJaw.JawSizeSpecList.Source.First(s => s.Item == "開度差");
                if (spec.Enable)
                {
                    double bfDiff = Math.Abs(d_front - d_back);
                    MCAJaw.JawResultGroup.Collection1.Add(new JawSpec(spec.Item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, bfDiff));
                    //MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection1[^1].OK ? 0 : 1;    // 保留

                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawResultGroup.Collection1[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增  key, value
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add(spec.Key, bfDiff); }
                }

                //MCAJaw.JawSpecGroup.Collection1.Add(MCAJaw.JawSpecGroup.Collection1[0]);
                //MCAJaw.JawSpecGroup.Collection1.RemoveAt(0);
                MCAJaw.JawResultGroup.Collection1.Move(0, MCAJaw.JawResultGroup.Collection1.LastIndex());

                #endregion

                #region Camera 3 結果
                // 若平直度未檢測到，播放警告
                if (cam3results.Keys.Count == 0) { SoundAlarm.Play(); }
                foreach (string item in cam3results.Keys)
                {
                    Debug.WriteLine($"平直度 {string.Join(",", cam3results[item])}");

#if false
                    Dictionary<double, int> dict = new Dictionary<double, int>();
                    foreach (double value in cam3results[item])
                    {
                        if (!dict.ContainsKey(value))
                        {
                            dict.Add(value, 1);
                        }
                        else
                        {
                            dict[value]++;
                        }
                    } 
#endif

                    // Debug.WriteLine($"{dict.Values.Max()}");
                    // Debug.WriteLine($"{string.Join(",", dict)}");

                    double avg = cam3results[item].Average();
                    //spec = MCAJaw.JawResultGroup.SizeSpecList.First(s => s.Item == item);
                    spec = MCAJaw.JawSizeSpecList.Source.First(s => s.Item == item);
                    MCAJaw.JawResultGroup.Collection3.Add(new JawSpec(item, spec.CenterSpec, spec.LowerCtrlLimit, spec.UpperCtrlLimit, avg));
                    // MCAJaw.JawInspection.LotResults[spec.Key].Count += MCAJaw.JawSpecGroup.Collection3[^1].OK ? 0 : 1;   // 保留

                    // 先判斷是否已為 NG，若已計為NG則數量不再+1
                    if (!isNG)
                    {
                        // 判斷是否 OK
                        bool ok = MCAJaw.JawResultGroup.Collection3[^1].OK;
                        // ok => Count 不加 0
                        MCAJaw.JawInspection.LotResults[spec.Key].Count += ok ? 0 : 1;
                        // 若不 ok => 標示這 pc 為 NG 品
                        isNG = !ok;
                    }

                    // 資料庫物件新增  key, value
                    if (jawFullSpecIns != null) { jawFullSpecIns.Results.Add($"{spec.Key}", avg); }
                }
                #endregion

                // 判斷是否為良品
                MCAJaw.JawInspection.LotResults["good"].Count += MCAJaw.JawResultGroup.Col1Result && MCAJaw.JawResultGroup.Col2Result && MCAJaw.JawResultGroup.Col3Result ? 1 : 0;
                #endregion
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
                MsgInformer.AddError(MsgInformer.Message.MsgCode.JAW, ex.Message);
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
            //Point cPt1L = new();
            //Point cPt2L = new();
            //Point cPt1R = new();
            //Point cPt2R = new();
            // 輪廓點
            Point[] contourPts = new Point[] { new Point(), new Point(), new Point(), new Point() };
            // 角點
            Point[] cornerPts = new Point[] { new Point(), new Point(), new Point(), new Point() };

            try
            {
                GetCoarsePos(src, out Point baseL, out Point baseR);
                CenterX = (baseL.X + baseR.X) / 2;

                #region 計算輪廓度 // LCY、RCY 輪廓度基準，後面會用到 (舊方法重複性低下)
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
                if (spec?.Enable == true && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_front);
                    }
                }
                #endregion

                #region 取得輪廓度點 2 
                // 取得輪廓度點 2 左 (實際上是右) 
                //GetContourCornerPoint(src, baseL, LX, JawPos.Left, out cPt1L, out cPt2L);
                GetContourCornerPoint(src, baseL, LX, JawPos.Left, out contourPts[0], out contourPts[1]);
                // 取得輪廓度點 2 右 (實際上是左)
                //GetContourCornerPoint(src, baseR, RX, JawPos.Right, out cPt1R, out cPt2R);
                GetContourCornerPoint(src, baseR, RX, JawPos.Right, out contourPts[2], out contourPts[3]);
                // 取得亞像素點
                Point2f[] subContourPts = Cv2.CornerSubPix(src, new Point2f[] { contourPts[0], contourPts[1], contourPts[2], contourPts[3] }, new Size(11, 11), new Size(-1, -1), TermCriteria.Both(40, 0.01));
                //Point2f[] subContourPts = Cv2.CornerSubPix(src, new Point2f[] { cPt1L, cPt2L, cPt1R, cPt2R }, new Size(11, 11), new Size(-1, -1), TermCriteria.Both(40, 0.01));
#if DEBUG || debug
                foreach (Point2f item in subContourPts)
                {
                    Cv2.Circle(src, (int)item.X, (int)item.Y, 5, Scalar.Gray, 2);
                }

                Debug.WriteLine($"{string.Join(", ", subContourPts)}");
#endif
                #endregion

                #region 計算輪廓度 (3 項)

                #region 輪廓度 (高低差)
                spec = specList?[12];    // contour 高低差
                if (spec?.Enable == true && results != null)
                {
                    // double c_005 = Math.Abs(subPtsArr[0].Y - subPtsArr[2].Y) * Cam1Unit + spec.Correction + spec.CorrectionSecret;
                    double c_005 = (Math.Abs((subContourPts[0].Y + subContourPts[1].Y) / 2 - (subContourPts[2].Y + subContourPts[3].Y) / 2) * Cam1Unit) + spec.Correction + spec.CorrectionSecret;
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(c_005);
                    }
                }
                else if (results == null)
                {
                    Debug.WriteLine($"c_005 : {Math.Abs(subContourPts[0].Y - subContourPts[2].Y) * Cam1Unit:F5}");
                }
                #endregion

                #region 輪廓度 (右)
                spec = specList?[13];   // contour R
                if (spec?.Enable == true && results != null)
                {
                    double c_005R = (Math.Abs(subContourPts[0].Y - subContourPts[1].Y) * Cam1Unit) + spec.Correction + spec.CorrectionSecret;
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(c_005R);
                    }
                }
                else if (results == null)
                {
                    Debug.WriteLine($"c_005R: {Math.Abs(subContourPts[0].Y - subContourPts[1].Y) * Cam1Unit:F5}");
                }
                #endregion

                #region 輪廓度 (左)
                spec = specList?[14];   // contour L
                if (spec?.Enable == true && results != null)
                {
                    double c_005L = (Math.Abs(subContourPts[2].Y - subContourPts[3].Y) * Cam1Unit) + spec.Correction + spec.CorrectionSecret;
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(c_005L);
                    }
                }
                else if (results == null)
                {
                    Debug.WriteLine($"c_005L: {Math.Abs(subContourPts[2].Y - subContourPts[3].Y) * Cam1Unit:F5}");
                }
                #endregion

                #endregion

                #region 計算 013
                spec = specList?[5];    // 013R 
                if (spec?.Enable == true && results != null)
                {
                    Cal013DistanceValue2(src, baseL, JawPos.Left, LX, (subContourPts[0].Y + subContourPts[1].Y) / 2, out double d_013R, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_013R);
                    }
                }
                else if (results == null)
                {
                    Cal013DistanceValue2(src, baseL, JawPos.Left, LX, (subContourPts[0].Y + subContourPts[1].Y) / 2, out double d_013R);
                    Debug.WriteLine($"013R: {d_013R:F5}");
                }

                spec = specList?[6];    // 013L 
                if (spec?.Enable == true && results != null)
                {
                    Cal013DistanceValue2(src, baseR, JawPos.Right, RX, (subContourPts[2].Y + subContourPts[3].Y) / 2, out double d_013L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_013L);
                    }
                }
                else if (results == null)
                {
                    Cal013DistanceValue2(src, baseR, JawPos.Right, RX, (subContourPts[2].Y + subContourPts[3].Y) / 2, out double d_013L);
                    Debug.WriteLine($"013L: {d_013L:F5}");
                }
                #endregion

                #region 取得影像上方角點 (計算 024 用)
                /// 
                /// 先過驗證
                /// 在中、大 JAW 重寫啟用、關閉邏輯
                ///

                // 取得角點 左 (實際為右)
                GetCornerPoint(src, baseL, LX, JawPos.Left, out cornerPts[0], out cornerPts[1]);
                // 取得角點 右 (實際為左)
                GetCornerPoint(src, baseR, RX, JawPos.Right, out cornerPts[2], out cornerPts[3]);
                // 取得亞像素點
                Point2f[] subCornerPts = Cv2.CornerSubPix(src, new Point2f[] { cornerPts[0], cornerPts[1], cornerPts[2], cornerPts[3] }, new Size(11, 11), new Size(-1, -1), TermCriteria.Both(40, 0.01));
#if DEBUG || debug
                foreach (Point2f item in subCornerPts)
                {
                    Cv2.Circle(src, (int)item.X, (int)item.Y, 5, Scalar.Gray, 2);
                }
                Debug.WriteLine($"{string.Join(", ", subCornerPts)}");
#endif
                #endregion

                #region 計算 024
                spec = specList?[7];    // 024R // 輪廓角點 - 角點 (影像上方)
                if (spec?.Enable == true && results != null)
                {
                    double d_024Rt = (subCornerPts[2].Y + subCornerPts[3].Y) / 2;
                    double d_024Rb = (subContourPts[2].Y + subContourPts[3].Y) / 2;
                    double d_024R = (Math.Abs(d_024Rb - d_024Rt) * Cam1Unit) + spec.Correction + spec.CorrectionSecret;

                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024R);
                    }
                }
                else if (results == null)
                {
                    double d_024Rt = (subCornerPts[2].Y + subCornerPts[3].Y) / 2;
                    double d_024Rb = (subContourPts[2].Y + subContourPts[3].Y) / 2;
                    double d_024R = Math.Abs(d_024Rb - d_024Rt) * Cam1Unit;

                    Debug.WriteLine($"024R: {d_024R:F5}");
                }

                spec = specList?[8];    // 024L // 輪廓角點 - 角點
                if (spec?.Enable == true && results != null)
                {
                    double d_024Lt = (subCornerPts[0].Y + subCornerPts[1].Y) / 2;
                    double d_024Lb = (subContourPts[0].Y + subContourPts[1].Y) / 2;
                    double d_024L = (Math.Abs(d_024Lb - d_024Lt) * Cam1Unit) + spec.Correction + spec.CorrectionSecret;

                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024L);
                    }
                }
                else if (results == null)
                {
                    double d_024Lt = (subCornerPts[0].Y + subCornerPts[1].Y) / 2;
                    double d_024Lb = (subContourPts[0].Y + subContourPts[1].Y) / 2;
                    double d_024L = Math.Abs(d_024Lb - d_024Lt) * Cam1Unit;

                    Debug.WriteLine($"024L: {d_024L:F5}");
                }
                #endregion  

                #region 計算 0.008 左 (實際上是右)
                spec = specList?[3];
                if (spec?.Enable == true && results != null)
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
                if (spec?.Enable == true && results != null)
                {
                    Cal008DistanceValue(src, baseR, RX, out double RX008, out double d_008L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_008L);
                    }
                }
                #endregion



#if false
                #region 計算 0.013 左 (實際上是右) (待廢)
                spec = specList?[5];
                Cal013DistanceValue(src, baseL, JawPos.Left, LX, out double LtopY013, out double LbotY013, out double d_013R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec?.Enable == true && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        if (d_013R != -1) { results[spec.Item].Add(d_013R); }
                    }
                }
                #endregion

                #region 計算 0.013 右 (實際上是左) (待廢)
                spec = specList?[6];
                Cal013DistanceValue(src, baseR, JawPos.Right, RX, out double RtopY013, out double RbotY013, out double d_013L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec?.Enable == true && results != null)
                {
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        if (d_013L != -1) { results[spec.Item].Add(d_013L); }
                    }
                }
                #endregion

                #region 計算 0.024 左 (實際上是右) (待廢)
                spec = specList?[7];
                // double d_024R = (Math.Abs(LCY - LtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec?.Enable == true && results != null)
                {
                    Cal024DistanceValue(src, baseL, JawPos.Left, LX, LtopY013, out double LbotY024, out double d_024R, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024R);
                    }
                }
                #endregion

                #region 計算 0.024 右 (實際上是左) (待廢)
                spec = specList?[8];
                // double d_024L = (Math.Abs(RCY - RtopY) * Cam1Unit) + (spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec?.Enable == true && results != null)
                {
                    Cal024DistanceValue(src, baseR, JawPos.Right, RX, RtopY013, out double RbotY024, out double d_024L, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_024L);
                    }
                }
                #endregion  
#endif
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
            // 3. 2 x 0.088 4. 0.176

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
                // Cal088DistanceValue(src, JigPosY, RX, JawPos.Right, out d_088R);
                // 088 右 或 176 開啟
                if ((spec?.Enable == true || specList?[2].Enable == true) && results != null)
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
                // Cal088DistanceValue(src, JigPosY, LX, JawPos.Left, out d_088L);
                // 088 左 或 176 開啟
                if ((spec?.Enable == true || specList?[2].Enable == true) && results != null)
                {
                    Cal088DistanceValue(src, JigPosY, LX, JawPos.Left, out d_088L, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(d_088L);
                    }
                }
                #endregion

                #region 計算 0.176
                spec = specList?[2];
                if (spec?.Enable == true && results != null)
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

                #region 計算 平直度
                spec = specList?[15];
                // Cal007FlatnessValue(src, datumY, out double f_007, spec != null ? spec.Correction + spec.CorrectionSecret : 0);
                if (spec != null && spec.Enable && results != null)
                {
                    //Cal007FlatnessValue2(src, datumY, out double[] arrayY, out double f_007, spec.Correction + spec.CorrectionSecret);
                    Cal007FlatnessValue4(src, datumY, out double f_007, spec.Correction + spec.CorrectionSecret);
                    lock (results)
                    {
                        if (!results.ContainsKey(spec.Item)) { results[spec.Item] = new List<double>(); }
                        results[spec.Item].Add(f_007);
                    }
                }
                else if (results == null)
                {
                    Cal007FlatnessValue4(src, datumY, out double f_007);
                    Debug.WriteLine($"f007: {f_007}");
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
        #endregion

        #region 前面相機
        /// <summary>
        /// 確認是否有工件 (Cam1)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <returns>是否有料件</returns>
        public bool CheckPartCam1(Mat src)
        {
            // ROI
            Rect roi = JawROIs["有料檢知"];

            Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte threshold);
            return threshold is > 50 and < 180;
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
        [Obsolete("此方法有可能找不到 HoughLine")]
        public bool CalContourValue(Mat src, Point leftPt, Point rightPt, out double LeftY, out double RightY, out double d_005max, double correction = 0, double upperLimit = 0.005)
        {
            // 計算 roi
            Rect left = new(leftPt.X - 22, leftPt.Y - 50, 20, 70);
            Rect right = new(rightPt.X + 2, rightPt.Y - 50, 20, 70);

            double sumLength = 0;

            LineSegmentPoint[] lineH;
            double min, max, center;

            Methods.GetRoiCanny(src, left, 50, 120, out Mat leftCanny);
            Methods.GetRoiCanny(src, right, 50, 120, out Mat rightCanny);

            #region 左邊
            Methods.GetHoughLinesHFromCanny(leftCanny, left.Location, out lineH, 2, 2, 5);

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

            #endregion

            #region 右邊
            Methods.GetHoughLinesHFromCanny(rightCanny, right.Location, out lineH, 2, 2, 5);

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : 0; // 先判斷有辨識到最小值

            // 小於中心值
            IEnumerable<LineSegmentPoint> maxH_R = lineH.Where(line => (line.P1.Y + line.P2.Y) / 2 <= center);
            // 計算 maxH 總長
            sumLength = maxH_R.Sum(line => line.Length());
            // 計算平均 Y 座標
            RightY = maxH_R.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));

            #endregion

            // 計算 輪廓度
            d_005max = LeftY != 0 && RightY != 0 ? (Math.Abs(LeftY - RightY) * Cam1Unit) + correction : -1;

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
        /// <param name="roiPos">Jaw 左、右</param>
        /// <param name="p1">(out) 輪廓度基準點</param>
        /// <param name="c_005max">(out) 輪廓度</param>
        /// <param name="correction">校正值</param>
        /// <param name="upperLimit">管制上限 (default: 0.005)</param>
        /// <returns></returns>
        [Obsolete("此方法有機會找不到 HoughLine, 使用 GetContourCornerPoint instead")]
        public bool CalContourValue2(Mat src, Point basePt, double baseX, JawPos roiPos, out Point p1, out Point p2, out double c_005max, double correction = 0, double upperLimit = 0.005)
        {
            Rect roi;

            switch (roiPos)
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

            #region 尋找轉角點
            // 連接輪廓點
            Point[] pts = contours.SelectMany(pts => pts).ToArray();
            // > baseX && < center

            Point[] filter = Array.Empty<Point>();
            // 點 1, 2
            //Point p1;
            p2 = new();
            switch (roiPos)
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

            // Cv2.Rectangle(src, leftRoi, Scalar.Black, 2);
            // Cv2.Rectangle(src, rightRoi, Scalar.Black, 2);

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
            Debug.WriteLine($"前開: {Math.Abs(leftX - rightX)} px, Distance: {distance}");

            leftCanny.Dispose();
            rightCanny.Dispose();
        }

        /// <summary>
        /// 取得輪廓度角點
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePt">基準點</param>
        /// <param name="baseX">基準 X (從前開取得)</param>
        /// <param name="roiPos">Jaw 左、右</param>
        /// <param name="p1">內側角點</param>
        /// <param name="p2">外側角點</param>
        public void GetContourCornerPoint(Mat src, Point basePt, double baseX, JawPos roiPos, out Point p1, out Point p2)
        {
            Rect roi;

            switch (roiPos)
            {
                case JawPos.Left:
                    roi = new Rect((int)baseX + 2, basePt.Y - 45, 20, 60);
                    break;
                case JawPos.Right:
                    roi = new Rect((int)baseX - 22, basePt.Y - 45, 20, 60);
                    break;
                default:
                    roi = new Rect();
                    break;
            }

            double min, max, center;

            // 取得 Canny
            Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
            // 取得輪廓點
            Cv2.FindContours(canny, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, roi.Location);

            //Cv2.Rectangle(src, roi, Scalar.Black, 1);

            #region 計算中心值
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 2, 5);

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : max;    // 計算中心值或是使用最大值
            #endregion

            //foreach (LineSegmentPoint item in lineH)
            //{
            //    Cv2.Line(src, item.P1, item.P2, Scalar.Gray, 2);
            //}

            Debug.WriteLine($"center: {center} {min} {max} {roiPos}");

            #region 尋找轉角點
            // 連接輪廓點
            Point[] pts = contours.SelectMany(pts => pts).ToArray();
            // 新增空 point Array
            Point[] filter = Array.Empty<Point>();

            p2 = new Point();
            switch (roiPos)
            {
                case JawPos.Left:
                    filter = pts.Where(pt => pt.X > baseX && pt.Y < center).Distinct().OrderBy(pt => pt.X).ToArray();
                    // 尋找點 2 // 一定要<= 否則陣列長度有誤
                    for (int i = 1; i <= filter.Length; i++)
                    {
                        if (i == 1) { continue; }

                        if (filter[^i].X < filter[^(i - 1)].X && filter[^i].Y == filter[^(i - 1)].Y)
                        {
                            p2 = filter[^(i - 1)];
                            // Debug.WriteLine($"pt i-1: {filter[^(i - 1)]}");
                            break;
                        }
                    }
                    break;
                case JawPos.Right:
                    filter = pts.Where(pt => pt.X < baseX && pt.Y < center).Distinct().OrderByDescending(pt => pt.X).ToArray();
                    // 尋找點 2 // 一定要<= 否則陣列長度有誤
                    for (int i = 1; i <= filter.Length; i++)
                    {
                        if (i == 1) { continue; }

                        if (filter[^i].X > filter[^(i - 1)].X && filter[^i].Y == filter[^(i - 1)].Y)
                        {
                            p2 = filter[^(i - 1)];
                            // Debug.WriteLine($"pt i-1: {filter[^(i - 1)]}");
                            break;
                        }
                    }
                    break;
                default:
                    break;
            }
            #endregion

            p1 = filter[0];
        }

        /// <summary>
        /// 取得角點 (輪廓度另一邊)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePt">基準點</param>
        /// <param name="baseX">基準 X</param>
        /// <param name="roiPos">ROI 位置</param>
        /// <param name="p1">內側角點</param>
        /// <param name="p2">外側角點</param>
        public void GetCornerPoint(Mat src, Point basePt, double baseX, JawPos roiPos, out Point p1, out Point p2)
        {
            Rect roi;

            switch (roiPos)
            {
                case JawPos.Left:
                    roi = new Rect((int)baseX + 2, basePt.Y - 130, 20, 50);
                    break;
                case JawPos.Right:
                    roi = new Rect((int)baseX - 22, basePt.Y - 130, 20, 50);
                    break;
                default:
                    roi = new Rect();
                    break;
            }

            double min, max, center;

            // 取得 Canny
            Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
            // 取得輪廓點
            Cv2.FindContours(canny, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, roi.Location);

            #region 計算中心值
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 2, 5);

            min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));

            center = Math.Abs(max - min) > 10 ? (min + max) / 2 : min;  // 計算中心值或是使用最小值
            #endregion

            #region 尋找轉角點
            // 連接輪廓點
            Point[] pts = contours.SelectMany(pts => pts).ToArray();
            // 新增空 point Array
            Point[] filter = Array.Empty<Point>();

            // 尋找點
            p2 = new Point();
            switch (roiPos)
            {
                case JawPos.Left:
                    filter = pts.Where(pt => pt.X > baseX && pt.Y > center).Distinct().OrderBy(pt => pt.X).ToArray();
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
                    filter = pts.Where(pt => pt.X < baseX && pt.Y > center).Distinct().OrderByDescending(pt => pt.X).ToArray();
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
            #endregion

            p1 = filter[0];
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
        /// <param name="roiPos">Jaw 左、右</param>
        /// <param name="X">ROI X (從前開取得)</param>
        /// <param name="topY">上邊緣</param>
        /// <param name="botY">下邊緣</param>
        /// <param name="distance">(out) 0.013 距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        public bool Cal013DistanceValue(Mat src, Point basePoint, JawPos roiPos, double X, out double topY, out double botY, out double distance, double correction = 0, double limitL = 0.011, double limitU = 0.015)
        {
            // 計算 roi
            // Rect roi = new(basePoint.X - 20, basePoint.Y - 70, 40, 90);
            Rect roi = new Rect();

            switch (roiPos)
            {
                case JawPos.Left:
                    roi = new Rect((int)X + 1, basePoint.Y - 50, (int)(basePoint.X - X - 2), 70);
                    break;
                case JawPos.Right:
                    roi = new Rect(basePoint.X + 1, basePoint.Y - 50, (int)(X - basePoint.X - 2), 70);
                    break;
                default:
                    break;
            }

            double sumLength = 0;

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 1, 5);

            double min = lineH.Min(line => Math.Min(line.P1.Y, line.P2.Y));
            double max = lineH.Max(line => Math.Max(line.P1.Y, line.P2.Y));
            double center = (min + max) / 2;

            // Cv2.ImShow($"canny{leftRight}", canny);

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

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// 計算 0.013 距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="roiPos">Jaw 左 or右 ROI</param>
        /// <param name="X">ROI X (從前開取得)</param>
        /// <param name="cY">013 基準 1 (從輪廓度取得)</param>
        /// <param name="distance">(out) 013 量測距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU">管制上限</param>
        /// <returns><strong>是否合格</strong></returns>
        public bool Cal013DistanceValue2(Mat src, Point basePoint, JawPos roiPos, double X, double cY, out double distance, double correction = 0, double limitL = 0.011, double limitU = 0.015)
        {
            // 計算 roi
            Rect roi = new Rect();

            switch (roiPos)
            {
                case JawPos.Left:
                    roi = new Rect((int)X, basePoint.Y - 10, 20, 20);
                    break;
                case JawPos.Right:
                    roi = new Rect((int)X - 19, basePoint.Y - 10, 20, 20);
                    break;
                default:
                    break;
            }

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughLinesHFromCanny(canny, roi.Location, out LineSegmentPoint[] lineH, 2, 1, 5);

#if DEBUG || debug
            Cv2.Rectangle(src, roi, Scalar.Black, 1);
            // 這邊要確認 lineH 重複性
            foreach (LineSegmentPoint item in lineH)
            {
                Cv2.Line(src, item.P1, item.P2, Scalar.LightGray, 2);
            }
#endif

            // 線段計算總長
            double sumLength = lineH.Sum(line => line.Length());
            // 計算 Bot Y
            double botY = lineH.Aggregate(0.0, (sum, next) => sum + ((next.P1.Y + next.P2.Y) / 2 * next.Length() / sumLength));
            // 計算 distance
            distance = (Math.Abs(cY - botY) * Cam1Unit) + correction;
            // 銷毀 canny
            canny.Dispose();

            //Debug.WriteLine($"{roiPos} 013 botY: {botY}");

            return limitL <= distance && distance <= limitU;
        }

        /// <summary>
        /// 計算 0.024距離 (左右分開呼叫)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="basePoint">基準點</param>
        /// <param name="roiPos"></param>
        /// <param name="X">ROI X (從前開取得)</param>
        /// <param name="refY">參考 Y (從 013 或輪廓取得)</param>
        /// <param name="botY">上邊緣</param>
        /// <param name="distance">(out) 0.024距離</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitL">管制下限</param>
        /// <param name="limitU"></param>
        /// <returns></returns>
        public bool Cal024DistanceValue(Mat src, Point basePoint, JawPos roiPos, double X, double refY, out double botY, out double distance, double correction, double limitL = 0.0225, double limitU = 0.0255)
        {
            Rect roi;

            switch (roiPos)
            {
                case JawPos.Left:
                    roi = new Rect((int)X + 1, basePoint.Y - 150, (int)(basePoint.X - X - 2), 70);
                    break;
                case JawPos.Right:
                    roi = new Rect(basePoint.X + 1, basePoint.Y - 150, (int)(X - basePoint.X - 2), 70);
                    break;
                default:
                    roi = new Rect();
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
        #endregion

        #region 下面相機
        /// <summary>
        /// 確認是否有工件 (Cam2)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <returns>是否有料件</returns>
        public bool CheckPartCam2(Mat src)
        {
            // ROI
            Rect roi = JawROIs["有料檢知2"];

            //Cv2.Rectangle(src, roi, Scalar.Black, 1);
            Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte threshold);
            //Debug.WriteLine($"threhold: {threshold}");
            return threshold is > 50 and < 180;
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
            Debug.WriteLine($"Right: {rightX} Left: {leftX}, {rightX - leftX}, {distance} {distance:0.00000}");
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
        #endregion

        #region 側面相機
        /// <summary>
        /// 確認是否有工件 (Cam3)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <returns>是否有料件</returns>
        public bool CheckPartCam3(Mat src)
        {
            // ROI 
            Rect roi = JawROIs["有料檢知3"];

            //Cv2.Rectangle(src, roi, Scalar.Black, 1);
            Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte threshold);
            //Debug.WriteLine($"threhold: {threshold}");
            return threshold is > 50 and < 180;
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
        /// 計算平直度
        /// </summary>
        /// <returns></returns>
        [Obsolete("deprecated")]
        public bool Cal007FlatnessValue(Mat src, double baseDatumY, out double[] arrayY, out double flatValue, double correction = 0, double limitU = 0.007)
        {
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // ROIs
            Rect roi = new(140, (int)(baseDatumY + 50), 860, 40);
            //Rect roi = new(140, (int)(baseDatumY + 50), 800, 40);

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
            // 

            Methods.GetRoiCanny(src, roi, 25, 60, out Mat canny);
            Cv2.Rectangle(src, roi, Scalar.Gray, 2);

            List<double> minLineY = new();
            for (int i = 0; i < rois.Length; i++)
            {
                //Cv2.Rectangle(src, rois[i], Scalar.Black, 1);
                Mat c = new(canny, rois[i].Subtract(new Point(roi.X, roi.Y)));
                // Mat c2 = new(canny, rois[i]);
                Methods.GetHoughLinesHFromCanny(c, rois[i].Location, out LineSegmentPoint[] lineH, 5, 5, 3);

                // Methods.GetRoiOtsu(src, rois[i].Subtract(new Point(roi.X, roi.Y)), 0, 255, out Mat Otsu, out byte threshhold);
                // Cv2.ImShow($"otsu{i}", Otsu);
                // Cv2.Rectangle(canny, rois[i].Subtract(new Point(roi.X, roi.Y)), Scalar.Gray, 2);
                //Debug.WriteLine($"{i} {lineH.Length}");

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

#if DEBUG || debug
                    foreach (LineSegmentPoint line in lineH)
                    {
                        Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1);
                    }
#endif
                }
            }
            // 
            // Mat c1 = new Mat(canny, roi1.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c2 = new Mat(canny, roi2.Subtract(new Point(roi.Left, roi.Top)));
            // Mat c3 = new Mat(canny, roi3.Subtract(new Point(roi.Left, roi.Top)));
            // Debug.WriteLine($"{string.Join(",", minLineY)}, count: {minLineY.Count}, {rois.Length}");
            // 

            canny.Dispose();

            arrayY = minLineY.ToArray();
            flatValue = ((minLineY.Max() - minLineY.Min()) * Cam3Unit) + correction;
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            return flatValue <= limitU;
        }

        /// <summary>
        /// 計算平直度
        /// </summary>
        /// <returns></returns>
        [Obsolete("deprecated")]
        public bool Cal007FlatnessValue2(Mat src, double baseDatumY, out double[] arrayY, out double flatValue, double correction = 0, double limitU = 0.007)
        {
            DateTime t1 = DateTime.Now;

            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");
            // ROIs
            Rect roi = new(120, (int)(baseDatumY + 65), 880, 20);
            // Rect roi = new(140, (int)(baseDatumY + 50), 800, 40);

            //Mat src2 = new(src, roi);
            //Mat blur = new();
            //Cv2.BilateralFilter(src2, blur, 7, 10, 5);

            Mat src2 = new Mat(src, roi);
            unsafe
            {
                byte* b = src2.DataPointer;

                for (int i = 0; i < src2.Width; i += 5)
                {
                    if (i is (>= 590 and < 650)) { b += 5; continue; }

                    for (int j = 1; j < src2.Height; j++)
                    {
                        //Debug.WriteLine($"({i}, {j}) : {b[1200 * j]}");

                        if (b[1200 * j] - b[1200 * (j - 1)] < -20)
                        {
                            b[1200 * j] = 0;
                            break;
                        }
                    }

                    b += 5;
                }
            }

            //Cv2.ImShow("src2", src2);

            // Methods.GetRoiCanny(src, roi, 30, 60, out Mat canny3);
            // Methods.GetContoursFromCanny(canny, roi.Location, out Point[][] _, out Point[] pts);

            byte[][] ths = {
                new byte[] { 50, 120 },
                new byte[] { 40, 80},
                new byte[] { 35, 70},
                new byte[] { 30, 60},
                // new byte[] { 25, 50},
            };

            List<double> listY_L = new();
            List<double> listY_L2 = new();
            List<double> listY_R = new();
            List<double> listY_R2 = new();

            int cnt = 0, cnt2 = 0;

            for (int i = 580; i >= 120; i -= 20)
            {
                if (i is (>= 200 and < 260)) { continue; }
                // 計算迴圈次數
                cnt++;

                foreach (byte[] th in ths)
                {
                    bool canBreak = false;

                    // roi 1
                    Rect subRoi = new(i, (int)(baseDatumY + 65), 20, 20);
                    Methods.GetRoiCanny(src, subRoi, th[0], th[1], out Mat c);
                    Methods.GetHoughLinesHFromCanny(c, subRoi.Location, out LineSegmentPoint[] lineH, 5, 5, 3);

                    // roi 2
                    Rect subRoi2 = new(i + 10, (int)(baseDatumY + 65), 20, 20);
                    Methods.GetRoiCanny(src, subRoi2, th[0], th[1], out Mat c2);
                    Methods.GetHoughLinesHFromCanny(c2, subRoi2.Location, out LineSegmentPoint[] lineH2, 5, 5, 3);
                    //Cv2.Line(src, subRoi.X, subRoi.Top, subRoi.X, subRoi.Bottom, Scalar.Black, 1);

                    if (lineH.Length > 0)
                    {
                        double minY = lineH.Min(l => (l.P1.Y + l.P2.Y) / 2);
                        Methods.GetContoursFromCanny(c, subRoi.Location, out Point[][] _, out Point[] pts);
                        // 過濾無關點
                        pts = pts.Where(pt => minY - 1 <= pt.Y && pt.Y <= minY + 1).OrderBy(pt => pt.X).ToArray();
                        // 

                        #region 計算 subpixel Y
                        double num = 0.0, sum = 0.0;
                        for (int j = 1; j < pts.Length; j++)
                        {
                            double dis = pts[j].DistanceTo(pts[j - 1]);
                            sum += dis;
                            num += (pts[j - 1].Y + pts[j].Y) / 2 * dis;
                        }
                        double y = num / sum;
                        #endregion

                        if (listY_L.Count == 0)
                        {
                            listY_L.Add(y);
                            canBreak = true;
                        }
                        else
                        {
                            if (Math.Abs(y - listY_L[^1]) > 2) { continue; }

                            listY_L.Add(y);
                            canBreak = true;
                        }
                    }

                    if (lineH2.Length > 0)
                    {
                        double minY = lineH2.Min(l => (l.P1.Y + l.P2.Y) / 2);
                        Methods.GetContoursFromCanny(c2, subRoi2.Location, out Point[][] _, out Point[] pts);
                        // 過濾無關點
                        pts = pts.Where(pt => minY - 1 <= pt.Y && pt.Y <= minY + 1).OrderBy(pt => pt.X).ToArray();
                        // 

                        #region 計算 subpixel Y
                        double num = 0.0, sum = 0.0;
                        for (int j = 1; j < pts.Length; j++)
                        {
                            double dis = pts[j].DistanceTo(pts[j - 1]);
                            sum += dis;
                            num += (pts[j - 1].Y + pts[j].Y) / 2 * dis;
                        }
                        double y = num / sum;
                        #endregion

                        if (listY_L2.Count == 0)
                        {
                            listY_L2.Add(y);
                            canBreak = true;
                        }
                        else
                        {
                            if (Math.Abs(y - listY_L2[^1]) > 2) { continue; }

                            listY_L2.Add(y);
                            canBreak = true;
                        }
                    }

                    //foreach (LineSegmentPoint line in lineH) { Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1); }
                    //foreach (LineSegmentPoint line in lineH2) { Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1); }

                    //Cv2.Line(src, subRoi.X, subRoi.Top, subRoi.X, subRoi.Bottom, Scalar.Black, 1);
                    //Cv2.Line(src, subRoi2.X, subRoi2.Top, subRoi2.X, subRoi2.Bottom, Scalar.Gray, 1);

                    if (canBreak) { break; }
                }
            }

            for (int i = 600; i <= 980; i += 20)
            {
                if (i is (>= 700 and < 780)) { continue; }
                // 計算迴圈次數
                cnt2++;

                foreach (byte[] th in ths)
                {
                    bool canBreak = false;

                    // roi 1
                    Rect subRoi = new(i, (int)(baseDatumY + 65), 20, 20);
                    Methods.GetRoiCanny(src, subRoi, th[0], th[1], out Mat c);
                    Methods.GetHoughLinesHFromCanny(c, subRoi.Location, out LineSegmentPoint[] lineH, 5, 5, 3);

                    // roi 2
                    Rect subRoi2 = new(i - 10, (int)(baseDatumY + 65), 20, 20);
                    Methods.GetRoiCanny(src, subRoi2, th[0], th[1], out Mat c2);
                    Methods.GetHoughLinesHFromCanny(c2, subRoi2.Location, out LineSegmentPoint[] lineH2, 5, 5, 3);

                    // Cv2.Line(src, subRoi.X, subRoi.Top, subRoi.X, subRoi.Bottom, Scalar.Black, 1);

                    if (lineH.Length > 0)
                    {
                        double minY = lineH.Min(l => (l.P1.Y + l.P2.Y) / 2);
                        Methods.GetContoursFromCanny(c, subRoi.Location, out Point[][] _, out Point[] pts);
                        // 過濾無關點
                        pts = pts.Where(pt => minY - 1 <= pt.Y && pt.Y <= minY + 1).OrderBy(pt => pt.X).ToArray();
                        // double Y = pts.Aggregate(0.0, (sum, next) => sum);
                        // double y = pts.Average(pt => pt.Y);

                        #region 計算 subpixel Y
                        double num = 0.0, sum = 0.0;
                        for (int j = 1; j < pts.Length; j++)
                        {
                            double dis = pts[j].DistanceTo(pts[j - 1]);
                            sum += dis;
                            num += (pts[j - 1].Y + pts[j].Y) / 2 * dis;
                        }
                        double y = num / sum;
                        #endregion

                        // Debug.WriteLine($"{string.Join(",", pts)} {num / sum}");

                        if (listY_R.Count == 0)
                        {
                            listY_R.Add(y);
                            canBreak = true;
                        }
                        else
                        {
                            // 與上一筆相差太多，代表可能邊緣抓取錯誤，直接略過
                            if (Math.Abs(y - listY_R[^1]) > 2) { continue; }

                            listY_R.Add(y);
                            canBreak = true;
                        }
                    }

                    if (lineH2.Length > 0)
                    {
                        double minY = lineH2.Min(l => (l.P1.Y + l.P2.Y) / 2);
                        Methods.GetContoursFromCanny(c2, subRoi2.Location, out Point[][] _, out Point[] pts);
                        // 過濾無關點
                        pts = pts.Where(pt => minY - 1 <= pt.Y && pt.Y <= minY + 1).OrderBy(pt => pt.X).ToArray();
                        //

                        #region 計算 subpixel Y
                        double num = 0.0, sum = 0.0;
                        for (int j = 1; j < pts.Length; j++)
                        {
                            double dis = pts[j].DistanceTo(pts[j - 1]);
                            sum += dis;
                            num += (pts[j - 1].Y + pts[j].Y) / 2 * dis;
                        }
                        double y = num / sum;
                        #endregion

                        if (listY_R2.Count == 0)
                        {
                            listY_R2.Add(y);
                            canBreak = true;
                        }
                        else
                        {
                            // 與上一筆相差太多，代表可能邊緣抓取錯誤，直接略過
                            if (Math.Abs(y - listY_R2[^1]) > 2) { continue; }

                            listY_R2.Add(y);
                            canBreak = true;
                        }
                    }

                    //foreach (LineSegmentPoint line in lineH) { Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1); }
                    //foreach (LineSegmentPoint line in lineH2) { Cv2.Line(src, line.P1, line.P2, Scalar.Black, 1); }

                    //Cv2.Line(src, subRoi2.X, subRoi2.Top, subRoi2.X, subRoi2.Bottom, Scalar.Gray, 1);
                    //Cv2.Line(src, subRoi.X, subRoi.Top, subRoi.X, subRoi.Bottom, Scalar.Black, 1);

                    c.Dispose();
                    c2.Dispose();

                    if (canBreak) { break; }
                }
            }

            Cv2.Rectangle(src, roi, Scalar.Gray, 1);
            // canny.Dispose();

            int len1 = Math.Max(listY_L.Count, listY_L2.Count);
            int len2 = Math.Max(listY_R.Count, listY_R2.Count);

            double[] arrayL = new double[len1];
            double[] arrayR = new double[len2];

            for (int i = 0; i < len1; i++)
            {
                if (listY_L.Count - 1 >= i && listY_L2.Count - 1 >= i)
                {
                    arrayL[i] = (listY_L[i] + listY_L2[i]) / 2;
                }
                else if (listY_L.Count - 1 >= i && listY_L2.Count - 1 < i)
                {
                    arrayL[i] = (listY_L[i]);
                }
                else if (listY_L.Count - 1 < i && listY_L2.Count - 1 >= i)
                {
                    arrayL[i] = (listY_L2[i]);
                }
            }

            for (int i = 0; i < len2; i++)
            {
                //arrayYR2[i] = Math.Sqrt(arrayYR[i] * arrayYR2[i]);
                if (listY_R.Count - 1 >= i && listY_R2.Count - 1 >= i)
                {
                    arrayR[i] = (listY_R[i] + listY_R2[i]) / 2;
                }
                else if (listY_R.Count - 1 >= i && listY_R2.Count - 1 < i)
                {
                    arrayR[i] = (listY_R[i]);
                }
                else if (listY_R.Count - 1 < i && listY_R2.Count - 1 >= i)
                {
                    arrayR[i] = (listY_R2[i]);
                }
            }

            Debug.WriteLine($"{listY_L.Count} {listY_L2.Count}");
            Debug.WriteLine($"{listY_R.Count} {listY_R2.Count}");

            //Debug.WriteLine($"cnt: {cnt2} R: {string.Join(",", arrayYR)} {arrayYR.Length}");

            arrayY = arrayL.Reverse().Concat(arrayR).ToArray();
            //Debug.WriteLine($"cnt: {cnt + cnt2}, {arrayY.Length} Arr: {string.Join(",", arrayY)}");

            flatValue = ((arrayY.Max() - arrayY.Min()) * Cam3Unit) + correction;
            // Debug.WriteLine($"{DateTime.Now:mm:ss.fff}");

            return flatValue <= limitU;
        }

        /// <summary>
        /// 計算平直度 (指標法), 由左往右尋找
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="baseDatumY">基準 Y</param>
        /// <param name="flatValue">平直度</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitU">規格上限</param>
        /// <returns></returns>
        public unsafe bool Cal007FlatnessValue3(Mat src, double baseDatumY, out double flatValue, double correction = 0, double limitU = 0.007)
        {
            // 使用完刪除
            DateTime t1 = DateTime.Now;
            // ROI
            Rect roi = new(120, (int)(baseDatumY + 60), 880, 30);
            // 
            Mat roiMat = new(src, roi);

            byte* b = roiMat.DataPointer;

            List<double> listY = new();
            List<double> listY2 = new();
            for (int i = 0; i < roiMat.Width; i += 3)
            {
                // 避開 pin
                if (i is (>= 590 and < 650))
                {
                    b += 3;
                    continue;
                }

                double[] grayArr = new double[roiMat.Height];
                double tmpGrayAbs = 0;
                int tmpY = 0;
                for (int j = 0; j < roiMat.Height; j++)
                {
                    double avg = ((b[1200 * j] + b[1200 * j + 1] + b[1200 * j + 2]) / 3);
                    grayArr[j] = avg;
                    int k = j - 1;
                    if (j == 0) continue;

                    if (grayArr[j] < grayArr[k] && Math.Abs(grayArr[j] - grayArr[k]) > tmpGrayAbs)
                    {
                        tmpY = j;
                        tmpGrayAbs = Math.Abs(grayArr[j] - grayArr[k]);
                    }

                    if (grayArr[j] - grayArr[k] > 10) break;
                }

                // listY.Add(tmpY);

                if (i == 0 || Math.Abs(tmpY - listY[^1]) < 3)
                {
                    listY.Add(tmpY);

                    b[1200 * tmpY] = 0;
                    // b[1200 * tmpY + 1] = 0;
                    // b[1200 * tmpY + 2] = 0;
                    // b[1200 * tmpY - 1] = 0;
                    // b[1200 * tmpY - 2] = 0;
                    b[1200 * (tmpY + 1)] = 0;
                    b[1200 * (tmpY + 2)] = 0;
                    b[1200 * (tmpY - 1)] = 0;
                    b[1200 * (tmpY - 2)] = 0;
                }
                else { listY.Add(listY[^1]); }

                if (listY.Count > 5) { listY2.Add((listY[^1] + listY[^2] + listY[^3] + listY[^4] + listY[^5] + listY[^6]) / 6); }
                b += 3;
            }

            Cv2.Rectangle(src, roi, Scalar.Gray, 1);

            //Debug.WriteLine($"ListY: {string.Join(",", listY)}");
            //Debug.WriteLine($"ListY2: {string.Join(",", listY2)}");

            flatValue = ((listY2.Max() - listY2.Min()) * Cam3Unit) + correction;
            return false;
        }

        /// <summary>
        /// 計算平直度 (指標法), 由中往兩端尋找
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <param name="baseDatumY">基準 Y</param>
        /// <param name="flatValue">平直度</param>
        /// <param name="correction">校正值</param>
        /// <param name="limitU">規格上限</param>
        /// <returns></returns>
        public unsafe bool Cal007FlatnessValue4(Mat src, double baseDatumY, out double flatValue, double correction = 0, double limitU = 0.007)
        {
            // 使用完刪除
            DateTime t1 = DateTime.Now;
            // ROI
            Rect roi = new(120, (int)(baseDatumY + 60), 860, 30);
            // 
            Mat roiMat = new(src, roi);
            int srcWidth = src.Width;

            byte* b = roiMat.DataPointer;

            List<double> listY = new();
            List<double> listY2 = new();


            double[] grayArr;
            double tmpGrayAbs = 0;
            int tmpY = 0;

            for (int i = roiMat.Width / 2, i2 = roiMat.Width / 2 - 3; i < roiMat.Width || i2 >= 0; i += 3, i2 -= 3)
            {
                // 避開 pin
                if (i is (< 590 or >= 650) && i < roiMat.Width)
                {
                    grayArr = new double[roiMat.Height];
                    tmpGrayAbs = 0;
                    tmpY = 0;
                    for (int j = 0; j < roiMat.Height; j++)
                    {
                        double avg = (b[srcWidth * j + i] + b[srcWidth * j + i + 1] + b[srcWidth * j + i + 2]) / 3;
                        grayArr[j] = avg;
                        int k = j - 1;
                        if (j == 0) continue;

                        //if (i == roiMat.Width / 2)
                        //{
                        //    Debug.WriteLine($"{j} {grayArr[j]} {grayArr[k]} {grayArr[j] - grayArr[k]}tmpY: {tmpY}");
                        //}

                        if (grayArr[j] < grayArr[k] && Math.Abs(grayArr[j] - grayArr[k]) > tmpGrayAbs)
                        {
                            tmpY = j;
                            tmpGrayAbs = Math.Abs(grayArr[j] - grayArr[k]);
                        }

                        if (grayArr[j] - grayArr[k] > 10) break;
                    }

                    // listY.Add(tmpY);

                    //if (i == 0 || Math.Abs(tmpY - listY[^1]) < 3)
                    if (listY.Count == 0 || Math.Abs(tmpY - listY[^1]) < 3)
                    {
                        listY.Add(tmpY);

#if DEBUG || debug
                        // 著色
                        b[srcWidth * tmpY + i] = 0;
                        b[srcWidth * (tmpY + 1) + i] = 0;
                        b[srcWidth * (tmpY + 2) + i] = 0;
                        b[srcWidth * (tmpY - 1) + i] = 0;
                        b[srcWidth * (tmpY - 2) + i] = 0;
#endif
                    }
                    else { listY.Add(listY[^1]); }

                    if (listY.Count > 4) { listY2.Add((listY[^1] + listY[^2] + listY[^3] + listY[^4] + listY[^5]) / 5); }
                }


                if (i2 > 0)
                {
                    grayArr = new double[roiMat.Height];
                    tmpGrayAbs = 0;
                    tmpY = 0;
                    for (int j = 0; j < roiMat.Height; j++)
                    {
                        double avg = (b[srcWidth * j + i2] + b[srcWidth * j + i2 + 1] + b[srcWidth * j + i2 + 2]) / 3;
                        grayArr[j] = avg;
                        int k = j - 1;
                        if (j == 0) continue;

                        if (grayArr[j] < grayArr[k] && Math.Abs(grayArr[j] - grayArr[k]) > tmpGrayAbs)
                        {
                            tmpY = j;
                            tmpGrayAbs = Math.Abs(grayArr[j] - grayArr[k]);
                        }

                        if (grayArr[j] - grayArr[k] > 10) break;
                    }


                    if (Math.Abs(tmpY - listY[0]) < 3)
                    {
                        listY.Insert(0, tmpY);

#if DEBUG || debug
                        // 著色
                        b[srcWidth * tmpY + i2] = 50;
                        b[srcWidth * (tmpY + 1) + i2] = 50;
                        b[srcWidth * (tmpY + 2) + i2] = 50;
                        b[srcWidth * (tmpY - 1) + i2] = 50;
                        b[srcWidth * (tmpY - 2) + i2] = 50;
#endif
                    }
                    else { listY.Insert(0, listY[0]); }


                    if (listY.Count > 4) { listY2.Insert(0, (listY[0] + listY[1] + listY[2] + listY[3] + listY[4]) / 5); }
                }
            }

            //Cv2.Rectangle(src, roi, Scalar.Gray, 1);

            flatValue = ((listY2.Max() - listY2.Min()) * Cam3Unit) + correction;

            roiMat.Dispose();

            Debug.WriteLine($"Y: {listY.Count} Y2:{listY2.Count}");
            Debug.WriteLine($"{(DateTime.Now - t1).TotalMilliseconds} ms");

            return false;
        }
        #endregion
    }
#endif
}