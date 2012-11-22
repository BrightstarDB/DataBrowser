using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DataBrowser.Providers;

namespace DataBrowser.Model
{
    public class Resource
    {
        public Uri Identity { get; set; }
        public ResourceType Type { get; set; }
        public List<Property> Properties { get; set; }
        public String Title { get; set; }
        public IProvider DataProvider { get; private set; }
        public Uri Image { get; set; }

        public Resource(IProvider provider, ResourceType type)
        {
            DataProvider = provider;
            Type = type;
            // Image = new Uri("http://www.brightstardb.com/images/logo.png";
        }
            
    }
}
