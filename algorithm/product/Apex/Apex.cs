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

        #region APEX ROI
        /// <summary>
        /// 角度校正窗戶ROI
        /// </summary>
        private readonly Rect WindowLeftRightRoi = new(100, 840, 1000, 240);
        /// <summary>
        /// 窗戶抓取上下邊緣Roi
        /// </summary>
        private readonly Rect WindowTopBotEdgeRoi = new(450, 240, 250, 1400);
        /// <summary>
        /// 耳朵孔ROI
        /// </summary>
        private readonly Rect EarHoleRoi = new(500, 850, 200, 200);
        /// <summary>
        /// 耳朵抓取左右邊緣Roi
        /// </summary>
        private readonly Rect EarLeftRightRoi = new(350, 900, 500, 200);
        #endregion

        #region Apex 表面 ROI (Camera 1)
        private readonly Rect[] Surface1ROIs = new Rect[] {
            new Rect(720, 130, 850, 20),   // 中心左
            new Rect(1570, 130, 780, 20),   // 中心
            new Rect(2350, 130, 400, 20),   // 窗戶
            new Rect(2750, 130, 350, 20),   // 窗戶右
            new Rect(3105, 130, 30, 20),    // 頸縮
            new Rect(3145, 130, 55, 20),    // 尾端
        };
        #endregion

        #region Apex 表面 ROI (Camera 2)
        private readonly Rect[] Surface2ROIs = new Rect[] {
            new Rect(720, 130, 850, 20),   // 中心左
            new Rect(1570, 130, 780, 20),   // 中心
            new Rect(2350, 130, 400, 20),   // 窗戶
            new Rect(2750, 130, 350, 20),   // 窗戶右
            new Rect(3105, 130, 30, 20),    // 頸縮
            new Rect(3145, 130, 55, 20),    // 尾端
        };
        #endregion

        /// <summary>
        /// Apex 對位用 Flag 結構
        /// </summary>
        public struct ApexAngleCorrectionStruct
        {
            public ApexAngleCorrectionStruct(byte direction) : this()
            {
                CorrectionMode = direction;
            }

            /// <summary>
            /// 工件對位步驟旗標
            /// bit 0 ~ 3, 0 ~ 15
            /// </summary>
            public byte Steps { get; set; }             // 1 bit
            /// <summary>
            /// 前一次檢驗之 Width
            /// </summary>
            public ushort LastWindowWidth { get; set; } // 2 bits
            /// <summary>
            /// 最大檢驗之 Width
            /// </summary>
            public ushort MaxWindowWidth { get; set; }  // 2 bits

            /// <summary>
            /// 窗戶 Width 穩定計數
            /// </summary>
            public byte WidthStable { get; set; }       // 1 bit

            /// <summary>
            /// 0.111 孔穩定計數
            /// </summary>
            public byte CircleStable { get; set; }      // 1 bit

            /// <summary>
            /// Otsu 閾值
            /// </summary>
            public byte OtsuThreshlod { get; set; }     // 1 bit

            /// <summary>
            /// 方向 (0: 快正轉, 1: 快逆轉, 2: 慢正轉, 3: 慢逆轉, 4: 低速正轉, 5: 低速逆轉, 6: 正接近, 7: 逆接近, 8: 未定)
            /// </summary>
            public byte CorrectionMode { get; set; }    // 1 bit
        }

        /// <summary>
        /// Apex 瑕疵檢驗用步驟結構
        /// </summary>
        public struct ApexDefectInspectionSteps
        {
            /// <summary>
            /// 窗戶步驟 (單步測試用)
            /// 0b0000(0): 
            /// 0b0001(1): 
            /// 0b0010(2): 
            /// 0b0011(3): 
            /// 0b0100(4): 
            /// 0b0101(5): 
            /// 0b0110(6): 
            /// </summary>
            [Obsolete("單步測試用")]
            public byte WindowSteps { get; set; }   // 1 bit

            /// <summary>
            /// 耳朵檢驗步驟 (單步測試用)
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
            [Obsolete("單步測試用")]
            public byte EarSteps { get; set; }      // 1 bit

            /// <summary>
            /// 耳朵窗戶同時檢驗步驟；
            /// 0b0000(0):
            /// 0b0001(1):
            /// 0b0010(2):
            /// 0b0011(3):
            /// 0b0100(4):
            /// 0b0101(5):
            /// 0b0110(6):
            /// 0b0111(7):
            /// 0b1000(8):
            /// 0b1001(9):
            /// 0b1010(10):
            /// 0b1011(11):
            /// 0b1100(12):
            /// 0b1101(13):
            /// 0b1110(14):
            /// 0b1111(15):
            /// </summary>
            public byte Steps { get; set; }         // 1 bit

            /// <summary>
            /// 表面兩個相機同時檢驗步驟；
            /// 0b0000:
            /// </summary>
            public byte SurfaceSteps { get; set; }  // 1 bit
        }

        /// <summary>
        /// Apex 管件選轉定位用結構旗標
        /// </summary>
        public ApexAngleCorrectionStruct ApexAngleCorrectionFlags = new ApexAngleCorrectionStruct(5);

        /// <summary>
        /// Apex 瑕疵檢驗用步驟旗標
        /// </summary>
        public ApexDefectInspectionSteps ApexDefectInspectionStepsFlags;

        #region 工件角度校正，工件角度校正，工件角度校正，使用ImageGrabbed事件
        /// <summary>
        /// 確認旋轉方向前步驟
        /// </summary>
        public void PreCheckRatationWay()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(160, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(20, 200, 2000, 2000);
            // 啟動定速旋轉
            _ = ServoMotion.Axes[1].TryVelMove(0);
        }

        /// <summary>
        /// 確認旋轉方向，確認Otsu 閾值
        /// </summary>
        /// <param name="cam">目標相機</param>
        public void CheckRatationWay(BaslerCam cam, out byte direction)
        {
            direction = 2;  // 2 未定
            try
            {
                IGrabResult grabResult = null;
                Mat mat = null;

                cam.Camera.ExecuteSoftwareTrigger();
                grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                mat = BaslerFunc.GrabResultToMatMono(grabResult);

                Rect roi = WindowLeftRightRoi;

                if (ApexAngleCorrectionFlags.OtsuThreshlod == 0)
                {
                    Methods.GetRoiOtsu(mat, roi, 0, 255, out _, out byte th);
                    ApexAngleCorrectionFlags.OtsuThreshlod = th;
                }
                byte otsuTh = ApexAngleCorrectionFlags.OtsuThreshlod;   // Otsu 閾值
                                                                        //// 需要開運算除毛邊?
                Methods.GetRoiCanny(mat, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out Mat canny);

                // 這邊第一次要確認方向
                byte dir = ApexAngleCorrectionFlags.CorrectionMode;
                bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width1, 3, 50, 100);

                Debug.WriteLine($"width 1:{width1}");

                SpinWait.SpinUntil(() => false, 100);

                cam.Camera.ExecuteSoftwareTrigger();
                grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                mat = BaslerFunc.GrabResultToMatMono(grabResult);

                Methods.GetRoiCanny(mat, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out canny);

                // 這邊第一次要確認方向
                dir = ApexAngleCorrectionFlags.CorrectionMode;
                FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width2, 3, 50, 100);

                Debug.WriteLine($"width 2:{width2}");

                ServoMotion.Axes[1].StopMove();

                if (width1 == 0 && width2 == 0)
                {
                    direction = 0;      // 快正轉
                }
                else
                {
                    if (width2 > width1)    // 正轉
                    {
                        if (width2 < 200)   // Vel = 200
                        {
                            direction = 0;  // 快正轉
                        }
                        else if (width2 < 300)
                        {
                            direction = 2;  // 慢正轉
                        }
                        else if (width2 < 350)
                        {
                            direction = 4;  // 低速正轉
                        }
                        else
                        {
                            direction = 6;  // 正接近
                        }
                    }
                    else
                    {
                        if (width1 < 200)
                        {
                            direction = 1;  // 快逆轉
                        }
                        else if (width1 < 300)
                        {
                            direction = 3;  // 慢逆傳
                        }
                        else if (width1 < 350)
                        {
                            direction = 5;  // 低速逆轉
                        }
                        else
                        {
                            direction = 7;  // 逆接近
                        }
                    }
                }
            }
            catch (TimeoutException T)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{T.Message}");

                Debug.WriteLine($"T {T.Message} {T.Source}");
            }
            catch (InvalidOperationException I)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{I.Message}");

                Debug.WriteLine($"I {I.Message} {I.Source}");
            }
            catch (Exception E)
            {
                MsgInformer.AddWarning(MsgInformer.Message.MsgCode.CAMERA, $"{E.Message}");

                Debug.WriteLine($"E {E.Message} {E.Source}");
            }
        }

        /// <summary>
        /// 角度校正前手續；
        /// Light 1：160, 0, 128, 128
        /// Light 2：0, 0 
        /// </summary>
        public void PreAngleCorrection()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(160, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            //// 變更馬達速度
            //ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
            //// 啟動馬達
            //uint ret;
            //do
            //{
            //    // ret = ServoMotion.Axes[1].TryPosMove(5000);
            //    ret = ServoMotion.Axes[1].TryVelMove(0);
            //} while (ret != 0);
        }

        /// <summary>
        /// 角度校正變更速度 (低速)
        /// </summary>
        //public void PreAngleCorrectionLowSpeed()
        //{
        //    ServoMotion.Axes[1].ChangeVel(200);
        //    ////ServoMotion.Axes[1].PosMove(5000);
        //}

        /// <summary>
        /// 角度校正, 
        /// 校正後旋轉軸歸零.  
        /// ※需要連續拍攝
        /// </summary>
        /// <param name="src"></param>
        public void AngleCorrection(Mat src)
        {
            Rect roi = new(100, 840, 1000, 240);

            Methods.GetRoiCanny(src, roi, 75, 120, out Mat canny);
            byte dir = 0;
            bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width, 3, 50);

            byte endStep = 0b0110;

            // canny.Dispose();
            if (FindWindow && ApexAngleCorrectionFlags.Steps != endStep)
            {
                Dispatcher.Invoke(() =>
                {
                    Cv2.ImShow("AngleCorrectionCanny", canny);
                });

                switch (ApexAngleCorrectionFlags.Steps)
                {
                    // 這邊應該判斷 width 是增加的
                    case 0b0000:    // 0 // 高速中，初步找窗戶
                        if (width > 200 && width > ApexAngleCorrectionFlags.LastWindowWidth + 5)    // width 增加中
                        {
                            ServoMotion.Axes[1].ChangeVel(200); // 轉低速
                            ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                            ApexAngleCorrectionFlags.Steps += 0b01;
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    case 0b0001:    // 1 // 低速中，慢速找窗戶
                        if (width > 300 && width > ApexAngleCorrectionFlags.LastWindowWidth + 3)
                        {
                            ServoMotion.Axes[1].ChangeVel(50);  // 轉極低速
                            ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                            ApexAngleCorrectionFlags.Steps += 0b01;
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    case 0b0010:    // 2 // 極低速，定位窗戶
                        if (width > 350)
                        {
                            ServoMotion.Axes[1].StopMove();

                            ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                            ApexAngleCorrectionFlags.Steps += 0b01;
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    case 0b0011:    // 3
                        if (width > ApexAngleCorrectionFlags.LastWindowWidth)   // width 增加中
                        {
                            _ = ServoMotion.Axes[1].TryPosMove(5);
                        }
                        else
                        {
                            ApexAngleCorrectionFlags.CorrectionMode ^= 0b01; // 逆轉

                            // 轉過頭
                            ApexAngleCorrectionFlags.MaxWindowWidth = ApexAngleCorrectionFlags.LastWindowWidth;
                            ApexAngleCorrectionFlags.Steps += 0b01;
                            //ApexAngleCorrectionFlags.Steps = 0b0110;
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    case 0b0100:    // 4 
                        if (width < ApexAngleCorrectionFlags.MaxWindowWidth)    // 當前 width < 最大 width
                        {
                            if (ApexAngleCorrectionFlags.CorrectionMode == 1)
                            {
                                // 正轉 3 pulse
                                _ = ServoMotion.Axes[1].TryPosMove(1);
                            }
                            else
                            {
                                // 逆轉 -3 pulse
                                _ = ServoMotion.Axes[1].TryPosMove(-1);
                            }
                        }
                        else
                        {
                            ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                            ApexAngleCorrectionFlags.Steps = 0b0110;
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    case 0b0101:    // 5
                        if (width < ApexAngleCorrectionFlags.MaxWindowWidth) // 
                        {
                            if (width < ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                ApexAngleCorrectionFlags.CorrectionMode ^= 0b01;
                                ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                                break;
                            }

                            if (ApexAngleCorrectionFlags.CorrectionMode == 1)
                            {
                                // 正轉 1 pulse
                                _ = ServoMotion.Axes[1].TryPosMove(1);
                            }
                            else
                            {
                                // 逆轉 - pulse
                                _ = ServoMotion.Axes[1].TryPosMove(-1);
                            }
                        }
                        else
                        {
                            ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;

                            // 重置 POS
                            if (ServoMotion.Axes[1].TryResetPos() == (uint)Advantech.Motion.ErrorCode.SUCCESS)
                            {
                                ApexAngleCorrectionFlags.Steps += 0b01;

                                Cv2.DestroyAllWindows();
                            }
                        }
                        ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                        break;
                    default:
                        break;
                }
            }

            if (ApexAngleCorrectionFlags.Steps != endStep + 1)   // endStep + 1 // 之後刪除 + 1
            {
                Debug.WriteLine($"Steps: {ApexAngleCorrectionFlags.Steps}");
                Debug.WriteLine($"Width: {width}");
                Debug.WriteLine($"Last Width: {ApexAngleCorrectionFlags.LastWindowWidth}");
                Debug.WriteLine($"Max Width: {ApexAngleCorrectionFlags.MaxWindowWidth}");
                Debug.WriteLine($"Direction: {ApexAngleCorrectionFlags.CorrectionMode}");
            }
        }

        /// <summary>
        /// 角度校正，
        /// 相機(窗戶) 1 粗定位，粗定位時不使用相機 2
        /// 相機(耳朵) 2 精定位，精定位時不使用相機 1
        /// </summary>
        /// <param name="src">相機 1 影像</param>
        /// <param name="src2">相機 2 影像</param>
        public void AngleCorrection(Mat src, Mat src2)
        {
            if (src != null && !src.Empty())
            {
                // X: 1200 - 100, Y: 960 - 120
                //Rect roi = new(100, 840, 1000, 240);
                Rect roi = WindowLeftRightRoi;

                //if (ApexAngleCorrectionFlags.OtsuThreshlod == 0)
                //{
                //    Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte th);
                //    ApexAngleCorrectionFlags.OtsuThreshlod = th;
                //}
                byte otsuTh = ApexAngleCorrectionFlags.OtsuThreshlod;   // Otsu 閾值
                // 需要開運算除毛邊?

                Methods.GetRoiCanny(src, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out Mat canny);

                // 這邊第一次要確認方向
                //byte dir = ApexAngleCorrectionFlags.CorrectionMode;
                bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width, 3, 50, 100);

                //Debug.WriteLine($"Dir: {dir}");

                if (FindWindow && ApexAngleCorrectionFlags.Steps != 0b0110) // end step: 6
                {
                    //ApexAngleCorrectionFlags.CorrectionMode = dir;   // 有找到窗再變更
                    // Cv2.ImShow("Canny", new Mat(src, roi));
                    Cv2.ImShow("ApexCorrectionCanny", canny);
                    Cv2.MoveWindow("ApexCorrectionCanny", 20, 500);

                    //Debug.WriteLine($"Dir: {dir} step: {ApexAngleCorrectionFlags.Steps}");

                    switch (ApexAngleCorrectionFlags.Steps)
                    {
                        case 0b0000:    // 0 // 確認方向
                            #region 0b0000 // 0 // 確認方向是否有誤
                            if (ApexAngleCorrectionFlags.LastWindowWidth > 0)
                            {
                                if (width > ApexAngleCorrectionFlags.LastWindowWidth)
                                {
                                    ApexAngleCorrectionFlags.CorrectionMode = 1; // 正轉
                                    ApexAngleCorrectionFlags.Steps += 0b01;
                                }
                                else
                                {
                                    ApexAngleCorrectionFlags.CorrectionMode = 0; // 逆轉
                                    ServoMotion.Axes[1].StopEmg();
                                    ServoMotion.Axes[1].VelMove(1);
                                    // ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                    ApexAngleCorrectionFlags.Steps += 0b01;
                                }
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0001:    // 1 // 快速找窗戶
                            #region 0b0001 // 1 // 快速找窗戶
                            if (width > 200 && width > ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                ServoMotion.Axes[1].ChangeVel(200);
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0010:    // 2 // 慢速找窗戶
                            #region 0b0010 // 2 // 慢速找窗戶
                            if (width > 300 && width > ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                ServoMotion.Axes[1].ChangeVel(50);
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0011:    // 3 // 極慢速找窗戶
                            #region 0b0011 // 3 // 極慢速找窗戶
                            if (width > 350)
                            {
                                ServoMotion.Axes[1].StopMove();

                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0100:    // 4 // 每次 +5 pulse
                            #region 0b0100 // 4 // 每次 +5 pulse
                            if (width >= ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                if ((ApexAngleCorrectionFlags.CorrectionMode & 0b0001) == 0b0000)   // 正向
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(5);
                                }
                                else if ((ApexAngleCorrectionFlags.CorrectionMode & 0b0001) == 0b0001)  // 逆向
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(-5);
                                }
                                //_ = ServoMotion.Axes[1].TryPosMove(5);
                            }
                            else
                            {
                                // 轉過頭
                                ApexAngleCorrectionFlags.MaxWindowWidth = ApexAngleCorrectionFlags.LastWindowWidth;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0101:    // 5 // 
                            #region 0b0101 // 5 // 粗定位
                            if (width < ApexAngleCorrectionFlags.MaxWindowWidth)
                            {
                                if (width > ApexAngleCorrectionFlags.LastWindowWidth)
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(-1);
                                }
                                else if (width < ApexAngleCorrectionFlags.LastWindowWidth)
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(1);
                                }
                                else
                                {
                                    ApexAngleCorrectionFlags.WidthStable += 0b01;

                                    if (ApexAngleCorrectionFlags.WidthStable > 5)
                                    {
                                        ApexAngleCorrectionFlags.WidthStable = 0;
                                        ApexAngleCorrectionFlags.Steps += 0b01;

                                        Cv2.DestroyWindow("ApexCorrectionCanny");
                                    }
                                }
                            }
                            else
                            {
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                // 到此粗定位結束
                                Cv2.DestroyWindow("ApexCorrectionCanny");
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        default:
                            break;
                    }
                }
                //else
                //{
                //    // 判斷為 確認方向的步驟中
                //    if (ApexAngleCorrectionFlags.Steps == 0b0000)
                //    {
                //        // 找不到窗戶，直接正轉
                //        ApexAngleCorrectionFlags.Direction = 1;
                //        // 啟動馬達
                //        ServoMotion.Axes[1].PosMove(5000);
                //        // 開始找窗戶.
                //        ApexAngleCorrectionFlags.Steps = 0b0001;
                //    }
                //}
                Debug.WriteLine($"Width: {width} Last: {ApexAngleCorrectionFlags.LastWindowWidth} Max: {ApexAngleCorrectionFlags.MaxWindowWidth}");
            }

            if (src2 != null && !src2.Empty())
            {
                // X: 600 - 90, Y: 960 - 90
                Rect roi = new(510, 860, 180, 180);

                int l = 0; // 孔左輪廓 count
                int r = 0; // 孔右輪廓 count

                // 銳化垂直
                Methods.GetRoiVerticalFilter2D(src2, roi, 1.5, -0.5, out Mat filter);
                Methods.GetCanny(filter, 75, 150, out Mat canny);

                // 找輪廓
                Cv2.FindContours(canny, out Point[][] cons, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                // 過濾過短輪廓
                cons = cons.Where(cons => cons.Length > 50).ToArray();
                int maxLength = cons.Max(c => c.Length);

                // 找全輪廓外接圓
                Point[] concat = cons.SelectMany(con => con.ToArray()).ToArray();
                Cv2.MinEnclosingCircle(concat, out Point2f c, out float rad);

                Moments[] moments = new Moments[cons.Length];
                Point2f[] centers = new Point2f[cons.Length];

                // 轉彩色
                Cv2.CvtColor(filter, filter, ColorConversionCodes.GRAY2BGR);

                // 計算質心
                for (int i = 0; i < cons.Length; i++)
                {
                    moments[i] = Cv2.Moments(cons[i]);

                    #region 可以刪
                    centers[i] = new Point2f((float)(moments[i].M10 / moments[i].M00), (float)(moments[i].M01 / moments[i].M00));
                    Cv2.Circle(filter, (int)centers[i].X, (int)centers[i].Y, 5, Scalar.Red, 2);
                    #endregion
                    Cv2.DrawContours(src2, cons, i, Scalar.Black, 2);

                    if (cons[i].Length == maxLength) continue;

                    double cX = moments[i].M10 / moments[i].M00;
                    if (cX < c.X && Math.Abs(cX - c.X) > 10)
                    {
                        l++;
                    }
                    else if (cX > c.X && Math.Abs(cX - c.X) > 10)
                    {
                        r++;
                    }
                }

                Cv2.Circle(filter, (int)c.X, (int)c.Y, 5, Scalar.Green, 2);
                Cv2.Circle(filter, (int)c.X, (int)c.Y, (int)rad, Scalar.Cyan, 2);

                #region 可刪
                Cv2.ImShow("Ear filter", filter);
                Cv2.ImShow("Ear Canny", canny);

                //Mat small = new();
                //Cv2.Resize(src2, small, new OpenCvSharp.Size(src2.Width / 2, src2.Height / 2));

                //Cv2.ImShow("src2", small);
                Cv2.ImShow($"ZOOM", new Mat(src2, roi));

                Cv2.MoveWindow("Ear filter", 20, 20);
                Cv2.MoveWindow("Ear Canny", 20 + roi.Width, 20);
                Cv2.MoveWindow("ZOOM", 20 + roi.Width * 2, 20);
                #endregion

                Debug.WriteLine($"L: {l}; R: {r}, {c}");
                if (ApexAngleCorrectionFlags.Steps >= 6)
                {
                    switch (ApexAngleCorrectionFlags.Steps)
                    {
                        case 0b0110:    // 6 // 判斷 L = R 
                            #region 0b0110 // 6 // 判斷 L = R
                            if (l > r)
                            {
                                _ = ServoMotion.Axes[1].TryPosMove(-1);
                            }
                            else if (l < r)
                            {
                                _ = ServoMotion.Axes[1].TryPosMove(1);
                            }
                            else
                            {
                                if (ApexAngleCorrectionFlags.CircleStable++ > 3)
                                {
                                    ApexAngleCorrectionFlags.CircleStable = 0;
                                    ApexAngleCorrectionFlags.Steps += 0b01;
                                    break;
                                }
                            }
                            #endregion
                            break;
                        case 0b0111:    // 7
                                        //if (l + r == 1) // 剩一個大輪廓
                                        //{
                                        //    if (ApexAngleCorrectionFlags.CircleStable++ > 10)
                                        //    {
                                        //        ApexAngleCorrectionFlags.CircleStable = 0;
                                        //        ApexAngleCorrectionFlags.Steps += 0b01;
                                        //        break;
                                        //    }
                                        //}
                            if (l > r)
                            {
                                _ = ServoMotion.Axes[1].TryPosMove(-1);
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            else if (l < r)
                            {
                                _ = ServoMotion.Axes[1].TryPosMove(1);
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            else
                            {
                                //if (ApexAngleCorrectionFlags.CircleStable++ > 3)
                                //{
                                //    ApexAngleCorrectionFlags.CircleStable = 0;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                break;
                            }
                            // 到此精定位結束
                            break;
                        case 0b1000:    // 8
                                        // 重置 POS
                            if (ServoMotion.Axes[1].TryResetPos() == (uint)Advantech.Motion.ErrorCode.SUCCESS)
                            {
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                // Cv2.DestroyAllWindows();

                                Cv2.DestroyWindow("ZOOM");
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            Debug.WriteLine($"Steps: {ApexAngleCorrectionFlags.Steps}");
        }

        /// <summary>
        /// 角度校正後手續，變更旋轉速度
        /// </summary>
        public void PostAngleCorrection()
        {
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(20, 500, 5000, 5000);
        }
        #endregion

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
                        case 0b0000:    // 0 // 0.111 孔毛邊
                            #region 0b0000 // 0 // 0.111 孔毛邊
                            PreEarHoleInspection();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            // 擷取
                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            EarHoleInspection(mat2, out holeC, out holeR);
                            // holeMask = new Mat(); //holeC = holeC.Add(EarHoleRoi.Location);

                            // holeMask = new Mat(EarHoleRoi.Height, EarHoleRoi.Width, MatType.CV_8UC1, Scalar.Black);
                            // Cv2.Circle(holeMask, (int)holeC.X, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            // Debug.WriteLine($"hole center: {holeC}, hole r: {holeR}");

                            // Cv2.ImShow("holeMask", holeMask);

                            ApexDefectInspectionStepsFlags.Steps += 0b01;
                            #endregion
                            break;
                        case 0b0001:    // 1 // 窗戶 & 耳朵(L) ROI
                            #region 0b0001 // 1 // 窗戶 & 耳朵(L) ROI
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
                        case 0b0010:    // 2 // 窗戶 & 耳朵(L) ROI
                            #region 0b0010 // 2 // 窗戶 & 耳朵(L) ROI
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
                        case 0b0011:    // 3 // 窗戶 & 耳朵(L) 檢測
                            #region 0b0011 // 3 // 窗戶 & 耳朵(L) 檢測
                            PreEarWindowIns();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam1.Camera.ExecuteSoftwareTrigger();
                            cam2.Camera.ExecuteSoftwareTrigger();

                            grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);

                            mat1 = BaslerFunc.GrabResultToMatMono(grabResult1);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 窗戶檢測
                            WindowInspection(mat1, xArray, winRoiL, winRoiR);
                            // 耳朵檢測
                            EarInspectionL(mat2, earRoiL, earRoiR);

                            #region 孔周圍
                            //Rect holeRoiL = new(earRoiL.Right, 850, earRoiR.Right - earRoiL.Right, 200);
                            //Mat holeMatL = new(mat2, holeRoiL);   // 這個要清除

                            //holeMask = new Mat(holeMatL.Height, holeMatL.Width, MatType.CV_8UC1, Scalar.Black);
                            //Cv2.Circle(holeMask, (holeMask.Width / 2) + 10, (int)holeC.Y, (int)holeR, Scalar.White, -1);
                            //// 耳朵孔檢測
                            //EarHoleInsL(holeMatL, holeMask);
                            //holeMatL.Dispose();
                            #endregion

                            #region 畫窗戶 ROI
#if false
                            Cv2.Rectangle(mat1, winRoiL, Scalar.Black, 2);
                            Cv2.Rectangle(mat1, winRoiR, Scalar.Black, 2);
                            Cv2.Resize(mat1, mat1, new OpenCvSharp.Size(mat1.Width / 2, mat1.Height / 2));
                            Cv2.ImShow("mat1", mat1.Clone());
                            Cv2.MoveWindow("mat1", 600, 20); 
#endif
                            #endregion

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
                        case 0b0100:    // 4 // 窗戶 側光檢測
                            #region 0b0100 // 4 // 窗戶 側光檢測
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
                        case 0b0101:    // 5 // 窗戶 側光檢測 2
                            #region 0b0101 // 5 // 窗戶 側光檢測 2
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
                        case 0b0110:    // 6
                            #region 0b0110 // 6 // // 耳朵 (L) 側光檢測
                            PreEarInsSide();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測 (側光)
                            EarInspectionL(mat2, earRoiL, earRoiR);


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
                        case 0b0111:    // 7
                            #region 0b0111 // 7 // 窗戶 & 耳朵(L) ROI
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
                        case 0b1000:    // 8 // 耳朵 (R) 檢測
                            #region 0b1000 // 8 // 耳朵 (R) 檢測
                            PreEarWindowIns();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測
                            EarInspectionR(mat2, earRoiL, earRoiR);

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
                        case 0b1001:    // 9 // 耳朵 (R) 側光檢測
                            #region 0b1001 // 9 // 耳朵 (R) 側光檢測
                            PreEarInsSide();
                            _ = SpinWait.SpinUntil(() => false, 50);

                            cam2.Camera.ExecuteSoftwareTrigger();
                            grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                            mat2 = BaslerFunc.GrabResultToMatMono(grabResult2);

                            // 耳朵檢測 (側光)
                            EarInspectionR(mat2, earRoiL, earRoiR);

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

                    // cam1.Camera.ExecuteSoftwareTrigger();
                    // cam2.Camera.ExecuteSoftwareTrigger();

                    // using IGrabResult grabResult1 = cam1.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                    // using IGrabResult grabResult2 = cam2.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);

                    // if (grabResult1.GrabSucceeded)
                    // {
                    // }

                    // if (grabResult2.GrabSucceeded)
                    // {
                    // }

                    #region Dispose 物件，釋放記憶體
                    mat1?.Dispose();
                    mat2?.Dispose();
                    grabResult1?.Dispose();
                    grabResult2?.Dispose();
                    #endregion
                }

                PostWindowInspection();
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
                    GetEarInspectionRoiL(src2, out eRoiL, out eRoiR);
                }
                else if (direction == 1)
                {
                    GetEarInspectionRoiR(src2, out eRoiL, out eRoiR);
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
                int msDelay = 75;

                Rect roiL = new();
                Rect roiR = new();

                double top = 0;
                double bottom = 0;
                double[] xPos = Array.Empty<double>();
                double[] xPos2 = Array.Empty<double>();
                double[] xPos3 = Array.Empty<double>();
                double[] xArray = Array.Empty<double>();

                int Loop = 0;
                Debug.WriteLine($"Start: {DateTime.Now:ss.fff}");

                Cv2.DestroyAllWindows();
                while (ApexDefectInspectionStepsFlags.WindowSteps != 0b10000)
                {
                    Debug.WriteLine($"Loop: {Loop} Steps: {ApexDefectInspectionStepsFlags.WindowSteps}");
                    if (Loop++ >= 16)
                    {
                        break;
                    }

                    switch (ApexDefectInspectionStepsFlags.WindowSteps)
                    {
                        case 0b0000:    // 0
                            await PreWindowInspectionRoi();
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0010:    // 2
                            PreWindowInspectionRoi2();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0100:    // 4
                            PreWindowInspectionRoi3();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b0110:    // 6
                            PreWindowInspection();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1000:    // 8
                            PreWindowInspection2();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1010:    // 10
                            PreWindowInspection3();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1100:    // 12 // 開啟側光
                            PreWindowInsSide();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        case 0b1110:    // 14 // 開啟側光
                            PreWindowInsSide2();
                            _ = SpinWait.SpinUntil(() => false, msDelay);
                            ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                            continue;
                        default:
                            break;
                    }

                    cam.Camera.ExecuteSoftwareTrigger();

                    using IGrabResult grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                    Debug.WriteLine($"Frames: {grabResult.ImageNumber}");

                    if (grabResult.GrabSucceeded)
                    {
                        Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);   // 轉 MatMono 

                        switch (ApexDefectInspectionStepsFlags.WindowSteps)
                        {
                            case 0b0001:    // 1 ROI
                                GetWindowInspectionTopBottomEdge(mat, out top, out bottom);
                                GetWindowInspectionRoi(mat, out xPos, out roiL, out roiR);

                                if (xPos.Length == 7)    // 有找到 7 個分界點
                                {
                                    xArray = xPos;
                                    xPos = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                                }

                                break;
                            case 0b0011:    // 3 ROI
                                // MainWindow.WindowInspectionTopBottomLimit(mat, out yPos);
                                GetWindowInspectionRoi(mat, out xPos2, out roiL, out roiR);

                                if (xPos2.Length == 7)  // 有找到 7 個分界點
                                {
                                    xArray = xPos2;
                                    xPos2 = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    ApexDefectInspectionStepsFlags.WindowSteps += 0b01;
                                }
                                break;
                            case 0b0101:    // 5 ROI

                                GetWindowInspectionRoi(mat, out xPos3, out roiL, out roiR);

                                if (xPos3.Length == 7)   // 有找到 7 個分界點
                                {
                                    xArray = xPos3;
                                    xPos3 = null;
                                    roiL.Y = roiR.Y = (int)top;
                                    roiL.Height = roiR.Height = (int)(bottom - top);
                                    ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                else
                                {
                                    // 如果取三次ROI還失敗，這邊合併並抽取 (30 pixel 間隔)
                                    xArray = xPos.Concat(xPos2).Concat(xPos3).OrderBy(x => x).ToArray();

                                    List<double> xList = new();
                                    for (int i = 0; i < xArray.Length; i++)
                                    {
                                        if (i == 0 || xArray[i - 1] + 30 < xArray[i])
                                        {
                                            xList.Add(xArray[i]);
                                        }
                                    }
                                    xArray = xList.ToArray();
                                    xList.Clear();

                                    roiL = new Rect((int)xArray[1] - 20, (int)top, (int)(xArray[2] - xArray[1]) + 40, (int)(bottom - top));
                                    roiR = new Rect((int)xArray[^3] - 20, (int)top, (int)(xArray[^2] - xArray[^3]) + 40, (int)(bottom - top));

                                    xPos = xPos2 = xPos3 = null;
                                    ApexDefectInspectionStepsFlags.WindowSteps = 0b0110;
                                }
                                break;
                            case 0b0111:    // 7
                                WindowInspection(mat, xArray, roiL, roiR);
                                ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m1 = new();
                                Cv2.Resize(mat, m1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat1", m1);
                                Cv2.MoveWindow("mat1", 0, 0);
                                #endregion
                                break;
                            case 0b1001:    // 9
                                WindowInspection(mat, xArray, roiL, roiR);
                                ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m2 = new();
                                Cv2.Resize(mat, m2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat2", m2);
                                Cv2.MoveWindow("mat2", 600, 0);
                                #endregion
                                break;
                            case 0b1011:    // 11
                                WindowInspection(mat, xArray, roiL, roiR);
                                ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat m3 = new();

                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Cv2.Resize(mat, m3, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("mat3", m3);
                                Cv2.MoveWindow("mat3", 1200, 0);
                                #endregion
                                break;
                            case 0b1101:    // 13 // 側光
                                // 1 - 2: R 角, 2 - 4: 窗戶邊緣, ^3 - ^2: R 角
                                Rect roiTop = new((int)xArray[1], (int)(top - 80), (int)(xArray[^2] - xArray[1]), 120);
                                WindowInspectionSideLight(mat, roiTop);
                                ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat otsu1 = new();

                                Cv2.Rectangle(mat, roiTop, Scalar.Gray, 2);
                                Cv2.Resize(mat, otsu1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("Otsu1", otsu1);
                                Cv2.MoveWindow("Otsu1", 300, 0);
                                #endregion
                                break;
                            case 0b1111:    // 15 // 側光
                                // 1 - 2: R 角, 2 - 4: 窗戶邊緣, ^3 - ^2: R 角
                                Rect roiBot = new((int)xArray[1], (int)bottom - 40, (int)(xArray[^2] - xArray[1]), 120);
                                WindowInspectionSideLight(mat, roiBot);
                                ApexDefectInspectionStepsFlags.WindowSteps += 0b01;

                                #region 待刪除
                                Mat otsu2 = new();

                                Cv2.Rectangle(mat, roiBot, Scalar.Gray, 2);
                                Cv2.Resize(mat, otsu2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("Otsu2", otsu2);
                                Cv2.MoveWindow("Otsu2", 900, 0);
                                #endregion
                                break;
                            default:
                                break;
                        }
                        ImageSource = mat.ToImageSource();
                    }
                }
                PostWindowInspection();

                Debug.WriteLine($"Stop: {DateTime.Now:ss.fff}");
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
        /// 取得窗戶瑕疵 ROI 前手續；
        /// Light 1：320, 0, 160, X；
        /// Light 2：0, 0
        /// </summary>
        public async Task PreWindowInspectionRoi()
        {
            // 變更光源
            LightCtrls[0].SetAllChannelValue(320, 0, 160, 0);
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(80, 800, 5000, 5000);
            // 觸發馬達
            await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得窗戶瑕疵 ROI 前手續 2；
        /// Light 1：288, 0, 144, 0；
        /// Light 2：0, 0
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
        /// 取得窗戶瑕疵 ROI 前手續 3；
        /// Light 1：256, 0, 128, 0；
        /// Light 2：0, 0
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
        public void GetWindowInspectionTopBottomEdge(Mat src, out double top, out double bottom)
        {
            //Rect roi = new(450, 240, 250, 1400);
            Rect roi = WindowTopBotEdgeRoi;

            Methods.GetRoiOtsu(src, roi, 0, 255, out Mat otsu, out byte threshold);
            // 埢積核
            Mat morele = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 3), new Point(-1, -1));
            // 閉運算
            Cv2.MorphologyEx(otsu, otsu, MorphTypes.Close, morele, null, 2);

            //Methods.GetRoiCanny(src, roi, 60, 120, out Mat canny);
            Methods.GetCanny(otsu, (byte)(threshold - 20), (byte)(threshold * 1.8), out Mat canny);
            //otsu.Dispose();
            // Cv2.Resize(canny, canny, new OpenCvSharp.Size(canny.Width / 2, canny.Height / 2));

            //Mat otsu2 = new Mat();
            //Mat canny2 = new Mat();
            //Cv2.Resize(otsu, otsu2, new OpenCvSharp.Size(otsu.Width / 2, otsu.Height / 2));
            //Cv2.Resize(canny, canny2, new OpenCvSharp.Size(canny.Width / 2, canny.Height / 2));
            //Cv2.ImShow("top bottom otsu", otsu2);
            //Cv2.ImShow("top bottom canny", canny2);

            // Cv2.ImShow("src", new Mat(src, roi));
            // Cv2.ImShow("TopBottomEdge", canny);
            Methods.GetHoughWindowYPos(canny, roi.Y, out top, out bottom, 10, 40);
        }

        /// <summary>
        /// 取得窗戶瑕疵檢驗 ROI
        /// </summary>
        public void GetWindowInspectionRoi(Mat src, out double[] xPos, out Rect roiL, out Rect roiR)
        {
            //Rect roi = new(100, 840, 1000, 240);
            Rect roi = WindowLeftRightRoi;

            Methods.GetRoiVerticalFilter2D(src, roi, 1.8, -0.6, out Mat filter);
            Methods.GetCanny(filter, 60, 100, out Mat canny);
            //Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
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
                roiL = new Rect((int)xPos[1] - 2, 240, (int)(xPos[2] - xPos[1]) + 4, 1400);
                roiR = new Rect((int)xPos[^3] - 2, 240, (int)(xPos[^2] - xPos[^3] + 4), 1400);
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

        /// <summary>
        /// 窗戶瑕疵檢驗，
        /// 測試是否拆步驟 (先取 ROI 再瑕疵檢)
        /// </summary>
        /// <param name="src"></param>
        public bool WindowInspection(Mat src, double[] xPos, Rect roiL, Rect roiR)
        {
            if (xPos.Length < 7) return false;

            // Mat matL = new(src, roiL);
            // Mat matR = new(src, roiR);

            #region 前處理
            Methods.GetRoiGaussianBlur(src, roiL, new OpenCvSharp.Size(3, 3), 0.808, 0, out Mat tempL);
            Methods.GetRoiGaussianBlur(src, roiR, new OpenCvSharp.Size(3, 3), 0.808, 0, out Mat tempR);

            Methods.GetVerticalFilter2D(tempL, 1.35, -0.45, out Mat matL);
            Methods.GetVerticalFilter2D(tempR, 1.35, -0.45, out Mat matR);

            Methods.GetOtsu(matL, 0, 255, out _, out byte th1);
            Methods.GetOtsu(matR, 0, 255, out _, out byte th2);

            Debug.WriteLine($"th : {th1}; th2 : {th2}");

            tempL.Dispose();
            tempR.Dispose();
            #endregion

            // Cv2.GaussianBlur(matL, matL, new OpenCvSharp.Size(3, 3), 5, 0);
            // Cv2.GaussianBlur(matR, matR, new OpenCvSharp.Size(3, 3), 5, 0);

            // Cv2.ImShow("matL", matL);
            // Cv2.ImShow("matR", matR);

            #region 取得窗戶 canny
            Methods.GetCanny(matL, 75, 150, out Mat lcw1);
            Methods.GetCanny(matL, 60, 120, out Mat lcw2);
            //Methods.GetCanny(matL, 50, 100, out Mat lcw3);  // 保留

            Methods.GetCanny(matR, 75, 150, out Mat rcw1);
            Methods.GetCanny(matR, 60, 120, out Mat rcw2);
            //Methods.GetCanny(matR, 50, 100, out Mat rcw3);  // 保留

            matL.Dispose();
            matR.Dispose();
            #endregion

            // 尋找輪廓
            Cv2.FindContours(lcw1 & lcw2, out Point[][] DiffConsL, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiL.Location);
            // 尋找輪廓
            Cv2.FindContours(rcw1 & rcw2, out Point[][] DiffConsR, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, roiR.Location);
            lcw1.Dispose();
            lcw2.Dispose();
            rcw1.Dispose();
            rcw2.Dispose();

            // Cv2.ImShow("LLL", lcw3 - lcw1 - lcw2);
            // Cv2.ImShow("RRR", rcw3 - rcw1 - rcw2);
            // Debug.WriteLine($"{DiffConsL.Length} {DiffConsR.Length}");

            int[] cc = DiffConsR.Select(cons => cons.Length).ToArray();

            // X 範圍 ROI - 6
            Point[] FilterL = DiffConsL.Where(c => 20 < c.Length && c.Length < 1200).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
            {
                return xPos[1] + 3 < pt.X && pt.X < xPos[2] - 3;
            }).ToArray();

            // X 範圍 ROI - 6 
            Point[] FilterR = DiffConsR.Where(c => 20 < c.Length && c.Length < 1200).Aggregate(Array.Empty<Point>(), (acc, c) => acc.Concat(c).ToArray()).Where(pt =>
            {
                return xPos[^3] + 3 < pt.X && pt.X < xPos[^2] - 3;
            }).ToArray();


            Scalar scalar = Scalar.Green;
            if (FilterL.Length > 100 || FilterR.Length > 100)
            {
                Debug.WriteLine($"L: {Cv2.ArcLength(FilterL, false)} {Cv2.ContourArea(FilterL)}");
                Debug.WriteLine($"R: {Cv2.ArcLength(FilterR, false)} {Cv2.ContourArea(FilterR)}");

                Cv2.CvtColor(src, src, ColorConversionCodes.GRAY2BGR);
                scalar = Scalar.Red;
            }

            #region Draw circles
            for (int i = 0; i < FilterL.Length; i++)
            {
                Cv2.Circle(src, FilterL[i], 5, scalar, 2);
                //Cv2.Circle(ConMatL, FilterL[i].Subtract(roiL.Location), 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatL, FilterL[i], 5, Scalar.Gray, 1);
            }

            Debug.WriteLine($"Left Con Length: {FilterL.Length}");

            //Cv2.Resize(ConMatL, ConMatL, new OpenCvSharp.Size(roiL.Width / 2, roiL.Height / 2));
            //Cv2.ImShow("Left Con Mat", ConMatL);
            //Cv2.MoveWindow("Left Con Mat", 0, 0);

            for (int i = 0; i < FilterR.Length; i++)
            {
                Cv2.Circle(src, FilterR[i], 5, scalar, 2);
                //Cv2.Circle(ConMatR, FilterR[i].Subtract(roiR.Location), 5, Scalar.Black, 2);
                //Cv2.Circle(ConMatL, FilterL[i], 5, Scalar.Gray, 1);
            }

            Debug.WriteLine($"Right Con Length: {FilterR.Length}");

            //Cv2.Resize(ConMatR, ConMatR, new OpenCvSharp.Size(roiR.Width / 2, roiR.Height / 2));
            //Cv2.ImShow("Right Con Mat", ConMatR);
            //Cv2.MoveWindow("Right Con Mat", 300, 0);
            #endregion

            if (FilterL.Length < 100 && FilterR.Length < 100)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)前手續，
        /// 0, 256
        /// </summary>
        public void PreWindowInsSide()
        {
            // 光源值待定 
            LightCtrls[0].SetAllChannelValue(0, 0, 0, 0);
            LightCtrls[1].SetAllChannelValue(0, 320);
        }

        /// <summary>
        /// 窗戶瑕疵檢測(側光)前手續，
        /// 128, 0
        /// </summary>
        public void PreWindowInsSide2()
        {
            // 光源值待定 
            LightCtrls[0].SetAllChannelValue(0, 0, 0, 0);
            LightCtrls[1].SetAllChannelValue(160, 0);
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
            // Methods.GetRoiHorizonalFilter2D(src, roi, 2.7, -0.3, out Mat filter);
            // Methods.GetOtsu(filter, 0, 255, out Mat otsu, out byte threshold);

            // 
            // 是否增加閉運算?
            // 
            Debug.WriteLine($"Window Side Light Otsu Threshhold : {threshold}");

            // Cv2.ImShow("SideLightBlur", blur);
            // Cv2.ImShow("SideLightOtsu", otsu);

            if (threshold > 40)
            {
                // 閾值過大，代表有瑕疵造成反射
                Methods.GetCanny(otsu, (byte)(threshold - 20), (byte)(threshold * 1.8), out Mat canny);
                // Cv2.ImShow("Otsu Canny", canny);

                Cv2.CvtColor(src, src, ColorConversionCodes.GRAY2BGR);
                Cv2.FindContours(canny, out Point[][] cons, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple, roi.Location);
                for (int i = 0; i < cons.Length; i++)
                {
                    Cv2.MinEnclosingCircle(cons[i], out Point2f c, out float r);
                    Cv2.Circle(src, (int)c.X, (int)c.Y, (int)r, Scalar.Red, 2);
                }
                return false;
            }
            else
            {
                // otsu.Dispose();
                return true;
            }
        }

        /// <summary>
        /// 窗戶檢測完畢，關閉所有光源
        /// </summary>
        private void PostWindowInspection()
        {
            // 變更光源 1
            LightCtrls[0].ResetAllChannel();
            // 變更光源 2
            LightCtrls[1].ResetAllChannel();
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

                byte endStep = 0b1110;

                // 關閉所有視窗
                Cv2.DestroyAllWindows();
                while (ApexDefectInspectionStepsFlags.EarSteps != endStep)   // 14
                {
                    Debug.WriteLine($"Count: {count} Steps: {ApexDefectInspectionStepsFlags.EarSteps}");
                    if (count++ >= endStep)
                    {
                        break;
                    }

                    switch (ApexDefectInspectionStepsFlags.EarSteps)
                    {
                        case 0b000:     // 0
                            PreEarHoleInspection();
                            SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0010:    // 2
                            await PreEarInspectionRoiL();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0100:    // 4
                            PreEarIns();
                            SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b0110:    // 6
                            PreEarInsSide();
                            SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b1000:    // 8
                            await PreEarInspectionRoiR();
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b1010:    // 10
                            PreEarIns();
                            SpinWait.SpinUntil(() => false, 100);
                            ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                            continue;
                        case 0b1100:    // 12
                            PreEarInsSide();
                            SpinWait.SpinUntil(() => false, 100);
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
                            case 0b0001:
                                EarHoleInspection(mat, out _, out _);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            case 0b0011:    // 3 // Find ROI
                                GetEarInspectionRoiL(mat, out roiL, out roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                #region 待刪
                                //Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                //Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                //Mat m1 = new();
                                //Cv2.Resize(mat, m1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                //Cv2.ImShow("ROIs", m1);
                                //Cv2.MoveWindow("ROIs", 0, 0);
                                #endregion
                                break;
                            case 0b0101:    // 5 // 瑕疵
                                EarInspectionL(mat, roiL, roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                Debug.WriteLine($"{roiL}");
                                Debug.WriteLine($"{roiR}");

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m1 = new();
                                Cv2.Resize(mat, m1, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("m1", m1);
                                Cv2.MoveWindow("m1", 0, 0);
                                #endregion
                                break;
                            case 0b0111:    // 7 // 瑕疵 (側光)
                                EarInspectionL(mat, roiL, roiR);

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m11 = new();
                                Cv2.Resize(mat, m11, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("m11", m11);
                                Cv2.MoveWindow("m11", 450, 0);
                                #endregion
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;
                                break;
                            case 0b1001:    // 9 // ROI
                                GetEarInspectionRoiR(mat, out roiL, out roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                #region 待刪
                                //Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                //Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                //Mat m2 = new();
                                //Cv2.Resize(mat, m2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                //Cv2.ImShow("ROIs2", m2);
                                //Cv2.MoveWindow("ROIs2", 900, 0);
                                #endregion
                                break;
                            case 0b1011:    // 11 // 瑕疵
                                EarInspectionR(mat, roiL, roiR);
                                ApexDefectInspectionStepsFlags.EarSteps += 0b1;

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m2 = new();
                                Cv2.Resize(mat, m2, new OpenCvSharp.Size(mat.Width / 2, mat.Height / 2));
                                Cv2.ImShow("m2", m2);
                                Cv2.MoveWindow("m2", 900, 0);
                                #endregion
                                break;
                            case 0b1101:    // 13 // 瑕疵 (側光)
                                EarInspectionR(mat, roiL, roiR);

                                #region 待刪
                                Cv2.Rectangle(mat, roiL, Scalar.Gray, 2);
                                Cv2.Rectangle(mat, roiR, Scalar.Gray, 2);

                                Mat m22 = new();
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
                PostEarIns();
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
        /// 耳朵孔檢測前手續；
        /// Light 1：128, 0, 0, 108；
        /// Light 2：0, 0
        /// </summary>
        public void PreEarHoleInspection()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(128, 0, 0, 108);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 耳朵孔檢驗
        /// </summary>
        /// <param name="src">來源影像</param>
        public bool EarHoleInspection(Mat src, out Point2f center, out float radius)
        {
            //Rect roi = new(510, 870, 180, 180);
            //Rect roi = new(510, 860, 180, 180);
            Rect roi = EarHoleRoi;

            Methods.GetRoiFilter2D(src, roi, 2.7, -0.3, out Mat filter);
            //Methods.GetRoiCanny(src, roi, 50, 120, out Mat canny);
            Methods.GetCanny(filter, 50, 120, out Mat canny);
            // Cv2.ImShow("hole canny", canny);

            // 尋找輪廓
            Cv2.FindContours(canny, out Point[][] cons, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 過濾過短輪廓
            cons = cons.Where(con => con.Length > 50).ToArray();
            // 連接所有輪廓
            Point[] concat = cons.SelectMany(con => con.ToArray()).ToArray();

            #region 
            #region 刪
            Mat circleMat = new(src, roi);
            Cv2.CvtColor(circleMat, circleMat, ColorConversionCodes.GRAY2BGR);
            #endregion

            //
            Cv2.MinEnclosingCircle(concat, out Point2f c, out float r);
            double max = concat.Max(pt => pt.DistanceTo((Point)c));
            double min = concat.Min(pt => pt.DistanceTo((Point)c));
            double avg = concat.Average(pt => pt.DistanceTo((Point)c));
            //
            center = c;
            radius = r;
            //

            #region 刪
            Debug.WriteLine($"Max: {max}");
            Debug.WriteLine($"Min: {min}");
            Debug.WriteLine($"Avg: {avg}");
            Debug.WriteLine($"Rad: {r}");

            // Cv2.Circle(circleMat, (int)c.X, (int)c.Y, (int)r, Scalar.Red, 2);    // 外徑
            // Cv2.Circle(circleMat, (int)c.X, (int)c.Y, 2, Scalar.Blue, 2);        // 圓心

            // Cv2.ImShow("filter", filter);
            // Cv2.ImShow("filter2D", circleMat);
            // Cv2.MoveWindow("filter", 20, 20);
            // Cv2.MoveWindow("filter2D", 220, 20);
            #endregion
            #endregion


            if (min < 0.5 * r)
            {
                // 銑削變形

                return false;
            }
            else if (min < r - 5 || max - min > 5)
            {
                #region 之後只保留 flag
                Cv2.Circle(circleMat, (int)c.X, (int)c.Y, (int)r, Scalar.Red, 2);   // 外徑
                Cv2.Circle(circleMat, (int)c.X, (int)c.Y, 2, Scalar.Blue, 2);       // 圓心

                Debug.WriteLine($"Cons Length {cons.Length}");
                Debug.WriteLine($"Concat cons Length {concat.Length}");

                Cv2.ImShow("filter", filter);
                Cv2.ImShow("filter2D", circleMat);
                Cv2.MoveWindow("filter", 20, 20);
                Cv2.MoveWindow("filter2D", 220, 20);
                #endregion

                return false;
            }
            else
            {
                #region 之後只保留 flag
                Cv2.Circle(circleMat, (int)c.X, (int)c.Y, (int)r, Scalar.Green, 2); // 外徑
                Cv2.Circle(circleMat, (int)c.X, (int)c.Y, 2, Scalar.Green, 2);      // 圓心

                Cv2.ImShow("filter", filter);
                Cv2.ImShow("filter2D", circleMat);
                Cv2.MoveWindow("filter", 20, 20);
                Cv2.MoveWindow("filter2D", 220, 20);
                #endregion;

                return true;
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
            ServoMotion.Axes[1].SetAxisVelParam(10, 500, 1000, 1000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-100, true);
        }

        /// <summary>
        /// 取得耳朵瑕疵檢驗 ROI (L)
        /// </summary>
        /// <param name="roi">(out) ROI Rect</param>
        public void GetEarInspectionRoiL(Mat src, out Rect roiL, out Rect roiR)
        {
            //Rect roi = new(350, 900, 500, 200);
            Rect roi = EarLeftRightRoi;

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out _, out double[] xPos, 3, 30);

            Debug.WriteLine($"xPos Count: {xPos.Length} {string.Join(" , ", xPos)}");
            //Cv2.ImShow($"canny{DateTime.Now:ss.fff}", canny.Clone());
            canny.Dispose();

            roiL = new((int)xPos[0], 930, 45, 150);
            roiR = new((int)xPos[^1] - 45, 930, 45, 200);
        }

        /// <summary>
        /// 耳朵瑕疵檢測 (L)
        /// </summary>
        public bool EarInspectionL(Mat src, Rect roiL, Rect roiR)
        {
            // Canny + Otsu
            Methods.GetRoiOtsu(src, roiL, 0, 255, out Mat Otsu1, out byte th1);
            Methods.GetRoiOtsu(src, roiR, 0, 255, out Mat Otsu2, out byte th2);
            //Debug.WriteLine($"Concat Otsu Threshold: {th1} {th2}");

            Methods.GetRoiCanny(src, roiL, (byte)(th1 - 10), (byte)(th1 * 1.8), out Mat Canny1);
            Methods.GetRoiCanny(src, roiR, (byte)(th2 - 10), (byte)(th2 * 1.8), out Mat Canny2);

            Cv2.FindContours(Canny1, out Point[][] cons, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
            Cv2.FindContours(Canny2, out Point[][] cons2, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

            Mat c = new(src, roiL);
            Cv2.CvtColor(c, c, ColorConversionCodes.GRAY2BGR);
            for (int i = 0; i < cons.Length; i++)
            {
                Cv2.DrawContours(c, cons, i, Scalar.Red, 2);
            }

            Mat c2 = new(src, roiR);
            Cv2.CvtColor(c2, c2, ColorConversionCodes.GRAY2BGR);
            for (int i = 0; i < cons2.Length; i++)
            {
                Cv2.DrawContours(c2, cons2, i, Scalar.Red, 2);
            }

            Debug.WriteLine($"th1: {th1}, th2: {th2}");

            c.Resize(c2.Rows, Scalar.Purple);
            Cv2.HConcat(c, c2, c2);
            string dt = $"{DateTime.Now:ss.fff}";

            Cv2.ImShow($"Ear(L) contours {dt}", c2);
            Cv2.MoveWindow($"Ear(L) contours {dt}", 20 + (th1 + th2) * 10, 20);

            //Cv2.ImShow($"Ear(L) contours {dt}", c);
            //Cv2.MoveWindow($"Ear(L) contours {dt}", 20, 20 + (th1 + th2) * 10);
            //Cv2.ImShow($"Ear(L) contours2 {dt}", c2);
            //Cv2.MoveWindow($"Ear(L) contours2 {dt}", 70 + c.Width, 20 + (th1 + th2) * 10);

            // 這邊要寫演算，ex 毛邊、車刀紋、銑削不良
            return true;
        }

        /// <summary>
        /// 耳朵孔檢測，檢測孔轉一個角度之後有無刮傷
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        public void EarHoleInsL(Mat src, Mat mask)
        {
            using Mat bitwise = new();
            Cv2.BitwiseAnd(src, mask, bitwise);

            // 這邊要寫演算法  // 判斷是有否有毛邊、刮傷
            Methods.GetOtsu(bitwise, 0, 255, out Mat holeOtsu, out byte th);
            Cv2.VConcat(bitwise, holeOtsu, holeOtsu);
            Debug.WriteLine($"L th: {th}");
            Cv2.ImShow("BitwiseL", holeOtsu);
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
            ServoMotion.Axes[1].SetAxisVelParam(10, 500, 1000, 1000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(100, true);
        }

        /// <summary>
        /// 取得耳朵瑕疵檢驗 ROI (R)
        /// </summary>
        /// <param name="roi">(out) ROI Rect</param>
        public void GetEarInspectionRoiR(Mat src, out Rect roiL, out Rect roiR)
        {
            //Rect roi = new(350, 900, 500, 200);
            Rect roi = EarLeftRightRoi;

            Methods.GetRoiCanny(src, roi, 60, 100, out Mat canny);
            Methods.GetHoughVerticalXPos(canny, roi.X, out _, out double[] xPos, 3, 30);
            canny.Dispose();

            roiL = new((int)xPos[0], 930, 45, 200);
            roiR = new((int)xPos[^1] - 45, 930, 45, 150);
        }

        /// <summary>
        /// 耳朵瑕疵檢測 (L)
        /// </summary>
        public bool EarInspectionR(Mat src, Rect roiL, Rect roiR)
        {
            // Canny + Otsu
            Methods.GetRoiOtsu(src, roiL, 0, 255, out _, out byte th1);
            Methods.GetRoiOtsu(src, roiR, 0, 255, out _, out byte th2);
            // Debug.WriteLine($"Concat2 Otsu Threshold: {th1} {th2}");

            Methods.GetRoiCanny(src, roiL, (byte)(th1 - 5), (byte)(th1 * 1.8), out Mat Canny1);
            Methods.GetRoiCanny(src, roiR, (byte)(th2 - 5), (byte)(th1 * 1.8), out Mat Canny2);

            Cv2.FindContours(Canny1, out Point[][] cons, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
            Cv2.FindContours(Canny2, out Point[][] cons2, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

            Mat c = new(src, roiL);
            Cv2.CvtColor(c, c, ColorConversionCodes.GRAY2BGR);
            for (int i = 0; i < cons.Length; i++)
            {
                Cv2.DrawContours(c, cons, i, Scalar.Red, 2);
            }

            Mat c2 = new(src, roiR);
            Cv2.CvtColor(c2, c2, ColorConversionCodes.GRAY2BGR);
            for (int i = 0; i < cons2.Length; i++)
            {
                Cv2.DrawContours(c2, cons2, i, Scalar.Red, 2);
            }

            Debug.WriteLine($"th1: {th1}, th2: {th2}");

            c2.Resize(c.Rows, Scalar.Purple);
            Cv2.HConcat(c, c2, c2);
            string dt = $"{DateTime.Now:ss.fff}";

            Cv2.ImShow($"Ear(R) contours {dt}", c2);
            Cv2.MoveWindow($"Ear(R) contours {dt}", 200 + (th1 + th2) * 10, 50);


            //Cv2.ImShow($"Ear(R) contours {dt}", c);
            //Cv2.MoveWindow($"Ear(R) contours {dt}", 200, 50 + (th1 + th2) * 5);
            //Cv2.ImShow($"Ear(R) contours2 {dt}", c2);
            //Cv2.MoveWindow($"Ear(R) contours2 {dt}", 250 + c.Width, 50 + (th1 + th2) * 5);

            // 這邊要寫演算，ex 毛邊、車刀紋、銑削不良
            return true;
        }

        /// <summary>
        /// 耳朵孔檢測，檢測孔轉一個角度之後有無刮傷
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        public void EarHoleInsR(Mat src, Mat mask)
        {
            using Mat bitwise = new();
            Cv2.BitwiseAnd(src, mask, bitwise);

            // 這邊要寫演算法  // 判斷是有否有毛邊、刮傷
            Methods.GetOtsu(bitwise, 0, 255, out Mat holeOtsu, out byte th);
            Cv2.VConcat(bitwise, holeOtsu, holeOtsu);
            Debug.WriteLine($"R th: {th}");
            Cv2.ImShow("BitwiseR", holeOtsu);
        }

        /// <summary>
        /// 耳朵瑕疵前手續；
        /// Light1: 256, 0, 128, 96；
        /// </summary>
        public void PreEarIns()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(256, 0, 0, 96);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
        }

        /// <summary>
        /// 耳朵瑕疵前手續 (Side Light)；
        /// Light1 : 128, 0, 0, 0；
        /// Light2 : 0, 128；
        /// </summary>
        public void PreEarInsSide()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(128, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 96);
        }

        /// <summary>
        /// 耳朵檢測完畢，關閉所有光源
        /// </summary>
        public void PostEarIns()
        {
            // 變更光源 1
            LightCtrls[0].ResetAllChannel();
            // 變更光源 2
            LightCtrls[1].ResetAllChannel();
        }
        #endregion

        // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
        // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 
        // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // 

        #region 表面瑕疵檢驗
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

                byte endStep = 0b1111;

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
                        case 0b0000:
                            await PreSurfaceIns();

                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0001:
                            cam3.Camera.StreamGrabber.Start();

                            ApexDefectInspectionStepsFlags.SurfaceSteps += 0b01;
                            break;
                        case 0b0010:

                            break;
                        case 0b0100:

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
        /// Motor Pos：-185 (窗戶轉正)
        /// </summary>
        public async Task PreSurfaceIns()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(96, 0, 0, 0);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(100, 1000, 1000, 1000);
            // 旋轉至目標位置
            await ServoMotion.Axes[1].PosMoveAsync(-185, true);
        }

        public void SurfaceIns1(Mat src)
        {
            foreach (Rect roi in Surface1ROIs)
            {
                Methods.GetRoiGaussianBlur(src, roi, new OpenCvSharp.Size(3, 3), 1.2, 0, out Mat blur);

                Mat hist = new();
                Cv2.CalcHist(new Mat[] { blur }, new int[] { 0 }, new Mat(), hist, 1, new int[] { 256 }, new Rangef[] { new Rangef(0.0f, 256.0f) });

                int[] maxArr = new int[1];
                int[] minArr = new int[1];
                Cv2.MinMaxIdx(hist, out double min, out double max, minArr, maxArr);

                Debug.WriteLine($"Max: {max}, Min: {min}, {string.Join(",", maxArr)}, {string.Join(",", minArr)}");

                Mat histChart = new(new OpenCvSharp.Size(256, 300), MatType.CV_8UC3, Scalar.White);
                for (int i = 0; i < 256; i++)
                {
                    int len = (int)(hist.Get<float>(i) / (1.2 * max) * histChart.Rows);
                    Cv2.Line(histChart, i, histChart.Rows, i, histChart.Rows - len, Scalar.Blue, 1);
                }
                Cv2.Line(histChart, maxArr[0], histChart.Rows, maxArr[0], histChart.Rows - (int)(max / (1.2 * max) * histChart.Rows), Scalar.Red, 1);

                // 計算平均值和標準差
                Cv2.MeanStdDev(blur, out Scalar mean, out Scalar stddev);
                // 平均值 & 標準差
                Debug.WriteLine($"Mat mean: {mean}, Stddev: {stddev}");

                Methods.GetHorizontalGrayScale(blur, out byte[] grayArr, out short[] grayArrDiff, true, out Mat chart, Scalar.Black);
                Methods.CalLocalOutliers(chart, grayArr, 50, 15, null, out Point[] peaks, out Point[] valleys);

                // 眾數線
                Cv2.Line(chart, 0, 300 - maxArr[0], chart.Width, 300 - maxArr[0], Scalar.Orange, 1);
                // 平均數
                Cv2.Line(chart, 0, 300 - (int)mean[0], chart.Width, 300 - (int)mean[0], Scalar.Blue, 1);
                // 一個標準差內
                Cv2.Line(chart, 0, 300 - (int)(mean[0] + stddev[0]), chart.Width, 300 - (int)(mean[0] + stddev[0]), Scalar.DarkCyan, 1);
                Cv2.Line(chart, 0, 300 - (int)(mean[0] - stddev[0]), chart.Width, 300 - (int)(mean[0] - stddev[0]), Scalar.DarkCyan, 1);

                Cv2.Rectangle(src, roi, Scalar.Black, 2);

                Cv2.CvtColor(blur, blur, ColorConversionCodes.GRAY2BGR);
                Cv2.VConcat(new Mat[] { chart, blur }, chart);


                Dispatcher.Invoke(() =>
                {
                    Cv2.ImShow($"GrayScale{roi.X}", chart);
                    Cv2.MoveWindow($"GrayScale{roi.X}", roi.X - 720, 100);
                    Cv2.ImShow($"Hist Diagram{roi.X}", histChart);
                    Cv2.MoveWindow($"Hist Diagram{roi.X}", roi.X - 720, 100 + 300);
                    //Cv2.MoveWindow("GrayScale", 100, 100);
                });
            }
        }

        public void SurfaceIns2(Mat src)
        {
            return;
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