using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;

namespace eurosportCapper
{
    public enum Broadcast
    {
        British,
        German,
        French
    }

    public class AllProductsResponse
    {
        [JsonProperty(PropertyName = "PlayerObj")]
        public List<Channel> Channels { get; set; } 
    }

    public class Eurosport
    {
        private const string LoginPath = "https://playercrm.ssl.eurosport.com/JsonPlayerCrmApi.svc/Login";

        private const string ProductPath =
            "http://videoshop.ext.eurosport.com/JsonProductService.svc/GetAllProductsByDeviceMobile";

        private readonly HttpClient _client;
        private readonly string _email;
        private readonly string _password;

        private Dictionary<String, Object> _loginData;
        private Dictionary<String, Object> _context; 

        private void SetupContext(Broadcast broadcastType)
        {
            _context = new Dictionary<String, Object>()
            {
                {"c", "EUR"},
                {"d", 4},
                {"s", 1},
                {"v", "2.2.2"},
                {"p", "1"},
                {"b", "google"},
                {"ap", 21},
                {"mi", "LRX22G"},
                {"mn", "Nexus 7"},
                {"ma", "LGE"},
                {"tt", "Phone"},
                {"di", "dimension=1196x768,density=319,79x318,74,scale=2,00x2,00"},
                {"o", "11"},
                {"osn", "Android"},
                {"osv", "5.0.1"},
                {"st", "2"}
            };

            switch (broadcastType)
            {
                case Broadcast.British:
                    _context.Add("g", "GB");
                    _context.Add("l", "en");
                    break;

                case Broadcast.German:
                    _context.Add("g", "DE");
                    _context.Add("l", "de");
                    break;

                case Broadcast.French:
                    _context.Add("g", "FR");
                    _context.Add("l", "fr");
                    break;
            }
        }

        public Eurosport(string email, string password, Broadcast broadcastType)
        {
            _email = email;
            _password = password;

            _client = Utility.CreateClient();

            SetupContext(broadcastType);
        }

        public List<Channel> GetAllProducts()
        {
            if (_loginData == null)
                throw new InvalidDataException("No login information");

            var contextJson = JsonConvert.SerializeObject(_context);
            var dataJson = JsonConvert.SerializeObject(_loginData);
            var url = ProductPath + "?data=" + Uri.EscapeDataString(dataJson) + "&context=" +
                      Uri.EscapeDataString(contextJson);
            var resp = _client.GetAsync(url).Result;
            if (!resp.IsSuccessStatusCode)
                return null;

            var respText = resp.Content.ReadAsStringAsync().Result;
            dynamic products = JsonConvert.DeserializeObject(respText);

            bool success = ((string) products.ActiveUserRef.Response.Message).Contains("return success");
            if (!success)
                return null;

            return JsonConvert.DeserializeObject<AllProductsResponse>(respText).Channels;
        }

        public bool Login()
        {
            var data = new Dictionary<String, Object>()
            {
                {"email", _email},
                {"password", _password},
                {"udid", "d2addfa1-9371-54d2-beef-dabae7de5eaa"}
            };

            var contextJson = JsonConvert.SerializeObject(_context);
            var dataJson = JsonConvert.SerializeObject(data);
            var url = LoginPath + "?data=" + Uri.EscapeDataString(dataJson) + "&context=" +
                      Uri.EscapeDataString(contextJson);
            var resp = _client.GetAsync(url).Result;
            if (!resp.IsSuccessStatusCode)
                return false;
            var respData = resp.Content.ReadAsStringAsync().Result;
            dynamic loginData = JsonConvert.DeserializeObject(respData);
            if (!((bool)loginData.Response.Success))
                return false;

            _loginData = new Dictionary<string, object>
            {
                {"userid", loginData.Id},
                {"hkey", loginData.Hkey},
                {"languageid", 2},
                {"isfullaccess", 0},
                {"isbroadcasted", 0},
                {"epglight", false}
            };

            return true;
        }
    }

}