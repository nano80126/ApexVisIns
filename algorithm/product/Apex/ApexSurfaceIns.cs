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
                IGrabResult grabResult3 = null;
                IGrabResult grabResult4 = null;
                Mat mat1 = null;
                Mat mat2 = null;
                Mat mat3 = null;
                Mat mat4 = null;

                #region 保留 

                #endregion

                int CycleCount = 0;

                byte endStep = 0b0111;

                while (ApexDefectInspectionStepsFlags.SurfaceSteps != 0b1000)
                {
                    Debug.WriteLine($"Surface Step: {ApexDefectInspectionStepsFlags.SurfaceSteps}");
                    if (CycleCount++ > endStep)
                    {
                        break;
                    }
                    Debug.WriteLine($"Cycle Count: {CycleCount}");

                    switch (ApexDefectInspectionStepsFlags.SurfaceSteps)
                    {
                        case 0b0000:    // 0
                            // 表面檢驗前步驟
                            await PreSurfaceIns();
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0001:    // 1
                            StartSurfaceCameraContinous();
                            // 稍微等待，確保相機啟動
                            _ = SpinWait.SpinUntil(() => false, 50);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0010:    // 2
                            // to 550 (窗戶邊緣) // 
                            await PreSurfaceIns2();
                            _ = SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0011:    // 3
                            // to 1250 () // 開始驗窗戶
                            await PreSurfaceIns3();
                            _ = SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0100:    // 4
                            // to 
                            await PreSurfaceIns4();
                            _ = SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        default:
                            break;
                    }
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
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 4000, 4000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-185, true);
        }

        /// <summary>
        /// Motor Move to 550 (離開窗戶邊緣)
        /// </summary>
        public async Task PreSurfaceIns2()
        {
            await ServoMotion.Axes[1].PosMoveAsync(550, true);
        }

        /// <summary>
        /// Motor Move to 1250 (進入窗戶邊緣)
        /// </summary>
        /// <returns></returns>
        public async Task PreSurfaceIns3()
        {
            await ServoMotion.Axes[1].PosMoveAsync(1250, true);
        }

        /// <summary>
        /// Motor Move to 2065 (背面窗戶正對 camera3)
        /// </summary>
        /// <returns></returns>
        public async Task PreSurfaceIns4()
        {
            await ServoMotion.Axes[1].PosMoveAsync(2065, true);
        }

        /// <summary>
        /// 管件表面檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        public void SurfaceIns1(Mat src)
        {
            Mat LargeChart = new Mat();

            // 灰階均值 Arr
            double[] meanArr = new double[6];
            // 標準差 Arr
            double[] stdArr = new double[6];
            // APEX尾端用
            double[] tempArr = new double[3];

            // for (int i = 0; i < Surface1ROIs.Length; i++)
            // {
            //     Rect roi = Surface1ROIs[i];
            // }

            for (int i = 0; i < Surface1ROIs.Length; i++)
            {
                Rect roi = Surface1ROIs[i];
                // Pos：550 ~ 1250
                if (roi.X == 2350 && ApexDefectInspectionStepsFlags.SurfaceSteps != 3)
                {
                    // roi.X = 2350 (窗戶) // Surface = 3，時要檢驗窗戶
                    // 否則跳過
                    continue;
                }

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
                Cv2.MeanStdDev(blur, out Scalar mean, out Scalar stddev);
                // 平均值 & 標準差
                Debug.WriteLine($"Mat mean: {mean}, Stddev: {stddev}");

                Methods.GetHorizontalGrayScale(blur, out byte[] grayArr, out short[] grayArrDiff, true, out Mat chart, Scalar.Black);
                Methods.CalLocalOutliers(chart, grayArr, 50, 15, stddev[0], out Point[] peaks, out Point[] valleys);

                // 眾數線
                Cv2.Line(chart, 0, 300 - maxArr[0], chart.Width, 300 - maxArr[0], Scalar.Orange, 1);
                // 平均數
                Cv2.Line(chart, 0, 300 - (int)mean[0], chart.Width, 300 - (int)mean[0], Scalar.Blue, 1);
                // 一個標準差內
                Cv2.Line(chart, 0, 300 - (int)(mean[0] + stddev[0]), chart.Width, 300 - (int)(mean[0] + stddev[0]), Scalar.DarkCyan, 1);
                Cv2.Line(chart, 0, 300 - (int)(mean[0] - stddev[0]), chart.Width, 300 - (int)(mean[0] - stddev[0]), Scalar.DarkCyan, 1);

                Cv2.PutText(chart, $"{mean[0]:f2}, {stddev[0]:f2}", new Point(20, 20), HersheyFonts.HersheySimplex, 0.5, Scalar.Blue, 1);

                Cv2.Rectangle(src, roi, new Scalar(mean[0]), 1);

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
                //Dispatcher.Invoke(() =>
                //{
                //    Cv2.ImShow($"GrayScale{roi.X}", chart);
                //    Cv2.MoveWindow($"GrayScale{roi.X}", (roi.X - 720) / 2, 100 + roi.X / 10);
                //    Cv2.ImShow($"Hist Diagram{roi.X}", histChart);
                //    Cv2.MoveWindow($"Hist Diagram{roi.X}", (roi.X - 720) / 2, 100 + 300 + roi.X / 10);
                //    // Cv2.MoveWindow("GrayScale", 100, 100);
                //});
            }
            Cv2.Line(src, 1960, 20, 1960, src.Height - 40, Scalar.Black, 2);
            Cv2.Line(LargeChart, 1240, 20, 1240, LargeChart.Height - 40, Scalar.Red, 1);

            Dispatcher.Invoke(() =>
            {
                LargeChart = LargeChart.Resize(OpenCvSharp.Size.Zero, 0.75, 0.75);
                Cv2.ImShow($"GrayScale Chart", LargeChart);
                // Cv2.MoveWindow($"GrayScale Chart", 20, 200);
            });
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
