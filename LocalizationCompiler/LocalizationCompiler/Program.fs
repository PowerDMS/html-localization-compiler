// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   The Program
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace LocalizationCompiler

module Program =

    open System
    open System.IO

    open Ids.Generators
    open Ids.Localization.Parsers
    open Ids.Localization.Parsers.XliffGenerator.XliffGenerator

    let supportedFileTypes = new System.Text.RegularExpressions.Regex("htm|html|js")

    let rec generateXlfFromFiles (d : DirectoryInfo) (x : XliffGenerator) =
        printfn "%s" d.FullName
        d.EnumerateFiles()
            |> Seq.filter(fun f -> supportedFileTypes.IsMatch(f.Extension))
            |> Seq.iter(fun f -> 
                printfn "\t%s" f.Name
                x.Generate f.Name (LocalizationTagParser.parse (File.ReadAllText(f.FullName))) |> ignore)
        
        d.EnumerateDirectories() |> Seq.iter (fun d' -> generateXlfFromFiles d' x)
        
    let withConsoleColor color f = 
        Console.ForegroundColor <- color
        f()
        Console.ForegroundColor <- ConsoleColor.Gray

    let printMatchedFile (f : FileInfo) = withConsoleColor ConsoleColor.Green (fun () -> printfn "\t%s" f.Name)

    let printLanguage (s : string) = withConsoleColor ConsoleColor.Yellow (fun () -> printfn "\n\nLanguage: %s\n" s)

    let rec generateApplicationFromXlf (tags : LocalizationMatch seq) (applicationDirectory : DirectoryInfo) (outputDirectory : DirectoryInfo) =
        printfn "%s" applicationDirectory.FullName

        applicationDirectory.EnumerateFiles()
            |> Seq.iter(fun f ->
                let outputName = Path.Combine(outputDirectory.FullName, f.Name)
                if f.Extension |> supportedFileTypes.IsMatch then
                    printMatchedFile f

                    let newContents = HtmlCompiler.generateNewHtmlFile (File.ReadAllText f.FullName) tags
                    File.WriteAllText(outputName, newContents, Text.Encoding.UTF8)
                else
                    printfn "\t%s" f.Name
                    File.Copy(f.FullName, outputName, true))

        applicationDirectory.EnumerateDirectories()
            |> Seq.iter(fun d -> generateApplicationFromXlf tags d (outputDirectory.CreateSubdirectory(d.Name)))

    [<EntryPoint>]
    let Main(args) =
        let listArgs = Array.toList args

        if listArgs.Length = 0 || listArgs.Head = "-?" || listArgs.Head = "--help" then
            printfn "Usage:\n"
            printfn "Generating xliff"
            printfn "   LocalizationCompiler.exe -g <website dir> <output file name>\n"
            printfn "Compiling the HTML"
            printfn "   LocalizationCompiler.exe <dir containing xliffs> <website dir> <output dir>\n"

        else if listArgs.Head = "-g" then
            let (dir::outputName::_) = List.tail listArgs

            printfn "Generating..."
            let websiteDirectory = new DirectoryInfo(dir)

            let generator = new XliffGenerator()

            generateXlfFromFiles websiteDirectory generator

            generator.Generator.Save(outputName)

        else
            printfn "Building static sites..."
            let (xliffsDir::applicationDir::outputDir::_) = listArgs

            let xliffs = (new DirectoryInfo(xliffsDir)).EnumerateFiles("*.xlf")
            xliffs
                |> Seq.map (fun xlf -> XmlToLocalizationMatch.generate xlf.FullName)
                |> Seq.iter (fun (lang, tags) -> 
                    printLanguage lang
                    generateApplicationFromXlf tags (new DirectoryInfo(applicationDir)) ((new DirectoryInfo(outputDir)).CreateSubdirectory(lang)))

        0

