<#
.SYNOPSIS
    Builds a self-contained portable MarkMello for Windows x64 (no installer, no .NET install needed).

.DESCRIPTION
    Runs `dotnet publish` (Release, win-x64, self-contained, not single-file) into -OutputDir and
    renames MarkMello.exe to -ExeName.

    Why the rename: Windows remembers "Open with" applications by FILE NAME. An installed MarkMello
    registers `MarkMello.exe`, so a portable copy with the same name is mistaken for it and `.md`
    files keep opening the installed version. A unique name avoids that.

    Why the release source: the update check is pointed at -ReleaseOwner/-ReleaseRepo. The defaults
    are this fork's repository, which has no releases, so the portable build is never offered the
    upstream release as an "update" that would replace it.

.PARAMETER OutputDir
    Where to publish. The default is `publish\portable-win-x64` in the repository (git-ignored).
    Close the program before rebuilding into the same folder.

.EXAMPLE
    .\packaging\windows\build-portable.ps1

.EXAMPLE
    .\packaging\windows\build-portable.ps1 -OutputDir ..\MarkMello-outline
#>
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\..\publish\portable-win-x64"),

    [string]$Version = "0.6.1",

    [string]$ReleaseOwner = "konicaRu",

    [string]$ReleaseRepo = "Markdown-my-viewer-MarkMello",

    [string]$ExeName = "MarkMello-outline.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$project = Join-Path $repoRoot "src\MarkMello.Desktop\MarkMello.Desktop.csproj"
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Project not found: $project"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found. Install the .NET SDK 10 (winget install Microsoft.DotNet.SDK.10)."
}

# The project targets net10.0, so an SDK older than 10 cannot build it.
$hasSdk10 = (& dotnet --list-sdks) | Where-Object { $_ -match '^(\d+)\.' -and [int]$Matches[1] -ge 10 }
if (-not $hasSdk10) {
    throw ".NET SDK 10 or newer is required (winget install Microsoft.DotNet.SDK.10)."
}

$outputPath = [System.IO.Path]::GetFullPath($OutputDir)

# A running copy locks its files; fail early with a clear message instead of halfway through publish.
$running = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    try { $_.Path -and $_.Path.StartsWith($outputPath, [System.StringComparison]::OrdinalIgnoreCase) } catch { $false }
}
if ($running) {
    throw "MarkMello is running from $outputPath. Close it and run the script again."
}

& dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    "-p:Version=$Version" `
    "-p:MarkMelloReleaseOwner=$ReleaseOwner" `
    "-p:MarkMelloReleaseRepo=$ReleaseRepo" `
    -o $outputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$builtExe = Join-Path $outputPath "MarkMello.exe"
if (-not (Test-Path -LiteralPath $builtExe -PathType Leaf)) {
    throw "Publish finished but $builtExe was not produced."
}

$finalExe = Join-Path $outputPath $ExeName
Move-Item -LiteralPath $builtExe -Destination $finalExe -Force

Write-Host ""
Write-Host "Portable build is ready: $finalExe"
