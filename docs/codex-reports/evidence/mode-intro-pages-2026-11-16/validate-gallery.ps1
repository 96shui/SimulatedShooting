param([string]$Name="gallery-tool-validation")
$ErrorActionPreference='Stop'
$projectPath=(Get-Location).Path
while (Get-Process Unity -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 5 }
$evidencePath=Join-Path $projectPath 'docs/codex-reports/evidence/mode-intro-pages-2026-11-16'
$env:RUNTIME_GALLERY_OUT=Join-Path $evidencePath $Name
$env:RUNTIME_GALLERY_MODE1='1'
try {
    $unityArgs=@('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','VRShooting.Editor.TacticalUIGallery.CaptureBatch','-logFile',('"'+$evidencePath+('/'+$Name+'.log"')))
    $unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    Write-Output ('gallery-tool-validation PID '+$unityProcess.Id)
    $unityProcess.WaitForExit()
    Write-Output ('gallery-tool-validation exit '+$unityProcess.ExitCode)
} finally {
    Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue
    Remove-Item Env:RUNTIME_GALLERY_MODE1 -ErrorAction SilentlyContinue
}
