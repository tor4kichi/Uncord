using Discord.WebSocket;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncord.Models;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class MenuViewModel : ViewModelBase
    {
        public DiscordContext DiscordContext { get; private set; }

        // TODO: ホーム、新着メッセージ、お気に入り

        private bool _IsOpenMenu;
        public bool IsOpenMenu
        {
            get { return _IsOpenMenu; }
            private set { SetProperty(ref _IsOpenMenu, value); }
        }
        

        CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        INavigationService NavigationService { get; }

        public ReadOnlyReactiveProperty<string> LoginUserName { get; }
        public ReadOnlyReactiveProperty<string> LoginUserAvaterUrl { get; }

        public ReadOnlyReactiveCollection<SocketGuild> Guilds { get; private set; }

        public ReactiveProperty<SocketGuild> SelectedGuild { get; }



        public ReactiveProperty<string> GuildName { get; private set; }

        ObservableCollection<SocketTextChannel> _TextChannels;
        public ReadOnlyReactiveCollection<SocketTextChannel> TextChannels { get; private set; }

        public ReadOnlyReactiveProperty<SocketTextChannel> CurrentTextChannel { get; }

        ObservableCollection<SocketVoiceChannel> _VoiceChannels;
        public ReadOnlyReactiveCollection<SocketVoiceChannel> VoiceChannels { get; private set; }

        public ReadOnlyReactiveProperty<SocketVoiceChannel> CurrentVoiceChannel { get; }

        public ReactiveProperty<SocketVoiceChannel> AfkChannel { get; private set; }
        public ReactiveProperty<bool> HasAfkChannel { get; private set; }

        public DelegateCommand UnselectTextChannelCommand { get; private set; }

        public DelegateCommand UnselectVoiceChannelCommand { get; private set; }

        private AsyncLock _GuildChangingLock { get; } = new AsyncLock();

        public MenuViewModel(DiscordContext discordContext, INavigationService navService)
        {
            DiscordContext = discordContext;
            NavigationService = navService;

            Guilds = DiscordContext.Guilds;
            SelectedGuild = new ReactiveProperty<SocketGuild>();
            GuildName = new ReactiveProperty<string>("");

            LoginUserName = DiscordContext.ObserveProperty(x => x.CurrentUser)
                .Select(x => x?.Username ?? "")
                .ToReadOnlyReactiveProperty();

            LoginUserAvaterUrl = DiscordContext.ObserveProperty(x => x.CurrentUser)
                .Select(x => x?.GetAvatarUrl() ?? "")
                .ToReadOnlyReactiveProperty();


            _TextChannels = new ObservableCollection<SocketTextChannel>();
            TextChannels = _TextChannels.ToReadOnlyReactiveCollection();
            CurrentTextChannel = DiscordContext.ObserveProperty(x => x.CurrentTextChannel)
                .ToReadOnlyReactiveProperty();

            _VoiceChannels = new ObservableCollection<SocketVoiceChannel>();
            VoiceChannels = _VoiceChannels.ToReadOnlyReactiveCollection();
            CurrentVoiceChannel = DiscordContext.ObserveProperty(x => x.CurrentVoiceChannel)
                .ToReadOnlyReactiveProperty();

            AfkChannel = new ReactiveProperty<SocketVoiceChannel>();
            HasAfkChannel = AfkChannel.Select(x => x != null)
                .ToReactiveProperty();


            SelectedGuild.Subscribe(async guild => 
            {
                using (var releaser = await _GuildChangingLock.LockAsync())
                {
                    if (guild != null)
                    {
                        _TextChannels.Clear();
                        _VoiceChannels.Clear();
                        AfkChannel.Value = null;

                        GuildName.Value = guild.Name;

                        DiscordContext.DiscordSocketClient.ChannelCreated += Discord_ChannelCreated;
                        DiscordContext.DiscordSocketClient.ChannelDestroyed += Discord_ChannelDestroyed;
                        DiscordContext.DiscordSocketClient.ChannelUpdated += Discord_ChannelUpdated;


                        // Listup Text Channels
                        var channels = guild.TextChannels.ToList();
                        channels.Sort((x, y) => x.Position - y.Position);
                        foreach (var textChannel in channels)
                        {
                            _TextChannels.Add(textChannel);
                        }


                        // Listup Voice Channels
                        var afkChannel = guild.AFKChannel;
                        var afkChannelId = afkChannel?.Id;
                        var voiceChannels =
                                guild.VoiceChannels
                                .Where(x => x.Id != afkChannelId)
                                .ToList();
                        voiceChannels.Sort((x, y) => x.Position - y.Position);

                        foreach (var channel in voiceChannels)
                        {
                            _VoiceChannels.Add(channel);
                        }

                        if (afkChannel != null)
                        {
                            AfkChannel.Value = afkChannel;
                        }
                    }
                    else
                    {
                        await Task.Delay(300);
                        _TextChannels.Clear();
                        _VoiceChannels.Clear();
                        AfkChannel.Value = null;
                        GuildName.Value = null;
                    }
                }
            });
        }

        public void OpenMenu()
        {
            IsOpenMenu = true;
        }

        private DelegateCommand _LogoutCommand;
        public DelegateCommand LogoutCommand
        {
            get
            {
                return _LogoutCommand
                    ?? (_LogoutCommand = new DelegateCommand(async () =>
                    {
                        NavigationService.Navigate(PageTokens.AccountLoginPageToken, null);

                        NavigationService.ClearHistory();

                        await DiscordContext.LogOut();
                    }));
            }
        }


        private DelegateCommand<SocketGuild> _SelectGuildCommand;
        public DelegateCommand<SocketGuild> SelectGuildCommand
        {
            get
            {
                return _SelectGuildCommand
                    ?? (_SelectGuildCommand = new DelegateCommand<SocketGuild>((guild) =>
                    {
                        SelectedGuild.Value = guild;
                    }));
            }
        }

        private DelegateCommand _OpenSettingsPageCommand;
        public DelegateCommand OpenSettingsPageCommand
        {
            get
            {
                return _OpenSettingsPageCommand
                    ?? (_OpenSettingsPageCommand = new DelegateCommand(() =>
                    {
                        NavigationService.Navigate(PageTokens.SettingsToken, null);
                    }));
            }
        }

        private DelegateCommand _OpenFriendsPageCommand;
        public DelegateCommand OpenFriendsPageCommand
        {
            get
            {
                return _OpenFriendsPageCommand
                    ?? (_OpenFriendsPageCommand = new DelegateCommand(() =>
                    {
                        NavigationService.Navigate(PageTokens.FriendsPageToken, null);
                    }));
            }
        }

        private DelegateCommand<SocketTextChannel> _OpenTextChannelPageCommand;
        public DelegateCommand<SocketTextChannel> OpenTextChannelPageCommand
        {
            get
            {
                return _OpenTextChannelPageCommand
                    ?? (_OpenTextChannelPageCommand = new DelegateCommand<SocketTextChannel>((textChannel) =>
                    {
                        if (textChannel != null)
                        {
                            NavigationService.Navigate(PageTokens.TextChannelPageToken, textChannel.Id);
                        }
                    }));
            }
        }

        private DelegateCommand<SocketVoiceChannel> _OpenVoiceChannelPageCommand;
        public DelegateCommand<SocketVoiceChannel> OpenVoiceChannelPageCommand
        {
            get
            {
                return _OpenVoiceChannelPageCommand
                    ?? (_OpenVoiceChannelPageCommand = new DelegateCommand<SocketVoiceChannel>((voiceChannel) =>
                    {
                        if (voiceChannel != null)
                        {
                            NavigationService.Navigate(PageTokens.VoiceChannelPageToken, voiceChannel.Id);
                        }
                    }));
            }
        }

        private DelegateCommand<SocketVoiceChannel> _ConnectVoiceChannelCommand;
        public DelegateCommand<SocketVoiceChannel> ConnectVoiceChannelCommand
        {
            get
            {
                return _ConnectVoiceChannelCommand
                    ?? (_ConnectVoiceChannelCommand = new DelegateCommand<SocketVoiceChannel>(async (voiceChannel) =>
                    {
                        // Voiceチャンネルを選択
                        if (voiceChannel != null)
                        {
                            NavigationService.Navigate(PageTokens.VoiceChannelPageToken, voiceChannel.Id);
                            await DiscordContext.ConnectVoiceChannel(voiceChannel.Id);
                        }
                    }));
            }
        }


        private async Task Discord_ChannelUpdated(SocketChannel oldChannel, SocketChannel newChannel)
        {
            await Discord_ChannelDestroyed(oldChannel);
            await Discord_ChannelCreated(newChannel);
        }

        private Task Discord_ChannelCreated(SocketChannel newChannel)
        {
            if (newChannel is SocketGuildChannel)
            {
                var newGuildChanneld = newChannel as SocketGuildChannel;
                if (newGuildChanneld.Guild.Id == SelectedGuild.Value.Id)
                {
                    if (newGuildChanneld is SocketTextChannel)
                    {
                        _TextChannels.Add(newGuildChanneld as SocketTextChannel);
                    }
                    else if (newGuildChanneld is SocketVoiceChannel)
                    {
                        _VoiceChannels.Add(newGuildChanneld as SocketVoiceChannel);
                    }

                }
            }

            return Task.CompletedTask;
        }

        private Task Discord_ChannelDestroyed(SocketChannel oldChannel)
        {
            if (oldChannel is SocketGuildChannel)
            {
                var oldGuildChanneld = oldChannel as SocketGuildChannel;
                if (oldGuildChanneld.Guild.Id == SelectedGuild.Value.Id)
                {
                    if (oldGuildChanneld is SocketTextChannel)
                    {
                        var channelVM = _TextChannels.SingleOrDefault(x => oldChannel.Id == x.Id);
                        _TextChannels.Remove(channelVM);
                    }
                    else if (oldGuildChanneld is SocketVoiceChannel)
                    {
                        var channelVM = _VoiceChannels.SingleOrDefault(x => oldChannel.Id == x.Id);
                        _VoiceChannels.Remove(channelVM);
                    }
                }
            }

            return Task.CompletedTask;
        }

    }
}
