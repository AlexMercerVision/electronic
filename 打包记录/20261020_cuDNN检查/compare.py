from pathlib import Path
import os,subprocess,json,time,hashlib
root=Path(__file__).resolve().parent
batch=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01')
app=batch/'03_Release/App';exe=batch/'04_Audit/GpuProbe.exe'
rows=[]
for label,path in [('baseline98',r'D:\NVIDIAGPU\bin'),('candidate914',r'C:\Program Files\NVIDIA\CUDNN\v9.14\bin\12.9;D:\NVIDIAGPU\bin')]:
 env=os.environ.copy();env['PATH']=path+r';C:\Windows\System32;C:\Windows';env['CUDA_CACHE_PATH']=str(root/'CudaCache')
 log=root/(label+'.log');modules=root/(label+'-modules.txt')
 t=time.time()
 with log.open('wb') as f:
  p=subprocess.run([str(exe),str(app),str(modules)],cwd=root,env=env,stdout=f,stderr=subprocess.STDOUT,timeout=90)
 text=log.read_text(encoding='utf-8-sig',errors='replace')
 mods=modules.read_text(encoding='utf-8-sig').splitlines() if modules.exists() else []
 row={'Case':label,'ExitCode':p.returncode,'ElapsedSeconds':round(time.time()-t,2),'WarningMismatch':'compiled with CUDNN' in text,'NativeSuccess':'GPU0 ret=0' in text,'CudnnModules':[s for s in mods if 'cudnn' in Path(s).name.lower()],'CudaModules':[s for s in mods if 'cublas' in s.lower()]}
 rows.append(row);print(json.dumps(row,ensure_ascii=True),flush=True)
(root/'results.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
