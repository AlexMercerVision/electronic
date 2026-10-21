from pathlib import Path
import json,hashlib,shutil,csv,sys
sys.dont_write_bytecode=True
records=Path(__file__).resolve().parent
rc01=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01')
rc02=Path(r'E:\VB_Det_PACK\20261020_50Series_RC02')
assert rc02.parent==rc01.parent and rc02!=rc01
if rc02.exists():raise SystemExit('RC02 exists; refusing overwrite')
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
def save(p,obj):p.write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8-sig')
before=[{'File':str(p.relative_to(rc01)),'Bytes':p.stat().st_size,'SHA256':sha(p)} for p in rc01.rglob('*') if p.is_file()]
save(records/'rc01-before.json',before)
release=rc02/'03_Release';app=release/'App'
for p in [release,release/'Documents',release/'Prerequisites',release/'Diagnostics',rc02/'06_RunLogs']:p.mkdir(parents=True,exist_ok=True)
shutil.copytree(rc01/'03_Release/App',app)
provenance=[]
for p in app.rglob('*'):
 if p.is_file():
  src=rc01/'03_Release/App'/p.relative_to(app);assert sha(src)==sha(p)
  provenance.append({'Destination':'App/'+p.relative_to(app).as_posix(),'Source':str(src),'Bytes':p.stat().st_size,'SHA256':sha(p),'Reason':'Identical frozen RC01 same-build application/model/VPP payload'})
assert len(provenance)==67
for n in ['GpuProbe.exe','GpuProbe.cs']:shutil.copy2(rc01/'04_Audit'/n,release/'Diagnostics'/n)
# Windows PowerShell 5.1 requires BOM for scripts containing Chinese literals.
(release/'Launch.ps1').write_text((records/'Launch.ps1').read_text(encoding='utf-8-sig'),encoding='utf-8-sig',newline='\r\n')
(release/'Launch.cmd').write_text((records/'Launch.cmd').read_text(encoding='ascii'),encoding='ascii',newline='\r\n')
paths={'CudaRoot':r'D:\NVIDIAGPU','CudnnBin':r'C:\Program Files\NVIDIA\CUDNN\v9.14\bin\12.9','LogDirectory':'../06_RunLogs'}
save(release/'RuntimePaths.json',paths)
cuda=Path(paths['CudaRoot']);cudnn=Path(paths['CudnnBin'])
files=[]
def lock(root,p,reason):
 files.append({'Root':root,'File':p.name,'Bytes':p.stat().st_size,'SHA256':sha(p),'SourceAtFreeze':str(p),'Reason':reason})
for p in sorted(cudnn.glob('cudnn*.dll')):lock('CudnnBin',p,'Complete cuDNN9.14.0.64 CUDA12 component set; not CUDA13')
assert len(files)==8
for name in ['cublas64_12.dll','cublasLt64_12.dll','cudart64_12.dll','cufft64_11.dll','cusolver64_11.dll','cusparse64_12.dll','nvrtc64_120_0.dll','nvrtc-builtins64_128.dll','nvJitLink_120_0.dll']:
 lock('CudaBin',cuda/'bin'/name,'CUDA12.8 toolkit runtime components; includes current model loaded and potential dynamic GPU dependencies')
own=['电极帽缺陷检测.exe','CommonCls.dll','xbVP_Control.dll','PipeChanel.dll','电极帽缺陷检测.exe.config','System.Memory.dll','System.Buffers.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll','model_infer.dll','paddle_inference_c.dll','phi.dll','common.dll','mkldnn.dll','mklml.dll','libiomp5md.dll']
for name in own:lock('App',app/name,'Unchanged RC01 batch; native and managed dependencies')
lock('Diagnostics',release/'Diagnostics/GpuProbe.exe','Unchanged RC01 native GPU-only diagnostic; no camera/serial/config/authentication access')
sdk=json.loads((cuda/'version.json').read_text(encoding='utf-8'))
runtime={'Batch':rc02.name,'CudaToolkitVersion':'12.8.0','CudnnVersion':'9.14.0.64','CudnnBuild':'CUDA12','CudaSdkVersionJson':sdk,'DriverBaseline':'RTX5060 / 616.56; driver files are installed externally and not copied','Files':files,'PathsFile':'RuntimePaths.json','ChangesSystemPath':False}
save(release/'Prerequisites/GPU50-runtime-lock.json',runtime)
save(records/'payload-provenance.json',provenance)
save(release/'Documents/payload-provenance.json',provenance)
shutil.copy2(rc01/'04_Audit/verification.json',release/'Documents/RC01-build-verification.json')
shutil.copy2(rc01/'04_Audit/managed-closure.json',release/'Documents/managed-closure.json')
shutil.copy2(rc01/'04_Audit/native-imports.json',release/'Documents/native-imports.json')
shutil.copy2(rc01/'03_Release/Verify-Release.ps1',release/'Verify-Release.ps1')
(release/'README.md').write_text('''# 电极帽缺陷检测 · RC02 测试入口

请先关闭旧版检测程序，再双击本目录的 **Launch.cmd**。它会核对并只为本次软件进程设置CUDA12.8/cuDNN9.14 CUDA12版环境，再启动App中的主程序。直接双击App内exe会绕过专用环境，不作为RC02测试入口。

本批App的67个文件与RC01逐字节一致；模型和用户选择的LD.vpp保留，未改业务代码、算法或相机/串口/授权逻辑。源构建为VisionDection.sln Debug/x64、11项目、0错误/83既有警告。本轮无C#修改，不重新构建，具体版本及依赖一致性见Documents/RC01-build-verification.json和同批哈希。

运行库仍由本机已装环境提供，未复制CUDA/cuDNN进App、未修改全局PATH。RuntimePaths.json当前定位D:\\NVIDIAGPU与C:\\Program Files\\NVIDIA\\CUDNN\\v9.14\\bin\\12.9；锁定清单核对完整8个cuDNN组件、CUDA12.8运行组件和本批核心程序/原生库。版本/哈希不匹配会停止并显示原因，不能单独混换一个DLL。

启动器的环境记录默认写到本批目录06_RunLogs；应用自身日志继续服从现有配置（当前电脑为D:\\data\\Log）。启动器创建主进程不等于模型/硬件/检测已验证。

**本机已有配置与授权会按原程序规则继续使用**，不会因换了发布目录而重置。当前读取的模型为D:\\data\\inference_model，VPP为D:\\data\\LD.vpp；已有ProgramData工作VPP也不会被安装资源覆盖。请在界面/日志确认实际加载文件后测试。新电脑仍按源码的新机参数和注册流程处理，不复制旧参数/授权。

可在本目录命令行执行 `Launch.cmd -CheckOnly` 仅核对依赖，或 `Launch.cmd -VerifyGpu` 运行独立GPU内存图像检查；后者不打开完整主程序、不取相机图、不操作光源/IO。发布文件校验使用Verify-Release.ps1。

测试请先核对正常加载模型/VPP，再用真实A/B面OK、NG、边界样本比较结果，之后验证相机、光源、手动Y输出、外部X触发及连续检测。本批仍按50系使用，30/40系需支持对应架构的成套推理库。尚未制作Setup、补齐外部安装介质或完成5070现场验收。
''',encoding='utf-8-sig')
(release/'Prerequisites/REQUIRED.md').write_text('''# RC02 外部依赖

GPU：本机RTX5060/616.56驱动，CUDA12.8.0，cuDNN9.14.0.64的CUDA12版；具体17个外部DLL哈希及本批软件依赖见GPU50-runtime-lock.json。当前cuDNN目录名12.9属于CUDA12构建分支，按此前官方支持矩阵及本机隔离测试与CUDA12.8配合。不可使用同版本的bin/13.0分支。

本批不改系统环境，只设置新建软件进程的PATH和CUDA_PATH。环境目录可在RuntimePaths.json中适配目标电脑，但文件版本/内容仍必须符合锁定清单；改路径后需重新生成发布清单并验证。

其他：.NET Framework4.7.2兼容运行时、VC14 x64及VisionPro所需VC100、MVS4.8.0.3和配套相机驱动、匹配59.2.0.0程序集的VisionPro运行组件/授权、HALCON12运行环境/授权、串口适配器驱动。本轮不安装这些组件，不将.NET封装DLL当作SDK安装或授权。OpenCV当前wrapper/Extern与Paddle/phi/MKL链继续随App交付，mklml来自App，无需Python环境。

所有外部安装介质仍未收入本批；这是当前电脑测试用的受控目录，不是完整离线安装器。后续5070目标机需要先具备对应环境。
''',encoding='utf-8-sig')
(rc02/'README.md').write_text('''# 20261020_50Series_RC02

测试入口：03_Release/Launch.cmd。App完整复制自冻结RC01，同批程序/DLL/模型/VPP不变，仅新增专用启动环境和版本/哈希检查；RC01保留。

软件输入：03_Release。启动记录：06_RunLogs。构建来源：E:\\VB_Det_PACK\\20261020_50Series_RC01；不重复构建/混用原bin。统一检查记录：E:\\jieyawenjian\\VB_Det\\打包记录\\20261020_RC02启动环境。

本机测试将读取已有ProgramData配置及AppData授权，模型/VPP可继续指向已保存的D盘路径，启动入口不改写这些设置。此目录不是新的机器数据隔离区。真实检测结果由用户测试；自动检查仅运行独立GPU诊断。
''',encoding='utf-8-sig')
save(records/'assembly-plan.json',{'RC01':str(rc01),'RC02':str(rc02),'CopiedAppFiles':67,'RuntimeLockedFiles':len(files),'NewCSharpChanges':False,'FullBuild':'Reuses unchanged RC01 11-project Debug/x64 full build; no new build','MainAppStartedByAgent':False})
print(json.dumps({'RC02':str(rc02),'AppFiles':len(provenance),'ExternalRuntimeFiles':17,'AllLockedFiles':len(files)},ensure_ascii=True))
