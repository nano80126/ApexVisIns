using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Point = OpenCvSharp.Point;

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// BFR 測試用結構
        /// </summary>
        private struct NitinolBFR
        {
            /// <summary>
            /// Pin 中心
            /// </summary>
            public double centerY;
            /// <summary>
            /// Pin 上緣
            /// </summary>
            public double topMinY;
            /// <summary>
            /// Pin 下緣
            /// </summary>
            public double botMaxY;

            /// <summary>
            /// Point1 of transformation 
            /// </summary>
            public Point pt1;
            /// <summary>
            /// Point2 of transformation
            /// </summary>
            public Point pt2;

            /// <summary>
            /// 點佇列 1
            /// </summary>
            public readonly Queue<Point> ptQueue1;
            /// <summary>
            /// 點佇列 2 
            /// </summary>
            public readonly Queue<Point> ptQueue2;

            /// <summary>
            /// 假溫度 (測試時使用, Start from -40 ℃)
            /// </summary>
            public double FakeTemp;

            public NitinolBFR(double value)
            {
                centerY = topMinY = botMaxY = value;
                pt1 = new Point(0, 0);
                pt2 = new Point(0, 0);
                ptQueue1 = new Queue<Point>();
                ptQueue2 = new Queue<Point>();
                FakeTemp = -40;
            }
        }

        /// <summary>
        /// Struct for BFR process
        /// </summary>
        private NitinolBFR StructBFR = new(0);

        /// <summary>
        /// 處理 Nitinol 演算法
        /// </summary>
        /// <param name="mat"></param>
        public void ProcessNitinol(Mat mat)
        {
            int matWidth = mat.Width;
            int matHeight = mat.Height;

            algorithm.Nitinol img = new(mat);   // 裡面會 Dispose mat

            try
            {
                // img.Sharpen();   // 銳化

                // 計算 pin 中心位置
                if (StructBFR.centerY == 0)
                {
                    int X = (matWidth / 2) - 40;   // (1920 / 2) - 40
                    int Y = matHeight / 10;        // (1920 / 10)
                    Rect roi = new(X, Y, 80, matHeight - (2 * Y));  // Create a roi at center of image

                    //Dispatcher.Invoke(() => img.GetHorizontalPinCenter(roi, out centerY, true));
                    //Dispatcher.Invoke(() => img.GetHorizontalPinCenter(roi, out centerY, true));
                    //Dispatcher.Invoke(() => img.GetHorizontalPinCenter(roi, out centerY, true));
                    img.GetHorizontalPinCenter(roi, out StructBFR.centerY, true);
#if DEBUG
                    Debug.WriteLine($"Center Y: {StructBFR.centerY}");
#endif
                }

                // 計算 pin 上緣位置
                if (StructBFR.topMinY == 0)
                {
                    int posX = matWidth / 4;
                    int posY = (matHeight / 2) - 182 - 40;   // height / 2 - 182 (pixel / 2) - 40 (roi height)
                    int width = matWidth / 2;

                    img.GetHorizontalPosY(new Rect(posX, posY, width, 80), new Point(0, StructBFR.centerY - (matHeight / 2)), out double topY, out StructBFR.topMinY, out _, true);

#if DEBUG
                    Debug.WriteLine($"Top: {topY}");
#endif
                }

                // 計算 pin 下緣位置
                if (StructBFR.botMaxY == 0)
                {
                    int posX = matWidth / 4;
                    int posY = (matHeight / 2) + 182 - 40;
                    int width = matWidth / 2;
                    img.GetHorizontalPosY(new Rect(posX, posY, width, 80), new Point(0, StructBFR.centerY - (matHeight / 2)), out double botY, out _, out StructBFR.botMaxY, true);

#if DEBUG
                    Debug.WriteLine($"Bot: {botY}");
#endif
                }

#if true

                int roiX1 = (int)AssistPoints[0].X - 50;
                int roiX2 = (int)AssistPoints[1].X - 50;

                //Debug.WriteLine($"roiX1: {AssistPoints[0].X}, roiX2: {AssistPoints[1].X}");

                // Y 座標 < pin 上緣 (topMinY)
                img.GetContoursMaxX(new Rect(roiX1 < 0 ? 0 : roiX1, (int)(StructBFR.topMinY - 30 - 8), 100, 30), StructBFR.topMinY, false, out Point topPt, true);
                // Y 座標 > pin 下緣 (bitMaxY)
                img.GetContoursMaxX(new Rect(roiX2 < 0 ? 0 : roiX2, (int)(StructBFR.botMaxY + 8), 100, 30), StructBFR.botMaxY, true, out Point botPt, true);
#endif

                // 畫圖
                img.DoActions();

                StructBFR.ptQueue1.Enqueue(topPt);
                StructBFR.ptQueue2.Enqueue(botPt);

                Dispatcher.Invoke(() =>
                {
                    // FIFO 五點平均
                    if (StructBFR.ptQueue1.Count >= 3)
                    {
                        StructBFR.pt1 = new Point(StructBFR.ptQueue1.Average(pt => pt.X), StructBFR.ptQueue1.Average(pt => pt.Y));
                        _ = StructBFR.ptQueue1.Dequeue();
                        //StructBFR.ptQueue1.Clear();

                        AssistPoints[0].AssignPoint(StructBFR.pt1.X, StructBFR.pt1.Y);
                    }

                    // FIFO 五點平均
                    if (StructBFR.ptQueue2.Count >= 3)
                    {
                        StructBFR.pt2 = new Point(StructBFR.ptQueue2.Average(pt => pt.X), StructBFR.ptQueue2.Average(pt => pt.Y));
                        _ = StructBFR.ptQueue2.Dequeue(); // 去頭
                        //StructBFR.ptQueue2.Clear(); // 清除

                        AssistPoints[1].AssignPoint(StructBFR.pt2.X, StructBFR.pt2.Y);
                    }

                    if (BFRTrail.Running)
                    {
                        // 溫度測試
                        if (BFRTrail.TemperatureEnable)
                        {
                            if (!Thermometer.IsSerialPortOpen)
                            {
                                MsgInformer.AddError(MsgInformer.Message.MsgCode.BFR, "Thermometer is not connected", MsgInformer.Message.MessageType.Warning);
                            }

                            // Y 軸變形量, X 軸溫度
                            BFRTrail.AddRecordNewTemperature(StructBFR.pt1.X, StructBFR.pt2.X, Thermometer.Temperature);
                            // 更新 Chart
                            ListViewTab.RenderPlot();
                        }
                        // 自由測試
                        else if (BFRTrail.Unrestricted)
                        {
                            if (StructBFR.pt1.X != 0 && StructBFR.pt2.X != 0)
                            {
                                // Y 軸變形量, X 軸溫度
                                BFRTrail.AddRecord(StructBFR.pt1.X, StructBFR.pt2.X, StructBFR.FakeTemp++);
                                // 更新 Chart 
                                ListViewTab.RenderPlot();
                            }
                        }

                        //// 重啟後會重置 
                        // if (StructBFR.pt1.X == 0 || StructBFR.pt2.X == 0)
                        // {
                        //     Debug.WriteLine($"{StructBFR.pt1} , {StructBFR.pt2}");
                        // }
                    }

                    // Indicator.Image = img.GetMat();
                    if (OnTabIndex == 0)
                    {
                        ImageSource = img.GetMat().ToImageSource();
                    }
                });


                if (BFRTrail.TemperatureEnable && BFRTrail.Running)
                {
                    if (Thermometer.Temperature >= BFRTrail.Temperature)
                    {
                        //BFRTrail.Stop();
                        BFRTrail.End();
                    }
                }
                //img.Dispose();
            }
            catch (OpenCVException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCV, ex.Message, MsgInformer.Message.MessageType.Error);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            catch (OpenCvSharpException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.OPENCVS, ex.Message, MsgInformer.Message.MessageType.Error);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgInformer.AddError(MsgInformer.Message.MsgCode.EX, ex.Message, MsgInformer.Message.MessageType.Error);
                    ImageSource = img.GetMat().ToImageSource();
                });
            }
            finally
            {
                img.Dispose();
            }
        }
    }
}

namespace ApexVisIns.algorithm
{
    public class Nitinol : Algorithm
    {
        //public MainWindow mw { set; get; } 

        //private readonly static byte Thread1 = 140;
        //private readonly static byte Thread2 = 60;

        public Nitinol() { }

        public Nitinol(Mat mat) : base(mat) { }

        /// <summary>
        /// 銳化影像
        /// </summary>
        public void Sharpen()
        {
            try
            {
                // kernel
                InputArray kernel = InputArray.Create(new int[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } });
                Cv2.Filter2D(img, img, MatType.CV_8U, kernel, new OpenCvSharp.Point(-1, -1), 0);
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
        /// 計算 pin 位置
        /// </summary>
        /// <param name="roi">ROI</param>
        /// <param name="Y">Y 座標</param>
        /// <param name="drawImg">是否畫圖</param>
        public void GetHorizontalPinCenter(Rect roi, out double Y, bool drawImg = true)
        {
            try
            {
                Mat clone = new(img, roi);  // 要 Dispose

                Methods.GetHoughLinesH(clone, roi.Location, 85, 20, out LineSegmentPoint[] lineH, 3);

                if (lineH?.Length > 0)
                {
                    double Max = lineH.Max(item => Math.Max(item.P1.Y, item.P2.Y));
                    double Min = lineH.Min(item => Math.Min(item.P1.Y, item.P2.Y));
                    double Mean = (Max + Min) / 2.0;
                    Y = Mean;
                }
                else
                {
                    Y = 0;
                }

                if (drawImg)
                {
                    ActionAdd(() =>
                    {
                        Cv2.Rectangle(img, roi, Scalar.White, 2);
                        for (int i = 0; i < lineH.Length; i++)
                        {
                            Cv2.Line(img, lineH[i].P1, lineH[i].P2, Scalar.White, 2);
                        }
                    });
                }
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
        /// 計算 水平線 Y 座標
        /// </summary>
        /// <param name="roi">ROI</param>
        /// <param name="offset">偏移</param>
        /// <param name="Y">Y 座標</param>
        /// <param name="minY">最小值Y</param>
        /// <param name="maxY"> </param>
        /// <param name="drawimg"></param>
        public void GetHorizontalPosY(Rect roi, OpenCvSharp.Point offset, out double Y, out double minY, out double maxY, bool drawimg = true)
        {
            try
            {
                Rect _roi = roi.Add(offset);
                Mat clone = new(img, _roi);
                //
                Methods.GetHoughLinesH(clone, _roi.Location, 85, 50, out LineSegmentPoint[] lineH, 3);

                if (lineH?.Length > 0)
                {
                    // 計算線總長
                    double totalLength = lineH.Sum(line => line.Length());
                    // 計算 Y 軸座標 // 個別水平線 Y 軸座標 * 占比
                    double value = lineH.Sum(line => (line.P1.Y + line.P2.Y) / 2.0 * (line.Length() / totalLength));
                    // 平均 Y
                    Y = value;

                    maxY = 0;
                    minY = roi.Height;

                    double temp;
                    double max = 0;
                    double min = img.Height;

                    Array.ForEach(lineH, line =>
                    {
                        if (min > (temp = Math.Min(line.P1.Y, line.P2.Y)))
                        {
                            min = temp;
                        }

                        if (max < (temp = Math.Max(line.P1.Y, line.P2.Y)))
                        {
                            max = temp;
                        }
                    });
                    // 最小 Y
                    minY = min;
                    // 最大 Y
                    maxY = max;
                }
                else
                {
                    Y = 0;
                    minY = img.Height;
                    maxY = 0;
                }

                if (drawimg)
                {
                    ActionAdd(() =>
                    {
                        Cv2.Rectangle(img, _roi, Scalar.White, 2);
                        for (int i = 0; i < lineH.Length; i++)
                        {
                            Cv2.Line(img, lineH[i].P1, lineH[i].P2, Scalar.LightGray, 2);
                        }
                    });
                }
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
        /// 計算 垂直線 X 座標
        /// </summary>
        /// <param name="roi"></param>
        /// <param name="offset"></param>
        /// <param name="X"></param>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="drawimg"></param>
        public void GetVerticalPosX(Rect roi, OpenCvSharp.Point offset, out double X, out double minX, out double maxX, bool drawimg = true)
        {
            X = 0;
            minX = 0;
            maxX = 0;
        }

        /// <summary>
        /// 取得大於(小於)邊界值 Y 且 X 最大之座標點
        /// </summary>
        /// <param name="roi"></param>
        /// <param name="marinY">邊界值 Y</param>
        /// <param name="greater">是否大於邊界值</param>
        /// <param name="pt"></param>
        /// <param name="drawImg"></param>
        public void GetContoursMaxX(Rect roi, double marinY, bool greater, out OpenCvSharp.Point pt, bool drawImg = true)
        {
            try
            {
                pt = new Point();

                Mat clone = new(img, roi);
                Methods.GetContours(clone, new Point(roi.X, roi.Y), 60, 5, out Point[][] con, out Point[] connectedCon);

                if (greater)
                {

                    for (int i = 0; i < connectedCon.Length; i++)
                    {
                        if (connectedCon[i].Y > marinY)
                        {
                            if (connectedCon[i].X > pt.X)
                            {
                                pt = connectedCon[i];
                            }
                        }
                    }
                }
                else
                {

                    for (int i = connectedCon.Length - 1; i >= 0; i--)
                    {
                        if (connectedCon[i].Y < marinY)
                        {
                            if (connectedCon[i].X > pt.X)
                            {
                                pt = connectedCon[i];
                            }
                        }
                    }
                }

                if (drawImg)
                {
                    ActionAdd(() =>
                    {
                        for (int i = 0; i < con.Length; i++)
                        {
                            Cv2.DrawContours(img, con, i, Scalar.White, 1);
                        }
                        //Cv2.Rectangle(img, roi, Scalar.Orange, 1);
                        Cv2.Rectangle(img, roi, Scalar.Black, 1);
                    });
                }
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
            //base.Dispose(disposing);
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //img.Dispose();
                srcImg.Dispose();
                //throw new NotImplementedException();
            }
            _disposed = true;
        }
    }
}
