// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XliffGenerator.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Generates xliff files
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids.Localization.Parsers.XliffGenerator

open System
open System.Xml
open System.Text.RegularExpressions
open Ids.Localization.Parsers.LocalizationTag

module XliffGenerator =
    let generateIdTag(localizationTag : LocalizationTag) : IdLocalizationTag =
        let sha256Hasher = System.Security.Cryptography.SHA256.Create()
        let flatText = localizationTag.source
                        |> Seq.map (fun part ->
                            match part with
                                | Text x -> x
                                | Variable x -> x)
                        |> Seq.reduce (+)
            
        let id = Convert.ToBase64String(sha256Hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(flatText + localizationTag.translatorNote)))
        {
            id = id;
            source = localizationTag.source;
            translatorNote = localizationTag.translatorNote;
            isInAttribute = localizationTag.isInAttribute;
        }

    type XliffGenerator() = 

        let ns = "urn:oasis:names:tc:xliff:document:1.2"

        let generator = new XmlDocument()
        let declaration = generator.AppendChild(generator.CreateXmlDeclaration("1.0", "utf-8", null))
        let namespaceManager = new XmlNamespaceManager(generator.NameTable)
        let rootNode = generator.AppendChild(generator.CreateNode(XmlNodeType.Element, "xliff", ns))
        let rootVersionAttribute = rootNode.Attributes.Append(generator.CreateAttribute("version"))
        let fileNode = rootNode.AppendChild(generator.CreateNode(XmlNodeType.Element, "file", ns))
        let fileNodeOriginalAttribute = fileNode.Attributes.Append(generator.CreateAttribute("original"))
        let dataTypeAttribute = fileNode.Attributes.Append(generator.CreateAttribute("datatype"))
        let sourceLanguageAttribute = fileNode.Attributes.Append(generator.CreateAttribute("source-language"))
        let targetLanguageAttribute = fileNode.Attributes.Append(generator.CreateAttribute("target-language"))
        let bodyTag = fileNode.AppendChild(generator.CreateNode(XmlNodeType.Element, "body", ns))

        do
            rootVersionAttribute.Value <- "1.2"
            dataTypeAttribute.Value <- "x-blah-msg-bundle"
            sourceLanguageAttribute.Value <- "en"
            namespaceManager.AddNamespace("x", ns)


        let tagToXmlElement (tag : IdLocalizationTag) =
            let newTag = generator.CreateNode(XmlNodeType.Element, "trans-unit", ns)
            let idAttribute = newTag.Attributes.Append(generator.CreateAttribute("id"))
            let dataTypeAttribute = newTag.Attributes.Append(generator.CreateAttribute("datatype"))

            dataTypeAttribute.Value <- "html"

            let sourceTextNode = newTag.AppendChild(generator.CreateNode(XmlNodeType.Element, "source", ns))
            let targetTextNode = newTag.AppendChild(generator.CreateNode(XmlNodeType.Element, "target", ns))
            let noteNode = newTag.AppendChild(generator.CreateNode(XmlNodeType.Element, "note", ns))
            let notesText = noteNode.AppendChild(generator.CreateNode(XmlNodeType.Text, "", ns))

            tag.source
                |> Seq.map (fun tagPart ->
                    match tagPart with
                        | Text x -> let node = generator.CreateNode(XmlNodeType.Text, "", ns)
                                    node.Value <- x
                                    node
                        | Variable x -> let node = generator.CreateNode(XmlNodeType.Element, "x", ns)
                                        let attribute = node.Attributes.Append(generator.CreateAttribute("id"))
                                        attribute.Value <- x
                                        node)
                |> Seq.iter(fun node -> sourceTextNode.AppendChild(node) |> ignore)

            notesText.Value <- tag.translatorNote

            idAttribute.Value <- tag.id

            let search = sprintf @"//*[@%s]" idAttribute.OuterXml

            if bodyTag.SelectNodes(search, namespaceManager).Count < 1 then
                bodyTag.AppendChild(newTag) |> ignore
            else ignore 0


        member x.Generator with get() = generator

        member x.Generate originalFileName (localizationTags : LocalizationTag seq) =
            fileNodeOriginalAttribute.Value <- originalFileName
            targetLanguageAttribute.Value <- "en-US"

            localizationTags |> Seq.map generateIdTag |> Seq.iter tagToXmlElement

            generator