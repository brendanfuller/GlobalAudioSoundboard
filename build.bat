@echo off
echo ================================================
echo Building Global Audio Soundboard...
echo ================================================
echo.

dotnet build GlobalAudio.sln -c Release

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
echo Executable location: GlobalAudio\bin\Release\net8.0-windows\GlobalAudio.exe
echo.
pause
