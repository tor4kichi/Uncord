using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml;
using Uncord.Models;
using Reactive.Bindings;

namespace Uncord.ViewModels
{
    public class LoggedInProcessPageViewModel : ViewModelBase
    {
        INavigationService _NavigationService;
        DiscordContext _DicordContext;

        public string Token { get; set; }

        public ReactiveProperty<string> LoginProcessStatus { get; }

        public LoggedInProcessPageViewModel(Models.DiscordContext dicordContext, INavigationService navService)
        {
            _NavigationService = navService;
            _DicordContext = dicordContext;

            Token = _DicordContext.DiscordAccessToken;
        }
            

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            (App.Current as App).IsHideMenu = true;

            

            Window.Current.Dispatcher
                .RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () => 
                {
                    await _DicordContext.WaitLoginAction();

                    LoginProcessStatus.Value = "ログイン完了";

                    await Task.Delay(TimeSpan.FromSeconds(0));

                    _NavigationService.Navigate(PageTokens.LandingPageToken, null);

                    _NavigationService.ClearHistory();

                    (App.Current as App).IsHideMenu = false;
                })
                .AsTask()
                .ConfigureAwait(false);
            

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            (App.Current as App).IsHideMenu = false;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }
}
