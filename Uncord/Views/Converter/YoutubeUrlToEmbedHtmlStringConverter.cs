using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uncord.Views.Converter
{
    public class YoutubeUrlToEmbedHtmlStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                var url = value as string;
                var uri = new Uri(url);

                var decoder = new Windows.Foundation.WwwFormUrlDecoder(uri.Query);

                var videoId = decoder.GetFirstValueByName("v");

                if (string.IsNullOrWhiteSpace(videoId))
                {
                    videoId = uri.Segments.Last();
                }

                string html = @"<iframe width=""360"" height=""240"" src=""http://www.youtube.com/embed/" + videoId + @"?rel=0"" frameborder=""0"" allowfullscreen></iframe>";

                return html;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
