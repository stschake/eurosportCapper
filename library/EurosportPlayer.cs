using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using esnew.model;

namespace esnew
{

    public class EurosportPlayer
    {
        private const string API_KEY = "2I84ZDjA2raVJ3hyTdADwdwxgDz7r62J8J0W8bE8N8VVILY446gDlrEB33fqLaXD";
        private HttpClient _client = CreateClient();
        private string _userToken;
        private string _clientToken;

        private static HttpClient CreateClient()
        {
            var ret = new HttpClient();
            ret.DefaultRequestHeaders.Add("x-bamsdk-platform", "windows");
            ret.DefaultRequestHeaders.Add("x-bamsdk-version", "2.1");
            ret.DefaultRequestHeaders.Add("User-Agent",
             "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.101 Safari/537.36");
            return ret;
        }

        public async Task<bool> Login(string email, string password, Country country)
        {
            return await Login(email, password, country.ToLocation());
        }

        private void ForceHeader(string name, string value)
        {
            if (_client.DefaultRequestHeaders.Contains(name))
                _client.DefaultRequestHeaders.Remove(name);
            _client.DefaultRequestHeaders.Add(name, value);
        }

        private async Task<string> GetAccessToken(Location location, string grantType, string token)
        {
            var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
            var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var formContent = new StringContent($"grant_type={grantType}&latitude={latitude}&longitude={longitude}&platform=browser&token={token}",
                 Encoding.UTF8, "application/x-www-form-urlencoded");

            ForceHeader("Authorization", "Bearer " + API_KEY);
            var resp = await _client.PostAsync("https://global-api.svcs.eurosportplayer.com/token", formContent);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Error retrieving token: " + content);

            var data = JObject.Parse(content);
            if (data["access_token"] == null)
                throw new Exception("Missing field access_token, data: " + data);

            return data["access_token"].ToString();
        }

        private async Task<string> GetClientToken(Location location, string deviceToken)
        {
            return await GetAccessToken(location, "client_credentials", deviceToken);
        }

        private async Task<string> RunGraphqlQuery(string query, string token)
        {
            dynamic wrapper = new ExpandoObject();
            wrapper.query = query;
            wrapper.operationName = "";
            wrapper.variables = new object();
            var serializedWrapper = JsonConvert.SerializeObject(wrapper);

            var reqContent = new StringContent(serializedWrapper, Encoding.UTF8, "application/json");
            ForceHeader("accept", "application/json");
            ForceHeader("authorization", token);
            var resp = await _client.PostAsync("https://search-api.svcs.eurosportplayer.com/svc/search/v2/graphql", reqContent);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Unsuccessful GraphQL query: " + resp.StatusCode + " " + content);

            return content;
        }

        private async Task<string> GetLoginCode(string email, string password, string clientToken)
        {
            ForceHeader("accept", "application/vnd.identity-service+json; version=1.0");
            ForceHeader("Authorization", clientToken);
            var req = $"{{\"type\":\"email-password\",\"email\":{{\"address\":\"{email}\"}},\"password\":{{\"value\":\"{password}\"}}}}";
            var reqContent = new StringContent(req, Encoding.UTF8, "application/json");
            var resp = await _client.PostAsync("https://global-api.svcs.eurosportplayer.com/v2/user/identity", reqContent);
            var content = await resp.Content.ReadAsStringAsync();
            _client.DefaultRequestHeaders.Remove("accept");
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // not an exception, just means invalid login data
                return null;
            }
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Error retrieving login code: " + content);

            var data = JObject.Parse(content);
            if (data["code"] == null)
                throw new Exception("Missing field code, data: " + data);

            return data["code"].ToString();
        }

        private async Task<string> GetUserToken(Location location, string loginCode)
        {
            return await GetAccessToken(location, "urn:mlbam:params:oauth:grant_type:token", loginCode);
        }

        public async Task<List<Airing>> GetAiringsOnNow()
        {
            var queryRes = await RunGraphqlQuery(GraphqlQuery.AiringsOnNow, _userToken);
            var resultData = JObject.Parse(queryRes);

            return resultData["data"]["onNow"]["hits"]
                    .Children()
                    .Select(child => child["hit"])
                    .Select(token => token.ToObject<Airing>()).ToList();
        }

        public async Task<OnDemandContent> GetAllOnDemand()
        {
            dynamic query = new ExpandoObject();
            query.uiLang = "en";
            query.mediaRights = new string[] { "GeoMediaRight" };
            query.preferredLanguages = new string[] { "en", "de" };
            var serializedQuery = JsonConvert.SerializeObject(query);
            var encodedQuery = System.Uri.EscapeUriString(serializedQuery);

            ForceHeader("accept", "application/json");
            ForceHeader("authorization", _userToken);
            var resp = await _client.GetAsync(
                "https://search-api.svcs.eurosportplayer.com/svc/search/v2/graphql/persisted/query/eurosport/web/ondemand/all?variables=" + encodedQuery);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Unsuccessful query: " + resp.StatusCode + " " + content);

            var result = JObject.Parse(content);
            var items = result["data"]["bucket"]["aggs"][0]["buckets"];
            var ret = new OnDemandContent();
            foreach (var item in items)
            {
                foreach (var hit in item["hits"])
                {
                    switch (hit["hit"]["type"].ToString())
                    {
                        case "Video":
                            ret.Videos.Add(hit["hit"].ToObject<Video>());
                            break;
                        case "Airing":
                            ret.Airings.Add(hit["hit"].ToObject<Airing>());
                            break;
                        default:
                            // Unknown type
                            break;
                    }
                }
            }

            return ret;
        }

        public async Task<StreamHandler> GetStream(PlaybackUrl url)
        {
            var finalUrl = url.IsTemplated ?
             url.WithScenario(Scenario.BrowserUnlimited) : url.URL;

            ForceHeader("authorization", _userToken);
            ForceHeader("accept", "application/vnd.media-service+json; version=1");
            var resp = await _client.GetStringAsync(finalUrl);
            var respObj = JObject.Parse(resp);
            var slideUrl = (respObj["stream"]["slide"] ?? respObj["stream"]["complete"]).ToString();

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("authorization", _userToken);
            return new StreamHandler(client, slideUrl);
        }

        public async Task<bool> Login(string email, string password, Location location)
        {
            string deviceId = Settings.Instance.DeviceGuid.ToString();
            _clientToken = await GetClientToken(location, deviceId);
            var loginCode = await GetLoginCode(email, password, _clientToken);
            if (loginCode == null)
                return false;

            _userToken = await GetUserToken(location, loginCode);
            ForceHeader("authorization", _userToken);
            return true;
        }
    }

}