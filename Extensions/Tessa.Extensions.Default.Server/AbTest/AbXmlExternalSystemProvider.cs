#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.AbTest
{
    public static class AbXmlExternalSystemProvider
    {
        public static string GetXml(string? name, string? driver, List<object?>? files)
        {
            var filesElements = new List<XElement>();
            if (files is not null)
            {
                foreach (var file in files)
                {
                    if (file is Dictionary<string, object?> fileDict)
                    {
                        var filesElement = new XElement(
                            "file",
                            new XElement("name", fileDict["name"]),
                            new XElement("content", SanitizeXmlString(fileDict["content"]?.ToString() ?? string.Empty)));
                        filesElements.Add(filesElement);
                    }
                }
            }

            var element = new XElement(
                "doc",
                new XElement("name", name),
                new XElement("driver", driver),
                new XElement("files", filesElements),
                new XElement("time", DateTime.Now.ToString("T", CultureInfo.InvariantCulture)));

            return element.ToString();
        }

        private static string SanitizeXmlString(string str)
        {
            var sanitizedString = StringBuilderHelper.Acquire(str.Length);
            foreach (var ch in str)
            {
                if (XmlConvert.IsXmlChar(ch))
                {
                    sanitizedString.Append(ch);
                }
            }

            return new XText(sanitizedString.ToStringAndRelease()).ToString();
        }
    }
}
