using Discord;
using Discord.WebSocket;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRTXamlToolkit.Async;

namespace Uncord.ViewModels
{
    public class GuildTextChannelViewModel : BindableBase, IDisposable
    {
        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public SocketTextChannel TextChannel { get; private set; }


        public string Name { get; private set; }

        // 状態
        public ReactiveProperty<bool> NowSendingMessage { get; private set; }


        // メッセージ読み取り
        AsyncLock _MessageUpdateLock = new AsyncLock();
        ObservableCollection<Discord.IMessage> _Messages;
        public ReadOnlyReactiveCollection<MessageViewModel> Messages { get; private set; }

        // メッセージ書き込み
        public ReactiveProperty<string> SendMessageText { get; private set; }
        public ReactiveCommand SendMessageCommand { get; private set; }



        public GuildTextChannelViewModel(SocketTextChannel textChannel)
        {
            TextChannel = textChannel;

            Name = TextChannel.Name;

            NowSendingMessage = new ReactiveProperty<bool>()
                .AddTo(_CompositeDisposable);

            _Messages = new ObservableCollection<IMessage>();
            
            Messages = _Messages
                .CollectionChangedAsObservable()
                .Where((x) => 
                {
                    if (x.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        var items = x.NewItems;
                        // 前回受け取ったメッセージと同じ送信者の場合は
                        // 前回生成したMessageVMにIMessageをまとめる
                        foreach (var item in items.Cast<IMessage>())
                        {
                            var lastMessage = Messages.LastOrDefault();
                            if (lastMessage?.IsSameAuthor(item) ?? false)
                            {
                                lastMessage.AddMessage(item);
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    else if (x.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        var items = x.OldItems;
                        // 前回受け取ったメッセージと同じ送信者の場合は
                        // 前回生成したMessageVMにIMessageをまとめる
                        foreach (var item in items.Cast<IMessage>())
                        {
                            foreach (var message in Messages)
                            {
                                if (message.TryRemoveMessage(item))
                                {
                                    break;
                                }
                            }
                        }

                        return false;
                    }

                    return false;
                })
                .Select(x => new MessageViewModel(x.NewItems[0] as IMessage))
                .SubscribeOnUIDispatcher()
                .ToReadOnlyReactiveCollection()
                .AddTo(_CompositeDisposable);

            SendMessageText = new ReactiveProperty<string>("")
                .AddTo(_CompositeDisposable);
            SendMessageCommand = 
                Observable.CombineLatest(
                    NowSendingMessage.Select(x => !x),
                    SendMessageText.Select(x => !string.IsNullOrEmpty(x))
                    )
                .Select(x => x.All(y => y))
                .ToReactiveCommand()
                .AddTo(_CompositeDisposable);

            SendMessageCommand.Subscribe(async x => await SendMessage(SendMessageText.Value))
                .AddTo(_CompositeDisposable);

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



        private async Task Discord_MessageReceived(SocketMessage newMessage)
        {
            using (var releaser = await _MessageUpdateLock.LockAsync())
            {
                if (newMessage.Channel.Id == TextChannel.Id)
                {
                    _Messages.Add(newMessage);

                    await Task.Delay(50);
                }
            }
        }

        public async Task Load()
        {
            using (var releaser = await _MessageUpdateLock.LockAsync())
            {
                if (_Messages.Count > 0) { return; }

                if (TextChannel == null) { throw new Exception(); }

                var rawMessages = await TextChannel.GetMessagesAsync().Flatten();

                foreach (var message in rawMessages.Reverse().ToArray())
                {
                    _Messages.Add(message);

                    await Task.Delay(1);
                }
            }
        }

        public void Dispose()
        {
            if (TextChannel != null)
            {
                TextChannel.Discord.MessageReceived -= Discord_MessageReceived;
            }

            _CompositeDisposable.Dispose();
        }
    }


    public class MessageViewModel : BindableBase
    {
        public ObservableCollection<IMessage> Messages { get; private set; } = new ObservableCollection<IMessage>();

        public IMessage MessageSample { get; private set; }
        public IUser Author { get; private set; }
        public string AuthorAvatarUrl { get; private set; }
        public DateTime MessageRecievedAt { get; private set; }

        public MessageViewModel(IMessage firstMessage)
        {
            Messages.Add(firstMessage);
            MessageSample = firstMessage;

            Author = MessageSample.Author;
            AuthorAvatarUrl = MessageSample.Author.GetAvatarUrl(size:64);
            MessageRecievedAt = firstMessage.Timestamp.LocalDateTime;
        }

        public bool IsSameAuthor(IMessage message)
        {
            return Messages.First().Author.Id == message.Author.Id;
        }

        public void AddMessage(IMessage message)
        {
            Messages.Add(message);

            MessageRecievedAt = message.Timestamp.LocalDateTime;
            RaisePropertyChanged(nameof(MessageRecievedAt));
        }

        // TODO: 削除した場合にnullを挿入して削除されたメッセージとしてUI上に表示できるようにする
        public bool TryRemoveMessage(IMessage message)
        {
            return Messages.Remove(message);
        }
    }
}
