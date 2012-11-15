using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBrowser.Providers
{
    public class Utils
    {
        public static string GetDisplayNameFromUrl(string propUrl)
        {
            if (propUrl.LastIndexOf("#") >= 0)
            {
                return propUrl.Substring(propUrl.LastIndexOf("#") + 1);
            }
            else if (propUrl.LastIndexOf("/") >= 0)
            {
                return propUrl.Substring(propUrl.LastIndexOf("/") + 1);
            }
            return propUrl;
        }
    }
}
