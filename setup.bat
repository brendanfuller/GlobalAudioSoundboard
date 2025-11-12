@echo off
echo ================================================
echo Global Audio Soundboard - Setup Script
echo ================================================
echo.

REM Check if .NET SDK is already installed
dotnet --list-sdks > "%TEMP%\dotnet-sdks.txt" 2>nul
if %errorlevel% equ 0 (
    REM Check if the output file has content
    for /f %%i in ("%TEMP%\dotnet-sdks.txt") do set size=%%~zi
    if %size% gtr 0 (
        echo .NET SDK is already installed:
        type "%TEMP%\dotnet-sdks.txt"
        del "%TEMP%\dotnet-sdks.txt"
        echo.
        goto :restore
    )
    del "%TEMP%\dotnet-sdks.txt"
)

echo .NET SDK is not installed or not working properly.
echo.
echo ================================================
echo MANUAL INSTALLATION REQUIRED
echo ================================================
echo.
echo Please follow these steps:
echo.
echo 1. Open your browser and go to:
echo    https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo 2. Download the ".NET 8.0 SDK" (NOT Runtime) for Windows x64
echo.
echo 3. Run the installer and follow the prompts
echo.
echo 4. After installation completes, RESTART THIS COMMAND PROMPT
echo    (Close this window and open a new one)
echo.
echo 5. Run setup.bat again
echo.
echo ================================================
echo.
echo Opening download page in your browser...
echo.

REM Open the download page
start https://dotnet.microsoft.com/download/dotnet/8.0

echo.
echo Press any key to exit...
pause >nul
exit /b 1

:restore
echo ================================================
echo Restoring NuGet packages...
echo ================================================
echo.

dotnet restore GlobalAudio.sln

if %errorlevel% neq 0 (
    echo Failed to restore packages.
    pause
    exit /b 1
)

echo.
echo ================================================
echo Setup complete!
echo ================================================
echo.
echo To build the project, run: build.bat
echo To run the project, run: run.bat
echo.
pause
