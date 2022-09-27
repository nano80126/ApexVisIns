using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
        #region Fields
        private TcpClient _tcpClient;
        #endregion

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

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpClient == null)
            {
                _tcpClient = new TcpClient("127.0.0.1", 8016);
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            //_tcpClient.Close();

            //return;

            if (_tcpClient != null)
            {
                NetworkStream networkStream = _tcpClient.GetStream();

                byte[] data = Encoding.UTF8.GetBytes("test");

                networkStream.Write(data, 0, data.Length);
                //System.Diagnostics.Debug.WriteLine($"{DateTime.Now:mm:ss.fff} 123");

                data = new byte[256];

                int i = networkStream.Read(data, 0, data.Length);

                string str = Encoding.UTF8.GetString(data, 0, i);

                System.Diagnostics.Debug.WriteLine($"Str: {str}");

                //networkStream.Close();
                //_tcpClient.Close();
            }
        }
    }
}
