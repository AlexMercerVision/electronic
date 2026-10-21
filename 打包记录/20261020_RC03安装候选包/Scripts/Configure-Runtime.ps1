[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$InstallRoot,
    [Parameter(Mandatory=$true)][string]$ReportDirectory,
    [string]$CudaRoot,
    [string]$CudnnBin,
    [switch]$Apply,
    [switch]$AllowMissingDependencies
)
$ErrorActionPreference = 'Stop'
try {
    if (-not [Environment]::Is64BitProcess) { throw 'Environment checks require 64-bit Windows PowerShell.' }
    . (Join-Path $PSScriptRoot 'RuntimeSupport.ps1')
    [IO.Directory]::CreateDirectory($ReportDirectory) | Out-Null
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'GPU50-runtime-lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $paths = Find-GpuPaths $lock $CudaRoot $CudnnBin
    $findings = @(Get-EnvironmentFindings)
    $missing = @($findings | Where-Object { -not $_.Passed } | ForEach-Object Name)
    if (-not $paths.CudaRoot) { $missing += 'CUDA 12.8.0: version + all locked hashes' }
    if (-not $paths.CudnnBin) { $missing += 'cuDNN 9.14.0.64 CUDA12: all locked hashes' }
    $report = [ordered]@{
        Batch = '20261020_50Series_RC03_Installer'; Time = (Get-Date).ToString('o'); Is64BitProcess = [Environment]::Is64BitProcess
        Paths = $paths; Findings = $findings; Missing = $missing
        AllowMissingDependencies = [bool]$AllowMissingDependencies
        Boundary = 'File/registration precheck only; SDK licenses, device drivers, GPU architecture, inference and physical IO require acceptance.'
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ReportDirectory 'dependency-report.json') -Encoding UTF8
    @('ElectrodeDet RC03 dependency check', ('Missing: ' + ($missing -join ', ')), 'Install matching vendor media from Prerequisites, then rerun Setup.', $report.Boundary) |
        Set-Content -LiteralPath (Join-Path $ReportDirectory 'dependency-summary.txt') -Encoding ASCII
    if ($missing.Count -and -not $AllowMissingDependencies) { exit 2 }
    if ($Apply) {
        & (Join-Path $InstallRoot 'Verify-Installed.ps1') -InstallRoot $InstallRoot
        $config = [ordered]@{
            CudaRoot = $paths.CudaRoot; CudnnBin = $paths.CudnnBin
            LogDirectory = (Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'ElectrodeDet\Logs\Launcher')
        }
        $file = Join-Path $InstallRoot 'RuntimePaths.json'
        $temporary = $file + '.new'
        try {
            $config | ConvertTo-Json | Set-Content -LiteralPath $temporary -Encoding UTF8
            # Windows PowerShell 5.1 converts $null to an empty string for this overload.
            # A real same-volume backup path keeps atomic replacement valid on upgrades.
            if (Test-Path -LiteralPath $file) { [IO.File]::Replace($temporary, $file, ($file + '.bak')) }
            else { [IO.File]::Move($temporary, $file) }
        } finally {
            if (Test-Path -LiteralPath $temporary) { [IO.File]::Delete($temporary) }
        }
        Copy-Item -LiteralPath $file -Destination (Join-Path $ReportDirectory 'RuntimePaths.generated.json') -Force
        Write-Host 'RuntimePaths.json generated; existing application data and authorization preserved.'
    }
    exit 0
} catch {
    [Console]::Error.WriteLine($_.Exception.ToString())
    if (Test-Path -LiteralPath $ReportDirectory) { $_.Exception.ToString() | Set-Content -LiteralPath (Join-Path $ReportDirectory 'configure-error.txt') -Encoding UTF8 }
    exit 1
}
