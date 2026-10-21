from pathlib import Path
import json,hashlib,shutil,csv,sys,xml.etree.ElementTree as ET
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parent
rc01=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01');rc02=Path(r'E:\VB_Det_PACK\20261020_50Series_RC02');release=rc02/'03_Release';app=release/'App'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2),encoding='utf-8-sig')
before=load(root/'rc01-before.json')
assert {r['File'] for r in before}=={str(p.relative_to(rc01)) for p in rc01.rglob('*') if p.is_file()}
for r in before:assert sha(rc01/r['File'])==r['SHA256'],r['File']
provenance=load(root/'payload-provenance.json')
assert len(provenance)==67
for r in provenance:assert sha(release/r['Destination'])==r['SHA256']==sha(Path(r['Source']))
lock=load(release/'Prerequisites/GPU50-runtime-lock.json')
for r in lock['Files']:assert sha(Path(r['SourceAtFreeze']))==r['SHA256'],r['File']
sys.path.insert(0,str(root.parent/'20261020_RC01发布审计'))
from pe_metadata import PE
expected={'System.Memory':'4.0.1.2','System.Buffers':'4.0.3.0','System.Numerics.Vectors':'4.1.4.0','System.Runtime.CompilerServices.Unsafe':'6.0.0.0'}
versions={n:PE(app/(n+'.dll')).managed()['Version'] for n in expected};assert versions==expected
config=ET.parse(app/'电极帽缺陷检测.exe.config').getroot();ns={'a':'urn:schemas-microsoft-com:asm.v1'}
redirects={e.find('a:assemblyIdentity',ns).get('name'):e.find('a:bindingRedirect',ns).get('newVersion') for e in config.findall('.//a:dependentAssembly',ns)}
for n,v in expected.items():assert redirects[n]==v
own={n:PE(app/(n+('.exe' if n=='电极帽缺陷检测' else '.dll'))).managed()['Version'] for n in ['电极帽缺陷检测','CommonCls','xbVP_Control','PipeChanel']}
for n in own:
 for r in PE(app/(n+('.exe' if n=='电极帽缺陷检测' else '.dll'))).managed()['References']:
  if r['Name'] in own:assert r['Version']==own[r['Name']]
bin_before=[r for r in load(rc01/'04_Audit/original-before.json') if r['Path'].lower().startswith(r'e:\jieyawenjian\vb_det\bin'+'\\')]
for r in bin_before:assert sha(Path(r['Path']))==r['SHA256'],r['Path']
assert len(bin_before)==len([p for p in Path(r'E:\jieyawenjian\VB_Det\bin').rglob('*') if p.is_file()])
tests=load(root/'entry-tests.json');assert len(tests)==5 and all(r['PASS'] for r in tests)
passing=[]
for p in sorted((root/'GPU验证').glob('launch*.json')):
 r=load(p)
 if r['Status']=='GpuVerificationPassed':passing.append((p,r))
assert len(passing)>=2
passing=passing[-2:]
for p,r in passing:
 assert r['RuntimeApi']=='12.8' and r['CudnnVersion']=='9.14.0.64' and r['CudnnMismatchWarning'] is False
 assert len(r['ActualGpuModules'])==9
 for q in [p,Path(r['ProbeLog']),Path(r['ModulesFile'])]:shutil.copy2(q,release/'Documents'/q.name)
report={'Batch':rc02.name,'ApplicationSourceBatch':str(rc01),'ApplicationFilesIdenticalToRC01':67,'SourceCodeChanged':False,'NewBuildExecuted':False,'BuildEvidence':'Unchanged RC01 VisionDection.sln Debug/x64 full 11-project Build, 0 errors / 83 existing warnings','OwnAssemblyVersions':own,'FoundationAssemblyVersions':versions,'BindingRedirectsMatch':True,'RC01FilesUnchanged':len(before),'OriginalBinFilesUnchanged':len(bin_before),'RuntimeLockFiles':len(lock['Files']),'ExternalGPUFiles':17,'CudaToolkit':'12.8.0','Cudnn':'9.14.0.64 CUDA12 build','DedicatedEntry':'03_Release/Launch.cmd','WorkingDirectory':'03_Release/App','FinalEntryGpuIndependentRunsPassed':2,'ActualGpuModulesPerRun':9,'ActualCudaRuntimeAPI':'12.8','CudnnMismatchWarning':False,'NegativeCasesPassed':['cuDNN9.8','cuDNN9.14 CUDA13 branch','missing CUDA directory'],'MainApplicationStartedByAgent':False,'RealImageAccuracyAndHardware':'Not tested in this step; user will run main application','LaunchLogDirectory':str(rc02/'06_RunLogs'),'ChecksRecordDirectory':str(root),'RuntimeUsesExistingMachineConfigAndAuthorization':True,'InstallerCreated':False,'ExternalInstallerMediaIncluded':False}
save(root/'verification.json',report);save(release/'Documents/RC02-verification.json',report)
shutil.copy2(root/'entry-tests.json',release/'Documents/entry-tests.json')
provenance_map={r['Destination']:r for r in provenance};manifest=[]
for p in sorted(release.rglob('*')):
 if not p.is_file() or p.name in ['release-manifest.csv','release-manifest.sha256']:continue
 rel=p.relative_to(release).as_posix();r=provenance_map.get(rel,{})
 m=PE(p).managed() if p.suffix.lower() in ['.dll','.exe'] else None
 manifest.append({'RelativePath':rel,'Bytes':p.stat().st_size,'SHA256':sha(p),'AssemblyVersion':m['Version'] if m else '', 'Source':r.get('Source','RC02 startup environment / verification metadata'),'Reason':r.get('Reason','Dedicated launcher and locked runtime')})
with (release/'release-manifest.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=list(manifest[0]));w.writeheader();w.writerows(manifest)
(release/'release-manifest.sha256').write_text(sha(release/'release-manifest.csv')+'  release-manifest.csv\n',encoding='ascii')
report['ReleaseManifestEntries']=len(manifest)
# Keep the archive summary count outside the frozen release report to avoid another hash cycle.
save(root/'verification.json',report)
(root/'README.md').write_text(f'''# RC02启动环境记录（2026-10-20）

完成：建立E:\\VB_Det_PACK\\20261020_50Series_RC02，保留RC01；专用入口为03_Release/Launch.cmd。
App67文件与RC01逐字节一致，同批DLL/模型/LD.vpp不变，本轮无C#修改或重编。沿用RC01实际VisionDection.sln Debug/x64全11项目Build（0错误/83既有警告），本轮复核同批引用、四项基础依赖与重定向一致。

锁定CUDA12.8.0及cuDNN9.14.0.64 CUDA12版，共17个外部GPU组件、16个核心应用文件、1个GPU诊断程序；启动前校验版本、长度和SHA256，仅设置新建进程环境。Windows PowerShell兼容性修正使用.NET SHA256与纯.NET模块清单读取；历史开发中未完成的Preflight记录不算通过，最终通过记录见verification.json、entry-tests.json与GPU验证目录最新两组。

最终同一Launch.cmd的两次独立GPU检查均通过：实际Runtime API12.8，7个cuDNN模块来自9.14/bin/12.9，2个cuBLAS来自CUDA12.8，Paddle/phi/MKL链全部来自RC02 App，推理ret=0且版本不匹配警告消失。旧cuDNN9.8、CUDA13版cuDNN、缺失CUDA目录三项拒绝检查通过。

RC01共{len(before)}文件、原公共bin{len(bin_before)}文件核对不变；RC02发布清单{len(manifest)}项。运行入口/依赖清单/哈希均在03_Release，本目录保留制作脚本、清单来源、模块路径与原始验证日志，记录不写C盘。

未启动完整主程序、未操作相机/串口/IO、未验证真实样本准确率/5070/连续稳定性、未制作安装器或收集外部介质。测试主程序由用户执行：先关闭旧程序，再双击RC02的Launch.cmd；启动器记录在批次06_RunLogs，业务日志服从现有配置。现机模型/VPP保存路径仍可指向D盘，原配置/授权不重置、不覆盖。
''',encoding='utf-8-sig')
print(json.dumps({'RC02':str(rc02),'AppIdentical':67,'ManifestEntries':len(manifest),'GpuRunsPassed':2,'NegativeCasesPassed':3,'RC01Unchanged':len(before),'OriginalBinUnchanged':len(bin_before)},ensure_ascii=True))
