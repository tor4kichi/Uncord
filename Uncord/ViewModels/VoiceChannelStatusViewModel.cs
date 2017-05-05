using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.ViewModels
{
    public class VoiceChannelStatusViewModel : BindableBase
    {
        CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        INavigationService NavigationService { get; }

        public Models.DiscordContext DiscordContext { get; }

        public ReadOnlyReactiveProperty<bool> IsConnectVoiceChannel { get; }

        public ReadOnlyReactiveProperty<string> CurrentVoiceChannelName { get; }

        public VoiceChannelStatusViewModel(Models.DiscordContext discordContext, INavigationService navService)
        {
            DiscordContext = discordContext;
            NavigationService = navService;

            IsConnectVoiceChannel = DiscordContext
                .ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x != null)
                .ToReadOnlyReactiveProperty();

            CurrentVoiceChannelName = DiscordContext
                .ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x?.Name ?? "")
                .ToReadOnlyReactiveProperty();
        }

        private DelegateCommand _OpenVoiceChannelPageCommand;
        public DelegateCommand OpenVoiceChannelPageCommand
        {
            get
            {
                return _OpenVoiceChannelPageCommand
                    ?? (_OpenVoiceChannelPageCommand = new DelegateCommand(() =>
                    {
                        if (DiscordContext.CurrentVoiceChannel != null)
                        {
                            NavigationService.Navigate(
                                PageTokens.VoiceChannelPageToken, 
                                DiscordContext.CurrentVoiceChannel.Id
                                );
                        }
                    }));
            }
        }

        private DelegateCommand _DisconnectVoiceChannelCommand;
        public DelegateCommand DisconnectVoiceChannelCommand
        {
            get
            {
                return _DisconnectVoiceChannelCommand
                    ?? (_DisconnectVoiceChannelCommand = new DelegateCommand(async () =>
                    {
                        // Voiceチャンネルの選択を解除
                        await DiscordContext.DisconnectCurrentVoiceChannel();
                    }));
            }
        }
    }
}
