using System.Collections.Generic;
using Newtonsoft.Json;

namespace eurosportCapper
{
    public class Channel
    {
        [JsonProperty(PropertyName = "channellabel")]
        public string Label { get; private set; }
        [JsonProperty(PropertyName = "channellivelabel")]
        public string LiveLabel { get; private set; }
        [JsonProperty(PropertyName = "channellivesublabel")]
        public string SubLabel { get; private set; }

        [JsonProperty(PropertyName = "livestreams")]
        public List<LiveStream> LiveStreams { get; private set; }

        [JsonProperty(PropertyName = "tvschedules")]
        public List<ScheduleItem> Schedule { get; private set; }
    }
}