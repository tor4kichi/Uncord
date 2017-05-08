using Discord;
using Discord.WebSocket;
using Prism.Commands;
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
    public class MessageAggregatedByAuthorViewModel : BindableBase
    {
        public ObservableCollection<MessageViewModel> Messages { get; private set; } = new ObservableCollection<MessageViewModel>();

        public IMessage MessageSample { get; private set; }
        public IUser Author { get; private set; }
        public string AuthorAvatarUrl { get; private set; }
        public DateTime MessageRecievedAt { get; private set; }

        public MessageAggregatedByAuthorViewModel(IMessage firstMessage)
        {
            Messages.Add(new MessageViewModel(firstMessage));
            MessageSample = firstMessage;

            Author = MessageSample.Author;
            AuthorAvatarUrl = MessageSample.Author.GetAvatarUrl(size:64);
            MessageRecievedAt = firstMessage.Timestamp.LocalDateTime;
        }

        public bool IsSameAuthor(IMessage message)
        {
            return Messages.First().Message.Author.Id == message.Author.Id;
        }

        public void AddMessage(IMessage message)
        {
            Messages.Add(new MessageViewModel(message));

            MessageRecievedAt = message.Timestamp.LocalDateTime;
            RaisePropertyChanged(nameof(MessageRecievedAt));
        }

        // TODO: 削除した場合にnullを挿入して削除されたメッセージとしてUI上に表示できるようにする
        public bool TryRemoveMessage(IMessage message)
        {
            var target = Messages.FirstOrDefault(x => x.Message.Id == message.Id);
            if (target == null) { return false; }
            return Messages.Remove(target);
        }

        
    }

    public class MessageViewModel : BindableBase
    {
        public IMessage Message { get; }

        public MessageViewModel(IMessage message)
        {
            Message = message;
        }


        private DelegateCommand _RemoveMessageCommand;
        public DelegateCommand RemoveMessageCommand
        {
            get
            {
                return _RemoveMessageCommand
                    ?? (_RemoveMessageCommand = new DelegateCommand(async () =>
                    {
                        try
                        {
                            await Message.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                    ));
            }
        }
    }
}
