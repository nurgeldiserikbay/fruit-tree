param(
    [Parameter(Mandatory = $true)]
    [string] $UnityEditor,
    [switch] $BuildApk
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logs = Join-Path $project 'Builds'
New-Item -Path $logs -ItemType Directory -Force | Out-Null

function Invoke-Unity {
    param([string[]] $Arguments, [string] $LogName)
    $log = Join-Path $logs $LogName
    & $UnityEditor -batchmode -nographics -quit -projectPath $project @Arguments -logFile $log
    $code = $LASTEXITCODE
    if ($code -ne 0) {
        Write-Host "Unity failed. Log: $log"
        if (Test-Path $log) { Get-Content $log -Tail 75 }
        throw "Unity command failed with exit code $code."
    }
    Write-Host "OK - $LogName"
}

if (-not (Test-Path $UnityEditor)) {
    throw "Unity Editor not found at: $UnityEditor"
}

function Assert-TestResults {
    param([string] $FileName)
    $file = Join-Path $logs $FileName
    if (-not (Test-Path $file)) { throw "Unity did not write test results: $file" }
    [xml] $report = Get-Content $file -Raw
    $run = $report.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw "Unexpected Unity test report format: $file" }
    $total = [int] $run.GetAttribute('total')
    $passed = [int] $run.GetAttribute('passed')
    $failed = [int] $run.GetAttribute('failed')
    $skipped = [int] $run.GetAttribute('skipped')
    $inconclusive = [int] $run.GetAttribute('inconclusive')
    $result = $run.GetAttribute('result')
    if ($result -ne 'Passed' -or $total -le 0 -or $passed -ne $total -or
        $failed -gt 0 -or $skipped -gt 0 -or $inconclusive -gt 0) {
        throw "Unity tests incomplete: result=$result passed=$passed total=$total failed=$failed skipped=$skipped inconclusive=$inconclusive (see $file)"
    }
    Write-Host "Tests passed: $passed / $total in $FileName"
}

Invoke-Unity -Arguments @('-executeMethod', 'FruitTree.Editor.ValidateProject.Run') -LogName 'scene-validation.log'

# Never allow an old successful XML report to mask a missing test run.
Remove-Item (Join-Path $logs 'editmode-results.xml') -Force -ErrorAction SilentlyContinue
Invoke-Unity -Arguments @('-runTests', '-testPlatform', 'EditMode',
    '-testResults', (Join-Path $logs 'editmode-results.xml')) -LogName 'editmode.log'
Assert-TestResults 'editmode-results.xml'
Remove-Item (Join-Path $logs 'playmode-results.xml') -Force -ErrorAction SilentlyContinue
Invoke-Unity -Arguments @('-runTests', '-testPlatform', 'PlayMode',
    '-testResults', (Join-Path $logs 'playmode-results.xml')) -LogName 'playmode.log'
Assert-TestResults 'playmode-results.xml'

# A tree cannot unlock the next species unless every fruit is harvestable.
# This release gate validates all ten species, not only the initial apple tree.
Invoke-Unity -Arguments @('-executeMethod',
    'FruitTree.Editor.FruitReachabilityAudit.AuditAllSpecies') -LogName 'all-fruits-audit.log'

if ($BuildApk) {
    Invoke-Unity -Arguments @('-buildTarget', 'Android',
        '-executeMethod', 'FruitTree.Editor.BuildAndroid.Run') -LogName 'android-build.log'
    Write-Host "Built APK (if supported by this Unity installation): $(Join-Path $logs 'FruitTree.apk')"
}

Write-Host "Unity checks completed. Review XML test results and logs in $logs."
