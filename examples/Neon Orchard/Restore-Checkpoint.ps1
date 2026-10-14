# SPDX-License-Identifier: Apache-2.0
param(
    [Parameter(Mandatory)][string]$Snapshot,
    [Parameter(Mandatory)][string]$Destination
)
$ErrorActionPreference = 'Stop'
$snapshotPath = (Resolve-Path -LiteralPath $Snapshot).Path
$destinationPath = [System.IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $destinationPath) { throw 'Destination must be a new directory.' }
$project = Get-Content -Raw -LiteralPath $snapshotPath | ConvertFrom-Json
if ($project.format -ne 'seqvium-project') { throw 'Not a Seqvium project snapshot.' }
$mediaRoot = Join-Path $PSScriptRoot 'Neon Orchard.seqvium.media'
# Validate every locator and immutable media hash before creating any output.
$locators = @($project.state.resources.managedLocator | Sort-Object -Unique)
foreach ($locator in $locators) {
    if ($locator -cnotmatch '^wav/[0-9a-f]{32}-([0-9a-f]{64})\.wav$') {
        throw "Unsupported media locator: $locator"
    }
    $expectedHash = $Matches[1]
    $source = Join-Path $mediaRoot $locator
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
        throw "Media integrity mismatch: $locator"
    }
}
New-Item -ItemType Directory -Path $destinationPath | Out-Null
$target = Join-Path $destinationPath 'Neon Orchard.seqvium'
Copy-Item -LiteralPath $snapshotPath -Destination $target
New-Item -ItemType Directory -Path ($target + '.media/wav') | Out-Null
foreach ($locator in $locators) {
    Copy-Item -LiteralPath (Join-Path $mediaRoot $locator) -Destination (Join-Path ($target + '.media') $locator)
}
New-Item -ItemType Directory -Path (Join-Path $destinationPath 'licenses') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses/VSCO2-CE-CC0.txt') -Destination (Join-Path $destinationPath 'licenses')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../LICENSE') -Destination (Join-Path $destinationPath 'licenses/Apache-2.0.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MEDIA.json') -Destination $destinationPath
Write-Output $target
