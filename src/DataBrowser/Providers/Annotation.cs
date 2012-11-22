using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBrowser.Providers
{
    public class Annotation
    {
        public const string Prefix = "http://brightstardb.databrowser.annotations/";
        public const string DisplayName = Prefix + "DisplayName";
        public const string ImageUrl = Prefix + "ImageUrl";
        public const string ThumbnailUrl = Prefix + "ThumbnailUrl";
        public const string Hide = Prefix + "Hide";
        public const string IsThumnbnailUrl = Prefix + "IsThumbnailUrl";
        public const string IsImageUrl = Prefix + "IsImageUrl";

        public string Target { get; internal set; }
        public string Property { get; internal set; }
        public string Value { get; internal set; } 

        public Annotation(string target, string property, string value)
        {
            Target = target;
            Property = property;
            Value = value;
        }
    }
}
