# 双相机连接回归追溯（2026-10-13）

## 本轮结果

已完整阅读 AGENTS.md、PROJECT_CONTEXT.md。已备份当前全部 10 个已修改的跟踪文件、Git 差异及公共 bin 的 SHA256 清单。本轮没有修改业务源码、工程、Designer、配置、映射、VPP 或模型，没有运行应用或相机。双相机连接能力尚未恢复。

按用户任务2第8条停止源码修改：已找到旧逻辑及16:19前的旧DLL，但无法确认范围读取失败后的锐度写入成功，也不能确认完整回退该方法安全。编译产物是当前源码的验证输出，不是相机恢复版。

## 版本及现场证据

- HEAD 为 d8d9c3d（2026-10-10）；b9b7ee2（2026-10-09）及相邻 ElectrodeDet/ElectrodeDet0 目录都保留相同的旧 SetSharpness 逻辑。没有找到可与用户现场16:19前成功运行唯一对应的完整源码快照。
- tmp/session-validation/common/CommonCls.dll 的文件时间为2026-10-13 15:28:57。只读取其方法元数据/IL，未调用 SDK：SetSharpness 在 IL_000C 调用 GetSharpness 后的下一操作码为0x26（pop），确认旧DLL丢弃范围读取返回码，与上述旧源码一致。
- 用户确认16:19前双路能运行，作为现场证据保留。D:/data/Log 未找到该时间段成功连接的持久化记录；旧 AddLog 在 FrmMainUI.cs:3079-3096 仅将错误写入文件，成功只在UI显示，Oper日志写入代码在3112起已注释。当前 CameraLog 首条为16:53:55，bin/UserData/camera_preview_debug.log 最后更新为9月16日。
- 当前 CameraLog_20261013.txt 17:12及17:23均显示 Cam1/DA8696070、Cam0/DA8696075 的 Sharpness.GetRange=0x80000106。手动连接最终成功=0、启用工位=2、枚举设备=2、映射有效=True。未进入Gamma和缓冲初始化。0x80000106不能证明之后的写入也会失败，更不能证明写入会成功。

## 精确差异

HEAD HIKDeviceSource.cs:388 的旧实现：

```csharp
var range = new MyCamera.MVCC_INTVALUE();
cam.MV_CC_GetSharpness_NET(ref range);              // 返回码丢弃
value = Clamp(value, range.nMin, range.nMax);      // 失败也继续使用范围
cam.MV_CC_SetEnumValue_NET("SharpnessAuto", 0);     // 返回码丢弃
int ret = cam.MV_CC_SetSharpness_NET(value);
return ret == MyCamera.MV_OK;                     // 最终写入仍决定返回值
```

当前 HIKDeviceSource.cs:608：

```csharp
var range = new MyCamera.MVCC_INTVALUE();
if (!CheckSdkResult(cam, "Sharpness.GetRange", value.ToString(),
    cam.MV_CC_GetSharpness_NET(ref range))) return false; // 611：新增提前失败
uint adjusted = Clamp(value, range.nMin, range.nMax);
if (!SetEnumValueChecked(cam, "SharpnessAuto", 0)) return false; // 613：另一新增阻断
if (!CheckSdkResult(cam, "Sharpness", adjusted.ToString(),
    cam.MV_CC_SetSharpness_NET(adjusted))) return false;
value = adjusted;
return true;
```

SetCamParam 对 SetSharpness 的 false 返回值进行失败判定，在旧版本就存在（HEAD FrmMainUI.cs:1890附近），不是本轮新增。新增阻断发生在包装方法的范围/自动模式检查。

旧 StartGrab（HEAD FrmMainUI.cs:1967）不检查初始化状态，直接调用 SDK StartGrabbing，仅以SDK返回码判定采集启动；采集模式读回用于日志。当前 FrmMainUI.cs:2229 加入状态<2即不调用SDK的保护，2244将模式诊断读回失败并入采集启动失败。因此旧版能够出图，并不能单独证明旧锐度恢复已经成功；旧StartGrab可能在参数恢复失败后仍启动采集。

当前 InitCam:1965 的参数失败continue、2007提前设状态3、2031无条件iInitType=0及 ProcessInitCamStatus:1263成功日志均有旧版对应逻辑，属于遗留状态缺陷，并非全部由新检查引入。当前启动保护将此前可能继续启动的路径阻断。

## 其他新增强制失败条件

| 位置（当前源码） | 变化及风险 |
|---|---|
| HIKDeviceSource.cs:278-281 | AcquisitionMode/TriggerMode写入返回码从忽略改为初始化失败条件；当前已通过该阶段。 |
| HIKDeviceSource.cs:756、763、779、781 / FrmMainUI.cs:2160 | SetParam从void变bool，曝光范围、自动模式、曝光写入失败都会阻断连接；当前已通过。 |
| HIKDeviceSource.cs:613 | SharpnessAuto=0写入新增强制成功条件；当前未执行到。 |
| HIKDeviceSource.cs:625、627 | Gamma.GetRange及GammaAuto新增强制成功条件；当前未执行到。Gamma值写入失败在旧版就会返回false。 |
| HIKDeviceSource.cs:578、582 | Width/Height范围读取新增强制失败；当前未执行到。尺寸写入失败旧版已检查。 |
| FrmMainUI.cs:2182 | 旧SetCamParam忽略SetTrigger返回值；现在触发设置失败阻断参数恢复。 |
| FrmMainUI.cs:2185 | AcquisitionMode/TriggerMode/TriggerSource/PixelFormat读回失败新增为参数恢复失败条件。 |
| FrmMainUI.cs:2229、2244 | 初始化状态不足拒绝Start；SDK Start成功但任一诊断读回失败仍设为状态1，且当前没有相应Stop回滚。 |

ROI写入及最终PixelFormat失败判定已有旧版检查，不应当作新增回归直接撤销。SetGain虽新增检查，当前连接恢复路径的SetParam仍只写曝光，没有调用SetGain；本轮不扩大增益逻辑。

## 保留的修复方案（未实施）

1. 精准原样恢复旧SetSharpness并补充日志：可去掉611/613的新增提前返回，但仍会在范围读取失败后使用不可靠范围，可能改变目标值；最终锐度写入仍可能失败。不能认定此方案安全或必定恢复连接，故未实施。
2. 更保守的备选：范围读取失败时只记录错误，不使用该范围、不修改保存值，按既有调用顺序尝试写入保存的原目标；最终写入必须依据真实SDK返回码。自动模式失败单独记录，不伪造该节点成功。不启用SharpnessEnable、不引入其他模式。这不是严格原样恢复，尚无现场写入成功证据，因此也未实施。
3. 不能直接忽略整个SetSharpness、返回true、或删除StartGrab初始化保护来宣称恢复。需要先确认旧运行实际Sharpness写入成功与否、旧出图是否依赖未恢复的设备参数。
4. 冷启动状态的最小修复应保留调用顺序，仅增加启用工位的参数/缓冲准备结果汇总；失败不能记录初始化成功；缓冲完成只置2，真实Start成功才置3。诊断读回结果应与真实Start返回码分别表示。遵从用户停止条件，本轮未单独实施这些源码修改。

## 实际完整构建验证

使用 E:/VS-IDE/MSBuild/Current/Bin/amd64/MSBuild.exe，版本18.6.3，原项目及原解决方案，Debug/x64；没有使用外置Compile targets、跳过资源或修改语言版本。仅重定向OutputPath、ReferencePath、IntermediateOutputPath到本审计目录，公共bin未覆盖。

| 构建 | 结果 |
|---|---|
| CommonCls/CommonCls.csproj /t:Build | 退出码0，15警告，0错误 |
| VisionControl/VisionControl.csproj /t:Build | 退出码0，24警告，0错误 |
| VisionApplication/VisionApplication.csproj /t:Build | 退出码0，39警告（含依赖项目），0错误 |
| VisionBasedLaser.sln /t:Build | 退出码0，83警告，0错误，全部11个解决方案项目完成 |

沙箱内解决方案构建在BJJCZ的x86 GenerateResource任务停滞，已取消；获得工具批准后，在沙箱外使用同一解决方案、Debug/x64与项目配置成功完成，未修改BJJCZ工程或其x86目标配置。

构建日志未发现MSB3277/MSB3245/MSB3258或语法/缺失方法错误。基础依赖实际AssemblyVersion为System.Memory 4.0.1.2、System.Buffers 4.0.3.0、System.Numerics.Vectors 4.1.4.0、Unsafe 6.0.0.0；不将构建结果等同于真实SDK/相机验证。

本轮已修改源码的SHA256变化数量=0；公共bin检查274个文件，变化=0、新增=0。完整解决方案日志为full-solution-build-utf8.log；验证输出为full-solution-build，不应作为恢复版部署。

## 现场恢复测试步骤（取得有依据的修复版之后）

1. 两台相机已接入后冷启动一次：核对Job0/DA8696070、Job1/DA8696075绑定，逐台参数、PayloadSize、缓冲及StartGrabbing的真实结果，整体成功必须为2/2。
2. 双路预览持续至少60秒：分别确认后续成功帧数与显示帧数持续增长，记录连续无数据和线程退出原因，不能只看首帧。
3. 停止预览确认线程退出；程序无相机启动后再接入两台，点击连接一次，重复核对2/2及持续取帧。
4. 任一步参数恢复失败，保留失败节点/目标/SDK码及完整异常；停止把该轮当作恢复成功，不修改现场曝光、增益、Gamma、锐度、ROI、像素格式默认值或映射。

本轮没有执行以上现场步骤。
