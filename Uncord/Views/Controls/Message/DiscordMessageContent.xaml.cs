using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uncord.Views.Controls
{
    public sealed partial class DiscordMessageContent : UserControl
    {
        public delegate string UserIdToUserNameConverter(string userId);

        public static UserIdToUserNameConverter UserIdToUserName { get; set; }


        public static readonly Regex MessageUserTagRegex = new Regex("<@(\\d*)>");

        public static string MessageContentToMarkdown(string content)
        {
            return MessageUserTagRegex.Replace(content,
                (x) =>
                {
                    var userId = x.Groups[1].Value;
                    var userName = UserIdToUserName?.Invoke(userId) ?? userId;
                    return $"[@{userName}](https://users/{userId})";
                });
        }

        public static readonly DependencyProperty MessageContentProperty =
           DependencyProperty.Register(
               nameof(MessageContent), // プロパティ名を指定
               typeof(string), // プロパティの型を指定
               typeof(DiscordMessageContent), // プロパティを所有する型を指定
               new PropertyMetadata("", (x, y) => 
               {
                   var me = x as DiscordMessageContent;
                   var content = me.MessageContent;
                   me.MarkdownMessageContent = MessageContentToMarkdown(content);
               })); // メタデータを指定。ここではデフォルト値を設定してる

        public string MessageContent
        {
            get { return (string)GetValue(MessageContentProperty); }
            set { SetValue(MessageContentProperty, value); }
        }


        public static readonly DependencyProperty MarkdownMessageContentProperty =
           DependencyProperty.Register(
               nameof(MarkdownMessageContent), // プロパティ名を指定
               typeof(string), // プロパティの型を指定
               typeof(DiscordMessageContent), // プロパティを所有する型を指定
               new PropertyMetadata("")); // メタデータを指定。ここではデフォルト値を設定してる

        public string MarkdownMessageContent
        {
            get { return (string)GetValue(MarkdownMessageContentProperty); }
            private set { SetValue(MarkdownMessageContentProperty, value); }
        }


        public DiscordMessageContent()
        {
            this.InitializeComponent();
        }
    }
}
