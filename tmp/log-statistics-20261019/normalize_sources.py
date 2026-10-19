from pathlib import Path
import subprocess
root = Path(__file__).resolve().parents[2]
for name in ['CommonCls/CDL.cs','CommonCls/CUserParam.cs','VisionApplication/FrmMainUI.cs',
             'VisionApplication/FrmMain_Laser.cs','VisionControl/VisionControl.cs','VisionControl/VisionControlExtention.cs']:
    original = subprocess.check_output(['E:/Git/cmd/git.exe', '-c', 'safe.directory=E:/jieyawenjian/VB_Det', 'show', 'HEAD:'+name], cwd=root)
    target = root/name
    current = target.read_text(encoding='utf-8-sig')
    # Preserve the original leading blank line and BOM conventions.
    first = original.decode('utf-8-sig').splitlines()[0]
    if not first.strip(): current = first + '\n' + current.split('\n',1)[1]
    target.write_text(current, encoding='utf-8-sig' if original.startswith(b'\xef\xbb\xbf') else 'utf-8', newline='\r\n')
