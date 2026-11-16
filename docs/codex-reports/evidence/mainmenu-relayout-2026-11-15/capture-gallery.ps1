$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity instance still running' }
$sourcePath=Join-Path $projectPath 'docs/codex-reports/evidence/mainmenu-relayout-2026-11-15/MainMenuRelayoutEvidence.cs.txt'
$helperPath=Join-Path $projectPath 'Assets/VRShooting/Editor/MainMenuRelayoutEvidence.cs'
if (Test-Path -LiteralPath $helperPath) { throw 'Temporary helper path already exists' }
Copy-Item -LiteralPath $sourcePath -Destination $helperPath
$env:RUNTIME_GALLERY_OUT=Join-Path $projectPath 'docs/codex-reports/evidence/mainmenu-relayout-2026-11-15'
try {
    $unityArgs=@('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','MainMenuRelayoutEvidence.CaptureBatch','-logFile',('"'+$projectPath+'/Logs/mainmenu-relayout/gallery.log"'))
    $unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    Write-Output ('gallery PID '+$unityProcess.Id)
} finally {
    Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue
}
# After Unity exits: remove only this helper and its generated .meta, then restore unrelated batch writes.
