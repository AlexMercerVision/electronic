# 相机连接只读同步（2026-10-14）

启动、工具栏连接、图像采集菜单连接及自动重连只读取并同步当前设备成像参数，不恢复项目保存的曝光、增益、Gamma、PixelFormat、分辨率和 ROI。已连接设备重复点击连接保留句柄并刷新读取值。采集模式、触发控制和必要 SDK 启动采集保持既有业务语义。

Gamma“使能/用户”及锐度控件隐藏、禁用、解绑，锐度不参与读取和批量设置。曝光、增益、Gamma数值及光源原操作入口保留；Gamma数值写入取消无条件 GammaAuto。彩色候选及原有 Mono8 回退保留，中间失败不记录 SDK失败事件，任一输出成功就成功，全部失败才报告。

主动断开等预览线程退出后释放设备和缓冲，Close/Destroy异常不伪造断开成功；暂停自动重连到下一次手动连接。保存的自动重连配置未修改。

业务文件：CommonCls/HIKDeviceSource.cs、VisionApplication/FrmMainUI.cs、VisionApplication/FrmMainUIExtention.cs。备份在 backup，历史说明在 PROJECT_CONTEXT.md；Designer、工程、AcqFIFO、VisionControlExtention、现场配置/模型/VPP未修改。

最终验证：真实 VisionBasedLaser.sln，Debug/x64，全部11项目完整 Build，退出码0，最终完整 Build 0错误、16既有警告（首次全量构建83警告）。首次沙箱运行在BJJCZ x86资源宿主受阻后取消，经批准沙箱外同配置完成；输出/中间目录仍隔离，未替换项目引用。11个编译器命令已核对实际依赖路径；CommonCls=1.0.0.0，xbVP_Control=1.0.9783.32590，主程序引用与本批 DLL一致。基础依赖及bindingRedirect符合AGENTS.md。公共bin 276文件修改0、新增0；另核对工程与关键未修改源码哈希。

42项提取实际方法的行为检查通过；详见 behaviour-checks.txt、compiler-references.json、artifact-verification.json、solution-build-utf8.log。未启动主程序或连接相机。完整构建与假SDK不代表现场故障已解决。

camera-readonly-programs.zip包含同批以下六个文件，尚未部署：

1. CommonCls.dll / CommonCls.pdb
2. 激光除胶.exe / 激光除胶.pdb
3. xbVP_Control.dll / xbVP_Control.pdb

现场验证：MVS按序列号记录曝光/自动曝光、增益、Gamma、PixelFormat、宽高和偏移；释放MVS设备连接后启动本批程序，核对两工位映射及界面读取值，退出后再用MVS核对成像节点。自动曝光开启时核对模式保持，数值允许随场景变化。测试两个连接入口、断开后等待超过30秒保持断开、手动再次连接，以及两路预览至少60秒、单次采集、原有主动曝光/增益/Gamma/光源操作。锐度及Gamma扩展复选框应不可见。相同相机参数下若MVS和项目仍有色彩差异，应再检查显示/去马赛克处理，不能继续通过连接时改相机参数来补偿。
