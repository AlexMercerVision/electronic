from pathlib import Path
import os,json,hashlib,shutil
from pe_metadata import PE
base=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01');a=base/'04_Audit';app=base/'03_Release/App'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
loaded=[Path(x) for x in (a/'gpu-loaded-modules.txt').read_text().splitlines()]
records=json.loads((a/'payload-provenance.json').read_text(encoding='utf-8'))
mkl=[p for p in loaded if p.name.lower()=='mklml.dll'];assert len(mkl)==1
p=mkl[0];q=app/p.name;assert not q.exists();shutil.copy2(p,q);assert sha(p)==sha(q)
records.append({'Source':str(p),'Destination':p.name,'Bytes':q.stat().st_size,'SHA256':sha(q),'Reason':'Actually loaded by current GPU native inference; removes Python environment dependency'})
(a/'payload-provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
rows=[]
for p in loaded:
 if str(p).lower().startswith(str(app).lower()):continue
 if str(p).lower().startswith('c:\\windows') and not p.name.lower().startswith(('nv','vcomp','vcruntime','msvcp')):continue
 pe=PE(p)
 rows.append({'Source':str(p),'File':p.name,'Bytes':p.stat().st_size,'SHA256':sha(p),'Architecture':hex(pe.machine),'Imports':pe.imports(),'Delivery':'App' if p.name.lower()=='mklml.dll' else 'External installed runtime / driver; DLL not copied'})
(a/'gpu-runtime-lock.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
imports=[]
for p in app.rglob('*.dll'):
 pe=PE(p)
 if pe.managed() is None:imports.append({'File':str(p.relative_to(app)),'Architecture':hex(pe.machine),'Imports':pe.imports()})
(a/'native-imports.json').write_text(json.dumps(imports,ensure_ascii=False,indent=2),encoding='utf-8')
print('Added actually loaded mklml.dll; froze '+str(len(rows))+' external/runtime modules')
