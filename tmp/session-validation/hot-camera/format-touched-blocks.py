from pathlib import Path
def indent(path, begin, end, spaces):
    p=Path(path)
    raw=p.read_bytes()
    bom=raw.startswith(b'\xef\xbb\xbf')
    s=raw.decode('utf-8-sig').replace('\r\n','\n')
    a=s.index(begin)
    b=s.index(end,a)
    block=s[a:b]
    s=s[:a]+''.join((' '*spaces+line if line.strip() else line) for line in block.splitlines(True))+s[b:]
    p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+s.replace('\n','\r\n').encode('utf-8'))
indent('VisionApplication/FrmMainUI.cs','            EnsureCameraSerialBindingArray();\n            string expectedSerial','            }\n            catch (Exception ex)\n            {\n                LogCameraConnection(operation, false,\n                    $"Job{jobIndex}',4)
indent('VisionControl/VisionControlExtention.cs','            // 根据配置设置镜像（在开始预览前）','                if (!acqFIFO.TryReservePreview',4)
indent('VisionControl/VisionControlExtention.cs','                    uint frameLen = stFrameInfo.stFrameInfo.nFrameLen;','                            }\n                            finally\n                            {\n                                int freeRet',12)
