using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Loouq.CloudChart
{

    /// <summary>
    /// Interaction logic for LineChart.xaml
    /// </summary>
    public partial class EventLineChart : UserControl
    {
        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }


        public double XAxisMin
        {
            get { return _xAxisMin; }
            set
            {
                _xAxisMin = value;
                OnPropertyChanged("XAxisMin");
            }
        }
        private double _xAxisMin;

        public double XAxisMax
        {
            get { return _xAxisMax; }
            set
            {
                _xAxisMax = value;
                OnPropertyChanged("XAxisMax");
            }
        }
        private double _xAxisMax;


        public double XAxisUnit => TimeSpan.TicksPerSecond;


        public EventLineChart()
        {
            try
            {
                InitializeComponent();

                var eventMapper = Mappers.Xy<EventModel>()
                    .X(eventModel => eventModel.EventTime.Ticks)
                    .Y(eventModel => eventModel.EventValue);
                XFormatter = value => XAxisFormatter(value);
                YFormatter = value => YAxisFormatter(value);

                SeriesCollection = new SeriesCollection(eventMapper);

                foreach (var series in AppConfig.SeriesConfigurations)
                {
                    SeriesCollection.Add(new LineSeries
                    {
                        Title = series.Name,
                        Stroke = series.StrokeColor,
                        Fill = System.Windows.Media.Brushes.Transparent,
                        StrokeThickness = 2,
                        LineSmoothness = series.LineSmoothness,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 5,
                        Values = new ChartValues<EventModel>()
                    });
                }

                DataContext = this;
                SetXAxisLimits(DateTime.Now, DateTime.Now.AddSeconds(AppConfig.ChartDurationSeconds));

                DispatcherTimer dataRenderTimer = new DispatcherTimer();
                dataRenderTimer.Tick += new EventHandler(DataRenderTimer_Tick);
                dataRenderTimer.Interval = TimeSpan.FromSeconds(AppConfig.SampleIntervalSeconds);
                dataRenderTimer.Start();

                // fire one-shot timer to render data as quickly as possible (delay 1 second for CTOR to finish)
                using (Timer dataStreamStartTimer = new Timer(_ => DataRenderTimer_Tick(null, null), null, 1000, Timeout.Infinite)) { }
            }
            catch (Exception)
            {
                throw;
            }
        }


        private async void DataRenderTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                DateTimeOffset maxTime = DateTimeOffset.Now;
                var deviceEvents = await App.EventStream.GetEvents();                   // returns all events from device since last GetEvents()
                if (deviceEvents.Count > 0)
                {
                    foreach (var deviceEvent in deviceEvents)
                    {
                        foreach (var dataPoint in deviceEvent.EventData)
                        {
                            var series = SeriesCollection.Where(w => w.Title == dataPoint.Key).Single();
                            var model = new EventModel(deviceEvent.EventTime, dataPoint.Value);
                            series.Values.Add(model);
                            if (series.Values.Count > AppConfig.SampleCount)
                                series.Values.RemoveAt(0);
                        }
                        maxTime = deviceEvent.EventTime;
                    }
                    DateTimeOffset minTime = ((EventModel)SeriesCollection.First().Values[0]).EventTime;
                    SetXAxisLimits(minTime, maxTime);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public void ClearChartData()
        {
            foreach (var series in SeriesCollection)
            {
                series.Values.Clear();
            }
        }


        public string XAxisFormatter(double value)
        {
            var formatString = AppConfig.ChartTimeFormat;
            var result = (new System.DateTime((long)value, DateTimeKind.Utc)).ToString(formatString);
            return result;
        }

        public string YAxisFormatter(double value)
        {
            return value.ToString("0.000");
        }


        private void SetXAxisLimits(DateTimeOffset axisMinTime, DateTimeOffset axisMaxTime)
        {
            try
            {
                XAxisMin = axisMinTime.Ticks;
                XAxisMax = axisMaxTime.Ticks;

                //// NOTE: can not get binding to Axis properties to honor OnPropertyChanged in XAML
                //// So the next couple lines force the property change in the class object
                var chartXAxis = this.EventChart.AxisX.First();
                chartXAxis.MaxValue = XAxisMax;
                chartXAxis.MinValue = XAxisMin;
            }
            catch (Exception)
            {
                throw;
            }
        }



        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            Debug.WriteLine(string.Format("EventLineChart(), OnPropertyChanged({0})", propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
