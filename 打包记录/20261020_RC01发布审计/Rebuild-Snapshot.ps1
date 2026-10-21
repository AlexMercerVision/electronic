param([string]$MSBuild = 'E:\VS-IDE\MSBuild\Current\Bin\amd64\MSBuild.exe')
$ErrorActionPreference = 'Stop'
$batch = Split-Path -Parent $PSScriptRoot
$snapshot = Join-Path $batch '01_SourceSnapshot'
if (-not (Test-Path -LiteralPath (Join-Path $snapshot 'VisionDection.sln'))) { throw 'Source snapshot missing' }
if ($batch -eq 'E:\jieyawenjian\VB_Det' -or $batch.StartsWith('E:\jieyawenjian\VB_Det\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Original workspace is not an output location' }
$run = Join-Path $batch ('02_Build\Recheck_' + (Get-Date -Format yyyyMMdd_HHmmss) + '_' + [guid]::NewGuid().ToString('N').Substring(0,8))
$output = Join-Path $run 'Output\'
$obj = Join-Path $run 'Obj\'
New-Item -ItemType Directory -Path $output,$obj | Out-Null
Push-Location -LiteralPath $snapshot
try {
    & $MSBuild VisionDection.sln '/t:PipeChanel;Build' /p:Configuration=Debug /p:Platform=x64 "/p:OutputPath=$output" "/p:ReferencePath=$output" "/p:IntermediateOutputPath=$obj" /p:UseSharedCompilation=false /nr:false /m:1 /v:minimal /fl "/flp:logfile=$run\solution-build.log;verbosity=diagnostic"
    if ($LASTEXITCODE -ne 0) { throw "Build failed. See $run" }
    Write-Host "Build passed. New output: $output. Frozen 03_Release was not replaced."
} finally { Pop-Location }
