param([ValidateSet('EditMode','PlayMode')][string]$Kind,[string]$Name,[string]$Filter='')
$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
while (Get-Process Unity -ErrorAction SilentlyContinue) {
    Write-Output 'Waiting for the existing Unity instance to exit; no process will be killed.'
    Start-Sleep -Seconds 5
}
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mode-intro-pages-2026-11-16'
if ((Test-Path -LiteralPath ($evidencePath+'/'+$Name+'.xml')) -or (Test-Path -LiteralPath ($evidencePath+'/'+$Name+'.log'))) {
    throw 'Use a new result name; do not overwrite test evidence.'
}
$unityArgs=@('-batchmode','-projectPath',('"'+$projectPath+'"'),'-runTests','-testPlatform',$Kind,'-testResults',('"'+$evidencePath+'/'+$Name+'.xml"'),'-logFile',('"'+$evidencePath+'/'+$Name+'.log"'))
if ($Filter) { $unityArgs+=@('-testFilter',$Filter) }
$unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output ($Name+' PID '+$unityProcess.Id)

# Unity sometimes stalls after it has persisted NUnit XML and shut down input/licensing.
# Only terminate this runner's own process, after a completed XML and 30 quiet seconds.
while (-not $unityProcess.HasExited) {
    Start-Sleep -Seconds 3
    $resultFile=Join-Path $evidencePath ($Name+'.xml')
    $logFile=Join-Path $evidencePath ($Name+'.log')
    if ((Test-Path -LiteralPath $resultFile) -and (Test-Path -LiteralPath $logFile)) {
        $last=(Get-Content -LiteralPath $logFile -Tail 15) -join "`n"
        if ($last -match 'Input System module state changed to: Shutdown\.' -and
            ((Get-Date)-(Get-Item -LiteralPath $logFile).LastWriteTime).TotalSeconds -gt 30) {
            $completed=[xml](Get-Content -LiteralPath $resultFile -Raw)
            if ($completed.'test-run'.result) {
                Stop-Process -Id $unityProcess.Id -ErrorAction SilentlyContinue
                @{ name=$Name; processId=$unityProcess.Id; reason='Completed XML saved; Unity stalled after input/licensing shutdown'; result=$completed.'test-run'.result } |
                    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath ($Name+'-shutdown.json')) -Encoding utf8
            }
        }
    }
}
Write-Output ($Name+' exited; result '+(Test-Path -LiteralPath (Join-Path $evidencePath ($Name+'.xml'))))
