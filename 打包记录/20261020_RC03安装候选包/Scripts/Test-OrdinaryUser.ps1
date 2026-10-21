param([string]$InstallRoot, [string]$ReportFile)
$ErrorActionPreference = 'Stop'
try {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Token still has Administrators membership enabled' }
    $probe = Join-Path $InstallRoot 'ordinary-user-write-test.tmp'
    $writeDenied = $false
    try { [IO.File]::WriteAllText($probe, 'permission probe') }
    catch [UnauthorizedAccessException] { $writeDenied = $true }
    if (-not $writeDenied) { throw 'Ordinary user can write application installation directory' }
    $ps = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    & $ps -NoProfile -ExecutionPolicy Bypass -File "$InstallRoot\Launch.ps1" -CheckOnly
    if ($LASTEXITCODE -ne 0) { throw 'Restricted launcher preflight failed' }
    & $ps -NoProfile -ExecutionPolicy Bypass -File "$InstallRoot\Launch.ps1" -VerifyGpu
    if ($LASTEXITCODE -ne 0) { throw 'Restricted GPU probe failed' }
    [ordered]@{ Account=$identity.Name; AdministratorsEnabled=$false; InstallDirectoryWriteDenied=$writeDenied; LauncherPreflight='Passed'; GpuProbe='Passed'; Boundary='Restricted current-user token, medium integrity; not a separate real user account or full application acceptance.' } |
        ConvertTo-Json | Set-Content -LiteralPath $ReportFile -Encoding UTF8
    exit 0
} catch {
    $_.Exception.ToString() | Set-Content -LiteralPath $ReportFile -Encoding UTF8
    exit 1
}
