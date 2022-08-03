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


namespace LockPlate.Control
{
    /// <summary>
    /// TitleCard.xaml 的互動邏輯
    /// </summary>
    public partial class TitleCard : Card
    {
        public TitleCard()
        {
            InitializeComponent();
        }


        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(nameof(Title), typeof(string), typeof(TitleCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconKindProperty = DependencyProperty.RegisterAttached(nameof(IconKind), typeof(PackIconKind), typeof(TitleCard), new PropertyMetadata(PackIconKind.Abc));

        public static new readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(nameof(Content), typeof(object), typeof(TitleCard), new PropertyMetadata(null));

        public static readonly DependencyProperty FooterProperty = DependencyProperty.RegisterAttached(nameof(Footer), typeof(object), typeof(TitleCard), new PropertyMetadata(null));

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

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = Header.Focus();
        }
    }
}
