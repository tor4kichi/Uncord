using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uncord.Views.TemplateSelector
{
    public class GeneralTemplateSelector : DataTemplateSelector
    {
        public List<ValueAndTemplate> Items { get; } = new List<ValueAndTemplate>();
        public DataTemplate Empty { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var itemString = item?.ToString();
            foreach (var i in Items)
            {
                var vt = (i as ValueAndTemplate);
                if (vt.Value.Equals(itemString))
                {
                    return vt.Template;
                }
            }

            return Empty ?? base.SelectTemplateCore(item, container);
        }
    }

    [ContentProperty(Name = "Template")]
    public class ValueAndTemplate : DependencyObject
    {
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(ValueAndTemplate),
                new PropertyMetadata(null));



        public DataTemplate Template
        {
            get { return (DataTemplate)GetValue(TemplateProperty); }
            set { SetValue(TemplateProperty, value); }
        }

        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.Register(
                nameof(Template),
                typeof(DataTemplate),
                typeof(ValueAndTemplate),
                new PropertyMetadata(null));
    }
}
