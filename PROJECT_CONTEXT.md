# 项目上下文（精简版）

## 当前结论（2026-10-09）

### xbDet（`VisionApplication/FrmDL.cs`）

- `numericUpDown1` 显示“推理间隔(ms)”，`ValueChanged` 写入 `continue_infer_delay`，但 `FileInferThread()` 从未使用该字段，也没有 `Thread.Sleep`；文件夹连续推理会逐张快速跑完，表现为“启动后一闪而过”。相机实时循环使用 `numericUpDown1.Value`，因此间隔只对相机模式有效。
- 三个勾选项实际含义和状态：
  - “保存图片”：事件只设置 `bSave`；`FrmDL.cs` 内没有读取 `bSave` 的保存图片代码，当前无效。
  - “保存缺陷样本标注结果”：`bSaveXml` 为真且检测到框时，`inferSetImage()` 调用 `CreateRemarkXML_En(...)` 输出 XML，当前有效（前提是保存路径和模板文件存在）。
  - “保存负样本标注结果”：`bSavePosiXml` 为真且没有框时输出 XML，当前有效（命名沿代码原文，界面文字为“负样本”）。
- 这三项只在窗体实例内生效，没有看到持久化到 `CUserParam`；本轮未修改。

### CPU/GPU

- `FrmDL` 的“运行环境”组合框才是 CPU/GPU 选择；改变时只更新内存中的 `use_gpu`，模型已初始化、推理或采集时拒绝改变。点击“模型初始化”才将 `use_gpu` 写入 `dlParam.bUseGPU` 并调用 `CDL.InitDL()`。
- 主项目启动/切换深度推理时使用保存的 `cup.p_DL.bUseGPU`，通过 `CDL.InitDL → model_infer.dll!InitModel(..., use_gpu, gpuID, ...)` 传入；主界面菜单“电极帽缺陷检测/尺寸测量”切的是推理模式，不是 CPU/GPU。
- 当前同一套 `bin` 环境日志已同时验证初始化：2026-10-08 的 `useGPU=False` 和 2026-10-09 的 `useGPU=True, gpuID=0` 均返回 `InitModel success`。因此换上 GPU 运行环境后仍能选择 CPU 初始化；但 CPU 完整推理结果和性能仍需现场实测，不能只凭初始化成功断言。
- `FrmMainUI.designer.cs` 中 `menuTool_infer` 只有文本“深度推理”，没有 Click 事件；不要把它当作已实现的 CPU/GPU 设置入口。

## 仍需现场验证

- GPU/CPU 各自完整运行一次，核对 `dl_init_debug.log`、推理耗时、结果一致性和显存/CPU 占用；切换后必须先销毁再初始化模型。
- xbDet 文件连续推理需用多张图片验证“是否按期望暂停/完成”；当前间隔未接入文件循环，若要修复需另行确认期望语义后再改业务代码。
- 两个 XML 输出项需验证保存目录、`NullMark_00.xml` 和输出文件是否实际生成；“保存图片”若要启用，需先确认保存格式、目录和文件名规则。

## 已完成修复（归档摘要）

- 2026-09：统一三项目基础托管依赖和 bindingRedirect；分别 Debug 构建成功，公共 `bin` 版本与配置一致。
- 2026-09：完成相机单帧缓冲区生命周期、Bayer 枚举转换、按工位显示清理、相机/光源索引解耦、A/B/同步采集显示语义修正；代码构建成功，真实硬件验证仍需现场完成。
- 2026-09：VPP 路径/长度接口检查、单次运行完成时序、Job0/Job1 组合 IO 位图和串口写锁已实现；控制器响应、LED/继电器和现场输入仍未替代静态分析。

## 已知风险（保留）

- `ClientTools.csproj` 不在主解决方案且仍可能输出旧依赖，专项统一前不要单独构建。
- IO 主动上报 `0x45` 的 10/25 字节格式、输入偏移和 CRC 尚未用现场抓包最终确认；COM 打开、协议有效、业务触发、物理动作必须分层判断。
- 相机序列号与物理槽/工位绑定的长期整理尚未实施；非零旋转/镜像时原始缓存与显示方向差异也未扩大处理。

## P0 现场问题与本轮修复（2026-10-12）

- 现场四次冷启动约三次出现 A 面黑屏、状态栏“部分相机连接成功”、`视觉工位1异常` 与 `启动相机采集-Cam1` 失败；截图中的 PMA/精定位错误必须与相机启动错误分开判读。
- 已确认的启动越界缺陷：`FrmMainUI.InitCam()` 在发现 `cup.p_CD.camInd[ind]` 超出当前 `cameraNum` 后仅设置状态变化，随后仍直接访问 `camera2[iCamInd]` 和 `rts.cs.iStatus[iCamInd]`，可能抛 `IndexOutOfRangeException` 并进入全局清理，影响另一工位。已在访问前增加边界检查，单个无效工位只记录并跳过；`CheckCamPrepared()` 同步增加状态数组边界检查。
- 已将 `InitCam`、`StartGrabbing`、PMA 参数和精定位异常改为记录完整 `Exception.ToString()`、工位索引、数组长度和初始化阶段。PMA 在源码中指 Cognex PMAlign 模块，当前证据表明其错误来自 `VisionControlExtention.SetPMARunParam/GetPMAParam/SetUseTuneLocation` 的异常包装，不等于相机 SDK 失败。
- 本轮未改动预览与检测并发取帧、序列号映射/对象槽位、关闭线程等待三项风险；它们分别仍需现场日志确认运行时取帧、重连顺序和关闭阶段影响。
- 验证：`CommonCls` Build 通过，`VisionControl` Build 通过；当前环境缺少可用完整 MSBuild，`VisionApplication.csproj` 未能获得有效编译诊断，主程序 Build 不判定为通过。未启动 EXE，未连接真实硬件。

## PMA 与视觉工位异常修复（2026-10-12）

- `视觉工位1异常` 的唯一界面生成点是 `FrmMainUI.OnVisionErrorFromVP`；`jobIndex=0` 对应 A 面及配置中的物理相机0（当前 `serUserParam.json` 为 `DA8696070`）。它是 VisionControl 错误事件的统一显示，不等同于相机连接失败。
- PMA 越界根因是 `VisionControlExtention.SetPMARunParam/GetPMAParam` 直接使用固定长度为2的 `pma[jobIndex]`，而 UI 的“全部工位”会传 `-1`，预留工位会传 `2`。现已在 Job、PMA 数组、ToolGroup、`CogPMAlignTool1` 四层校验；数组未扩容，工具为空和工位未初始化分别报告。
- PMA 参数、模板、测试图像及精定位 UI 入口均拒绝未启用单工位；`InitParamUI`/工位切换仅在有效 Job0/Job1 读取参数。无效 `jobIndex` 的视觉回调显示为“视觉操作异常”，避免把 `-1` 误标为工位1。
- 静态验证：`VisionControl.csproj` 使用 `dotnet msbuild` 通过；主项目无法由当前 dotnet MSBuild 完成旧式项目引用解析，未获得有效源码编译诊断。未启动 EXE，未连接真实设备；相机流程和“部分相机连接成功”提示未改动。

## 双工位回放长度测量修复（2026-10-13）

- 现场 `D:/data/Log/ErrorLog_20261013.txt` 多次记录 Job0 超时后取消同步 IO 批次。原 `RunOneTimeThread(bool)` 随即退出工位循环，Job1 未进入检测；超时后仍更新完成日志和统计，截图中的 A 面长度不保证属于该轮。
- `VisionControl.HandleUserResults()` 取到空队列后原有 20×100ms 等待没有重新取结果，现改为空队列立即返回，保留后续 UserResultAvailable 事件处理以及结果处理完成/Job Stopped 双条件完成机制。
- `FrmMainUI.RunOneTimeThread(bool)` 的 VPP 路径不再提前清零完成标志，交由检测入口在检查/恢复 busy 后初始化；同步长度测量逐工位执行，一路启动失败/超时或 IO 批次拒绝不阻断另一路检测。取消的组合 IO 批次在该轮不重建、不提交后续工位结果，保持完整批次输出语义；超时的测量工位不记录完成或更新统计。测量算法、超时配置、缺陷检测成功路径及 Designer 未改动。
- 验证：基于实际运行方法和 busy 初始化方法编译的隔离桩验证通过 9 项场景，覆盖双成功、A/B 启动失败/超时、A busy、超时后恢复、IO 禁用/拒绝及缺陷成功路径。`VisionControl` 在指定现有 bin 引用路径后隔离 Build 通过；主项目仍在旧式项目引用解析阶段失败，未获得完整 Build 结果。未启动 EXE 或执行实际 Measure.vpp，需现场连续回放确认 A/B 各自本轮结果及 IO 批次输出，首次超时的事件耗时仍需现场记录。

## 回放、IO竞争及长度结果/参数修复（2026-10-13）

- 当前映射 Job0→Cam1、Job1→Cam0。修复 AcqFIFO 文件回放中工位目录与物理相机缓存索引混用；图像格式、原图及字节缓存统一按物理相机槽读写，离线推理与 VPP 输入同步使用该槽。映射配置未修改。
- 手动单次运行增加所属线程完成标志，结果回调不再通过跨轮累计计数提前调用 FinalizeRun；每轮/新IO批次初始化计数。IO入口仅在成功加入后返回可提交/取消的批次令牌，拒绝请求不取消他人批次；输出期间不由取消路径释放，发送退出时在 finally 释放批次。位图、通道、IOTimeDelay、bUpdateIO 配置未改动。
- VPP分别记录处理完成与测量有效。验证工具运行状态、当前模式工具启用状态、输出类型、有限非负长度及动态lenTh判定一致性；失败清除本轮缓存中的旧OK/长度，保留完成信号。结果确认后在UI线程切换显示，最后释放工位等待线程；显示先于IO提交。无效测量不提交IO、不记录正常完成或统计。上层超时不得直接清除仍待结果/Stopped汇合的VPP busy。
- 长度参数Get/Set返回真实成功状态；获取读取当前Job的lenTh，设置后读回核对。只有读写及界面同步成功才记录成功，检测期间拒绝修改该Job参数。未修改SaveVPP、持久化、Designer、LD.vpp、测量脚本或模型。
- 验证：CommonCls、VisionControl隔离Build通过；主程序使用本轮编译依赖和仅供验证的外置targets完成Compile，完整Build仍退出失败且未给出有效错误诊断。构建输出位于tmp/session-validation，未覆盖公共bin。未启动EXE、未执行现场VPP或连接硬件；冷启动回放、连续触发/PLC脉冲时序、动态lenTh边界及参数会话效果均需现场验证。

## 双 USB3 热连接与预览可靠性（2026-10-13）

- 现场错误日志确认：DA8696070、DA8696075 均在 Create/Open 后、StartGrabbing 前发生参数恢复失败；清理 Stop=0x80000003 不是参数节点的原始错误。后续 Job0/Cam0/DA8696070 多次 GetImageBuffer=0x80000007，仅能确认无数据，不能判定 USB 带宽不足或缺少触发。
- 修改范围限于 CommonCls/HIKDeviceSource.cs、CommonCls/AcqFIFO.cs、VisionApplication/FrmMainUI.cs、VisionControl/VisionControlExtention.cs。参数恢复使用结构体及 ROI 数组的局部副本，全部成功才提交；SDK 范围读取失败立即返回，逐节点保留目标值、原始返回码、索引/序列号及异常堆栈。AcquisitionMode/TriggerMode 写入检查返回码，参数恢复和启动后读回采集/触发状态；未完成参数和缓冲初始化不得启动采集，热连接整体成功要求有效映射且所有启用工位成功。
- 预览停止确认 Join 退出后才清空引用或释放缓冲；超时保留资源并拒绝重启/重连，增量连接及 InitCam 都检查旧预览线程。同路预览使用生命周期锁和物理相机占用；常规灰度/彩色单帧入口拒绝预览占用，预览与这两入口共用取帧锁。Get 成功后的显示/翻转置于 try/finally，检查 Free 返回码；非无数据错误、设备断开或异常记录原因后退出，不强制终止线程。
- 新增运行期 CameraLog_yyyyMMdd.txt（使用现有日志目录）：持久化成功/失败的连接与 StartGrabbing、采集模式读回、首帧时间、每 5 秒取帧/显示计数、连续及累计无数据计数、最近取帧/显示时间、线程 ID 和退出原因/完整异常。旧错误日志和 UI 提示继续保留。
- 验证输出隔离到 tmp/session-validation/hot-camera。CommonCls、VisionControl Build 通过，主工程使用本轮隔离依赖及外置 targets 的 Compile 通过；未将主工程判为完整发布 Build。假 SDK 验证 25 项通过，覆盖 Join 超时资源保护、重复预览拒绝、异常后 Free、无数据/退出区分、参数失败不污染保存值及范围失败不写入。公共 bin 的主程序、配置、CommonCls.dll、xbVP_Control.dll 哈希未变。未运行实际程序或相机，未修改 Designer、工程、映射配置及曝光/增益/ROI 默认值。
- 保留疑点：具体失败参数节点及单帧冻结的直接原因仍需新日志确认，不能宣称现场故障完全修复。旧光度/字节采集入口未扩大修改；若现场进入这些入口，仍需核实是否与预览并行消费。观察两路后续成功帧数是否持续增长、显示计数是否跟随、线程是否仍在运行，以及失败节点的实际 SDK 返回码。

## 双相机连接回归的精准撤销（2026-10-13）

- 现场确认两路 Sharpness.GetRange=0x80000106 后均未启动采集。Git旧方法与15:28旧CommonCls DLL的IL一致：范围读取和SharpnessAuto结果原本不阻断后续写入；旧StartGrab无初始化状态门槛，模式读回仅用于诊断。用户本轮明确授权以恢复旧连接行为为最高优先级，撤销上一节新增的辅助调用强制失败条件，不新增恢复策略。上一节关于范围读取失败即返回、模式读回决定启动成功的描述不再代表当前行为。
- 本轮业务源码仅修改CommonCls/HIKDeviceSource.cs和VisionApplication/FrmMainUI.cs：锐度/Gamma/尺寸范围恢复旧范围计算及SDK调用顺序，范围和自动模式失败只记录；保留原有锐度/Gamma/宽高最终写入失败判断。曝光和触发恢复旧入口不以返回值阻断；连续/触发初始设置和模式读回失败不关闭已打开的句柄、不阻断恢复。StartGrabbing恢复按SDK真实返回码设置采集状态，不再被状态门槛或读回失败改判。
- 保留Create/Open、最终PixelFormat、ROI写入及有效索引的原有判断、参数局部副本与异常堆栈；映射、默认值、配置、回放、IO和长度修复未改。AcqFIFO及VisionControlExtention源码哈希未变，预览Join保护、finally归还和同路取帧互斥保留。失败SDK日志简化为调用名称、原始码、物理索引/序列号；Start持久化真实返回码。
- 备份与产物位于tmp/camera-connection-restore-20261013-181717。基于提取旧/新方法的假SDK对比67项通过，覆盖辅助失败继续执行、写入失败仍返回false、SDK调用/目标值与旧版一致、读回失败不改判Start结果。真实VisionBasedLaser.sln全部11项目Debug/x64完整Build退出码0，0错误、83警告；未出现构建期程序集冲突。公共bin的274文件哈希未变，未新增文件；未运行EXE或相机。
- 需要部署本轮CommonCls.dll及激光除胶.exe；与本轮主程序配套使用同批xbVP_Control.dll及调试符号，保持现场配置/模型/VPP不覆盖。仅确认代码回归撤销和完整构建通过，双路冷启动、一次热连接及连续预览仍需现场验证；若最终Sharpness/Gamma/尺寸写入仍失败，保留实际SDK错误，不将辅助读取失败与最终写入失败混为一谈。

## 启动光源状态与可选锐度恢复（2026-10-14）

- 现场 ErrorLog/CameraLog 确认两路 PixelFormat 候选及 Sharpness 节点报 0x80000106，随后 StartGrabbing 返回成功；12:37 上一轮双路预览实际收到并显示 280/242 帧。候选格式失败、参数恢复失败和设备启动结果须分别判断。本轮未扩大修改 PixelFormat、其他参数、采集线程、IO 或相机初始化汇总代码。
- 光源启动红字 job=0 controllerSlot=2 port=unknown not-open 来自历史 RTS 的 bSerInit=true：InitCam 选中相机时触发光源诊断，InitSerial 尚未创建串口。本轮在 ReadRunTimeStatus 后立即重置 bSerInit=false，由本轮 InitSerial 重建状态；槽位、COM 映射及统计保留。
- 对照 D:/E盘备份文件/VB_Det 原版：原流程无条件读写 Sharpness/SharpnessAuto，不主动开启 SharpnessEnable。业务修改限于 CommonCls/HIKDeviceSource.cs 和 VisionApplication/FrmMainUI.cs；保留 SetSharpness 两参数接口，新增仅供参数恢复使用的可选节点跳过入口。设备连接且节点查询成功，确认 Sharpness 未实现/不可用/只读或 SharpnessEnable=false 时跳过，保存值不变，不产生失败记录；锐度可用时恢复原设置，不写入不可用的 SharpnessAuto。辅助查询无法确定能力时沿用原参数调用，不据查询失败新增连接阻断；实际范围读取或写入失败仍报告，范围读取失败不按空范围写入零值，断连异常保留堆栈。原两参数/UI 调用语义保留。
- 验证产物位于 tmp/startup-sharpness-20261014。提取实际方法的假 SDK/历史状态检查 17 项通过；真实 VisionBasedLaser.sln 全部 11 项目 Debug/x64 完整 Build 退出码0、0错误、83警告。沙箱内首次停在 BJJCZ 的 x86 资源宿主启动，取消后经批准在沙箱外完成相同配置 Build；未修改项目引用、工程或目标平台，输出/中间目录隔离，引用搜索指向同批构建产物。
- 全部 11 个编译器命令确认 CommonCls/xbVP_Control 引用来自本轮 build；主程序和 VisionControl 引用的 CommonCls=1.0.0.0，主程序引用的 xbVP_Control=1.0.9783.28265 与本轮 DLL 一致；四项基础依赖版本符合 AGENTS.md。公共 bin 274 文件哈希无变化、无新增，工程、AcqFIFO、VisionControlExtention 和 Designer 哈希保留。
- 实机只读节点检查 EnumDevices=0x00000000、设备数0，未取得两台相机真实节点状态，未启动主程序或验证新产物采集；不能声称现场故障已修复。配套产物仍须整体使用本轮 CommonCls.dll、激光除胶.exe、xbVP_Control.dll 及对应 PDB，不覆盖配置/模型/VPP；现场需确认光源启动无提前诊断失败、双路冷启动/热连接和持续预览，以及可用锐度节点实际写入结果。

## 连接只读同步、停用锐度及 Gamma 扩展控件（2026-10-14）

- 用户明确要求：连接时保留 MVS 当前成像设置，只读取并同步；主动曝光、增益、Gamma 数值和光源操作保留；锐度及新增 Gamma“使能/用户”停用。彩色候选流程保留，任一候选或原有 Mono8 回退成功即成功，中间失败不产生 SDK 失败事件，全部失败才报告。
- 业务修改限定 CommonCls/HIKDeviceSource.cs、VisionApplication/FrmMainUI.cs、VisionApplication/FrmMainUIExtention.cs。启动、两个手动连接入口及自动重连改用 ReadCamParam；已在线设备点击连接也只读同步，不重建句柄。读取曝光/自动曝光状态、增益、可读 Gamma、实际宽高/偏移及 PixelFormat；参数结构体通过 ref 返回，必需读取失败不提交部分结果，ROI 使用新数组。Mono8、BGR8/RGB8、四种 Bayer8 按现有采集入口分类，不支持的格式明确失败且不写设备回退。光源 iBrightness 和历史锐度字段不由相机读取覆盖。PayloadSize 分配路径保留，既有采集/触发业务调用保持独立，不恢复成像参数。
- Gamma“使能/用户”及锐度控件在普通代码中隐藏、禁用、解绑事件，事件方法停用；批量设置移除锐度，参数获取不读锐度。主动 Gamma 数值写入去掉无条件 GammaAuto，范围读取失败不写入零值；真实数值写入失败保留。原曝光/增益/Gamma/光源 UI 操作方法与修改前一致，UI 同步由事件屏蔽保护，允许显示超出旧设计范围的真实参数。
- 主动断开保留预览退出保护及设备/缓冲释放，Close/Destroy 失败不记录成功；运行态标志暂停自动重连，下一次手动连接解除，不修改保存的自动重连配置。连接/断开共用已有操作互斥状态，拒绝释放尚在连接中的句柄。
- 验证及产物：tmp/camera-readonly-20261014。真实 VisionBasedLaser.sln 全部11项目 Debug/x64 完整 Build 退出码0，最终完整 Build 0错误、16既有警告（首次全量构建83警告）；沙箱首次停在 BJJCZ x86 资源宿主，取消后经批准沙箱外完成同配置 Build。输出/中间目录隔离，所有编译器的 CommonCls/xbVP_Control 引用来自本轮 build；CommonCls=1.0.0.0，xbVP_Control=1.0.9783.32590，主程序实际引用版本一致，四项基础依赖及 bindingRedirect 符合约束。公共 bin 276文件哈希无变化、无新增，工程、Designer、AcqFIFO、VisionControlExtention 保留。
- 提取本轮实际方法的42项假 SDK/WinForms数值/连接入口行为检查通过，覆盖零 SDK 写入读取、必需节点失败不污染参数、可选 Gamma、不支持格式、候选/回退成功无失败事件、全部失败上报、主动 Gamma 失败、手动断开暂停及恢复自动重连；静态核对调用链和原调参/光源方法保留。未启动主程序、未连接真实相机、未验证实机参数前后、双路持续预览或光源物理输出；不能声称现场效果已验证。配套程序/PDB已打包，未部署到公共bin。
