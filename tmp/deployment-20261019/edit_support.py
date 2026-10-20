from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def read(name):
    data=(ROOT/name).read_bytes()
    try: return data.decode('utf-8-sig').replace('\r\n','\n')
    except UnicodeDecodeError: return data.decode('gb18030').replace('\r\n','\n')
def write(name,text):
    p=ROOT/name
    data=p.read_bytes()
    prefix=b'\xef\xbb\xbf' if data.startswith(b'\xef\xbb\xbf') else b''
    try: data.decode('utf-8-sig'); encoding='utf-8'
    except UnicodeDecodeError: encoding='gb18030'
    p.write_bytes(prefix+text.replace('\r\n','\n').replace('\n','\r\n').encode(encoding))
def replace(text,old,new,count=1):
    assert text.count(old)==count,(old[:100],text.count(old),count)
    return text.replace(old,new)
