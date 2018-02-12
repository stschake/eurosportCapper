using System;
using System.Collections.Generic;
using System.Globalization;

namespace esnew 
{

    public class M3UStreamItem
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int AverageBandwidth { get; set; }
        public int Bandwidth { get; set; }
        public string Path { get; set; }
    }  

    public class M3UEncryptionKey
    {
        // "AES-128"
        public string Method { get; set; }

        public string Uri { get; set; }

        public byte[] IV { get; set; }

        // Not set by M3UPlaylist, need to fetch it yourself
        public byte[] Key { get; set; }
    }

    public class M3UVideoItem {
        public string Path { get; set; }
        public M3UEncryptionKey Key { get; set; }
        public int Sequence { get; set; }
    }

    public class M3UPlaylist
    {
        public List<M3UVideoItem> VideoItems { get; set; } = new List<M3UVideoItem>();
        public List<M3UStreamItem> StreamItems { get; set; } = new List<M3UStreamItem>();
        public List<M3UEncryptionKey> Keys { get; set; } = new List<M3UEncryptionKey>();

        public int MediaSequence { get; set; }

        private static Dictionary<string, string> ParseProperties(string line)
        {
            var ret = new Dictionary<string, string>();
            line = line.Substring(line.IndexOf(':') + 1);
            bool literal = false;
            for (int i = 0; i < line.Length;)
            {
                string key = "", value = "";                
                for (; i < line.Length; i++) {
                    if (line[i] == '=')
                        break;
                    key += line[i];
                }
                i++;
                for (; i < line.Length; i++) {
                    if (line[i] == '\"') {
                        literal = !literal;
                        continue;
                    }
                    if (line[i] == ',' && !literal)
                        break;
                    value += line[i];
                }
                i++;
                ret.Add(key, value);
            }
            return ret;
        }

        public static M3UPlaylist Parse(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != "#EXTM3U")
                return null;

            var ret = new M3UPlaylist();
            int seq = 0;
            M3UEncryptionKey currentKey = null;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("#EXT-X-STREAM-INF:") && (i+1) != lines.Length) {
                    var props = ParseProperties(line);
                    var item = new M3UStreamItem { Path = lines[++i] };
                    if (props.ContainsKey("RESOLUTION")) {
                        var rect = props["RESOLUTION"].Split('x');
                        item.Width = int.Parse(rect[0]);
                        item.Height = int.Parse(rect[1]);
                    }
                    if (props.ContainsKey("BANDWIDTH")) {
                        item.Bandwidth = int.Parse(props["BANDWIDTH"]);
                    }
                    if (props.ContainsKey("AVERAGE-BANDWIDTH")) {
                        item.AverageBandwidth = int.Parse(props["AVERAGE-BANDWIDTH"]);
                    }
                    ret.StreamItems.Add(item);
                }
                else if (line.StartsWith("#EXT-X-MEDIA-SEQUENCE")) {
                    ret.MediaSequence = int.Parse(line.Split(':')[1]);
                }
                else if (line.StartsWith("#EXT-X-KEY")) {
                    var props = ParseProperties(line);
                    currentKey = new M3UEncryptionKey
                    { 
                        Method = props["METHOD"],
                        Uri = props["URI"],
                    };
                    var iv = props["IV"];
                    iv = iv.Substring(2);
                    currentKey.IV = new byte[iv.Length/2];
                    for (int j = 0; j < (iv.Length/2); j++)
                    {
                        currentKey.IV[j] = byte.Parse(iv[j*2+0].ToString() + iv[j*2+1], NumberStyles.HexNumber);
                    }
                    ret.Keys.Add(currentKey);
                }
                else if (line.StartsWith("#EXTINF") && (i+1) != lines.Length) {
                    ret.VideoItems.Add(new M3UVideoItem {
                        Path = lines[++i],
                        Sequence = ret.MediaSequence + (seq++),
                        Key = currentKey 
                    });
                }
            }
            return ret;
        }
    }

}
