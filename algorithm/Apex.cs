using System;
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
                        img.GetCannyROI(roi);
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
        public Apex() {}

        public Apex(Bitmap bmp) : base(bmp) {}

        public Apex(OpenCvSharp.Mat mat) : base(mat) {}

        public Apex(string path) : base(path) {}

        public void GetCannyROI(Rect roi)
        {
            try
            {
                using Mat clone = new(img, roi);
                using Mat blur = new();
                Mat canny = new();

                Cv2.BilateralFilter(clone, blur, 5, 50, 100);
                Cv2.Canny(blur, canny, 50, 30, 3);

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
    }
}