$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Another Unity instance is running' }
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mainmenu-heading-restore-2026-11-16'
$helperPath=Join-Path $projectPath 'Assets/VRShooting/Editor/MainMenuHeadingEvidence.cs'
if (Test-Path -LiteralPath $helperPath) { throw 'Temporary helper already exists' }
Copy-Item -LiteralPath ($evidencePath+'/MainMenuHeadingEvidence.cs.txt') -Destination $helperPath
$env:RUNTIME_GALLERY_OUT=$evidencePath
try {
    $unityArgs=@('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','MainMenuHeadingEvidence.CaptureBatch','-logFile',('"'+$evidencePath+'/gallery.log"'))
    $unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    Write-Output ('gallery PID '+$unityProcess.Id)
    $unityProcess.WaitForExit()
    Write-Output ('gallery exit '+$unityProcess.ExitCode)
    if ($unityProcess.ExitCode -ne 0) { throw 'Gallery capture failed; inspect gallery.log' }
} finally {
    Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue
    if ($null -eq $unityProcess -or $unityProcess.HasExited) {
        Remove-Item -LiteralPath $helperPath -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($helperPath+'.meta') -ErrorAction SilentlyContinue
    }
}
