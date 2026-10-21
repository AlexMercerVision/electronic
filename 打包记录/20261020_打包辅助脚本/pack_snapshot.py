from pathlib import Path
import os, re, json, hashlib, shutil, subprocess, datetime, xml.etree.ElementTree as ET
src=Path(r'E:\jieyawenjian\VB_Det'); base=Path(r'E:\VB_Det_PACK')
assert base.resolve()!=src.resolve() and src.resolve() not in base.resolve().parents
batch=base/'20261020_50Series_RC01'
if batch.exists(): raise SystemExit('Batch exists; refusing overwrite: '+str(batch))
batch.mkdir(parents=True)
snap=batch/'01_SourceSnapshot'; audit=batch/'04_Audit'
for p in [snap,audit,batch/'02_Build/Output',batch/'02_Build/Obj',batch/'03_Release/App',batch/'03_Release/Prerequisites',batch/'03_Release/Documents',batch/'05_Symbols']:
 p.mkdir(parents=True,exist_ok=True)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
records=[]
def cp(p):
 rel=p.relative_to(src); q=snap/rel
 if q.exists():return
 q.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(p,q)
 assert sha(p)==sha(q),str(p)
 records.append({'Source':str(p),'Snapshot':str(rel),'Bytes':p.stat().st_size,'SHA256':sha(q)})
sln=src/'VisionDection.sln'
projects=[src/p for p in re.findall(r'"([^"\r\n]+\.csproj)"',sln.read_text(encoding='utf-8-sig'))]
assert len(projects)==11
state=subprocess.check_output([r'E:\Git\cmd\git.exe','-c','safe.directory='+str(src).replace('\\','/'),'status','--porcelain=v1'],cwd=src).decode('utf-8')
(audit/'source-state.txt').write_text(state,encoding='utf-8')
protected=list((src/'bin').rglob('*'))+list(src.rglob('*.Designer.cs'))+list(src.rglob('*.designer.cs'))+[sln,src/'AGENTS.md',src/'PROJECT_CONTEXT.md',Path(r'D:\data\LD.vpp')]+list(Path(r'D:\data\inference_model').rglob('*'))
protected=sorted(set(p for p in protected if p.is_file()))
(audit/'original-before.json').write_text(json.dumps([{'Path':str(p),'Bytes':p.stat().st_size,'SHA256':sha(p)} for p in protected],ensure_ascii=False,indent=2),encoding='utf-8')
excluded={'bin','obj','.vs','.git','UserData','Log','Logs','publish','app.publish'}
for folder in set(p.parent for p in projects):
 for root,dirs,files in os.walk(folder):
  dirs[:]=[d for d in dirs if d not in excluded]
  for n in files:
   if n.endswith(('.user','.suo','.pfx','.pdb','.exe','.bak')):continue
   cp(Path(root)/n)
for name in ['VisionDection.sln','Directory.Build.props','AGENTS.md','PROJECT_CONTEXT.md'] :cp(src/name)
# Preserve package imports and package compile inputs separately from customer payload.
needed=set()
for p in projects:
 text=p.read_text(encoding='utf-8-sig')
 needed.update(re.findall(r'packages\\([^\\]+)',text))
 for hint in ET.parse(p).getroot().iter():
  if hint.tag.rsplit('}',1)[-1]=='HintPath' and hint.text:
   q=(p.parent/hint.text).resolve()
   if q.is_file() and src in q.parents and q.name not in ['CommonCls.dll','xbVP_Control.dll','PipeChanel.dll']: cp(q)
for n in needed:
 d=src/'packages'/n
 if d.exists():
  for p in d.rglob('*'):
   if p.is_file():cp(p)
for p in (src/'bin').glob('Cognex*.dll'):cp(p)
cp(src/'LaserIOUI/System.Resources.Extensions.dll')
# The original Directory.Build.props uses this resource reader, not the .NET5 application's other files.
for p in (src/'dll').glob('*.dll'):cp(p)
(audit/'source-snapshot-manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
(audit/'batch.json').write_text(json.dumps({'Batch':str(batch),'Original':str(src),'Solution':'VisionDection.sln','Configuration':'Debug/x64','Projects':[str(p.relative_to(src)) for p in projects],'VPP':r'D:\data\LD.vpp','VppSHA256':sha(Path(r'D:\data\LD.vpp'))},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'Batch':str(batch),'SnapshotFiles':len(records),'SnapshotMiB':round(sum(x['Bytes'] for x in records)/1048576,1),'ProtectedFiles':len(protected)},ensure_ascii=False))
