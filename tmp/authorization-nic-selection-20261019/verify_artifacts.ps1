$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$buildRoot = Join-Path $taskRoot 'build'
$repoRoot = (Resolve-Path (Join-Path $taskRoot '..\..')).Path
$commonPath = Join-Path $buildRoot 'CommonCls.dll'
[void][Reflection.Assembly]::LoadFrom($commonPath)

# Read-only: never call DateVerify, Regist, WriteCID or WritePasswordFile.
$authPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'GearDet\authentication.txt'
$auth = New-Object CommonCls.Protect($authPath)
$cid = $auth.GetCID()
$ethernet = @([Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() | Where-Object {
    $_.NetworkInterfaceType -eq [Net.NetworkInformation.NetworkInterfaceType]::Ethernet
})
$firstMac = if ($ethernet.Count) { $ethernet[0].GetPhysicalAddress().ToString() } else { '' }
$bound = @($ethernet | Where-Object { $_.GetPhysicalAddress().ToString() -eq $cid })
if (!$auth.PasswordFileFound -or !$auth.PasswordFileValid -or [string]::IsNullOrEmpty($cid)) {
    throw 'This machine does not have the expected valid existing authorization file.'
}
if (!$bound.Count -or $auth.GetMacAddress() -ne $cid) {
    throw 'The new real CommonCls.dll did not select the existing bound Ethernet adapter.'
}
$realMachine = [ordered]@{
    AuthorizationFileValid = $auth.PasswordFileValid
    BoundAdapterPresent = ($bound.Count -gt 0)
    LegacyFirstAdapterMatchesCid = ($firstMac -eq $cid)
    NewSelectionMatchesCid = ($auth.GetMacAddress() -eq $cid)
    MatchingAdapterCount = $bound.Count
}

$unregistered = New-Object CommonCls.Protect((Join-Path $taskRoot 'nonexistent-authentication.txt'))
if ($unregistered.GetMacAddress() -ne $firstMac) { throw 'Unregistered selection changed.' }
$auth.GetType().GetField('mIsValid', [Reflection.BindingFlags]'Instance,NonPublic').SetValue($auth, $false)
if ($auth.GetMacAddress() -ne $firstMac) { throw 'Invalid authorization selection changed.' }
$auth.GetType().GetField('mIsValid', [Reflection.BindingFlags]'Instance,NonPublic').SetValue($auth, $true)
$auth.SetCID('NO_MATCHING_ADAPTER_FOR_REGRESSION_CHECK')
if ($auth.GetMacAddress() -ne $firstMac) { throw 'No-match fallback changed.' }

$mainFile = @(Get-ChildItem -LiteralPath $buildRoot -File -Filter '*.exe' | Where-Object {
    $_.BaseName -notmatch '^[A-Za-z]'
})
if ($mainFile.Count -ne 1) { throw 'Cannot identify the generated main executable.' }
$projectVersions = @{}
foreach ($file in @('CommonCls.dll', 'xbVP_Control.dll', 'PipeChanel.dll')) {
    $projectVersions[[IO.Path]::GetFileNameWithoutExtension($file)] =
        [Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildRoot $file)).Version.ToString()
}
$mainAssembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($mainFile[0].FullName)
foreach ($reference in $mainAssembly.GetReferencedAssemblies()) {
    if ($projectVersions.ContainsKey($reference.Name) -and $projectVersions[$reference.Name] -ne $reference.Version.ToString()) {
        throw ('Main executable reference mismatch: ' + $reference.Name)
    }
}
$controlAssembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $buildRoot 'xbVP_Control.dll'))
$controlCommon = @($controlAssembly.GetReferencedAssemblies() | Where-Object Name -eq 'CommonCls')
if ($controlCommon.Count -ne 1 -or $controlCommon[0].Version.ToString() -ne $projectVersions.CommonCls) {
    throw 'VisionControl CommonCls reference mismatch.'
}

$expected = @{
    'System.Memory' = '4.0.1.2'
    'System.Buffers' = '4.0.3.0'
    'System.Numerics.Vectors' = '4.1.4.0'
    'System.Runtime.CompilerServices.Unsafe' = '6.0.0.0'
}
$dependencyChecks = foreach ($name in $expected.Keys) {
    $publicPath = Join-Path $repoRoot ('bin\' + $name + '.dll')
    $generatedPath = Join-Path $buildRoot ($name + '.dll')
    $publicVersion = [Reflection.AssemblyName]::GetAssemblyName($publicPath).Version.ToString()
    $generatedVersion = [Reflection.AssemblyName]::GetAssemblyName($generatedPath).Version.ToString()
    if ($publicVersion -ne $expected[$name] -or $generatedVersion -ne $expected[$name]) {
        throw ('Dependency version mismatch: ' + $name)
    }
    [pscustomobject]@{Name=$name; PublicVersion=$publicVersion; GeneratedVersion=$generatedVersion}
}
foreach ($configPath in @(
    ($mainFile[0].FullName + '.config'),
    (Join-Path $repoRoot ('bin\' + $mainFile[0].Name + '.config'))
)) {
    $config = [xml](Get-Content -LiteralPath $configPath -Raw)
    foreach ($name in $expected.Keys) {
        $node = @($config.SelectNodes("//*[local-name()='dependentAssembly']") | Where-Object {
            $_.assemblyIdentity.name -eq $name
        })
        if ($node.Count -ne 1 -or $node[0].bindingRedirect.newVersion -ne $expected[$name]) {
            throw ('Binding redirect mismatch: ' + $configPath + ' / ' + $name)
        }
    }
}

$log = [IO.File]::ReadAllText((Join-Path $taskRoot 'solution-build.log'), [Text.Encoding]::GetEncoding(936))
$compileCalls = @($log -split "`n" | Where-Object { $_ -match 'csc.exe ' -and $_ -match '/out:' })
$compiledOutputs = @($compileCalls | ForEach-Object {
    $m = [regex]::Match($_, '/out:(?:"([^"]+)"|([^\s]+))')
    $m.Groups[1].Value + $m.Groups[2].Value
} | Sort-Object -Unique)
if ($compiledOutputs.Count -ne 11) { throw 'Not all 11 actual solution projects compiled.' }
foreach ($call in $compileCalls) {
    foreach ($m in [regex]::Matches($call, '/reference:(?:"([^"]+)"|([^\s]+))')) {
        $path = $m.Groups[1].Value + $m.Groups[2].Value
        if ([IO.Path]::GetFileName($path) -in @('CommonCls.dll','xbVP_Control.dll','PipeChanel.dll')) {
            if (!$path.StartsWith($buildRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw ('Compiler used an old project dependency: ' + $path)
            }
        }
    }
}
if ($log -match '(?im): error (?:MSB|CS)\d+') { throw 'Full build contains an error.' }

$before = Get-Content -LiteralPath (Join-Path $taskRoot 'preserved-before.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $before.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath $entry.Name -Algorithm SHA256).Hash -ne $entry.Value) {
        throw ('Preserved file changed: ' + $entry.Name)
    }
}
$binPrefix = (Join-Path $repoRoot 'bin') + '\'
$binBefore = @($before.PSObject.Properties | Where-Object { $_.Name.StartsWith($binPrefix) }).Count
$binAfter = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'bin') -File -Recurse -Force).Count
if ($binBefore -ne $binAfter) { throw 'Public bin file count changed.' }

$report = [ordered]@{
    Solution = 'VisionBasedLaser.sln'
    Configuration = 'Debug/x64'
    CompiledProjectCount = $compiledOutputs.Count
    ProjectVersions = $projectVersions
    CompilerReferencesFromThisBuild = $true
    Dependencies = @($dependencyChecks)
    BindingRedirectsMatch = $true
    RealMachine = $realMachine
    UnregisteredInvalidAndNoMatchFallbacksVerified = $true
    AuthorizationFileUnchanged = $true
    PublicBinFilesUnchanged = $binBefore
    PreservedFiles = @($before.PSObject.Properties).Count
    MainApplicationStarted = $false
    DeployedToPublicBin = $false
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'artifact-verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
