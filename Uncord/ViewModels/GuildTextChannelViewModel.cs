using Discord;
using Discord.WebSocket;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class GuildTextChannelViewModel : BindableBase, IDisposable
    {
        public string Name { get; private set; }

        public SocketTextChannel TextChannel { get; private set; }

        ObservableCollection<Discord.IMessage> _Messages;
        public ReadOnlyReactiveCollection<Discord.IMessage> Messages { get; private set; }

        public ReactiveProperty<string> SendMessage { get; private set; }

        AsyncLock _MessageLoadingLock = new AsyncLock();

        public GuildTextChannelViewModel(SocketTextChannel textChannel)
        {
            TextChannel = textChannel;

            Name = TextChannel.Name;
            _Messages = new ObservableCollection<IMessage>();
            Messages = _Messages.ToReadOnlyReactiveCollection();

            SendMessage = new ReactiveProperty<string>("");

            TextChannel.Discord.MessageReceived += Discord_MessageReceived;
        }

        private Task Discord_MessageReceived(SocketMessage newMessage)
        {
            if (newMessage.Channel.Id == TextChannel.Id)
            {
                _Messages.Insert(0, newMessage);
            }

            return Task.CompletedTask;
        }

        public async Task Load()
        {
            using (var releaser = await _MessageLoadingLock.LockAsync())
            {
                if (_Messages.Count > 0) { return; }

                if (TextChannel == null) { throw new Exception(); }

                var rawMessages = await TextChannel.GetMessagesAsync().Flatten();

                foreach (var message in rawMessages)
                {
                    _Messages.Insert(0, message);
                }
            }
        }

        public void Dispose()
        {
            if (TextChannel != null)
            {
                TextChannel.Discord.MessageReceived -= Discord_MessageReceived;
            }
        }
    }
}
