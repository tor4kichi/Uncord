using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Discord.WebSocket;
using Reactive.Bindings;

namespace Uncord.ViewModels
{
    public class GuildPageViewModel : ViewModelBase
    {
        Models.DiscordContext _DiscordContext;

        SocketGuild _Guild;

        public ReactiveProperty<string> GuildName { get; private set; }

        public GuildPageViewModel(Models.DiscordContext discordContext)
        {
            _DiscordContext = discordContext;
            GuildName = new ReactiveProperty<string>();
        }
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is ulong)
            {
                var guildId = (ulong)e.Parameter;
                _Guild = _DiscordContext.GetGuild(guildId);
            }

            if (_Guild == null)
            {
                return;
            }

            GuildName.Value = _Guild.Name;

            base.OnNavigatedTo(e, viewModelState);
        }
    }
}
