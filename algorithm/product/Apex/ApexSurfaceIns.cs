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
        #region 表面瑕疵檢驗
        /// <summary>
        /// Apex 管件表面檢驗順序
        /// </summary>
        /// <param name="cam1"></param>
        /// <param name="cam2"></param>
        /// <param name="cam3"></param>
        /// <param name="cam4"></param>
        public async void ApexSurfaceInspectionSequence(BaslerCam cam1, BaslerCam cam2, BaslerCam cam3, BaslerCam cam4)
        {
            try
            {
                IGrabResult grabResult1 = null;
                IGrabResult grabResult2 = null;
                Mat mat1 = null;
                Mat mat2 = null;
#if false
                IGrabResult grabResult2 = null;
                IGrabResult grabResult3 = null;
                IGrabResult grabResult4 = null;
                Mat mat2 = null;
                Mat mat3 = null;
                Mat mat4 = null; 
#endif

                #region 保留 

                #endregion

                int CycleCount = 0;
                byte endStep = 0b1001;  // 9

                while (ApexDefectInspectionStepsFlags.SurfaceSteps < endStep)
                {
                    Debug.WriteLine($"Surface Step: {ApexDefectInspectionStepsFlags.SurfaceSteps}");
                    if (CycleCount++ > endStep)
                    {
                        break;
                    }
                    Debug.WriteLine($"Cycle Count: {CycleCount}");

                    switch (ApexDefectInspectionStepsFlags.SurfaceSteps)
                    {
                        case 0b0000:
                            #region 0b0000(0)
                            // 表面檢驗前步驟
                            await PreSurfaceIns();
                            // 開始表面檢驗 // 停止窗戶檢驗 
                            ApexDefectInspectionStepsFlags.SurfaceInsOn = 0b01;
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b0001:    // 1
                            #region 0b0001(1)
                            StartSurfaceCameraContinous();  // 啟動表面相機連續拍攝
                            // 稍微等待，確保相機啟動
                            _ = SpinWait.SpinUntil(() => false, 50);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b0010:    // 2
                            #region 0b0010(2) 
                            // to 550 (窗戶邊緣) // 到點後開始驗窗戶 
                            await PreSurfaceIns2();
                            // 等待 50ms 重新拍攝
                            _ = SpinWait.SpinUntil(() => false, 50);
                            // 開始驗窗戶
                            ApexDefectInspectionStepsFlags.SurfaceInsOn |= 0b10;
                            // _ = SpinWait.SpinUntil(() => false, 3000);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        #region 窗戶表面
                        case 0b0011:    // 3
                            #region 0b0011(3)
                            //// to 1120 (窗戶側邊正對 cam1) //
                            await PreSurfaceWindow1();
                            //// 這邊停止驗窗戶
                            //ApexDefectInspectionStepsFlags.WindowInsOn = 0b0;
                            // 不驗表面、不驗窗戶
                            ApexDefectInspectionStepsFlags.SurfaceInsOn = 0b00;
                            //// 等待光源
                            _ = SpinWait.SpinUntil(() => false, 50);
                            ////_ = SpinWait.SpinUntil(() => false, 3000);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b0100:    // 4
                            #region 0b0100(4)
                            //// 檢驗窗戶毛邊
                            cam1.Camera.ExecuteSoftwareTrigger();
                            cam2.Camera.ExecuteSoftwareTrigger();

                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);

                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);
                            // 正光驗毛邊
                            bool bur = WindowBurrIns(mat1);

                            bool bur2 = EarBurrIns(mat2);

                            Debug.WriteLine($"---------------------------------- bur: {bur}");

                            //Cv2.ImShow("ear bur", mat2.Clone().Resize(OpenCvSharp.Size.Zero, 0.5, 0.5));
                            // 這邊開始驗窗戶
                            // ApexDefectInspectionStepsFlags.WindowInsOn = 0b1;
                            // _ = SpinWait.SpinUntil(() => false, 3000);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
#if false
                        case 0b0101:    // 5
                        #region 0b0101(5)
#if false
                            PreSurfaceWindowSide1();
                            // 等待光源
                            _ = SpinWait.SpinUntil(() => false, 50);
#endif
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                        #endregion
                            break;
                        case 0b0110:    // 6
                        #region 0b0110(6)
#if false
                            cam1.Camera.ExecuteSoftwareTrigger();
                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);
                            // WindowBurrIns(mat1);
                            WindowSideBurrIns(mat1);
                            // 
                            //PostSurfaceWindow1();
                            // 等待 50ms 重新拍攝 
                            //_ = SpinWait.SpinUntil(() => false, 50);
                            // 驗表面、窗戶
                            //ApexDefectInspectionStepsFlags.SurfaceInsOn = 0b11;  
#endif
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                        #endregion
                            break; 
#endif
                        #endregion
                        case 0b0101:
                            #region 0b0101(5)  // 窗毛毛邊驗完 // 回復光源
                            PostSurfaceWindow1();
                            // 等待光源
                            _ = SpinWait.SpinUntil(() => false, 50);
                            ApexDefectInspectionStepsFlags.SurfaceInsOn = 0b11;
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b0110:
                            #region 0b0110(6) // to 1250 (窗戶邊緣) // 到點後結束驗窗戶
                            await PreSurfaceIns3(); // to 1250 (窗戶邊緣)
                            // 等待 50 ms 確定有拍攝
                            _ = SpinWait.SpinUntil(() => false, 50);
                            // 停止驗窗戶
                            ApexDefectInspectionStepsFlags.SurfaceInsOn &= 0b01;
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b0111:
                            #region 0b0111(7) // 
                            await PreSurfaceIns4(); // to 2065 (背面)
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        case 0b1000:
                            #region 0b1000(8) // 
                            StopWindowEarGrabber();         // 停止窗戶、耳朵 Grabber
                            StopSurfaceCameraContinous();   // 表面相機連續拍攝
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            #endregion
                            break;
                        default:
                            throw new ShouldNotBeReachedException("Code here should not be reached.");
                            //throw new Exception("Code here must not be reached");
                    }

                    #region Dispose 物件，釋放記憶體
                    mat1?.Dispose();
                    mat2?.Dispose();
                    grabResult1?.Dispose();
                    grabResult2?.Dispose();
                    #endregion
                }
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
        /// 表面檢測前手續；
        /// Light1：128, 0, 0, 0；
        /// Light2：128, 0；
        /// Motor Pos：-185 (窗戶正對 camera3)；
        /// </summary>
        public async Task PreSurfaceIns()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(80, 800, 3200, 3200);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-185, true);
        }

        /// <summary>
        /// Motor Move to 550 (離開窗戶邊緣)
        /// </summary>
        public async Task PreSurfaceIns2()
        {
            Debug.WriteLine($"pos: {ServoMotion.Axes[1].PosActual}");
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(550, true);
            Debug.WriteLine($"pos: {ServoMotion.Axes[1].PosActual}");
        }

        /// <summary>
        /// Motor Move to 1120 (窗戶側邊正對 Cam1，檢驗毛邊)
        /// Light 1：0, 0, 128, 128
        /// Light 2：0, 0
        /// </summary>
        /// <returns></returns>
        public async Task PreSurfaceWindow1()
        {
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(1120, true);
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(0, 0, 128, 108);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 窗戶正對 cam1 打側光；
        /// Light 1：0, 0, 0, 0
        /// Light 2：64, 0
        /// </summary>
        public void PreSurfaceWindowSide1()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(0, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(64, 0);
        }

        /// <summary>
        /// 回復光源；
        /// Light 1：96, 0, 0, 0；
        /// Light 2：0, 0；
        /// </summary>
        public void PostSurfaceWindow1()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// Motor Move to 1250 (進入窗戶邊緣)
        /// </summary>
        /// <returns></returns>
        public async Task PreSurfaceIns3()
        {
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(1250, true);
        }

        /// <summary>
        /// Motor Move to 2065 (背面窗戶正對 camera3)
        /// </summary>
        /// <returns></returns>
        public async Task PreSurfaceIns4()
        {
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(2065, true);
        }

        /// <summary>
        /// 窗戶毛邊檢驗 (pulse 1120 & 1120 + 2250)
        /// </summary>
        /// <param name="cam"></param>
        public bool WindowBurrIns(Mat src)
        {
            Rect roi = WindowSurfaceRoi;

            #region 開運算
            // Mat ele = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5), new Point(-1, -1));
            // Cv2.MorphologyEx(otsu, otsu, MorphTypes.Close, ele, iterations: 3);
            #endregion

            // Methods.GetRoiCanny(src, roi2, 75, 150, out Mat canny);
            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);

            // Methods.GetContoursX(canny.Clone(), new Rect(0, 0, 80, roi.Height), out double avgLX, out int minLX, out int maxLX);
            Methods.GetContoursX(canny, new Rect(0, 0, 80, roi.Height), out double avgLX, out int minLX, out int maxLX);
            // Methods.GetContoursX(canny.Clone(), new Rect(roi.Width - 80, 0, 80, roi.Height), out double avgRX, out int minRX, out int maxRX);
            Methods.GetContoursX(canny, new Rect(roi.Width - 80, 0, 80, roi.Height), out double avgRX, out int minRX, out int maxRX);

            if (maxLX - minLX > 7 || avgLX - minLX > 5 || maxLX - avgLX > 5)
            {
                return false;
            }
            else if (maxRX - minRX > 7 || avgRX - minRX > 5 || maxRX - avgRX > 5)
            {
                return false;
            }
            return true;
        }

        public bool EarBurrIns(Mat src)
        {
            Rect roi = new(200, 1080, 800, 150);

            Cv2.ImShow("src", src.Resize(OpenCvSharp.Size.Zero, 0.7, 0.7));
            Cv2.ImShow("ear roi", new Mat(src, roi).Resize(OpenCvSharp.Size.Zero, 0.7, 0.7));


            return true;
        }

        /// <summary>
        /// 窗戶毛邊檢驗(側光) 
        /// </summary>
        public void WindowSideBurrIns(Mat src)
        {
            Rect roi = WindowSurfaceRoi2;

            // Cv2.Rectangle(src, new Rect(100, 300, 1000, 1280), Scalar.Gray, 2);
            // Cv2.Rectangle(src, new Rect(150, 300, 200, 1280), Scalar.Black, 1);
            // Cv2.Rectangle(src, new Rect(850, 300, 200, 1280), Scalar.Black, 1);

            Cv2.ImShow($"Window side 1120", new Mat(src, roi).Resize(OpenCvSharp.Size.Zero, 0.5, 0.5));
        }

        /// <summary>
        /// 管件表面檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        public bool SurfaceIns1(Mat src)
        {
            Mat LargeChart = new();

            int peaks = 0;      // 峰值數量
            int valleys = 0;    // 谷值數量

            // 可以刪除
            int k = 0;

            foreach (string key in Surface1ROIsDic.Keys)
            {
                Rect roi = Surface1ROIsDic[key];
                // Pos：550 ~ 1250
                // if (key == "窗" && ApexDefectInspectionStepsFlags.SurfaceSteps != 3)
                // 判斷是否要驗窗戶
                if (key == "窗" && (ApexDefectInspectionStepsFlags.SurfaceInsOn & 0b10) == 0b00)
                {
                    // 可以刪除
                    k++;
                    continue;
                }

#if false
                // if (roi.X == 2350 && ApexDefectInspectionStepsFlags.SurfaceSteps != 3)
                // {
                //     // roi.X = 2350 (窗戶) // Surface = 3，時要檢驗窗戶
                //     // 否則跳過
                //     // continue;
                // }   
#endif

                Methods.GetRoiGaussianBlur(src, roi, new OpenCvSharp.Size(3, 3), 1.2, 0, out Mat blur);

                Mat hist = new();
                Cv2.CalcHist(new Mat[] { blur }, new int[] { 0 }, new Mat(), hist, 1, new int[] { 256 }, new Rangef[] { new Rangef(0.0f, 256.0f) });

                int[] maxArr = new int[1];
                int[] minArr = new int[1];
                Cv2.MinMaxIdx(hist, out double min, out double max, minArr, maxArr);

                Debug.WriteLine($"Max: {max}, Min: {min}, {string.Join(",", maxArr)}, {string.Join(",", minArr)}");

                Mat histChart = new(new OpenCvSharp.Size(256, 300), MatType.CV_8UC3, Scalar.White);
                for (int j = 0; j < 256; j++)
                {
                    int len = (int)(hist.Get<float>(j) / (1.2 * max) * histChart.Rows);
                    Cv2.Line(histChart, j, histChart.Rows, j, histChart.Rows - len, Scalar.Blue, 1);
                }
                Cv2.Line(histChart, maxArr[0], histChart.Rows, maxArr[0], histChart.Rows - (int)(max / (1.2 * max) * histChart.Rows), Scalar.Red, 1);

                // 計算平均值和標準差
                Cv2.MeanStdDev(blur, out Scalar mean, out Scalar stdDev);

                // meanArr[k] = mean[0];
                // stdArr[k] = stdDev[0];

                // 平均值 & 標準差
                Debug.WriteLine($"Mat mean: {mean}, Stddev: {stdDev}");

                // Methods.GetHorizontalGrayScale(blur, out byte[] grayArr, out short[] grayArrDiff, true, out Mat chart, Scalar.Black);
                Methods.GetHorizontalGrayScale(blur, out byte[] grayArr, out double grayMean, true, out Mat chart, Scalar.Black);
                // Methods.CalLocalOutliers(chart, grayArr, 50, 15, stdDev[0], out Point[] peaks, out Point[] valleys);
                Methods.CalLocalOutliers(chart, grayArr, 50, 15, stdDev[0], out int p, out int v);

                peaks += p;
                valleys += v;

                // 眾數線
                Cv2.Line(chart, 0, 300 - maxArr[0], chart.Width, 300 - maxArr[0], Scalar.Orange, 1);
                // 平均數
                //Cv2.Line(chart, 0, 300 - (int)mean[0], chart.Width, 300 - (int)mean[0], Scalar.Blue, 1);
                Cv2.Line(chart, 0, 300 - (int)grayMean, chart.Width, 300 - (int)grayMean, Scalar.Blue, 1);
                // 一個標準差內
                Cv2.Line(chart, 0, 300 - (int)(grayMean + stdDev[0]), chart.Width, 300 - (int)(grayMean + stdDev[0]), Scalar.DarkCyan, 1);
                //Cv2.Line(chart, 0, 300 - (int)(mean[0] + stdDev[0]), chart.Width, 300 - (int)(mean[0] + stdDev[0]), Scalar.DarkCyan, 1);
                Cv2.Line(chart, 0, 300 - (int)(grayMean - stdDev[0]), chart.Width, 300 - (int)(grayMean - stdDev[0]), Scalar.DarkCyan, 1);
                //Cv2.Line(chart, 0, 300 - (int)(mean[0] - stdDev[0]), chart.Width, 300 - (int)(mean[0] - stdDev[0]), Scalar.DarkCyan, 1);

                Cv2.PutText(chart, $"{mean[0]:f2}, {stdDev[0]:f2}", new Point(20, 20), HersheyFonts.HersheySimplex, 0.5, Scalar.Blue, 1);
                //Cv2.Rectangle(src, roi, new Scalar(mean[0]), 1);

                Cv2.CvtColor(blur, blur, ColorConversionCodes.GRAY2BGR);
                Cv2.VConcat(new Mat[] { chart, blur }, chart);

                if (LargeChart.Empty())
                {
                    LargeChart = chart;
                }
                else
                {
                    Cv2.HConcat(LargeChart, chart, LargeChart);
                }

                // 可以刪除
                k++;
            }
            Cv2.Line(src, 1960, 20, 1960, src.Height - 40, Scalar.Black, 2);
            Cv2.Line(LargeChart, 1240, 20, 1240, LargeChart.Height - 40, Scalar.Red, 1);

            //Dispatcher.Invoke(() =>
            //{
            //    LargeChart = LargeChart.Resize(OpenCvSharp.Size.Zero, 0.75, 0.75);
            //    Cv2.ImShow($"GrayScale Chart", LargeChart);
            //    // Cv2.MoveWindow($"GrayScale Chart", 20, 200);
            //});

            Dispatcher.Invoke(() =>
            {
                LargeChart = LargeChart.Resize(OpenCvSharp.Size.Zero, 0.75, 0.75);
                Cv2.ImShow($"GrayScale Chart", LargeChart);
                // Cv2.MoveWindow($"GrayScale Chart", 20, 200);

                if (peaks + valleys > 0)
                {
                    Cv2.ImShow($"Gray Chart{DateTime.Now:ss.fff}", LargeChart);
                }
            });

            #region test here
#if false
            double std1 = new double[4] { stdArr[0], stdArr[5], stdArr[6], stdArr[7] }.Max();
            double std2 = new double[2] { stdArr[1], stdArr[4] }.Max();
            double std3 = new double[2] { stdArr[2], stdArr[3] }.Max();

            Debug.WriteLine($"mean: {string.Join(" , ", meanArr)}");
            Debug.WriteLine($"std: {string.Join(" , ", stdArr)}");

            double mean11Abs = Math.Abs(meanArr[0] - meanArr[5]);
            Debug.WriteLine($"{mean11Abs} {std1}");
            if (mean11Abs > std1 * 2) return false;

            double mean22Abs = Math.Abs(meanArr[0] - meanArr[6]);
            Debug.WriteLine($"{mean22Abs} {std1}");
            if (mean22Abs > std1 * 2) return false;

            double mean33Abs = Math.Abs(meanArr[0] - meanArr[7]);
            Debug.WriteLine($"{mean33Abs} {std1}");
            if (mean33Abs > std1 * 2) return false;

            if (ApexDefectInspectionStepsFlags.WindowInsOn == 0b00)
            {
                double mean2Abs = Math.Abs(meanArr[1] - meanArr[4]);
                if (mean2Abs > std2 * 2) return false;
                Debug.WriteLine($"{mean2Abs} {std2}");
            }
            else
            {

            }

            double mean3Abs = Math.Abs(meanArr[2] - meanArr[3]);
            if (mean3Abs < std3 * 2) return false;
            Debug.WriteLine($"{mean3Abs} {std3}"); 
#endif
            #endregion
            return peaks + valleys == 0;
        }

        /// <summary>
        /// 管件表面檢驗
        /// </summary>
        /// <param name="src"></param>
        public void SurfaceIns2(Mat src)
        {
            return;
        }
        #endregion
    }
}
