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
        #region 保留等待重構
        /// <summary>
        /// Apex 處理,
        /// 保留做為參考
        /// </summary>
        /// <param name="mat">來源影像</param>
        [Obsolete("等待重構")]
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
        #endregion


        ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// 
        ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// 
        ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// 

        /// <summary>
        /// Apex 對位用 Flag 結構
        /// </summary>
        public struct ApexCounterPointStruct
        {
            /// <summary>
            /// 工件對位步驟旗標
            /// bit 0 ~ 3
            /// </summary>
            public byte Steps { get; set; }
            public ushort LastWindowWidth { get; set; }
            public ushort MaxWindowWidth { get; set; }
        }

        public ApexCounterPointStruct ApexCountPointFlags;

        public void InitCountPointStruct()
        {
            ApexCountPointFlags = new ApexCounterPointStruct();
        }

        /// <summary>
        /// 角度校正前手續
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(轉一圈多)
        /// </summary>
        public void PreAngleCorrection()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(96, 0, 128, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(20, 200, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(5000);
        }

        /// <summary>
        /// 角度校正, 
        /// 校正後旋轉軸歸零.  
        /// ※需要連續拍攝
        /// </summary>
        /// <param name="src"></param>
        public void AngleCorrection(Mat src)
        {
            // 進入前要 Call PreCounterPos()
            // 變更光源 (96, 0, 128, 0)
            // 變更馬達速度 (20, 200, 10000,10000)

            Rect roi = new(100, 840, 1000, 240);

            Methods.GetRoiCanny(src, roi, 75, 120, out Mat canny);
            Methods.GetVertialWindowWidth(canny, out int count, out double width);

            if (count == 4 && (ApexCountPointFlags.Steps & 0b1000) != 0b1000)
            {
                if ((ApexCountPointFlags.Steps & 0b0001) != 0b0001)
                {
                    if (width >= 350)
                    {
                        //step1done = true;
                        ApexCountPointFlags.Steps |= 0b0001;
                        ServoMotion.Axes[1].StopMove();
                        ApexCountPointFlags.LastWindowWidth = (ushort)width;
                        ApexCountPointFlags.MaxWindowWidth = (ushort)width;
                        // 停止快動，進入慢速段
                    }
                }
                else if ((ApexCountPointFlags.Steps & 0b0011) != 0b0011)
                {
                    //if (width < 385)
                    //{
                    if (width > ApexCountPointFlags.LastWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(5);
                    }
                    else
                    {
                        ApexCountPointFlags.MaxWindowWidth = ApexCountPointFlags.LastWindowWidth;
                        //step2done = true;
                        ApexCountPointFlags.Steps |= 0b0010;
                        // 慢速轉超過，回轉
                    }
                    ApexCountPointFlags.LastWindowWidth = (ushort)width;
                    //}
                    //else
                    //{
                    //    ApexCountPointFlags.Steps |= 0b0010;
                    //    //step2done = true;
                    //}
                }
                //else if (step2done && !step3done)
                else if ((ApexCountPointFlags.Steps & 0b0111) != 0b0111)
                {
                    //if (width < 385)
                    //{
                    if (width < ApexCountPointFlags.MaxWindowWidth && width > ApexCountPointFlags.LastWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(-3);
                    }
                    else
                    {
                        ApexCountPointFlags.MaxWindowWidth = (ushort)width;
                        //step3done = true;
                        ApexCountPointFlags.Steps |= 0b0100;
                    }
                    ApexCountPointFlags.LastWindowWidth = (ushort)width;
                    //}
                    //else
                    //{
                    //    ApexCountPointFlags.Steps |= 0b0100;
                    //}
                }
                else if ((ApexCountPointFlags.Steps & 0b1111) != 0b1111)
                //else if (step3done && !step4done)
                {
                    //if (width < 385)
                    //{
                    if (width < ApexCountPointFlags.MaxWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(1);
                    }
                    else
                    {
                        ApexCountPointFlags.MaxWindowWidth = (ushort)width;
                        ApexCountPointFlags.Steps |= 0b1000;

                        // 重置 POS
                        ServoMotion.Axes[1].ResetPos();
                    }
                    //}
                }
            }

            Debug.WriteLine($"{ApexCountPointFlags.Steps}");
            Debug.WriteLine($"{width}");
            Debug.WriteLine($"{ApexCountPointFlags.MaxWindowWidth}");
        }

        #region 窗戶瑕疵, Window Defect
        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(-100)
        /// </summary>
        public void PreWindowInspection()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(320, 0, 128, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(-100, true);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(-100)
        /// </summary>
        public void PreWindowInspection2()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(256, 0, 114, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(-100, true);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(-100)
        /// </summary>
        public void PreWindowInspection3()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(224, 0, 114, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(-100, true);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗，
        /// 測試是否拆步驟 (先取 ROI 再瑕疵檢)
        /// </summary>
        /// <param name="src"></param>
        /// <returns>良品(true) / 不良品(false)</returns>
        public bool WindowInspection(Mat src)
        {
            Rect roi = new(100, 840, 1000, 240);

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out int count, out double[] xPos);
            canny.Dispose();

            #region 陣列抽取
            List<double> xPosList = new();
            for (int i = 0; i < xPos.Length; i++)
            {
                if (i == 0 || xPos[i - 1] + 5 < xPos[i])
                {
                    xPosList.Add(xPos[i]);
                }
            }
            xPos = xPosList.ToArray();
            xPosList.Clear();
            xPosList = null;
            #endregion

            Debug.WriteLine($"count: {xPos.Length}; {string.Join(" , ", xPos.Select(x => Math.Round(x, 2)))}");

            // 尋找管內窗戶邊緣, 位置約落在 750 ~ 780
            int cIdx = Array.FindIndex(xPos, 0, x => x is < 780 and > 750);
            Debug.WriteLine($"center index: {cIdx}");

            if (count >= 7)
            {
                Rect leftRoiWindow = new((int)xPos[1] - 20, 255, (int)xPos[cIdx - 1] - (int)xPos[1] + 40, 1400);
                Rect rightRoiWindow = new((int)xPos[cIdx + 1] - 20, 255, (int)xPos[^2] - (int)xPos[cIdx + 1] + 40, 1400);

                Mat leftRoiMat = new(src, leftRoiWindow);       // left canny window
                Mat rightRoiMat = new(src, rightRoiWindow);     // right canny window   

                #region 取得窗戶 canny
                Methods.GetCanny(leftRoiMat, 75, 150, out Mat lcw1);    // left canny window 1
                Methods.GetCanny(leftRoiMat, 60, 120, out Mat lcw2);
                Methods.GetCanny(leftRoiMat, 50, 100, out Mat lcw3);
                //Methods.GetCanny(leftRoiMat, 35, 150, out Mat cannyWindow4);

                Methods.GetCanny(rightRoiMat, 75, 150, out Mat rcw1);   // right canny window 1
                Methods.GetCanny(rightRoiMat, 60, 120, out Mat rcw2);
                Methods.GetCanny(rightRoiMat, 50, 100, out Mat rcw3);
                //Methods.GetCanny(rightRoiMat, 35, 150, out Mat cannyWindow44);
                #endregion

                // 尋找輪廓，內部輪廓 - 外部輪廓
                Cv2.FindContours(lcw3 - lcw1 - lcw2, out Point[][] leftConsDiff, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, leftRoiWindow.Location);
                // 尋找輪廓
                Cv2.FindContours(rcw3 - rcw1 - rcw2, out Point[][] rightConsDiff, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, rightRoiWindow.Location);


                #region 可刪
                Mat leftConMat = new(lcw1.Height, lcw1.Width, MatType.CV_8UC1, Scalar.Black);
                Mat rightConMat = new(rcw1.Height, rcw1.Width, MatType.CV_8UC1, Scalar.Black);
                #endregion

                // 過濾過短輪廓
                Point[] leftFilter = leftConsDiff.Where(c => c.Length > 20).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
                {
                    return xPos[1] + 3 < pt.X && pt.X < xPos[cIdx - 1] - 3;
                }).ToArray();

                // 過濾過短輪廓
                Point[] rightFilter = rightConsDiff.Where(c => c.Length > 20).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
                {
                    return xPos[cIdx + 1] + 3 < pt.X && pt.X < xPos[^2] - 3;
                }).ToArray();

                /// 左邊
                for (int i = 0; i < leftFilter.Length; i++)
                {
                    Cv2.Circle(leftConMat, leftFilter[i].Subtract(leftRoiWindow.Location), 5, Scalar.Gray, 1);
                    Cv2.Circle(src, leftFilter[i], 5, Scalar.Red, 2);
                    //Debug.WriteLine($"{leftFilter[i]}  {leftFilter[i].Subtract(rightRoiWindow.Location)}");
                }
                Debug.WriteLine($"Left Con Length: {leftFilter.Length}");

                Cv2.Resize(leftConMat, leftConMat, new OpenCvSharp.Size(leftRoiWindow.Width * 3 / 5, leftRoiWindow.Height * 3 / 5));
                Cv2.ImShow("Left Con Mat", leftConMat);
                Cv2.MoveWindow("Left Con Mat", 0, 0);

                /// 右邊
                for (int i = 0; i < rightFilter.Length; i++)
                {
                    Cv2.Circle(rightConMat, rightFilter[i].Subtract(rightRoiWindow.Location), 5, Scalar.Gray, 1);
                    Cv2.Circle(src, rightFilter[i], 5, Scalar.Red, 2);
                    //Debug.WriteLine($"{rightFilter[i]}  {rightFilter[i].Subtract(rightRoiWindow.Location)}");
                }
                Debug.WriteLine($"Right Con Length: {rightFilter.Length}");

                Cv2.Resize(rightConMat, rightConMat, new OpenCvSharp.Size(rightRoiWindow.Width * 3 / 5, rightRoiWindow.Height * 3 / 5));
                Cv2.ImShow("Right Con Mat", rightConMat);
                Cv2.MoveWindow("Right Con Mat", leftRoiWindow.X - 100, 0);
                //Cv2.dra

                #region 畫出標示 (之後移除)
                // 找出 / 標示分界點
                //for (int i = 0; i < xPos.Length; i++)
                //{
                //    Cv2.Circle(src, new Point(xPos[i], 960), 7, Scalar.Black, 3);
                //}
                // 標示 窗戶 ROI
                Cv2.Rectangle(src, leftRoiWindow, Scalar.Gray, 2);
                // 標示 窗戶 ROI
                Cv2.Rectangle(src, rightRoiWindow, Scalar.Gray, 2);
                #endregion
            }

            #region 標示分界
            for (int i = 0; i < xPos.Length; i++)
            {
                Cv2.Circle(src, new Point(xPos[i], 960), 7, Scalar.Black, 3);
            }
            #endregion

            /// 等耳朵一起處理好，決定閾值
            /// 等耳朵一起處理好，決定閾值
            /// 等耳朵一起處理好，決定閾值


            return true;
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)前手續，
        /// 128, 0
        /// </summary>
        public void PreWindowInspectionSideLight()
        {
            // 光源值待定 
            LightCtrls[1].SetAllChannelValue(128, 0);
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)前手續，
        /// 0, 256
        /// </summary>
        public void PreWindowInspectionSideLight2()
        {
            // 光源值待定 
            LightCtrls[1].SetAllChannelValue(0, 256);
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <returns>良品(true) / 不良品(false)</returns>
        public bool WindowInspectionSideLight(Mat src)
        {
            Rect roi = new(350, 1400, 500, 300);

            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out double threshHold);

            /// 待刪
            Debug.WriteLine($"{threshHold}");

            if (threshHold > 50)
            {
                // 如果需要回傳顯示不良範圍
                // 這邊處理
                // code here

                otsu.Dispose();
                return false;
            }
            else
            {
                otsu.Dispose();
                return true;
            }
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)
        /// </summary>
        /// <param name="src">來源影像</param>
        /// <returns>良品(true) / 不良品(false)</returns>
        public bool WindowInspectionSideLight2(Mat src)
        {
            Rect roi = new(350, 160, 500, 300);

            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out double threshHold);

            /// 待刪
            Debug.WriteLine($"{threshHold}");

            if (threshHold > 50)
            {
                // 如果需要回傳顯示不良範圍
                // 這邊處理
                // code here

                otsu.Dispose();
                return false;
            }
            else
            {
                otsu.Dispose();
                return true;
            }
        }
        #endregion

        #region 耳朵
        /// <summary>
        /// 耳朵瑕疵檢測前手續，
        /// Light: 96, 0, 128, 128
        /// Motion: xxxxx, -100
        /// </summary>
        public void PreEarInspectionRoi()
        {
            LightCtrls[0].SetAllChannelValue(96, 0, 128, 128);
        }

        /// <summary>
        /// 取得耳朵瑕疵檢驗 ROI
        /// </summary>
        /// <param name="roi">(out) ROI Rect</param>
        public void GetEarInspectionRoi(Mat src, out Rect roiL, out Rect roiR)
        {
            Rect roi = new(300, 600, 600, 200);

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out _, out double[] xPos, 3, 50);
            canny.Dispose();

            roiL = new((int)xPos[0] + 1, 600, 50, 200);
            roiR = new((int)xPos[1] - 51, 580, 50, 200);
        }

        /// <summary>
        /// 耳朵瑕疵前手續，
        /// Light: 256, 0, 128, 96
        /// Motion: xxxxx, -100
        /// </summary>
        public void PreEarInspection()
        {
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 96);
        }

        /// <summary>
        /// 耳朵瑕疵檢測
        /// </summary>
        public bool EarInspection(Mat src, Rect roiL, Rect roiR)
        {
            // Canny + Otsu

            return true;
        }

        // 另一邊
        #endregion
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


    }
}