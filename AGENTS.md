# ElectrodeDet 项目协作说明

## 工作规则

- 分析或修改前先完整阅读本文件和 `PROJECT_CONTEXT.md`，再结合当前源码、实际日志和本轮需求确认范围。
- 修改前先沿调用链确认根因；保持现有命名、业务语义和最小修改范围，不擅自新增功能或改变既有逻辑。
- 用户要求“只分析”时不得修改业务代码、配置或工程文件；本轮只允许整理用户明确指定的说明文档。
- 禁止直接修改 `*.Designer.cs`；UI 改动优先使用设计器。
- 修改代码前说明文件范围；修改后说明影响范围和验证方法。每三轮修改将主要问题和修改点写入 `PROJECT_CONTEXT.md`。

## 依赖与构建

- `CommonCls`、`VisionControl`、`VisionApplication` 共用根目录 `bin`。基础依赖统一为：System.Memory 4.5.5/AssemblyVersion 4.0.1.2、System.Buffers 4.5.1/4.0.3.0、System.Numerics.Vectors 4.5.0/4.1.4.0、System.Runtime.CompilerServices.Unsafe 6.0.0/6.0.0.0；禁止手工复制 DLL 解决冲突。
- 修改基础依赖后核对公共 `bin` 的实际 AssemblyVersion、`激光除胶.exe.config`，并分别构建上述三个项目；不要单独构建仍在公共 `bin` 输出旧依赖的 `ClientTools.csproj`。
- “主界面能打开”不等同于 Paddle、VPP、相机、串口、测量或模式切换已验证，结论必须有对应日志/现场现象。

## 相机、显示、光源与模式

- 单次运行结果必须保留。仅在采集成功取得新原图、准备切换显示时清理对应工位的 `StaticGraphics`/`InteractiveGraphics`；A 面覆盖 `cogRDMainUser` 与 `cogRDMain.Display`，B 面覆盖 `cogRDSub1`，取图失败不得清框，也不得扩大 `ClearRDRecord()` 语义。
- 真实相机单帧保持 `GetImageBuffer → 按本帧 nFrameLen 复制 → finally FreeImageBuffer`；允许的 PixelFormat 必须与入口一致，Bayer 必须按实际 PixelType 枚举转 BGR24。显示图像与 `bImgSrcData[cameraIndex]` 使用同一转换结果。
- 右侧相机项同时保留物理相机索引和工位索引；曝光按物理相机，光源按当前所选相机对应的串口槽，串口关系只由 `p_SD.serInd/bUsed` 决定，不硬编码 COM。工位 A/B/同步只控制采集、预览和显示范围，不自动改相机选择；右侧光源同步与工位同步独立。
- 相机参数刷新只刷新 UI，不翻转光源或发送命令。光源串口未配置/未打开时禁止改亮度或开关；发送失败不得更新保存值或伪造成功状态。

## 外部 IO

- ZK/ZKZK 使用第 4 路 `sPortCy`；`p_SD.bUsed[3]` 为真才打开并注册接收。COM 变绿只代表 OS 打开，不代表协议、业务触发或物理输出成功，四层状态必须分开。
- 输入为厂家扩展 `0x45` 主动上报，A/B 映射读 `p_IOPP.APlane/BPlane`，仍需自动触发、就绪、忙状态检查后才进入 `RunOneTimeForFace()`。10/25 字节偏移和 CRC 修复前必须有现场抓包，禁止猜测。
- 输出链和 `IOTimeDelay` 的语义以当前源码为准；任何通道、解析、串口修复都不得与光源/相机混改，须先说明证据和影响。

历史修复和现场验证记录统一放在 `PROJECT_CONTEXT.md`，本文件只保留当前约束。
