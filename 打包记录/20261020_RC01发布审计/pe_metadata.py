import struct
from pathlib import Path
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')
class PE:
 def __init__(self,p):
  self.b=Path(p).read_bytes();b=self.b;h=u(b,60);assert b[h:h+4]==b'PE\0\0';self.machine=u(b,h+4,2)
  opt=h+24;self.dirs=opt+(112 if u(b,opt,2)==523 else 96);self.sections=[]
  sh=opt+u(b,h+20,2)
  for i in range(u(b,h+6,2)):
   o=sh+i*40;self.sections.append((u(b,o+12),max(u(b,o+8),u(b,o+16)),u(b,o+20)))
 def off(self,rva):
  for r,n,p in self.sections:
   if r<=rva<r+n:return p+rva-r
  return rva
 def z(self,o):return self.b[o:self.b.index(b'\0',o)].decode('utf-8',errors='replace')
 def imports(self):
  r=u(self.b,self.dirs+8)
  if not r:return []
  o=self.off(r);names=[]
  while any(self.b[o:o+20]):
   names.append(self.z(self.off(u(self.b,o+12))));o+=20
  return names
 def managed(self):
  b=self.b;r=u(b,self.dirs+14*8)
  if not r:return None
  cli=self.off(r);root=self.off(u(b,cli+8));assert b[root:root+4]==b'BSJB'
  o=root+16+u(b,root+12);o=(o+3)&~3;streams=u(b,o+2,2);o+=4;heaps={}
  for i in range(streams):
   off,size=u(b,o),u(b,o+4);name=self.z(o+8);heaps[name]=(root+off,size);o=(o+8+len(name)+1+3)&~3
  t=heaps.get('#~',heaps.get('#-'))[0];hs=b[t+6];valid=u(b,t+8,8);o=t+24;rows={}
  for i in range(64):
   if valid>>i&1:rows[i]=u(b,o);o+=4
  strsz=4 if hs&1 else 2;blobsz=4 if hs&4 else 2;guidsz=4 if hs&2 else 2
  def idx(n):return 4 if rows.get(n,0)>=65536 else 2
  def coded(bits,ts):return 4 if max([rows.get(n,0) for n in ts])>=1<<(16-bits) else 2
  td=coded(2,[2,1,27]);resolution=coded(2,[0,26,35,1]);hascustom=coded(5,[6,4,1,2,8,9,10,0,14,23,20,17,26,27,32,35,38,39,40,42,44,43])
  sizes=[2+strsz+3*guidsz,resolution+2*strsz,4+2*strsz+td+idx(4)+idx(6),idx(4),2+strsz+blobsz,idx(6),8+strsz+blobsz+idx(8),idx(8),4+strsz,idx(2)+td,coded(3,[2,1,26,6,27])+strsz+blobsz,2+coded(2,[4,8,23])+blobsz,hascustom+coded(3,[6,10])+blobsz,coded(1,[4,8])+blobsz,2+coded(2,[2,6,32])+blobsz,6+idx(2),4+idx(4),blobsz,idx(2)+idx(20),idx(20),2+strsz+td,idx(2)+idx(23),idx(23),2+strsz+blobsz,2+idx(6)+coded(1,[20,23]),idx(2)+2*coded(1,[6,10]),strsz,blobsz,2+coded(1,[4,6])+strsz+idx(26),4+idx(4),8,4,16+blobsz+2*strsz,4,12,12+2*blobsz+2*strsz]
  offsets={}
  for i in range(36):offsets[i]=o;o+=rows.get(i,0)*sizes[i]
  strings=heaps['#Strings'][0]
  def string(o):return self.z(strings+u(b,o,strsz))
  a=offsets[32];name=string(a+16+blobsz);v='.'.join(str(u(b,a+4+j*2,2)) for j in range(4));refs=[]
  framework={'mscorlib','netstandard','Microsoft.CSharp','System','System.Core','System.Data','System.Drawing','System.Windows.Forms','System.Xml','System.Xml.Linq','System.Management','System.ServiceProcess','System.Deployment','System.Configuration','System.Numerics','System.Runtime.Serialization','System.Design','System.Web','System.Net.Http','System.ComponentModel.DataAnnotations','System.Security','System.Data.DataSetExtensions','System.Runtime.Remoting','System.Transactions','WindowsBase','PresentationCore','PresentationFramework','System.Xaml'}
  # Framework facade files exist under the installed target framework; NuGet-only assemblies must be supplied.
  reference=Path(r'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2')
  framework.update(p.stem for p in reference.rglob('*.dll'))
  for i in range(rows.get(35,0)):
   a=offsets[35]+i*sizes[35];n=string(a+12+blobsz)
   refs.append({'Name':n,'Version':'.'.join(str(u(b,a+j*2,2)) for j in range(4)),'Framework':n in framework})
  return {'Name':name,'Version':v,'References':refs}
if __name__=='__main__':
 import sys,json
 rows=[]
 for p in Path(sys.argv[1]).iterdir():
  if p.suffix.lower() not in ['.dll','.exe']:continue
  pe=PE(p);m=pe.managed()
  if m:rows.append({'File':p.name,'Path':str(p),**m})
 Path(sys.argv[2]).write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
 print('Parsed assembly metadata:',len(rows))
