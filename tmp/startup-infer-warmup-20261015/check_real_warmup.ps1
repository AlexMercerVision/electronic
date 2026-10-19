$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$repoRoot = 'E:\jieyawenjian\VB_Det'
$buildRoot = Join-Path $taskRoot 'build'
$runtimeRoot = Join-Path $taskRoot 'runtime-check'
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
Set-Location -LiteralPath $runtimeRoot
[Environment]::CurrentDirectory = $runtimeRoot
$publicBin = Join-Path $repoRoot 'bin'
$env:PATH = $publicBin + ';' + $env:PATH
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class WarmupNativePath {
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    public static extern bool SetDllDirectory(string path);
}
'@
if (-not [WarmupNativePath]::SetDllDirectory($publicBin)) { throw 'SetDllDirectory failed' }
[Reflection.Assembly]::LoadFrom((Join-Path $buildRoot 'CommonCls.dll')) | Out-Null
$cdl = New-Object CommonCls.CDL
if ($cdl.det_infer_img(32, 32)) { throw 'Uninitialized model was accepted' }
$config = Get-Content -LiteralPath (Join-Path $publicBin 'UserData\serUserParam.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$dlParam = New-Object CommonCls.DLParam
$dlParam.modelPath = $config.p_DL.modelPath
$dlParam.modelType = $config.p_DL.modelType
$dlParam.bUseGPU = $config.p_DL.bUseGPU
$dlParam.gpuID = $config.p_DL.gpuID
try {
    $initRet = $cdl.InitDL($dlParam)
    if ($initRet -ne 0) { throw "Real InitDL failed: $initRet" }
    if ($cdl.det_infer_img(0, 32)) { throw 'Zero width was accepted' }
    if ($cdl.det_infer_img([int]::MaxValue, [int]::MaxValue)) { throw 'Overflow dimensions were accepted' }
    $cameraIndex = [int]$config.p_CD.camInd[0]
    $width = [int]$config.p_Cam[$cameraIndex].iWidth
    $height = [int]$config.p_Cam[$cameraIndex].iHeight
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $first = $cdl.det_infer_img($width, $height)
    $firstMs = $watch.ElapsedMilliseconds
    if (-not $first) { throw 'Real first warmup failed' }
    $watch.Restart()
    $second = $cdl.det_infer_img($width, $height)
    $secondMs = $watch.ElapsedMilliseconds
    if (-not $second) { throw 'Real repeat inference failed' }
    [pscustomobject]@{
        Assembly = (Join-Path $buildRoot 'CommonCls.dll')
        NativeLibrary = (Join-Path $publicBin 'model_infer.dll')
        ModelPath = $dlParam.modelPath
        UseGPU = $dlParam.bUseGPU
        GPUId = $dlParam.gpuID
        Width = $width
        Height = $height
        FirstSuccess = $first
        FirstMs = $firstMs
        RepeatSuccess = $second
        RepeatMs = $secondMs
        UninitializedRejected = $true
        ZeroSizeRejected = $true
        OverflowRejected = $true
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'real-warmup-result.json') -Encoding UTF8
    Get-Content -LiteralPath (Join-Path $taskRoot 'real-warmup-result.json') -Encoding UTF8
}
finally {
    $cdl.CloseDL()
}
