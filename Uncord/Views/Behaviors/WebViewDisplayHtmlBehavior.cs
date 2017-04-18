using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uncord.Views.Behaviors
{
    public class WebViewDisplayHtmlBehavior : Behavior<WebView>
    {

        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.Register(
                nameof(Html), 
                typeof(string),
                typeof(WebViewDisplayHtmlBehavior), 
                new PropertyMetadata(null, (x, y) => 
                {
                    var me = x as WebViewDisplayHtmlBehavior;
                    me.SetHtml(me.Html);
                })
                );

        public string Html
        {
            get { return (string)GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }


        private void SetHtml(string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                AssociatedObject.NavigateToString(html);
            }
        }
    }
}
