using System;
using esnew;
using Xunit;

namespace test
{
    public class M3UPlaylistTest
    {
        [Fact]
        public void TestItemList()
        {
            var input =
@"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:6
#EXT-X-MEDIA-SEQUENCE:297252
#EXT-X-PROGRAM-DATE-TIME:2018-02-11T00:19:08.595Z
#EXT-X-KEY:METHOD=AES-128,URI=""testA"",IV=0x4F8C178221D11047303F0D1F08D27502
#EXTINF:5,
042/00/19/08_595.ts
#EXT-X-KEY:METHOD=AES-128,URI=""testB"",IV=0x83062521FA3B76DF3F9815F1E5B8E4E3
#EXTINF:5,
042/00/20/03_595.ts";

            var list = M3UPlaylist.Parse(input);
            Assert.NotNull(list);
            Assert.Empty(list.StreamItems);
            Assert.Equal(2, list.VideoItems.Count);
            Assert.Equal(297252, list.MediaSequence);
            var itemA = list.VideoItems[0];
            Assert.NotNull(itemA.Key);
            Assert.Equal("AES-128", itemA.Key.Method);
            Assert.Equal("testA", itemA.Key.Uri);
            Assert.Equal(new byte[] {0x4F, 0x8C, 0x17, 0x82, 0x21, 0xD1, 0x10, 0x47, 0x30, 0x3f, 0x0d, 0x1f, 0x8, 0xd2, 0x75, 0x02}, itemA.Key.IV);
            Assert.Equal("042/00/19/08_595.ts", itemA.Path);
            Assert.Equal(297252, itemA.Sequence);
            var itemB = list.VideoItems[1];
            Assert.NotNull(itemB.Key);
            Assert.NotEqual(itemA.Key, itemB.Key);
            Assert.Equal("AES-128", itemB.Key.Method);
            Assert.Equal("testB", itemB.Key.Uri);
            Assert.Equal("042/00/20/03_595.ts", itemB.Path);
            Assert.Equal(297253, itemB.Sequence);
            Assert.Equal(2, list.Keys.Count);
        }

        [Fact]
        public void TestStreamList()
        {
            var input = 
@"#EXTM3U
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-STREAM-INF:RESOLUTION=896x504,AVERAGE-BANDWIDTH=1900000,BANDWIDTH=2200000,CODECS=""avc1.4d001f,mp4a.40.2"",CLOSED-CAPTIONS=NONE
1800K/1800_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=320x180,AVERAGE-BANDWIDTH=188000,BANDWIDTH=202000,CODECS=""avc1.42001e,mp4a.40.2"",CLOSED-CAPTIONS=NONE
192K/192_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=384x216,AVERAGE-BANDWIDTH=514000,BANDWIDTH=565000,CODECS=""avc1.42001e,mp4a.40.2"",CLOSED-CAPTIONS=NONE
450K/450_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=512x288,AVERAGE-BANDWIDTH=895000,BANDWIDTH=1000000,CODECS=""avc1.42001e,mp4a.40.2"",CLOSED-CAPTIONS=NONE
800K/800_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=640x360,AVERAGE-BANDWIDTH=1300000,BANDWIDTH=1450000,CODECS=""avc1.4d001f,mp4a.40.2"",CLOSED-CAPTIONS=NONE
1200K/1200_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=960x540,AVERAGE-BANDWIDTH=2600000,BANDWIDTH=3000000,CODECS=""avc1.4d001f,mp4a.40.2"",CLOSED-CAPTIONS=NONE
2500K/2500_slide.m3u8
#EXT-X-STREAM-INF:RESOLUTION=1280x720,AVERAGE-BANDWIDTH=3600000,BANDWIDTH=4250000,CODECS=""avc1.640028,mp4a.40.2"",CLOSED-CAPTIONS=NONE
3500K/3500_slide.m3u8";

            var list = M3UPlaylist.Parse(input);
            Assert.NotNull(list);
            Assert.Equal(7, list.StreamItems.Count);
            Assert.Empty(list.VideoItems);
            var stream = list.StreamItems[0];
            Assert.Equal(896, stream.Width);
            Assert.Equal(504, stream.Height);
            Assert.Equal(1900000, stream.AverageBandwidth);
            Assert.Equal(2200000, stream.Bandwidth);
            Assert.Equal("1800K/1800_slide.m3u8", stream.Path);
        }
    }
}
