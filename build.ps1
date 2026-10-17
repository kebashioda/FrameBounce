$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $projectRoot 'src\FrameBounce.cs'
$assetPath = Join-Path $projectRoot 'assets'
$outputPath = Join-Path $projectRoot 'dist\Frame Bounce.scr'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assemblyRoot = Join-Path $env:WINDIR 'Microsoft.NET\assembly'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found. Build this project on 64-bit Windows with .NET Framework 4.x.'
}

$compilerArgs = @('/nologo', '/target:winexe', ('/out:' + $outputPath))
$referenceNames = @('WindowsBase', 'PresentationCore', 'PresentationFramework',
    'WindowsFormsIntegration', 'System.Xaml')

foreach ($referenceName in $referenceNames) {
    $pattern = Join-Path $assemblyRoot ("GAC_*\$referenceName\*\$referenceName.dll")
    $reference = Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $reference) {
        throw "Required Windows assembly was not found: $referenceName"
    }
    $compilerArgs += '/r:' + $reference.FullName
}

$compilerArgs += @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll')
$sprites = Get-ChildItem -LiteralPath $assetPath -Filter '*.png' -File | Sort-Object Name
if ($sprites.Count -ne 3) {
    throw "Expected three PNG sprites in $assetPath"
}
foreach ($sprite in $sprites) {
    $compilerArgs += '/resource:' + $sprite.FullName + ',' + $sprite.Name
}

$compilerArgs += $sourcePath
New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}
Write-Output "Built $outputPath"
