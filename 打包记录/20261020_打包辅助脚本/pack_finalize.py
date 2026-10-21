from pathlib import Path
import json,hashlib,re,xml.etree.ElementTree as ET,csv,shutil,subprocess
from pe_metadata import PE
base=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01');audit=base/'04_Audit';release=base/'03_Release';app=release/'App';out=base/'02_Build/Output'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
def load(n):return json.loads((audit/n).read_text(encoding='utf-8-sig'))
def save(p,obj):p.write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
log=(audit/'solution-build.log').read_text(encoding='gbk',errors='replace')
assert '0 个错误' in log and '83 个警告' in log
assert not re.search(r'warning MSB(3277|3245|3258)|error [A-Z]+\d+',log,re.I)
calls=[]
for line in log.splitlines():
 if 'csc.exe ' not in line or '/out:' not in line:continue
 def arg(m):return m.group(1) or m.group(2)
 target=arg(re.search(r'/out:(?:"([^"]+)"|(\S+))',line))
 refs=[arg(m) for m in re.finditer(r'/reference:(?:"([^"]+)"|(\S+))',line)]
 own=[r for r in refs if Path(r).name in ['CommonCls.dll','xbVP_Control.dll','PipeChanel.dll']]
 for r in own:assert Path(r).parent.resolve()==out.resolve(),r
 calls.append({'Output':target,'ProjectReferences':own})
assert len(set(c['Output'] for c in calls))==11,len(calls)
metadata={m['Name']:m for m in load('assembly-metadata.json')}
own={n:metadata[n]['Version'] for n in ['电极帽缺陷检测','CommonCls','xbVP_Control','PipeChanel']}
for n in own:
 for r in metadata[n]['References']:
  if r['Name'] in own:assert r['Version']==own[r['Name']]
expected={'System.Memory':'4.0.1.2','System.Buffers':'4.0.3.0','System.Numerics.Vectors':'4.1.4.0','System.Runtime.CompilerServices.Unsafe':'6.0.0.0'}
dep=[]
original=Path(r'E:\jieyawenjian\VB_Det')
for n,v in expected.items():
 versions={label:PE(root/(n+'.dll')).managed()['Version'] for label,root in [('Original',original/'bin'),('Build',out),('Release',app)]}
 assert set(versions.values())=={v},(n,versions)
 dep.append({'Name':n,'Required':v,**versions})
for p in [original/'bin/电极帽缺陷检测.exe.config',out/'电极帽缺陷检测.exe.config',app/'电极帽缺陷检测.exe.config']:
 root=ET.parse(p).getroot();ns={'a':'urn:schemas-microsoft-com:asm.v1'}
 redirects={e.find('a:assemblyIdentity',ns).get('name'):e.find('a:bindingRedirect',ns).get('newVersion') for e in root.findall('.//a:dependentAssembly',ns)}
 for n,v in expected.items():assert redirects[n]==v,(p,n)
for r in load('payload-provenance.json'):
 p=app/r['Destination'];assert sha(p)==r['SHA256'];assert sha(Path(r['Source']))==r['SHA256'],r['Source']
for r in load('source-snapshot-manifest.json'):
 assert sha(Path(r['Source']))==r['SHA256'],r['Source']
 assert sha(base/'01_SourceSnapshot'/r['Snapshot'])==r['SHA256'],r['Snapshot']
protected=load('original-before.json')
for r in protected:assert sha(Path(r['Path']))==r['SHA256'],r['Path']
before_bin={r['Path'].lower() for r in protected if r['Path'].lower().startswith(str(original/'bin').lower()+'\\')}
after_bin={str(p).lower() for p in (original/'bin').rglob('*') if p.is_file()}
assert before_bin==after_bin
assert not (app/'UserData').exists()
assert [p.name for p in app.rglob('*.vpp')]==['LD.vpp']
assert [p.name for p in app.glob('*.exe')]==['电极帽缺陷检测.exe']
for pattern in ['*authentication*','*password*','*.bak','*.dat','*.log','*.pdb']:
 assert not list(app.rglob(pattern)),pattern
assert sha(app/'VPP/LD.vpp')=='84B0B16D5AA2587581BA1464709086C1541F1549B655A3EA949D87EA8A90FC86'
mods=(audit/'gpu-loaded-modules-isolated.txt').read_text().splitlines()
assert not any('anaconda' in x.lower() or str(original).lower() in x.lower() for x in mods)
assert str(app/'mklml.dll').lower() in [x.lower() for x in mods]
for n,text in [('gpu-probe-isolated.log','GPU0 ret=0'),('opencv-probe.log','OpenCV conversion OK'),('vpp-probe.log','LD.vpp deserialize OK; Jobs=2')]:
 assert text in (audit/n).read_text(encoding='utf-8-sig',errors='replace')
# Record static PE imports for mixed-mode SDK wrappers as well as native DLLs.
native=[]
for p in app.rglob('*.dll'):
 pe=PE(p);imports=pe.imports()
 if imports:native.append({'File':str(p.relative_to(app)),'Architecture':hex(pe.machine),'Imports':imports})
save(audit/'native-imports.json',native)
runtime=load('gpu-runtime-lock.json');runtime=[r for r in runtime if not r['File'].lower().endswith('.exe')]
save(audit/'gpu-runtime-lock.json',runtime)
verification={'Solution':'VisionDection.sln','Configuration':'Debug/x64','Build':'PASS','CompiledProjects':calls,'Errors':0,'Warnings':83,'OwnAssemblyVersions':own,'Foundation':dep,'BindingRedirects':'PASS original/build/release','SourceSnapshotFilesUnchanged':len(load('source-snapshot-manifest.json')),'ProtectedOriginalFilesUnchanged':len(protected),'OriginalBinFilesUnchanged':len(before_bin),'OriginalBinAdded':0,'OriginalBinRemoved':0,'LD_SHA256':sha(app/'VPP/LD.vpp'),'AppFiles':len([p for p in app.rglob('*') if p.is_file()]),'AppBytes':sum(p.stat().st_size for p in app.rglob('*') if p.is_file()),'OpenCvConversion':'PASS copied wrapper + copied dll/x64/OpenCvSharpExtern.dll','VppDeserialize':'PASS current installed VisionPro runtime, 2 Jobs, not executed','GpuInference':'PASS RTX5060 GPU0, current CUDA12.8/cuDNN9.8, copied model, ret=0 / zero boxes','GpuWithoutPythonOrOriginalProjectPath':'PASS','CudnnMismatch':'Paddle compiled 9.12 vs installed 9.8 warning remains','MainApplicationLaunch':'NOT RUN','HardwareValidation':'NOT RUN (5070/cameras/light/IO/continuous/measurement)','Installer':'NOT CREATED','ExternalInstallerMedia':'NOT INCLUDED; see Prerequisites/REQUIRED.md'}
save(audit/'verification.json',verification)
for n in ['verification.json','payload-provenance.json','managed-closure.json','native-imports.json'] :shutil.copy2(audit/n,release/'Documents'/n)
shutil.copy2(audit/'gpu-runtime-lock.json',release/'Prerequisites/GPU50-runtime-lock.json')
for n in ['gpu-probe-isolated.log','opencv-probe.log','vpp-probe.log']:shutil.copy2(audit/n,release/'Documents'/n)
(release/'README.md').write_text('''# 电极帽缺陷检测 · 受控发布候选目录

批次：20261020_50Series_RC01。第二步文件收集、同批构建与审计已完成；此目录是下一步安装器的输入，尚不是 Setup.exe 或已通过5070现场验收的完整离线安装包。

- `App`：交给后续安装器复制的应用负载。唯一入口 `电极帽缺陷检测.exe`，模型及 VPP 已包括。
- `Prerequisites`：单独安装的运行环境要求及当前50系实际GPU模块锁定清单；本批未收入安装介质。
- `Documents`：文件来源、程序集依赖、构建/验证结果和新机操作说明。
- `release-manifest.csv`：全部交付文件的SHA256；复制后运行 `Verify-Release.ps1` 核验（只读，不安装、不启动应用）。

请完整复制此目录，不要只复制 exe；不要将01_SourceSnapshot、02_Build、04_Audit、05_Symbols当作客户安装内容。
安装或快捷方式的工作目录设为 `App`。目前保留一处XML模板相对读取，运行目录仍需正确设置。
''',encoding='utf-8-sig')
(release/'Prerequisites/REQUIRED.md').write_text('''# 外部运行环境（安装介质尚未收集）

本批保持5060现有环境，先部署5070；30/40系另建验证批次，20系旧环境未收集。以下是本机证据和部署要求，不表示可在新机免安装。

| 环境 | 锁定依据与处理 |
|---|---|
| Windows / .NET Framework | x64 Windows；目标Framework4.7.2兼容运行时。本机4.8（Release533509）。目标机安装兼容运行时，不需VS。 |
| VC运行库 | 原生推理加载VCOMP140；VisionPro加载MSVCP100/MSVCR100。VC14运行库x64和厂商要求的VC2010组件均需覆盖，后者通常由VisionPro安装器部署；不能仅安装一种新版VC运行库就假定全部齐备。 |
| GPU驱动 | 本机RTX5060，驱动616.56，设备能力12.0，Driver API13.4。5070安装对应驱动并重新验证；驱动DLL不得从System32复制部署。 |
| CUDA/cuDNN | 实际推理Runtime API12.8，动态库来自D:\\NVIDIAGPU\\bin；实际cuDNN9.8.0.87。采用匹配部署介质后按GPU50-runtime-lock.json核对，目标机无需相同盘符，但搜索路径必须指向正确运行库。不能把PATH里CUDA13.0、旧bin中CUDA10.2/cuDNN7当成本批依赖。 |
| 已知GPU风险 | 原生Paddle提示编译cuDNN9.12、当前加载9.8；本轮原样保留，5060空白图推理通过。新机完整模型输入、连续运行与稳定性验收前不得宣称该不匹配已解决。 |
| VisionPro | 程序与VPP要求59.2.0.0程序集及匹配x64原生运行组件、GAC/COM注册、有效授权。本机VPRO_ROOT=E:\\VP9.0\\VisionPro。App中的封装DLL不能代替厂商安装器；间接托管依赖详见Documents/managed-closure.json。 |
| HALCON | halcondotnet=12.0.0.0。需匹配HALCON12 x64运行库及实际功能所需授权；本轮未验证HALCON原生调用/授权。 |
| 海康MVS | 本机Win64_x64 MvCameraControl.dll=4.8.0.3，应用.NET封装=3.4.0.1。安装配套Runtime与相机驱动，再按序列号配置两台相机。 |
| 串口适配器 | 安装目标设备驱动，首次按光源和IO用途人工分配；不能沿用旧COM4或旧列表索引。光源前三槽可按现代码共享控制器，IO第4槽独占。 |

OpenCV本批已收入当前OpenCvSharp托管封装及配套`dll/x64/OpenCvSharpExtern.dll`，复制后颜色转换测试通过；不依赖另装OpenCV开发目录。未收入旧opencv_world346/346d和ffmpeg视频插件，现有JPG取图/保存链不要求视频插件。

软件包已经收入实际使用的Paddle/phi/common/MKL原生链，包括从当前Python环境实际加载并独立验证的mklml.dll；目标机不需因此安装Python或Anaconda。

本目录目前只有环境清单，没有.NET/VC/GPU/CUDA/cuDNN/VisionPro/HALCON/MVS/串口驱动安装介质。第三步制作完整离线安装器时必须补齐相应安装介质及安装检测逻辑；本轮没有下载、安装、替换这些环境。
''',encoding='utf-8-sig')
(release/'Documents/新机部署说明.md').write_text('''# 新机部署与验收

1. 先部署Prerequisites中列明的运行环境和硬件驱动、商业SDK运行授权。确认目标系统及GPU环境，再复制完整App到最终安装路径。
2. 快捷方式目标为主exe，工作目录设为App。现有机器上的完整主程序未用于本批测试：它会读取真实ProgramData/AppData，可能操作原配置和设备。
3. 首次启动沿用源码的新机流程，机器参数在`%ProgramData%\\ElectrodeDet\\UserData\\serUserParam.json`创建；该文件保存相机、串口、路径及检测参数，本批不附旧机器版本。相机/串口未分配、自动连接及自动运行关闭，用户按新机实际情况设置。
4. 初次授权文件在`%APPDATA%\\GearDet\\authentication.txt`。走正常新机注册、设置登录密码流程；旧机器授权不复制，商业SDK授权仍需另行处理。
5. 首次从`App\\VPP\\LD.vpp`复制工作文件到`%ProgramData%\\ElectrodeDet\\Workspaces\\Default\\VPP\\LD.vpp`；已有工作文件不会覆盖。若目标机有历史配置/工作区，需人工核对实际加载路径和VPP校验值，本包不会替换为所附LD。模型默认`App\\Assets\\Models\\Default\\inference_model`，默认GPU0/paddlex。
6. 重新选择相机序列号及A/B工位映射、实际COM/设备身份、曝光、光源亮度、IO映射和反馈。新机的标定/测量及VPP中内嵌模板必须重新核对，不能把旧机器结果当成新硬件标定。
7. 逐项记录：启动与首次授权、两台相机取图、光源控制、手动单次Y输出、外部X触发、双工位及连续重复检测；同时检查尺寸测量/模式切换/异常处理和日志。5070以及后续30/40系分别验收。

本批资源以用户明确选择的D:\\data\\LD.vpp为准（SHA256 84B0B16D5AA2587581BA1464709086C1541F1549B655A3EA949D87EA8A90FC86），未选内容不同的bin/VPP或ProgramData版本。模型来自D:\\data\\inference_model的当前7个文件。

NullMark_00.xml是代码需要的XML输出模板，保持原字节；其中旧示例图像路径是模板字段，不是模型/VPP加载位置。程序输出时重写filename/path；本轮未验证XML业务导出。

只收集主程序依赖，未收入其他工具exe、JCZ激光服务/驱动/模板、旧模型/VPP、旧CUDA/cuDNN/TensorRT、旧UserData/RTS、密码/机器授权、现场图像/日志。调试符号留在批次05_Symbols。

已完成的隔离检查见verification.json和三份probe日志；VPP仅反序列化未执行Job，GPU仅内存空白图推理，OpenCV仅内存颜色转换。这些不等于现场整机验收。
''',encoding='utf-8-sig')
(release/'Verify-Release.ps1').write_text('''$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$manifest = @(Import-Csv -LiteralPath (Join-Path $root 'release-manifest.csv') -Encoding UTF8)
$expected = @{}
foreach ($entry in $manifest) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $entry.RelativePath))
    if (-not $path.StartsWith($root + '\\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid manifest path' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing: $($entry.RelativePath)" }
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.Bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Changed: $($entry.RelativePath)" }
    $expected[$path] = $true
}
foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse) {
    if ($file.Name -in @('release-manifest.csv','release-manifest.sha256')) { continue }
    if (-not $expected.ContainsKey($file.FullName)) { throw "Unexpected: $($file.FullName)" }
}
$digest = ((Get-Content -LiteralPath (Join-Path $root 'release-manifest.sha256') -Raw).Trim() -split '\\s+')[0]
if ((Get-FileHash -LiteralPath (Join-Path $root 'release-manifest.csv') -Algorithm SHA256).Hash -ne $digest) { throw 'Manifest changed' }
Write-Host "PASS: $($manifest.Count) release files, SHA256 match."
''',encoding='utf-8-sig')
# Customer manifest excludes only itself and its digest to avoid a circular hash.
provenance={r['Destination']:r for r in load('payload-provenance.json')}
manifest=[]
for p in sorted(release.rglob('*')):
 if not p.is_file() or p.name in ['release-manifest.csv','release-manifest.sha256']:continue
 rel=p.relative_to(release).as_posix();origin=provenance.get(p.relative_to(app).as_posix(),{}) if app in p.parents else {}
 m=PE(p).managed() if p.suffix.lower() in ['.dll','.exe'] else None
 manifest.append({'RelativePath':rel,'Bytes':p.stat().st_size,'SHA256':sha(p),'AssemblyVersion':m['Version'] if m else '', 'Source':origin.get('Source','Generated batch documentation / audit'),'Reason':origin.get('Reason','Controlled release metadata')})
with (release/'release-manifest.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(manifest[0]));w.writeheader();w.writerows(manifest)
(release/'release-manifest.sha256').write_text(sha(release/'release-manifest.csv')+'  release-manifest.csv\n',encoding='ascii')
(base/'README.md').write_text(f'''# 电极帽缺陷检测 · 20261020_50Series_RC01

第二步完成：独立源码快照、11项目完整构建、受控应用负载、依赖清单和SHA256审计。

- 交付/后续安装器输入：`03_Release`（App {verification['AppFiles']}文件，{verification['AppBytes']/1048576:.1f} MiB）
- `01_SourceSnapshot`：构建所需源码/包/第三方编译输入快照，未替换工程引用。
- `02_Build`：全解决方案输出及中间文件，不交给客户整包安装。
- `04_Audit`：原文件保护记录、源文件清单、完整构建日志、实际引用/原生依赖/探针记录、复核脚本。
- `05_Symbols`：本批四个自有程序集对应PDB。

本批实际解决方案为VisionDection.sln，Debug/x64，11项目/0错误/83既有警告。同批程序集：{json.dumps(own,ensure_ascii=False)}。
原公共bin {len(before_bin)}文件、受保护文件{len(protected)}个和快照输入{verification['SourceSnapshotFilesUnchanged']}个均核对不变，公共bin无增删。原解决方案改名状态保持，没有覆盖原工程或运行目录。

已验证当前5060的独立GPU推理（无需Python或原工程路径）、复制OpenCV颜色转换、选定LD.vpp反序列化（2 Jobs）。未启动完整主程序、未测5070/30/40系或真实相机/串口/IO；保留cuDNN9.12/9.8不匹配提示。未制作Setup，外部依赖安装介质未收入，详见03_Release说明。

本批冻结后不要手工替换其中exe/DLL/VPP/模型；需要更新时新建批次并重新生成清单。
''',encoding='utf-8-sig')
# Remove only failed diagnostic helpers created in this batch, never original files.
for n in ['MetadataAudit.cs','MetadataAudit.exe','metadata.ps1']:
 p=audit/n
 assert p.parent.resolve()==audit.resolve()
 if p.exists():p.unlink()
print(json.dumps({'AppFiles':verification['AppFiles'],'AppMiB':round(verification['AppBytes']/1048576,1),'DeliveryFiles':len(manifest),'CompiledProjects':len(calls),'Warnings':83,'OriginalProtectedUnchanged':len(protected),'OriginalBinFilesUnchanged':len(before_bin)},ensure_ascii=False))
