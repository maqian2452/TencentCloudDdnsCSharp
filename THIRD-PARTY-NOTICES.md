# Third-Party Notices / 第三方声明

This document records third-party projects, packages, and references that are relevant to this repository.

本文档记录与本仓库相关的第三方项目、依赖包和参考来源。

## Project License / 项目许可证

`TencentCloudDdnsCSharp` is distributed under `GPL-3.0-only`.

`TencentCloudDdnsCSharp` 使用 `GPL-3.0-only` 发布。

## Reference Project / 参考项目

This project was designed with reference to:

- `xuchao1213/AliyunDdnsCSharp`
- Repository: https://github.com/xuchao1213/AliyunDdnsCSharp
- License: GPL-3.0

Early implementation decisions, especially the DDNS service shape, multi-provider IP resolution idea, configuration concepts such as `IpProviders`, `LOCAL`, `URL`, `AdapterName`, and `Prefix`, and the Windows service helper batch script pattern, were informed by or adapted from that project.

This repository replaces the Aliyun-specific implementation with a Tencent Cloud DNSPod implementation, uses a different .NET runtime stack, has a different DNSPod API client, and includes its own tests, documentation, release workflow, and configuration templates.

本项目早期设计参考了：

- `xuchao1213/AliyunDdnsCSharp`
- 仓库地址：https://github.com/xuchao1213/AliyunDdnsCSharp
- 许可证：GPL-3.0

本项目的 DDNS 服务形态、多 IP Provider 的解析思路、`IpProviders`、`LOCAL`、`URL`、`AdapterName`、`Prefix` 等配置概念，以及 Windows 服务辅助批处理脚本模式，受到该项目启发，部分脚本模式参考或改编自该项目。

当前仓库已将阿里云相关实现替换为腾讯云 DNSPod 实现，使用了不同的 .NET 运行栈、独立的 DNSPod API 客户端、测试、文档、发布工作流和安全配置模板。

## NuGet Packages / NuGet 依赖

The application depends on the following NuGet packages directly or indirectly:

- `Microsoft.Extensions.Hosting` and related `Microsoft.Extensions.*` packages: MIT License
- `Serilog`: Apache-2.0
- `Serilog.Extensions.Hosting`: Apache-2.0
- `TencentCloudSDK.Common`: Apache-2.0
- Transitive dependencies such as `Newtonsoft.Json` and `System.Text.Encodings.Web` are included through NuGet package dependency resolution and remain under their own upstream licenses.

应用程序直接或间接依赖以下 NuGet 包：

- `Microsoft.Extensions.Hosting` 及相关 `Microsoft.Extensions.*` 包：MIT License
- `Serilog`：Apache-2.0
- `Serilog.Extensions.Hosting`：Apache-2.0
- `TencentCloudSDK.Common`：Apache-2.0
- `Newtonsoft.Json`、`System.Text.Encodings.Web` 等传递依赖由 NuGet 依赖解析引入，并继续遵循其上游许可证。

## Release Packages / 发布包

Release ZIP files may contain self-contained .NET runtime components and NuGet package outputs required to run the application on Windows. Those components remain under their respective upstream licenses.

Release ZIP 包可能包含 Windows 自包含运行所需的 .NET 运行时组件和 NuGet 包产物。这些组件继续遵循其各自的上游许可证。

## Notes / 说明

This file is intended to make attribution and dependency information easier to find. It is not legal advice.

本文档用于集中说明来源、致谢和依赖许可证信息，不构成法律意见。
