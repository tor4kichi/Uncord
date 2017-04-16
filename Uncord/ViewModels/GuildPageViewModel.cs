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
    public class GuildPageViewModel : ViewModelBase
    {
        Models.DiscordContext _DiscordContext;

        SocketGuild _Guild;

        public ReactiveProperty<string> GuildName { get; private set; }

        ObservableCollection<GuildTextChannelViewModel> _TextChannels;
        public ReadOnlyReactiveCollection<GuildTextChannelViewModel> TextChannels { get; private set; }

        ObservableCollection<SocketVoiceChannel> _VoiceChannels;
        public ReadOnlyReactiveCollection<SocketVoiceChannel> VoiceChannels { get; private set; }

        public ReactiveProperty<SocketVoiceChannel> SelectedVoiceChannel { get; private set; }

        public ReactiveProperty<SocketGuildChannel> AfkChannel { get; private set; }
        public ReactiveProperty<bool> HasAfkChannel { get; private set; }

        public ReactiveProperty<GuildTextChannelViewModel> SelectedTextChannel { get; private set; }

        public Dictionary<ulong, GuildTextChannelViewModel> _TextChannelVMCacheMap = new Dictionary<ulong, GuildTextChannelViewModel>();

        public DelegateCommand UnselectTextChannelCommand { get; private set; }




        public GuildPageViewModel(Models.DiscordContext discordContext)
        {
            _DiscordContext = discordContext;
            GuildName = new ReactiveProperty<string>();

            _TextChannels = new ObservableCollection<GuildTextChannelViewModel>();
            TextChannels = _TextChannels.ToReadOnlyReactiveCollection();

            _VoiceChannels = new ObservableCollection<SocketVoiceChannel>();
            VoiceChannels = _VoiceChannels.ToReadOnlyReactiveCollection();

            AfkChannel = new ReactiveProperty<SocketGuildChannel>();
            HasAfkChannel = AfkChannel.Select(x => x != null)
                .ToReactiveProperty();

            SelectedVoiceChannel = new ReactiveProperty<SocketVoiceChannel>();
            SelectedTextChannel = new ReactiveProperty<GuildTextChannelViewModel>();

            SelectedTextChannel
                .Where(x => x != null)
                .Subscribe(async x => { await x.Load(); });

            UnselectTextChannelCommand = new DelegateCommand(() => 
            {
                SelectedTextChannel.Value = null;
            });
        }

        private GuildTextChannelViewModel GetTextChannelVM(SocketTextChannel textChannel)
        {
            if (textChannel == null) { return null; }

            GuildTextChannelViewModel vm = null;
            if (_TextChannelVMCacheMap.ContainsKey(textChannel.Id))
            {
                vm = _TextChannelVMCacheMap[textChannel.Id];
            }
            else
            {
                vm = new GuildTextChannelViewModel(textChannel);
                _TextChannelVMCacheMap.Add(textChannel.Id, vm);
            }

            return vm;
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

            _Guild.Discord.ChannelCreated += Discord_ChannelCreated;
            _Guild.Discord.ChannelDestroyed += Discord_ChannelDestroyed;
            _Guild.Discord.ChannelUpdated += Discord_ChannelUpdated;


            // Listup Text Channels
            foreach (var textChannel in _Guild.TextChannels.ToArray())
            {
                _TextChannels.Add(new GuildTextChannelViewModel(textChannel));
            }


            // Listup Voice Channels
            AfkChannel.Value = _Guild.AFKChannel;
            if (HasAfkChannel.Value)
            {
                var afkChannelId = AfkChannel.Value.Id;
                var channelsWithoutAfkChannel = 
                    _Guild.VoiceChannels
                    .Where(x => x.Id != afkChannelId)
                    .ToArray();

                foreach (var channel in channelsWithoutAfkChannel)
                {
                    _VoiceChannels.Add(channel);
                }
            }
            else
            {
                foreach (var channel in _Guild.VoiceChannels.ToArray())
                {
                    _VoiceChannels.Add(channel);
                }
            }


            SelectedTextChannel.Value = _TextChannels.SingleOrDefault(x => _Guild.DefaultChannel.Id == x.TextChannel.Id);

            GuildName.Value = _Guild.Name;

            base.OnNavigatedTo(e, viewModelState);
        }

        
        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (_Guild == null) { return; }

            _Guild.Discord.ChannelCreated -= Discord_ChannelCreated;
            _Guild.Discord.ChannelDestroyed -= Discord_ChannelDestroyed;
            _Guild.Discord.ChannelUpdated -= Discord_ChannelUpdated;

            _TextChannels.Clear();
            _VoiceChannels.Clear();

            foreach (var cachedTextChannelVM in _TextChannelVMCacheMap.Values)
            {
                cachedTextChannelVM.Dispose();
            }

            _TextChannelVMCacheMap.Clear();

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
                        _TextChannels.Add(new GuildTextChannelViewModel(newGuildChanneld as SocketTextChannel));
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
                        var channelVM = _TextChannels.SingleOrDefault(x => oldChannel.Id == x.TextChannel.Id);
                        _TextChannels.Remove(channelVM);
                    }
                    else if (oldGuildChanneld is SocketVoiceChannel)
                    {
                        _VoiceChannels.Remove(oldGuildChanneld as SocketVoiceChannel);
                    }

                }
            }

            return Task.CompletedTask;
        }

    }
}
