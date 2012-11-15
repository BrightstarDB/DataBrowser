using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.AtomPub;
using System.Xml.Linq;
using System.Net;
using System.IO;
using DataBrowser.Model;
using Windows.Web.Syndication;
using Windows.Web.AtomPub;

namespace DataBrowser.Providers
{
    public class ODataProvider : IProvider
    {
        private readonly string _endpoint;

        public ODataProvider(string endpoint)
        {
            _endpoint = endpoint;
        }

        private async Task<XDocument> DoGetAsync(string query)
        {
            var request = WebRequest.CreateHttp(query);
            using (var response = await request.GetResponseAsync())
            {
                var doc = XDocument.Load(new StreamReader(response.GetResponseStream()));
                return doc;
            }
        }

        public async Task<List<ResourceType>> GetResourceTypes()
        {
            try
            {
                // read service document
                var client = new AtomPubClient();
                var serviceDoc = await client.RetrieveServiceDocumentAsync(new Uri(_endpoint));

                // get workspace 0
                var workspace = serviceDoc.Workspaces[0];

                // get collections list
                var resourceTypes = new List<ResourceType>();
                foreach (var collection in workspace.Collections)
                {
                    var rt = new ResourceType(this);
                    rt.Identity = collection.Uri;
                    rt.Title = collection.Title.Text;
                    resourceTypes.Add(rt);
                }

                return resourceTypes;
            } catch(Exception ex)
            {
                return new List<ResourceType>();
            }
        }

        public async Task<List<Resource>> GetResources(ResourceType type)
        {
            var entries = DataCache.Instance.Lookup<SyndicationFeed>(type.Identity.AbsoluteUri);
            if (entries == null)
            {
                AtomPubClient client = new AtomPubClient();
                entries = await client.RetrieveFeedAsync(type.Identity);
                DataCache.Instance.Cache(type.Identity.AbsoluteUri, entries);
            }


            var resources = new List<Resource>();
            var resourceIndex = new Dictionary<string, Resource>();
            foreach (var entry in entries.Items)
            {
                var resource = new Resource(this, type);
                resource.Identity = new Uri(entry.Id);
                resource.Title = entry.Title.Text;
                resources.Add(resource);
            }

            // check for a link to more entries            
            var nextLink = entries.Links.FirstOrDefault(l => l.Relationship.Equals("next"));
            if (nextLink != null)
            {
                var nextResourceType = new ResourceType(this);
                nextResourceType.Identity = new Uri("http://www.brightstardb.com/databrowser/core/next");

                var nextResource = new Resource(this, nextResourceType);
                nextResource.Identity = nextLink.Uri;
                nextResource.Title = "More...";

                resources.Add(nextResource);
            }

            return resources;
        }


        public async Task<List<Resource>> GetResources(Uri collectionUri, ResourceType type)
        {
            var entries = DataCache.Instance.Lookup<SyndicationFeed>(collectionUri.AbsoluteUri);
            if (entries == null)
            {
                AtomPubClient client = new AtomPubClient();
                entries = await client.RetrieveFeedAsync(collectionUri);
                DataCache.Instance.Cache(collectionUri.AbsoluteUri, entries);                    
            }

            var resources = new List<Resource>();
            var resourceIndex = new Dictionary<string, Resource>();
            foreach (var entry in entries.Items)
            {
                var resource = new Resource(this, type);
                resource.Identity = new Uri(entry.Id);
                resource.Title = entry.Title.Text;
                resources.Add(resource);
            }

            // check for a link to more entries            
            var nextLink = entries.Links.FirstOrDefault(l => l.Relationship.Equals("next"));
            if (nextLink != null)
            {
                var nextResourceType = new ResourceType(this);
                nextResourceType.Identity = new Uri("http://www.brightstardb.com/databrowser/core/next");

                var nextResource = new Resource(this, nextResourceType);
                nextResource.Identity = nextLink.Uri;
                nextResource.Title = "More...";

                resources.Add(nextResource);
            }

            return resources;
        }

        private readonly static XNamespace DataServicesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace MetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private const string XmlSchemaDataTypeNamespace = "http://www.w3.org/2001/XMLSchema#";
        private const string OdataRelationshipRelTypePrefix = "http://schemas.microsoft.com/ado/2007/08/dataservices/related/";

        public async Task<List<Property>> GetResourceProperties(Resource resource)
        {
            AtomPubClient client = new AtomPubClient();
            var res = DataCache.Instance.Lookup<SyndicationItem>(resource.Identity.AbsoluteUri);
            if (res == null)
            {
                res = await client.RetrieveResourceAsync(resource.Identity);
                DataCache.Instance.Cache(resource.Identity.AbsoluteUri, res);
            }
         
            // process simple properties
            var properties = new List<Property>();

            var propertyElements = res.ElementExtensions;
            foreach (var item in propertyElements)
            {
                if (item.NodeName.ToLower().Equals("properties"))
                {
                    foreach (var prop in item.ElementExtensions)
                    {
                        if (string.IsNullOrEmpty(prop.NodeValue)) continue; 
                        var property = new Property(this, null);
                        property.PropertyName = prop.NodeName;
                        property.PropertyValue = prop.NodeValue;
                        property.IsLiteral = true;
                        properties.Add(property);
                    }
                }
            }
            
            // process link properties
            var links = res.Links.Where(l => l.Relationship.StartsWith(OdataRelationshipRelTypePrefix));
            foreach (var syndicationLink in links)
            {
                // property name
                var propertyName = syndicationLink.Relationship.Substring(OdataRelationshipRelTypePrefix.Length);

                // need to check if we need to load an entry or a feed
                if (syndicationLink.MediaType.ToLower().Contains("type=entry"))
                {
                    try
                    {
                        AtomPubClient relatedResourceClient = new AtomPubClient();
                        var relatedResource = await client.RetrieveResourceAsync(syndicationLink.Uri);
                        var property = new Property(this, null);
                        property.PropertyName = propertyName;
                        property.PropertyValue = syndicationLink.Uri.ToString();
                        property.PropertyValueName = relatedResource.Title.Text;
                        properties.Add(property);
                    }
                    catch (Exception)
                    {
                        // sometimes we get bad data from odata services.
                    }
                }
                else
                {
                    try
                    {
                        AtomPubClient relatedResourceClient = new AtomPubClient();
                        // always ask for four and show three, if we get four then we can have a show all property.
                        var entries = await client.RetrieveFeedAsync(new Uri(syndicationLink.Uri.ToString() + "?$top=4")); 

                        foreach (var entry in entries.Items.Take(3))
                        {
                            var property = new Property(this, null);
                            property.PropertyName = propertyName;
                            property.PropertyValue = entry.EditUri.AbsoluteUri;
                            property.PropertyValueName = entry.Title.Text;
                            properties.Add(property);
                        }

                        if (entries.Items.Count > 3)
                        {
                            var property = new Property(this, null);
                            property.PropertyName = "";
                            property.IsCollectionProperty = true;
                            property.PropertyValue = syndicationLink.Uri.ToString();
                            property.PropertyValueName = "See all related " + propertyName;
                            properties.Add(property);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return properties;
        }
    }
}
