using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Loouq.CloudTrack
{
    public class EventStream : INotifyPropertyChanged
    {
        private readonly CloudStream cloudStream;
        private double limitsMinValue = int.MaxValue;
        private double limitsMaxValue = int.MinValue;
        private string minText = "";
        private string maxText = "";
        private readonly bool dataSeriesValidateEnabled;
        private bool dataSeriesValidateCompleted;

        public string LimitsTitle { get; set; }
        public string LimitsText { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> DataSeriesMissingDetected;


        public EventStream(bool seriesValidateEnabled = true)
        {
            LimitsTitle = "";
            LimitsText = "";
            OnPropertyChanged("LimitsTitle");
            OnPropertyChanged("LimitsText");
            cloudStream = new CloudStream(AppConfig.EventStreamsUrl);
            this.dataSeriesValidateEnabled = seriesValidateEnabled;
        }


        public async Task<bool> Open()
        {
            var streamOptions = new StreamOptions()
            {
                DeviceId = AppConfig.DeviceId,
                StreamType = StreamOptions.StreamTypes.Live,
                Filter = AppConfig.Filter,
                StartTime = DateTime.UtcNow - TimeSpan.FromMinutes(AppConfig.PrefetchMinutes)
            };

            if (!string.IsNullOrEmpty(AppConfig.LimitsMinPath) || !string.IsNullOrEmpty(AppConfig.LimitsMaxPath))
            {
                LimitsTitle = "Limits";
                OnPropertyChanged("LimitsTitle");

            }
            return await cloudStream.Open(streamOptions.ToString(), AppConfig.UserName, AppConfig.Password);
        }


        public async Task<List<DeviceEvent>> GetEvents()
        {
            Debug.WriteLine("EventStream GetEvents()");

            const string TIMESTAMP_PATH = "eventMeta.eventTimeUtc";
            var response = new List<DeviceEvent>();
            var dataValidationMessage = "Event series missing the following configured data elements: ";
            var dataValidationFailed = false;

            try
            {
                var newLimits = false;
                var streamResponse = await cloudStream.Get();
                foreach (var eventJObj in streamResponse)
                {
                    var deviceEvent = new DeviceEvent()
                    {
                        EventTime = ((DateTime)eventJObj.SelectToken(TIMESTAMP_PATH)).AddHours(AppConfig.TimezoneOffset)
                    };
                    foreach (var series in AppConfig.SeriesConfigurations)
                    {
                        var seriesValueToken = (string)eventJObj.SelectToken(series.Path);
                        if (double.TryParse(seriesValueToken, out double seriesVal))
                        {
                            deviceEvent.EventData.Add(series.Name, seriesVal);
                            if (series.Path == AppConfig.LimitsMinPath && seriesVal < limitsMinValue)
                            {
                                newLimits = true;
                                limitsMinValue = seriesVal;
                                minText = string.Format("Min {0} = {1:0.00}{2} @ {3:MMM-d h:mm:ss tt} \n", series.Name, limitsMinValue, series.Units, deviceEvent.EventTime);
                            }
                            if (series.Path == AppConfig.LimitsMaxPath && seriesVal > limitsMaxValue)
                            {
                                newLimits = true;
                                limitsMaxValue = seriesVal;
                                maxText = string.Format("Max {0} = {1:0.00}{2} @ {3:MMM-d h:mm:ss tt}", series.Name, limitsMaxValue, series.Units, deviceEvent.EventTime);
                            }
                        }
                        else
                        {
                            deviceEvent.EventData.Add(series.Name, 0.0);
                            if (dataSeriesValidateEnabled && !dataSeriesValidateCompleted)
                            {
                                dataValidationFailed = true;
                                var delim = dataValidationMessage.EndsWith(": ") ? "" : ", ";
                                dataValidationMessage += string.Format("{0} {1} ({2})", delim, series.Name, series.Path);
                            }
                        }

                    }
                    response.Add(deviceEvent);
                    if (newLimits)
                    {
                        LimitsText = minText + maxText;
                        OnPropertyChanged("LimitsText");
                    }

                    dataSeriesValidateCompleted = dataSeriesValidateEnabled;        // signal the one time validation completed
                    if (dataValidationFailed)
                        DataSeriesMissingDetected(this, dataValidationMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in EventStream.GetEvents(): {0}", ex.Message);
            }
            return response;
        }

        public async Task<bool> Close()
        {
            return await cloudStream.Close();
        }


        public void ClearLimits()
        {
            limitsMinValue = int.MaxValue;
            limitsMaxValue = 0;
            minText = "";
            maxText = "";
            LimitsText = "";
        }


        private void OnPropertyChanged(string propertyName)
        {
            Debug.WriteLine(string.Format("EventStream OnPropertyChanged({0})", propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
