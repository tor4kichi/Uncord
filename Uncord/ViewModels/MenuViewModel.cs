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

        public ReadOnlyReactiveProperty<bool> HasCurrentVoiceChannel { get; }

        public ReadOnlyReactiveProperty<string> CurrentVoiceChannelName { get; }

        public MenuViewModel(DiscordContext discordContext, INavigationService navService)
        {
            DiscordContext = discordContext;
            NavigationService = navService;

            LoginUserName = DiscordContext.ObserveProperty(x => x.CurrentUser)
                .Select(x => x?.Username ?? "")
                .ToReadOnlyReactiveProperty();

            LoginUserAvaterUrl = DiscordContext.ObserveProperty(x => x.CurrentUser)
                .Select(x => x?.GetAvatarUrl() ?? "")
                .ToReadOnlyReactiveProperty();

            HasCurrentVoiceChannel = DiscordContext.ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x != null)
                .ToReadOnlyReactiveProperty();

            CurrentVoiceChannelName = DiscordContext.ObserveProperty(x => x.CurrentVoiceChannel)
                .Select(x => x?.Name ?? "")
                .ToReadOnlyReactiveProperty();
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


        private DelegateCommand _OpenPortalPageCommand;
        public DelegateCommand OpenPortalPageCommand
        {
            get
            {
                return _OpenPortalPageCommand
                    ?? (_OpenPortalPageCommand = new DelegateCommand(() =>
                    {
                        NavigationService.Navigate(PageTokens.ServerListPageToken, null);
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
