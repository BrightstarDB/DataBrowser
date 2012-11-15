using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBrowser.Model;

namespace DataBrowser.Providers
{
    public interface IProvider
    {
        Task<List<ResourceType>> GetResourceTypes();
        Task<List<Resource>> GetResources(ResourceType type);
        Task<List<Resource>> GetResources(Uri collectionUrl, ResourceType type);
        Task<List<Property>> GetResourceProperties(Resource resource);
    }
}
