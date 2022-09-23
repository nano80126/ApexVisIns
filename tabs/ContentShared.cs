using System.Windows;
using System.Windows.Media;

namespace MCAJawIns.Tab
{
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public Visibility Visibility
        {
            get => (Visibility)GetValue(VisibilityProperty);
            set => SetValue(VisibilityProperty, value);
        }


        public SolidColorBrush Background
        {
            get => (SolidColorBrush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(BindingProxy));


        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(nameof(Background), typeof(SolidColorBrush), typeof(BindingProxy));
    }
}
