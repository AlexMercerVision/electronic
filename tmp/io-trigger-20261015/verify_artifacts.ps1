$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$buildRoot = Join-Path $taskRoot 'build'
$expected = @{'System.Memory.dll'='4.0.1.2';'System.Buffers.dll'='4.0.3.0';'System.Numerics.Vectors.dll'='4.1.4.0';'System.Runtime.CompilerServices.Unsafe.dll'='6.0.0.0'}
$assemblies = foreach ($file in @('CommonCls.dll','xbVP_Control.dll','激光除胶.exe')) {
    $assembly = [Reflection.Assembly]::LoadFile((Join-Path $buildRoot $file))
    [pscustomobject]@{File=$file;Name=$assembly.GetName().Name;Version=$assembly.GetName().Version.ToString();References=@($assembly.GetReferencedAssemblies() | Where-Object {$_.Name -in @('CommonCls','xbVP_Control')} | ForEach-Object {[pscustomobject]@{Name=$_.Name;Version=$_.Version.ToString()}})}
}
foreach ($assembly in $assemblies) {
    foreach ($reference in $assembly.References) {
        $target = $assemblies | Where-Object Name -eq $reference.Name
        if ($reference.Version -ne $target.Version) {throw "Project dependency mismatch: $($assembly.Name) -> $($reference.Name)"}
    }
}
$bcl = foreach ($file in $expected.Keys) {
    $publicPath = Join-Path 'E:\jieyawenjian\VB_Det\bin' $file
    $buildVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildRoot $file)).Version.ToString()
    $publicVersion = [Reflection.AssemblyName]::GetAssemblyName($publicPath).Version.ToString()
    if ($buildVersion -ne $expected[$file] -or $publicVersion -ne $expected[$file]) {throw "Dependency mismatch: $file"}
    [pscustomobject]@{File=$file;BuildVersion=$buildVersion;PublicBinVersion=$publicVersion}
}
foreach ($directory in @($buildRoot,'E:\jieyawenjian\VB_Det\bin')) {
    $config = [xml](Get-Content -LiteralPath (Join-Path $directory '激光除胶.exe.config') -Raw)
    foreach ($file in $expected.Keys) {
        $name = [IO.Path]::GetFileNameWithoutExtension($file)
        $node = $config.SelectNodes("//*[local-name()='dependentAssembly']") | Where-Object {$_.assemblyIdentity.name -eq $name}
        if ($node.bindingRedirect.newVersion -ne $expected[$file]) {throw "Binding redirect mismatch: $directory $name"}
    }
}
$before = Get-Content -LiteralPath (Join-Path $taskRoot 'preserved-before.json') -Raw | ConvertFrom-Json
foreach ($entry in $before.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath $entry.Name -Algorithm SHA256).Hash -ne $entry.Value) {throw "Preserved file changed: $($entry.Name)"}
}
$publicBefore = @($before.PSObject.Properties | Where-Object {$_.Name.StartsWith('E:\jieyawenjian\VB_Det\bin\')}).Count
$publicAfter = @(Get-ChildItem -LiteralPath 'E:\jieyawenjian\VB_Det\bin' -File -Recurse -Force).Count
if ($publicBefore -ne $publicAfter) {throw 'Public bin file count changed'}
$logPath = Join-Path $taskRoot 'solution-build-final.log'
$log = [IO.File]::ReadAllText($logPath,[Text.Encoding]::GetEncoding(936))
if ($log -notmatch '0 个错误') {throw 'Missing successful full-build summary'}
if ($log -match '(?i)warning MSB(3277|3245|3258)|error [A-Z]+\d+') {throw 'Build contains assembly resolution or compilation errors'}
$compilerCalls = @($log -split "`n" | Where-Object {$_ -match 'csc.exe ' -and $_ -match '/out:'})
$compiled = foreach ($call in $compilerCalls) {
    $outputMatch = [regex]::Match($call,'/out:(?:"([^"]+)"|([^\s]+))')
    $references = foreach ($referenceMatch in [regex]::Matches($call,'/reference:(?:"([^"]+)"|([^\s]+))')) {
        $reference = if ($referenceMatch.Groups[1].Success) {$referenceMatch.Groups[1].Value} else {$referenceMatch.Groups[2].Value}
        if ([IO.Path]::GetFileName($reference) -in @('CommonCls.dll','xbVP_Control.dll')) {
            if (-not $reference.StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase)) {throw "Old dependency used by compiler: $reference"}
            $reference
        }
    }
    [pscustomobject]@{Output=($outputMatch.Groups[1].Value+$outputMatch.Groups[2].Value);ProjectReferences=@($references)}
}
if (@($compiled.Output | Sort-Object -Unique).Count -ne 11) {throw "Expected all 11 projects to compile, got $($compiled.Count)"}
$report = [pscustomobject]@{Solution='VisionBasedLaser.sln';Configuration='Debug/x64';CompiledProjects=$compiled;Assemblies=$assemblies;Dependencies=$bcl;BindingRedirectsMatch=$true;PublicBinFiles=$publicBefore;PublicBinChanged=0;PublicBinAdded=0;PreservedFiles=@($before.PSObject.Properties).Count}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'artifact-verification.json') -Encoding UTF8
$assemblies | Format-Table File,Version
$bcl | Format-Table
"All 11 projects compiled; references match this build; $publicBefore public bin files unchanged."
$log -split "`n" | Select-Object -Last 6
