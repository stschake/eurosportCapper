using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace eurosportCapper
{

    public class M3UItem
    {
        public string URL { get; private set; }

        public int Bandwidth { get; private set; }

        private bool IsAudioOnly { get; set; }

        public M3UItem(string url, int bandwidth, bool audioOnly)
        {
            URL = url;
            Bandwidth = bandwidth;
            IsAudioOnly = audioOnly;
        }
    }

    public class StreamPart
    {
        public string Path { get; private set; }
        public int Number { get; private set; }

        public StreamPart(string path, int number)
        {
            Path = path;
            Number = number;
        }
    }

    public class StreamDataEvent : EventArgs
    {
        public byte[] Data { get; private set; }

        public int PartNumber { get; private set; }

        public StreamDataEvent(byte[] data, int partNumber)
        {
            Data = data;
            PartNumber = partNumber;
        }
    }

    public class StreamHandler
    {
        private readonly HttpClient _client;
        private readonly LiveStream _stream;
        private readonly HttpClientHandler _handler = new HttpClientHandler();

        public event EventHandler<StreamDataEvent> OnStreamData;

        private string FetchString(string url)
        {
            var resp = _client.GetAsync(url).Result;
            IEnumerable<string> cookies;
            resp.Headers.TryGetValues("Set-Cookie", out cookies);
            if (cookies != null)
            {
                foreach (var cookie in cookies)
                    _handler.CookieContainer.SetCookies(new Uri(url), cookie);
            }
            return resp.Content.ReadAsStringAsync().Result;
        }

        private bool Download(string url, string target, int number)
        {
            try
            {
                var task = _client.GetByteArrayAsync(url);
                if (!task.Wait(TimeSpan.FromSeconds(7)))
                {
                    Console.WriteLine("Download of " + target + " failed with timeout");
                    return false;
                }
                var data = task.Result;
                if (OnStreamData != null)
                    OnStreamData(this, new StreamDataEvent(data, number));
                File.WriteAllBytes(target, data);
                Console.WriteLine("Downloaded " + target);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Download of " + target + " failed");
                return false;
            }
        }

        private static List<M3UItem> ParseM3U(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "#EXTM3U")
                return null;
            var ret = new List<M3UItem>();
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!line.StartsWith("#") || !line.Contains("BANDWIDTH="))
                    continue;
                var bwPart = line.Substring(line.IndexOf("BANDWIDTH=") + 10);
                if (bwPart.Contains(','))
                    bwPart = bwPart.Substring(0, bwPart.IndexOf(','));
                int bandwidth = int.Parse(bwPart);

                string url = null;
                bool audioOnly = false;
                int j = i + 1;
                for (; j < lines.Length; j++)
                {
                    var subLine = lines[j];
                    if (subLine.StartsWith("http"))
                    {
                        url = subLine;
                        break;
                    }
                    if (subLine.Contains("# audio-only"))
                        audioOnly = true;
                }
                if (j != lines.Length)
                {
                    ret.Add(new M3UItem(url, bandwidth, audioOnly));
                    i = j;
                }
            }
            return ret;
        }

        public StreamHandler(LiveStream stream)
        {
            _handler.CookieContainer = new CookieContainer();
            _client = Utility.CreateClient(_handler);
            _stream = stream;
        }

        private List<StreamPart> ParseParts(string playlist)
        {
            const string sequenceNumberStart = "#EXT-X-MEDIA-SEQUENCE:";

            var lines = playlist.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "#EXTM3U")
                return null;
            int seq = 1;
            var ret = new List<StreamPart>();
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    if (line.StartsWith(sequenceNumberStart))
                        seq = int.Parse(line.Substring(sequenceNumberStart.Length));
                }
                else
                    ret.Add(new StreamPart(line, seq++));
            }

            return ret;
        }

        public void Start()
        {
            string url = _stream.SecuredURL;
            if (string.IsNullOrEmpty(url))
                url = _stream.URL;
            if (string.IsNullOrEmpty(url))
                throw new InvalidDataException("Invalid livestream");

            var playlistsData = FetchString(url);
            if (string.IsNullOrEmpty(playlistsData))
                throw new InvalidDataException("No playlists received");

            var playlists = ParseM3U(playlistsData);
            if (playlists == null || playlists.Count == 0)
                throw new InvalidDataException("Parsing yielded no playlists");

            M3UItem best = playlists.OrderByDescending(p => p.Bandwidth).First();
            Console.WriteLine("Using stream with bandwidth = " + best.Bandwidth);
            int lastPart = 0;
            while (true)
            {
                var prefix = best.URL.Substring(0, best.URL.LastIndexOf('/') + 1);
                var streamData = FetchString(best.URL);
                var lastFetch = DateTime.Now;
                if (string.IsNullOrEmpty(streamData))
                    throw new InvalidDataException("No data received");
                Console.WriteLine(streamData);

                var parts = ParseParts(streamData);
                if (parts == null || parts.Count == 0)
                    throw new InvalidDataException("No parts received");

                var failed = parts.Where(p => p.Number > lastPart)
                    .AsParallel()
                    .Where(p => !Download(prefix + p.Path, p.Number + ".ts", p.Number));

                if (failed.Any())
                {
                    Console.WriteLine("At least one part failed to download, retrying");
                    var retrySuccess =
                        failed.AsParallel().Select(p => Download(prefix + p.Path, p.Number + ".ts", p.Number)).All(b => b);
                    if (!retrySuccess)
                        Console.WriteLine("Some part(s) still failed, skipping");
                }
                else
                {
                    lastPart = parts.Max(p => p.Number);

                    var elapsedSec = (DateTime.Now - lastFetch).TotalSeconds;
                    if (elapsedSec < 10)
                    {
                        Console.WriteLine("Sleeping " + (10 - elapsedSec) + "s");
                        Thread.Sleep(TimeSpan.FromSeconds(10 - elapsedSec));
                    }
                }
            }
        }
    }

}