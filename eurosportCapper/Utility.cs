using System.Net.Http;

namespace eurosportCapper
{

    public static class Utility
    {

        public static HttpClient CreateClient()
        {
            var ret = new HttpClient();
            ret.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 5.0.1; Eurosport Player/2.2.2)");
            return ret;
        }

        public static HttpClient CreateClient(HttpClientHandler handler)
        {
            var ret = new HttpClient(handler);
            ret.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 5.0.1; Eurosport Player/2.2.2)");
            return ret;
        }
    }

}