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
using MCAJawIns.Tab;


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

        }
    }
}
