using System;

namespace Loouq.CloudTrack
{
    public class EventModel
    {
        public DateTimeOffset EventTime { get; set; }
        public double EventValue { get; set; }

        public EventModel(DateTimeOffset eventTime, double eventValue)
        {
            EventTime = eventTime;
            EventValue = eventValue;
        }
    }
}
