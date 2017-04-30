using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncord.ViewModels
{
    public class NavigationPayloadBase
    {
        public string Serialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static T Deserialize<T>(string text)
            where T : NavigationPayloadBase
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
        }
    }
}
