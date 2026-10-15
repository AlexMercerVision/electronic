# ElectrodeDet 项目协作要求

分析或修改前完整阅读本文件和 `PROJECT_CONTEXT.md`，结合本轮需求、当前源码及实际日志确认范围。当前约束放本文件，现状与历史证据放上下文；历史记录不作为新的强制规则。

## 工作范围

- 修改前沿调用链确认根因并说明文件范围；保持现有命名、业务语义和最小范围，不擅自新增功能。修改后说明影响和验证方法，每三轮修改更新上下文。
- “只分析”不得修改业务代码、配置或工程；仅可整理用户明确授权的说明文档。
- 禁止直接修改 `*.Designer.cs`，UI 优先用设计器。

## 构建与验证（涉及 C# 修改时）

- 最终验证使用实际 `VisionBasedLaser.sln`、Debug/x64、真实项目引用，完整 Build 启动项目及依赖。单项目 Build、Compile 或隔离桩仅是中间验证。
- 可隔离输出，但不得替换工程引用、遗漏项目或用旧 DLL 代替本轮源码。构建前核对覆盖范围，未经允许不得覆盖公共 `bin`、配置、模型、VPP 等运行文件。
- `CommonCls`、`VisionControl`、`VisionApplication` 共用根目录 `bin`；禁止手工复制 DLL 解决依赖冲突，未专项统一前不要单独构建 `ClientTools.csproj`。
- 基础依赖统一为下表；修改后核对公共 `bin` 实际 AssemblyVersion、启动程序对应的 `.exe.config`（当前为 `电极帽缺陷检测.exe.config`），分别验证三项目并完成解决方案 Build。

| 依赖 | 包版本 | AssemblyVersion |
| --- | --- | --- |
| System.Memory | 4.5.5 | 4.0.1.2 |
| System.Buffers | 4.5.1 | 4.0.3.0 |
| System.Numerics.Vectors | 4.5.0 | 4.1.4.0 |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | 6.0.0.0 |

- 最终报告列明解决方案/配置、完整 Build 结果、实际引用程序集版本一致性、现场验证情况；未执行、失败或受阻须明确标注。
- Build 或主界面打开不代表 Paddle、VPP、相机、串口、测量、模式切换已验证；各功能须有对应日志/现场现象。相机初始化、SDK 参数、采集线程改动未经真实设备验证，不得声称现场故障已修复。

## 相机、显示与光源

- 启动先检查工位 `camInd`、相机对象及状态数组边界；无效工位记录可定位错误并隔离，不得清理另一台已正常连接的相机。
- 单次结果保留到成功取得新原图、准备切换显示时，才清理该工位的 `StaticGraphics`/`InteractiveGraphics`。A 面覆盖 `cogRDMainUser` 与 `cogRDMain.Display`，B 面覆盖 `cogRDSub1`；取图失败不得清框，不扩大 `ClearRDRecord()` 语义。
- 真实单帧保持 `GetImageBuffer → 按本帧 nFrameLen 复制 → finally FreeImageBuffer`。PixelFormat 与入口一致，Bayer 按实际 PixelType 转 BGR24；显示图像与 `bImgSrcData[cameraIndex]` 使用同一转换结果。
- 右侧相机项保留物理相机与工位索引；曝光按物理相机，光源按所选相机对应串口槽，仅由 `p_SD.serInd/bUsed` 决定，不硬编码 COM。A/B/同步仅控制采集、预览、显示范围，不自动换相机选择；光源同步独立。
- 参数刷新仅更新 UI，不翻转光源、不发命令。光源串口未配置/未打开禁止改亮度或开关；发送失败不更新保存值、不伪造成功。

## PMA/精定位

- 单工位参数、模板、精定位入口只接受已启用 Job0/Job1；全部工位 `-1` 和预留索引不得访问 PMA 数组，保留两个工具对象。
- 无效选择记录带 `jobIndex` 的操作失败；真实 Job/工具未初始化及 PMA/精定位异常保留完整堆栈和工位索引，不只记 `ex.Message`。

## 外部 IO

- ZK/ZKZK 使用第 4 路 `sPortCy`，仅 `p_SD.bUsed[3]` 为真才打开并注册接收。OS 打开、协议有效、业务触发、物理输出分层判断，COM 变绿仅说明 OS 打开。
- 输入为厂家扩展 `0x45` 主动上报，A/B 映射取 `p_IOPP.APlane/BPlane`；自动触发、就绪、忙状态检查后才进入 `RunOneTimeForFace()`。修复 10/25 字节偏移或 CRC 前须有现场抓包，不猜测。
- 输出链与 `IOTimeDelay` 以当前源码为准；通道、解析、串口修复不得与光源/相机混改，先说明证据和影响。
