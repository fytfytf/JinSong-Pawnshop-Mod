param([ValidateRange(20,120)][int]$TimeoutSeconds = 90, [switch]$DirectLaunch, [switch]$Focused, [switch]$MenuLanguage, [switch]$ChromeOnly, [switch]$FinishOnly, [switch]$ReportedIssues, [switch]$AlcoveOnly, [switch]$StreetOnly, [switch]$PolishOnly, [switch]$GridOnly, [switch]$MarketOnly, [switch]$CompanyOnly, [switch]$ArchiveServicesOnly, [switch]$FixtureClicksOnly, [switch]$WorkshopOnly, [switch]$NetworkOnly, [switch]$AssassinOnly, [switch]$WorkshopCompatibility)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$game = 'E:/steam/steamapps/common/Probably Stolen'
$source = Join-Path $project 'test-only/alpha240-ui-survey'
$helper = Join-Path $game 'Mods/jinsong_poc_probe'
$output = Join-Path $project ('evidence/runs/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-ui-survey')
$log = Join-Path $output 'runtime.log'
$owned = $null
$added = $false
$result = @{outcome='infra_error'; log=$log}
$names = @('manifest.xml','JinSongProbe.dll')
$surveyMode = if($Focused){' --jinsong-ui-survey-focus'}else{''}
if($MenuLanguage){$surveyMode+=' --jinsong-ui-survey-menu-language'}
if($ChromeOnly){$surveyMode+=' --jinsong-ui-survey-chrome-only'}
if($FinishOnly){$surveyMode+=' --jinsong-ui-survey-finish-only'}
if($ReportedIssues){$surveyMode+=' --jinsong-ui-survey-reported'}
if($AlcoveOnly){$surveyMode+=' --jinsong-ui-survey-alcove-only'}
if($StreetOnly){$surveyMode+=' --jinsong-ui-survey-street-only'}
if($PolishOnly){$surveyMode+=' --jinsong-ui-survey-polish-only'}
if($GridOnly){$surveyMode+=' --jinsong-ui-survey-grid-only'}
if($MarketOnly){$surveyMode+=' --jinsong-ui-survey-market-only'}
if($CompanyOnly){$surveyMode+=' --jinsong-ui-survey-company-only'}
if($ArchiveServicesOnly){$surveyMode+=' --jinsong-ui-survey-archive-services'}
if($FixtureClicksOnly){$surveyMode+=' --jinsong-ui-survey-fixture-clicks'}
if($WorkshopOnly){$surveyMode+=' --jinsong-ui-survey-workshop'}
if($NetworkOnly){$surveyMode+=' --jinsong-ui-survey-network'}
if($AssassinOnly){$surveyMode+=' --jinsong-ui-survey-assassin'}
if($WorkshopCompatibility){$surveyMode+=' --jinsong-ui-survey-workshop-compat'}
$hashes = @{}
try {
    if (Get-Process -Name 'Probably Stolen','ProbablyStolen' -ErrorAction SilentlyContinue) { throw 'Game is already running; no process changed.' }
    if (Test-Path -LiteralPath $helper) { throw 'Existing helper retained; target must be unused.' }
    foreach ($directory in @($game,(Join-Path $game 'Mods'),$source)) {
        for ($node=Get-Item -LiteralPath $directory; $node; $node=$node.Parent) {
            if ($node.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked path rejected.' }
        }
    }
    New-Item -ItemType Directory -Path $output | Out-Null
    New-Item -ItemType Directory -Path $helper | Out-Null
    $added=$true
    foreach ($name in $names) {
        $hashes[$name]=(Get-FileHash -LiteralPath (Join-Path $source $name)).Hash
        Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $helper $name)
    }
    if ($DirectLaunch) {
        # Normal game entry point with Steam API checks intact; no offline/authentication bypass.
        $owned=Start-Process -FilePath (Join-Path $game 'Probably Stolen.exe') -WorkingDirectory $game -ArgumentList "-logFile `"$log`" --jinsong-ui-survey$surveyMode" -WindowStyle Hidden -PassThru
    } else {
        Start-Process -FilePath 'E:/steam/steam.exe' -ArgumentList "-applaunch 4348910 -logFile `"$log`" --jinsong-ui-survey$surveyMode" -WindowStyle Hidden
    }
    $timer=[Diagnostics.Stopwatch]::StartNew()
    while (!$owned -and $timer.Elapsed.TotalSeconds -lt 20) {
        $match=Get-CimInstance Win32_Process -Filter "Name = 'Probably Stolen.exe'" | Where-Object { $_.ExecutablePath -eq (Join-Path $game 'Probably Stolen.exe') -and $_.CommandLine.Contains($log) } | Select-Object -First 1
        if ($match) { $owned=Get-Process -Id $match.ProcessId -ErrorAction SilentlyContinue }
        if (!$owned) { Start-Sleep -Milliseconds 400 }
    }
    if (!$owned) { throw 'No tagged game process launched.' }
    while (!$owned.HasExited -and $timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) { Start-Sleep -Milliseconds 400 }
    if (!$owned.HasExited) { throw 'Targeted ui-survey check did not finish within its bound.' }
    if (!(Test-Path -LiteralPath $log)) { throw 'No runtime evidence.' }
    $surveyLine = Select-String -LiteralPath $log -Pattern '\[JINSONG_SURVEY\] RESULT (\{.*\})$' | Select-Object -Last 1
    if (!$surveyLine) { throw 'No targeted test result; inspect startup/loader output.' }
    $surveyData = $surveyLine.Matches[0].Groups[1].Value | ConvertFrom-Json
    $result.outcome = $surveyData.outcome
    $result.checks = $surveyData.checks
    $result.runtime_errors = $surveyData.runtime_errors
    if($surveyData.runtime_errors -gt 0){$result.outcome='failed'}
    $result.load_attempts = $surveyData.load_attempts
    $result.phase_failures = $surveyData.phase_failures

} catch { $result.reason=$_.Exception.Message }
finally {
    if ($owned -and !$owned.HasExited) {
        [void]$owned.CloseMainWindow()
        if (!$owned.WaitForExit(3000)) { $owned.Kill(); [void]$owned.WaitForExit(3000) }
    }
    if ($added -and (!$owned -or $owned.HasExited)) {
        try {
            $actual=@(Get-ChildItem -LiteralPath $helper -Force)
            if ($actual.Count -ne 2) { throw 'Unknown helper content retained.' }
            foreach ($file in $actual) {
                if ($file.PSIsContainer -or $file.Name -notin $names -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne $hashes[$file.Name]) { throw 'Changed helper retained.' }
            }
            foreach ($name in $names) { Remove-Item -LiteralPath (Join-Path $helper $name) }
            Remove-Item -LiteralPath $helper
            $result.helper_removed=$true
        } catch { $result.outcome='infra_error'; $result.cleanup_reason=$_.Exception.Message }
    }
}
$captureRoot = Join-Path $env:USERPROFILE 'AppData\LocalLow\Questing Goose Studio\Probably Stolen\ModData\jinsong_poc_probe'
if ((Test-Path -LiteralPath $log) -and (Test-Path -LiteralPath $captureRoot)) {
    $declared = @(Select-String -LiteralPath $log -Pattern '\[JINSONG_SURVEY\] CAPTURED (survey-[a-z0-9_-]+\.png)$' | ForEach-Object { $_.Matches[0].Groups[1].Value })
    foreach ($name in $declared) {
        $file = Join-Path $captureRoot $name
        if (!(Test-Path -LiteralPath $file)) { continue }
        $entry = Get-Item -LiteralPath $file
        if ($entry.PSIsContainer -or ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unexpected capture entry retained' }
        Copy-Item -LiteralPath $file -Destination (Join-Path $output $name)
        if ((Get-FileHash -LiteralPath $file).Hash -ne (Get-FileHash -LiteralPath (Join-Path $output $name)).Hash) { throw 'Capture copy mismatch' }
        Remove-Item -LiteralPath $file
    }
    if (@(Get-ChildItem -LiteralPath $captureRoot -Force).Count -eq 0) { Remove-Item -LiteralPath $captureRoot }
    $result.screenshots = $declared.Count
}
if (Test-Path -LiteralPath $log) {
    $result.log_sha256=(Get-FileHash -LiteralPath $log).Hash.ToLowerInvariant()
    Select-String -LiteralPath $log -Pattern '\[JINSONG_SURVEY\]' | ForEach-Object { $_.Line }
}
$result | ConvertTo-Json -Compress
if ($result.outcome -eq 'passed') { exit 0 }
if ($result.outcome -eq 'failed') { exit 1 }
exit 2
