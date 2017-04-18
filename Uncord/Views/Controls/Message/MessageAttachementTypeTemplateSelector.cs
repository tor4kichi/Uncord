using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uncord.Views.Controls.Message
{
    public sealed class MessageAttachementTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Image { get; set; }
        public DataTemplate Other { get; set; }


        private string[] ImageFileExtentions = new string[] 
        {
            ".png",
            ".jpg",
        };


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is Discord.IAttachment)
            {
                var attachment = item as Discord.IAttachment;
                if (ImageFileExtentions.Any(x => attachment.Filename.EndsWith(x)))
                {
                    return Image;
                }
            }

            return Other ?? base.SelectTemplateCore(item, container);
        }
    }
}
