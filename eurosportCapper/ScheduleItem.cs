using System;
using Newtonsoft.Json;

namespace eurosportCapper
{
    public class ScheduleItem
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "shortname")]
        public string ShortName { get; private set; }

        [JsonProperty(PropertyName = "duration")]
        public int DurationRaw { get; private set; }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromMinutes(DurationRaw); }
        }

        [JsonProperty(PropertyName = "enddate")]
        public ScheduleDate EndDate { get; private set; }

        [JsonProperty(PropertyName = "startdate")]
        public ScheduleDate StartDate { get; private set; }

        [JsonProperty(PropertyName = "hd")]
        public bool IsHD { get; private set; }

        [JsonProperty(PropertyName = "sport")]
        public Sport Sport { get; private set; }

        [JsonProperty(PropertyName = "transmissiontypename")]
        public string TransmissionType { get; private set; }

        public bool IsCurrentlyRunning
        {
            get { return DateTime.Now >= StartDate.DateTime && DateTime.Now <= EndDate.DateTime; }
        }

        public bool IsLiveTransmission { get { return TransmissionType.Contains("Live"); } }
        public bool IsHighlights { get { return TransmissionType.Contains("Highlight"); } }

    }
}