using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace ApexVisIns
{
    public partial class MainWindow : Window
    {
        private void TitleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DebugMode)
            {
                if (e.ClickCount >= 2)
                {
                    Maxbtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    //Maxbtn.RaiseEvent
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void Minbtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maxbtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Quitbtn_Click(object sender, RoutedEventArgs e)
        {
            //if (BaslerCam != null && BaslerCam.IsOpen)
            //{
            //    Console.WriteLine("請先關閉相機連線。");
            //    return;
            //}
            //if ((bool)CamConnect.IsChecked)
            //{
            //    MsgInformer.AddError(MsgInformer.Message.MsgCode.APP, "Close camera connection before exit", MsgInformer.Message.MessageType.Info);
            //    return;
            //}

            if (!BaslerCam.IsConnected && BaslerCams.All(item => !item.IsConnected))
            {
                Close();
            }
            else
            {
                MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "Close camera connection before exit");
                return;
            }
        }
    }
}
