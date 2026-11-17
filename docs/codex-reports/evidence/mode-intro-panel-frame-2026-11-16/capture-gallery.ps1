$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Another Unity instance is active.' }
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mode-intro-panel-frame-2026-11-16'
$helperPath=Join-Path $projectPath 'Assets/VRShooting/Editor/ModeIntroPanelFrameEvidence.cs'
if (Test-Path -LiteralPath $helperPath) { throw 'Temporary helper path already exists.' }
Copy-Item -LiteralPath ($evidencePath+'/ModeIntroPanelFrameEvidence.cs.txt') -Destination $helperPath
$previousGalleryOut=$env:RUNTIME_GALLERY_OUT
$env:RUNTIME_GALLERY_OUT=$evidencePath
try {
    $unityArgs=@('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','ModeIntroPanelFrameEvidence.CaptureBatch','-logFile',('"'+$evidencePath+'/gallery.log"'))
    $unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    Write-Output ('gallery PID '+$unityProcess.Id)
    while (-not $unityProcess.HasExited) { Start-Sleep -Seconds 3 }
    Write-Output ('gallery exit '+$unityProcess.ExitCode)
} finally {
    $env:RUNTIME_GALLERY_OUT=$previousGalleryOut
    if ((-not (Get-Process Unity -ErrorAction SilentlyContinue)) -and (Test-Path -LiteralPath $helperPath)) {
        Remove-Item -LiteralPath $helperPath
        if (Test-Path -LiteralPath ($helperPath+'.meta')) { Remove-Item -LiteralPath ($helperPath+'.meta') }
    }
}
