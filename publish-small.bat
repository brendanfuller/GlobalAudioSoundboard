@echo off
echo Building framework-dependent single-file executable...
echo.
echo NOTE: This requires .NET 8 Desktop Runtime to be installed on target machine
echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
echo.

cd GlobalAudioSoundboard
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

echo.
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo.
    echo Single executable file created at:
    echo GlobalAudioSoundboard\bin\Release\net8.0-windows\win-x64\publish\GlobalAudioSoundboard.exe
    echo.
    echo This file is much smaller (~1-2 MB) but requires .NET 8 Desktop Runtime.
) else (
    echo Build failed with error code %ERRORLEVEL%
)

pause
