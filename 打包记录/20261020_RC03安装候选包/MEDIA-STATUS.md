# 离线环境介质状态（RC03候选包）

本目录由软件Setup之外的厂商介质组成。Setup只检查和引导，不会自动安装这些组件。安装前逐项核对本目录的SHA256清单；商业SDK按厂商正常安装和授权流程进行。

| 目录/介质 | 核对结果与安装方式 |
| --- | --- |
| GPU/cuda_12.8.0_571.96_windows.exe | 来自原下载目录，NVIDIA签名有效；安装CUDA12.8.0，保留已符合要求的目标机显卡驱动。安装后还须通过17项外部GPU文件锁定检查。 |
| GPU/cudnn_9.14.0_windows.exe | NVIDIA签名有效；选择CUDA12分支，要求组件9.14.0.64，不能使用CUDA13版。实际DLL目录由Setup识别或人工填写。 |
| VisionPro9_CR2/setup.exe | 完整DISK1介质101文件；启动程序和主MSI的Cognex签名有效。MSI的MsiAssemblyName表确认Cognex.VisionPro.Core、Amd64、59.2.0.0，与软件程序集匹配。尚未在新目标机安装/授权验证；通过setup.exe正常安装，保留配套cab与子目录。 |
| VisionPro9_CR2/ISSetupPrerequisites | 带签名有效的VC2010 x64 10.0.40219.325和VC2013 x64 12.0.30501，使用厂商安装步骤；该历史介质中的.NET4.0不能代替本项目所需的.NET>=4.7.2。 |
| VC/VC_redist.x64.exe | 本轮从微软官方链接下载，14.51.36247.0，Microsoft签名有效；未在本机运行。目标机已有满足要求的版本时跳过。 |
| DotNet/NDP48-x86-x64-AllOS-ENU.exe | 本轮从微软.NET官方下载页获取4.8离线运行时，Microsoft签名有效；未在本机运行。系统已内置兼容4.8/4.8.1时跳过，不能降级。 |
| PendingVerification/MVS_STD_5.0.1_260512.exe | 从原5.0.1 ZIP提取，Hikrobot签名有效；本机装有5.0.1及SDK Runtime x64 4.8.0.3，但未解包确认这份介质内置的运行组件，不能据此声称移机匹配已验证。目标安装后需识别x64 MvCameraControl.dll 4.8.0.3，再验收真实双相机。 |

尚缺/待目标机确认：HALCON12匹配安装介质及有效商业授权；目标串口适配器对应的厂家驱动；5070兼容NVIDIA显卡驱动。当前打包电脑PATH未找到HALCON12原生库，因此Setup默认依赖检查会拒绝，不能把本目录当作已齐套的断网整机环境。

为测试安装流程，Setup提供显式的“仅安装软件候选包”选项，允许先部署软件但保留缺项报告。该选项不验证商业授权、不初始化相机/串口，不代表检测软件全部功能可用。补齐后重新运行Setup生成本机路径并按安装说明验收。

官方来源：[VC14 x64](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)、[.NET4.8离线运行时](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)。GPU、VisionPro、MVS来自本机已有厂商介质，未运行其安装器。旧VC2017 14.13缓存介质未纳入离线交付。
