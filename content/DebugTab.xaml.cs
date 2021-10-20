using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ApexVisIns.content
{
    /// <summary>
    /// Programming.xaml 的互動邏輯
    /// </summary>
    public partial class DebugTab : StackPanel, INotifyPropertyChanged
    {

        #region Resources
        public static Crosshair Crosshair { set; get; }
        public static AssistRect AssistRect { set; get; }
        public static Indicator Indicator { set; get; }
        #endregion

        public DebugTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            #region Find Resource
            if (Crosshair == null)
            {
                Crosshair = TryFindResource("Crosshair2") as Crosshair;
            }

            if (AssistRect == null)
            {
                AssistRect = TryFindResource("AssistRect2") as AssistRect;
            }

            if (Indicator == null)
            {
                Indicator = TryFindResource("Indicator2") as Indicator;
            }
            #endregion
        }

        public double ZoomRatio
        {
            get => ImageViewbox.Width / ImageCanvas.Width * 100;
            set
            {
                int v = (int)Math.Floor(value);

                if (20 > v)
                {
                    ImageViewbox.Width = 0.2 * ImageCanvas.Width;
                }
                else if (v > 200)
                {
                    ImageViewbox.Width = 2 * ImageCanvas.Width;
                }
                else
                {
                    double ratio = value / 100;
                    ImageViewbox.Width = ratio * ImageCanvas.Width;
                }
                OnPropertyChanged(nameof(ZoomRatio));
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ImageScroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            //System.Windows.Point pt = e.GetPosition(ImageCanvas);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    ZoomRatio += 5;
                }
                else
                {
                    ZoomRatio -= 5;
                }
            }


        }

        private void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void ImageCanvas_MouseLeave(object sender, MouseEventArgs e)
        {

        }




        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
