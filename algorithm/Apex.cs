﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Point = OpenCvSharp.Point;
using System.Drawing;
using OpenCvSharp;

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
        public void ProcessApex(Mat mat)
        {
            int matWidth = mat.Width;
            int matHeight = mat.Height;

            algorithm.Apex img = new(mat);

            try
            {
                Rect roi = AssistRect.GetRect();

                if (roi.Width * roi.Height > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        
                        img.GetCannyROI(roi, 50, 0);

                        //img.GetSharpROI(roi);



                        Mat med = new();
                        Mat med2 = new();
                        Mat dif = new();
                        Mat range = new();

                        Mat roiImg = new(img.GetMat(), roi);

                        Cv2.MedianBlur(roiImg, med, 3);
                        Cv2.MedianBlur(roiImg, med2, 21);
                        Cv2.Absdiff(med, med2, dif);

                        BackgroundSubtractorMOG2 mog = BackgroundSubtractorMOG2.Create();
                        Mat mask = new();

                        mog.Apply(med, mask);
                        mog.Apply(med2, mask);

                        Cv2.ImShow("med", med);
                        Cv2.ImShow("med2", med2);

                        Cv2.ImShow("dif", dif);
                        Cv2.ImShow("mask", mask);

                        //Cv2.ImShow("bw", bw);
                        //Cv2.ImShow("dst", dst);
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    if (OnTabIndex == 0)
                    {
                        ImageSource = img.GetMat().ToImageSource();
                    }
                });
            }
            catch (OpenCVException)
            {

            }
            catch (OpenCvSharpException)
            {

            }
            catch (Exception)
            {

            }
            finally
            {


            }
        }
    }
}


namespace ApexVisIns.algorithm
{

    public class Apex : Algorithm
    {
        public Apex() { }

        public Apex(Bitmap bmp) : base(bmp) { }

        public Apex(OpenCvSharp.Mat mat) : base(mat) { }

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
                Mat blur = new();
                Mat canny = new();

                Cv2.BilateralFilter(clone, blur, 5, 50, 100);
                Cv2.Canny(blur, canny, th1, th2, 3);

                Cv2.ImShow("blur", blur);
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
    }
}