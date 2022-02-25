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
using ApexVisIns.Product;


namespace ApexVisIns.content
{
    /// <summary>
    /// MCAJaw.xaml 的互動邏輯
    /// </summary>
    public partial class MCAJaw : StackPanel
    {
        #region Resources
        public JawSpecGroup JawSpecGroup { get; set; }
        #endregion

        #region Variables

        #endregion

        public MCAJaw()
        {
            InitializeComponent();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            JawSpecGroup = FindResource("SpecGroup") as JawSpecGroup;

            if (JawSpecGroup.SpecCollection.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    JawSpecGroup.SpecCollection.Add(new JawSpec($"項目 {i}", i, i - 0.02 * i, i + 0.02 * i, i - 0.03 * i, i + 0.03 * i));
                }
            }
        }

        private void StackPanel_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine((sender as Button).CommandParameter);
        }
    }
}
