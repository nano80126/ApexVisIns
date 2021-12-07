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
using System.Runtime.InteropServices;

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
        
        #endregion


        public MotionTab()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            //int a = Marshal.SizeOf(MainWindow.ServoMotion.ServoReady);
            //int b = Marshal.SizeOf(MainWindow.ServoMotion.ServoAlm);

            //structA struchA = new structA(10, 20, "123");
            //structB struchB = new structB(1, 10, 20, 3, 4, 5);

            //int c = Marshal.SizeOf(struchA);
            //int d = Marshal.SizeOf(struchB);
            ////struch.c = "456";

            //Debug.WriteLine($"{c} {d}");
            #region 保留

            #endregion
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "運動頁面已載入");
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
