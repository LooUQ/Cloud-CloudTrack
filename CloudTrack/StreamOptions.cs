using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace Loouq.CloudTrack
{
    class StreamOptions
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("streamType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public StreamTypes StreamType { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("filter", NullValueHandling = NullValueHandling.Ignore)]
        public string Filter { get; set; }


        public StreamOptions()
        {
            StartTime = DateTime.UtcNow;
        }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }


        public enum StreamTypes
        {
            [EnumMember(Value = "live")]
            Live = 1,
            [EnumMember(Value = "liveLastEvent")]
            LiveLast = 2,
            [EnumMember(Value = "replay")]
            Replay = 11,
            [EnumMember(Value = "replayLastEvent")]
            ReplayLast = 12,
            [EnumMember(Value = "replayEventIndex")]
            ReplayIndex = 13
        }
    }


}
