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
using System.Diagnostics;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class VoiceChannelPageViewModel : UncordPageViewModelBase
    {
        private ReactiveProperty<SocketVoiceChannel> VoiceChannel { get; }
        public ulong? VoiceChannelId => VoiceChannel.Value?.Id;

        public ReactiveProperty<string> VoiceChannelName { get; }

        public ObservableCollection<SocketUser> Users { get; }

        public ReadOnlyReactiveProperty<bool> IsConnectedVoiceChannel { get; }

        AsyncLock _VoiceChannelUserUpdateLock = new AsyncLock();

        public VoiceChannelPageViewModel()
        {
            VoiceChannelName = new ReactiveProperty<string>();
            Users = new ObservableCollection<SocketUser>();

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
            
            DiscordContext.DiscordSocketClient.UserVoiceStateUpdated += DiscordSocketClient_UserVoiceStateUpdated;
            
            RefreshUser().ConfigureAwait(false);

            base.OnNavigatedTo(e, viewModelState);
        }

        private async Task RefreshUser()
        {
            using (var releaser = await _VoiceChannelUserUpdateLock.LockAsync())
            {
                Users.Clear();

                if (VoiceChannel.Value == null)
                {
                    return;
                }

                foreach (var user in VoiceChannel.Value.Users)
                {
                    Users.Add(user);
                }
            }
        }

        private async Task DiscordSocketClient_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            using (var releaser = await _VoiceChannelUserUpdateLock.LockAsync())
            {
                UIDispatcherScheduler.Default.Schedule(this, 
                    (scheduler, state) => 
                    {
                        if (arg2.VoiceChannel == null && arg3.VoiceChannel != null)
                        {
                            if (arg3.VoiceChannel == VoiceChannel.Value)
                            {
                                Users.Add(arg1);
                            }
                        }
                        else if (arg2.VoiceChannel != null && arg3.VoiceChannel == null)
                        {
                            if (arg2.VoiceChannel == VoiceChannel.Value)
                            {
                                Users.Remove(arg1);
                            }
                        }
                        else
                        {
                            // Update
                        }

                        return default(IDisposable);
                    });

                Debug.WriteLine(arg1.Username);
            }
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            VoiceChannelName.Value = "";
            Users.Clear();

            if (DiscordContext.DiscordSocketClient != null)
            {
                DiscordContext.DiscordSocketClient.UserVoiceStateUpdated -= DiscordSocketClient_UserVoiceStateUpdated;
            }


            base.OnNavigatingFrom(e, viewModelState, suspending);
        }


    }
}
