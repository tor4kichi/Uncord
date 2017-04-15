using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using WinRTXamlToolkit.Async;

namespace Uncord.Models
{
    // Discordへのログイン状態の保持
    // メール・パスワードの資格情報

    
    public class DiscordContext : BindableBase
    {
        public string DiscordAccessToken { get; private set; }

        public bool IsValidateToken => !string.IsNullOrEmpty(DiscordAccessToken);

        private RestSelfUser _CurrentUser;
        public RestSelfUser CurrentUser
        {
            get { return _CurrentUser; }
            set { SetProperty(ref _CurrentUser, value); }
        }

        private ObservableCollection<SocketGuild> _Guilds;
        public ReadOnlyReactiveCollection<SocketGuild> Guilds { get; private set; }

        private AsyncLock _LoginLock = new AsyncLock();

        private string _LoginProcessStateResourceId;
        public string LoginProcessStateResourceId
        {
            get { return _LoginProcessStateResourceId; }
            set { SetProperty(ref _LoginProcessStateResourceId, value); }
        }

        public DiscordSocketClient DiscordSocketClient { get; private set; }
        public DiscordRestClient DiscordRestClient { get; private set; }

        public DiscordContext()
        {
            _Guilds = new ObservableCollection<SocketGuild>();
            Guilds = _Guilds.ToReadOnlyReactiveCollection();
        }

        private async Task StartLoginProcess()
        {
            // TODO: Discordへログインした後の初期化処理
            DiscordRestClient = new Discord.Rest.DiscordRestClient();
            DiscordRestClient.LoggedIn += DiscordRestClient_LoggedIn;
            DiscordRestClient.LoggedOut += DiscordRestClient_LoggedOut;
            await DiscordRestClient.LoginAsync(Discord.TokenType.User, DiscordAccessToken);
        }



        private async Task DiscordRestClient_LoggedIn()
        {
            Debug.WriteLine("login with: " + DiscordRestClient.CurrentUser.Username);

            CurrentUser = DiscordRestClient.CurrentUser;

            DiscordSocketClient = new DiscordSocketClient();
            DiscordSocketClient.LoggedIn += DiscordSocketClient_LoggedIn;
            DiscordSocketClient.LoggedOut += DiscordSocketClient_LoggedOut;
            
            await DiscordSocketClient.LoginAsync(TokenType.User, DiscordAccessToken);

        }


        private async Task DiscordRestClient_LoggedOut()
        {
            CurrentUser = null;
            await DiscordSocketClient.LogoutAsync();
            DiscordSocketClient.Dispose();
            DiscordSocketClient = null;
        }


        #region SocketClient Event Handler


        private async Task DiscordSocketClient_LoggedIn()
        {
            DiscordSocketClient.Connected += DiscordSocketClient_Connected;
            DiscordSocketClient.Disconnected += DiscordSocketClient_Disconnected;

            await DiscordSocketClient.StartAsync();
        }

        
        private async Task DiscordSocketClient_LoggedOut()
        {
            await DiscordSocketClient.StopAsync();

            DiscordSocketClient.Connected -= DiscordSocketClient_Connected;
            DiscordSocketClient.Disconnected -= DiscordSocketClient_Disconnected;
            
            _Guilds.Clear();
        }

        private async Task DiscordSocketClient_Connected()
        {
            Debug.WriteLine("listup guild.");
            
            DiscordSocketClient.JoinedGuild += DiscordSocketClient_JoinedGuild;
            DiscordSocketClient.LeftGuild += DiscordSocketClient_LeftGuild;
            DiscordSocketClient.GuildUpdated += DiscordSocketClient_GuildUpdated;


            //            var groupChannels = await DiscordSocketClient.GetGroupChannelsAsync();
            //            var connections = await DiscordSocketClient.GetConnectionsAsync();
            //            var DmChannels = await DiscordSocketClient.GetDMChannelsAsync();
            //            var channels = DiscordSocketClient.PrivateChannels;

            await Task.Delay(0);
        }


        private Task DiscordSocketClient_Disconnected(Exception arg)
        {
            DiscordSocketClient.JoinedGuild -= DiscordSocketClient_JoinedGuild;
            DiscordSocketClient.LeftGuild -= DiscordSocketClient_LeftGuild;
            DiscordSocketClient.GuildUpdated -= DiscordSocketClient_GuildUpdated;


            return Task.CompletedTask;
        }



        #region Guild Event Handler


        private Task DiscordSocketClient_GuildUpdated(SocketGuild arg1, SocketGuild arg2)
        {
            _Guilds.Remove(arg1);
            _Guilds.Add(arg2);

            return Task.CompletedTask;
        }

        private Task DiscordSocketClient_JoinedGuild(SocketGuild arg)
        {
            _Guilds.Add(arg);

            return Task.CompletedTask;
        }

        private Task DiscordSocketClient_LeftGuild(SocketGuild arg)
        {
            _Guilds.Remove(arg);

            return Task.CompletedTask;
        }


        #endregion

        #endregion





        private async Task ClearupLoginInfo()
        {
            // TODO: ログイン後の処理を中止
            await DiscordRestClient.LogoutAsync();

            DiscordRestClient.Dispose();
            
            // TODO: ログイン情報を全て破棄
        }

        public async Task WaitLoginAction()
        {
            using (var releaser = await _LoginLock.LockAsync())
            {
                // nothing to do.
            }
        }

        
        public async Task<bool> TryLoginWithRecordedCredential()
        {
            await LogOut();

            using (var releaser = await _LoginLock.LockAsync())
            {
                var vault = new Windows.Security.Credentials.PasswordVault();

                try
                {

                    if (!TryGetRecentLoginAccount(out var mailAndPassword))
                    {
                        return false;
                    }

                    var token = await Util.DiscordApiHelper.LogInAsync(mailAndPassword.Item1, mailAndPassword.Item2);
                    if (string.IsNullOrEmpty(token))
                    {
                        return false;
                    }

                    DiscordAccessToken = token;
                }
                catch
                {
                    DiscordAccessToken = null;
                    return false;
                }
            }

            if (IsValidateToken)
            {
                await StartLoginProcess().ConfigureAwait(false);
            }

            return true;
        }

        public async Task<bool> TryLogin(string mail, string password)
        {
            await LogOut();

            using (var releaser = await _LoginLock.LockAsync())
            {
                var token = await Util.DiscordApiHelper.LogInAsync(mail, password);
                if (token == null)
                {
                    return false;
                }

                DiscordAccessToken = token;

                AddOrUpdateRecentLoginAccount(mail, password);
            }

            if (IsValidateToken)
            {
                await StartLoginProcess().ConfigureAwait(false);
            }

            return true;
        }

        public async Task LogOut()
        {
            bool nowLogout = false;
            using (var releaser = await _LoginLock.LockAsync())
            {
                if (IsValidateToken)
                {
                    if (!await Util.DiscordApiHelper.LogOutAsync(DiscordAccessToken))
                    {
                        Debug.WriteLine("failed logout. token is " + DiscordAccessToken);
                    }

                    DiscordAccessToken = null;

                    nowLogout = true;
                }
            }

            if (nowLogout)
            {
                await ClearupLoginInfo().ConfigureAwait(false);
            }
        }



        public bool TryGetRecentLoginAccount(out Tuple<string, string> mailAndPassword)
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var oldItems = vault.FindAllByResource(nameof(Uncord));
            if (oldItems.Count == 0)
            {
                mailAndPassword = null;
                return false;
            }
            var prevAccount = oldItems.First();
            var retrievedAccount = vault.Retrieve(prevAccount.Resource, prevAccount.UserName);

            if (string.IsNullOrWhiteSpace(retrievedAccount.UserName)
                || string.IsNullOrWhiteSpace(retrievedAccount.Password)
                )
            {
                mailAndPassword = null;
                return false;
            }

            mailAndPassword = new Tuple<string, string>(retrievedAccount.UserName, retrievedAccount.Password);

            return true;
        }

        private void AddOrUpdateRecentLoginAccount(string mail, string password)
        {
            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                try
                {
                    var oldItems = vault.FindAllByResource(nameof(Uncord));
                    foreach (var vaultItem in oldItems)
                    {
                        vault.Remove(vaultItem);
                    }
                    oldItems = null;
                }
                catch { }

                vault.Add(new Windows.Security.Credentials.PasswordCredential(
                    nameof(Uncord), mail, password));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }


        #region Guild 

        public SocketGuild GetGuild(ulong guildId)
        {
            return Guilds.SingleOrDefault(x => x.Id == guildId);
        }

        #endregion
    }
}
