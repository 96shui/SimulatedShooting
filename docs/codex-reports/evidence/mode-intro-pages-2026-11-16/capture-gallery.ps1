$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
while (Get-Process Unity -ErrorAction SilentlyContinue) {
    Write-Output 'Waiting for the existing Unity instance to exit; no process will be killed.'
    Start-Sleep -Seconds 5
}
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mode-intro-pages-2026-11-16'
$helperPath=Join-Path $projectPath 'Assets/VRShooting/Editor/ModeIntroEvidence.cs'
if (Test-Path -LiteralPath $helperPath) { throw 'Temporary helper path already exists' }
Copy-Item -LiteralPath ($evidencePath+'/ModeIntroEvidence.cs.txt') -Destination $helperPath
$env:RUNTIME_GALLERY_OUT=$evidencePath
try {
    $unityArgs=@('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','ModeIntroEvidence.CaptureBatch','-logFile',('"'+$evidencePath+'/gallery.log"'))
    $unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    Write-Output ('gallery PID '+$unityProcess.Id)
} finally { Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue }
# After Unity exits remove only this helper and its generated .meta.
