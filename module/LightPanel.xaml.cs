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
using MaterialDesignThemes.Wpf;


namespace ApexVisIns.module
{
    /// <summary>
    /// LightPanel.xaml 的互動邏輯
    /// </summary>
    public partial class LightPanel : Card
    {
        public LightPanel()
        {
            InitializeComponent();
        }

        private void ComPortSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


        }

        private void ComPortConnect_Click(object sender, RoutedEventArgs e)
        {


        }

        private void ChannelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;


            Debug.WriteLine($"{listBox.SelectedItem}");
            Debug.WriteLine($"{listBox.SelectedIndex}");
        }

        private void LightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            Debug.WriteLine($"Value: {e.NewValue} {e.OldValue}");


        }
    }
}
