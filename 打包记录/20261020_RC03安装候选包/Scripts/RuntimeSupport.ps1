$ErrorActionPreference = 'Stop'
function Get-FileDigest([string]$Path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
}
function Test-LockedRoot([string]$Path, [string]$Kind, $Lock) {
    if (-not $Path -or -not [IO.Path]::IsPathRooted($Path)) { return $false }
    try {
        if ($Kind -eq 'CudaBin') {
            $v = Get-Content -LiteralPath (Join-Path $Path 'version.json') -Raw | ConvertFrom-Json
            if ($v.cuda.version -ne $Lock.CudaToolkitVersion) { return $false }
            $base = Join-Path $Path 'bin'
        } else { $base = $Path }
        foreach ($entry in @($Lock.Files | Where-Object Root -eq $Kind)) {
            $file = Join-Path $base $entry.File
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { return $false }
            if ((Get-Item -LiteralPath $file).Length -ne [long]$entry.Bytes -or (Get-FileDigest $file) -ne $entry.SHA256) { return $false }
        }
        return $true
    } catch { return $false }
}
function Test-X64Pe([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = New-Object IO.BinaryReader($stream)
    try {
        $stream.Position = 0x3c; $pe = $reader.ReadInt32()
        $stream.Position = $pe
        return ($reader.ReadUInt32() -eq 0x4550 -and $reader.ReadUInt16() -eq 0x8664)
    } catch { return $false }
    finally { $reader.Dispose(); $stream.Dispose() }
}
function Find-GpuPaths($Lock, [string]$CudaRoot, [string]$CudnnBin) {
    $cudaCandidates = @($CudaRoot)
    $cudnnCandidates = @($CudnnBin)
    foreach ($scope in @('Process','Machine','User')) {
        $vars = [Environment]::GetEnvironmentVariables($scope)
        foreach ($key in $vars.Keys) {
            if ($key -match '^CUDA_PATH') { $cudaCandidates += [string]$vars[$key] }
            if ($key -match '^CUDNN') {
                $root = [string]$vars[$key]
                $cudnnCandidates += @($root, "$root\bin", "$root\bin\12.9", "$root\bin\12.8")
            }
        }
    }
    foreach ($base in @("$env:ProgramFiles\NVIDIA GPU Computing Toolkit\CUDA", "$env:ProgramFiles\NVIDIA\CUDNN")) {
        if (Test-Path -LiteralPath $base) {
            foreach ($dir in Get-ChildItem -LiteralPath $base -Directory) {
                if ($base.EndsWith('\CUDA')) { $cudaCandidates += $dir.FullName }
                else {
                    $cudnnCandidates += @($dir.FullName, "$($dir.FullName)\bin")
                    if (Test-Path -LiteralPath "$($dir.FullName)\bin") {
                        $cudnnCandidates += @(Get-ChildItem -LiteralPath "$($dir.FullName)\bin" -Directory | ForEach-Object FullName)
                    }
                }
            }
        }
    }
    foreach ($part in @($env:PATH -split ';' | Where-Object { $_ })) {
        $cudnnCandidates += $part
        if ((Split-Path $part -Leaf) -eq 'bin') { $cudaCandidates += Split-Path $part -Parent }
    }
    # An explicitly selected folder is authoritative: never hide a bad selection with a fallback.
    if ($CudaRoot) { $cudaCandidates = @($CudaRoot) }
    if ($CudnnBin) { $cudnnCandidates = @($CudnnBin) }
    $cudaFound = $null; $cudnnFound = $null
    foreach ($candidate in @($cudaCandidates | Where-Object { $_ } | Select-Object -Unique)) {
        if (Test-LockedRoot $candidate 'CudaBin' $Lock) { $cudaFound = [IO.Path]::GetFullPath($candidate); break }
    }
    foreach ($candidate in @($cudnnCandidates | Where-Object { $_ } | Select-Object -Unique)) {
        if (Test-LockedRoot $candidate 'CudnnBin' $Lock) { $cudnnFound = [IO.Path]::GetFullPath($candidate); break }
    }
    [pscustomobject]@{ CudaRoot = $cudaFound; CudnnBin = $cudnnFound }
}
function Get-EnvironmentFindings {
    $findings = New-Object 'System.Collections.Generic.List[object]'
    $release = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
    $findings.Add([pscustomobject]@{ Name = '.NET Framework >=4.7.2'; Passed = ($release -ge 461808); Evidence = "$release" })
    foreach ($name in @('msvcr100.dll','msvcp100.dll','atl100.dll','msvcr120.dll','msvcp120.dll','vcruntime140.dll','vcruntime140_1.dll','msvcp140.dll','vcomp140.dll','MFPlat.dll')) {
        $file = Join-Path "$env:SystemRoot\System32" $name
        $findings.Add([pscustomobject]@{ Name = $name; Passed = (Test-Path -LiteralPath $file); Evidence = $file })
    }
    $dirs = @($env:PATH -split ';' | Where-Object { $_ })
    $mvs = @($dirs | ForEach-Object { Join-Path $_ 'MvCameraControl.dll' } | Where-Object { Test-Path -LiteralPath $_ } | Where-Object {
        [Diagnostics.FileVersionInfo]::GetVersionInfo($_).FileVersion -eq '4.8.0.3' -and (Test-X64Pe $_)
    })
    $findings.Add([pscustomobject]@{ Name = 'MVS native 4.8.0.3 on PATH'; Passed = ($mvs.Count -gt 0); Evidence = ($mvs -join ';') })
    $gacRoot = "$env:SystemRoot\Microsoft.NET\assembly\GAC_64\Cognex.VisionPro.Core"
    $vp = @(if (Test-Path -LiteralPath $gacRoot) {
        Get-ChildItem -LiteralPath $gacRoot -Filter Cognex.VisionPro.Core.dll -Recurse -File | Where-Object {
            [Reflection.AssemblyName]::GetAssemblyName($_.FullName).Version.ToString() -eq '59.2.0.0'
        } | ForEach-Object FullName
    })
    $findings.Add([pscustomobject]@{ Name = 'VisionPro Core 59.2 registered GAC'; Passed = ($vp.Count -gt 0); Evidence = ($vp -join ';') })
    $halcon = @($dirs | ForEach-Object { Join-Path $_ 'halcon.dll' } | Where-Object { Test-Path -LiteralPath $_ } | Where-Object {
        [Diagnostics.FileVersionInfo]::GetVersionInfo($_).FileMajorPart -eq 12 -and (Test-X64Pe $_)
    })
    $findings.Add([pscustomobject]@{ Name = 'HALCON 12 native on PATH'; Passed = ($halcon.Count -gt 0); Evidence = ($halcon -join ';') })
    $findings.Add([pscustomobject]@{ Name = 'NVIDIA x64 driver nvcuda.dll'; Passed = (Test-Path -LiteralPath "$env:SystemRoot\System32\nvcuda.dll"); Evidence = "$env:SystemRoot\System32\nvcuda.dll" })
    return $findings.ToArray()
}
