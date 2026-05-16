# Git 与 GitHub 入门说明

这份文档面向当前仓库的实际使用场景，帮助你理解：

- `Git` 是什么
- `GitHub` 是什么
- 这个项目平时应该怎么提交、推送、发版
- 遇到常见问题时怎么判断和处理

## 1. Git 和 GitHub 的区别

可以先用一句话记住：

- `Git`：本地版本管理工具
- `GitHub`：托管 Git 仓库的网站平台

通俗一点：

- 你在电脑上写代码、提交历史，用的是 `Git`
- 你把仓库推到网上、做 Release、跑 Actions，用的是 `GitHub`

## 2. 这个仓库里哪些目录和 GitHub 相关

当前公开仓库根目录里，常见目录作用如下：

- `TencentCloudDdnsCSharp/`：主程序代码
- `TencentCloudDdnsCSharp.Tests/`：测试代码
- `.github/`：GitHub 平台配置
- `docs/`：我们自己写的补充说明文档
- `README.md` / `README.zh-CN.md`：仓库首页说明
- `LICENSE`：开源许可证
- `.gitignore`：告诉 Git 哪些文件不要提交

特别注意：

- `.github/workflows/*.yml` 会被 GitHub Actions 执行
- `.github/release.yml` 会影响自动生成 Release Notes 的分类
- `docs/*.md` 只是普通说明文档，不会被 GitHub 自动执行

## 3. 最常见的 Git 概念

### 仓库 repository

一个项目目录如果被 Git 管理，就叫一个仓库。

### 提交 commit

一次提交就是一次“保存快照”。

例如：

```powershell
git add .
git commit -m "Fix DNS update bug"
```

### 分支 branch

分支可以理解成一条独立开发线。

这个项目最常用的是：

- `main`：主分支

### 标签 tag

标签通常用来标记版本。

例如：

```powershell
git tag v1.0.3
```

### 远程仓库 remote

远程仓库就是 GitHub 上的仓库地址。

例如这个项目的远程：

```text
https://github.com/maqian2452/TencentCloudDdnsCSharp.git
```

## 4. 最常见的 GitHub 概念

### About

仓库首页右侧的简介区，通常包括：

- 描述 description
- 网站 homepage
- 主题 topics

### Actions

GitHub 的自动化执行系统。

本仓库目前有两个工作流：

- `dotnet`：自动构建和测试
- `release`：自动打包和发布 Release

### Releases

发布版本页面，用来放：

- 版本号
- 更新说明
- 可下载 ZIP 包

本项目当前是“推送 tag 自动创建 Release”。

## 5. 这个项目最常用的本地命令

在仓库根目录执行：

### 构建

```powershell
dotnet build .\TencentCloudDdnsCSharp.sln
```

### 测试

```powershell
dotnet test .\TencentCloudDdnsCSharp.sln
```

### 本地控制台运行

```powershell
dotnet run --project .\TencentCloudDdnsCSharp\TencentCloudDdnsCSharp.csproj -- -c
```

### 本地发布主项目

不要直接对整个 solution 做 `publish`，应该明确指定主项目：

```powershell
dotnet publish .\TencentCloudDdnsCSharp\TencentCloudDdnsCSharp.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish\win-x64
```

## 6. 这个项目最常见的 Git 命令

### 查看状态

```powershell
git status
```

### 查看最近提交

```powershell
git log --oneline -n 10
```

### 添加并提交修改

```powershell
git add .
git commit -m "Describe what changed"
```

### 推送主分支

```powershell
git push origin main
```

### 创建并推送版本标签

```powershell
git tag v1.0.4
git push origin v1.0.4
```

## 7. 这个项目的发版流程

这是你以后最常用的一套流程。

### 第一步：本地改代码

先修改代码、配置工作流或文档。

### 第二步：本地验证

```powershell
dotnet build .\TencentCloudDdnsCSharp.sln
dotnet test .\TencentCloudDdnsCSharp.sln
```

### 第三步：提交并推送到 main

```powershell
git add .
git commit -m "Fix something"
git push origin main
```

### 第四步：打版本标签

```powershell
git tag v1.0.4
git push origin v1.0.4
```

### 第五步：等待 GitHub Actions 自动发版

GitHub 会根据 `.github/workflows/release.yml`：

- restore
- build
- test
- publish
- 打 ZIP
- 创建 Release

## 8. 这次项目里遇到过的典型问题

这些问题以后你很可能还会再遇到。

### 1. Push Protection 拦截“看起来像密钥”的示例值

即使只是示例配置，只要格式太像真实密钥，GitHub 也可能拒绝推送。

处理方法：

- 示例里不要用真实格式的密钥占位值
- 尽量改成 `YOUR_SECRET_ID` 这种普通文本

### 2. Release 工作流成功，但包内容不对

例如：

- 脚本没有被带进包
- 不该发布的文件进了包

处理方法：

- 检查 `.csproj` 里的 `CopyToPublishDirectory`
- 检查发布后 ZIP 的实际内容

### 3. `dotnet publish` 不要直接对 solution 跑

因为测试项目不是给你发布成 exe 的。

正确做法：

```powershell
dotnet publish .\TencentCloudDdnsCSharp\TencentCloudDdnsCSharp.csproj ...
```

### 4. GitHub Release 已经发出后，不建议反复改旧版本

例如：

- `v1.0.0` 已经公开后
- 后续修复更适合发 `v1.0.1`

这样版本语义更清楚。

### 5. 单文件发布和多文件发布的区别

如果觉得 Release 目录太大，可以改成：

- `PublishSingleFile=true`

本项目后来就是这样优化的。

## 9. 常见文件到底该不该提交

### 应该提交

- 源代码
- 测试代码
- `README`
- `.github` 工作流
- `LICENSE`
- 安全示例配置 `.jsonc`

### 不应该提交

- 真实密钥
- `conf/*.json` 正式运行配置
- `bin/`
- `obj/`
- `Logs/`
- `.dotnet/`
- `.nuget/`

## 10. 你可以先记住的最小工作流

如果你暂时不想记太多，只要记住下面这 6 步就够用了：

1. 改代码
2. `dotnet build`
3. `dotnet test`
4. `git add` + `git commit`
5. `git push origin main`
6. `git tag vx.y.z` + `git push origin vx.y.z`

## 11. 后续建议

如果你以后还想继续熟悉 GitHub，建议按这个顺序学习：

1. 先熟悉 `status / add / commit / push`
2. 再熟悉 `tag / release / actions`
3. 最后再学 `branch / merge / pull request`

对你当前这个项目来说，前两步已经足够覆盖大多数日常使用场景。
