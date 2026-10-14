$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$projectRoot=Split-Path (Split-Path $taskRoot -Parent) -Parent
$buildRoot=Join-Path $taskRoot 'build'
$identities=@{}
$records=foreach($file in @('CommonCls.dll','xbVP_Control.dll','激光除胶.exe')) {
    $path=Join-Path $buildRoot $file
    $assembly=[Reflection.Assembly]::LoadFile($path)
    $identity=$assembly.GetName()
    $identities[$identity.Name]=$identity.Version.ToString()
    [pscustomobject]@{File=$file;Identity=$identity.FullName;References=@($assembly.GetReferencedAssemblies() | Where-Object {$_.Name -in @('CommonCls','xbVP_Control')} | ForEach-Object {[pscustomobject]@{Name=$_.Name;Version=$_.Version.ToString()}})}
}
foreach($record in $records) {
    foreach($reference in $record.References) {
        if($reference.Version -ne $identities[$reference.Name]) {throw "Reference mismatch: $($record.File) -> $($reference.Name)"}
    }
}
$expected=@{'System.Memory.dll'='4.0.1.2';'System.Buffers.dll'='4.0.3.0';'System.Numerics.Vectors.dll'='4.1.4.0';'System.Runtime.CompilerServices.Unsafe.dll'='6.0.0.0'}
$bcl=foreach($file in $expected.Keys) {
    $version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildRoot $file)).Version.ToString()
    if($version -ne $expected[$file]) {throw "$file version=$version"}
    [pscustomobject]@{File=$file;Version=$version}
}
$configuration=[xml](Get-Content -LiteralPath (Join-Path $buildRoot '激光除胶.exe.config') -Raw)
$redirects=@($configuration.SelectNodes("//*[local-name()='dependentAssembly']"))
foreach($file in $expected.Keys) {
    $assemblyName=[IO.Path]::GetFileNameWithoutExtension($file)
    $node=$redirects | Where-Object {$_.assemblyIdentity.name -eq $assemblyName}
    if($node.bindingRedirect.newVersion -ne $expected[$file]) {throw "Redirect mismatch: $assemblyName"}
}
$before=Get-Content -LiteralPath (Join-Path $taskRoot 'preserved-before.json') -Raw | ConvertFrom-Json
foreach($entry in $before.PSObject.Properties) {
    $path=Join-Path $projectRoot $entry.Name
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value) {throw "Preserved file changed: $path"}
}
$binCount=@($before.PSObject.Properties | Where-Object {$_.Name.StartsWith('bin\')}).Count
$currentBin=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'bin') -File -Recurse -Force).Count
if($binCount -ne $currentBin) {throw "Public bin file count changed"}
$report=[pscustomobject]@{Assemblies=$records;BclVersions=$bcl;ReferenceVersionsMatch=$true;BindingRedirectsMatch=$true;PreservedFiles=@($before.PSObject.Properties).Count;PublicBinFiles=$binCount;PublicBinChanged=0;PublicBinAdded=0}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'artifact-verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
