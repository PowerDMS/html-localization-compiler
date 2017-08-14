echo off
"..\bin\Debug\LocalizationCompiler.exe" "translations" "website" "output"
IF NOT "%~1"=="nopause" PAUSE