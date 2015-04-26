using Newtonsoft.Json;

namespace eurosportCapper
{
    public class LiveStream
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }
        [JsonProperty(PropertyName = "url")]
        public string URL { get; private set; }
        [JsonProperty(PropertyName = "securedurl")]
        public string SecuredURL { get; private set; }
        [JsonProperty(PropertyName = "label")]
        public string Label { get; private set; }
        [JsonProperty(PropertyName = "audio")]
        public string Audio { get; private set; }

        public StreamHandler Stream()
        {
            return new StreamHandler(this);
        }
    }
}