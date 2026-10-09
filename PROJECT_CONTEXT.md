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
