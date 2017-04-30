using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Discord.WebSocket;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Prism.Commands;

namespace Uncord.ViewModels
{
    public class GuildChannelsPageViewModel : UncordPageViewModelBase
    {
        Models.DiscordContext _DiscordContext;

        SocketGuild _Guild;

        public ReactiveProperty<string> GuildName { get; private set; }

        ObservableCollection<SocketTextChannel> _TextChannels;
        public ReadOnlyReactiveCollection<SocketTextChannel> TextChannels { get; private set; }

        ObservableCollection<SocketVoiceChannel> _VoiceChannels;
        public ReadOnlyReactiveCollection<SocketVoiceChannel> VoiceChannels { get; private set; }

        public ReactiveProperty<SocketVoiceChannel> AfkChannel { get; private set; }
        public ReactiveProperty<bool> HasAfkChannel { get; private set; }

        public DelegateCommand UnselectTextChannelCommand { get; private set; }

        public DelegateCommand UnselectVoiceChannelCommand { get; private set; }



        public GuildChannelsPageViewModel(Models.DiscordContext discordContext)
        {
            _DiscordContext = discordContext;
            GuildName = new ReactiveProperty<string>();

            _TextChannels = new ObservableCollection<SocketTextChannel>();
            TextChannels = _TextChannels.ToReadOnlyReactiveCollection();

            _VoiceChannels = new ObservableCollection<SocketVoiceChannel>();
            VoiceChannels = _VoiceChannels.ToReadOnlyReactiveCollection();

            AfkChannel = new ReactiveProperty<SocketVoiceChannel>();
            HasAfkChannel = AfkChannel.Select(x => x != null)
                .ToReactiveProperty();
        }

        

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is ulong)
            {
                var guildId = (ulong)e.Parameter;
                _Guild = _DiscordContext.GetGuild(guildId);
            }

            if (_Guild == null)
            {
                return;
            }

            _DiscordContext.DiscordSocketClient.ChannelCreated += Discord_ChannelCreated;
            _DiscordContext.DiscordSocketClient.ChannelDestroyed += Discord_ChannelDestroyed;
            _DiscordContext.DiscordSocketClient.ChannelUpdated += Discord_ChannelUpdated;


            // Listup Text Channels
            var channels = _Guild.TextChannels.ToList();
            channels.Sort((x, y) => x.Position - y.Position);
            foreach (var textChannel in channels)
            {
                _TextChannels.Add(textChannel);
            }


            // Listup Voice Channels
            var afkChannel = _Guild.AFKChannel;
            var afkChannelId = afkChannel?.Id;
            var voiceChannels =
                    _Guild.VoiceChannels
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

            GuildName.Value = _Guild.Name;

            base.OnNavigatedTo(e, viewModelState);
        }

        
        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (_Guild == null) { return; }

            _DiscordContext.DiscordSocketClient.ChannelCreated -= Discord_ChannelCreated;
            _DiscordContext.DiscordSocketClient.ChannelDestroyed -= Discord_ChannelDestroyed;
            _DiscordContext.DiscordSocketClient.ChannelUpdated -= Discord_ChannelUpdated;

            _TextChannels.Clear();
            _VoiceChannels.Clear();

            base.OnNavigatingFrom(e, viewModelState, suspending);
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
                if (newGuildChanneld.Guild.Id == this._Guild.Id)
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
                if (oldGuildChanneld.Guild.Id == this._Guild.Id)
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
