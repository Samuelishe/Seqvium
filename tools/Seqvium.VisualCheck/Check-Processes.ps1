# Independent Windows processes; only process objects created here may be terminated.
param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../../.artifacts/desktop-f1/processes'),
    [string]$ExecutablePath = (Join-Path $PSScriptRoot 'bin/Release/net10.0/Seqvium.VisualCheck.exe'))
$ErrorActionPreference = 'Stop'
$output = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfff') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$executable = [IO.Path]::GetFullPath($ExecutablePath)
$owned = [Collections.Generic.List[Diagnostics.Process]]::new()
$results = [Collections.Generic.List[object]]::new()
$status = 'failure'
$overall = [DateTime]::UtcNow.AddSeconds(90)
& $executable retention begin $output processes $PID
if ($LASTEXITCODE -ne 0) { throw 'Cannot establish artifact ownership.' }
function Deadline { if ([DateTime]::UtcNow -ge $overall) { throw 'Process verification exceeded its 90-second deadline.' } }
function Launch([string]$mode, [string]$identity, [string]$directory) {
    Deadline
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $arguments = @($mode, $identity, ('"' + $directory + '"'))
    $logId = $mode + '-' + [Guid]::NewGuid().ToString('N')
    $process = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $directory ($logId + '.stdout.log')) `
        -RedirectStandardError (Join-Path $directory ($logId + '.stderr.log'))
    $owned.Add($process)
    return $process
}
function Ready($process, [string]$directory) {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (!(Test-Path -LiteralPath (Join-Path $directory 'ready.txt'))) {
        Deadline
        if ($process.HasExited -or [DateTime]::UtcNow -gt $deadline) { throw "Primary did not become ready: $directory" }
        Start-Sleep -Milliseconds 50
    }
}
function Exit-Code($process, [int]$expected) {
    Deadline
    $remaining = [Math]::Min(10000, [Math]::Max(1, ($overall - [DateTime]::UtcNow).TotalMilliseconds))
    if (!$process.WaitForExit([int]$remaining)) { throw "Child did not exit within deadline: $($process.Id)" }
    if ($process.ExitCode -ne $expected) { throw "Unexpected exit $($process.ExitCode), expected $expected" }
}
try {
    $identity = [Guid]::NewGuid().ToString('N')
    $directory = Join-Path $output $identity
    $first = Launch 'desktop' $identity $directory
    Ready $first $directory
    $second = Launch 'desktop' $identity $directory
    Exit-Code $second 0
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    while ((!(Test-Path -LiteralPath (Join-Path $directory 'window-state.txt')) -or
        (Get-Content -LiteralPath (Join-Path $directory 'window-state.txt')) -ne 'Normal') -and [DateTime]::UtcNow -lt $deadline) { Deadline; Start-Sleep -Milliseconds 50 }
    if ((Get-Content -LiteralPath (Join-Path $directory 'window-state.txt')) -ne 'Normal') { throw 'Existing minimized window was not restored.' }
    if (@(Get-Content -LiteralPath (Join-Path $directory 'documents.txt')).Count -ne 1) { throw 'A second Desktop document was created.' }
    $results.Add([pscustomobject]@{ Check = 'two independent Desktop processes / restore minimized window'; Result = 'passed'; Primary = $first.Id; Secondary = $second.Id })
    # Explicit crash of this isolated test-owned primary only.
    Stop-Process -InputObject $first -Force
    if (!$first.WaitForExit(5000)) { throw 'Test-owned crashed primary did not exit.' }
    Remove-Item -LiteralPath (Join-Path $directory 'ready.txt')
    $replacement = Launch 'desktop' $identity $directory
    Ready $replacement $directory
    if (@(Get-Content -LiteralPath (Join-Path $directory 'documents.txt')).Count -ne 2) { throw 'Crash recovery did not create exactly one new document.' }
    Set-Content -LiteralPath (Join-Path $directory 'stop.txt') -Value 'stop'
    Exit-Code $replacement 0
    $results.Add([pscustomobject]@{ Check = 'crashed primary / reacquire ownership / graceful joined shutdown'; Result = 'passed' })
    foreach ($mode in @('noipc', 'stalled')) {
        $identity = [Guid]::NewGuid().ToString('N'); $directory = Join-Path $output $identity
        $primary = Launch $mode $identity $directory; Ready $primary $directory
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $client = Launch 'desktop' $identity $directory; Exit-Code $client 2
        if (Test-Path -LiteralPath (Join-Path $directory 'documents.txt')) { throw 'An IPC failure created a Desktop document.' }
        if (!(Test-Path -LiteralPath (Join-Path $directory 'diagnostic.txt'))) { throw 'Missing bounded failure diagnostic.' }
        Set-Content -LiteralPath (Join-Path $directory 'stop.txt') -Value 'stop'; Exit-Code $primary 0
        $results.Add([pscustomobject]@{ Check = $mode; Result = 'passed'; ElapsedMilliseconds = $timer.ElapsedMilliseconds; SecondDocument = $false })
    }
    $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'process-results.json')
    $results | Format-Table
    $status = 'success'
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $output 'failure.txt')
    throw
}
finally {
    $joined = $true
    foreach ($process in $owned) {
        try {
            if (!$process.HasExited) { Stop-Process -InputObject $process -Force }
            if (!$process.WaitForExit(5000)) { $joined = $false }
        } catch { $joined = $false; Write-Warning $_ }
        finally { $process.Dispose() }
    }
    $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'process-results.json')
    # A run with an unjoined child stays incomplete and cannot be rotated.
    if ($joined) {
        & $executable retention complete $output $status
        if ($LASTEXITCODE -ne 0) { throw 'Artifact completion/retention failed.' }
    }
}
