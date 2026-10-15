# 路径设置删除及启动预热分析（2026-10-15）

## 本轮已实施

仅修改 VisionApplication/Frm_RuntimeSet.cs。两个构造入口在 InitializeComponent 后解除激光标刻路径按钮事件并 Dispose 标签、文本框、按钮，将其移出运行时控件树；删除对应路径选择分支及 SetPathParam/GetPathParm 中的参数读写。

AGENTS.md 禁止直接修改 Designer，因此生成的控件定义保留。CommonCls/CUserParam.cs 的 ManuMarkPath 仍被主窗体旧的文件、备份和激光入口引用，本轮仅删除路径设置中的该项，不删除共享字段、不迁移配置、不扩大为全项目激光模块清理。

## 推理预热现状（仅分析，未修改）

- 当前编译入口：FrmMainUI_Load → InitDL → CDL.InitDL → model_infer.dll!InitModel。当前主程序没有调用无参数 CDL.det_infer_img()；该调用仅在未参与当前主工程编译的 FrmMainUI（1）.cs 中。
- CDL.cs:257 保留启动预热函数，固定读取 C:\Program Files\Cognex\VisionPro\Images\smiley.bmp；本机该文件不存在。成功路径仍返回初始值 false，异常被吞，未检查原生推理返回值；Mat 未 Dispose，FloatToIntptr 分配的固定句柄也未释放。不能只恢复调用便认定预热成功。
- 正式检测经 VisionControl.RunOneTime → DLInfer/DLInferOffLine → CDL.det_infer_img_ByteColor/ByteGray → Det_ModelPredict。主窗体将同一 CDL 对象交给 vpWin，当前彩色推理在该对象的锁内串行调用原生推理。
- 核对 bin/UserData/serUserParam.json：深度推理开启，GPU0，模型 D:\data\inference_model；Job0→Cam1、Job1→Cam0，两路保存尺寸均为3072×2048、彩色。启动连接会读取设备实际参数，不能把保存值当作实时尺寸。
- bin/UserData/dl_init_debug.log 最近一次：2026-10-15 15:38:39.085 调用 InitModel，15:38:42.763 成功，约3.678秒。该记录是模型加载耗时；没有对应的首帧/后续纯推理耗时，不能将全部首检延时归因于推理。

## 待用户确认的最小方案

仅修改 CommonCls/CDL.cs 和 VisionApplication/FrmMainUI.cs：

1. 修正已有预热函数，按有效相机尺寸构造内存 BGR 图像，直接调用当前模型的 Det_ModelPredict，丢弃结果。预热函数局部分配/释放固定句柄，取消固定示例图片依赖，核对原生返回码语义并记录返回码/耗时/完整异常；零检测框不能当作失败。
2. 在启动模型初始化成功、InitCam 完成之后，InitSerial 之前，同步预热一次并更新启动页提示；此时尚未开放串口业务触发和主界面。按已启用 Job0/Job1 的有效映射选择尺寸，当前相同尺寸共用同一个模型，只需一次。无效映射/尺寸明确记录，不使用预留工位。
3. 预热失败明确记录操作失败并保留原有模型状态，不伪报成功。本次不增加模式切换、手动重新加载等其他入口的预热，不触发真实采集、显示、计数、存图或IO输出。

验证应比较冷启动后首帧与后续帧的纯推理耗时，并分别观察整次检测耗时。预热仅将首次推理的初始化成本前移，不能保证相机首次取图、图像转换、显示等也完全没有额外耗时；GPU线程/输入尺寸相关行为需要真实运行确认。

## 本轮构建与边界

- 实际 VisionBasedLaser.sln，Debug/x64，全部11项目完整 Build 通过，退出码0，0错误/83既有警告；沙箱内BJJCZ x86资源宿主受阻后取消，经批准沙箱外完成同一配置构建。
- 输出及中间目录隔离在本目录 build/obj，真实项目引用不变。CommonCls、VisionControl、VisionApplication 均重新编译；编译器实际引用来自本批产物，CommonCls=1.0.0.0，xbVP_Control=1.0.9784.28769，启动程序引用版本匹配。详见 artifact-verification.json、solution-build.log。
- 公共bin及隔离输出四项依赖分别为 Memory=4.0.1.2、Buffers=4.0.3.0、Vectors=4.1.4.0、Unsafe=6.0.0.0；两处电极帽缺陷检测.exe.config的bindingRedirect均匹配。
- 公共bin的276文件哈希不变、无新增；快照中的其他源码、Designer、配置与工程文件均未变化。未部署、未启动主程序或执行现场界面/相机/推理/IO验证。
