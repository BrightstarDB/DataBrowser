using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using DataBrowser.Providers;

namespace DataBrowser.Model
{
    public class ResourceType : Resource
    {
        public string InstanceImage { get; set; }
        public ResourceType(IProvider provider) : base(provider, null){} 
    }
}
