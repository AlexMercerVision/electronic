[CmdletBinding()]
param(
    [string]$BatchRoot = 'E:\VB_Det_PACK\20261020_50Series_RC03_Installer',
    [string]$Compiler = 'E:\jieyawenjian\Inno-Setup\Inno Setup 7\ISCC.exe',
    [switch]$Prepare
)
$ErrorActionPreference = 'Stop'
$audit = Split-Path $PSScriptRoot -Parent
$inputRoot = Join-Path $BatchRoot 'Input'
$sourceRoot = 'E:\VB_Det_PACK\20261020_50Series_RC02\03_Release'
if (-not (Test-Path -LiteralPath "$inputRoot\App")) { throw 'Create isolated RC03 Input from RC02 before running this build.' }
. (Join-Path $PSScriptRoot 'RuntimeSupport.ps1')
if ($Prepare) {
    # Preserve the frozen application byte for byte; only packaging/launcher scripts change.
    $provenance = @(foreach ($file in Get-ChildItem -LiteralPath "$inputRoot\App" -File -Recurse) {
        $relative = $file.FullName.Substring($inputRoot.Length + 1)
        $original = Join-Path $sourceRoot $relative
        $digest = Get-FileDigest $file.FullName
        if ($digest -ne (Get-FileDigest $original)) { throw "RC02 payload differs: $relative" }
        [pscustomobject]@{ RelativePath = $relative; Bytes = $file.Length; SHA256 = $digest; Source = $original }
    })
    if ($provenance.Count -ne 67) { throw 'Expected exactly 67 frozen application files.' }
    $provenance | Export-Csv -LiteralPath (Join-Path $audit 'app-provenance.csv') -NoTypeInformation -Encoding UTF8
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ElectrodeDet.iss') -Destination "$BatchRoot\Installer\ElectrodeDet.iss" -Force
    foreach ($name in @('RuntimeSupport.ps1','Configure-Runtime.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination "$inputRoot\Prerequisites\$name" -Force
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Verify-Installed.ps1') -Destination "$inputRoot\Verify-Installed.ps1" -Force
    Copy-Item -LiteralPath (Join-Path $audit '安装说明及校验清单.md') -Destination "$inputRoot\安装说明及校验清单.md" -Force
    $launcher = [IO.File]::ReadAllText((Join-Path $sourceRoot 'Launch.ps1'))
    $launcher = $launcher.Replace('$recordFile = $null', @'
$recordFile = $null
$record = [ordered]@{ Batch = '20261020_50Series_RC03_Installer'; Status = 'Starting' }
'@)
    $launcher = $launcher.Replace('try {' + "`n" + '    if (-not $RuntimePathsFile)', @'
try {
    if (-not $RecordDirectory) { $RecordDirectory = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'ElectrodeDet\Logs\Launcher' }
    [IO.Directory]::CreateDirectory($RecordDirectory) | Out-Null
    $recordFile = Join-Path $RecordDirectory ('launch_start_' + (Get-Date -Format 'yyyyMMdd_HHmmss_fff') + '_' + $PID + '.json')
    & (Join-Path $release 'Verify-Installed.ps1') -InstallRoot $release
    if (-not $RuntimePathsFile)
'@)
    $launcher = $launcher.Replace("Batch = `$lock.Batch; Time", "Batch = '20261020_50Series_RC03_Installer'; SourceLockBatch = `$lock.Batch; Time")
    $launcher = $launcher.Replace('RC02启动/检查失败：', 'RC03启动/检查失败：')
    if ($launcher -notmatch 'Verify-Installed.ps1') { throw 'Launcher patch did not match the frozen source.' }
    # BOM required by Windows PowerShell 5.1 for Chinese paths and messages.
    [IO.File]::WriteAllText((Join-Path $inputRoot 'Launch.ps1'), $launcher, (New-Object Text.UTF8Encoding($true)))
    $manifest = @(foreach ($file in Get-ChildItem -LiteralPath $inputRoot -File -Recurse | Sort-Object FullName) {
        if ($file.Name -in @('payload-manifest.csv','payload-manifest.sha256','RuntimePaths.json')) { continue }
        [pscustomobject]@{ RelativePath = $file.FullName.Substring($inputRoot.Length + 1); Bytes = $file.Length; SHA256 = (Get-FileDigest $file.FullName) }
    })
    $manifest | Export-Csv -LiteralPath "$inputRoot\payload-manifest.csv" -NoTypeInformation -Encoding UTF8
    ((Get-FileDigest "$inputRoot\payload-manifest.csv") + '  payload-manifest.csv') | Set-Content -LiteralPath "$inputRoot\payload-manifest.sha256" -Encoding ASCII
}
& "$inputRoot\Verify-Installed.ps1" -InstallRoot $inputRoot
$buildLog = Join-Path $audit 'installer-build.log'
('Compiler: ' + $Compiler + "`r`nStarted: " + (Get-Date).ToString('o')) | Set-Content -LiteralPath $buildLog -Encoding UTF8
$argsList = @("$BatchRoot\Installer\ElectrodeDet.iss")
& $Compiler @argsList 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) { throw "ISCC failed: $LASTEXITCODE" }
('ISCC exit code: 0; completed: ' + (Get-Date).ToString('o')) | Add-Content -LiteralPath $buildLog -Encoding UTF8
$setup = Join-Path $BatchRoot 'Output\电极帽缺陷检测_Setup.exe'
[pscustomobject]@{ File = $setup; Bytes = (Get-Item -LiteralPath $setup).Length; SHA256 = (Get-FileDigest $setup); Signature = (Get-AuthenticodeSignature -LiteralPath $setup).Status.ToString() } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $audit 'setup-artifact.json') -Encoding UTF8
