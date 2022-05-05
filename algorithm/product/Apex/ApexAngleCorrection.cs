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
        #region 管件角度校正，最終使耳朵圓孔正對 Camera2，使用ImageGrabbed事件
        public async void ApexAngleCorrectionSequence(BaslerCam cam1)
        {
            try
            {
                double width1 = -1;
                double width2 = -1;
                byte mode = 8;  // 未定

                byte endStep = 0b1001;
                int cycleCount = 0;


                while (ApexAngleCorrectionFlags.CheckModeStep < endStep)
                {
                    Debug.WriteLine($"Step:{ApexAngleCorrectionFlags.CheckModeStep}");
                    if (cycleCount++ > endStep)
                    {
                        break;
                    }
                    Debug.WriteLine($"Cycle Count:{cycleCount}");

                    switch (ApexAngleCorrectionFlags.CheckModeStep)
                    {
                        case 0b0000:
                            #region 0b0000(0) // 變更光源、馬達速度
                            PreCheckCorrectionMode();
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0001:
                            #region 0b0001(1) // 狀態 1 width 
                            CheckCorrectionGetWidth(cam1.Camera, out width1);
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0010:
                            #region 0b0010(2) // 啟動馬達旋轉 50 pulse
                            await CheckCorrectionMotorMove(50);
                            //_ = SpinWait.SpinUntil(() => false, 500);
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0011:
                            #region 0b0011(3) // 狀態 2 width 
                            CheckCorrectionGetWidth(cam1.Camera, out width2);
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0100:
                            #region 0b0100(4) // 停止Grabber，計算校正模式
                            StopWindowEarGrabber();                         // 停止 Grabber
                            CalCorrectionMode(width1, width2, out mode);    // 計算出校正模式
                            //Debug.WriteLine($"Width1: {width1}, Width2: {width2}");
                            ApexAngleCorrectionFlags.CorrectionMode = mode;
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0101:
                            #region 0b0101(5) // 回轉 50 pulse
                            Debug.WriteLine($"Mode: {mode}");
                            if (mode == 5)
                            {
                                await CheckCorrectionMotorMove(-50);
                            }
                            else if (mode == 7)
                            {
                                await CheckCorrectionMotorMove(-45);
                            }
                            // _ = SpinWait.SpinUntil(() =>  false, 500);
                            ApexAngleCorrectionFlags.LastWindowWidth = ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)Math.Max(width1, width2);
                            #endregion
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            break;
                        case 0b0110:
                            #region 0b0110(6) // 根據 mode 啟動馬達
                            StartCorrectionMotor(mode);
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b0111:
                            #region 0b0111 // 7 //
                            // 保留，實際上光源沒有變更
                            //PreCorrectionContinuous();    // pass
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
                            break;
                        case 0b1000:    // 8 
                            #region 0b1000 // 8 //
                            StartWindowEarCameraContinous();    // 開啟窗戶、耳朵相機連續拍攝
                            ApexAngleCorrectionFlags.CheckModeStep += 0b01;
                            #endregion
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
        /// 確認旋轉方向前步驟；
        /// Light1：0, 0, 128, 128；
        /// Light2：0, 0；
        /// Motor ：10, 100, 1000, 1000；
        /// </summary>
        public void PreCheckCorrectionMode()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(0, 0, 128, 144);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            ServoMotion.Axes[1].SetAxisVelParam(10, 100, 1000, 1000);
            // 啟動定速旋轉
            //_ = ServoMotion.Axes[1].TryVelMove(0);
        }

        /// <summary>
        /// (保留)
        /// 對位連續拍攝前步驟；
        /// Light1：0, 0, 128, 128；
        /// Light2：0, 0；
        /// </summary>
        public void PreCorrectionContinuous()
        {
            // 變更光源 1
            LightCtrls[0].SetAllChannelValue(160, 0, 128, 128);
            // 變更光源 2
            LightCtrls[1].SetAllChannelValue(0, 0);
            // 變更馬達速度
            //ServoMotion.Axes[1].SetAxisVelParam(10, 100, 1000, 1000);
            // 啟動定速旋轉
            //_ = ServoMotion.Axes[1].TryVelMove(0);
        }

        /// <summary>
        /// 確認校正模式，馬達旋轉 50 pulse
        /// </summary>
        public async Task CheckCorrectionMotorMove(double pos)
        {
            // 正轉 50 pulse
            await ServoMotion.Axes[1].PosMoveAsync(pos);
        }

        /// <summary>
        /// 取得 Corretion 窗戶 width
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="width"></param>
        public void CheckCorrectionGetWidth(Camera camera, out double width)
        {
            // Width = 0;
            camera.ExecuteSoftwareTrigger();

            using IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
            using Mat mat = BaslerFunc.GrabResultToMatMono(grabResult);

            Rect roi = WindowLeftRightRoi;

            if (ApexAngleCorrectionFlags.OtsuThreshlod == 0)
            {
                Methods.GetRoiOtsu(mat, roi, 0, 255, out _, out byte th);
                ApexAngleCorrectionFlags.OtsuThreshlod = th;
            }

            byte otsuTh = ApexAngleCorrectionFlags.OtsuThreshlod;
            //Methods.GetRoiCanny(mat, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out Mat canny);
            // 二值
            Methods.GetRoiBinarization(mat, roi, otsuTh, 255, out Mat bin);
            // 閉運算
            Mat ele = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3), new Point(-1, -1));
            Cv2.MorphologyEx(bin, bin, MorphTypes.Close, ele, null, 5);
            // canny
            Methods.GetCanny(bin, 75, 150, out Mat canny);

            bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out width, 3, 30, 50);

            Cv2.VConcat(new Mat(mat, roi), canny, canny);
            //Cv2.VConcat(new Mat(mat, roi), bin, bin);
            //Cv2.VConcat(bin, canny, canny);
            Cv2.PutText(canny, $"{width:f2}", new Point(20, 20), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 1);
            //Cv2.ImShow($"src{DateTime.Now:ss.fff}", new Mat(mat, roi));
            Cv2.ImShow($"canny{DateTime.Now:ss.fff}", canny);

            #region 釋放資源
            ele.Dispose();
            //bin.Dispose();
            //canny.Dispose();
            #endregion
        }

        /// <summary>
        /// 計算校正模式
        /// </summary>
        /// <param name="width1"></param>
        /// <param name="width2"></param>
        public void CalCorrectionMode(double width1, double width2, out byte mode)
        {

            if (width1 == 0 && width2 == 0)
            {
                mode = 0;      // 快正轉
            }
            else
            {
                if (width2 > width1)    // 正轉
                {
                    if (width2 < 200)   // Vel = 200
                    {
                        mode = 0;  // 快正轉
                    }
                    else if (width2 < 300)
                    {
                        mode = 2;  // 慢正轉
                    }
                    else if (width2 < 350)
                    {
                        mode = 4;  // 低速正轉
                    }
                    else
                    {
                        mode = 6;  // 正接近
                    }
                }
                else
                {
                    if (width1 < 200)
                    {
                        mode = 1;  // 快逆轉
                    }
                    else if (width1 < 300)
                    {
                        mode = 3;  // 慢逆傳
                    }
                    else if (width1 < 350)
                    {
                        mode = 5;  // 低速逆轉
                    }
                    else
                    {
                        mode = 7;  // 逆接近
                    }
                }
            }
        }

        /// <summary>
        /// 根據校正模式(mode)啟動馬達
        /// </summary>
        public void StartCorrectionMotor(byte mode)
        {
            switch (mode)
            {
                case 0: // 快正轉
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(0);
                    ApexAngleCorrectionFlags.Steps = 0;
                    break;
                case 1: // 快逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(50, 500, 10000, 10000);
                    ServoMotion.Axes[1].VelMove(1);
                    ApexAngleCorrectionFlags.Steps = 0;
                    break;
                case 2: // 慢正轉
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(0);
                    ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 3: // 慢逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(20, 200, 4000, 4000);
                    ServoMotion.Axes[1].VelMove(1);
                    ApexAngleCorrectionFlags.Steps = 1;
                    break;
                case 4: // 低速正轉
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(0);
                    ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 5: // 低速逆轉
                    ServoMotion.Axes[1].SetAxisVelParam(5, 50, 1000, 1000);
                    ServoMotion.Axes[1].VelMove(1);
                    ApexAngleCorrectionFlags.Steps = 2;
                    break;
                case 6: // 正接近
                    ApexAngleCorrectionFlags.Steps = 3;
                    break;
                case 7: // 逆接近
                    ApexAngleCorrectionFlags.Steps = 3;
                    break;
                default:    // 8 未定
                    break;
            }
        }

        /// <summary>
        /// 確認旋轉方向，確認Otsu 閾值
        /// </summary>
        /// <param name="cam">目標相機</param>
        public void CheckCorrectionMode(BaslerCam cam, out byte mode)
        {
            mode = 8;  // 8 未定

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
                                                                        // 需要開運算除毛邊?
                Methods.GetRoiCanny(mat, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out Mat canny);
                // Cv2.ImShow("mat1", mat);

                // 抓取第一次窗戶寬度
                bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width1, 3, 50, 100);

                Debug.WriteLine($"width 1: {width1}");

                _ = SpinWait.SpinUntil(() => false, 100);

                cam.Camera.ExecuteSoftwareTrigger();
                grabResult = cam.Camera.StreamGrabber.RetrieveResult(500, TimeoutHandling.ThrowException);
                mat = BaslerFunc.GrabResultToMatMono(grabResult);

                Methods.GetRoiCanny(mat, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out canny);
                // Cv2.ImShow("mat2", mat);

                // 抓取第二次窗戶寬度
                // dir = ApexAngleCorrectionFlags.CorrectionMode;
                FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width2, 3, 50, 100);

                Debug.WriteLine($"width 2: {width2}");

                ServoMotion.Axes[1].StopMove();

                if (width1 == 0 && width2 == 0)
                {
                    mode = 0;      // 快正轉
                }
                else
                {
                    if (width2 > width1)    // 正轉
                    {
                        if (width2 < 200)   // Vel = 200
                        {
                            mode = 0;  // 快正轉
                        }
                        else if (width2 < 300)
                        {
                            mode = 2;  // 慢正轉
                        }
                        else if (width2 < 350)
                        {
                            mode = 4;  // 低速正轉
                        }
                        else
                        {
                            mode = 6;  // 正接近
                        }
                    }
                    else
                    {
                        if (width1 < 200)
                        {
                            mode = 1;  // 快逆轉
                        }
                        else if (width1 < 300)
                        {
                            mode = 3;  // 慢逆傳
                        }
                        else if (width1 < 350)
                        {
                            mode = 5;  // 低速逆轉
                        }
                        else
                        {
                            mode = 7;  // 逆接近
                        }
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
        [Obsolete("此方法只使用窗戶校正，準確度低且容易被毛邊影響")]
        public void AngleCorrection(Mat src)
        {
            Rect roi = new(100, 840, 1000, 240);

            Methods.GetRoiCanny(src, roi, 75, 120, out Mat canny);
            //byte dir = 0;
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
        /// 相機1(窗戶)粗定位，粗定位時不使用相機2；
        /// 相機2(耳朵)精定位，精定位時不使用相機1；
        /// </summary>
        /// <param name="src">相機 1 影像</param>
        /// <param name="src2">相機 2 影像</param>
        public void AngleCorrection(Mat src, Mat src2)
        {
            if (src != null && !src.Empty())
            {
                // X: 1200 - 100, Y: 960 - 120
                // Rect roi = new(100, 840, 1000, 240);
                Rect roi = WindowLeftRightRoi;

                // if (ApexAngleCorrectionFlags.OtsuThreshlod == 0)
                // {
                //     Methods.GetRoiOtsu(src, roi, 0, 255, out _, out byte th);
                //     ApexAngleCorrectionFlags.OtsuThreshlod = th;
                // }

                byte otsuTh = ApexAngleCorrectionFlags.OtsuThreshlod;   // Otsu 閾值
                // 需要開運算除毛邊?
                Methods.GetRoiCanny(src, roi, (byte)(otsuTh - 30), (byte)(otsuTh * 1.2), out Mat canny);
                // 尋找窗戶
                bool FindWindow = Methods.GetVertialWindowWidth(canny, out _, out double width, 3, 30, 100);

                if (FindWindow && ApexAngleCorrectionFlags.Steps != 0b0101) // 
                {
                    // Cv2.ImShow("Canny", new Mat(src, roi));
                    Cv2.ImShow("ApexCorrectionCanny", canny);
                    Cv2.MoveWindow("ApexCorrectionCanny", 20, 500);

                    switch (ApexAngleCorrectionFlags.Steps)
                    {
                        case 0b0000:    // 0 // 快速找窗戶
                            #region 0b0000 // 0 // 快速找窗戶
                            if (width > 200 && width > ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                ServoMotion.Axes[1].ChangeVel(200);
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0001:    // 1 // 慢速找窗戶
                            #region 0b0001 // 1 // 慢速找窗戶
                            if (width > 300 && width > ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                ServoMotion.Axes[1].ChangeVel(50);
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0010:    // 2 // 極慢速找窗戶
                            #region 0b0010 // 2 // 極慢速找窗戶
                            if (width > 350)
                            {
                                ServoMotion.Axes[1].StopMove();

                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        case 0b0011:    // 3 // 每次 +5 pulse
                            #region 0b0011 // 3 // 每次 +5 pulse
                            Debug.WriteLine($"Last {ApexAngleCorrectionFlags.LastWindowWidth} Max: {ApexAngleCorrectionFlags.MaxWindowWidth}");

                            if (width >= ApexAngleCorrectionFlags.LastWindowWidth)
                            {
                                if ((ApexAngleCorrectionFlags.CorrectionMode & 0b01) == 0b00)   // 正向
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(5);
                                }
                                else if ((ApexAngleCorrectionFlags.CorrectionMode & 0b01) == 0b01)  // 逆向
                                {
                                    _ = ServoMotion.Axes[1].TryPosMove(-5);
                                }
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
                        case 0b0100:    // 4 // 
                            #region 0b0100 // 4 // 粗定位
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
                                        //Debug.WriteLine($"粗定位結束: {DateTime.Now:mm:ss.fff} {(DateTime.Now - StartCorrection).TotalMilliseconds}");
                                    }
                                }

                                //Debug.WriteLine($"粗定位結束: {DateTime.Now:mm:ss.fff} {(DateTime.Now - StartCorrection).TotalMilliseconds}");
                            }
                            else
                            {
                                ApexAngleCorrectionFlags.MaxWindowWidth = (ushort)width;
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                // 到此粗定位結束
                                Cv2.DestroyWindow("ApexCorrectionCanny");
                                //Debug.WriteLine($"粗定位結束: {DateTime.Now:mm:ss.fff} {(DateTime.Now - StartCorrection).TotalMilliseconds}");
                            }
                            ApexAngleCorrectionFlags.LastWindowWidth = (ushort)width;
                            #endregion
                            break;
                        default:
                            break;
                    }
                }
#if false
                Debug.WriteLine($"Width: {width} Last: {ApexAngleCorrectionFlags.LastWindowWidth} Max: {ApexAngleCorrectionFlags.MaxWindowWidth}");
#endif
                #region 資源釋放
                //src.Dispose();
                //canny.Dispose();
                #endregion
            }

            if (src2 != null && !src2.Empty())
            {
                // X: 600 - 90, Y: 960 - 90
                //Rect roi = new(510, 860, 180, 180);
                Rect roi = new(EarHoleRoi.X - 10, EarHoleRoi.Y + 10, EarHoleRoi.Width + 20, EarHoleRoi.Height - 20);

                int l = 0;  // 孔左輪廓計數
                int r = 0;  // 孔右輪廓計數

                // 銳化垂直
                Methods.GetRoiVerticalFilter2D(src2, roi, 1.2, -0.4, out Mat filter);
                Methods.GetCanny(filter, 75, 150, out Mat canny);

                // 找輪廓
                Cv2.FindContours(canny, out Point[][] cons, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                // 過濾過短輪廓
                cons = cons.Where(cons => cons.Length > 50).ToArray();
                // int maxLength = cons.Max(c => c.Length);
                double maxLength = cons.Max(c => Cv2.ArcLength(c, false));  // 最大弧長

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
                    // 這是在幹嘛? // 可省略
                    Cv2.DrawContours(src2, cons, i, Scalar.Black, 2);
                    // Debug.WriteLine($"Length: {cons[i].Length} {Cv2.ArcLength(cons[i], false)}");

                    double arc = Cv2.ArcLength(cons[i], false);
                    if (arc == maxLength)   // 大圓弧長
                    {
                        if (arc > 400)
                        {
                            continue;
                        }
                        //else
                        //{
                        //Debug.WriteLine($"Max Arc: {arc}");
                        //}
                    }

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
                Cv2.ImShow($"ZOOM", new Mat(src2, roi));

                Cv2.MoveWindow("Ear filter", 20, 20);
                Cv2.MoveWindow("Ear Canny", 20 + roi.Width, 20);
                Cv2.MoveWindow("ZOOM", 20 + roi.Width * 2, 20);
                #endregion

                // Debug.WriteLine($"L: {l}; R: {r}, {c}");

                if (ApexAngleCorrectionFlags.Steps >= 5)
                {
                    switch (ApexAngleCorrectionFlags.Steps)
                    {
                        case 0b0101:
                            #region 0b0110(5) // 判斷 L = R
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
                        case 0b0110:
                            #region 0b0110(6) // 判斷 L、R
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
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                break;
                            }
                            // 到此精定位結束 
                            #endregion
                            break;
                        case 0b0111:
                            #region 0b00111(7) // 重置 pos
                            if (ServoMotion.Axes[1].TryResetPos() == (uint)Advantech.Motion.ErrorCode.SUCCESS)
                            {
                                ApexAngleCorrectionFlags.Steps += 0b01;
                                // Cv2.DestroyAllWindows();

                                Cv2.DestroyWindow("ZOOM");
                                // 終止連續拍攝
                                StopWindowEarCameraContinous();
                                //Debug.WriteLine($"精定位結束: {DateTime.Now:mm:ss.fff} {(DateTime.Now - StartCorrection).TotalMilliseconds}");
                            }
                            #endregion
                            break;
                        default:        // 0b1000 // 8
                            //StopWindowEarCameraContinous();
                            break;
                    }
                }

                #region 資源釋放
                //src2.Dispose();
                //filter.Dispose();
                //canny.Dispose();
                #endregion

            }

#if false
            Debug.WriteLine($"Steps: {ApexAngleCorrectionFlags.Steps}"); 
#endif
        }
        #endregion
    }
}
