$ErrorActionPreference='Stop'
$root='E:\VB_Det_PACK\20261020_50Series_RC01'
$output=Join-Path $root '02_Build\Output'
$rows=foreach($file in Get-ChildItem -LiteralPath $output -File | Where-Object {$_.Extension -in '.dll','.exe'}) {
 try {
  $assembly=[Reflection.Assembly]::LoadFile($file.FullName)
  $refs=foreach($ref in $assembly.GetReferencedAssemblies()) {
   $token=([BitConverter]::ToString($ref.GetPublicKeyToken())).Replace('-','').ToLowerInvariant()
   $framework=($token -in @('b77a5c561934e089','b03f5f7f11d50a3a','31bf3856ad364e35','cc7b13ffcd2ddd51')) -and ($ref.Name -eq 'mscorlib' -or $ref.Name -eq 'netstandard' -or $ref.Name -like 'System*' -or $ref.Name -like 'Microsoft.CSharp*')
   [pscustomobject]@{Name=$ref.Name;Version=$ref.Version.ToString();Framework=$framework}
  }
  [pscustomobject]@{File=$file.Name;Path=$file.FullName;Name=$assembly.GetName().Name;Version=$assembly.GetName().Version.ToString();References=@($refs)}
 } catch { Write-Host ('Metadata skipped (native or x86): '+$file.Name) }
}
$rows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $root '04_Audit\assembly-metadata.json') -Encoding UTF8
"Read $(@($rows).Count) assembly metadata records"
