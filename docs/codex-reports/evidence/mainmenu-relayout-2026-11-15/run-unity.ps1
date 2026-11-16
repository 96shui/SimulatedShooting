param([string]$Kind,[string]$Name,[string]$Filter='')
$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity instance still running' }
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mainmenu-relayout-2026-11-15'
$unityArgs=@('-batchmode','-projectPath',('"'+$projectPath+'"'),'-runTests','-testPlatform',$Kind,'-testResults',('"'+$evidencePath+'/'+$Name+'.xml"'),'-logFile',('"'+$projectPath+'/Logs/mainmenu-relayout/'+$Name+'.log"'))
if ($Filter) { $unityArgs+=@('-testFilter',$Filter) }
$unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output ($Name+' PID '+$unityProcess.Id)
