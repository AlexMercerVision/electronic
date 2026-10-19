# 启动推理预热最小修改

## 行为与范围

业务源码仅修改 CommonCls/CDL.cs 和 VisionApplication/FrmMainUI.cs；PROJECT_CONTEXT.md同步现状。保留此前路径设置删除，未修改Designer、工程引用、配置或相机/串口/IO实现。

启动流程：InitDL成功 → LoadVPFile → SearchCam/InitCam → 启动页55%“正在预热深度推理” → WarmupDL → InitSerial → 后续初始化与主界面。

WarmupDL仅检查Job0/Job1，跳过未启用工位；映射须在物理槽0/1及参数数组范围内，尺寸须为正、可表示为int且BGR长度不溢出。选择第一个有效工位后只调用一次，不逐台相机检测。相机连接成功时使用已读取到内存的实际参数，否则仅在映射和保存尺寸仍有效时使用内存保存值；不会借用预留工位。映射或尺寸无效逐项记录jobIndex/cameraIndex，无有效工位则操作日志记失败。

CDL.det_infer_img(width,height)取消Cognex示例图片依赖，构造内存黑色BGR图像，使用同一个CDL对象、同一原生接口和既有彩色推理锁。通过GCHandle固定结果数组，finally释放。原生返回0为成功，零检测框也成功；返回非0或异常明确记失败，异常保留完整堆栈。日志写工作目录UserData/dl_init_debug.log，搜索[Warmup]可查看选中的工位、尺寸、GPU、返回码、框数及预热耗时。

本机bin/model_infer.dll的导出及反汇编已核对：Det_ModelPredict的RVA=0x18BF0，参数错误分支返回-1、未就绪分支返回-2、正常预测的状态原样返回；内部预测函数正常路径清零eax、错误路径使用负值。现有FrmDL1.cs也按ret!=0处理失败。反汇编记录见model-infer-disassembly.txt。

预热结果直接丢弃，不进入采集、叠框、统计、存图或物理输出。失败保留原模型初始化状态，不伪报成功；没有增加模式切换/重新加载等其他入口的预热。程序启动会等待这一次推理结束，预期将首次推理的初始化成本前移。

## 验证

- 实际VisionBasedLaser.sln全部11项目Debug/x64完整Build通过，退出码0，0错误/82警告；旧预热函数的未使用异常变量警告消除。沿用经批准的沙箱外构建，输出与中间目录隔离于build/obj。
- CommonCls、VisionControl、VisionApplication均本轮编译，编译器实际引用来自同批产物；CommonCls=1.0.0.0、xbVP_Control=1.0.9784.34759，启动程序引用匹配。工程引用未改，未用旧DLL代替本轮项目源码。详见solution-build.log及artifact-verification.json。
- 公共bin及隔离产物基础依赖：System.Memory=4.0.1.2、System.Buffers=4.0.3.0、System.Numerics.Vectors=4.1.4.0、System.Runtime.CompilerServices.Unsafe=6.0.0.0；两处电极帽缺陷检测.exe.config的bindingRedirect一致。
- 公共bin276文件哈希不变、无新增；快照中的其他源码、Designer、配置、VPP与工程文件未变化。
- 从实际源码提取WarmupDL进行13项选择/拒绝检查：交换映射、仅B启用、Job0无效后使用Job1、预留物理槽、零尺寸、字节长度溢出、uint溢出、缺数组、短数组、仅预留Job2启用、原生推理失败不重试等通过。启动顺序与初始化成功条件也已核对。这是逻辑检查，不代替主程序现场运行。见startup-selection-checks.txt。
- 使用本轮实际CommonCls.dll、公共bin现有原生运行库和D:\data\inference_model模型，在独立进程、GPU0、3072×2048内存图像上真实推理：第一次2242ms，同线程第二次41ms；另一个新进程第一次2228ms，随后新后台线程内部46ms（含线程启动/封送共57ms）。两次进程中均返回0、框数0，证明预热成功不依赖检测到目标。见real-warmup-result.json、worker-warmup-result.json及runtime-check/UserData/dl_init_debug.log。
- 实际CDL方法的未初始化模型、零尺寸及算术溢出均拒绝并记录完整异常。真实测试仅初始化/销毁模型，未启动主程序、连接相机或打开串口，日志写本目录runtime-check，没有手工复制或替换公共bin的DLL。

原生库真实运行时另有既有环境告警：Paddle编译所用cuDNN为9.12，本机加载为9.8。本次推理成功，未调整运行库版本；该现象保留为运行环境待核对项。

## 现场边界

本批未部署，公共bin仍是原运行文件。真实GPU测试使用内存黑图，测到的是该独立进程内的推理预热效果；不是相机取图、图像转换、显示、统计或IO构成的完整检测耗时，也不代表主程序现场验收通过。后续使用同批程序集配套部署后，应查看启动[Warmup]成功日志，并比较A/B真实首帧与后续帧的推理/整次检测耗时。
