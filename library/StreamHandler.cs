using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.IO;

namespace esnew
{

    public class VideoSequenceData
    {
        public string SourceUrl { get; set; }
        public int SequenceNumber { get; set; }
        public byte[] RawData { get; set; }
        public int PartsInQueue { get; set; }
    }

    public class StreamHandler
    {
        private HttpClient _client;
        private string _slideUrl;

        public event EventHandler<VideoSequenceData> NewVideoSequence;

        public StreamHandler(HttpClient client, string slideUrl)
        {
            _client = client;
            _slideUrl = slideUrl;
        }

        public async Task Start()
        {
            var streams = M3UPlaylist.Parse(await _client.GetStringAsync(_slideUrl));

            // This will typically select a 720p stream with 3500 Kbps bitrate, the best on offer
            var bestStream = streams.StreamItems.OrderByDescending(s => s.Width*s.Height).First();

            var url = _slideUrl.Substring(0, _slideUrl.LastIndexOf('/') + 1);
            var itemsListSrc = await _client.GetStringAsync(url + bestStream.Path);
            var itemsList = M3UPlaylist.Parse(itemsListSrc);
            
            _client.DefaultRequestHeaders.Remove("accept");
            _client.DefaultRequestHeaders.Add("accept", "*/*");

            // Prefetch all the AES keys
            foreach (var key in itemsList.Keys)
            {
                key.Key = await _client.GetByteArrayAsync(key.Uri);
                if (key.Key == null || key.Key.Length != 16)
                    throw new Exception("Failed to retrieve AES key: " + key.Uri);
            }

            var videoBaseUrl = url + bestStream.Path;
            videoBaseUrl = videoBaseUrl.Substring(0, videoBaseUrl.LastIndexOf('/')+1);
            int i = 0;
            foreach (var video in itemsList.VideoItems)
            {
                var videoUrl = videoBaseUrl + video.Path;
                var raw = await _client.GetByteArrayAsync(videoUrl);
                i++;
                Aes crypto = Aes.Create();
                var decryptor = crypto.CreateDecryptor(video.Key.Key, video.Key.IV);
                byte[] decrypted = decryptor.TransformFinalBlock(raw, 0, raw.Length);

                if (NewVideoSequence != null)
                {
                    NewVideoSequence(this, new VideoSequenceData {
                        SourceUrl = videoUrl,
                        RawData = decrypted,
                        SequenceNumber = video.Sequence,
                        PartsInQueue = itemsList.VideoItems.Count - i
                    });
                }
            }
        }
    }

}