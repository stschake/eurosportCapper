using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace esnew.model
{

    public class MediaConfig
    {
        public string State { get; set; }
        public string ProductType { get; set; }
        public string Type { get; set; }
    }

    public class TitleVariant
    {
        public string Language { get; set; }
        public string Title { get; set; }
        public string DescriptionLong { get; set; }
        public string DescriptionShort { get; set; }
        public string EpisodeName { get; set; }
    }

    public class PlaybackUrl
    {
        [JsonProperty(PropertyName = "href")]
        public string URL { get; set; }

        [JsonProperty(PropertyName = "rel")]
        public RelationType Relation { get; set; }

        [JsonProperty(PropertyName = "templated")]
        public bool IsTemplated { get; set; }

        public string WithScenario(Scenario scenario)
        {
            return URL.Replace("{scenario}", scenario.AsTemplateValue());
        }
    }

    public enum Scenario
    {
        BrowserUnlimited
    }

    public static class ScenarioExtensions
    {
        public static string AsTemplateValue(this Scenario scenario)
        {
            switch (scenario)
            {
                case Scenario.BrowserUnlimited:
                    return "browser~unlimited";
                default:
                    return null;
            }
        }
    }

    public enum RelationType
    {
        Video,
        Linear,
        Event
    }

    public class Channel
    {
        public int Id { get; set; }
        public string CallSign { get; set; }
        public int PartnerId { get; set; }
    }

    public class Airing
    {
        [JsonProperty(PropertyName = "liveBroadcast")]
        public bool IsLiveBroadcast { get; set; }

        [JsonProperty(PropertyName = "linear")]
        public bool IsLinear { get; set; }

        public string ContentId { get; set; }
        public string MediaId { get; set; }
        
        public string ProgramId { get; set; }

        public TimeSpan RunTime { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime Expires { get; set; }

        public Channel Channel { get; set; }

        public List<Genre> Genres { get; set; }

        public MediaConfig MediaConfig { get; set; }

        public List<TitleVariant> Titles { get; set; }

        public TitleVariant EnglishTitle 
        {
            get
            {
                return Titles.FirstOrDefault(tv => tv.Language.Equals("en"));
            }
        }

        public List<PlaybackUrl> PlaybackUrls { get; set; }

        public PlaybackUrl LinearPlaybackUrl {
            get { return PlaybackUrls.FirstOrDefault(pu => pu.Relation.Equals(RelationType.Linear)); }
        }

        public PlaybackUrl VideoPlaybackUrl {
            get { return PlaybackUrls.FirstOrDefault(pu => pu.Relation.Equals(RelationType.Video)); }
        }

        public PlaybackUrl EventPlaybackUrl {
            get { return PlaybackUrls.FirstOrDefault(pu => pu.Relation.Equals(RelationType.Event)); }
        }

        public PlaybackUrl BestPlaybackUrl {
            get
            {
                return LinearPlaybackUrl ?? VideoPlaybackUrl ?? EventPlaybackUrl;
            }
        }
    }

}