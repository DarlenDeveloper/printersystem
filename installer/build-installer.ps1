param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = "1.0.0",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$RepositoryRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepositoryRoot "artifacts\installer"
}

$PublishDirectory = Join-Path $RepositoryRoot "artifacts\publish\win-x64"
$InstallerPath = Join-Path $OutputDirectory "ChequeMate-$Version-win-x64-Setup.exe"
$FileVersion = "$Version.0"

New-Item -ItemType Directory -Force -Path $PublishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$MakeNsisCommand = Get-Command makensis -ErrorAction SilentlyContinue
if ($null -ne $MakeNsisCommand) {
    $MakeNsisPath = $MakeNsisCommand.Source
} else {
    $KnownPath = Join-Path ${env:ProgramFiles(x86)} "NSIS\makensis.exe"
    if (Test-Path $KnownPath) {
        $MakeNsisPath = $KnownPath
    } else {
        throw "NSIS is required. Install it from https://nsis.sourceforge.io/Download and rerun this script."
    }
}

Push-Location $RepositoryRoot
try {
    dotnet publish "src\ChequeMate.App\ChequeMate.App.csproj" `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $PublishDirectory `
        -p:Version=$Version `
        -p:DebugType=None `
        -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw "Application publish failed." }

    & $MakeNsisPath `
        "/DAPP_VERSION=$Version" `
        "/DAPP_FILE_VERSION=$FileVersion" `
        "/DPUBLISH_DIR=$PublishDirectory" `
        "/DOUTPUT_FILE=$InstallerPath" `
        "installer\ChequeMate.nsi"
    if ($LASTEXITCODE -ne 0) { throw "Setup executable build failed." }

    $Hash = Get-FileHash -Algorithm SHA256 $InstallerPath
    $HashLine = "$($Hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($InstallerPath))"
    Set-Content -Encoding ascii -Path "$InstallerPath.sha256" -Value $HashLine

    Write-Host "Installer: $InstallerPath"
    Write-Host "SHA-256:   $($Hash.Hash.ToLowerInvariant())"
}
finally {
    Pop-Location
}
