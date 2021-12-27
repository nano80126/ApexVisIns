using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// MainTab.xaml 的互動邏輯
    /// </summary>
    public partial class MainTab : StackPanel
    {
        #region Resources

        #endregion

        #region Variables
        public MainWindow MainWindow { get; set; }
        #endregion

        public MainTab()
        {
            InitializeComponent();
        }

        #region Load & UnLoad
        /// <summary>
        /// Main Tab 載入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "主頁面已載入");

        }

        /// <summary>
        /// Main Tab 卸載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Main Tab Unload");
        }
        #endregion


        #region 初始化
        /// <summary>
        /// 相機初始化
        /// </summary>
        private void InitCamera()
        {



            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "相機初始化完成");
            // 更新progress value
        }

        /// <summary>
        /// 運動初始化
        /// </summary>
        private void InitMotion()
        {




            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "運動軸初始化完成");
            // 更新progress value
        }

        /// <summary>
        /// 光源控制初始化
        /// </summary>
        private void InitLighCtrls()
        {
            LightController light24V = MainWindow.LightCtrls[0];
            LightController light_6V = MainWindow.LightCtrls[1];


            if (!light24V.IsComOpen)
            {
                light24V.ComOpen("COM1", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                // 重置所有通道
                light24V.ResetAllValue();

            }


            if (!light_6V.IsComOpen)
            {
                light_6V.ComOpen("COM2", 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                // 重置所有通道
                light_6V.ResetAllValue();
            }


            // 下面需要停止 LightEnumer


            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成");
            // 更新progress value
        }
        #endregion

        /// <summary>
        /// 規格選擇變更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpecSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageBox.Show((sender as ListBox).SelectedIndex.ToString());
        }

        /// <summary>
        /// 測試完刪除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [Obsolete("確認硬體狀態")]
        private void CheckHwStatus_Click(object sender, RoutedEventArgs e)
        {
            foreach (BaslerCam cam in MainWindow.BaslerCams)
            {
                Debug.WriteLine(cam.IsConnected);
            }

            bool open = MainWindow.LightController.IsComOpen;
            Debug.WriteLine(open);


            Debug.WriteLine(MainWindow.ServoMotion.MaxAxisCount);
        }
    }
}
