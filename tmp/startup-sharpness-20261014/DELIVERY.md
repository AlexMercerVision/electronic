# 启动串口状态与可选锐度修复（2026-10-14）

修改 CommonCls/HIKDeviceSource.cs、VisionApplication/FrmMainUI.cs，并在 PROJECT_CONTEXT.md 记录依据和验证。原版参考为 D:/E盘备份文件/VB_Det；未复制旧业务文件或旧程序集。

- 读取历史 RTS 后立即将本轮 bSerInit 重置为 false，由 InitSerial 根据本轮串口打开结果赋值。光源槽、COM 映射和历史统计保持。
- 启动及重连的参数恢复检查设备连接、Sharpness 节点访问状态和 SharpnessEnable。确认节点未实现、不可用、只读，或功能关闭时跳过可选锐度，不写入其值、不保存失败结果，不主动开启功能。可写且启用的锐度继续设置，不可用的 SharpnessAuto 不写入；真实范围读取、最终写入及设备断开故障保留。查询能力不明确时保留原调用流程。原两参数接口和 UI 显式设置语义保持。

最终验证对象：真实 VisionBasedLaser.sln，Debug/x64，/t:Build，全部11个项目。MSBuild 18.6.3，退出码0，0错误、83警告。原 BJJCZ 项目的 x86 平台保持，沙箱资源宿主启动受阻后，经批准在沙箱外完成完整 Build。输出和中间目录隔离，工程及引用未替换；ReferencePath 指向本轮 build，全部11个编译器命令已核对实际依赖路径。

17项提取实际方法的行为检查通过。主程序/控制程序集引用 CommonCls 1.0.0.0；主程序引用 xbVP_Control 1.0.9783.28265，与本轮产物一致。基础依赖实际版本：System.Memory 4.0.1.2、System.Buffers 4.0.3.0、System.Numerics.Vectors 4.1.4.0、System.Runtime.CompilerServices.Unsafe 6.0.0.0。详情见 artifact-verification.json、compiler-references.json、solution-build-utf8.log 和 behaviour-checks.txt。

公共 bin 检查274文件：修改0、新增0。配置、模型、VPP、工程、Designer、AcqFIFO、VisionControlExtention 未覆盖。配套程序集和 PDB 打包在 startup-sharpness-programs.zip，尚未部署到公共 bin。应配套使用以下六个文件，不覆盖其他运行文件：

1. CommonCls.dll / CommonCls.pdb
2. 激光除胶.exe / 激光除胶.pdb
3. xbVP_Control.dll / xbVP_Control.pdb

当前 SDK 只读枚举返回成功、设备数0，未取得真实锐度节点/使能状态。未运行主程序、新版相机采集或光源物理输出；完整 Build 和假 SDK 检查不代表现场故障已修复。

现场验证：关闭旧进程后使用本轮配套产物，核对 Job0/Job1 序列号及两路 StartGrabbing；冷启动光源不应再提前出现 unknown not-open，串口成功仍需分别确认实际亮度命令与物理输出。双路预览至少60秒，核对首帧和后续取帧/显示计数持续增长，再停止并重启预览；两路断开后接回，执行一次热连接确认2/2成功。不可用/关闭锐度应跳过且无失败记录，可用锐度应实际写入；真实故障应保留明确节点与错误码。
