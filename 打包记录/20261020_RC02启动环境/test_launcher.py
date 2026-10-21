from pathlib import Path
import os,json,subprocess,time,hashlib
root=Path(__file__).resolve().parent
release=Path(r'E:\VB_Det_PACK\20261020_50Series_RC02\03_Release')
ps=r'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
settings=json.loads((release/'RuntimePaths.json').read_text(encoding='utf-8-sig'))
env_before=os.environ.copy();results=[]
for name,change in [('reject-cuDNN98',{'CudnnBin':r'D:\NVIDIAGPU\bin'}),('reject-CUDA13-cuDNN',{'CudnnBin':r'C:\Program Files\NVIDIA\CUDNN\v9.14\bin\13.0'}),('reject-missing-CUDA',{'CudaRoot':str(root/'NoSuchCudaDirectory')})]:
 pth=root/(name+'.json');pth.write_text(json.dumps({**settings,**change}),encoding='utf-8')
 t=time.time();p=subprocess.run([ps,'-NoProfile','-ExecutionPolicy','Bypass','-File',str(release/'Launch.ps1'),'-CheckOnly','-RuntimePathsFile',str(pth),'-RecordDirectory',str(root/'GPU验证')],stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=60)
 (root/(name+'.log')).write_bytes(p.stdout)
 assert p.returncode==1,(name,p.returncode)
 results.append({'Case':name,'ExitCode':p.returncode,'Expected':'Reject before starting any application','PASS':True,'Seconds':round(time.time()-t,2)})
for i in range(1,3):
 t=time.time()
 # Exercise the exact double-click entry; each run is a fresh PowerShell + native probe process.
 p=subprocess.run([os.environ['COMSPEC'],'/d','/c',str(release/'Launch.cmd'),'-VerifyGpu','-RecordDirectory',str(root/'GPU验证')],stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=60)
 (root/('entry-gpu-'+str(i)+'.log')).write_bytes(p.stdout)
 assert p.returncode==0,p.stdout.decode('mbcs',errors='replace')
 results.append({'Case':'Launch.cmd GPU fresh process '+str(i),'ExitCode':p.returncode,'PASS':True,'Seconds':round(time.time()-t,2)})
assert env_before==dict(os.environ),'Parent process environment changed'
(root/'entry-tests.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(results,ensure_ascii=True),flush=True)
