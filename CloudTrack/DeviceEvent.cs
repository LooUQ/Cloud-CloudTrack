using System;
using System.Collections.Generic;

namespace Loouq.CloudTrack
{
    public class DeviceEvent
    {
        public DateTime EventTime { get; set; }
        public Dictionary<string, double> EventData { get; set; }

        public DeviceEvent()
        {
            EventData = new Dictionary<string, double>();
        }
    }
}
