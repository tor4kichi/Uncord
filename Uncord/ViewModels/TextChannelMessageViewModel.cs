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
    public class TextChannelMessageViewModel : BindableBase
    {
        public ObservableCollection<IMessage> Messages { get; private set; } = new ObservableCollection<IMessage>();

        public IMessage MessageSample { get; private set; }
        public IUser Author { get; private set; }
        public string AuthorAvatarUrl { get; private set; }
        public DateTime MessageRecievedAt { get; private set; }

        public TextChannelMessageViewModel(IMessage firstMessage)
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
