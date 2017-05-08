using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uncord.Views.Converter
{
    public class ParcentageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                var v = (int)((double)value * 100);

                return v + "%";
            }
            else if (value is float)
            {
                var v = (int)((float)value * 100);

                return v + "%";
            }
            else
            {
                throw new NotSupportedException(value?.ToString() ?? "?");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
