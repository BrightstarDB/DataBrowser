using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DataBrowser
{
    public static class XDocumentSparqlExtensions
    {
        private static readonly XNamespace SparqlResultsNamespace = "http://www.w3.org/2005/sparql-results#";

        ///<summary>
        /// Returns the collection of elements that represent a single result row.
        ///</summary>
        ///<param name="document">The XDocument containing the result data</param>
        ///<returns>An enumeration of elements.</returns>
        public static IEnumerable<XElement> SparqlResultRows(this XDocument document)
        {
            return document.Descendants(SparqlResultsNamespace + "result");
        }

        /// <summary>
        /// Returns the boolean value encoded in the SPARQL results for an ASK query
        /// </summary>
        /// <param name="document">The XDocument containing the result data</param>
        /// <returns>Returns true if the result document contains a &lt;boolean&gt; element with a value of "true", false otherwise</returns>
        public static bool SparqlBooleanResult(this XDocument document)
        {
            if (document.Root == null) return false;
            var boolEl = document.Root.Element(SparqlResultsNamespace + "boolean");
            return (boolEl != null && boolEl.Value.Trim().ToLowerInvariant().Equals("true"));
        }

        /// <summary>
        /// Returns the value of the matching Sparql result column.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column value to return</param>
        /// <returns>The value of the column or null if no column exists.</returns>
        public static object GetColumnValue(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            return binding == null ? null : GetTypedValue(binding);
        }

        /// <summary>
        /// Returns the string value of a SPARQL result column
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="columnIndex">The zero-based index of the column value to return</param>
        /// <returns>The value of the column or null if no column exists</returns>
        public static object GetColumnValue(this XElement row, int columnIndex)
        {
            var binding = row.Elements(SparqlResultsNamespace + "binding").ElementAtOrDefault(columnIndex);
            return binding == null ? null : GetTypedValue(binding);
        }

        /// <summary>
        /// Returns the variable column names in the Sparql result.
        /// </summary>
        /// <param name="doc">The XDocument containing the result data.</param>
        /// <returns>An IEnumerable string of column variable names.</returns>
        public static IEnumerable<string> GetVariableNames(this XDocument doc)
        {
            if (doc.Root != null)
            {
                var head = doc.Root.Element(SparqlResultsNamespace + "head");
                if (head != null)
                {
                    return
                        head.Elements(SparqlResultsNamespace + "variable").Where(e => (e.Attribute("name") != null)).Select(
                            e => e.Attribute("name").Value);
                }
            }
            return new string[0];
        }

        private static object GetTypedValue(XElement binding)
        {
            if (binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault() != null)
            {
                var datatype = GetBindingDataType(binding) ?? Constants.DefaultDatatypeUri;
                object parsedValue;
                if (RdfDatatypes.TryParseLiteralString(binding.Value, datatype, out parsedValue)) return parsedValue;
                return binding.Value;
            }
            return new Uri(binding.Value);
        }

        /// <summary>
        /// Test if the specified Sparql result column is a literal.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column to check if its a literal</param>
        /// <returns>True if the column is a literal. False if the column doesn't exist.</returns>
        public static bool IsLiteral(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();

            if (binding == null) return false;
            return binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault() != null;
        }


        /// <summary>
        /// Gets the datatype for the specified Sparql result column.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column to get the data type from</param>
        /// <returns>The literal datatype. Null if the column isn't present or the column is a URI</returns>
        public static string GetLiteralDatatype(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            return binding == null ? null : GetBindingDataType(binding);
        }

        private static string GetBindingDataType(XElement binding)
        {
            var literal = binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault();
            if (literal == null) return null;

            var attribute = literal.Attribute("datatype");
            if (attribute == null) return null;

            return attribute.Value;
        }

        /// <summary>
        /// Gets the language code for the specified literal column
        /// </summary>
        /// <param name="row">The XElement that represents the sparql resul row</param>
        /// <param name="name">The name of the sparql result parameter</param>
        /// <returns>Language code of null if the named column doesnt exist, or there is no langusge code attribute</returns>
        public static string GetLiteralLanguageCode(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            if (binding == null) return null;

            var literal = binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault();
            if (literal == null) return null;

            var attribute = literal.Attribute("lang");
            if (attribute == null) return null;

            return attribute.Value;
        }
    }

    public static class Constants
    {
        /// <summary>
        /// A constant that defines the URI assigned to the default graph in a BrightstarDB store
        /// </summary>
        public const string DefaultGraphUri = "http://www.brightstardb.com/.well-known/model/defaultgraph";

        /// <summary>
        /// A constant that defines the prefix part of auto-generated URIs created by BrightstarDB
        /// </summary>
        public const string GeneratedUriPrefix = "http://www.brightstardb.com/.well-known/genid/";

        /// <summary>
        /// A constant that defines a wildcard resource match in a delete pattern
        /// </summary>
        public const string WildcardUri = "http://www.brightstardb.com/.well-known/model/wildcard";

        /// <summary>
        /// A constant that defines the default datatype assigned to literal values
        /// </summary>
        public const string DefaultDatatypeUri = RdfDatatypes.PlainLiteral;

        /// <summary>
        /// Predicate URI used in optimistic locking
        /// </summary>
        public const string VersionPredicateUri = "http://www.brightstardb.com/.well-known/model/version";

        /// <summary>
        /// A regular expression to use for validating store names
        /// </summary>
        public const string StoreNameRegex = @"^[a-zA-Z0-9-_\.\+,\(\)]{1,1024}$";

    }

    public static class RdfDatatypes
    {
        /// <summary>
        /// The XML namespace for W3C XML Schema
        /// </summary>
        public const string XsdNamespace = "http://www.w3.org/2001/XMLSchema#";

        /// <summary>
        /// The XML namespace for RDFS
        /// </summary>
        public const string RdfSchemaNamespace = "http://www.w3.org/2000/01/rdf-schema#";

        /// <summary>
        /// The XML namespace for NetworkedPlanet-defined datatypes
        /// </summary>
        public const string NetworkedPlanetXsdNamespace = "http://www.networkedplanet.com/namespaces/datatypes#";

        /// <summary>
        /// The datatype URI for an RDF plain literal
        /// </summary>
        public const string PlainLiteral = RdfSchemaNamespace + "PlainLiteral";

        /// <summary>
        /// The datatype URI for an RDF xml literal
        /// </summary>
        public const string XmlLiteral = RdfSchemaNamespace + "XMLLiteral";

        /// <summary>
        /// The XML Schema string datatype URI
        /// </summary>
        public const string String = XsdNamespace + "string";

        /// <summary>
        /// The XML Schema bool datatype URI
        /// </summary>
        public const string Boolean = XsdNamespace + "boolean";

        /// <summary>
        /// The XML Schema dateTime datatype URI
        /// </summary>
        public const string DateTime = XsdNamespace + "dateTime";

        /// <summary>
        /// The XML Schema date datatype URI
        /// </summary>
        public const string Date = XsdNamespace + "date";

        /// <summary>
        /// The XML Schema double datatype URI
        /// </summary>
        public const string Double = XsdNamespace + "double";

        /// <summary>
        /// The XML Schema integer datatype URI
        /// </summary>
        public const string Integer = XsdNamespace + "integer";

        /// <summary>
        /// The XML Schema float datatype URI
        /// </summary>
        public const string Float = XsdNamespace + "float";

        /// <summary>
        /// The XML Schema long datatype URI
        /// </summary>
        public const string Long = XsdNamespace + "long";

        /// <summary>
        /// The XML Schema byte datatype URI
        /// </summary>
        public const string Byte = XsdNamespace + "byte";

        /// <summary>
        /// The XML Schema decimal datatype URI
        /// </summary>
        public const string Decimal = XsdNamespace + "decimal";

        /// <summary>
        /// The XML Schema short datatype URI
        /// </summary>
        public const string Short = XsdNamespace + "short";

        /// <summary>
        /// The XML Schema unsigned long datatype URI
        /// </summary>
        public const string UnsignedLong = XsdNamespace + "unsignedLong";
        /// <summary>
        /// The XML Schema unsigned integer datatype URI
        /// </summary>
        public const string UnsignedInteger = XsdNamespace + "unsignedInt";
        /// <summary>
        /// The XML Schema unsigned short integer datatype URI
        /// </summary>
        public const string UnsignedShort = XsdNamespace + "unsignedShort";
        /// <summary>
        /// The XML Schema unsigned byte datatype URI
        /// </summary>
        public const string UnsignedByte = XsdNamespace + "unsignedByte";

        /// <summary>
        /// The XML Schema base-64 binary datatype URI
        /// </summary>
        public const string Base64Binary = XsdNamespace + "base64Binary";

        /// <summary>
        /// The NP XML Schema char datatype URI
        /// </summary>
        public const string Char = NetworkedPlanetXsdNamespace + "char";

        #region Rdf Datatype Definitions
        private static readonly RdfDatatype RdfString = new RdfDatatype(String, (o => o as string), (s => s));
        private static readonly RdfDatatype RdfBoolean = new RdfDatatype(Boolean, (o => ((bool)o) ? "true" : "false"), (s) => Convert.ToBoolean(s));
        private static readonly RdfDatatype RdfDateTime = new RdfDatatype(DateTime, (o => ((DateTime)o).ToString("O")), s => Convert.ToDateTime(s));
        //private static readonly RdfDatatype RdfDate = new RdfDatatype(Date, (o => ((DateTime)o).ToShortDateString()), s => Convert.ToDateTime(s));
        private static readonly RdfDatatype RdfDouble = new RdfDatatype(Double, (o => ((double)o).ToString()), s => Convert.ToDouble(s));
        private static readonly RdfDatatype RdfInteger = new RdfDatatype(Integer, (o => ((int)o).ToString()), s => Convert.ToInt32(s));
        private static readonly RdfDatatype RdfFloat = new RdfDatatype(Float, (o => ((float)o).ToString()), s => Convert.ToSingle(s));
        private static readonly RdfDatatype RdfLong = new RdfDatatype(Long, (o => ((long)o).ToString()), s => Convert.ToInt64(s));
        private static readonly RdfDatatype RdfByte = new RdfDatatype(Byte, (o => ((sbyte)o).ToString()), s => Convert.ToSByte(s));
        private static readonly RdfDatatype RdfDecimal = new RdfDatatype(Decimal, (o => ((decimal)o).ToString()), s => Convert.ToDecimal(s));
        private static readonly RdfDatatype RdfShort = new RdfDatatype(Short, (o => ((short)o).ToString()), s => Convert.ToInt16(s));
        private static readonly RdfDatatype RdfUnsignedLong = new RdfDatatype(UnsignedLong, (o => ((ulong)o).ToString()), s => Convert.ToUInt64(s));
        private static readonly RdfDatatype RdfUnsignedInt = new RdfDatatype(UnsignedInteger, (o => ((uint)o).ToString()), s => Convert.ToUInt32(s));
        private static readonly RdfDatatype RdfUnsignedShort = new RdfDatatype(UnsignedShort, (o => ((ushort)o).ToString()), s => Convert.ToUInt16(s));
        private static readonly RdfDatatype RdfUnsignedByte = new RdfDatatype(UnsignedByte, (o => ((byte)o).ToString()), s => Convert.ToByte(s));
        //private static readonly RdfDatatype RdfChar = new RdfDatatype(Char, o => ((char)o).ToString(), s => s.FirstOrDefault());
        private static readonly RdfDatatype RdfByteArray = new RdfDatatype(Base64Binary, o => Convert.ToBase64String((byte[])o), s => Convert.FromBase64String(s));

        private static readonly Dictionary<Type, RdfDatatype> SystemTypeToRdfType =
            new Dictionary<Type, RdfDatatype>
                {
                    {typeof (String), RdfString},
                    {typeof (Boolean), RdfBoolean},
                    {typeof (DateTime), RdfDateTime},
                    {typeof (double), RdfDouble},
                    {typeof (Int32), RdfInteger},
                    {typeof (float), RdfFloat},
                    {typeof (long), RdfLong},
                    {typeof (sbyte), RdfByte},
                    {typeof (decimal), RdfDecimal},
                    {typeof (short), RdfShort},
                    {typeof (ulong), RdfUnsignedLong},
                    {typeof (uint), RdfUnsignedInt},
                    {typeof (ushort), RdfUnsignedShort},
                    {typeof (byte), RdfUnsignedByte},
                    //{typeof (char), RdfChar},
                    {typeof (byte[]), RdfByteArray},
                };

        private static readonly Dictionary<string, RdfDatatype> DatatypeUriToRdfType =
            new Dictionary<string, RdfDatatype>
                {
                    {String, RdfString},
                    {Boolean, RdfBoolean},
                    {DateTime, RdfDateTime},
                    //{Date, RdfDate},
                    {Double, RdfDouble},
                    {Integer, RdfInteger},
                    {Float, RdfFloat},
                    {Long, RdfLong},
                    {Byte, RdfByte},
                    {Decimal, RdfDecimal},
                    {Short, RdfShort},
                    {UnsignedLong, RdfUnsignedLong},
                    {UnsignedInteger, RdfUnsignedInt},
                    {UnsignedShort, RdfUnsignedShort},
                    {UnsignedByte, RdfUnsignedByte},
                    //{Char, RdfChar},
                    {Base64Binary, RdfByteArray}
                };

        #endregion
        /// <summary>
        /// Returns the datatype URI to use for items of the specified type
        /// </summary>
        /// <param name="systemType">The .NET type to map to an RDF datatype</param>
        /// <returns>The RDF datatype URI for <paramref name="systemType"/></returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="systemType"/> does not reference a type that can be mapped to an RDF datatype by this class</exception>
        public static string GetRdfDatatype(Type systemType)
        {
            if (systemType == null) return PlainLiteral;
            RdfDatatype ret;
            if (SystemTypeToRdfType.TryGetValue(systemType, out ret)) return ret.DatatypeUri;
            throw new ArgumentException(String.Format("The type {0} is not mapped to any known RDF datatype", systemType.FullName), "systemType");
        }

        ///<summary>
        /// Attempts to convert the specified value to an RDF literal string
        ///</summary>
        ///<param name="value">The value to be converted to a literal</param>
        ///<returns>The converted literal string</returns>
        ///<exception cref="ArgumentException">Thrown if <paramref name="value"/> is not of a type that can be mapped to an RDF datatype by this class</exception>
        public static string GetLiteralString(object value)
        {
            if (value == null) return String.Empty;
            RdfDatatype ret;
            if (SystemTypeToRdfType.TryGetValue(value.GetType(), out ret)) return ret.FormatLiteral(value);
            throw new ArgumentException(String.Format("The type {0} is not mapped to any known RDF datatype", value.GetType().FullName), "value");
        }

        /// <summary>
        /// Attempts to parse a typed value from a literal string 
        /// </summary>
        /// <param name="literalString">The literal string to be parsed</param>
        /// <param name="literalDatatype">The declared datatype for the literal</param>
        /// <returns>The parsed object</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="literalDatatype"/> is not an RDF datatype known to this class</exception>
        public static object ParseLiteralString(string literalString, string literalDatatype)
        {
            if (literalDatatype.Equals(PlainLiteral) || literalDatatype.Equals(String)) return literalString;
            RdfDatatype datatype;
            if (DatatypeUriToRdfType.TryGetValue(literalDatatype, out datatype))
            {
                return datatype.ParseLiteral(literalString);
            }
            throw new ArgumentException(String.Format("The datatype URI {0} is not mapped to any known RDF datatype", literalDatatype), "literalDatatype");
        }

        /// <summary>
        /// Performs a safe parse on the specified literal string
        /// </summary>
        /// <param name="literalString">The literal string to be parsed</param>
        /// <param name="literalDatatype">The datatype to parse the literal as</param>
        /// <param name="parsedValue">Receives the parsed literal value, or <paramref name="literalString"/> if the datatype is not supported, or if the datatype conversion failed</param>
        /// <returns>True if the literal was converted to the target datatype successfully, false otherwise.</returns>
        public static bool TryParseLiteralString(string literalString, string literalDatatype, out object parsedValue)
        {
            try
            {
                parsedValue = ParseLiteralString(literalString, literalDatatype);
                return true;
            }
            catch (FormatException)
            {
                parsedValue = literalString;
                return false;
            }
            catch (ArgumentException)
            {
                parsedValue = literalString;
                return false;
            }
        }


        class RdfDatatype
        {
            public string DatatypeUri { get; private set; }
            public Func<object, string> FormatLiteral { get; private set; }
            public Func<string, object> ParseLiteral { get; private set; }

            public RdfDatatype(string datatypeUri, Func<object, string> formatter, Func<string, object> parser)
            {
                DatatypeUri = datatypeUri;
                FormatLiteral = formatter;
                ParseLiteral = parser;
            }
        }
    }


}
