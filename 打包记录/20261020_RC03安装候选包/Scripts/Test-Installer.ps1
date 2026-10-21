[CmdletBinding()]
param([string]$BatchRoot = 'E:\VB_Det_PACK\20261020_50Series_RC03_Installer', [switch]$ResumeAfterInstall)
$ErrorActionPreference = 'Stop'
$audit = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'RuntimeSupport.ps1')
$setup = Join-Path $BatchRoot 'Output\电极帽缺陷检测_Setup.exe'
$target = Join-Path $env:ProgramFiles 'ElectrodeDet'
if ((Test-Path -LiteralPath $target) -and -not $ResumeAfterInstall) { throw 'Validation install directory already exists. Stop to protect previous evidence.' }
$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{E38CA144-9F6A-4980-BB09-AC7CB6FB3030}_is1'
if ((Test-Path -LiteralPath $uninstallKey) -and -not $ResumeAfterInstall) { throw 'Product already registered. Refusing to alter an existing installation.' }
$shortcut = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\电极帽缺陷检测\电极帽缺陷检测.lnk"
if ((Test-Path -LiteralPath $shortcut) -and -not $ResumeAfterInstall) { throw 'Existing application shortcut found. Refusing to overwrite it.' }
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) '电极帽缺陷检测.lnk'
if ((Test-Path -LiteralPath $desktopShortcut) -and -not $ResumeAfterInstall) { throw 'Existing application desktop shortcut found. Refusing to overwrite it.' }
if ($ResumeAfterInstall) {
    if ((Get-ItemProperty -LiteralPath $uninstallKey).InstallLocation.TrimEnd('\') -ne $target) { throw 'Resume target does not match the validation install' }
    $baseline = @(Import-Csv -LiteralPath (Join-Path $audit 'data-before-install.csv') -Encoding UTF8)
} else {
$baseline = @(foreach ($root in @("$env:ProgramData\ElectrodeDet", "$env:APPDATA\GearDet")) {
    if (Test-Path -LiteralPath $root) {
        foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
            # Installer/launcher logs are expected to be added; all existing business files are protected.
            if ($file.FullName -match '\\Logs\\(Installer|Launcher)\\') { continue }
            [pscustomobject]@{ Path = $file.FullName; SHA256 = (Get-FileDigest $file.FullName) }
        }
    }
})
$baseline | Export-Csv -LiteralPath (Join-Path $audit 'data-before-install.csv') -NoTypeInformation -Encoding UTF8
}
$results = New-Object 'System.Collections.Generic.List[object]'
function Invoke-Setup([string]$Phase, [switch]$AllowMissing) {
    $log = Join-Path $audit ($Phase + '.log')
    $argsText = '/VERYSILENT /SUPPRESSMSGBOXES /SP- /NORESTART /DIR="' + $target + '" /TASKS="desktopicon" /LOG="' + $log + '"'
    if ($AllowMissing) { $argsText += ' /ALLOWMISSINGDEPENDENCIES=1' }
    $process = Start-Process -FilePath $setup -ArgumentList $argsText -WindowStyle Hidden -PassThru -Wait
    $results.Add([pscustomobject]@{ Phase = $Phase; ExitCode = $process.ExitCode; Log = $log })
    return $process.ExitCode
}
# Current build host has no HALCON12 on PATH: strict precheck must reject before any file is installed.
if (-not $ResumeAfterInstall) {
if ((Invoke-Setup 'strict-missing-dependency') -eq 0 -or (Test-Path -LiteralPath "$target\App")) { throw 'Expected strict dependency rejection' }
if ((Get-Content -LiteralPath (Join-Path $audit 'strict-missing-dependency.log') -Raw) -notmatch 'HALCON 12') { throw 'Strict install did not reach the expected HALCON dependency check' }
if ((Invoke-Setup 'install' -AllowMissing) -ne 0) { throw 'Isolated candidate install failed' }
} else {
    if ((Get-Content -LiteralPath (Join-Path $audit 'strict-missing-dependency.log') -Raw) -notmatch 'HALCON 12') { throw 'No strict dependency rejection evidence' }
    $results.Add([pscustomobject]@{ Phase='strict-missing-dependency'; ExpectedRejectionVerified=$true; Log=(Join-Path $audit 'strict-missing-dependency.log') })
    $results.Add([pscustomobject]@{ Phase='install'; ExitCode=0; Log=(Join-Path $audit 'install.log'); Resume='Installed before restricted-process diagnostic failed to initialize' })
}
& "$target\Verify-Installed.ps1" -InstallRoot $target
$config = Get-Content -LiteralPath "$target\RuntimePaths.json" -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $config.LogDirectory.StartsWith($env:ProgramData, [StringComparison]::OrdinalIgnoreCase)) { throw 'Launcher log path must use ProgramData' }
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
if ($link.TargetPath -ne "$target\Launch.cmd" -or $link.WorkingDirectory -ne "$target\App") { throw 'Start menu shortcut does not use dedicated entry and App working directory' }
$results.Add([pscustomobject]@{ Phase = 'start-menu-shortcut'; Target = $link.TargetPath; WorkingDirectory = $link.WorkingDirectory })
$desktopLink = $shell.CreateShortcut($desktopShortcut)
if ($desktopLink.TargetPath -ne "$target\Launch.cmd" -or $desktopLink.WorkingDirectory -ne "$target\App") { throw 'Desktop shortcut does not use dedicated entry and App working directory' }
$results.Add([pscustomobject]@{ Phase = 'desktop-shortcut'; Target = $desktopLink.TargetPath; WorkingDirectory = $desktopLink.WorkingDirectory })
$ps = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$ordinaryReport = Join-Path "$env:ProgramData\ElectrodeDet\Logs\Launcher" ('ordinary-user-' + (Get-Date -Format yyyyMMdd_HHmmss) + '.json')
& (Join-Path $PSScriptRoot 'Test-RestrictedPermissions.ps1') -InstallRoot $target -ReportFile $ordinaryReport
Copy-Item -LiteralPath $ordinaryReport -Destination (Join-Path $audit 'ordinary-user-test.json') -Force
& $ps -NoProfile -ExecutionPolicy Bypass -File "$target\Launch.ps1" -CheckOnly
if ($LASTEXITCODE -ne 0) { throw 'Installed launcher preflight failed' }
& $ps -NoProfile -ExecutionPolicy Bypass -File "$target\Launch.ps1" -VerifyGpu
if ($LASTEXITCODE -ne 0) { throw 'Installed launcher GPU diagnostic failed' }
$badConfig = Join-Path $audit 'invalid-runtime-paths.json'
@{ CudaRoot = 'Z:\ElectrodeDet-Nonexistent-CUDA'; CudnnBin = $config.CudnnBin; LogDirectory = (Join-Path $audit 'negative-launch') } |
    ConvertTo-Json | Set-Content -LiteralPath $badConfig -Encoding UTF8
& $ps -NoProfile -ExecutionPolicy Bypass -File "$target\Launch.ps1" -CheckOnly -RuntimePathsFile $badConfig -RecordDirectory (Join-Path $audit 'negative-launch')
if ($LASTEXITCODE -eq 0) { throw 'Missing CUDA path was not rejected' }
$results.Add([pscustomobject]@{ Phase = 'missing-cuda-path-rejected'; ExitCode = $LASTEXITCODE })
if ((Invoke-Setup 'upgrade' -AllowMissing) -ne 0) { throw 'Upgrade reinstall failed' }
& "$target\Verify-Installed.ps1" -InstallRoot $target
foreach ($entry in $baseline) {
    if (-not (Test-Path -LiteralPath $entry.Path) -or (Get-FileDigest $entry.Path) -ne $entry.SHA256) { throw "Existing data changed during install/upgrade: $($entry.Path)" }
}
$results.Add([pscustomobject]@{ Phase = 'data-preserved-after-upgrade'; ProtectedExistingFiles = $baseline.Count })
$config | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $audit 'installed-runtime-paths.json') -Encoding UTF8
$acl = Get-Acl -LiteralPath "$env:ProgramData\ElectrodeDet"
$acl.Sddl | Set-Content -LiteralPath (Join-Path $audit 'programdata-acl.txt') -Encoding UTF8
$process = Start-Process -FilePath "$target\unins000.exe" -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="' + (Join-Path $audit 'uninstall.log') + '"') -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw 'Uninstall failed' }
foreach ($entry in $baseline) {
    if (-not (Test-Path -LiteralPath $entry.Path) -or (Get-FileDigest $entry.Path) -ne $entry.SHA256) { throw "Existing data changed after uninstall: $($entry.Path)" }
}
if (Test-Path -LiteralPath "$target\App\电极帽缺陷检测.exe") { throw 'Installed application was not removed' }
if (Test-Path -LiteralPath $uninstallKey) { throw 'Uninstall registration was not removed' }
if ((Test-Path -LiteralPath $shortcut) -or (Test-Path -LiteralPath $desktopShortcut)) { throw 'Installed shortcuts were not removed' }
$results.Add([pscustomobject]@{ Phase = 'uninstall-data-preserved'; ExitCode = $process.ExitCode; ProtectedExistingFiles = $baseline.Count })
$results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $audit 'installer-tests.json') -Encoding UTF8
