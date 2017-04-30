using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.ViewModels
{
    public class FriendsPageViewModel : UncordPageViewModelBase
    {
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is ulong)
            {
                var userId = (ulong)e.Parameter;

            }

            base.OnNavigatedTo(e, viewModelState);
        }

    }
}
