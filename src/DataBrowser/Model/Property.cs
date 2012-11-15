using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBrowser.Providers;

namespace DataBrowser.Model
{
    public class Property
    {
        public ResourceType PropertyType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public bool IsLiteral { get; set; }
        public string PropertyValueName { get; set; }
        public IProvider DataProvider { get; private set; }
        public bool IsCollectionProperty { get; set; }

        public Property(IProvider provider, ResourceType type)
        {
            DataProvider = provider;
            PropertyType = type;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(PropertyValueName))
            {
                return PropertyName + " :  " + PropertyValue;
            }
            else
            {
                return PropertyName + " :  " + PropertyValueName;
            }

        }
    }
}
