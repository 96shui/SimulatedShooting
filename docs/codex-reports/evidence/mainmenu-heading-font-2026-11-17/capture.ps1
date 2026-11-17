param([string]$ProjectPath=(Get-Location).Path,[Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Another Unity instance is running' }
$evidence=Split-Path -Parent $PSCommandPath
$output=Join-Path $evidence $Name
if (Test-Path -LiteralPath $output) { throw 'Do not overwrite evidence' }
New-Item -ItemType Directory -Path $output | Out-Null
$helper=Join-Path $ProjectPath 'Assets/VRShooting/Editor/HeadingFontEvidence.cs'
if (Test-Path -LiteralPath $helper) { throw 'Temporary helper already exists' }
Copy-Item -LiteralPath (Join-Path $evidence 'HeadingFontEvidence.cs.txt') -Destination $helper
$env:RUNTIME_GALLERY_OUT=$output
try {
    $arguments=@('-batchmode','-quit','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','HeadingFontEvidence.CaptureBatch','-logFile',('"'+$output+'/gallery.log"'))
    $process=Start-Process 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ($Name+' PID '+$process.Id)
    $process.WaitForExit()
    Write-Output ($Name+' exit '+$process.ExitCode)
    if ($process.ExitCode -ne 0) { throw 'Capture failed; inspect gallery.log' }
} finally {
    Remove-Item Env:RUNTIME_GALLERY_OUT -ErrorAction SilentlyContinue
    if ($null -eq $process -or $process.HasExited) {
        Remove-Item -LiteralPath $helper -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ($helper+'.meta') -ErrorAction SilentlyContinue
    }
}
