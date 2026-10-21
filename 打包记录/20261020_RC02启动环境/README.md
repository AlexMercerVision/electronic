# RC02启动环境记录（2026-10-20）

完成：建立E:\VB_Det_PACK\20261020_50Series_RC02，保留RC01；专用入口为03_Release/Launch.cmd。
App67文件与RC01逐字节一致，同批DLL/模型/LD.vpp不变，本轮无C#修改或重编。沿用RC01实际VisionDection.sln Debug/x64全11项目Build（0错误/83既有警告），本轮复核同批引用、四项基础依赖与重定向一致。

锁定CUDA12.8.0及cuDNN9.14.0.64 CUDA12版，共17个外部GPU组件、16个核心应用文件、1个GPU诊断程序；启动前校验版本、长度和SHA256，仅设置新建进程环境。Windows PowerShell兼容性修正使用.NET SHA256与纯.NET模块清单读取；历史开发中未完成的Preflight记录不算通过，最终通过记录见verification.json、entry-tests.json与GPU验证目录最新两组。

最终同一Launch.cmd的两次独立GPU检查均通过：实际Runtime API12.8，7个cuDNN模块来自9.14/bin/12.9，2个cuBLAS来自CUDA12.8，Paddle/phi/MKL链全部来自RC02 App，推理ret=0且版本不匹配警告消失。旧cuDNN9.8、CUDA13版cuDNN、缺失CUDA目录三项拒绝检查通过。

RC01共1695文件、原公共bin264文件核对不变；RC02发布清单88项。运行入口/依赖清单/哈希均在03_Release，本目录保留制作脚本、清单来源、模块路径与原始验证日志，记录不写C盘。

未启动完整主程序、未操作相机/串口/IO、未验证真实样本准确率/5070/连续稳定性、未制作安装器或收集外部介质。测试主程序由用户执行：先关闭旧程序，再双击RC02的Launch.cmd；启动器记录在批次06_RunLogs，业务日志服从现有配置。现机模型/VPP保存路径仍可指向D盘，原配置/授权不重置、不覆盖。
