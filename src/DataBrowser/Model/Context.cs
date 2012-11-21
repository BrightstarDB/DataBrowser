using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using DataBrowser.Providers;

namespace DataBrowser.Model
{
    public class Context
    {
        private static List<Context> _contexts; 

        public static List<Context> Contexts
        {
            get
            {
                if (_contexts == null)
                {
                    _contexts = new List<Context>
                                    {
                                        new Context("Netflix", "Netflix OData endpoint",
                                                    new ODataProvider("http://odata.netflix.com/v2/Catalog/")),
                                        new Context("WebNodes", "WedNodes OData endpoint",
                                                    new ODataProvider("http://demo.webnodes.com/odata/")),
                                        new Context("Local B*", "Testing Sparql",
                                                    new SparqlEndpointProvider(
                                                        "http://localhost:8081/bs370/sparql"))
                                    };
                }

                //contextList.Add(new Context() { Title = "DBPedia", Endpoints = new List<string> { "http://localhost:8090/brightstar" }});
                //contextList.Add(new Context() { Title = "OData Samples", Endpoints = new List<string> { "http://localhost:8090/brightstar" }});
                //contextList.Add(new Context() { Title = "News Feeds", Endpoints = new List<string> { "http://localhost:8090/brightstar" } });
                //contextList.Add(new Context() { Title = "Twitter Feeds", Endpoints = new List<string> { "http://localhost:8090/brightstar" } });
                //contextList.Add(new Context() { Title = "Website and its RDFa", Endpoints = new List<string> { "http://localhost:8090/brightstar" } });
                //contextList.Add(new Context() { Title = "Search results", Endpoints = new List<string> { "http://localhost:8090/brightstar" } });
                return _contexts;
            }
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public IProvider DataProvider { get; private set; }

        public Context(String title, string description, IProvider provider)
        {
            Title = title;
            DataProvider = provider;
            Description = description;
        }

        //private const string TypesQuery = "select distinct ?type ?typeName where { ?a a ?type . ?type <http://www.w3.org/2000/01/rdf-schema%23label> ?typeName }"; 

        //public async Task<List<ResourceType>> GetTypesAsync()
        //{
        //    //var request = WebRequest.CreateHttp(Endpoints[0] + "?query=" + TypesQuery);
        //    //using (var response = await request.GetResponseAsync())
        //    //{
        //    //    var types = new List<ResourceType>();                
        //    //    types.Add(new ResourceType() { Title = "Recent" });
        //    //    var doc = XDocument.Load(new StreamReader(response.GetResponseStream()));
        //    //    foreach (var row in doc.SparqlResultRows())
        //    //    {
        //    //        var title = row.GetColumnValue("typeName");
        //    //        var type = row.GetColumnValue("type");
        //    //        types.Add(new ResourceType(){ Title =  title.ToString(), Identity = new Uri(type.ToString())});    
        //    //    }
        //    //    return types;
        //    //}
        //    var doc = await DoGetAsync(TypesQuery);
        //    var types = new List<ResourceType>();                
        //    foreach (var row in doc.SparqlResultRows())
        //    {
        //        var title = row.GetColumnValue("typeName");
        //        var type = row.GetColumnValue("type");
        //        types.Add(new ResourceType() { Context = this, Title = title.ToString(), Identity = new Uri(type.ToString()) });
        //    }
        //    return types;

        //}

        //public async Task<XDocument> DoGetAsync(string query)
        //{
        //    var request = WebRequest.CreateHttp(Endpoints[0] + "?query=" + query);
        //    using (var response = await request.GetResponseAsync())
        //    {
        //        var doc = XDocument.Load(new StreamReader(response.GetResponseStream()));
        //        return doc;
        //    }
        //}

    }
}
