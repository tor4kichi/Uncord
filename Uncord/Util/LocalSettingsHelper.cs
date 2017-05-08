using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uncord.Util
{
    public static class LocalSettingsHelper
    {
        public static T GetValue<T>(string containerName, string key, T defaultValue = default(T))
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                try
                {
                    return (T)ApplicationData.Current.LocalSettings.Values[key];
                }
                catch (InvalidCastException) { }
            }
            

            return defaultValue;
        }

        public static bool TryGetValue<T>(string containerName, string key, out T outValue)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                try
                {
                    outValue = (T)ApplicationData.Current.LocalSettings.Values[key];
                    return true;
                }
                catch (InvalidCastException) { }
            }

            outValue = default(T);
            return false;
        }

        public static void SetValue<T>(string containerName, string key, T value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;

            ApplicationData.Current.SignalDataChanged();
        }
    }
}
