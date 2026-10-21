# 电极帽缺陷检测 · 打包记录

更新：2026-10-20。集中查看这几轮的部署、受控发布和GPU环境检查记录；后续记录继续放本目录。原工程程序、公共bin、现场配置/授权未因归档而改变。

## 查看入口

| 记录 | 内容 |
| --- | --- |
| [RC03安装候选包](E:/jieyawenjian/VB_Det/打包记录/20261020_RC03安装候选包/README.md) | 原生x64 Setup、默认Program Files安装/升级/卸载、受限令牌文件权限、本机同入口GPU验证及离线校验清单；5070/真实普通账号及硬件验收待执行 |
| [第三步安装器方案](E:/jieyawenjian/VB_Det/打包记录/20261020_第三步安装器方案/方案.md) | 制作安装器前的历史方案与用户测试日志复核；当前Setup及验证结果见RC03记录 |
| [RC02启动环境与验证](E:/jieyawenjian/VB_Det/打包记录/20261020_RC02启动环境/README.md) | 专用Launch.cmd入口、CUDA12.8/cuDNN9.14锁定、同入口GPU验证及拒绝检查；同目录verification.json、entry-tests.json保留结果 |
| [cuDNN分析与下一步](E:/jieyawenjian/VB_Det/打包记录/20261020_cuDNN检查/分析与下一步.md) | 原因、最小方案、验证边界及后续部署顺序 |
| [cuDNN对比结果](E:/jieyawenjian/VB_Det/打包记录/20261020_cuDNN检查/results.json) | 9.8基线与9.14 CUDA12版的实际加载路径/推理结果；同目录含两组原始日志与模块清单 |
| [GPU架构检查](E:/jieyawenjian/VB_Det/打包记录/20261020_cuDNN检查/architecture.json) | 当前phi及paddle_inference_c仅见sm_120、未发现PTX；30/40系另行准备推理库 |
| [RC01验证结果](E:/jieyawenjian/VB_Det/打包记录/20261020_RC01发布审计/verification.json) | 11项目构建、同批引用、基础依赖版本、原文件保护及已验证/未验证项目 |
| [RC01构建日志](E:/jieyawenjian/VB_Det/打包记录/20261020_RC01发布审计/solution-build.log) | Debug/x64完整构建，0错误/83既有警告 |
| [发布文件来源](E:/jieyawenjian/VB_Det/打包记录/20261020_RC01发布审计/payload-provenance.json) | App文件来源和SHA256；同目录含托管/原生依赖清单、GPU基线锁定及源码快照清单 |
| [部署首步验证](E:/jieyawenjian/VB_Det/打包记录/部署首步验证摘要/deployment-20261019/DELIVERY.md) | 路径、新机配置、串口绑定、首次授权的软件验证及边界 |
| [相机与共享光源串口验证](E:/jieyawenjian/VB_Det/打包记录/部署首步验证摘要/hardware-controls-20261020/DELIVERY.md) | 部署首步之后的相机启动/手动连接与共享光源串口修正 |
| [迁移清单](E:/jieyawenjian/VB_Det/打包记录/迁移清单.json) | 55个归档文件的原路径、归档路径、迁移前后哈希；21文件迁出C盘，34文件从E盘保留原件并集中复制 |

## 当前状态

- 第三步已生成RC03软件安装候选包：[离线交付目录](E:/VB_Det_PACK/20261020_50Series_RC03_Installer/离线交付)。默认安装/升级/卸载返回0，11项既有数据哈希保留；桌面/开始菜单走专用入口，启动日志写ProgramData。受限令牌文件权限、本机5060同入口GPU检查通过；真实不同普通账号及5070断网整机未验收。RC01/RC02冻结清单仍通过。
- 第一阶段部署代码已完成软件验证；实际解决方案现名`VisionDection.sln`，原名`VisionBasedLaser.sln`。
- 第二步受控发布目录已完成：[RC01批次说明](E:/VB_Det_PACK/20261020_50Series_RC01/README.md)，[客户交付及安装器输入](E:/VB_Det_PACK/20261020_50Series_RC01/03_Release/README.md)。App为67文件/1109.2MiB，完整交付清单79文件；仅当前模型和用户选定的D:\data\LD.vpp，未导入旧机器参数和授权。
- RC01冻结环境仍是CUDA12.8/cuDNN9.8。RC02已建立专用进程环境入口：[测试Launch.cmd](E:/VB_Det_PACK/20261020_50Series_RC02/03_Release/Launch.cmd)，同入口CUDA12.8/cuDNN9.14 CUDA12版独立推理验证通过，版本不匹配警告消失；全局环境不变。用户后续本机日志已确认模型/预热、VPP、双相机显示首帧、A/B检测和IO发送记录；物理输出、外部触发、准确率/稳定性及5070仍需专门验收，详见第三步方案。
- 下一步：补齐HALCON12介质/授权、目标串口/显卡驱动，核对MVS5.0.1介质内置Runtime→无VS的5070断网验收（真实普通账号、真实样本及连续运行、相机/光源、物理IO/外部触发、测量/模式切换、升级卸载保留）。30/40系另行准备并验证原生推理库；20系不纳入当前包。

## 归档说明

- C盘诊断文件已迁入本目录，迁移先复制并逐项校验，再删除对应C盘原文件；同目录的CUDA缓存也一并保留。`compare.py`已改为向自身目录输出，其他原始日志保持原字节。
- `20261020_RC01发布审计`和`部署首步验证摘要`是集中查看的副本，原发布批次、源码快照和历史验证产物保持原位；打包产物仍在E:\VB_Det_PACK，不把整个源码或模型再复制进记录目录。
- `20261020_打包辅助脚本`保存C盘原辅助文件用于追溯；其中`MetadataAudit.cs`、`metadata.ps1`为未采用的读取方案，最终依据是PE静态解析结果和原始日志。历史脚本只作归档，不在副本目录直接执行构建/打包；重建使用原批次04_Audit/Rebuild-Snapshot.ps1，避免路径推导落到错误目录。
- 迁移清单中的C盘原路径是历史来源，不表示这些文件仍需保留在C盘；原始日志中的C盘系统DLL/安装位置同样是运行证据，无需改写。
- 此前归档轮次只迁移记录、整理索引及更新AGENTS.md/PROJECT_CONTEXT.md，未改业务源码、工程、运行配置或GPU环境；未重新构建或执行硬件验证。本轮RC03安装器结果见上方新记录。
