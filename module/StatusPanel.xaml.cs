using System;
using System.Collections.Generic;
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
using MaterialDesignThemes.Wpf;

namespace MCAJawIns.module
{
    /// <summary>
    /// StatusPanel.xaml 的互動邏輯
    /// </summary>
    public partial class StatusPanel : Card
    {
        public MainWindow MainWindow { get; set; }


        public StatusPanel()
        {
            InitializeComponent();
        }
    }
}
