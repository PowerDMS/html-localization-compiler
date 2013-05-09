// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationCompiler.cs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Localization Compiler
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids.Localization.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.XPath;

    public class LocalizationCompiler
    {
        private readonly XmlDocument _Xliff;

        private readonly XmlNamespaceManager _XliffNs;

        public LocalizationCompiler(FileInfo xliff, DirectoryInfo directory)
        {
            _Xliff = new XmlDocument();
            _Xliff.Load(xliff.FullName);

            _XliffNs = new XmlNamespaceManager(_Xliff.NameTable);
            _XliffNs.AddNamespace("x", "urn:oasis:names:tc:xliff:document:1.2");
        }

        [Pure]
        public IEnumerable<MatchedNode> ExtractSources()
        {
            var blah = _Xliff.SelectNodes("/x:xliff/x:file/x:body/x:trans-unit", _XliffNs);

            if (blah != null)
            {
                return blah.Cast<XmlNode>().Select(this.ConvertNodeToAngular);
            }

            return Enumerable.Empty<MatchedNode>();
        }

        [Pure]
        public String FormatForAngular(String input)
        {
            return "{{" + input + "}}";
        }

        [Pure]
        public MatchedNode ConvertNodeToAngular(XmlNode node)
        {
            var sourceNode = node.SelectSingleNode("x:source", _XliffNs);
            var targetNode = node.SelectSingleNode("x:target", _XliffNs);
            var innerSourceXml = sourceNode.InnerXml;
            var innerTargetXml = targetNode.InnerXml;

            var selectChildNodes = sourceNode.SelectNodes("x:x", _XliffNs);
            var children = selectChildNodes == null ? Enumerable.Empty<XmlNode>() : selectChildNodes.Cast<XmlNode>();

            var xmlAndAngularParameters = children.Select(n => new Tuple<String, String>(n.OuterXml, FormatForAngular(n.Attributes["id"].Value)));

            innerSourceXml = xmlAndAngularParameters.Aggregate(innerSourceXml, (acc, parameter) => acc.Replace(parameter.Item1, parameter.Item2));
            innerTargetXml = xmlAndAngularParameters.Aggregate(innerTargetXml, (acc, parameter) => acc.Replace(parameter.Item1, parameter.Item2));

            return new MatchedNode(innerSourceXml, innerTargetXml);
        }
    }
}
