param(
    [Parameter(Mandatory=$true)][string]$MediaMtxDirectory,
    [Parameter(Mandatory=$true)][string]$FfmpegDirectory,
    [string]$InnoCompiler
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet restore windows/Nxtroute/Nxtroute.csproj -r win-x64 --configfile NuGet.Config --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
    dotnet publish windows/Nxtroute/Nxtroute.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --no-restore -o dist/portable
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    $components = Join-Path $projectRoot 'dist/portable/components'
    New-Item -ItemType Directory -Path $components -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $MediaMtxDirectory 'mediamtx.exe') -Destination $components
    Copy-Item -LiteralPath (Join-Path $FfmpegDirectory 'bin/ffmpeg.exe') -Destination $components
    Copy-Item -LiteralPath (Join-Path $FfmpegDirectory 'bin/ffprobe.exe') -Destination $components
    Copy-Item -LiteralPath (Join-Path $MediaMtxDirectory 'LICENSE') -Destination (Join-Path $components 'MediaMTX-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $FfmpegDirectory 'LICENSE.txt') -Destination (Join-Path $components 'FFmpeg-LICENSE.txt')
    Copy-Item -LiteralPath LICENSE -Destination dist/portable
    Copy-Item -LiteralPath docs/DEPENDENCIES.md -Destination dist/portable
    if ($InnoCompiler) {
        & $InnoCompiler windows/installer/NXTROUTE.iss
        if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
    }
} finally { Pop-Location }
