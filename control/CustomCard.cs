using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace LockPlate.Control
{
    public class CustomCard : ContentControl
    {
        // public TitleCard()
        // {
        //     InitializeComponent();
        // }

        static CustomCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomCard), new FrameworkPropertyMetadata(typeof(CustomCard)));
        }

        private static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(nameof(Title), typeof(string), typeof(CustomCard), new PropertyMetadata("Heading"));

        private static readonly DependencyProperty IconKindProperty = DependencyProperty.RegisterAttached(nameof(IconKind), typeof(PackIconKind), typeof(CustomCard), new PropertyMetadata(PackIconKind.Abc));

        private static new readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(nameof(Content), typeof(object), typeof(CustomCard), new PropertyMetadata(null));

        private static readonly DependencyProperty FooterProperty = DependencyProperty.RegisterAttached(nameof(Footer), typeof(object), typeof(CustomCard), new PropertyMetadata(null));

        /// <summary>
        /// Card Header Text
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Card Headr ICON
        /// </summary>
        public PackIconKind IconKind
        {
            get => (PackIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        /// <summary>
        /// Card Content
        /// </summary>
        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Card Footer or Action
        /// </summary>
        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }


    }
}
