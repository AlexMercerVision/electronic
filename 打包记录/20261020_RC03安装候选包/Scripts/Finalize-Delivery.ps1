param([string]$BatchRoot = 'E:\VB_Det_PACK\20261020_50Series_RC03_Installer')
$ErrorActionPreference = 'Stop'
$audit = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'RuntimeSupport.ps1')
if (-not (Test-Path -LiteralPath (Join-Path $audit 'installer-tests.json'))) { throw 'Complete install/upgrade/uninstall verification before delivery.' }
$protected = @(Import-Csv -LiteralPath (Join-Path $audit 'protected-source-bin.csv') -Encoding UTF8)
foreach ($entry in $protected) {
    if ((Get-FileDigest $entry.Path) -ne $entry.SHA256) { throw "Protected source/bin changed: $($entry.Path)" }
}
$expected = @{ 'System.Memory'='4.0.1.2'; 'System.Buffers'='4.0.3.0'; 'System.Numerics.Vectors'='4.1.4.0'; 'System.Runtime.CompilerServices.Unsafe'='6.0.0.0' }
$projectRoot = Split-Path (Split-Path $audit -Parent) -Parent
$versions = @(foreach ($dir in @((Join-Path $projectRoot 'bin'), "$BatchRoot\Input\App")) {
    foreach ($name in $expected.Keys) {
        $file = Join-Path $dir ($name + '.dll')
        $actual = [Reflection.AssemblyName]::GetAssemblyName($file).Version.ToString()
        if ($actual -ne $expected[$name]) { throw "Assembly mismatch: $file" }
        [pscustomobject]@{ File=$file; AssemblyVersion=$actual }
    }
    $configPath = Join-Path $dir '电极帽缺陷检测.exe.config'
    [xml]$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
    foreach ($name in $expected.Keys) {
        $node = @($config.SelectNodes("//*[local-name()='dependentAssembly']") | Where-Object { $_.assemblyIdentity.name -eq $name })
        if ($node.Count -ne 1 -or $node[0].bindingRedirect.newVersion -ne $expected[$name]) { throw "Binding redirect mismatch: $configPath/$name" }
    }
})
$versions | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $audit 'assembly-versions.json') -Encoding UTF8
$oldMedia = "$BatchRoot\Prerequisites\PendingVerification\VC_redist.x64_14.13.26020.exe"
if (Test-Path -LiteralPath $oldMedia) {
    New-Item -ItemType Directory -Path "$BatchRoot\MediaNotUsed" -Force | Out-Null
    $destination = "$BatchRoot\MediaNotUsed\VC_redist.x64_14.13.26020.exe"
    if (-not ([IO.Path]::GetFullPath($oldMedia)).StartsWith($BatchRoot + '\') -or -not ([IO.Path]::GetFullPath($destination)).StartsWith($BatchRoot + '\')) { throw 'Media move escapes batch' }
    Move-Item -LiteralPath $oldMedia -Destination $destination
}
Copy-Item -LiteralPath (Join-Path $audit 'MEDIA-STATUS.md') -Destination "$BatchRoot\Prerequisites\MEDIA-STATUS.md"
$delivery = Join-Path $BatchRoot '离线交付'
if (Test-Path -LiteralPath $delivery) { throw 'Delivery folder already exists. Do not overwrite a frozen candidate.' }
New-Item -ItemType Directory -Path $delivery | Out-Null
Copy-Item -LiteralPath "$BatchRoot\Output\电极帽缺陷检测_Setup.exe" -Destination $delivery
Copy-Item -LiteralPath "$BatchRoot\Prerequisites" -Destination "$delivery\Prerequisites" -Recurse
Copy-Item -LiteralPath (Join-Path $audit '安装说明及校验清单.md') -Destination $delivery
$verifySource = [IO.File]::ReadAllText('E:\VB_Det_PACK\20261020_50Series_RC02\03_Release\Verify-Release.ps1')
$verifySource = $verifySource.Replace('release-manifest.csv','delivery-manifest.csv').Replace('release-manifest.sha256','delivery-manifest.sha256')
[IO.File]::WriteAllText((Join-Path $delivery '校验交付.ps1'), $verifySource, (New-Object Text.UTF8Encoding($true)))
$manifest = @(foreach ($file in Get-ChildItem -LiteralPath $delivery -File -Recurse | Sort-Object FullName) {
    [pscustomobject]@{ RelativePath=$file.FullName.Substring($delivery.Length+1); Bytes=$file.Length; SHA256=(Get-FileDigest $file.FullName) }
})
$manifest | Export-Csv -LiteralPath "$delivery\delivery-manifest.csv" -NoTypeInformation -Encoding UTF8
((Get-FileDigest "$delivery\delivery-manifest.csv") + '  delivery-manifest.csv') | Set-Content -LiteralPath "$delivery\delivery-manifest.sha256" -Encoding ASCII
& "$delivery\校验交付.ps1"
$setup = Get-Content -LiteralPath (Join-Path $audit 'setup-artifact.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ((Get-FileDigest "$delivery\电极帽缺陷检测_Setup.exe") -ne $setup.SHA256) { throw 'Delivery Setup differs from tested output' }
Copy-Item -LiteralPath "$delivery\delivery-manifest.csv","$delivery\delivery-manifest.sha256" -Destination $audit
$summary = [ordered]@{
    Batch='20261020_50Series_RC03_Installer'; Delivery=$delivery; Setup=$setup
    DeliveryFiles=$manifest.Count; DeliveryBytes=($manifest | Measure-Object Bytes -Sum).Sum
    ProtectedSourceAndBinFiles=$protected.Count; SourceAndBinUnchanged=$true
    BusinessBuild='No C# business changes/rebuild. RC02 same-build App67 retained; origin: VisionDection.sln Debug/x64 full 11-project build, 0 errors/83 existing warnings.'
    Boundaries=@('No full main application or hardware test this round','Separate ordinary account and 5070 offline acceptance pending','HALCON12, serial adapter driver and GPU target driver pending; MVS installer runtime match pending','Setup unsigned; candidate only')
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $audit 'verification.json') -Encoding UTF8
$auditLink = (Join-Path $audit 'README.md').Replace('\','/')
@"
# 电极帽缺陷检测 RC03 安装候选包

客户交付入口：离线交付目录，包含电极帽缺陷检测_Setup.exe、Prerequisites、安装说明及SHA256清单。

实际编译脚本：Installer/ElectrodeDet.iss；静态输入：Input，App67项与RC02一致。Input不固化运行库路径，安装时生成RuntimePaths.json。

本机默认安装/升级/卸载、快捷方式、受限令牌文件权限和独立GPU检查见[项目审计记录]($auditLink)。SDK缺项和目标机验收边界见离线交付/Prerequisites/MEDIA-STATUS.md。Setup未签署发布者代码签名。

RC01和RC02冻结保留。新机尚需补齐环境、正常注册/商业授权并在无VS的5070断网验收；物理输出、外部触发、长期稳定性和不同普通用户账号启动未在本轮完成。
"@ | Set-Content -LiteralPath "$BatchRoot\README.md" -Encoding UTF8
Write-Host "Delivery frozen: $delivery"
