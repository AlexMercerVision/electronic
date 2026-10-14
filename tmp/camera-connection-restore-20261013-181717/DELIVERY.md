# 双相机连接行为恢复交付（2026-10-13）

本轮实际修改两个业务源码文件，并按项目三轮记录规则更新PROJECT_CONTEXT.md。修改前源码在backup目录。没有全文件回退，没有调整配置、参数默认值或序列号映射，没有修改Designer、VPP、模型、回放、IO或长度代码。

## 精准撤销

| 文件与当前行号 | 撤销的新增阻断 |
|---|---|
| CommonCls/HIKDeviceSource.cs:278 | AcquisitionMode/TriggerMode初始设置失败不再关闭/销毁已打开的句柄；保留SDK错误日志，按旧流程继续。 |
| CommonCls/HIKDeviceSource.cs:570 | Width/Height范围读取失败不再提前返回；恢复旧先宽后高的SDK调用顺序，两次最终写入仍必须均成功。 |
| CommonCls/HIKDeviceSource.cs:604 | Sharpness.GetRange和SharpnessAuto失败不再提前返回；恢复旧范围计算及最终锐度写入，实际写入失败仍返回false。 |
| CommonCls/HIKDeviceSource.cs:619 | Gamma.GetRange和GammaAuto失败不再提前返回；最终Gamma写入失败仍返回false。 |
| CommonCls/HIKDeviceSource.cs:746 | 曝光恢复不再在辅助读/自动设置失败时提前退出，保留完整旧调用顺序与日志；bool接口仍返回实际汇总结果，调用方恢复旧版不据此中断。 |
| VisionApplication/FrmMainUI.cs:2139 | 曝光、触发返回值和模式诊断读回不再新增为参数恢复失败条件；保留原有最终PixelFormat、锐度、Gamma、尺寸、ROI失败判断。 |
| VisionApplication/FrmMainUI.cs:2200 | 触发设置按旧顺序继续执行，避免第一次模式写入失败时阻断原有TriggerSource调用；自身返回值仍真实。 |
| VisionApplication/FrmMainUI.cs:2224 | 去掉新增状态<2启动门槛；Start成功只由SDK真实返回码决定，诊断读回失败不再把已启动采集改判为失败。 |

恢复依据：Git HEAD旧方法、之前审计报告、2026-10-13 15:28的旧DLL IL，以及用户确认16:19前双路可运行。旧范围计算明确保留，无新增“读失败改用原目标”等策略，无SharpnessEnable操作、无新的设备节点模式或默认值。新SetParam的bool签名保留，避免不必要的接口变更。

## 原有判断与安全修复

保留Create/Open失败、最终PixelFormat失败、锐度/Gamma/宽高最终写入失败、ROI写入失败、数组边界与异常处理。参数保存仍使用局部副本，全部原有必要步骤通过才提交。未将失败SDK节点记录为成功；辅助失败日志与最终写入/启动结果分别记录。

CommonCls/AcqFIFO.cs和VisionControl/VisionControlExtention.cs的SHA256与修改前一致：停止预览Join超时保护、旧线程退出确认、Get成功后的finally Free、Free返回码检查、同路预览/普通单帧互斥均保留。

InitCam的原有调用顺序与汇总代码本轮未扩大修改。冷启动在参数写入失败后仍按旧Start路径调用SDK；不应仅凭旧“初始化正常”UI行判断现场恢复，必须核对两路真实Start返回码与后续帧增长。

## 完整Build

真实VisionBasedLaser.sln，Debug/x64，/t:Build，全部11个项目：CommonCls、VisionControl、VisionApplication、ParamSet、LS_UartAssist、PerMang、CamDebug、BJJCZ、PipeChanel、xbDet、MarkManager。

使用E:/VS-IDE/MSBuild/Current/Bin/amd64/MSBuild.exe 18.6.3，未用隔离Compile替代Build，未跳过资源或修改工程/语言版本。仅重定向输出/引用搜索/中间目录，使用同批新CommonCls和VisionControl依赖。在沙箱外完成正常x86资源任务，BJJCZ原项目x86目标没有改动。

退出码0，0错误，83警告。没有语法或缺失方法错误，没有MSB3277/MSB3245/MSB3258。日志为solution-build-utf8.log，完整输出为build目录。辅助假SDK比较67项通过，但不作为完整Build或实机验证替代。

公共bin检查274个文件：SHA256变化0，新增0。没有运行程序或相机，没有执行部署。

## 部署与现场验证

需要部署新CommonCls.dll和激光除胶.exe；主程序本次编译引用同批xbVP_Control.dll（源码未改，完整Build重新生成AssemblyVersion），建议配套部署该DLL及对应PDB。不要覆盖现场exe.config、UserData、相机/工位配置、模型或VPP，不更新第三方SDK/基础依赖DLL来解决本次回归。部署操作尚未执行。

1. 关闭旧程序后使用新程序集启动，两台相机已接入。确认Job0/DA8696070及Job1/DA8696075绑定；本次枚举槽顺序可变化，不手动交换配置。
2. 核对每台StartGrabbing SDK=0x00000000；读取失败允许保留原始日志，但最终Sharpness/Gamma/尺寸/ROI写入失败仍必须单独观察，不能当成已经成功恢复参数。
3. 双路实时预览至少60秒：画面持续变化、后续成功帧数及显示帧数增长，连续无数据和线程退出日志可定位。
4. 停止预览确认旧线程退出，再启动预览，确认没有同路双线程或缓冲区释放异常。
5. 程序无相机启动后再接入两台，点击连接一次，核对连接成功2/2及两路连续采集。不要连续点击掩盖失败。
6. 若最终写入或Start仍失败，保留SDK调用名称、原始错误码、相机索引/序列号及完整异常。当前代码已撤销确认的新增阻断，但未实测，不能宣称现场全部故障完全修复。
