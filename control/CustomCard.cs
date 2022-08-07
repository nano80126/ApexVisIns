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

namespace LockPlate.Control
{
    //[System.Windows.Markup.ContentProperty(nameof(Footer))]
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

        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(nameof(Title), typeof(string), typeof(CustomCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconKindProperty = DependencyProperty.RegisterAttached(nameof(IconKind), typeof(PackIconKind), typeof(CustomCard), new PropertyMetadata(PackIconKind.Abc));

        //private static new readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(nameof(Content), typeof(DependencyObject), typeof(CustomCard), new PropertyMetadata(null));

        public static readonly DependencyProperty BodyPaddingProperty =
            DependencyProperty.RegisterAttached(nameof(BodyPadding), typeof(Thickness), typeof(CustomCard), 
                new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer), typeof(object), typeof(CustomCard), new PropertyMetadata(null));

        public static readonly DependencyProperty FooterTemplateProperty = DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(CustomCard), new PropertyMetadata(null));


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
        //public new DependencyObject Content
        //{
        //    get => (DependencyObject)GetValue(ContentProperty);
        //    set => SetValue(ContentProperty, value);
        //}

        public Thickness BodyPadding
        {
            get => (Thickness)GetValue(BodyPaddingProperty);
            set => SetValue(BodyPaddingProperty, value);
        }

        /// <summary>
        /// Card Footer or Action
        /// </summary>
        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        public DataTemplate FooterTemplate
        {
            get => (DataTemplate)GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }

        //public static void SetFooter(DependencyObject target, object value)
        //{
        //    target.SetValue(FooterProperty, value);
        //}

        //public static object GetFooter(DependencyObject target)
        //{
        //    return target.GetValue(FooterProperty);
        //}

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // DockPanel dockPanel = GetTemplateChild("Panel") as DockPanel;
            DockPanel dockPanel = Template.FindName("Panel", this) as DockPanel;
            dockPanel.MouseDown += DockPanel_MouseDown;
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            _ = (Template.FindName("Header", this) as DockPanel).Focus();
        }
    }
}
