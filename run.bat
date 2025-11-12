@echo off
echo ================================================
echo Running Global Audio Soundboard...
echo ================================================
echo.

cd GlobalAudioSoundboard
dotnet run

if %errorlevel% neq 0 (
    echo.
    echo Failed to run the application.
    pause
    exit /b 1
)
