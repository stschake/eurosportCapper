using Newtonsoft.Json;

namespace eurosportCapper
{
    public class Sport
    {
        [JsonProperty(PropertyName="name")]
        public string Name { get; private set; }

        public bool IsCycling
        {
            get { return Name.Contains("Cycling"); }
        }
    }
}