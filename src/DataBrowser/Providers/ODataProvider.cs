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
        /// <summary>
        /// The OData Endpoint
        /// </summary>
        private readonly string _endpoint;

        /// <summary>
        /// The URL of an optional OData annotations document
        /// </summary>
        private readonly string _annotationsUrl;

        /// <summary>
        /// A set of annotations to help with rendering
        /// </summary>
        private List<Annotation> _annotations;

        /// <summary>
        /// Indicates if the provider is initialised
        /// </summary>
        public bool IsIntialised { get; internal set; }

        /// <summary>
        /// Maps container collection names to types
        /// </summary>
        private Dictionary<string, string> _collectionToTypeMappings; 

        public ODataProvider(string endpoint, string annotationsUrl = null)
        {
            _endpoint = endpoint;
            _annotationsUrl = annotationsUrl;
            _annotations = new List<Annotation>();
            _collectionToTypeMappings = new Dictionary<string, string>();
            Initialise();
        }

        private async Task<bool> Initialise()
        {
            await ParseMetadata();
            await ParseAnnotations();
            IsIntialised = true;
            return true;
        }

        private async Task<bool> ParseMetadata()
        {
            var metadataDocument = await DoGetAsync(_endpoint + "/$metadata");
            if (metadataDocument != null)
            {
                // get entity sets
                foreach (var entitySetElement in metadataDocument.Descendants().Where(elem => elem.Name.LocalName.Equals("EntitySet")))
                {
                    var collectionName = entitySetElement.Attribute("Name").Value;
                    var typeName = entitySetElement.Attribute("EntityType").Value;
                    _collectionToTypeMappings.Add(collectionName, typeName.Substring(typeName.LastIndexOf('.') + 1));
                }
            }
            return true;
        }

        private async Task<bool> ParseAnnotations()
        {
            if (_annotationsUrl != null)
            {
                var annotationsDocument = await DoGetAsync(_annotationsUrl);
                if (annotationsDocument != null)
                {
                    foreach (var descendant in annotationsDocument.Descendants().Where(elem => elem.Name.LocalName.Equals("Annotations")))
                    {
                        var target = descendant.Attribute("Target").Value;
                        foreach (var valueAnnotation in descendant.Elements().Where(elem => elem.Name.LocalName.Equals("ValueAnnotation")))
                        {
                            var term = valueAnnotation.Attribute("Term").Value.Replace("DataBrowser.", "http://brightstardb.databrowser.annotations/");
                            if (valueAnnotation.Attribute("String")!=null)
                            {
                                var val = valueAnnotation.Attribute("String").Value;
                                var annotation = new Annotation(target, term, val);
                                _annotations.Add(annotation);
                            } else if (valueAnnotation.Attribute("Boolean") != null)
                            {
                                var val = valueAnnotation.Attribute("Boolean").Value;
                                var annotation = new Annotation(target, term, val);
                                _annotations.Add(annotation);                                
                            } else
                            {
                                continue;
                            }
                        }

                    }
                }
            }
            return true;
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

                    // check for type annotations
                    var collectionName = collection.NodeValue;
                    if (collectionName != null)
                    {
                        // get type name from collection name
                        string typeName;
                        if (!_collectionToTypeMappings.TryGetValue(collectionName, out typeName))
                        {
                            continue;
                        }

                        // get type annotations
                        var annotations = _annotations.Where(a => a.Target.Equals(typeName)).ToList();
                        if (annotations.Count > 0)
                        {
                            // see if we should hide the type
                            var hideAnnotation = annotations.FirstOrDefault(a => a.Property.Equals(Annotation.Hide));
                            if (hideAnnotation != null && hideAnnotation.Value.Equals("True"))
                            {
                                continue;                                
                            }

                            // try and get a display name
                            var displayNameAnnotation = annotations.FirstOrDefault(a => a.Property.Equals(Annotation.DisplayName));
                            if (displayNameAnnotation != null)
                            {
                                rt.Title = displayNameAnnotation.Value;
                            }
                            
                            // try and get an image
                            var imageAnnotation = annotations.FirstOrDefault(a => a.Property.Equals(Annotation.ThumbnailUrl));
                            if (imageAnnotation != null && Uri.IsWellFormedUriString(imageAnnotation.Value, UriKind.Absolute))
                            {
                                rt.Image = new Uri(imageAnnotation.Value);
                            }
                        }
                    }

                    resourceTypes.Add(rt);
                }

                return resourceTypes;
            } catch(Exception ex)
            {
                return new List<ResourceType>();
            }
        }

        private Uri GetImageUrl(SyndicationItem item)
        {
            // get category
            var category = item.Categories.FirstOrDefault();
            if (category != null)
            {
                var typeName = category.Term.Substring(category.Term.LastIndexOf('.') + 1);
                if (item.Content.Type.Equals("application/xml"))
                {
                    var properties = item.Content.Xml;
                    foreach (var p in properties.FirstChild.ChildNodes)
                    {
                        var annotation =
                            _annotations.FirstOrDefault(a => a.Target.Equals(typeName + "/" + p.LocalName) &&
                                                             a.Property.Equals(Annotation.IsThumnbnailUrl));

                        if (annotation != null) return new Uri(p.InnerText);
                    }
                } else
                {
                    
                }
            }
            return null;
        }

        public async Task<List<Resource>> GetResources(ResourceType type)
        {
            var entries = DataCache.Instance.Lookup<SyndicationFeed>(type.Identity.AbsoluteUri);
            if (entries == null)
            {
                var client = new AtomPubClient();
                entries = await client.RetrieveFeedAsync(type.Identity);
                DataCache.Instance.Cache(type.Identity.AbsoluteUri, entries);
            }

            var resources = new List<Resource>();
            foreach (var entry in entries.Items)
            {
                var resource = new Resource(this, type) {Identity = new Uri(entry.Id), Title = entry.Title.Text};
                resource.Image = GetImageUrl(entry);
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

            if (res.Content.Type.Equals("application/xml"))
            {
                var xml = res.Content.Xml;
                foreach (var cn in xml.FirstChild.ChildNodes)
                {
                    try
                    {
                        if (cn.FirstChild == null || cn.FirstChild.NodeValue == null) continue;
                        var val = cn.FirstChild.NodeValue.ToString();
                        if (string.IsNullOrEmpty(val)) continue;
                        var property = new Property(this, null);
                        property.PropertyName = cn.LocalName.ToString();
                        property.PropertyValue = val;
                        property.IsLiteral = true;
                        properties.Add(property);
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                    }
                }
            }
            else
            {
                var propertyElements = res.ElementExtensions;
                foreach (var item in propertyElements)
                {
                    if (item.NodeName.ToLower().Equals("properties"))
                    {
                        foreach (var prop in item.ElementExtensions)
                        {
                            if (string.IsNullOrEmpty(prop.NodeValue)) continue;
                            var property = new Property(this, null)
                                               {
                                                   PropertyName = prop.NodeName,
                                                   PropertyValue = prop.NodeValue,
                                                   IsLiteral = true
                                               };
                            properties.Add(property);
                        }
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
