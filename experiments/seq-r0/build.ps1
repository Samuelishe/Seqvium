# SPDX-License-Identifier: Apache-2.0
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $compiler = (Get-Command gcc -ErrorAction Stop).Source
    $version = & $compiler -dumpfullversion
    if ($version -ne '16.2.0') { throw "Probe requires GCC 16.2.0; found $version" }
    & dotnet build Probe.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Managed build failed' }
    & $compiler -std=c11 -O2 -Wall -Wextra -Werror -ffp-contract=off -shared native/kernel.c native/probe.c native/device.c native/controlled.c -o bin/Release/net10.0/r0.dll -lole32 -luuid -lavrt -static-libgcc
    if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
    $toolchainRoot = Split-Path (Split-Path $compiler -Parent) -Parent
    $notices = Join-Path $PSScriptRoot 'bin/Release/net10.0/licenses'
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $toolchainRoot 'share/licenses/crt/COPYING.MinGW-w64-runtime.txt') -Destination $notices
    Copy-Item -LiteralPath (Join-Path $toolchainRoot 'share/licenses/gcc-libs/COPYING.RUNTIME') -Destination $notices
    Copy-Item -LiteralPath (Join-Path $toolchainRoot 'share/licenses/gcc-libs/COPYING3') -Destination $notices
} finally { Pop-Location }
