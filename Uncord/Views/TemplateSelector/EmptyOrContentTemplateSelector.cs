using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uncord.Views.TemplateSelector
{
    public class EmptyOrContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Content { get; set; }
        public DataTemplate Empty { get; set; }


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item != null)
            {
                return Content;
            }
            else
            {
                return Empty;
            }
        }
    }
}
