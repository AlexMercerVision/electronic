$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$buildRoot=Join-Path $taskRoot 'build'
$identities=@{}
$records=foreach($assemblyFile in @('CommonCls.dll','xbVP_Control.dll','激光除胶.exe')) {
    $path=Join-Path $buildRoot $assemblyFile
    $assembly=[Reflection.Assembly]::LoadFile($path)
    $identity=$assembly.GetName()
    $identities[$identity.Name]=$identity.Version.ToString()
    [pscustomobject]@{File=$assemblyFile;Identity=$identity.FullName;Hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;References=@($assembly.GetReferencedAssemblies() | Where-Object {$_.Name -in @('CommonCls','xbVP_Control')} | ForEach-Object {[pscustomobject]@{Name=$_.Name;Version=$_.Version.ToString()}})}
}
foreach($record in $records) {
    foreach($reference in $record.References) {
        if($reference.Version -ne $identities[$reference.Name]) { throw "Reference version mismatch: $($record.File) -> $($reference.Name)" }
    }
}
$bcl=foreach($file in @('System.Memory.dll','System.Buffers.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')) {
    [pscustomobject]@{File=$file;Version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildRoot $file)).Version.ToString()}
}
$runtimeBefore=Get-Content -LiteralPath (Join-Path $taskRoot 'runtime-before.json') -Raw | ConvertFrom-Json
$changed=@($runtimeBefore | Where-Object {!(Test-Path -LiteralPath $_.Path) -or (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -ne $_.Hash})
$knownPaths=@($runtimeBefore | ForEach-Object {$_.Path})
$runtimeRoot=Join-Path (Split-Path (Split-Path $taskRoot -Parent) -Parent) 'bin'
$added=@(Get-ChildItem -LiteralPath $runtimeRoot -File -Recurse | Where-Object {$_.FullName -notin $knownPaths})
if($changed.Count -ne 0 -or $added.Count -ne 0) { throw "Public bin changed: modified=$($changed.Count), added=$($added.Count)" }
$preserved=Get-Content -LiteralPath (Join-Path $taskRoot 'preserved-before.json') -Raw | ConvertFrom-Json
foreach($item in $preserved) {
    if((Get-FileHash -LiteralPath $item.Path -Algorithm SHA256).Hash -ne $item.Hash) { throw "Preserved file changed: $($item.Path)" }
}
$report=[pscustomobject]@{Assemblies=$records;BclVersions=$bcl;PublicBinFiles=$runtimeBefore.Count;PublicBinModified=$changed.Count;PublicBinAdded=$added.Count;PreservedFiles=$preserved.Count;AssemblyReferenceVersionsMatch=$true}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'artifact-verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
