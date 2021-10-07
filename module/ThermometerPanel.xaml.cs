using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows;

namespace ApexVisIns.module
{
    /// <summary>
    /// Thermometer.xaml 的互動邏輯
    /// </summary>
    public partial class ThermometerPanel : Card
    {
        public MainWindow MainWindow { get; set; }

        #region Long Life Worker
        public Thermometer Thermometer;
        #endregion

        public ThermometerPanel()
        {
            InitializeComponent();
        }
        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            // move to main and bind 
            if (Thermometer == null)
            {
                Thermometer = TryFindResource(nameof(Thermometer)) as Thermometer;
                Thermometer?.OpenSerialPort();

                // 綁定回 MainWindow
                MainWindow.Thermometer = Thermometer;
            }
        }

        private void OpenSerialPortBtn_Click(object sender, RoutedEventArgs e)
        {
            //if (MainWindow != null)
            //{
            Thermometer.Initialize();
            Thermometer.OpenSerialPort();
            //}
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Thermometer != null)
            {
                Thermometer.WorkerResume();
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Thermometer != null)
            {
                Thermometer.WorkerPause();
            }
        }
    }
}
