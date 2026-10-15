$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$buildRoot = Join-Path $taskRoot 'build'
$applicationName = '电极帽缺陷检测'
$assemblies = foreach ($file in @('CommonCls.dll','xbVP_Control.dll',($applicationName+'.exe'))) {
    $assembly = [Reflection.Assembly]::LoadFile((Join-Path $buildRoot $file))
    [pscustomobject]@{File=$file;Name=$assembly.GetName().Name;Version=$assembly.GetName().Version.ToString();References=@($assembly.GetReferencedAssemblies() | Where-Object {$_.Name -in @('CommonCls','xbVP_Control')} | ForEach-Object {[pscustomobject]@{Name=$_.Name;Version=$_.Version.ToString()}})}
}
foreach ($assembly in $assemblies) {
    foreach ($reference in $assembly.References) {
        $target = $assemblies | Where-Object Name -eq $reference.Name
        if ($reference.Version -ne $target.Version) {throw "Project dependency mismatch: $($assembly.Name)"}
    }
}
$info = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $buildRoot ($applicationName+'.exe')))
if ($info.FileDescription -ne $applicationName -or $info.ProductName -ne $applicationName) {throw 'File metadata does not match application name'}
$deployment = [xml](Get-Content -LiteralPath (Join-Path $buildRoot ($applicationName+'.application')) -Raw)
if ($deployment.assembly.description.product -ne $applicationName) {throw 'ClickOnce product name mismatch'}
if ($deployment.assembly.dependency.dependentAssembly.codebase -ne ($applicationName+'.exe.manifest')) {throw 'Deployment manifest filename mismatch'}
$manifest = [xml](Get-Content -LiteralPath (Join-Path $buildRoot ($applicationName+'.exe.manifest')) -Raw)
if ($manifest.assembly.entryPoint.commandLine.file -ne ($applicationName+'.exe')) {throw 'Executable manifest filename mismatch'}
$expected = @{'System.Memory.dll'='4.0.1.2';'System.Buffers.dll'='4.0.3.0';'System.Numerics.Vectors.dll'='4.1.4.0';'System.Runtime.CompilerServices.Unsafe.dll'='6.0.0.0'}
$dependencies = foreach ($file in $expected.Keys) {
    $buildVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildRoot $file)).Version.ToString()
    $publicVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path 'E:\jieyawenjian\VB_Det\bin' $file)).Version.ToString()
    if ($buildVersion -ne $expected[$file] -or $publicVersion -ne $expected[$file]) {throw "Dependency version mismatch: $file"}
    [pscustomobject]@{File=$file;BuildVersion=$buildVersion;PublicBinVersion=$publicVersion}
}
foreach ($configPath in @((Join-Path $buildRoot ($applicationName+'.exe.config')),'E:\jieyawenjian\VB_Det\bin\电极帽缺陷检测.exe.config')) {
    $config = [xml](Get-Content -LiteralPath $configPath -Raw)
    foreach ($file in $expected.Keys) {
        $name = [IO.Path]::GetFileNameWithoutExtension($file)
        $node = $config.SelectNodes("//*[local-name()='dependentAssembly']") | Where-Object {$_.assemblyIdentity.name -eq $name}
        if ($node.bindingRedirect.newVersion -ne $expected[$file]) {throw "Binding redirect mismatch: $configPath $name"}
    }
}
$before = Get-Content -LiteralPath (Join-Path $taskRoot 'preserved-before.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $before.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath $entry.Name -Algorithm SHA256).Hash -ne $entry.Value) {throw "Preserved file changed: $($entry.Name)"}
}
$publicBefore = @($before.PSObject.Properties | Where-Object {$_.Name.StartsWith('E:\jieyawenjian\VB_Det\bin\')}).Count
$publicAfter = @(Get-ChildItem -LiteralPath 'E:\jieyawenjian\VB_Det\bin' -File -Recurse -Force).Count
if ($publicBefore -ne $publicAfter) {throw 'Public bin file count changed'}
$log = [IO.File]::ReadAllText((Join-Path $taskRoot 'solution-build.log'),[Text.Encoding]::GetEncoding(936))
if ($log -notmatch '0 个错误' -or $log -match '(?i)warning MSB(3277|3245|3258)|error [A-Z]+\d+') {throw 'Full build failed or contains assembly resolution errors'}
$compiled = foreach ($call in @($log -split "`n" | Where-Object {$_ -match 'csc.exe ' -and $_ -match '/out:'})) {
    $outputMatch = [regex]::Match($call,'/out:(?:"([^"]+)"|([^\s]+))')
    $references = foreach ($referenceMatch in [regex]::Matches($call,'/reference:(?:"([^"]+)"|([^\s]+))')) {
        $reference = $referenceMatch.Groups[1].Value+$referenceMatch.Groups[2].Value
        if ([IO.Path]::GetFileName($reference) -in @('CommonCls.dll','xbVP_Control.dll')) {
            if (-not $reference.StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase)) {throw "Old dependency used by compiler: $reference"}
            $reference
        }
    }
    [pscustomobject]@{Output=($outputMatch.Groups[1].Value+$outputMatch.Groups[2].Value);ProjectReferences=@($references)}
}
if (@($compiled.Output | Sort-Object -Unique).Count -ne 11) {throw 'Not all 11 solution projects compiled'}
$report = [pscustomobject]@{ApplicationName=$applicationName;Description=$info.FileDescription;ProductName=$info.ProductName;DeploymentNameMatches=$true;Solution='VisionBasedLaser.sln';Configuration='Debug/x64';CompiledProjects=$compiled;Assemblies=$assemblies;Dependencies=$dependencies;BindingRedirectsMatch=$true;PublicBinFiles=$publicBefore;PublicBinChanged=0;PublicBinAdded=0;PreservedFiles=@($before.PSObject.Properties).Count}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'artifact-verification.json') -Encoding UTF8
$assemblies | Format-Table File,Version
$dependencies | Format-Table
"File properties and generated manifests use $applicationName; all 11 projects compiled with matching references; $publicBefore public bin files unchanged."
$log -split "`n" | Select-Object -Last 6
