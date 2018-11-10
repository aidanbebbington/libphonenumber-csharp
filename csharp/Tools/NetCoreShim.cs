
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Tools {
    internal static class NetCoreShim {
        internal static IEnumerable<XElement> GetElementsByTagName(this XDocument document, string tagName)
            => document.Descendants().Where(d => d.Name.LocalName == tagName);

        internal static IEnumerable<XElement> GetElementsByTagName(this XElement document, string tagName)
            => document.Descendants().Where(d => d.Name.LocalName == tagName);

        internal static bool HasAttribute(this XElement element, string attribute)
            => element.Attribute(attribute) != null;

        internal static string GetAttribute(this XElement element, string attribute) 
            => element.Attribute(attribute)?.Value ?? string.Empty;

        internal static List<TOutput> ConvertAll<TOutput>(this List<char> list, Func<char, TOutput> converter)
            => list.Select(converter).ToList();
    }
}