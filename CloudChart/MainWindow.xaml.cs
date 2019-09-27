using System.ComponentModel;
using System.Windows;

namespace Loouq.CloudChart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ChartTitle { get; set; }
        public EventStream EventStream { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            EventStream = App.EventStream;

            ChartTitle = string.IsNullOrEmpty(AppConfig.ChartTitle) ? "IoT Data" : AppConfig.ChartTitle;

            DataContext = this;
        }

        public void OnButtonClick_Clear(object sender, RoutedEventArgs e)
        {
            Chart.ClearChartData();
            EventStream.ClearLimits();
        }
    }
}
