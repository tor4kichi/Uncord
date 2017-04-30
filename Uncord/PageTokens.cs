using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord
{
    public sealed class PageTokens
    {
        public static string AccountLoginPageToken = ToPageToken(typeof(Views.AccountLoginPage));
        public static string LoggedInProcessPageToken = ToPageToken(typeof(Views.LoggedInProcessPage));

        public static string PortalPageToken = ToPageToken(typeof(Views.PortalPage));
        public static string SettingsToken = ToPageToken(typeof(Views.SettingsPage));

        public static string FriendsToken = ToPageToken(typeof(Views.FriendsPage));
        public static string GuildChannelsToken = ToPageToken(typeof(Views.GuildChannelsPage));
        public static string TextChannelToken = ToPageToken(typeof(Views.TextChannelPage));





        public static string ToPageToken(Type type)
        {
            var name = type.Name;

            if (name.EndsWith("Page"))
            {
                // "Page"を削った文字列を返す
                return name.Substring(0, name.LastIndexOf("Page"));
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
