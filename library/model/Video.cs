using System;
using System.Collections.Generic;

namespace esnew.model
{

    public class OnDemandContent
    {
        public List<Video> Videos { get; set; } = new List<Video>();
        public List<Airing> Airings { get; set; } = new List<Airing>();
    }

    public class TitleTag
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string DisplayName { get; set; }
    }

    public class VideoTitle
    {
        public string Title { get; set; }
        public string TitleBrief { get; set; }
        public string EpisodeName { get; set; }
        public string SummaryLong { get; set; }
        public string SummaryShort { get; set; }
        public List<TitleTag> Tags { get; set; }
    }

    public class VideoMedia
    {
        public List<PlaybackUrl> PlaybackUrls { get; set; }
    }

    public class Video
    {
        public string ContentId { get; set; }        
        public string ProgramId { get; set; }
        public TimeSpan RunTime { get; set; }
        public DateTime Appears { get; set; }
        public DateTime Expires { get; set; }
        public List<Genre> Genres { get; set; }
        public List<VideoTitle> Titles { get; set; }
        public List<VideoMedia> Media { get; set; }

        
    }

}