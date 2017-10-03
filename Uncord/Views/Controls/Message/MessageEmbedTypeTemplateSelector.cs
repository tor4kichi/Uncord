using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uncord.Views.Controls.Message
{
    public sealed class MessageEmbedTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Image { get; set; }
        public DataTemplate Video { get; set; }
        public DataTemplate Link { get; set; }
        public DataTemplate Tweet { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is Discord.IEmbed)
            {
                var embed = item as Discord.IEmbed;
                switch (embed.Type)
                {
                    case Discord.EmbedType.Image:
                        return Image;
                    case Discord.EmbedType.Video:
                        return Video;
                    case Discord.EmbedType.Link:
                        return Link;
                    case Discord.EmbedType.Tweet:
                        return Tweet;
                    default:
                        throw new NotSupportedException();
                }
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
