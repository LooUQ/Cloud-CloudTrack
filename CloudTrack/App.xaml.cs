using System;
using System.Diagnostics;
using System.Windows;

namespace Loouq.CloudTrack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static EventStream EventStream { get; private set; }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            bool configLoaded = AppConfig.LoadConfig();
            if (!configLoaded || AppConfig.SeriesConfigurations.Count == 0)
            {
                var dialogBoxResult = MessageBox.Show(
                                        "Unable to configure data series based on configuration in the ChartSettings.json file. CloudTrack will now close.",
                                        "LooUQ CloudTrack",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Asterisk
                );
                Shutdown();
                Environment.Exit(0);
            }

            var streamAvailable = false;
            var validateDataSeries = AppConfig.ValidateDataSeries.ToLower().Contains("true");
            EventStream = new EventStream(validateDataSeries);
            try
            {
                if (await EventStream.Open()) {
                    streamAvailable = true;
                }
                EventStream.DataSeriesMissingDetected += ShowDataSeriesValidationWarning;
            }
            catch (Exception)
            { }
            if (!streamAvailable)
            {
                var dialogBoxResult = MessageBox.Show(
                        "Unable to open LooUQ Cloud event stream using the information in the ChartSettings.json, CloudTrack will now close.",
                        "LooUQ CloudTrack",
                        MessageBoxButton.OK,
                        MessageBoxImage.Asterisk
                );
                Shutdown();
                Environment.Exit(0);
            }


            // app config and data layer are now available start UI 
            MainWindow = new MainWindow();
            MainWindow.Show();

            Current.MainWindow.Closed += (s, a) => Shutdown();
        }

        public static void ShowDataSeriesValidationWarning(object sender, string validationMessage)
        {
            Debug.WriteLine("data series missing defined element");
        }
    }
}
