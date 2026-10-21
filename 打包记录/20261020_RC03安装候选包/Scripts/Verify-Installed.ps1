param([string]$InstallRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')
. (Join-Path $root 'Prerequisites\RuntimeSupport.ps1')
$manifestPath = Join-Path $root 'payload-manifest.csv'
$digest = ([IO.File]::ReadAllText((Join-Path $root 'payload-manifest.sha256')).Trim() -split '\s+')[0]
if ((Get-FileDigest $manifestPath) -ne $digest) { throw 'Payload manifest changed' }
$entries = @(Import-Csv -LiteralPath $manifestPath -Encoding UTF8)
$expected = @{}
foreach ($entry in $entries) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $entry.RelativePath))
    if (-not $path.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escapes install root' }
    if ($expected.ContainsKey($path)) { throw 'Duplicate manifest path' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing: $path" }
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.Bytes -or (Get-FileDigest $path) -ne $entry.SHA256) { throw "Changed: $path" }
    $expected[$path] = $true
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'App') -File -Recurse) {
    if (-not $expected.ContainsKey($file.FullName)) { throw "Unexpected application file: $($file.FullName)" }
}
Write-Host "PASS: $($entries.Count) installed static files match SHA256. RuntimePaths.json is generated per machine."
