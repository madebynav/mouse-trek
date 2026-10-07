param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'The Windows .NET Framework C# compiler was not found.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Add-Type -AssemblyName System.Drawing
$iconBitmap = New-Object System.Drawing.Bitmap 32,32
$iconGraphics = [System.Drawing.Graphics]::FromImage($iconBitmap)
$iconGraphics.Clear([System.Drawing.Color]::FromArgb(17,22,32))
$iconBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(181,154,255))
$iconGraphics.FillEllipse($iconBrush,5,11,24,17)
$iconGraphics.FillEllipse($iconBrush,15,1,12,12)
$iconGraphics.FillEllipse($iconBrush,7,4,11,11)
$eyeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(17,22,32))
$iconGraphics.FillEllipse($eyeBrush,23,15,3,4)
$noseBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,186,137))
$iconGraphics.FillEllipse($noseBrush,28,21,4,4)
$iconHandle = $iconBitmap.GetHicon()
$generatedIcon = [System.Drawing.Icon]::FromHandle($iconHandle)
$iconFile = Join-Path $OutputDirectory 'PixelTrek.ico'
$iconStream = [System.IO.File]::Create($iconFile)
$generatedIcon.Save($iconStream)
$iconStream.Dispose()
$generatedIcon.Dispose()
$iconGraphics.Dispose()
$iconBitmap.Dispose()
$iconBrush.Dispose()
$eyeBrush.Dispose()
$noseBrush.Dispose()
$sources = @('Core.cs','Input.cs','Widget.cs','Program.cs','Tests.cs','Adventure.cs','Dashboard.cs','TrekTests.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /debug- /checked+ "/out:$(Join-Path $OutputDirectory 'PixelTrek.exe')" "/win32manifest:$(Join-Path $PSScriptRoot 'PixelTrek.manifest')" "/win32icon:$iconFile" "/resource:$iconFile,PixelTrek.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PixelTrek.exe.config') -Destination $OutputDirectory -Force
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'README.txt')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination $OutputDirectory -Force }
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'LICENSE.txt')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE.txt') -Destination $OutputDirectory -Force }
Write-Output "Built $(Join-Path $OutputDirectory 'PixelTrek.exe')"
