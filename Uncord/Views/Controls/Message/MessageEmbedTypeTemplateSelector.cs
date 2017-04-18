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

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is string)
            {
                switch(item as string)
                {
                    case "image":
                        break;
                    case "video":
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
