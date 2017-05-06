using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using System.Reactive.Linq;
using Uncord.Models;
using Prism.Commands;

namespace Uncord.ViewModels
{
    public class AccountLoginPageViewModel : ViewModelBase
    {
        DiscordContext _DiscordContext;
        INavigationService _NavigationService;

        public ReactiveProperty<string> Mail { get; private set; }
        public ReactiveProperty<string> Password { get; private set; }

        public ReactiveCommand TryLoginCommand { get; private set; }

        public ReactiveProperty<bool> NowTryLogin { get; private set; }

        public AccountLoginPageViewModel(DiscordContext discordContext, INavigationService navService)
        {
            _DiscordContext = discordContext;
            _NavigationService = navService;

            Mail = new ReactiveProperty<string>("");
            Password = new ReactiveProperty<string>("");

            TryLoginCommand = Observable.CombineLatest(
                Mail.Select(x => !string.IsNullOrEmpty(x)), /* TODO: check with regex */
                Password.Select(x => !string.IsNullOrWhiteSpace(x))
                )
                .Select(x => x.All(y => y))
                .ToReactiveCommand();

            NowTryLogin = new ReactiveProperty<bool>(false);

            TryLoginCommand.Subscribe(async _ => await TryLogin(Mail.Value, Password.Value));

            if (_DiscordContext.TryGetRecentLoginAccount(out var mailAndPassword))
            {
                Mail.Value = mailAndPassword.Item1;
                Password.Value = mailAndPassword.Item2;
            }
        }


        private async Task TryLogin(string mail, string password)
        {
            try
            {
                NowTryLogin.Value = true;

                var isLoginSuccess = await _DiscordContext.TryLogin(mail, password);
                if (isLoginSuccess)
                {
                    _NavigationService.Navigate(PageTokens.LoggedInProcessPageToken, null);

                    _NavigationService.ClearHistory();
                }
                else
                {
                    // ログイン失敗
                }
            }
            finally
            {
                NowTryLogin.Value = false;
            }
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            (App.Current as App).IsHideMenu = true;

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            (App.Current as App).IsHideMenu = false;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }
}
