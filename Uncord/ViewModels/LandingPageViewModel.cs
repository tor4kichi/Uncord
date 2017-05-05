using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;

namespace Uncord.ViewModels
{
    public class LandingPageViewModel : UncordPageViewModelBase
    {
        public LandingPageViewModel()
        {

        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            (App.Current as App).OpenMenu();

            base.OnNavigatedTo(e, viewModelState);
        }
    }
}
