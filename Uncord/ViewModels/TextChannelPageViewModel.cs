﻿using Discord;
using Discord.WebSocket;
using Prism.Mvvm;
using Prism.Windows.Navigation;
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
    public class TextChannelPageViewModel : UncordPageViewModelBase
    {

        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public DiscordSocketClient SocketClient => DiscordContext.DiscordSocketClient;

        public SocketTextChannel TextChannel { get; private set; }


        public string Name { get; private set; }

        // 状態
        public ReactiveProperty<bool> NowSendingMessage { get; private set; }


        // メッセージ読み取り
        AsyncLock _MessageUpdateLock = new AsyncLock();
        ObservableCollection<Discord.IMessage> _Messages;
        public ReadOnlyReactiveCollection<TextChannelMessageViewModel> Messages { get; private set; }

        // メッセージ書き込み
        public ReactiveProperty<string> SendMessageText { get; private set; }
        public ReactiveCommand SendMessageCommand { get; private set; }


        public ReactiveProperty<bool> IsInvalidTextChannel { get; }

        public TextChannelPageViewModel()
        {
            IsInvalidTextChannel = new ReactiveProperty<bool>(false);

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
                .Select(x => new TextChannelMessageViewModel(x.NewItems[0] as IMessage))
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
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            // ログインしていない？
            if (SocketClient == null)
            {
                IsInvalidTextChannel.Value = true;

                Debug.WriteLine("TextChannelPage にテキストチャンネルID(ulong)が正常に渡されませんでした。");

                return;
            }

            // ナビゲーションパラメータにはテキストチャンネルIDが必要
            if (e.Parameter is ulong)
            {
                var textChannelId = (ulong)e.Parameter;

                TextChannel = SocketClient.GetChannel(textChannelId) as SocketTextChannel;
            }

            // テキストチャンネルが見つからない場合は異常をページ表示に反映
            if (TextChannel == null)
            {
                IsInvalidTextChannel.Value = true;

                Debug.WriteLine("TextChannelPage にテキストチャンネルID(ulong)が正常に渡されませんでした。");

                return;
            }

            Name = TextChannel.Name;
            RaisePropertyChanged(nameof(Name));

            // テキストチャンネルページを表示中だけメッセージ受信を処理する
            SocketClient.MessageReceived += Discord_MessageReceived;

            _Messages.Clear();

            await _MessageUpdateLock.LockAsync()
                .ContinueWith(async x => 
                {
                    using (x.Result)
                    {
                        if (_Messages.Count > 0) { return; }

                        if (TextChannel == null) { throw new Exception(); }

                        var rawMessages = await TextChannel.GetMessagesAsync().Flatten();

                        UIDispatcherScheduler.Default.Schedule(this, TimeSpan.Zero, (scheeduler, state) =>
                        {
                            foreach (var message in rawMessages.Reverse().ToArray())
                            {
                                _Messages.Add(message);
                            }

                            return null;

                        });
                    }
                })
                .ConfigureAwait(false);
            
            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            // メッセージ受信処理を終了
            if (SocketClient != null)
            {
                SocketClient.MessageReceived -= Discord_MessageReceived;
            }

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }



        private async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }

            using (var releaser = await _MessageUpdateLock.LockAsync())
            {
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

        
        public void Dispose()
        {
            _CompositeDisposable.Dispose();
        }
    }
}