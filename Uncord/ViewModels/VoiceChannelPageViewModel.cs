using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Discord.WebSocket;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Prism.Commands;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace Uncord.ViewModels
{
    public class VoiceChannelPageViewModel : UncordPageViewModelBase
    {
        private ReactiveProperty<SocketVoiceChannel> VoiceChannel { get; }
        public ulong? VoiceChannelId => VoiceChannel.Value?.Id;

        public ReactiveProperty<string> VoiceChannelName { get; }

        public ObservableCollection<SocketGuildUser> Users { get; }

        public ReadOnlyReactiveProperty<bool> IsConnectedVoiceChannel { get; }

        public VoiceChannelPageViewModel()
        {
            VoiceChannelName = new ReactiveProperty<string>();
            Users = new ObservableCollection<SocketGuildUser>();

            VoiceChannel = new ReactiveProperty<SocketVoiceChannel>();

            IsConnectedVoiceChannel = 
                Observable.CombineLatest(
                    VoiceChannel,
                    DiscordContext.ObserveProperty(x => x.CurrentVoiceChannel)
                    )
                .Select(x => x[0] != null && x[0] == x[1])
                .ToReadOnlyReactiveProperty();
                
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is ulong)
            {
                var channelId = (ulong)e.Parameter;

                VoiceChannel.Value = DiscordContext.DiscordSocketClient.GetChannel(channelId) as SocketVoiceChannel;
            }

            if (VoiceChannel.Value == null)
            {
                return;
            }

            var voiceChannel = VoiceChannel.Value;
            VoiceChannelName.Value = voiceChannel.Name;
            
            foreach (var user in voiceChannel.Users)
            {
                Users.Add(user);
            }

            base.OnNavigatedTo(e, viewModelState);
        }


        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            VoiceChannelName.Value = "";
            Users.Clear();

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }


    }
}
