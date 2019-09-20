using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Loouq.CloudTrack
{
    public static class AppConfig
    {
        public static string UserName { get; private set; }
        public static string Password { get; private set; }
        public static string EventStreamsUrl { get; private set; }
        public static string ValidateDataSeries { get; private set; }
        public static int PrefetchMinutes { get; private set; }
        public static int SampleIntervalSeconds { get; private set; }
        public static string ChartTitle { get; private set; }
        public static int TimezoneOffset { get; private set; }
        public static string ChartTimeFormat { get; private set; }
        public static int SampleCount { get; private set; }
        public static int ChartDurationSeconds { get; private set; }
        public static string DeviceId { get; private set; }
        public static string Filter { get; private set; }
        public static List<SeriesConfig> SeriesConfigurations { get; private set; }
        public static string LimitsMinPath { get; private set; }
        public static string LimitsMaxPath { get; private set; }


        private static double defaultLineSmoothness;


        public static bool LoadConfig()
        {
            EventStreamsUrl = ConfigurationManager.AppSettings["streamsUrl"];
            PrefetchMinutes = int.TryParse(ConfigurationManager.AppSettings["prefetchMinutes"], out int prefetchMin) ? prefetchMin : 0;
            ValidateDataSeries = ConfigurationManager.AppSettings["validateDataSeries"];
            defaultLineSmoothness = double.TryParse(ConfigurationManager.AppSettings["lineSmoothness"], out double smoothness) ? smoothness : 0;
            SampleIntervalSeconds = int.TryParse(ConfigurationManager.AppSettings["sampleIntervalSeconds"], out int intervalSecs) ? intervalSecs : 300;
            SampleCount = int.TryParse(ConfigurationManager.AppSettings["sampleCount"], out int pointCount) ? pointCount : 25;

            return LoadChartSettings();
        }

        private static bool LoadChartSettings()
        {
            SeriesConfigurations = new List<SeriesConfig>();
            try
            {
                var chartSettingsPath = ConfigurationManager.AppSettings["chartSettingsPath"];
                chartSettingsPath = string.IsNullOrEmpty(chartSettingsPath) ? @".\" : chartSettingsPath;

                var chartSettings = File.ReadAllText(chartSettingsPath + "ChartSettings.json");
                var settingsJObj = JObject.Parse(chartSettings);

                UserName = (string)settingsJObj["userName"];
                Password = (string)settingsJObj["password"];

                ChartTitle = (string)settingsJObj["chart"]["title"];
                TimezoneOffset = int.TryParse((string)settingsJObj["chart"]["timezoneOffset"], out int tzOffset) ? tzOffset : 0;
                ChartTimeFormat = (string)settingsJObj["chart"]["timeFormat"] ?? "HH:mm:ss";
                SampleCount = int.TryParse((string)settingsJObj["chart"]["sampleCount"], out int sampleCount) ? sampleCount : SampleCount;

                DeviceId = (string)settingsJObj["stream"]["deviceId"];
                Filter = (string)settingsJObj["stream"]["filter"];
                PrefetchMinutes = int.TryParse((string)settingsJObj["stream"]["samplePrefetchMinutes"], out int prefetchMin) ? prefetchMin : PrefetchMinutes;
                SampleIntervalSeconds = int.TryParse((string)settingsJObj["stream"]["sampleIntervalSeconds"], out int intervalSec) ? intervalSec : SampleIntervalSeconds;

                var seriesConfig = (JArray)settingsJObj["seriesConfig"];
                foreach (var series in seriesConfig)
                {
                    if (ValidatePath((string)series["path"]))
                    {
                        Brush strokeColor;
                        try
                        {
                            strokeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)series["color"]));
                        }
                        catch (Exception)
                        {
                            strokeColor = null;
                        }
                        var config = new SeriesConfig()
                        {
                            Name = (string)series["name"],
                            Units = (string)(series["units"] ?? ""),
                            Path = (string)series["path"],
                            StrokeColor = strokeColor,
                            LineSmoothness = (double)(series["lineSmoothness"] ?? defaultLineSmoothness)
                        };
                        config.Name = (string.IsNullOrEmpty(config.Name)) ? config.Path.Split('.').Last() : config.Name;
                        SeriesConfigurations.Add(config);

                        if (series["min"] != null)
                            LimitsMinPath = (string)series["path"];
                        if (series["max"] != null)
                            LimitsMaxPath = (string)series["path"];
                    }
                    else
                    {
                        var dialogBoxResult = MessageBox.Show(
                            string.Format("Data series path: {0} is empty or invalid. No data for this series will be processed", (string)series["path"]),
                            "CloudTrack: ChartSettings.json Invalid",
                            MessageBoxButton.OK,
                            MessageBoxImage.Asterisk
                        );
                    }
                }

                ChartDurationSeconds = (SampleCount * SampleIntervalSeconds);

                return true;
            }
            catch (Exception)           // just suck up any exceptions, app init will detect problems in chart settings
            {
                return false;
            }
        }

        private static bool ValidatePath(string elementPath)
        {
            return !string.IsNullOrEmpty(elementPath) && (elementPath.StartsWith("eventMeta") || elementPath.StartsWith("displayValue") || elementPath.StartsWith("eventBody.telemetry"));
        }
    }
}
