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
using Uncord.Models;

namespace Uncord.ViewModels
{
    public class VoiceChannelStatusViewModel : BindableBase
    {
        CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        INavigationService NavigationService { get; }

        public Models.DiscordContext DiscordContext { get; }
        public Models.AudioPlaybackManager AudioManager { get; }

        public ReadOnlyReactiveProperty<bool> IsConnectVoiceChannel { get; }

        public ReadOnlyReactiveProperty<string> CurrentVoiceChannelName { get; }
        public ReadOnlyReactiveProperty<string> CurrentServerName { get; }

        public ReadOnlyReactiveProperty<InputDeviceState> InputDeviceState { get; }
        public ReadOnlyReactiveProperty<bool> HasMicError { get; }

        private ReactiveProperty<bool> IsMicMute_Internal { get; }
        private ReactiveProperty<bool> IsSpeakerMute_Internal { get; }

        public ReadOnlyReactiveProperty<bool> IsMicMute { get; }
        public ReadOnlyReactiveProperty<bool> IsSpeakerMute { get; }


        public VoiceChannelStatusViewModel(Models.DiscordContext discordContext, Models.AudioPlaybackManager audioManager, INavigationService navService)
        {
            DiscordContext = discordContext;
            AudioManager = audioManager;
            NavigationService = navService;

            IsSpeakerMute_Internal = AudioManager.ToReactivePropertyAsSynchronized(x => x.IsSpeakerMute);
            IsMicMute_Internal = AudioManager.ToReactivePropertyAsSynchronized(x => x.IsMicMute);

            IsSpeakerMute = IsSpeakerMute_Internal
                .ToReadOnlyReactiveProperty();

            // リアルスピーカーミュートまたは内部マイクミュートの場合、
            // リアルマイクミュートがオンになる
            IsMicMute = Observable.CombineLatest(
                IsSpeakerMute,
                IsMicMute_Internal
                )
                .Select(x => x.Any(y => y))
                .ToReadOnlyReactiveProperty();

            IsConnectVoiceChannel = DiscordContext
                .ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x != null)
                .ToReadOnlyReactiveProperty();

            CurrentVoiceChannelName = DiscordContext
                .ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x?.Name ?? "")
                .ToReadOnlyReactiveProperty();

            CurrentServerName = DiscordContext
                .ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x?.Guild.Name ?? "")
                .ToReadOnlyReactiveProperty();

            InputDeviceState = AudioManager.ObserveProperty(x => x.InputDeviceState)
                .ToReadOnlyReactiveProperty();
            HasMicError = InputDeviceState
                .Select(x => x != Models.InputDeviceState.Avairable)
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

        private DelegateCommand _ToggleMicMuteCommand;
        public DelegateCommand ToggleMicMuteCommand
        {
            get
            {
                return _ToggleMicMuteCommand
                    ?? (_ToggleMicMuteCommand = new DelegateCommand(() =>
                    {
                        if (IsSpeakerMute_Internal.Value)
                        {
                            IsSpeakerMute_Internal.Value = false;
                            IsMicMute_Internal.Value = false;
                        }
                        else
                        {
                            IsMicMute_Internal.Value = !IsMicMute_Internal.Value;
                        }
                    }));
            }
        }

        private DelegateCommand _ToggleSpeakerMuteCommand;
        public DelegateCommand ToggleSpeakerMuteCommand
        {
            get
            {
                return _ToggleSpeakerMuteCommand
                    ?? (_ToggleSpeakerMuteCommand = new DelegateCommand(() =>
                    {
                        IsSpeakerMute_Internal.Value = !IsSpeakerMute_Internal.Value;
                    }));
            }
        }


        private DelegateCommand _UpdateMicCommand;
        public DelegateCommand UpdateMicCommand
        {
            get
            {
                return _UpdateMicCommand
                    ?? (_UpdateMicCommand = new DelegateCommand(() =>
                    {
                        AudioManager.ResetMic();
                    }));
            }
        }
    }
}
