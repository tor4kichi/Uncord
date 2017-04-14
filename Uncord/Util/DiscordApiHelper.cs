using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Uncord.Util
{

    // api unofficial refarence 
    // https://media.readthedocs.org/pdf/discordapi-unoffical/latest/discordapi-unoffical.pdf

    public static class DiscordApiHelper
    {
        public static HttpClient GetClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "UWP-Uncord/" + AppVersion.GetAppVersion());
            client.DefaultRequestHeaders
              .Accept
              .Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }


        public static async Task<string> LogInAsync(string mail, string password)
        {
            var discordCredencialVault = new { email = mail, password = password};
            var json = JsonConvert.SerializeObject(discordCredencialVault);
            var content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

            var res = await GetClient().PostAsync(new Uri("https://discordapp.com/api/auth/login"), content);

            if (res.IsSuccessStatusCode)
            {
                var resJson = await res.Content.ReadAsStringAsync();
                var deserializedJson = JsonConvert.DeserializeAnonymousType(resJson, new { token = "" });
                return deserializedJson.token;
            }
            else
            {
                return null;
            }
        }

        public static async Task<bool> LogOutAsync(string token)
        {
            var userLoginToken = new { token = token };
            var json = JsonConvert.SerializeObject(userLoginToken);
            var content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

            var res = await GetClient().PostAsync(new Uri("https://discordapp.com/api/auth/logout"), content);

            return res.IsSuccessStatusCode;
        }
    }
}
