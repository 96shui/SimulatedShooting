param([string]$ProjectPath=(Get-Location).Path,[Parameter(Mandatory=$true)][string]$Name,[switch]$Probe)
$ErrorActionPreference='Stop'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Another Unity instance is running' }
$evidence=Split-Path -Parent $PSCommandPath
$output=Join-Path $evidence $Name
if (Test-Path -LiteralPath $output) { throw 'Do not overwrite evidence' }
New-Item -ItemType Directory -Path $output | Out-Null
$helper=Join-Path $ProjectPath 'Assets/VRShooting/Editor/ResultsTitleEvidence.cs'
if (Test-Path -LiteralPath $helper) { throw 'Temporary helper already exists' }
Copy-Item -LiteralPath (Join-Path $evidence 'ResultsTitleEvidence.cs.txt') -Destination $helper
$env:RUNTIME_GALLERY_OUT=$output
if ($Probe) { $env:RESULTS_TITLE_PROBE="1" }
try {
    $arguments=@('-batchmode','-quit','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','ResultsTitleEvidence.CaptureBatch','-logFile',('"'+$output+'/gallery.log"'))
    $process=Start-Process 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ($Name+' PID '+$process.Id)
    $process.WaitForExit()
    Write-Output ($Name+' exit '+$process.ExitCode)
    if ($process.ExitCode -ne 0) { throw 'Capture failed; inspect gallery.log' }
} finally {
    Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue
    Remove-Item Env:RESULTS_TITLE_PROBE -ErrorAction SilentlyContinue
    if ($null -eq $process -or $process.HasExited) {
        Remove-Item -LiteralPath $helper -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($helper+'.meta') -ErrorAction SilentlyContinue
    }
}
