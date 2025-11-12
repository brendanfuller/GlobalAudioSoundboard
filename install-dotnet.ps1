# PowerShell script to install .NET SDK
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Installing .NET 8 SDK" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if already installed
$sdks = dotnet --list-sdks 2>$null
if ($sdks) {
    Write-Host ".NET SDK is already installed:" -ForegroundColor Green
    Write-Host $sdks
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

Write-Host "Downloading .NET 8 SDK installer..." -ForegroundColor Yellow
Write-Host ""

$installerPath = "$env:TEMP\dotnet-sdk-8-installer.exe"
$downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/93961dfb-d1e0-49c8-9230-abcba1ebab5a/811ed1eb63d7652325727720edda26a8/dotnet-sdk-8.0.404-win-x64.exe"

try {
    # Download with progress
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
    $ProgressPreference = 'Continue'

    Write-Host "Download complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Starting installer..." -ForegroundColor Yellow
    Write-Host "NOTE: The installer window will open. Please follow the prompts." -ForegroundColor Yellow
    Write-Host ""

    # Run installer and wait for it to complete
    Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -Verb RunAs

    Write-Host ""
    Write-Host "Installation complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: You must close and reopen your terminal/command prompt" -ForegroundColor Red
    Write-Host "for the changes to take effect!" -ForegroundColor Red
    Write-Host ""

    # Clean up
    Remove-Item $installerPath -ErrorAction SilentlyContinue

    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
catch {
    Write-Host "ERROR: Failed to download or install .NET SDK" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install manually from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
