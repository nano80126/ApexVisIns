using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
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

        #region 窗戶耳朵同時檢驗
        public async void ApexWindowEarInspectionSequence(BaslerCam cam1, BaslerCam cam2)
        {
            try
            {
                IGrabResult grabResult1 = null;
                IGrabResult grabResult2 = null;
                Mat mat1 = null;
                Mat mat2 = null;

                #region 窗戶
                Rect winRoiL = Rect.Empty;
                Rect winRoiR = Rect.Empty;
                double top = 0; // 窗戶內部上緣
                double bot = 0; // 窗戶內部下緣
                double[] xPos = Array.Empty<double>();
                double[] xPos2 = Array.Empty<double>();
                double[] xPos3 = Array.Empty<double>();
                double[] xArray = Array.Empty<double>();
                #endregion

                #region 耳朵
                Rect earRoiL = Rect.Empty;
                Rect earRoiR = Rect.Empty;

                Mat holeMask = null;
                Point2f holeC = new(0, 0);
                float holeR = 0;
                #endregion

                int CycleCount = 0;
                byte endStep = 0b1010;

                while (ApexDefectInspectionStepsFlags.Steps != endStep)
                {
                    Debug.WriteLine($"Step: {ApexDefectInspectionStepsFlags.Steps}");

                    if (CycleCount++ > endStep)
                    {
                        break;
                    }
                    Debug.WriteLine($"Cycle Count: {CycleCount}");

                    switch (ApexDefectInspectionStepsFlags.Steps)
                    {
                        case 0b0000:
                            #region 0b0000(0) // 0.111 孔毛邊
                            PreEarHoleIns();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            // 擷取
                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            EarHoleIns(mat2, out holeC, out holeR);
                            // holeMask = new Mat(); //holeC = holeC.Add(EarHoleRoi.Location);

                            // holeMask = new Mat(EarHoleRoi.Height, EarHoleRoi.Width, MatType.CV_8UC1, Scalar.Black);
                            // Cv2.Circle(holeMask, (int)holeC.X, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            // Debug.WriteLine($"hole center: {holeC}, hole r: {holeR}");

                            // Cv2.ImShow("holeMask", holeMask);

                            ApexDefectInspectionStepsFlags.Steps += 0b01;
                            #endregion
                            break;
                        case 0b0001:
                            #region 0b0001(1) // 窗戶 & 耳朵(L) ROI
                            await PreEarLWindowRoi();

                            cam1.Camera.ExecuteSoftwareTrigger();
                            cam2.Camera.ExecuteSoftwareTrigger();

                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);

                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 抓取窗戶上下緣
                            GetWindowInspectionTopBottomEdge(mat1, out top, out bot);
                            // 抓取窗戶、耳朵 ROI
                            GetEarWindowRoi(mat1, mat2, out xPos, out winRoiL, out winRoiR, 0, out earRoiL, out earRoiR);

                            if (xPos.Length == 7)
                            {
                                Debug.WriteLine($"xPos: {string.Join(" , ", xPos)}");

                                xArray = xPos;
                                xPos = null;
                                winRoiL.Y = winRoiR.Y = (int)top;
                                winRoiL.Height = winRoiR.Height = (int)(bot - top);
                                ApexDefectInspectionStepsFlags.Steps += 0b10; // 2
                            }
                            else
                            {
                                Debug.WriteLine($"xPos' {string.Join(" , ", xPos)}");

                                ApexDefectInspectionStepsFlags.Steps += 0b01; // 1
                            }
                            #endregion
                            break;
                        case 0b0010:
                            #region 0b0010(2) // 窗戶 & 耳朵(L) ROI
                            PreEarLWindowRoi2();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam1.Camera.ExecuteSoftwareTrigger();
                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);

                            GetEarWindowRoi(mat1, null, out xPos2, out winRoiL, out winRoiR, 0, out _, out _);

                            if (xPos2.Length == 7)
                            {
                                Debug.WriteLine($"xPos2: {string.Join(" , ", xPos2)}");

                                xArray = xPos2;
                                xPos2 = null;
                                winRoiL.Y = winRoiR.Y = (int)top;
                                winRoiL.Height = winRoiR.Height = (int)(bot - top);
                                ApexDefectInspectionStepsFlags.Steps += 0b01; // 1
                            }
                            else
                            {
                                xArray = xPos.Concat(xPos2).OrderBy(x => x).ToArray();

                                #region 陣列抽取
                                List<double> xList = new(7);
                                for (int i = 0; i < xArray.Length; i++)
                                {
                                    if (i == 0 || xArray[i - 1] + 30 < xArray[i])
                                    {
                                        xList.Add(xArray[i]);
                                    }
                                }
                                xArray = xList.ToArray();
                                xList.Clear();
                                xList = null;
                                #endregion

                                Debug.WriteLine($"xArray': {string.Join(" , ", xArray)}");

                                winRoiL = new Rect((int)xArray[1] - 2, (int)top, (int)(xArray[2] - xArray[1]) + 4, (int)(bot - top));
                                winRoiR = new Rect((int)xArray[^3] - 2, (int)top, (int)(xArray[^2] - xArray[^3]) + 4, (int)(bot - top));
                                xPos = xPos2 = xPos3 = null;

                                ApexDefectInspectionStepsFlags.Steps += 0b01; // 1
                            }
                            #endregion
                            break;
                        case 0b0011:
                            #region 0b0011(3) // 窗戶 & 耳朵(L) 檢測
                            PreEarWindowIns();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam1.Camera.ExecuteSoftwareTrigger();
                            cam2.Camera.ExecuteSoftwareTrigger();

                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);

                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1); // 窗戶影像
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2); // 耳朵影像

                            //Debug.WriteLine($"xPos :{xPos.Length}");
                            // 窗戶檢測
                            WindowInspection(mat1, xArray, winRoiL, winRoiR);
                            // 耳朵檢測
                            EarInsL(mat2, earRoiL, earRoiR);

                            #region 畫耳朵 ROI
                            //Methods.GetOtsu(holeMatL, 0, 255, out Mat holeOtsu, out _);
                            //Cv2.VConcat(holeMatL, holeOtsu, holeOtsu);
                            //Cv2.ImShow("Hole (L)", holeOtsu.Clone());
                            //Cv2.ImShow("hole Mask", holeMask.Clone());

                            // 畫孔圓
                            //Rect r1 = new(earRoiL.Right, EarHoleRoi.Top, earRoiR.Right - earRoiL.Right, EarHoleRoi.Height);
                            //holeMask = new Mat(r1.Height, r1.Width, MatType.CV_8UC1);
                            //Cv2.Circle(mat2, ((earRoiR.Right + earRoiL.Right) / 2) + 10, EarHoleRoi.Top + (int)holeC.Y, (int)holeR, Scalar.White, 2);
                            //Mat r1Mat = new(mat2, r1);
                            //Cv2.ImShow("r1Mat", r1Mat);

                            // 耳朵側邊 // ROI
                            Cv2.Rectangle(mat2, earRoiL, Scalar.Gray, 2);
                            Cv2.Rectangle(mat2, earRoiR, Scalar.Gray, 2);
                            Cv2.Resize(mat2, mat2, OpenCvSharp.Size.Zero, 0.5, 0.5);
                            Cv2.ImShow("Ear(L)", mat2.Clone());
                            Cv2.MoveWindow("Ear(L)", 1200, 20);
                            #endregion

                            ApexDefectInspectionStepsFlags.Steps += 0b01; // 1 
                            #endregion
                            break;
                        case 0b0100:
                            #region 0b0100(4) // 窗戶 側光檢測
                            PreWindowInsSide();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam1.Camera.ExecuteSoftwareTrigger();
                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);

                            // 窗戶側光檢測
                            Rect roiTop = new((int)xArray[1], (int)(top - 80), (int)(xArray[^2] - xArray[1]), 120);
                            WindowInspectionSideLight(mat1, roiTop);

                            Mat winTop = new(mat1, roiTop);
                            Cv2.ImShow("winTop", winTop);
                            Cv2.MoveWindow("winTop", 20, 300);

                            ApexDefectInspectionStepsFlags.Steps += 0b01;
                            #endregion
                            break;
                        case 0b0101:
                            #region 0b0101(5) // 窗戶 側光檢測 2
                            PreWindowInsSide2();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam1.Camera.ExecuteSoftwareTrigger();
                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);

                            Rect roiBot = new((int)xArray[1], (int)(bot - 40), (int)(xArray[^2] - xArray[1]), 120);
                            WindowInspectionSideLight(mat1, roiBot);

                            Mat winBot = new(mat1, roiBot);
                            Cv2.ImShow("winBot", winBot);
                            Cv2.MoveWindow("winBot", 20, 500);

                            ApexDefectInspectionStepsFlags.Steps += 0b01;
                            #endregion
                            break;
                        case 0b0110:
                            #region 0b0110(6) // 耳朵 (L) 側光檢測
                            PreEarInsSide();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測 (側光)
                            EarInsL(mat2, earRoiL, earRoiR);


                            #region 孔周圍
                            Rect holeRoiL = new(earRoiL.Right, 850, earRoiR.Right - earRoiL.Right, 200);
                            Mat holeMatL = new(mat2, holeRoiL);   // 這個要清除

                            holeMask = new Mat(holeMatL.Height, holeMatL.Width, MatType.CV_8UC1, Scalar.Black);
                            Cv2.Circle(holeMask, (holeMask.Width / 2) + 10, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            // 耳朵孔檢測
                            EarHoleInsL(holeMatL, holeMask);
                            holeMatL.Dispose();
                            #endregion

                            //Rect holeRoi = new Rect(earRoiL.X + earRoiL.Width, 850, earRoiR.X - earRoiL.X, 200);
                            //Mat holeMat = new(mat2, holeRoi);
                            //Methods.GetOtsu(holeMat, 0, 255, out Mat holeOtsu, out _);
                            //Cv2.VConcat(holeMat, holeOtsu, holeOtsu);
                            //Cv2.ImShow("Hole (L)", holeOtsu.Clone());
                            //Mat hole = new(mat2, new Rect(ear));

                            ApexDefectInspectionStepsFlags.Steps += 0b01;   // 1
                            #endregion
                            break;
                        case 0b0111:
                            #region 0b0111(7) // 窗戶 & 耳朵(L) ROI
                            await PreEarRWindowRoi();

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);
                            //cam1.Camera.ExecuteSoftwareTrigger();
                            //grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            //mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);

                            GetEarWindowRoi(null, mat2, out _, out _, out _, 1, out earRoiL, out earRoiR);

                            ApexDefectInspectionStepsFlags.Steps += 0b01;   // 1
                            #endregion
                            break;
                        case 0b1000:
                            #region 0b1000(8) // 耳朵 (R) 檢測
                            PreEarWindowIns();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測
                            EarInsR(mat2, earRoiL, earRoiR);

                            #region 孔周圍
                            //Rect holeRoiR = new Rect(earRoiL.Left, EarHoleRoi.Y, earRoiR.Left - earRoiL.Left, 200);
                            //Mat holeMatR = new Mat(mat2, holeRoiR);

                            //holeMask = new Mat(holeMatR.Height, holeMatR.Width, MatType.CV_8UC1, Scalar.Black);
                            //Cv2.Circle(holeMask, (holeMask.Width / 2) - 10, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            //// 耳朵孔檢測
                            //EarHoleInsR(holeMatR, holeMask);
                            //holeMatR.Dispose();
                            #endregion

                            #region 畫耳朵 ROI
                            //Methods.GetOtsu(holeMatR, 0, 255, out Mat holeOtsu2, out _);
                            //Cv2.VConcat(holeMatR, holeOtsu2, holeOtsu2);
                            //Cv2.ImShow("Hole (R)", holeOtsu2.Clone());
                            //Cv2.ImShow("Hole Mask (R)", holeMask.Clone());

                            // 畫孔圓
                            //Rect r2 = new(earRoiL.Left, EarHoleRoi.Top, earRoiR.Left - earRoiL.Left, EarHoleRoi.Height);
                            //Cv2.Circle(mat2, ((earRoiR.Left + earRoiL.Left) / 2) - 10, EarHoleRoi.Top + (int)holeC.Y, (int)holeR, Scalar.White, 2);
                            //Mat r2Mat = new(mat2, r2);
                            //Cv2.ImShow("r2Mat", r2Mat);

                            // 耳朵側邊 // ROI
                            Cv2.Rectangle(mat2, earRoiL, Scalar.Gray, 2);
                            Cv2.Rectangle(mat2, earRoiR, Scalar.Gray, 2);
                            Cv2.Resize(mat2, mat2, OpenCvSharp.Size.Zero, 0.5, 0.5);
                            Cv2.ImShow("Ear (R)", mat2.Clone());
                            Cv2.MoveWindow("Ear (R)", 1400, 20);
                            #endregion

                            ApexDefectInspectionStepsFlags.Steps += 0b01;   // 1
                            #endregion
                            break;
                        case 0b1001:
                            #region 0b1001(9) // 耳朵 (R) 側光檢測
                            PreEarInsSide();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測 (側光)
                            EarInsR(mat2, earRoiL, earRoiR);

                            #region 孔周圍
                            Rect holeRoiR = new Rect(earRoiL.Left, EarHoleRoi.Y, earRoiR.Left - earRoiL.Left, 200);
                            Mat holeMatR = new Mat(mat2, holeRoiR);

                            holeMask = new Mat(holeMatR.Height, holeMatR.Width, MatType.CV_8UC1, Scalar.Black);
                            Cv2.Circle(holeMask, (holeMask.Width / 2) - 10, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            // 耳朵孔檢測
                            EarHoleInsR(holeMatR, holeMask);
                            holeMatR.Dispose();
                            #endregion

                            // Rect holeRoi2 = new(earRoiL.X, 850, earRoiR.X - earRoiL.X, 200);
                            // Mat holeMat2 = new(mat2, holeRoi2);
                            // Methods.GetOtsu(holeMat2, 0, 255, out Mat holeOtsu2, out _);
                            // Cv2.VConcat(holeMat2, holeOtsu2, holeOtsu2);
                            // Cv2.ImShow("Hole (R)", holeOtsu2.Clone());

                            ApexDefectInspectionStepsFlags.Steps += 0b01;   // 1
                            #endregion
                            break;
                        default:        // 10
                            throw new Exception("Code here must not be reached");
                    }

                    #region Dispose 物件，釋放記憶體
                    mat1?.Dispose();
                    mat2?.Dispose();
                    grabResult1?.Dispose();
                    grabResult2?.Dispose();
                    #endregion
                }
                // 檢驗結束，關閉燈光
                PostIns();
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{T.Message}");
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{I.Message}");
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{E.Message}");
            }
        }

        /// <summary>
        /// 窗戶、耳朵(L) ROI 尋找前步驟；
        /// Light 1：96, 0, 108, 128；
        /// Light 2：0, 0；
        /// Motion ：10, 500, 1000, 1000;
        /// Pos Abs：-100
        /// </summary>
        /// <returns></returns>
        public async Task PreEarLWindowRoi()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 108, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(10, 500, 1000, 1000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 窗戶、耳朵(R) ROI 尋找前步驟；
        /// Light 1：96, 0, 108, 128；
        /// Light 2：0, 0；
        /// Pos Abs：100
        /// </summary>
        /// <returns></returns>
        public async Task PreEarRWindowRoi()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 108, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(10, 500, 1000, 1000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(100, true);
        }

        /// <summary>
        /// 窗戶、耳朵(L) ROI 尋找前步驟Ver.2；
        /// Light 1：96, 0, 108, 128 
        /// Light 2：0, 0 
        /// </summary>
        /// <returns></returns>
        public void PreEarLWindowRoi2()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 取得窗戶、耳朵(L) ROI
        /// </summary>
        /// <param name="src">影像(窗戶)</param>
        /// <param name="src2">影像(耳朵)</param>
        /// <param name="winXpos">窗戶xArray</param>
        /// <param name="wRoiL">窗戶 ROI 左</param>
        /// <param name="wRoiR">窗戶 ROI 右</param>
        /// <param name="direction">耳朵方向，src2 為 null 時無作用</param>
        /// <param name="eRoiL">耳朵 ROI 左，src2 為 null 時無作用</param>
        /// <param name="eRoiR">耳朵 ROI 右，src2 為 null 時無作用</param>
        public void GetEarWindowRoi(Mat src, Mat src2, out double[] winXpos, out Rect wRoiL, out Rect wRoiR, byte direction, out Rect eRoiL, out Rect eRoiR)
        {
            if (src != null)
            {
                GetWindowInspectionRoi(src, out winXpos, out wRoiL, out wRoiR);
                #region 刪除
                // Cv2.Rectangle(src, wRoiL, Scalar.Black, 2);
                // Cv2.Rectangle(src, wRoiR, Scalar.Black, 2);
                // Cv2.Resize(src, src, new OpenCvSharp.Size(src.Width / 2, src.Height / 2));
                // Cv2.ImShow($"win {DateTime.Now:ss.fff}", src.Clone());
                #endregion
            }
            else
            {
                wRoiL = Rect.Empty;
                wRoiR = Rect.Empty;
                winXpos = Array.Empty<double>();
            }

            if (src2 != null)
            {
                if (direction == 0)
                {
                    GetEarInsRoiL(src2, out eRoiL, out eRoiR);
                }
                else if (direction == 1)
                {
                    GetEarInsRoiR(src2, out eRoiL, out eRoiR);
                }
                else
                {
                    throw new InvalidOperationException("Invalid direction param, Must be 0 or 1.");
                }
            }
            else
            {
                eRoiL = Rect.Empty;
                eRoiR = Rect.Empty;
            }
        }

        /// <summary>
        /// 耳朵、窗戶瑕疵檢側前步驟
        /// </summary>
        public void PreEarWindowIns()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 96);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 重置所有光源 
        /// </summary>
        public void PostIns()
        {
            // 變更光源 1
            LightCtrls[0].ResetAllChannel();
            // 變更光源 2
            LightCtrls[1].ResetAllChannel();

        }
        #endregion
    }
}
