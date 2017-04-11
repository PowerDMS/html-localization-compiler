// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HtmlCompiler.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Compiles localized versions of html files using xliff
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids

module Generators = 
    open Ids.Localization.Parsers.LocalizationTag

    open Ids.Localization.Parsers.XliffGenerator.XliffGenerator
        
    open System
    open System.Text.RegularExpressions

    let xmlReplacer = new Regex(@"<x id=[""'](.*?)[""'].*?/>", RegexOptions.IgnoreCase);
    let xmlReplacerLastTag = new Regex(@"<x id=[""'](.*?)[""'].*?/>$", RegexOptions.IgnoreCase);
    let replacer (m : Match) = String.Format("{{{{{0}}}}}", m.Groups.Item(1))
    let attributeReplacer (m : Match) = String.Format("' + {0} + '", m.Groups.Item(1))
    let attributeReplacerLastTag (m : Match) = String.Format("' + {0}", m.Groups.Item(1))

    type LocalizationMatch = {
        id : string;
        sourceText : string;
        target : string;
        attributeTarget : string;
    }

    module XmlToLocalizationMatch =
        open System
        open System.IO
        open System.Linq
        open System.Xml

        open System.Web;

        open System.Text.RegularExpressions

        let xliffDoc = new XmlDocument()
        xliffDoc.PreserveWhitespace <- true

        let manager = new XmlNamespaceManager(xliffDoc.NameTable)

        let generate (translatedXliff : string) = 
            manager.AddNamespace("x", "urn:oasis:names:tc:xliff:document:1.2")

            xliffDoc.Load(translatedXliff)

            let lang = xliffDoc.SelectNodes("/x:xliff/x:file", manager).Item(0).Attributes.GetNamedItem("target-language").Value

            let trim (sourceNode:XmlNode) (nodes:XmlNodeList) = 
                if nodes.Item(0) <> null && nodes.Item(0).NodeType = XmlNodeType.Whitespace then
                    sourceNode.RemoveChild(nodes.Item(0)) |> ignore

                if nodes.Item(nodes.Count-1) <> null && nodes.Item(nodes.Count-1).NodeType = XmlNodeType.Whitespace then
                    sourceNode.RemoveChild(nodes.Item(nodes.Count-1)) |> ignore

                sourceNode.ChildNodes

            let firstNonBlank = Seq.find (not << String.IsNullOrWhiteSpace)

            let nodes = 
                xliffDoc.SelectNodes("/x:xliff/x:file/x:body/x:trans-unit", manager)
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun x ->
                                  {
                                    id = x.Attributes.GetNamedItem("id").Value;
                                    sourceText = x.SelectNodes("x:source", manager).Item(0).InnerXml;
                                    target = x.SelectNodes("x:target", manager).Item(0).InnerXml;
                                    attributeTarget = x.SelectNodes("x:target", manager).Item(0).InnerXml;
                                  })
                    |> Seq.map (fun x -> { x with target = firstNonBlank [x.target; x.sourceText] |> HttpUtility.HtmlDecode;
                                                  attributeTarget = firstNonBlank [x.attributeTarget; x.sourceText] })
                    |> Seq.map (fun x ->
                        { x with target = xmlReplacerLastTag.Replace(xmlReplacer.Replace(x.target, replacer), replacer);
                                 attributeTarget = "\"'" + xmlReplacerLastTag.Replace(xmlReplacer.Replace(x.attributeTarget, attributeReplacer), attributeReplacerLastTag) + "'\"" })
            (lang, nodes)

    module HtmlCompiler =
        open FParsec
        open Ids.Localization.Parsers
        open Ids.Localization.Parsers.LocalizationTag

        let private deTag (t : LocalizationSourcePart) : string = 
            match t with
                | Text s -> s
                | Variable s -> "{{" + s + "}}"

        let private getTranslation (matches : LocalizationMatch seq) (tag : IdLocalizationTag) : string = 
            let optionalMatch = matches |> Seq.tryFind (fun x -> tag.id = x.id)
            match optionalMatch with
                | Some m -> if tag.isInAttribute then m.attributeTarget else m.target
                | None -> if not tag.isInAttribute
                            then String.Join("", tag.source |> Seq.map deTag)
                            else "\"'" + xmlReplacerLastTag.Replace(xmlReplacer.Replace(tag.source.ToString(), attributeReplacer), attributeReplacerLastTag) + "'\""

        let generateNewHtmlFile contents (matches : LocalizationMatch seq) : string =
            match run LocalizationTagParser.getChunks (Text.NormalizeNewlines contents) with
                | Success (x, _, endPosition) -> 
                    let rest = Text.NormalizeNewlines(contents).Substring(int32(endPosition.Index))
                    match x with
                        | [] -> rest
                        | x -> (x |> Seq.map(fun (a, b) -> (a, generateIdTag(b))) |> Seq.map (fun (a, b) -> a + (getTranslation matches b)) |> Seq.reduce (+)) + rest
                | Failure (reasons, state, _) -> reasons
