// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HtmlCompiler.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Compiles localized versions of html files using xliff
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids.Generators

open Ids.Localization.Parsers.LocalizationTag

type LocalizationMatch = {
    source : LocalizationSourcePart seq;
    sourceText : string;
    target : string;
    attributeTarget : string;
    translatorNote : string;
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

    let xmlReplacer = new Regex(@"<x id=[""'](.*?)[""'].*?/>", RegexOptions.IgnoreCase);
    let xmlReplacerLastTag = new Regex(@"<x id=[""'](.*?)[""'].*?/>$", RegexOptions.IgnoreCase);
    let replacer (m : Match) = String.Format("{{{{{0}}}}}", m.Groups.Item(1))
    let attributeReplacer (m : Match) = String.Format("' + {0} + '", m.Groups.Item(1))
    let attributeReplacerLastTag (m : Match) = String.Format("' + {0}", m.Groups.Item(1))

    let sha256Hasher = System.Security.Cryptography.SHA256.Create()

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
                              let sourceNode = x.SelectNodes("x:source", manager).Item(0)
                              { source = sourceNode.ChildNodes
                                                |> trim sourceNode
                                                |> Seq.cast<XmlNode>
                                                |> Seq.map (fun x -> match x with
                                                            | x when x.NodeType = XmlNodeType.Text -> Text (FParsec.Text.NormalizeNewlines x.Value)
                                                            | x when x.NodeType = XmlNodeType.Whitespace -> Text (FParsec.Text.NormalizeNewlines x.Value)
                                                            | _ -> Variable (FParsec.Text.NormalizeNewlines (x.Attributes.GetNamedItem("id").Value)));
                                     sourceText = x.SelectNodes("x:source", manager).Item(0).InnerXml;
                                     target = x.SelectNodes("x:target", manager).Item(0).InnerXml;
                                     attributeTarget = x.SelectNodes("x:target", manager).Item(0).InnerXml;
                                     translatorNote = x.SelectNodes("x:note", manager).Item(0).InnerXml })
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
            | Variable s -> s

    let private getTranslation (matches : LocalizationMatch seq) (tag : LocalizationTag) : string = 
        let m = (matches |> Seq.find (fun x -> (tag.source |> Seq.map (fun y -> y.GetHashCode()) |> Seq.reduce (+)) = (x.source |> Seq.map (fun y -> y.GetHashCode()) |> Seq.reduce (+))))
        if tag.isInAttribute then m.attributeTarget else m.target

    let generateNewHtmlFile contents (matches : LocalizationMatch seq) : string =
        match run LocalizationTagParser.getChunks (Text.NormalizeNewlines contents) with
            | Success (x, _, endPosition) -> 
                let rest = Text.NormalizeNewlines(contents).Substring(int32(endPosition.Index))
                match x with
                    | [] -> rest
                    | x -> (x |> Seq.map (fun (a, b) -> a + (getTranslation matches b)) |> Seq.reduce (+)) + rest
            | Failure (reasons, state, _) -> reasons
