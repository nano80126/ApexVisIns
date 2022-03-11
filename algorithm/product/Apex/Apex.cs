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
        public struct ApexAngleCorrectionStruct
        {
            /// <summary>
            /// 工件對位步驟旗標
            /// bit 0 ~ 3, 0 ~ 15
            /// </summary>
            public byte Steps { get; set; }
            /// <summary>
            /// 前一次檢驗之 Width
            /// </summary>
            public ushort LastWindowWidth { get; set; }
            /// <summary>
            /// 最大檢驗之 Width
            /// </summary>
            public ushort MaxWindowWidth { get; set; }
        }


        /// <summary>
        /// Apex 瑕疵檢驗用步驟結構
        /// </summary>
        public struct ApexDefectInspectionSteps
        {
            /// <summary>
            /// 窗戶步驟
            /// 0b0000(0): 
            /// 0b0001(1): 
            /// 0b0010(2): 
            /// 0b0011(3): 
            /// 0b0100(4): 
            /// 0b0101(5): 
            /// 0b0110(6): 
            /// </summary>
            public byte WindowSteps { get; set; }

            /// <summary>
            /// 耳朵檢驗步驟
            /// 0b0000(0): (L)抓取 ROI 打光 & 馬達旋轉；
            /// 0b0001(1): (L)抓取 ROI；
            /// 0b0010(2): (L)抓取 瑕疵 打光；
            /// 0b0011(3): (L)抓取 瑕疵；
            /// 0b0100(4): (L)抓取 瑕疵 打側光；
            /// 0b0101(5): (L)抓取 瑕疵(側光)；
            /// 0b0110(6): (R)抓取 ROI 打光 & 馬達旋轉；
            /// 0b0111(7): (R)抓取 ROI；
            /// 0b1000(8): (R)抓取瑕疵 打光;
            /// 0b1001(9): (R)抓取瑕疵；
            /// 0b1010(10): (R)抓取瑕疵 打側光；
            /// 0b1011(11): (R)抓取 瑕疵(側光)；
            /// </summary>
            public byte EarSteps { get; set; }


            // 管件表面步驟
            // 管件表面步驟
        }

        /// <summary>
        /// Apex 管件選轉定位用結構旗標
        /// </summary>
        public ApexAngleCorrectionStruct ApexAngleCorrectionFlags;

        /// <summary>
        /// Apex 瑕疵檢驗用步驟旗標
        /// </summary>
        public ApexDefectInspectionSteps ApexDefectInspectionStepsFlags;

        #region 工件角度校正，工件角度校正，工件角度校正
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

            if (count == 4 && (ApexAngleCorrectionFlags.Steps & 0b1000) != 0b1000)
            {
                if ((ApexAngleCorrectionFlags.Steps & 0b0001) != 0b0001)
                {
                    if (width >= 350)
                    {
                        //step1done = true;
                        ApexAngleCorrectionFlags.Steps |= 0b0001;
                        ServoMotion.Axes[1].StopMove();
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                        // 停止快動，進入慢速段
                    }
                }
                else if ((ApexAngleCorrectionFlags.Steps & 0b0011) != 0b0011)
                {
                    //if (width < 385)
                    //{
                    if (width > ApexAngleCorrectionFlags.LastWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(5);
                    }
                    else
                    {
                        ApexAngleCorrectionFlags.MaxWindowWidth = ApexAngleCorrectionFlags.LastWindowWidth;
                        //step2done = true;
                        ApexAngleCorrectionFlags.Steps |= 0b0010;
                        // 慢速轉超過，回轉
                    }
                    ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                    //}
                    //else
                    //{
                    //    ApexCountPointFlags.Steps |= 0b0010;
                    //    //step2done = true;
                    //}
                }
                //else if (step2done && !step3done)
                else if ((ApexAngleCorrectionFlags.Steps & 0b0111) != 0b0111)
                {
                    //if (width < 385)
                    //{
                    if (width < ApexAngleCorrectionFlags.MaxWindowWidth && width > ApexAngleCorrectionFlags.LastWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(-3);
                    }
                    else
                    {
                        ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                        //step3done = true;
                        ApexAngleCorrectionFlags.Steps |= 0b0100;
                    }
                    ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                    //}
                    //else
                    //{
                    //    ApexCountPointFlags.Steps |= 0b0100;
                    //}
                }
                else if ((ApexAngleCorrectionFlags.Steps & 0b1111) != 0b1111)
                //else if (step3done && !step4done)
                {
                    //if (width < 385)
                    //{
                    if (width < ApexAngleCorrectionFlags.MaxWindowWidth)
                    {
                        _ = ServoMotion.Axes[1].TryPosMove(1);
                    }
                    else
                    {
                        ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                        ApexAngleCorrectionFlags.Steps |= 0b1000;

                        // 重置 POS
                        ServoMotion.Axes[1].ResetPos();
                    }
                    //}
                }
            }

            Debug.WriteLine($"{ApexAngleCorrectionFlags.Steps}");
            Debug.WriteLine($"{width}");
            Debug.WriteLine($"{ApexAngleCorrectionFlags.MaxWindowWidth}");
        }

        /// <summary>
        /// 角度校正後手續，變更旋轉速度
        /// </summary>
        public void PostAngleCorrection()
        {
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
        }
        #endregion


        #region 窗戶瑕疵檢驗, Window Defect，窗戶瑕疵檢驗, Window Defect，窗戶瑕疵檢驗, Window Defect
        /// <summary>
        /// Apex 窗戶檢驗順序 (單一特徵)，
        /// 此為測試用，正式須配合窗戶檢驗方法
        /// </summary>
        /// <param name="cam"></param>
        public async void ApexWindpwInspectionSequence(BaslerCam cam)
        {
            try
            {
                
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }


        /// <summary>
        /// 取得窗戶瑕疵 ROI 前手續
        /// </summary>
        public async Task PreWindowInspectionRoi()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(320, 0, 160, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得窗戶瑕疵 ROI 前手續 2
        /// </summary>
        /// <returns></returns>
        public void PreWindowInspectionRoi2()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(288, 0, 144, 0);
            //LightCtrls[1].SetAllChannelValue(0, 0);
            //// 變更馬達速度
            //ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            //// 觸發馬達
            //await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得窗戶瑕疵 ROI 前手續 3
        /// </summary>
        /// <returns></returns>
        public void PreWindowInspectionRoi3()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 0);
            //LightCtrls[1].SetAllChannelValue(0, 0);
            //// 變更馬達速度
            //ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            //// 觸發馬達
            //await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得窗戶上下邊緣
        /// </summary>
        /// <param name="src">來源</param>
        /// <param name="top">窗戶上緣</param>
        /// <param name="bottom">窗戶下緣</param>
        public void WindowInspectionTopBottomEdge(Mat src, out double top, out double bottom)
        {
            Rect roi = new(500, 240, 200, 1400);

            Methods.GetRoiCanny(src, roi, 75, 150, out Mat canny);
            Methods.GetHoughWindowYPos(canny, roi.Y, out top, out bottom, 5, 50);
        }

        /// <summary>
        /// 取得窗戶瑕疵檢驗 ROI
        /// </summary>
        public void WindowInspectionRoi(Mat src, out double[] xPos, out Rect roiL, out Rect roiR)
        {
            Rect roi = new(100, 840, 1000, 240);

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out int count, out xPos, 3, 50);
            canny.Dispose();

            /// 陣列抽取，刪除相近邊緣
            if (count >= 7)
            {
                List<double> xList = new();
                for (int i = 0; i < count; i++)
                {
                    // 每個 x 座標至少相差 30 pixel
                    if (i == 0 || xPos[i - 1] + 30 < xPos[i])
                    {
                        xList.Add(xPos[i]);
                    }
                }
                xPos = xList.ToArray();
                xList.Clear();
            }

            #region 待刪除
            for (int i = 0; i < xPos.Length; i++)
            {
                Cv2.Circle(src, new Point(xPos[i], 960), 7, Scalar.Black, 3);
            } 
            #endregion

            if (xPos.Length == 7)
            {
                roiL = new Rect((int)xPos[1] - 20, 240, (int)(xPos[2] - xPos[1]) + 40, 1400);
                roiR = new Rect((int)xPos[^3] - 20, 240, (int)(xPos[^2] - xPos[^3] + 40), 1400);
            }
            else
            {
                roiL = new Rect();
                roiR = new Rect();
            }
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// Light1 : 320, 0, 128, 0；
        /// Light2 : 0, 0；
        /// </summary>
        public void PreWindowInspection()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(320, 0, 128, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// Light1 : 256, 0, 128, 0；
        /// Light2 : 0, 0；
        /// </summary>
        public void PreWindowInspection2()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// Light1 : 256, 0, 96, 0；
        /// Light2 : 0, 0；
        /// </summary>
        public void PreWindowInspection3()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(256, 0, 96, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
        }


#if false
        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(-100)
        /// </summary>
        [Obsolete()]
        public void PreWindowInspection_old()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(256, 0, 114, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(-100, true);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗前手續,
        /// 變更光源, 變更旋轉軸速度, 啟動旋轉軸(-100)
        /// </summary>
        [Obsolete()]
        public void PreWindowInspection_old2()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(224, 0, 114, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 觸發馬達
            ServoMotion.Axes[1].PosMove(-100, true);
        } 
#endif

        /// <summary>
        /// 窗戶瑕疵檢驗，
        /// 測試是否拆步驟 (先取 ROI 再瑕疵檢)
        /// </summary>
        /// <param name="src"></param>
        public void WindowInspection(Mat src, double[] xPos, Rect roiL, Rect roiR)
        {
            if (xPos.Length < 7) return;

            Mat matL = new(src, roiL);
            Mat matR = new(src, roiR);

            #region 取得窗戶 canny
            Methods.GetCanny(matL, 75, 150, out Mat lcw1);
            Methods.GetCanny(matL, 60, 120, out Mat lcw2);
            Methods.GetCanny(matL, 50, 100, out Mat lcw3);

            Methods.GetCanny(matR, 75, 150, out Mat rcw1);
            Methods.GetCanny(matR, 60, 120, out Mat rcw2);
            Methods.GetCanny(matR, 50, 100, out Mat rcw3);

#if false
            #region 待刪除
            Cv2.ImShow("lcw1", lcw1);
            Cv2.MoveWindow("lcw1", 100, 0);
            Cv2.ImShow("lcw2", lcw2);
            Cv2.MoveWindow("lcw2", 300, 0);
            Cv2.ImShow("lcw3", lcw3);
            Cv2.MoveWindow("lcw3", 500, 0);

            Cv2.ImShow("rcw1", rcw1);
            Cv2.MoveWindow("rcw1", 700, 0);
            Cv2.ImShow("rcw2", rcw2);
            Cv2.MoveWindow("rcw2", 900, 0);
            Cv2.ImShow("rcw3", rcw3);
            Cv2.MoveWindow("rcw3", 1100, 0);
            #endregion  
#endif

            matL.Dispose();
            matR.Dispose();
            #endregion

            // 尋找輪廓
            //Cv2.FindContours(lcw3 - lcw1 - lcw2, out Point[][] DiffConsL, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiL.Location);
            Cv2.FindContours(lcw1 & lcw2, out Point[][] DiffConsL, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiL.Location);
            // 尋找輪廓
            //Cv2.FindContours(rcw3 - rcw1 - rcw2, out Point[][] DiffConsR, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiR.Location);
            Cv2.FindContours(rcw1 & rcw2, out Point[][] DiffConsR, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiR.Location);

            // Cv2.ImShow("LLL", lcw3 - lcw1 - lcw2);
            // Cv2.ImShow("RRR", rcw3 - rcw1 - rcw2);
            // Debug.WriteLine($"{DiffConsL.Length} {DiffConsR.Length}");


#if false
            #region 可刪
            Mat ConMatL = new(lcw1.Height, lcw1.Width, MatType.CV_8UC1, Scalar.Black);
            Mat ConMatR = new(rcw1.Height, rcw1.Width, MatType.CV_8UC1, Scalar.Black);
            #endregion  
#endif


            Point[] FilterL = DiffConsL.Where(c => 1 < c.Length && c.Length < 1200).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
            {
                return xPos[1] + 3 < pt.X && pt.X < xPos[2] - 3;
            }).ToArray();


            Point[] FilterR = DiffConsR.Where(c => 1 < c.Length && c.Length < 1200).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
            {
                return xPos[^3] + 3 < pt.X && pt.X < xPos[^2] - 3;
            }).ToArray();

            //Debug.WriteLine($"{xPos[^3]} {xPos[^2]}");

            #region Draw circles
            for (int i = 0; i < FilterL.Length; i++)
            {
                Cv2.Circle(src, FilterL[i], 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatL, FilterL[i].Subtract(roiL.Location), 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatL, FilterL[i], 5, Scalar.Gray, 1);
            }

            Debug.WriteLine($"Left Con Length: {FilterL.Length}");

            //Cv2.Resize(ConMatL, ConMatL, new OpenCvSharp.Size(roiL.Width / 2, roiL.Height / 2));
            //Cv2.ImShow("Left Con Mat", ConMatL);
            //Cv2.MoveWindow("Left Con Mat", 0, 0);

            for (int i = 0; i < FilterR.Length; i++)
            {
                Cv2.Circle(src, FilterR[i], 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatR, FilterR[i].Subtract(roiR.Location), 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatL, FilterL[i], 5, Scalar.Gray, 1);
            }

            Debug.WriteLine($"Right Con Length: {FilterR.Length}");

            //Cv2.Resize(ConMatR, ConMatR, new OpenCvSharp.Size(roiR.Width / 2, roiR.Height / 2));
            //Cv2.ImShow("Right Con Mat", ConMatR);
            //Cv2.MoveWindow("Right Con Mat", 300, 0);
            #endregion
        }

        /// <summary>
        /// 窗戶瑕疵檢驗，
        /// 測試是否拆步驟 (先取 ROI 再瑕疵檢)
        /// </summary>
        /// <param name="src"></param>
        /// <returns>良品(true) / 不良品(false)</returns>
        [Obsolete("待刪除")]
        public bool WindowInspection_old(Mat src)
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
        public void PreWindowInspectionSide()
        {
            // 光源值待定 
            LightCtrls[1].SetAllChannelValue(128, 0);
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)前手續，
        /// 0, 256
        /// </summary>
        public void PreWindowInspectionSide2()
        {
            // 光源值待定 
            LightCtrls[1].SetAllChannelValue(0, 256);
        }

        /// <summary>
        /// 窗戶瑕疵檢驗 (測光) 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public bool WindowInspectionSideLight(Mat src, Rect roi)
        {
            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out byte threshold);

            Debug.WriteLine($"Otsu threshhold : {threshold}");

            if (threshold > 50)
            {
                Cv2.ImShow("Otsu1", otsu);
                Methods.GetCanny(otsu, threshold, (byte)(threshold + 75), out Mat canny);
                Cv2.ImShow("Otsu Canny", canny);
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
        [Obsolete("待刪除")]
        public bool WindowInspectionSideLight_old(Mat src)
        {
            Rect roi = new(350, 1400, 500, 300);

            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out byte threshHold);

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
        [Obsolete("待刪除")]
        public bool WindowInspectionSideLight2_old(Mat src)
        {
            Rect roi = new(350, 160, 500, 300);

            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out byte threshHold);

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

        #region 耳朵瑕疵檢驗， Ear Defect，耳朵瑕疵檢驗， Ear Defect，耳朵瑕疵檢驗， Ear Defect
        /// <summary>
        /// Apex 耳朵檢驗順序 (單一特徵)，
        /// 此為測試用，正式須配合窗戶檢驗方法
        /// </summary>
        /// <param name="cam"></param>
        public async void ApexEarInspectionSequence(BaslerCam cam)
        {
            try
            {
                Rect roiL = new();
                Rect roiR = new();
                int count = 0;

                // 關閉所有視窗
                Cv2.DestroyAllWindows();
                while (ApexDefectInspectionStepsFlags.EarSteps != 0b1100)
                {
                    //SpinWait.SpinUntil(() => false, 500);

                    Debug.WriteLine($"Count: {count} Steps: {ApexDefectInspectionStepsFlags.EarSteps}");
                    if (count++ >= 12)
                    {
                        break;
                    }

                    switch (ApexDefectInspectionStepsFlags.EarSteps)
                    {
                        case 0b0000:    // 0
                            await PreEarInspectionRoiL();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0010:    // 2
                            PreEarInspection();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0100:    // 4
                            PreEarInspectionSide();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0110:    // 6
                            await PreEarInspectionRoiR();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b1000:    // 8
                            PreEarInspection();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b1010:    // 10
                            PreEarInspectionSide();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        default:
                            break;
                    }

                    cam.Camera.ExecuteSoftwareTrigger();

                    using IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                    Debug.WriteLine(grabResult.ImageNumber);

                    if (grabResult.GrabSucceeded)
                    {
                        Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);   // 轉 MatMono 

                        switch (ApexDefectInspectionStepsFlags.EarSteps)
                        {
                            case 0b0001:    // ROI
                                GetEarInspectionRoiL(mat, out roiL, out roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m1 = new();
                                Cv2.Resize(mat, m1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("ROIs", m1);
                                Cv2.MoveWindow("ROIs", 0, 0);
                                #endregion
                                break;
                            case 0b0011:    // 瑕疵
                                EarInspectionL(mat, roiL, roiR);
                                //Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                //Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            case 0b0101:    // 瑕疵 (側光)
                                EarInspectionL(mat, roiL, roiR);

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m11 = new Mat();
                                Cv2.Resize(mat, m11, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("m11", m11);
                                Cv2.MoveWindow("m11", 450, 0); 
                                #endregion
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            case 0b0111:    // ROI
                                GetEarInspectionRoiR(mat, out roiL, out roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m2 = new();
                                Cv2.Resize(mat, m2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("ROIs2", m2);
                                Cv2.MoveWindow("ROIs2", 900, 0);
                                #endregion
                                break;
                            case 0b1001:    // 瑕疵
                                EarInspectionR(mat, roiL, roiR);
                                //Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                //Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            case 0b1011:    // 瑕疵 (側光)
                                EarInspectionR(mat, roiL, roiR);

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m22 = new Mat();
                                Cv2.Resize(mat, m22, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("m22", m22);
                                Cv2.MoveWindow("m22", 1350, 0); 
                                #endregion
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            default:
                                break;
                        }

                        // Cv2.ImShow("mat", mat);
                        ImageSource = mat.ToImageSource();
                    }
                }
                PostEarInspection();
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, T.Message);
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, I.Message);
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, E.Message);
            }
        }

        /// <summary>
        /// 耳朵瑕疵檢測前手續 (L)；
        /// Light: 96, 0, 128, 128；
        /// Motion: xxxxx, -100
        /// </summary>
        public async Task PreEarInspectionRoiL()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            // ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得耳朵瑕疵檢驗 ROI (L)
        /// </summary>
        /// <param name="roi">(out) ROI Rect</param>
        public void GetEarInspectionRoiL(Mat src, out Rect roiL, out Rect roiR)
        {
            Rect roi = new(300, 600, 600, 200);

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out _, out double[] xPos, 3, 50);
            canny.Dispose();

            roiL = new((int)xPos[0] + 1, 600, 50, 200);
            roiR = new((int)xPos[^1] - 51, 580, 50, 200);
        }

        /// <summary>
        /// 耳朵瑕疵前手續；
        /// Light: 256, 0, 128, 96；
        /// </summary>
        [Obsolete("Use PreEarInspection()")]
        public void PreEarInspectionL()
        {
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 96);
            //LightCtrls[1].SetAllChannelValue(0, 128);
        }

        /// <summary>
        /// 耳朵瑕疵檢測 (L)
        /// </summary>
        public bool EarInspectionL(Mat src, Rect roiL, Rect roiR)
        {
            // Canny + Otsu
            Methods.GetRoiOtsu(src, roiL, 0, 255, out Mat Otsu1, out byte th1);
            Methods.GetRoiOtsu(src, roiR, 0, 255, out Mat Otsu2, out byte th2);

            Methods.GetRoiCanny(src, roiL, 75, 150, out Mat Canny1);
            Methods.GetRoiCanny(src, roiR, 75, 150, out Mat Canny2);

            // 這邊要寫演算，ex 毛邊、車刀紋、銑削不良


            Mat concat = new();

            Cv2.HConcat(new Mat[] { Otsu1, Otsu2, Canny1, Canny2 }, concat);
            Otsu1.Dispose();
            Otsu2.Dispose();
            Canny1.Dispose();
            Canny2.Dispose();

            Cv2.ImShow("concat", concat);
            Cv2.MoveWindow("concat", 20, 0);
            return true;
        }

        /// <summary>
        /// 耳朵瑕疵檢測前手續 (R)；
        /// Light: 96, 0, 128, 128；
        /// Motion: xxxxx, 100；
        /// </summary>
        public async Task PreEarInspectionRoiR()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            //ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 10000, 10000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(100, true);
        }

        /// <summary>
        /// 取得耳朵瑕疵檢驗 ROI (R)
        /// </summary>
        /// <param name="roi">(out) ROI Rect</param>
        public void GetEarInspectionRoiR(Mat src, out Rect roiL, out Rect roiR)
        {
            Rect roi = new(300, 600, 600, 200);

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out _, out double[] xPos, 3, 50);
            canny.Dispose();

            roiL = new((int)xPos[0] + 1, 580, 50, 200);
            roiR = new((int)xPos[^1] - 51, 600, 50, 200);
        }

        /// <summary>
        /// 耳朵瑕疵前手續；
        /// Light: 256, 0, 128, 96；
        /// </summary>
        [Obsolete("Use PreEarInspection()")]
        public void PreEarInspectionR()
        {
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 96);
            //LightCtrls[1].SetAllChannelValue(0, 128);
        }

        /// <summary>
        /// 耳朵瑕疵檢測 (L)
        /// </summary>
        public bool EarInspectionR(Mat src, Rect roiL, Rect roiR)
        {
            // Canny + Otsu
            Methods.GetRoiOtsu(src, roiL, 0, 255, out Mat Otsu1, out byte th1);
            Methods.GetRoiOtsu(src, roiR, 0, 255, out Mat Otsu2, out byte th2);

            Methods.GetRoiCanny(src, roiL, 75, 150, out Mat Canny1);
            Methods.GetRoiCanny(src, roiR, 75, 150, out Mat Canny2);

            // 這邊要寫演算，ex 毛邊、車刀紋、銑削不良

            Mat concat = new();

            Cv2.HConcat(new Mat[] { Otsu1, Otsu2, Canny1, Canny2 }, concat);
            Otsu1.Dispose();
            Otsu2.Dispose();
            Canny1.Dispose();
            Canny2.Dispose();

            Cv2.ImShow("concat2", concat);
            Cv2.MoveWindow("concat2", 20, 300);
            return true;
        }

        /// <summary>
        /// 耳朵瑕疵前手續；
        /// Light1: 256, 0, 128, 96；
        /// </summary>
        public void PreEarInspection()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(256, 0, 128, 96);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 耳朵瑕疵前手續 (Side Light)；
        /// Light1 : 128, 0, 0, 0；
        /// Light2 : 0, 128；
        /// </summary>
        public void PreEarInspectionSide()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(128, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 128);
        }

        /// <summary>
        /// 耳朵檢測完畢，關閉所有光源
        /// </summary>
        public void PostEarInspection()
        {
            // 變更光源 1
            LightCtrls[0].ResetAllChannel();
            // 變更光源 2
            LightCtrls[1].ResetAllChannel();
        }
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