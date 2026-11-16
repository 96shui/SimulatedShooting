param([ValidateSet('EditMode','PlayMode')][string]$Kind,[string]$Name,[string]$Filter='')
$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
while (Get-Process Unity -ErrorAction SilentlyContinue) {
    Write-Output 'Waiting for the existing Unity instance to exit; no process will be killed.'
    Start-Sleep -Seconds 5
}
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mainmenu-heading-border-2026-11-15'
if ((Test-Path -LiteralPath ($evidencePath+'/'+$Name+'.xml')) -or (Test-Path -LiteralPath ($evidencePath+'/'+$Name+'.log'))) {
    throw 'Use a new result name; do not overwrite test evidence.'
}
$unityArgs=@('-batchmode','-projectPath',('"'+$projectPath+'"'),'-runTests','-testPlatform',$Kind,'-testResults',('"'+$evidencePath+'/'+$Name+'.xml"'),'-logFile',('"'+$evidencePath+'/'+$Name+'.log"'))
if ($Filter) { $unityArgs+=@('-testFilter',$Filter) }
$unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output ($Name+' PID '+$unityProcess.Id)
