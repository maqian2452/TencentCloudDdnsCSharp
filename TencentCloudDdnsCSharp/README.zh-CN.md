# TencentCloudDdnsCSharp 项目实现说明

简体中文 | [English](README.md)

本文档说明 `TencentCloudDdnsCSharp/` 目录下应用项目的实现细节。

## 运行模式

- `-c` 以控制台模式运行，适合调试和验证
- 不带参数时按服务型主机方式启动
- 项目附带的批处理脚本可用于安装、启动、停止和卸载 Windows 服务

程序入口在 `Program.cs`，整体运行基于 .NET Generic Host。

## 配置加载

运行时只加载 `conf/*.json`，不会自动执行 `conf/*.jsonc`。

主要行为包括：

- 启动时扫描配置文件
- 通过 `FileSystemWatcher` 监听新增、修改、删除和重命名
- 对频繁变动做短暂防抖，避免读取到半写入状态
- 每个有效配置对应一个 `RecordWorker`
- 按记录身份去重，避免等价任务被重复启动

## 配置校验

配置模型会在启动 worker 前先做基本校验。

主要包括：

- `RecordType` 只能是 `A` 或 `AAAA`
- `IntervalMinutes`、`Ttl`、`RecordId` 会进行基本合法性检查
- `IpProviders` 会按不同 Provider 类型分别校验
- 默认线路和记录身份会先做标准化处理

## IP 获取策略

项目支持多个 IP Provider，按配置顺序依次尝试，成功一个就停止后续尝试。

当前支持：

- `URL`：从外部接口获取当前公网 IP
- `LOCAL`：从本机网卡中解析地址

对 IPv6 场景来说，通常建议把公网 `URL` Provider 放在前面，再用 `LOCAL` 兜底。这样可以降低误选 `fdxx:` 这类 ULA 私有 IPv6 地址的风险。

## DNSPod API 调用

DNS 更新逻辑使用腾讯云 API v3 的 `TC3-HMAC-SHA256` 签名方式直接请求 DNSPod 接口。

当前主要使用的接口有：

- `DescribeRecordList`
- `CreateRecord`
- `ModifyDynamicDNS`

几个关键行为：

- “记录不存在”会被视为正常分支，而不是致命异常
- 可以通过 `RecordId` 将任务绑定到某一条精确记录
- 如果配置了错误的 `RecordId`，程序会告警并跳过，不会误创建新记录

## 单个任务执行流程

每个配置文件对应一个 `RecordWorker`。

标准流程如下：

1. 获取当前公网 IP
2. 查询 DNSPod 中的现有记录
3. 按 `RecordId` 或“主机名 + 类型 + 线路”缩小目标范围
4. 如果记录不存在且允许创建，则自动创建
5. 如果 IP 没有变化，则跳过更新
6. 如果 IP 已变化，则执行动态更新

当同一份配置可能命中多条记录时，worker 会直接告警并跳过，避免把错误记录更新掉。

## 日志

日志会写入 `Logs/` 下按天分目录的文件中。

主要特点：

- 每天一个目录
- 追加写入
- 主日志文件名为 `INFO.LOG`
- 异常和堆栈也会写入同一套日志流

## 排错建议

- 如果记录刚创建但公共 DNS 还查不到，通常先等传播和递归缓存刷新。
- 如果 `LOCAL` 拿到的 IPv6 不对，可以补 `AdapterName` 或 `Prefix`，或者把 `URL` Provider 放到前面。
- 如果担心匹配到错误的旧记录，建议显式配置 `RecordId`。
- 示例文件建议保持为 `.jsonc`，避免被程序当成正式配置自动加载。

## 调试流程

本地调试时，建议按下面的顺序来：

1. 先从示例模板复制出一个本地 `conf/*.json`
2. 使用 `-c` 启动程序
3. 确认程序拿到的 IP 就是你期望更新的地址
4. 观察 worker 是输出 `record created`、`ip not changed, skip`，还是进入了告警分支
5. 如果 API 调用成功但公共 DNS 还没刷新，再单独查询公共 DNS

常用命令：

```powershell
dotnet run -- -c
TencentCloudDdnsCSharp.exe -c
nslookup -type=AAAA ipv6.example.com 8.8.8.8
```

调试时重点看这些日志：

- worker 启动日志，确认配置是否已被加载
- IP 获取成功日志，确认是哪一个 Provider 生效
- 创建或更新记录的日志，确认最终写入值
- `RecordId` 不存在、多条记录命中、IP 获取失败这类告警日志
