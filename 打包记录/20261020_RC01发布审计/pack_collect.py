from pathlib import Path
import json,shutil,hashlib,re,csv
base=Path(r'E:\VB_Det_PACK\20261020_50Series_RC01'); src=Path(r'E:\jieyawenjian\VB_Det'); out=base/'02_Build/Output'; audit=base/'04_Audit'; app=base/'03_Release/App'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
meta=json.loads((audit/'assembly-metadata.json').read_text(encoding='utf-8-sig'))
byname={m['Name']:m for m in meta}; vpp=Path(r'D:\data\LD.vpp')
assert sha(vpp)=='84B0B16D5AA2587581BA1464709086C1541F1549B655A3EA949D87EA8A90FC86'
vppnames=sorted(set(x.decode('ascii') for x in re.findall(rb'Cognex[\w.]+(?=, Version=)',vpp.read_bytes())))
queue=['电极帽缺陷检测','System.Memory','System.Buffers','System.Numerics.Vectors','System.Runtime.CompilerServices.Unsafe','System.Resources.Extensions']+vppnames
selected={}; missing=[]
while queue:
 n=queue.pop(0)
 if n in selected:continue
 if n not in byname:raise RuntimeError('Missing runtime assembly '+n)
 m=byname[n];selected[n]=m
 for r in m['References']:
  if r['Name'] in byname:queue.append(r['Name'])
  elif not r['Framework']:missing.append({'From':n,**r})
external=[]
for r in missing:
 if not r['From'].startswith('Cognex'):raise RuntimeError('Missing application dependency '+str(r))
 candidates=[]
 for root in [Path(r'C:\Windows\assembly\GAC_MSIL'),Path(r'C:\Windows\Microsoft.NET\assembly\GAC_MSIL'),Path(r'C:\Windows\Microsoft.NET\assembly\GAC_64')]:
  d=root/r['Name']
  if d.exists():candidates+=list(d.rglob(r['Name']+'.dll'))
 for root in [Path(r'E:\VP9.0\VisionPro'),Path(r'E:\VPADD\VisionPro')]:
  candidates+=list(root.rglob(r['Name']+'.dll'))
 installed=[]
 from pe_metadata import PE
 for p in candidates:
  try:
   m=PE(p).managed()
   if m and m['Version']==r['Version']:installed.append(str(p))
  except Exception:pass
 if not installed:raise RuntimeError('Vendor dependency not found on current PC '+str(r))
 external.append({**r,'Provider':'VisionPro matching runtime installer / GAC','InstalledMatchingPaths':installed,'Shipped':False})
records=[]
def cp(p,rel,reason):
 q=app/rel;q.parent.mkdir(parents=True,exist_ok=True)
 if q.exists():raise RuntimeError('No overwrite '+str(q))
 shutil.copy2(p,q)
 assert sha(p)==sha(q)
 records.append({'Source':str(p),'Destination':str(rel).replace('\\','/'),'Bytes':q.stat().st_size,'SHA256':sha(q),'Reason':reason})
for n,m in sorted(selected.items()):cp(Path(m['Path']),m['File'],'Build output / managed or VPP dependency')
cp(out/'电极帽缺陷检测.exe.config','电极帽缺陷检测.exe.config','Build-generated runtime configuration')
for n in ['电极帽缺陷检测','CommonCls','xbVP_Control','PipeChanel']:
 p=out/(n+'.pdb');shutil.copy2(p,base/'05_Symbols'/p.name)
for n in ['model_infer.dll','paddle_inference_c.dll','phi.dll','common.dll','mkldnn.dll','libiomp5md.dll']:
 cp(src/'bin'/n,n,'Current model native PE import closure')
cp(src/'bin/dll/x64/OpenCvSharpExtern.dll','dll/x64/OpenCvSharpExtern.dll','Exact existing OpenCvSharp wrapper native backend')
cp(vpp,'VPP/LD.vpp','User-selected actual-running VPP (sole version)')
for p in Path(r'D:\data\inference_model').iterdir():
 if p.is_file():cp(p,'Assets/Models/Default/inference_model/'+p.name,'Actual-running model')
# XML output template is used through the working directory; retain its actual bytes.
cp(src/'bin/NullMark_00.xml','NullMark_00.xml','CDL XML annotation template, data fields overwritten by code')
(audit/'payload-provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
(audit/'managed-closure.json').write_text(json.dumps({'Selected':list(selected.values()),'VppAssemblies':vppnames,'ExternalVendorDependencies':external,'MissingApplicationDependencies':[],'ExcludedBuildFiles':[p.name for p in out.iterdir() if p.is_file() and p.name not in [r['Destination'] for r in records]]},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'AppFiles':len(records),'AppMiB':round(sum(r['Bytes'] for r in records)/1048576,1),'ManagedAssemblies':len(selected)},ensure_ascii=False))
