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
using System.Data;
using System.Drawing;
using System.IO;
using MCAJawIns.content;
using System.Diagnostics;

namespace MCAJawIns.Panel
{
    /// <summary>
    /// PointsPanel.xaml 的互動邏輯
    /// </summary>
    public partial class PointsPanel : Control.CustomCard
    {
        public EngineerTab EngineerTab { get; set; }


        public AssistPoints AssistPoints { get; set; }

        public PointsPanel()
        {
            InitializeComponent();

        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            AssistPoints = DataContext as AssistPoints;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AssistPoints.Source.Clear();
        }

        //private void a_TextInput(object sender, TextCompositionEventArgs e)
        //{
        //    Debug.WriteLine($"{int.Parse(a.Text) > int.Parse(b.Text)}");

        //    if (int.Parse(a.Text) > int.Parse(b.Text))
        //    {
        //        light.Background = System.Windows.Media.Brushes.Green;
        //    }
        //    else
        //    {
        //        light.Background = System.Windows.Media.Brushes.Red;
        //    }
        //}

        private void b_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Debug.WriteLine($"{int.Parse(aaa.Text) > int.Parse(bbb.Text)}");

            if (int.TryParse(aaa?.Text, out int aValue) && int.TryParse(bbb?.Text, out int bValue) && light != null)
            {
                if (aValue > bValue)
                {
                    light.Background = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    light.Background = System.Windows.Media.Brushes.Red;
                }
            }
        }
    }
}
