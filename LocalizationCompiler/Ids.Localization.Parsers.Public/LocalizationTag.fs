// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationTag.fs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   Localization Tag
// </summary>
// --------------------------------------------------------------------------------------------------------------------
module Ids.Localization.Parsers.LocalizationTag

type LocalizationSourcePart =
    | Text of string
    | Variable of string

type LocalizationTag = {
    source : LocalizationSourcePart seq;
    translatorNote : string;
    isInAttribute : bool;
    isInJavascriptString: bool;
}

type IdLocalizationTag = {
    id : string;
    source : LocalizationSourcePart seq;
    translatorNote : string;
    isInAttribute : bool;
    isInJavascriptString: bool;
}