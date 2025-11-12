@echo off
echo ================================================
echo Creating Application Icon
echo ================================================
echo.

REM Check if Python is installed
python --version >nul 2>nul
if %errorlevel% neq 0 (
    echo Python is not installed. Using online converter method...
    echo.
    echo Please follow these steps:
    echo 1. Go to: https://convertio.co/svg-ico/
    echo 2. Upload the file: GlobalAudio\icon.svg
    echo 3. Click "Convert"
    echo 4. Download and save as: GlobalAudio\app.ico
    echo.
    echo Opening converter website...
    start https://convertio.co/svg-ico/
    pause
    exit /b 0
)

echo Installing required packages...
pip install pillow cairosvg

if %errorlevel% neq 0 (
    echo.
    echo Failed to install packages. Using online converter instead...
    echo.
    echo Please follow these steps:
    echo 1. Go to: https://convertio.co/svg-ico/
    echo 2. Upload the file: GlobalAudio\icon.svg
    echo 3. Click "Convert"
    echo 4. Download and save as: GlobalAudio\app.ico
    echo.
    start https://convertio.co/svg-ico/
    pause
    exit /b 0
)

echo.
echo Converting SVG to ICO...
python create_icon.py

if %errorlevel% equ 0 (
    echo.
    echo Icon created successfully!
) else (
    echo.
    echo Conversion failed. Please use online converter.
)

echo.
pause
