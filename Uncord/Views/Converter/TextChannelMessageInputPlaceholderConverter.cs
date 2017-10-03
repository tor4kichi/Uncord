using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uncord.Views.Converter
{
    public class TextChannelMessageInputPlaceholderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var str = loader.GetString("SubmitToTextChannel");

                return str.Replace("{0}", value as string);
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
