using Discord.WebSocket;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncord.Models;

namespace Uncord.ViewModels
{
    public class ServerListPageViewModel : UncordPageViewModelBase
    {
        INavigationService _NavigationService;
        DiscordContext _DiscordContext;

        public ReadOnlyReactiveCollection<SocketGuild> Guilds { get; private set; }


        public ServerListPageViewModel(Models.DiscordContext discordContext, INavigationService navService)
        {
            _DiscordContext = discordContext;
            _NavigationService = navService;

            Guilds = _DiscordContext.Guilds;
        }
    }
}
