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
    }

    public class VisibilityBindingProxcy : BindingProxy
    {
        public static readonly DependencyProperty dependencyProperty = DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(BindingProxy));
        public Visibility Visibility
        {
            get => (Visibility)GetValue(dependencyProperty);
            set => SetValue(dependencyProperty, value);
        }
    }

    public class ColorBindingProxy : BindingProxy
    {
        public static readonly DependencyProperty dependencyProperty = DependencyProperty.Register(nameof(Color), typeof(SolidColorBrush), typeof(BindingProxy));
        public SolidColorBrush Color
        {
            get => (SolidColorBrush)GetValue(dependencyProperty);
            set => SetValue(dependencyProperty, value);
        }
    }

    public class TextBindngProxy : BindingProxy
    {
        public static readonly DependencyProperty dependencyProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBindngProxy));
        public string Text
        {
            get => (string)GetValue(dependencyProperty);
            set => SetValue(dependencyProperty, value);
        }
    }
}
