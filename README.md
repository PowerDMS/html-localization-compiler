HTML Localization Compiler
=====================
The HTML Localization Compiler will pull out localized strings from all HTML files in a specified directory to create an xliff file of all strings which need translations as well as their descriptions. Once you have made new xliff files for each desired language, you can then use this same tool to generate new directories containing every file in the specified folder where each HTML file will be compiled to have to proper localized text for each language in it.

You must identify localizable strings in your HTML files with the following syntax: `[[String in native language|Description for translator]]`

* Note: If you don't want to include a description, you must still include the pipe (e.g. `[[String in native language|]]`).

* Additional Note: If you are using Angular.js [http://angularjs.org/] you can still use `[[Strings containing [[Angular Interpolation Expressions]]!|Isn't that cool?]]`
	* Specifics, if it is in a regular single quoted attribute or just on the page, for example: `<span title="[[hello [[name]]|greeting]]">[[hello [[name]]|greeting]]</span>` it will compile using {{}} to `<span title="hello {{name}}">hello {{name}}</span>`
	* However if it is an angular directive data bound attribute, as interpreted by attribute="'string'", for example: `<span directive="'[[hello [[name]]|greeting]]'">[[hello [[name]]|greeting]]</span>` it will compile this to `<span title="'hello ' + name">hello {{name}}</span>`

Building
--------
We are intending to provide a binary download option, but for now you will need to open the solution in Visual Studio to build it (or use msbuild)

During Development
--------
During development time, you can optionally use the parseLocalizationTags javascript function (included in parse-localization-tags.js) to strip out the square brackets and translator notes so that the ui will look just like it will during release in the native language.

However, you will probably be much happier coding against the same html that your users will recieve so having the compilation be a part of a grunt watch task is a great idea.

Generating xliff
--------
LocalizationCompiler.exe -g <website directory> <output file name>

Compiling the HTML
--------
LocalizationCompiler.exe <directory containing xliff files> <website directory> <output directory>

License
=======

	HTML Localization Compiler
    Copyright (C) 2013 Innovative Data Solutions

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

