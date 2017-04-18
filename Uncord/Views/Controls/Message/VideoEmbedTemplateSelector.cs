using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uncord.Views.Controls.Message
{
    public class VideoEmbedTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Youtube { get; set; }
        public DataTemplate Niconico { get; set; }
        public DataTemplate Other { get; set; }

        // http://stackoverflow.com/questions/3717115/regular-expression-for-youtube-links
        public static readonly Regex YoutubeUrlRegex = new Regex("(?:https?:\\/\\/)?(?:www\\.)?youtu\\.?be(?:\\.com)?\\/?.*(?:watch|embed)?(?:.*v=|v\\/|\\/)([\\w\\-_]+)\\&?");
        
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is Discord.IEmbed)
            {
                var embed = item as Discord.IEmbed;
                if (embed.Video.HasValue)
                {
                    var video = embed.Video.Value;
                    var url = video.Url;
                    if (YoutubeUrlRegex.IsMatch(url))
                    {
                        return Youtube;
                    }
                }
            }
            return Other;
        }
    }
}
