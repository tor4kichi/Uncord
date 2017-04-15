using Discord.WebSocket;
using Prism.Commands;
using Prism.Mvvm;
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
    public class GuildListViewModel : BindableBase
    {
        INavigationService _NavigationService;
        DiscordContext _DiscordContext;

        public ReadOnlyReactiveCollection<SocketGuild> Guilds { get; private set; }

        public DelegateCommand<SocketGuild> OpenGuildCommand { get; private set; }


        public GuildListViewModel(Models.DiscordContext discordContext, INavigationService navService)
        {
            _DiscordContext = discordContext;
            _NavigationService = navService;

            Guilds = _DiscordContext.Guilds;

            OpenGuildCommand = new DelegateCommand<SocketGuild>((guild) => 
            {
                _NavigationService.Navigate(PageTokens.GuildPageToken, guild.Id);
            });
        }
    }
}
