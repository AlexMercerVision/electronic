# 已授权网卡优先选择（2026-10-19）

业务源码仅修改 CommonCls/Protect.cs 的 GetMacAddress，增加 16 行。有效授权文件且 CID 非空时，先遍历全部 Ethernet 网卡寻找与既有 CID 完全匹配的 MAC；无匹配、未授权、文件无效或 CID 缺失时，保留原首块 Ethernet 的选择与原异常返回。RoleCheck、白名单、期限校验、授权文件路径及写入逻辑未修改。

本修改消除已绑定网卡仍在本机但枚举顺序变化造成的错误拒绝，不保证所有初始化失败均不再出现；绑定网卡移除、换机、无效或过期授权仍按既有规则处理。

## 验证

- 实际 VisionBasedLaser.sln，Debug/x64，全部 11 项目完整 Build 通过，退出码 0、0 错误、84 警告。输出及中间文件隔离于本目录，工程引用未修改。通过解决方案目标先生成本批 PipeChanel，再完整 Build，确保编译时项目依赖均来自本批源码。
- CommonCls=1.0.0.0，xbVP_Control=1.0.9788.22853；主程序/视觉控件实际引用及全部编译器 CommonCls、xbVP_Control、PipeChanel 引用路径已核对。四项基础依赖及公共 bin/新生成主程序配置重定向匹配。详见 artifact-verification.json 和 solution-build.log。
- 84 警告包含既有未使用变量等，以及本批 x64 PipeChanel 被 x86 BJJCZ 使用的架构警告（MSB3270、CS8012）。此轮未调整 PipeChanel 平台，不将本目录视为可整体部署的现场安装包。
- 提取本轮和 HEAD 的实际 GetMacAddress，16 项受控检查通过；旧方法可复现虚拟网卡在前时选错，新方法在相同输入下选中绑定网卡。覆盖无授权/无效文件/空 CID/绑定网卡缺失/不同机器/Wi-Fi 不作为 Ethernet/枚举失败等。见 selection-regression-results.txt。
- 使用新生成的真实 CommonCls.dll、本机真实网卡和既有有效授权文件，只读检查成功匹配既有 CID，并验证未授权、无效授权和无匹配时的原有选择。此次实机枚举中旧首块网卡也已匹配 CID，因此此刻实机不复现此前顺序问题；顺序变化回归由上述受控检查覆盖。
- 公共 bin 264 个文件哈希不变、无新增；授权文件及其余受保护工程/源码共 1678 个文件哈希不变。验证未调用 DateVerify、Regist、WriteCID 或 WritePasswordFile，未写授权文件。

## 使用与边界

未部署公共 bin，未启动主程序，未验证真实设备和完整启动。修改的是工作区源码，现场使用前需在 VS 中以 Debug/x64 完整重新生成并启动，检查授权成功及后续初始化日志。不要把此目录的 DLL 整体手工复制到公共 bin。
