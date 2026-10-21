$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
function File-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
$manifestPath = Join-Path $root 'release-manifest.csv'
$digestPath = Join-Path $root 'release-manifest.sha256'
$digest = ([IO.File]::ReadAllText($digestPath).Trim() -split '\s+')[0]
if ((File-Sha256 $manifestPath) -ne $digest) { throw 'Manifest changed' }
$manifest = @(Import-Csv -LiteralPath $manifestPath -Encoding UTF8)
$expected = @{}
foreach ($entry in $manifest) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $entry.RelativePath))
    if (-not $path.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid manifest path' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing: $($entry.RelativePath)" }
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.Bytes -or (File-Sha256 $path) -ne $entry.SHA256) { throw "Changed: $($entry.RelativePath)" }
    if ($expected.ContainsKey($path)) { throw "Duplicate: $($entry.RelativePath)" }
    $expected[$path] = $true
}
foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse) {
    if ($file.FullName -eq $manifestPath -or $file.FullName -eq $digestPath) { continue }
    if (-not $expected.ContainsKey($file.FullName)) { throw "Unexpected: $($file.FullName)" }
}
Write-Host "PASS: $($manifest.Count) release files, SHA256 match."
