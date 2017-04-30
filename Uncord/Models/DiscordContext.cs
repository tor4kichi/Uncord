using Discord;
using Discord.Audio;
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

        private AudioPlaybackManager AudioManager;

        IAudioClient _CurrentVoiceAudioClient;



        private string _LoginProcessStateResourceId;
        public string LoginProcessStateResourceId
        {
            get { return _LoginProcessStateResourceId; }
            set { SetProperty(ref _LoginProcessStateResourceId, value); }
        }

        public DiscordSocketClient DiscordSocketClient { get; private set; }
        public DiscordRestClient DiscordRestClient { get; private set; }

        private AsyncLock _LoginLock = new AsyncLock();




        private RestSelfUser _CurrentUser;
        public RestSelfUser CurrentUser
        {
            get { return _CurrentUser; }
            set { SetProperty(ref _CurrentUser, value); }
        }




        private ObservableCollection<SocketGuild> _Guilds;
        public ReadOnlyReactiveCollection<SocketGuild> Guilds { get; private set; }


        private SocketGuild _CurrentGuild;
        public SocketGuild CurrentGuild
        {
            get { return _CurrentGuild; }
            private set { SetProperty(ref _CurrentGuild, value); }
        }

        private AsyncLock _GuildSelectionChangingLock = new AsyncLock();

        public async Task SetCurrentGuildAsync(SocketGuild guild)
        {
            using (var releaser = await _GuildSelectionChangingLock.LockAsync())
            {
                CurrentTextChannel = null;
                CurrentVoiceChannel = null;
                CurrentGuildAfkVoiceChannel = null;
                _CurrentGuildTextChannels.Clear();
                _CurrentGuildVoiceChannels.Clear();

                CurrentGuild = guild;

                if (CurrentGuild != null)
                {
                    foreach (var textChannel in CurrentGuild.TextChannels.ToArray())
                    {
                        _CurrentGuildTextChannels.Add(textChannel);
                    }
                    foreach (var voiceChannel in CurrentGuild.VoiceChannels)
                    {
                        _CurrentGuildVoiceChannels.Add(voiceChannel);
                    }

                    CurrentGuildAfkVoiceChannel = CurrentGuild.AFKChannel;
                }
            }
        }



        private ObservableCollection<SocketTextChannel> _CurrentGuildTextChannels;
        public ReadOnlyObservableCollection<SocketTextChannel> CurrentGuildTextChannels { get; }


        private ObservableCollection<SocketVoiceChannel> _CurrentGuildVoiceChannels;
        public ReadOnlyObservableCollection<SocketVoiceChannel> CurrentGuildVoiceChannels { get; }

        private SocketVoiceChannel _CurrentGuildAfkVoiceChannel;
        public SocketVoiceChannel CurrentGuildAfkVoiceChannel
        {
            get { return _CurrentGuildAfkVoiceChannel; }
            private set { SetProperty(ref _CurrentGuildAfkVoiceChannel, value); }
        }


        private SocketTextChannel _CurrentTextChannel;
        public SocketTextChannel CurrentTextChannel
        {
            get { return _CurrentTextChannel; }
            set { SetProperty(ref _CurrentTextChannel, value); }
        }

        private SocketVoiceChannel _CurrentVoiceChannel;
        public SocketVoiceChannel CurrentVoiceChannel
        {
            get { return _CurrentVoiceChannel; }
            private set { SetProperty(ref _CurrentVoiceChannel, value); }
        }

        private bool _IsConnectedVoiceChannel;
        public bool IsConnectedVoiceChannel
        {
            get { return _IsConnectedVoiceChannel; }
            private set { SetProperty(ref _IsConnectedVoiceChannel, value); }
        }

        private bool _IsDisconnectedVoiceChannel;
        public bool IsDisconnectedVoiceChannel
        {
            get { return _IsDisconnectedVoiceChannel; }
            private set { SetProperty(ref _IsDisconnectedVoiceChannel, value); }
        }

        private bool _IsNotConnectVoiceChannel;
        public bool IsNotConnectVoiceChannel
        {
            get { return _IsNotConnectVoiceChannel; }
            private set { SetProperty(ref _IsNotConnectVoiceChannel, value); }
        }

        private UncordChannelConnectState _ConnectState;
        public UncordChannelConnectState ConnectState
        {
            get { return _ConnectState; }
            private set
            {
                if (SetProperty(ref _ConnectState, value))
                {
                    IsNotConnectVoiceChannel = _ConnectState == UncordChannelConnectState.NotConnect;
                    IsConnectedVoiceChannel = _ConnectState == UncordChannelConnectState.Connected;
                    IsDisconnectedVoiceChannel = _ConnectState == UncordChannelConnectState.Disconnected;
                }
            }
        }

        private AsyncLock _VoiceChannelLock = new AsyncLock();

       
        public DiscordContext(AudioPlaybackManager audioManager)
        {
            AudioManager = audioManager;

            _Guilds = new ObservableCollection<SocketGuild>();
            Guilds = _Guilds.ToReadOnlyReactiveCollection();

            _CurrentGuildTextChannels = new ObservableCollection<SocketTextChannel>();
            CurrentGuildTextChannels = new ReadOnlyObservableCollection<SocketTextChannel>(_CurrentGuildTextChannels);

            _CurrentGuildVoiceChannels = new ObservableCollection<SocketVoiceChannel>();
            CurrentGuildVoiceChannels = new ReadOnlyObservableCollection<SocketVoiceChannel>(_CurrentGuildVoiceChannels);





            // this is unkode(durty code).
            // IMessage.Contentの内容をMarkdownに変換する際、
            // ユーザーIDからユーザー名を引きたいためにこんなことをしてます
            // Model上で解決すべき？
            Views.Controls.DiscordMessageContent.UserIdToUserName = UserIdToUserName;
        }

        private async Task StartLoginProcess()
        {
            // TODO: Discordへログインした後の初期化処理
            DiscordRestClient = new Discord.Rest.DiscordRestClient();
            DiscordRestClient.LoggedIn += DiscordRestClient_LoggedIn;
            DiscordRestClient.LoggedOut += DiscordRestClient_LoggedOut;
            await DiscordRestClient.LoginAsync(Discord.TokenType.User, DiscordAccessToken);
        }

        public string UserIdToUserName(string userId)
        {
            if (DiscordSocketClient != null)
            {
                var user =  DiscordSocketClient.GetUser(ulong.Parse(userId));
                return user?.Username;
            }
            else
            {
                return null;
            }
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

            CurrentVoiceChannel = null;
            CurrentTextChannel = null;
            CurrentGuild = null;
        }


        #region SocketClient Event Handler


        private async Task DiscordSocketClient_LoggedIn()
        {
            DiscordSocketClient.Connected += DiscordSocketClient_Connected;
            DiscordSocketClient.Disconnected += DiscordSocketClient_Disconnected;

            DiscordSocketClient.JoinedGuild += DiscordSocketClient_JoinedGuild;
            DiscordSocketClient.LeftGuild += DiscordSocketClient_LeftGuild;
            DiscordSocketClient.GuildUpdated += DiscordSocketClient_GuildUpdated;

            await DiscordSocketClient.StartAsync();
        }

        
        private async Task DiscordSocketClient_LoggedOut()
        {
            await DiscordSocketClient.StopAsync();

            DiscordSocketClient.JoinedGuild -= DiscordSocketClient_JoinedGuild;
            DiscordSocketClient.LeftGuild -= DiscordSocketClient_LeftGuild;
            DiscordSocketClient.GuildUpdated -= DiscordSocketClient_GuildUpdated;

            DiscordSocketClient.Connected -= DiscordSocketClient_Connected;
            DiscordSocketClient.Disconnected -= DiscordSocketClient_Disconnected;
            
            _Guilds.Clear();
        }

        private async Task DiscordSocketClient_Connected()
        {
            Debug.WriteLine("listup guild.");



            //            var groupChannels = await DiscordSocketClient.GetGroupChannelsAsync();
            //            var connections = await DiscordSocketClient.GetConnectionsAsync();
            //            var DmChannels = await DiscordSocketClient.GetDMChannelsAsync();
            //            var channels = DiscordSocketClient.PrivateChannels;
            await Task.Delay(0);
        }


        private Task DiscordSocketClient_Disconnected(Exception arg)
        {


            return Task.CompletedTask;
        }



        #region Guild Event Handler


        private async Task DiscordSocketClient_GuildUpdated(SocketGuild arg1, SocketGuild arg2)
        {
            using (var releaser = await _LoginLock.LockAsync())
            {
                _Guilds.Remove(arg1);
                _Guilds.Add(arg2);
            }
        }

        private async Task DiscordSocketClient_JoinedGuild(SocketGuild arg)
        {
            using (var releaser = await _LoginLock.LockAsync())
            {
                var already = _Guilds.FirstOrDefault(x => x.Id == arg.Id);

                if (already != null)
                {
                    _Guilds.Remove(already);
                }

                _Guilds.Add(arg);
            }
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


        #region Voice

        public async Task<bool> ConnectVoiceChannel(SocketVoiceChannel voiceChannel)
        {
            await DisconnectCurrentVoiceChannel();

            if (voiceChannel == null)
            {
                throw new ArgumentNullException("VoiceChannel is null");
            }

            // ボイスチャンネルへの接続を開始
            // 音声の送信はConnectedイベント後
            // 受信はStreamCreatedイベント後に行われます
            using (var releaser = await _VoiceChannelLock.LockAsync())
            {
                await voiceChannel.ConnectAsync((client) =>
                {
                    _CurrentVoiceAudioClient = client;
                    client.Connected += VoiceChannelConnected;
                    client.Disconnected += VoiceChannelDisconnected;
                    client.LatencyUpdated += VoiceChannelLatencyUpdated;
                    client.SpeakingUpdated += VoiceChannelSpeakingUpdated;
                    client.StreamCreated += VoiceChannelAudioStreamCreated;
                    client.StreamDestroyed += VoiceChannelAudioStreamDestroyed;
                });

                CurrentVoiceChannel = voiceChannel;
                ConnectState = UncordChannelConnectState.Connected;
            }

            return true;
        }

        public Task<bool> ConnectVoiceChannel(ulong voiceChannelId)
        {
            return ConnectVoiceChannel(DiscordSocketClient.GetChannel(voiceChannelId) as SocketVoiceChannel);
        }



        public async Task DisconnectCurrentVoiceChannel()
        {
            using (var releaser = await _VoiceChannelLock.LockAsync())
            {
                if (_CurrentVoiceAudioClient != null)
                {
                    _CurrentVoiceAudioClient.Dispose();
                    _CurrentVoiceAudioClient = null;
                }

                CurrentVoiceChannel = null;
                ConnectState = UncordChannelConnectState.NotConnect;
            }
        }


        private async Task VoiceChannelConnected()
        {
            await StartAudioCapture();
        }

        private async Task VoiceChannelDisconnected(Exception arg)
        {
            await StopAudioCapture();
        }





        private async Task VoiceChannelAudioStreamCreated(ulong arg1, AudioInStream stream)
        {
            AudioManager.StartAudioOutput(stream);

            await Task.Delay(0);
        }

        private async Task VoiceChannelAudioStreamDestroyed(ulong arg)
        {
            AudioManager.StopAudioOutput();

            using (var releaser = await _VoiceChannelLock.LockAsync())
            {
                if (IsConnectedVoiceChannel)
                {
                    // TODO: 意図しない切断の場合 ボイスチャンネルに再接続する
                    ConnectState = UncordChannelConnectState.Disconnected;
                }
            }


            await Task.Delay(0);
        }


        private Task VoiceChannelSpeakingUpdated(ulong arg1, bool arg2)
        {
#if DEBUG
            Debug.WriteLine($"speaking: {arg1} is {arg2}");
#endif
            return Task.CompletedTask;
        }

        private Task VoiceChannelLatencyUpdated(int arg1, int arg2)
        {
#if DEBUG
            Debug.WriteLine($"Latency: {arg1} : {arg2}");
#endif
            return Task.CompletedTask;
        }



        #region AudioCapture


        private async Task StartAudioCapture()
        {
            if (_CurrentVoiceAudioClient == null)
            {
                return;
            }

            AudioManager.StartAudioInput(_CurrentVoiceAudioClient);

            await Task.Delay(0);
        }



        private async Task StopAudioCapture()
        {
            AudioManager.StopAudioInput();

            await Task.Delay(0);
        }


        #endregion

        #endregion

    }


    public enum UncordChannelConnectState
    {
        NotConnect,
        Connected,
        Disconnected,
    }
}
