@echo off
echo ================================================
echo Building Global Audio Soundboard...
echo ================================================
echo.

dotnet build GlobalAudioSoundboard.sln -c Release

if %errorlevel% neq 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ================================================
echo Build successful!
echo ================================================
echo.
echo Executable location: GlobalAudioSoundboard\bin\Release\net8.0-windows\GlobalAudioSoundboard.exe
echo.
pause
