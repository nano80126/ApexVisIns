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
            //Debug.WriteLine("Main Tab Load");

            //Task.Run(() =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        InitCamera();
            //        MainWindow.ProgressValue = 10;
            //        SpinWait.SpinUntil(() => false, 2000);
            //    });

            //    SpinWait.SpinUntil(() => false, 2000);

            //    Dispatcher.Invoke(() =>
            //    {
            //        InitCamera();
            //        MainWindow.ProgressValue = 20;
            //        SpinWait.SpinUntil(() => false, 2000);
            //    });
            //});
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
        private void InitLighCtrl()
        {




            MainWindow.MsgInformer.AddSuccess(MsgInformer.Message.MsgCode.APP, "光源控制器初始化完成");
            // 更新progress value
        }
        #endregion

    }
}
