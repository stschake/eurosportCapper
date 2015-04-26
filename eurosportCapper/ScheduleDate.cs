using System;
using Newtonsoft.Json;

namespace eurosportCapper
{
    public class ScheduleDate
    {
        [JsonProperty(PropertyName = "date")]
        public string Date { get; private set; }

        [JsonProperty(PropertyName = "technicaldate")]
        public string TechnicalData { get; private set; }

        [JsonProperty(PropertyName = "time")]
        public string Time { get; private set; }

        [JsonProperty(PropertyName = "datetime")]
        public string DateTimeRaw { get; private set; }

        public DateTime DateTime
        {
            get
            {
                string s = DateTimeRaw.Substring(DateTimeRaw.IndexOf('(')+1);
                long ms = long.Parse(s.Substring(0, s.IndexOf('+')));
                string off = s.Substring(s.IndexOf('+')+1);
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                epoch = epoch.AddMilliseconds(ms);
                epoch = epoch.AddHours(int.Parse(off.Substring(0, 2)));
                return epoch;
            }
        }
    }
}