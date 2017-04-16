using Discord;
using Discord.WebSocket;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class GuildTextChannelViewModel : BindableBase, IDisposable
    {
        public SocketTextChannel TextChannel { get; private set; }


        public string Name { get; private set; }

        // 状態
        public ReactiveProperty<bool> NowSendingMessage { get; private set; }


        // メッセージ読み取り
        AsyncLock _MessageLoadingLock = new AsyncLock();
        ObservableCollection<Discord.IMessage> _Messages;
        public ReadOnlyReactiveCollection<Discord.IMessage> Messages { get; private set; }

        // メッセージ書き込み
        public ReactiveProperty<string> SendMessageText { get; private set; }
        public ReactiveCommand SendMessageCommand { get; private set; }


        public GuildTextChannelViewModel(SocketTextChannel textChannel)
        {
            TextChannel = textChannel;

            Name = TextChannel.Name;

            NowSendingMessage = new ReactiveProperty<bool>();

            _Messages = new ObservableCollection<IMessage>();
            Messages = _Messages.ToReadOnlyReactiveCollection();

            SendMessageText = new ReactiveProperty<string>("");
            SendMessageCommand = 
                Observable.CombineLatest(
                    NowSendingMessage.Select(x => !x),
                    SendMessageText.Select(x => !string.IsNullOrEmpty(x))
                    )
                .Select(x => x.All(y => y))
                .ToReactiveCommand();

            SendMessageCommand.Subscribe(async x => await SendMessage(SendMessageText.Value));

            TextChannel.Discord.MessageReceived += Discord_MessageReceived;
        }

        private async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            try
            {
                NowSendingMessage.Value = true;
                var userMessage = await TextChannel.SendMessageAsync(message);

                SendMessageText.Value = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                NowSendingMessage.Value = false;
            }


        }



        private Task Discord_MessageReceived(SocketMessage newMessage)
        {
            if (newMessage.Channel.Id == TextChannel.Id)
            {
                _Messages.Add(newMessage);
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
