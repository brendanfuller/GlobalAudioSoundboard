@echo off
echo Building single-file executable...
echo.

cd GlobalAudioSoundboard
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true

echo.
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo.
    echo Single executable file created at:
    echo GlobalAudioSoundboard\bin\Release\net8.0-windows\win-x64\publish\GlobalAudioSoundboard.exe
    echo.
    echo You can copy this single .exe file anywhere and run it without dependencies.
) else (
    echo Build failed with error code %ERRORLEVEL%
)

pause
