using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord
{
    public sealed class PageTokens
    {
        public static string EmptyPageToken = ToPageToken(typeof(Views.EmptyPage));


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
