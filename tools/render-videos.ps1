param([string]$FfmpegPath)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework compiler was not found' }
if ($FfmpegPath) {
    $ffmpeg = (Resolve-Path -LiteralPath $FfmpegPath).Path
} else {
    $command = Get-Command ffmpeg.exe -ErrorAction SilentlyContinue
    if ($command) { $ffmpeg = $command.Source }
    if (-not $ffmpeg) {
        $packageRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
        if (Test-Path -LiteralPath $packageRoot) {
            $ffmpeg = Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter ffmpeg.exe -ErrorAction SilentlyContinue |
                Select-Object -First 1 -ExpandProperty FullName
        }
    }
}
if (-not $ffmpeg) { throw 'FFmpeg was not found. Add ffmpeg.exe to PATH or pass -FfmpegPath.' }
$refs = @('WindowsBase', 'PresentationCore', 'PresentationFramework', 'WindowsFormsIntegration', 'System.Xaml') |
    ForEach-Object {
        $name = $_
        Get-ChildItem -Path (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_*\$name\*\$name.dll") -File |
            Select-Object -First 1 -ExpandProperty FullName
    }
$exe = Join-Path $env:TEMP 'framebounce-offscreen-render.exe'
$compilerArgs = @('/nologo', '/target:exe', ('/out:' + $exe), '/r:System.Windows.Forms.dll') +
    ($refs | ForEach-Object { '/r:' + $_ }) + @((Join-Path $root 'tools\offscreen-render.cs'))
$temporaryFiles = @()
try {
    & $compiler @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw 'Offscreen renderer compilation failed' }
    foreach ($item in @(
        @{ Speed = 100; File = 'demo.webm' },
        @{ Speed = 200; File = 'demo-2x-speed.webm' },
        @{ Speed = 400; File = 'demo-4x-speed.webm' }
    )) {
        $destination = Join-Path $root ('docs\' + $item.File)
        $temporary = Join-Path $root ('docs\' + $item.File.Replace('.webm', '.rendering.webm'))
        $temporaryFiles += $temporary
        & $exe (Join-Path $root 'dist\Frame Bounce.scr') $ffmpeg $temporary $item.Speed
        if ($LASTEXITCODE -ne 0) { throw "Offscreen render failed for $($item.File)" }
        Move-Item -LiteralPath $temporary -Destination $destination -Force
    }

    $normalVideo = Join-Path $root 'docs\demo.webm'
    foreach ($preview in @(
        @{ File = 'preview.png'; Args = @('-ss', '2', '-i', $normalVideo, '-frames:v', '1') },
        @{ File = 'preview-small.png'; Args = @('-ss', '2', '-i', $normalVideo, '-vf', 'scale=800:-1', '-frames:v', '1') }
    )) {
        $destination = Join-Path $root ('docs\' + $preview.File)
        $temporary = Join-Path $root ('docs\' + $preview.File.Replace('.png', '.rendering.png'))
        $temporaryFiles += $temporary
        $previewArgs = $preview.Args
        & $ffmpeg -hide_banner -loglevel error -y @previewArgs $temporary
        if ($LASTEXITCODE -ne 0) { throw "Preview extraction failed: $($preview.File)" }
        Move-Item -LiteralPath $temporary -Destination $destination -Force
    }
} finally {
    foreach ($temporary in $temporaryFiles) {
        Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $exe -Force -ErrorAction SilentlyContinue
}
