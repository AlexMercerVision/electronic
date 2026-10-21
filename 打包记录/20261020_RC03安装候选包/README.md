# RC03 安装候选包记录

批次：E:\VB_Det_PACK\20261020_50Series_RC03_Installer。交付目录：该批次“离线交付”；独立保存RC01、RC02。不修改业务C#、工程、公共bin、现场参数和授权。

最终结果：Inno Setup7.1.0原生x64编译成功，Setup为468145414字节（446.46MiB），SHA256为EE9AF21DC2C88175B8FDA2AA9179A3ABAF55628A13CC5E3A0F15A46A4FBDCAFB。默认C:\Program Files\ElectrodeDet安装、重装升级、卸载均返回0；默认依赖缺项拒绝返回7；缺CUDA路径启动拒绝返回1。桌面/开始菜单均指向专用入口。11项既有配置、工作VPP、授权及业务日志升级/卸载前后哈希一致，测试安装及快捷方式/卸载注册已移除。

离线交付目录已生成并校验：110项交付文件全部SHA256通过（另附清单及其摘要两文件），总资源约6.71GiB。包含Setup、独立Prerequisites、安装说明和校验脚本。旧VC2017 14.13缓存介质未收入交付。携带到目标机时复制整个离线交付目录；只拿Setup不包含外部环境介质。

受限令牌直接验证通过：Administrators未启用、中完整性、应用文件可读、安装目录拒写、实际Launcher日志目录可写。同入口在本机5060实际GPU诊断返回0、CUDA Runtime12.8、cuDNN9.14模块路径匹配且无版本不匹配警告。未在不同真实账号运行进程。466项业务源码/公共bin保护哈希不变，公共bin当前262文件无新增；RC01的79项和RC02的88项发布哈希复核通过。四项基础依赖和两处exe.config重定向一致。

## 内容与来源

- Setup中的67项App文件与已测试RC02逐字节相同，模型与唯一LD.vpp继续沿用既定选择。RC02的88项发布校验通过；来源见app-provenance.csv。
- 仅新增/调整安装、依赖检测、启动入口和静态校验脚本。Input不固化本机RuntimePaths.json，安装时识别或人工填写CUDA/cuDNN，按原RC02的17项外部GPU组件及关键应用文件锁定内容核对。
- 固定资源默认放C:\Program Files\ElectrodeDet，桌面和开始菜单入口均走Launch.cmd，工作目录为App；启动日志固定生成在CommonApplicationData\ElectrodeDet\Logs\Launcher。
- 仅ElectrodeDet机器数据目录设置Users修改权限。配置与工作VPP由既有应用逻辑维护，授权仍按实际用户AppData\GearDet保存；升级整批覆盖程序，卸载保留数据和第三方环境。
- 软件安装包尚未签署发布者代码签名。安装与离线目录的SHA256由本批清单核对。

## 检查记录

| 记录 | 用途 |
| --- | --- |
| installer-build.log、setup-artifact.json | 最终ISCC7.1.0完整编译输出、Setup大小/哈希/签名状态 |
| app-provenance.csv、protected-source-bin.csv | 冻结RC02应用来源、受保护业务源码和公共bin基线 |
| installer-tests.json | 最终安装、快捷方式、升级、卸载及数据保留检查 |
| ordinary-user-test.json | 移除Administrators权限、去除特权、中完整性令牌模拟下的文件读取、日志写入、安装目录拒写验证；使用线程模拟，不等同于不同真实用户账号启动程序 |
| strict-missing-dependency.log、install.log、upgrade.log、uninstall.log | 最终安装器实际日志 |
| installed-runtime-paths.json、RuntimeEvidence/ | 本机生成配置、安装检测、启动/GPU日志和实际模块路径 |
| assembly-versions.json | 公共bin与本批四项基础程序集和exe.config重定向一致性 |
| media-provenance.csv、vendor-signatures.json、visionpro-media-assembly.json | 外部介质复制来源、有效厂商签名及VisionPro59.2匹配依据 |
| vc14-download.json、dotnet-download.json、mvs-media-signature.json | 新收集介质的来源/版本/签名/哈希或待核对边界 |
| verification.json、delivery-manifest.csv | 最终汇总、离线目录逐项校验清单 |
| Attempt01/、Attempt02/、Attempt03/、restricted-process-limitation.json | 制作阶段失败证据；受限子进程返回0xC0000142，改用受限令牌直接验证文件权限；不同账号启动/GPU尚未验证 |
| Scripts/ | 安装脚本、构建、验证与归档辅助脚本的审计副本；实际编译入口在批次Installer/ElectrodeDet.iss |

## 验证边界

本轮未重编C#；67项App源于RC02，沿用RC01真实VisionDection.sln、Debug/x64、11项目完整Build（0错误/83既有警告），原有83警告不因打包而消失。未启动完整主程序、未操作相机/光源/串口，也未在5070或无VS/断网目标机执行。独立GPU诊断只验证本机5060内存图像推理和模块路径。

本机未找到PATH中的HALCON12原生库；默认安装检查应拒绝缺项。本机安装流程测试显式使用ALLOWMISSINGDEPENDENCIES=1，缺项继续写入报告，不记为完整环境通过。HALCON12介质/授权、目标机串口/显卡驱动仍待补，MVS5.0.1介质内置运行版本尚待核对。详见[介质状态](MEDIA-STATUS.md)与[安装说明](安装说明及校验清单.md)。

目标机后续验收仍包含：真实普通账号、首次授权/密码和设备分配、模型/预热和真实样本、双相机/光源、VPP测量与模式切换、手动Y物理输出、外部X触发、忙状态/未登录权限提示、长期稳定性、升级和卸载的数据保留。
