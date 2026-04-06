# TencentCloudDdnsCSharp

简体中文 | [English](README.md)

这是一个运行在 Windows 下的腾讯云 DNSPod DDNS 客户端，支持 IPv6、本机或外部 URL 获取公网 IP、Windows 服务模式，以及 GitHub 自动打包 Release。

## 仓库结构

- `TencentCloudDdnsCSharp/` - 应用主项目
- `TencentCloudDdnsCSharp.Tests/` - 单元测试项目
- `TencentCloudDdnsCSharp.sln` - 解决方案文件

## 功能特点

- 支持 `A` 和 `AAAA` 记录
- 支持通过本机网卡或外部 URL 获取 IP
- 支持控制台模式和 Windows 服务模式
- 支持 `conf/` 目录下多配置文件并行运行
- 支持记录不存在时自动创建
- 支持通过 `RecordId` 精确绑定已有记录
- 已集成 GitHub Actions 构建、测试和 Release 打包

## 安全说明

- 不要提交真实的 `conf/*.json` 运行配置。
- 建议只提交 `conf/*.jsonc` 这类安全模板。
- `SecretId` 和 `SecretKey` 这类密钥应仅保留在本地。
- 构建产物、日志和 IDE 状态文件已经通过 `.gitignore` 忽略。

## 快速开始

构建和测试：

```powershell
dotnet build TencentCloudDdnsCSharp.sln
dotnet test TencentCloudDdnsCSharp.sln
```

控制台模式运行：

```powershell
cd TencentCloudDdnsCSharp
dotnet run -- -c
```

或者直接运行生成后的程序：

```powershell
TencentCloudDdnsCSharp.exe -c
```

## 配置方式

- `conf/*.json` 会被视为正式运行配置。
- `conf/*.jsonc` 仅作为示例模板，不会被程序自动加载。
- 可以从 [example.example.com.jsonc](TencentCloudDdnsCSharp/conf/example.example.com.jsonc) 开始，复制成你自己的本地 `*.json` 文件后再填写密钥和域名信息。

## 调试

第一次本地验证时，建议先用控制台模式，不要一开始就直接安装 Windows 服务：

```powershell
cd TencentCloudDdnsCSharp
dotnet run -- -c
```

推荐顺序：

- 从示例模板复制出你自己的本地 `conf/*.json`
- 填写 `SecretId`、`SecretKey`、域名、记录类型和 IP Provider
- 先在控制台模式跑一次，确认拿到的 IP 和 DNS 更新日志都正常
- 控制台模式验证通过后，再安装 Windows 服务

## GitHub Release

这个仓库已经准备好了自动发布 Windows Release 包。

创建并推送版本标签：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

标签推送后，GitHub Actions 会自动：

- restore、build、test
- 生成 `win-x64` 发布包
- 打 ZIP 包
- 创建 GitHub Release
- 上传 ZIP 作为 Release 附件

## 文档

- Detailed implementation notes in English: [TencentCloudDdnsCSharp/README.md](TencentCloudDdnsCSharp/README.md)
- 详细实现说明（中文）: [TencentCloudDdnsCSharp/README.zh-CN.md](TencentCloudDdnsCSharp/README.zh-CN.md)
- GitHub 仓库页面配置说明: [docs/GITHUB_SETUP.md](docs/GITHUB_SETUP.md)
- 首个公开版本 Release 文案: [docs/releases/v1.0.0.md](docs/releases/v1.0.0.md)

## 许可证

本仓库使用 `GPL-3.0-only` 许可证，详见 [LICENSE](LICENSE)。
