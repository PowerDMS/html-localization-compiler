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

    type LocalizationMatch = {
        id : string;
        source : LocalizationSourcePart seq;
        target : LocalizationSourcePart seq;
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

        let private xmlToLM(node : XmlNode) =
            node.ChildNodes.Cast<XmlNode>()
            |> Seq.map(fun n -> match n.NodeType with
                                    | XmlNodeType.Element -> Variable (n.Attributes.GetNamedItem("id").Value)
                                    | _ -> Text n.Value)

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

            let nodes = 
                xliffDoc.SelectNodes("/x:xliff/x:file/x:body/x:trans-unit", manager)
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun x ->
                                  {
                                    id = x.Attributes.GetNamedItem("id").Value;
                                    source = xmlToLM(x.SelectNodes("x:source", manager).Item(0));
                                    target = xmlToLM(x.SelectNodes("x:target", manager).Item(0));
                                    attributeTarget = x.SelectNodes("x:target", manager).Item(0).InnerXml;
                                  })
            (lang, nodes)

    module FileCompiler =
        open System
        open FParsec
        open Ids.Localization.Parsers

        let private deTag (t : LocalizationSourcePart) : string = 
            match t with
                | Text s -> s
                | Variable s -> s

        let private deTagAttr (t : LocalizationSourcePart) : string = 
            match t with
                | Text s -> "'" + s + "'"
                | Variable s -> s 

        let private lspToTarg(lsp : LocalizationSourcePart seq) =
            String.Join("", lsp |> Seq.map deTag)

        let private lspToAttr(lsp : LocalizationSourcePart seq) =
            "\"" + String.Join(" + ", lsp |> Seq.map deTagAttr ) + "\""


        let firstNonEmpty = Seq.find (not << Seq.isEmpty)

        let private getTranslation (matches : LocalizationMatch seq) (tag : IdLocalizationTag) : string = 
            let optionalMatch = matches |> Seq.tryFind (fun x -> tag.id = x.id)
            match optionalMatch with
                | Some m -> if tag.isInHtmlAttribute
                                then lspToAttr(firstNonEmpty [m.target; m.source])
                                else lspToTarg(firstNonEmpty [m.target; m.source])
                | None -> if tag.isInHtmlAttribute
                            then lspToAttr(tag.source)
                            else lspToTarg(tag.source)

        let generateNewFile contents (matches : LocalizationMatch seq, isHtml : bool) : string =
            match run LocalizationTagParser.getChunks (Text.NormalizeNewlines contents, isHtml) with
                | Success (x, _, endPosition) -> 
                    let rest = Text.NormalizeNewlines(contents).Substring(int32(endPosition.Index))
                    match x with
                        | [] -> rest
                        | x -> (x |> Seq.map(fun (leader, tag) -> (leader, generateIdTag(tag))) |> Seq.map (fun (leader, tag) -> leader + (getTranslation matches tag)) |> Seq.reduce (+)) + rest
                | Failure (reasons, state, _) -> reasons
