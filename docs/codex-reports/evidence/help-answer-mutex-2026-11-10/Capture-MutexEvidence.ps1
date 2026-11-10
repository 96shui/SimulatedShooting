param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$results = Join-Path $PSScriptRoot 'capture.xml'
$log = Join-Path $PSScriptRoot 'capture.log'
$previous = $env:HELP_ANSWER_MUTEX_CAPTURE
$env:HELP_ANSWER_MUTEX_CAPTURE = '1'

try {
    # The test creates the production UI and uses its real button events and camera renderer.
    $arguments = @(
        '-batchmode', '-projectPath', ('"' + $project + '"'),
        '-runTests', '-testPlatform', 'PlayMode',
        '-testFilter', 'VRShooting.Tests.PlayMode.UI.Screen06_ZeroingImpactAnalysisUITests.Bdd06_InfoPanelsAreExclusiveAndKeepTheirFixedPositions',
        '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"')
    )
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity capture failed with exit code $($process.ExitCode); see $log" }
    [xml]$report = Get-Content -Raw -Encoding UTF8 $results
    if ($report.'test-run'.result -ne 'Passed') { throw "Screenshot test did not pass; see $results" }
    foreach ($name in @('01-collapsed.png', '02-help-expanded.png', '03-answer-expanded.png', '04-collapsed-restored.png')) {
        $image = Join-Path $PSScriptRoot $name
        if (!(Test-Path -LiteralPath $image)) { throw "Missing screenshot: $image" }
        Write-Output $image
    }
}
finally {
    $env:HELP_ANSWER_MUTEX_CAPTURE = $previous
}
