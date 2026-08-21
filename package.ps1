# Builds a release and zips it the way Vortex expects: the archive must contain a
# top-level Mods\ folder, or Vortex drops the dll into the game root and nothing loads.
#
#   pwsh -File package.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet build -c Release "$root\SmartSlotFilter.csproj"

$dll = "$root\bin\Release\net6.0\SmartSlotFilter.dll"
if (-not (Test-Path $dll)) { throw "no build output at $dll" }

# Version comes from the MelonInfo attribute, so the archive name can never drift
# from what the game reports in the console.
$src = Get-Content "$root\Main.cs" -Raw
if ($src -notmatch 'MelonInfo\(.*?,\s*"[^"]*",\s*"([0-9][^"]*)"') { throw "no version in MelonInfo" }
$version = $Matches[1]

$stage = "$root\bin\package"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory "$stage\Mods" | Out-Null
Copy-Item $dll "$stage\Mods\"

$zip = "$root\bin\SmartSlotFilter-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$stage\Mods" -DestinationPath $zip

Write-Output "packaged $zip"
