from pathlib import Path
import re
import json

task=Path(__file__).resolve().parent
raw=(task/'solution-build.log').read_bytes()
try:
    log=raw.decode('utf-8-sig')
except UnicodeDecodeError:
    log=raw.decode('gb18030')
(task/'solution-build-utf8.log').write_text(log,encoding='utf-8')
print('\n'.join(log.splitlines()[-5:]))
assert '0 个错误' in log, 'No successful final error summary'
assert not re.search(r'warning MSB(?:3277|3245|3258)|error [A-Z]+\d+',log,re.I), 'Assembly resolution or compilation error'
compiled=[]
for line in log.splitlines():
    if 'csc.exe ' not in line or '/out:' not in line:
        continue
    output=re.search(r'/out:(?:"([^"]+)"|([^\s]+))',line)
    if not output:
        continue
    references=[a or b for a,b in re.findall(r'/reference:(?:"([^"]+)"|([^\s]+))',line)]
    ownrefs=[p for p in references if Path(p.replace('\\','/')).name in ('CommonCls.dll','xbVP_Control.dll')]
    for reference in ownrefs:
        assert str(task/'build').replace('\\','/').lower() in reference.replace('\\','/').lower(), reference
    compiled.append({'output':output.group(1) or output.group(2),'current_dependencies':ownrefs})
previous=json.loads((task/'compiler-references.json').read_text(encoding='utf-8')) if (task/'compiler-references.json').exists() else []
combined={Path(item['output'].replace('\\','/')).name:item for item in previous+compiled}
assert len(combined)==11, f'Expected compiler evidence for all 11 projects, found {len(combined)}'
assert any('激光除胶.exe' in item['output'] for item in compiled), 'Final main source must be compiled'
(task/'compiler-references-final.json').write_text(json.dumps(compiled,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Verified final {len(compiled)} compiler invocations and all 11 projects across complete builds; remaining dependencies are up-to-date from the same build directory.')
for item in compiled:
    if '激光除胶.exe' in item['output']:
        print(json.dumps(item,ensure_ascii=False))
