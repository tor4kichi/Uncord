using Enough.Async;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace DiscordClient
{
    public class DiscordContext : IDisposable
    {
        private HttpClient _HttpClient;
        public HttpClient HttpClient
        {
            get
            {
                return _HttpClient ?? (_HttpClient = MakeClient());
            }
        }

        public string UserAgent { get; set; } = "UWP-DiscordClient/1.0";


        public DiscordAccount LoggedInAccount { get; private set; }
        public bool IsLoggedIn => LoggedInAccount != null;

        private AsyncLock _LoginLock = new AsyncLock();

        public DiscordContext()
        {
            
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }


        public HttpClient MakeClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            return client;
        }


        public async Task<bool> LogInAsync(string mail, string password)
        {
            if (IsLoggedIn) { throw new Exception("already LoggedIn."); }

            using (var releaser = await _LoginLock.LockAsync())
            {
                var discordCredencialVault = new { email = mail, password = password };
                var json = JsonConvert.SerializeObject(discordCredencialVault);
                var content = new HttpStringContent(json);

                var res = await HttpClient.PostAsync(new Uri("https://discordapp.com/api/auth/login"), content);

                if (res.IsSuccessStatusCode)
                {
                    var token = await res.Content.ReadAsStringAsync();

                    LoggedInAccount = new DiscordAccount(mail, password, token);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> LogOutAsync()
        {
            using (var releaser = await _LoginLock.LockAsync())
            {
                if (!IsLoggedIn)
                {
                    return true;
                }

                var userLoginToken = new { token = LoggedInAccount.Token };
                var json = JsonConvert.SerializeObject(userLoginToken);
                var content = new HttpStringContent(json);

                var res = await HttpClient.PostAsync(new Uri("https://discordapp.com/api/auth/logout"), content);

                return res.IsSuccessStatusCode;
            }
        }
    }
}
