using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uncord.Views.Converter
{
    public class EnumToLocalStringConverter : IValueConverter
    {
        public static string EnumStringIdSeparater = "_";


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is Enum)
                {
                    var enumType = value.GetType().Name;
                    var valueName = value.ToString();
                    var subCategoryString = parameter as string;
                    var textResourceId = subCategoryString != null
                        ? $"{enumType}{EnumStringIdSeparater}{valueName}{EnumStringIdSeparater}{subCategoryString}"
                        : $"{enumType}{EnumStringIdSeparater}{valueName}";

                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    var str = loader.GetString(textResourceId);

                    return str;
                }
            }
            catch { }

            return value.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
