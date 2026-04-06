# TencentCloudDdnsCSharp

[简体中文](README.zh-CN.md) | English

Windows DDNS client for Tencent Cloud DNSPod, with IPv6 support, local or URL-based IP detection, Windows service mode, and automated GitHub Releases.

## Repository layout

- `TencentCloudDdnsCSharp/` - application project
- `TencentCloudDdnsCSharp.Tests/` - unit tests
- `TencentCloudDdnsCSharp.sln` - solution file

## Highlights

- Supports `A` and `AAAA` DNS records
- Supports local adapter IP detection and external URL providers
- Supports Windows console mode and Windows service mode
- Supports multiple config files in `conf/`
- Supports automatic record creation when enabled
- Supports precise record targeting through `RecordId`
- Includes CI and automated release packaging for GitHub

## Security notes

- Do not commit real `conf/*.json` runtime configs.
- Only commit safe examples such as `conf/*.jsonc`.
- Secrets such as `SecretId` and `SecretKey` must stay local.
- Build output, logs, and IDE state are ignored by `.gitignore`.

## Quick start

Build and test:

```powershell
dotnet build TencentCloudDdnsCSharp.sln
dotnet test TencentCloudDdnsCSharp.sln
```

Run in console mode:

```powershell
cd TencentCloudDdnsCSharp
dotnet run -- -c
```

Or run the built executable:

```powershell
TencentCloudDdnsCSharp.exe -c
```

## Configuration

- `conf/*.json` is treated as active runtime configuration.
- `conf/*.jsonc` is treated as example/template content only.
- Start from [example.example.com.jsonc](TencentCloudDdnsCSharp/conf/example.example.com.jsonc), copy it to your own local `*.json`, then fill in credentials and domain settings.

## Debugging

For a first local verification, use console mode instead of installing the Windows service immediately:

```powershell
cd TencentCloudDdnsCSharp
dotnet run -- -c
```

Recommended flow:

- copy the sample config to your own local `conf/*.json`
- fill in `SecretId`, `SecretKey`, domain, record type, and IP providers
- run once in console mode and confirm the resolved IP and DNS update logs
- install the Windows service only after the console run works as expected

## GitHub Releases

This repository is prepared to publish Windows release packages automatically.

Create and push a version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

After the tag is pushed, GitHub Actions will:

- restore, build, and test the solution
- publish the application for `win-x64`
- create a ZIP package
- create a GitHub Release
- upload the ZIP as a Release asset

## Documentation

- Detailed implementation notes in English: [TencentCloudDdnsCSharp/README.md](TencentCloudDdnsCSharp/README.md)
- 详细实现说明（中文）: [TencentCloudDdnsCSharp/README.zh-CN.md](TencentCloudDdnsCSharp/README.zh-CN.md)
- GitHub repository setup notes: [docs/GITHUB_SETUP.md](docs/GITHUB_SETUP.md)
- First public release notes: [docs/releases/v1.0.0.md](docs/releases/v1.0.0.md)

## License

This repository is licensed under `GPL-3.0-only`. See [LICENSE](LICENSE).
