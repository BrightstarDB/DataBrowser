using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.IO;
using DataBrowser.Model;

namespace DataBrowser.Providers
{
    public class SparqlEndpointProvider : IProvider
    {
        /// <summary>
        /// The SPARQL service endpoint
        /// </summary>
        private string _endpoint;

        public SparqlEndpointProvider(string endpoint)
        {
            _endpoint = endpoint;
        }

        private async Task<XDocument> DoGetAsync(string query)
        {
            var request = WebRequest.CreateHttp(_endpoint + "?query=" + query);
            using (var response = await request.GetResponseAsync())
            {
                var doc = XDocument.Load(new StreamReader(response.GetResponseStream()));
                return doc;
            }
        }

        private const string TypesQuery = "select distinct ?type ?typeName where { ?a a ?type . ?type <http://www.w3.org/2000/01/rdf-schema%23label> ?typeName }"; 
        public async Task<List<ResourceType>> GetResourceTypes()
        {
            var doc = await DoGetAsync(TypesQuery);
            var resourceTypes = new List<ResourceType>();
            foreach (var row in doc.SparqlResultRows())
            {
                var title = row.GetColumnValue("typeName");
                var type = row.GetColumnValue("type");
                resourceTypes.Add(new ResourceType(this) { Title = title.ToString(), Identity = new Uri(type.ToString()) });
            }
            return resourceTypes;
        }

        private const string GetInstancesQuery = "select ?identity ?name where {{ ?identity a <{0}> . ?identity <http://www.w3.org/2000/01/rdf-schema%23label> ?name }} ORDER BY ?name";
        public async Task<List<Resource>> GetResources(ResourceType type)
        {
            var instances = new List<Resource>();
            var query = string.Format(GetInstancesQuery, type.Identity.AbsoluteUri);
            var doc = await DoGetAsync(query);
            foreach (var row in doc.SparqlResultRows())
            {
                var title = row.GetColumnValue("name").ToString();
                var uri = row.GetColumnValue("identity");
                if (!string.IsNullOrEmpty(title))
                {
                    instances.Add(new Resource(this, type) { Title = title.ToString(), Identity = new Uri(uri.ToString()) });
                }
            }
            return instances;
        }

        public async Task<List<Resource>> GetResources(Uri query, ResourceType type)
        {
            var instances = new List<Resource>();            
            var doc = await DoGetAsync(query.AbsoluteUri);
            foreach (var row in doc.SparqlResultRows())
            {
                var title = row.GetColumnValue("name").ToString();
                var uri = row.GetColumnValue("identity");
                if (!string.IsNullOrEmpty(title))
                {
                    instances.Add(new Resource(this, type) { Title = title.ToString(), Identity = new Uri(uri.ToString()) });
                }
            }
            return instances;
        }

        private const string GetPropertiesQuery = "select ?prop ?value ?valueName where {{ <{0}> ?prop ?value . OPTIONAL {{ ?value <http://www.w3.org/2000/01/rdf-schema%23label> ?valueName }} }}";
        public async Task<List<Property>> GetResourceProperties(Resource resource)
        {
            var properties = new List<Property>();
            var query = string.Format(GetPropertiesQuery, resource.Identity.AbsoluteUri);

            var doc = await DoGetAsync(query);

            // this collection maps the type of properties that have more than three entries to
            // the collection query it uses.
            var collectionProperties = new Dictionary<string, string>();

            foreach (var row in doc.SparqlResultRows())
            {
                var type = row.GetColumnValue("prop").ToString();
                var value = row.GetColumnValue("value").ToString();
                var valueName = row.GetColumnValue("valueName") as string;
                var isLiteral = row.IsLiteral("value");
                if (!isLiteral && String.IsNullOrEmpty(valueName))
                {
                    valueName = Utils.GetDisplayNameFromUrl(value);
                    if (string.IsNullOrEmpty(valueName)) continue;
                }

                if (!string.IsNullOrEmpty(value) && !type.Equals("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"))
                {
                    properties.Add(new Property(this, null)
                    {
                        IsLiteral = isLiteral,
                        PropertyType = new ResourceType(this) { Identity = new Uri(type) },
                        PropertyName = Utils.GetDisplayNameFromUrl(type),
                        PropertyValue = value,
                        PropertyValueName = valueName
                    });
                }
            }
            return properties;
        }
    }
}
