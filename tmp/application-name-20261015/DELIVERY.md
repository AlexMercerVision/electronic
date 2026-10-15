# 电极帽缺陷检测：名称统一

主程序文件名、文件属性中的说明/产品名称、主窗口初始及各模式切换后的标题、启动页标题/名称、重复启动提示、生成的ClickOnce产品/清单名称统一为“电极帽缺陷检测”。主窗口原有模型/VPP路径后缀保留。主程序输出为 `电极帽缺陷检测.exe`，配置文件为 `电极帽缺陷检测.exe.config`。

显示名称统一读取 `FrmMainUI.ApplicationName`；AssemblyInfo由多个工具工程共用，这些工具的文件说明/产品属性同样更新，但工具exe名称保持原样。Designer文件未修改，窗体构造时在InitializeComponent之后设置标题及启动页文字。

保留兼容项：

- 单实例互斥 `Global\VisionBasedLaser_LaserRemoveGlue`，使旧版和新版不能同时启动并争用相机、串口及激光设备。
- `%APPDATA%\GearDet` 授权目录及现有机器授权规则，避免现有用户授权丢失或被当作新用户重新初始化。
- 现场模板目录、`ManuMarkPath`、UserData/模型/VPP路径、解决方案/工程名称及命名空间不变。
- 公共bin的旧exe、旧清单及历史文档不删除、不改名；本批产物未部署。后续发布应使用本批重新生成的清单，不能手工编辑旧清单中的名称或哈希。

修改文件：`VisionApplication/FrmMainUI.cs`、`VisionApplication/Frm_Welcome.cs`、`VisionApplication/Program.cs`、`VisionApplication/Properties/AssemblyInfo.cs`、`VisionApplication/VisionApplication.csproj`；AGENTS.md中的当前exe配置名及PROJECT_CONTEXT.md同步更新。

## 验证

- 实际 `VisionBasedLaser.sln` 全部11项目 Debug/x64完整Build通过，0错误、83既有警告；沿用已授权的沙箱外构建，输出及中间目录隔离，工程引用未替换。
- 编译器实际CommonCls/xbVP_Control引用均来自本批源码构建；CommonCls=1.0.0.0、xbVP_Control=1.0.9784.27743，主程序引用版本匹配。
- 四项基础依赖AssemblyVersion及公共bin旧配置/隔离输出新配置的bindingRedirect均匹配AGENTS.md。
- exe的FileDescription/ProductName、`.application`产品名称、`.exe.manifest`启动文件名均已核对为新名称；PDB和exe配置文件使用新前缀。
- 公共bin的276文件哈希无变化、无新增。两个Designer、上轮IO的FrmMain_Laser.cs/CSerialDef.cs及其他列入快照的业务/配置文件哈希不变。
- 未启动主程序或连接设备，不将构建/名称核对视为相机、光源、IO和推理现场验证。

配套更新包 `电极帽缺陷检测_程序文件.zip` 包含同批CommonCls.dll/PDB、xbVP_Control.dll/PDB、新名称exe/PDB/config及重新生成的两个发布清单。尚未覆盖公共bin，也不是整机运行环境安装包。

使用时先退出旧版并备份运行文件，将同批文件配套部署到现有运行目录，继续使用原有配置、模型和VPP；快捷方式或启动脚本需要指向新的exe。旧exe不能与新版同时运行。不要把隔离build目录当作已准备好现场数据的独立运行目录。
