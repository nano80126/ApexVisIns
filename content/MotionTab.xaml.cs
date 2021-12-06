using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Advantech.Motion;

namespace ApexVisIns.content
{
    /// <summary>
    /// MotionTab.xaml 的互動邏輯
    /// </summary>
    public partial class MotionTab : StackPanel
    {
        #region Variables
        private uint boardCount;
        private DEV_LIST[] BoardList = new DEV_LIST[Motion.MAX_DEVICES];

        public struct struchA
        {
            //public byte a;
            ////public char[] c = new char[4] { };
            //public int c;
            //public byte b;
            public string str;

            public struchA(string str)
            {
                this.str = str;
                //this.a = a;
                //this.b = b;
            }
        }
        #endregion


        public MotionTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            int a = System.Runtime.InteropServices.Marshal.SizeOf(MainWindow.ServoMotion.ServoReady);
            int b = System.Runtime.InteropServices.Marshal.SizeOf(MainWindow.ServoMotion.ServoAlm);

            struchA struch = new struchA("1");

            int c = System.Runtime.InteropServices.Marshal.SizeOf(struch);
            //struch.c = "456";

            Debug.WriteLine($"{a} {b} {c}");
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void CardOpen_Click(object sender, RoutedEventArgs e)
        {


        }

        public void GetAvailableBoards()
        {
            int result = Motion.mAcm_GetAvailableDevs(BoardList, Motion.MAX_DEVICES, ref boardCount);


            MainWindow.ServoMotion.CardList.Clear();

            for (int i = 0; i < BoardList.Length; i++)
            {
                MainWindow.ServoMotion.CardList.Add(BoardList[i].DeviceName);
            }

            if (boardCount > 0)
            {
                Debug.WriteLine($"{BoardList[0].DeviceNum} {BoardList[0].NumofSubDevice}");
            }

        }

    }
}
