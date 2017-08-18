// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationTagParser.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Parses localization tags that match the format [[String in native language|Translator Notes]]
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids.Localization.Parsers

module LocalizationTagParser = 

    open FParsec
    open Ids.Localization.Parsers.LocalizationTag

    //Todo: Escaped chars (like pipes and whatever)
    
    let private escapedChar = pchar '\\' >>. anyChar

    let private chars = attempt escapedChar <|> anyChar

    let private parseTextPart = manyCharsTill chars (lookAhead <| ((spaces >>. skipString "|") <|> skipString "[[")) |>> fun x -> Text x

    let private parseVariablePart = pstring "[[" >>. manyCharsTill anyChar (pstring "]]") |>> fun x -> Variable x
    
    let private parseSource = many1Till (parseVariablePart <|> parseTextPart) (spaces >>? (pchar '|' .>> spaces))

    let private buildTag jsString source notes = { LocalizationTag.source = source; translatorNote = notes; isInJsString = jsString }
    
    let private closeTag str = (manyCharsTill anyChar (spaces >>? pstring str))

    let private pipeSourceToFn str = pipe2 parseSource (closeTag str)

    let private parseTag = skipString "[[" >>. spaces >>. pipeSourceToFn "]]" (buildTag false)

    let private parseTagInJsString = skipString "'[[" >>. spaces >>. pipeSourceToFn "]]'" (buildTag true)
    
    let getChunks fileContents isHtmlFile = fileContents |> many (attempt (manyCharsTill anyChar (lookAhead <| (skipString "\'[[" <|> skipString "[[")) .>>. (attempt parseTag <|> parseTagInJsString)))

    let private parseCruft = getChunks |>> (fun x -> Seq.map snd x)
    
    let parse contents = 
            let output = run parseCruft (Text.NormalizeNewlines contents)
            match output with
                | Success(result, _, _) -> result
                | Failure(_, _, _) -> Seq.empty