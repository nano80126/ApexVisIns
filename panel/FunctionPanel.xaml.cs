using System;
using System.Collections.Generic;
using System.IO;
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
using MCAJawIns.Tab;
using Microsoft.Win32;
using OpenCvSharp;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// FunctionPanel.xaml 的互動邏輯
    /// </summary>
    public partial class FunctionPanel : Control.CustomCard
    {
        #region Properties
        public EngineerTab EngineerTab { get; set; }
        #endregion

        public FunctionPanel()
        {
            InitializeComponent();
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            //EngineerTab.save
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            Mat mat = EngineerTab.Indicator.Image;

            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                FileName = string.Empty,
                Filter = "BMP Image(*.bmp)|*.bmp",
                InitialDirectory = $@"{Directory.GetCurrentDirectory()}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                Cv2.ImWrite(saveFileDialog.FileName, mat);
            }
        }
    }
}
