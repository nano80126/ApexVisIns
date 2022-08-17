using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace MCAJawIns
{
    public partial class MainWindow : Window
    {
        private void TitleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DebugMode)
            {
                if (e.ClickCount >= 2)
                {
                    //Maxbtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    // MaxBtn run command
                    Maxbtn.Command.Execute(Maxbtn.CommandParameter);   
                }
                else
                {
                    DragMove();
                }
            }
        }

        // // // // // // 以下要改成 Command // // // // // 

        [Obsolete("Use command method")]
        private void Minbtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MinWindow_Command(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        [Obsolete("Use command method")]
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

        private void MaxWindow_Commnad(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        [Obsolete("Use command method")]
        private void Quitbtn_Click(object sender, RoutedEventArgs e)
        {
            if (!BaslerCam.IsConnected && BaslerCams.All(item => !item.IsConnected))
            {
                Close();
            }
            else
            {
                MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "Close all camera connection before exit");
                return;
            }
        }

        private void QuitCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DebugMode && (BaslerCam == null || !BaslerCam.IsConnected) && (BaslerCams == null || BaslerCams.All(item => !item.IsConnected));
        }

        private void QuitWidow_Command(object sender, ExecutedRoutedEventArgs e)
        {
            if (BaslerCam?.IsConnected != true && BaslerCams.All(item => item.IsConnected != true))
            {
                Close();
            }
            else
            {
                MsgInformer.AddInfo(MsgInformer.Message.MsgCode.APP, "Close all camera connection before exit");
                return;
            }
        }

    }
}
