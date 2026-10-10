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

Invoke-Unity -Arguments @('-runTests', '-testPlatform', 'EditMode',
    '-testResults', (Join-Path $logs 'editmode-results.xml')) -LogName 'editmode.log'
Invoke-Unity -Arguments @('-runTests', '-testPlatform', 'PlayMode',
    '-testResults', (Join-Path $logs 'playmode-results.xml')) -LogName 'playmode.log'

if ($BuildApk) {
    Invoke-Unity -Arguments @('-buildTarget', 'Android',
        '-executeMethod', 'FruitTree.Editor.BuildAndroid.Run') -LogName 'android-build.log'
    Write-Host "Built APK (if supported by this Unity installation): $(Join-Path $logs 'FruitTree.apk')"
}

Write-Host "Unity checks completed. Review XML test results and logs in $logs."
