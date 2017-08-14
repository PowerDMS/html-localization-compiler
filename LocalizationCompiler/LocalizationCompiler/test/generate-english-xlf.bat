echo off
"..\bin\Debug\LocalizationCompiler.exe" -g "website" "translations\default.xlf"
IF NOT "%~1"=="nopause" PAUSE