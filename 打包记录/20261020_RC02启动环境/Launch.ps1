[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$VerifyGpu,
    [string]$RecordDirectory,
    [string]$RuntimePathsFile
)
$ErrorActionPreference = 'Stop'
$release = $PSScriptRoot
$app = Join-Path $release 'App'
$recordFile = $null

function Full-Path([string]$Path, [string]$Base) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw '路径不能为空。' }
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $Base $Path))
}
function Save-Record($Value) {
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $script:recordFile -Encoding UTF8
}
function File-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
function Locked-Path($Entry, $Roots) {
    if (-not $Roots.ContainsKey([string]$Entry.Root)) { throw "未知依赖目录：$($Entry.Root)" }
    $base = [IO.Path]::GetFullPath($Roots[[string]$Entry.Root]).TrimEnd('\')
    $path = [IO.Path]::GetFullPath((Join-Path $base $Entry.File))
    if (-not $path.StartsWith($base + '\', [StringComparison]::OrdinalIgnoreCase)) { throw '依赖清单路径越界。' }
    return $path
}
function Quote-Argument([string]$Value) {
    if ($Value.Contains('"')) { throw '参数路径包含不支持的引号。' }
    return '"' + $Value.TrimEnd('\') + '"'
}

try {
    if (-not $RuntimePathsFile) { $RuntimePathsFile = Join-Path $release 'RuntimePaths.json' }
    $settings = Get-Content -LiteralPath $RuntimePathsFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $lock = Get-Content -LiteralPath (Join-Path $release 'Prerequisites\GPU50-runtime-lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($lock.Batch -ne '20261020_50Series_RC02') { throw '启动入口与依赖清单批次不一致。' }
    $cuda = Full-Path $settings.CudaRoot $release
    $cudnn = Full-Path $settings.CudnnBin $release
    $roots = @{ CudaBin = (Join-Path $cuda 'bin'); CudnnBin = $cudnn; App = $app; Diagnostics = (Join-Path $release 'Diagnostics') }
    $version = Get-Content -LiteralPath (Join-Path $cuda 'version.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($version.cuda.version -ne $lock.CudaToolkitVersion) { throw "需要CUDA $($lock.CudaToolkitVersion)，当前目录为 $($version.cuda.version)：$cuda" }
    $top = Join-Path $cudnn 'cudnn64_9.dll'
    $actualVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($top).FileVersion
    if ($actualVersion -ne $lock.CudnnVersion) { throw "需要cuDNN $($lock.CudnnVersion) CUDA12版，当前为 $actualVersion：$cudnn" }
    Write-Host '正在检查运行环境，请稍候...'
    $checked = @()
    foreach ($entry in $lock.Files) {
        $path = Locked-Path $entry $roots
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "缺少依赖：$path" }
        if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.Bytes -or (File-Sha256 $path) -ne $entry.SHA256) {
            throw "依赖与RC02锁定内容不同：$path。请使用完整的CUDA12.8/cuDNN9.14 CUDA12版组件。"
        }
        $checked += [pscustomobject]@{ Root = $entry.Root; File = $entry.File; Path = $path; SHA256 = $entry.SHA256 }
    }
    # App-local GPU DLLs have higher loader priority than PATH. Reject shadow copies.
    $shadow = @(Get-ChildItem -LiteralPath $app -File | Where-Object { $_.Name -match '^(cudnn|cublas|cudart|cufft|cusolver|cusparse|nvrtc|nvJitLink).*\.dll$' })
    if ($shadow.Count) { throw ('App中存在会覆盖外部GPU环境的DLL，请恢复受控目录：' + ($shadow.Name -join ', ')) }
    $parts = @($roots.CudnnBin, $roots.CudaBin) + @($env:PATH -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $seen = @{}
    $pathParts = @(foreach ($part in $parts) {
        $key = $part.Trim().TrimEnd('\')
        if (-not $seen.ContainsKey($key)) { $seen[$key] = $true; $part }
    })
    $processPath = $pathParts -join ';'
    if (-not $RecordDirectory) { $RecordDirectory = Full-Path $settings.LogDirectory $release }
    $RecordDirectory = Full-Path $RecordDirectory $release
    [IO.Directory]::CreateDirectory($RecordDirectory) | Out-Null
    $stamp = (Get-Date -Format 'yyyyMMdd_HHmmss_fff') + '_' + $PID
    $recordFile = Join-Path $RecordDirectory ('launch_' + $stamp + '.json')
    $record = [ordered]@{
        Batch = $lock.Batch; Time = (Get-Date).ToString('o'); Mode = 'MainApplication';
        AppDirectory = $app; CudaRoot = $cuda; CudnnBin = $cudnn;
        CudaToolkitVersion = $version.cuda.version; CudnnVersion = $actualVersion;
        WorkingDirectory = $app; PathPrefix = @($roots.CudnnBin, $roots.CudaBin);
        ChangesSystemEnvironment = $false; CheckedDependencies = $checked; Status = 'PreflightPassed'
    }
    if ($CheckOnly) {
        $record.Mode = 'CheckOnly'; Save-Record $record
        Write-Host "PASS: CUDA12.8 / cuDNN9.14 CUDA12版依赖校验通过。记录：$recordFile"
        exit 0
    }
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.UseShellExecute = $false
    $start.WorkingDirectory = $app
    $start.EnvironmentVariables['PATH'] = $processPath
    $start.EnvironmentVariables['CUDA_PATH'] = $cuda
    $start.EnvironmentVariables['CUDA_PATH_V12_8'] = $cuda
    if ($VerifyGpu) {
        $record.Mode = 'VerifyGpu'
        $modulesFile = Join-Path $RecordDirectory ('gpu_' + $stamp + '_modules.txt')
        $logFile = Join-Path $RecordDirectory ('gpu_' + $stamp + '.log')
        $cache = Join-Path $RecordDirectory 'CudaCache'
        [IO.Directory]::CreateDirectory($cache) | Out-Null
        $start.EnvironmentVariables['CUDA_CACHE_PATH'] = $cache
        $start.FileName = Join-Path $roots.Diagnostics 'GpuProbe.exe'
        $start.Arguments = (Quote-Argument $app) + ' ' + (Quote-Argument $modulesFile)
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $record.ProbeLog = $logFile; $record.ModulesFile = $modulesFile
        Save-Record $record
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $start
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'GPU独立验证超过60秒，已结束该诊断进程。' }
        $text = $stdout.Result + [Environment]::NewLine + $stderr.Result
        [IO.File]::WriteAllText($logFile, $text, (New-Object Text.UTF8Encoding($true)))
        $record.ExitCode = $process.ExitCode
        if ($process.ExitCode -ne 0 -or $text -notmatch 'GPU0 ret=0' -or $text -match 'compiled with CUDNN|incompatible bug') { throw "GPU验证失败或存在cuDNN版本警告，请查看：$logFile" }
        if ($text -notmatch 'Runtime API Version: 12\.8') { throw '实际CUDA Runtime API并非12.8。' }
        # Plain .NET strings avoid serializing PowerShell 5.1 Get-Content provider metadata.
        $loaded = @([IO.File]::ReadAllLines($modulesFile, [Text.Encoding]::UTF8))
        $gpuLoaded = @($loaded | Where-Object { [IO.Path]::GetFileName($_) -match '^(cudnn|cublas).*\.dll$' })
        if (@($gpuLoaded | Where-Object { [IO.Path]::GetFileName($_) -eq 'cudnn64_9.dll' }).Count -ne 1) { throw '未发现实际加载的cuDNN入口。' }
        foreach ($module in $gpuLoaded) {
            $name = [IO.Path]::GetFileName($module)
            $expected = @($checked | Where-Object { $_.File -eq $name -and $_.Root -in @('CudaBin','CudnnBin') })
            if ($expected.Count -ne 1 -or -not $module.Equals($expected[0].Path, [StringComparison]::OrdinalIgnoreCase)) { throw "GPU组件加载路径不符合锁定清单：$module" }
        }
        foreach ($name in @('model_infer.dll','paddle_inference_c.dll','phi.dll','common.dll','mkldnn.dll','mklml.dll','libiomp5md.dll')) {
            $found = @($loaded | Where-Object { [IO.Path]::GetFileName($_) -eq $name })
            if ($found.Count -ne 1 -or -not $found[0].Equals((Join-Path $app $name), [StringComparison]::OrdinalIgnoreCase)) { throw "原生推理库未从本批App加载：$name" }
        }
        $record.ActualGpuModules = $gpuLoaded
        $record.RuntimeApi = '12.8'; $record.CudnnMismatchWarning = $false; $record.NativeInference = 'ret=0'; $record.Status = 'GpuVerificationPassed'
        Save-Record $record
        Write-Host "PASS: 实际CUDA12.8、cuDNN9.14组件路径一致，推理成功且无版本不匹配警告。记录：$recordFile"
        exit 0
    }
    $start.FileName = Join-Path $app '电极帽缺陷检测.exe'
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Normal
    $process = [Diagnostics.Process]::Start($start)
    $record.ProcessId = $process.Id
    $record.Status = 'MainProcessCreated'
    $record.ValidationBoundary = '已创建主进程；实际模型加载、相机/串口、检测结果由主程序日志和现场测试确认。'
    Save-Record $record
    Write-Host "软件已启动。启动环境记录：$recordFile"
    exit 0
} catch {
    if ($recordFile -and $record) {
        $record.Status = 'Failed'; $record.Error = $_.Exception.Message
        try { Save-Record $record } catch { }
    }
    [Console]::Error.WriteLine('RC02启动/检查失败：' + $_.Exception.Message)
    exit 1
}
