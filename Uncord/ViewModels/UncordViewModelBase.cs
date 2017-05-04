using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Uncord.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Uncord.ViewModels
{
    abstract public class UncordPageViewModelBase : ViewModelBase
    {
        private INavigationService NavigationService { get; }
        public DiscordContext DiscordContext { get; }
        public MenuViewModel MenuVM { get; }

        public UncordPageViewModelBase()
        {
            NavigationService = App.Current.Container.Resolve<INavigationService>();
            DiscordContext = App.Current.Container.Resolve<DiscordContext>();
            MenuVM = App.Current.Container.Resolve<MenuViewModel>();
        }



        protected void NavigatePage(string token, object payload = null)
        {
            NavigationService.Navigate(token, payload);
        }

        protected void NavigatePage(string token, NavigationPayloadBase payload)
        {
            NavigatePage(token, payload.Serialize());
        }




        private DelegateCommand _OpenMenuCommand;
        public DelegateCommand OpenMenuCommand
        {
            get
            {
                return _OpenMenuCommand
                    ?? (_OpenMenuCommand = new DelegateCommand(() =>
                    {
                        MenuVM.OpenMenu();
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
                        NavigatePage(PageTokens.ServerListPageToken);
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
                        NavigatePage(PageTokens.SettingsToken);
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
                        // TODO: ユーザーIDを指定してオープンに対応（DM表示）
                        NavigatePage(PageTokens.FriendsPageToken);
                    }));
            }
        }

        private DelegateCommand<ulong?> _OpenGuildChannelsPageCommand;
        public DelegateCommand<ulong?> OpenGuildChannelsPageCommand
        {
            get
            {
                return _OpenGuildChannelsPageCommand
                    ?? (_OpenGuildChannelsPageCommand = new DelegateCommand<ulong?>((guildId) =>
                    {
                        if (guildId.HasValue)
                        {
                            NavigatePage(PageTokens.GuildChannelsPageToken, guildId.Value);
                        }
                    }));
            }
        }

        private DelegateCommand<ulong?> _OpenTextChannelPageCommand;
        public DelegateCommand<ulong?> OpenTextChannelPageCommand
        {
            get
            {
                return _OpenTextChannelPageCommand
                    ?? (_OpenTextChannelPageCommand = new DelegateCommand<ulong?>((textChannelId) =>
                    {
                        if (textChannelId.HasValue)
                        {
                            NavigatePage(PageTokens.TextChannelPageToken, textChannelId.Value);
                        }
                    }));
            }
        }

        private DelegateCommand<ulong?> _ConnectVoiceChannelCommand;
        public DelegateCommand<ulong?> ConnectVoiceChannelCommand
        {
            get
            {
                return _ConnectVoiceChannelCommand
                    ?? (_ConnectVoiceChannelCommand = new DelegateCommand<ulong?>(async (voiceChannelId) =>
                    {
                        // Voiceチャンネルを選択
                        if (voiceChannelId.HasValue)
                        {
                            await DiscordContext.ConnectVoiceChannel(voiceChannelId.Value);
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

        private DelegateCommand _LogoutCommand;
        public DelegateCommand LogoutCommand
        {
            get
            {
                return _LogoutCommand
                    ?? (_LogoutCommand = new DelegateCommand(() =>
                    {
                        // TODO: ログアウト処理とログインページへの遷移
                        // DiscordContextのLogout呼び出し
                        // LoginPageへのルーティング
                    }));
            }
        }
        

    }

    abstract public class UncordPageViewModelWithPayload<T> : UncordPageViewModelBase
         where T : NavigationPayloadBase
    {
        public bool IsRequirePayload { get; }

        public T Payload { get; private set; }

        public UncordPageViewModelWithPayload(bool isRequirePayload = true)
        {
            IsRequirePayload = isRequirePayload;
        }


        
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                try
                {
                    Payload = NavigationPayloadBase.Deserialize<T>(e.Parameter as string);
                }
                catch when (IsRequirePayload)
                {
                    throw;
                }
                
                OnPayloadDeserialized(Payload);
                
            }


            base.OnNavigatedTo(e, viewModelState);
        }


        protected abstract void OnPayloadDeserialized(T payload);
    }
}
