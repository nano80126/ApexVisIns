using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace MCAJawIns.Control
{
    public class OutlineTextBlock : TextBlock
    {
        static OutlineTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutlineTextBlock), new FrameworkPropertyMetadata(typeof(OutlineTextBlock)));
        }

        public static readonly DependencyProperty ContaintProperty =
            DependencyProperty.Register(nameof(Text), typeof(ControlTemplate), typeof(OutlineTextBlock), new PropertyMetadata(null));

        /// <summary>
        /// Text
        /// </summary>
        public ControlTemplate Containt
        {
            get => (ControlTemplate)GetValue(ContaintProperty);
            set => SetValue(ContaintProperty, value);
        }




    }
}
