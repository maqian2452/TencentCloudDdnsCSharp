# 树莓派部署指南

本文说明如何把 `TencentCloudDdnsCSharp` 部署到树莓派或其他 Linux ARM64 设备上。

## 前提条件

- 树莓派系统建议使用 64 位 Raspberry Pi OS 或其他 `linux-arm64` 发行版。
- 系统需要支持 `systemd`。
- 发布包是 self-contained，不要求树莓派预装 .NET Runtime。
- 真实的 `conf/*.json` 配置文件只放在树莓派本机，不要提交到 GitHub。

## 获取发布包

在树莓派上可以直接下载 GitHub Release 中的 `linux-arm64` ZIP：

```bash
mkdir -p ~/apps
cd ~/apps
wget https://github.com/maqian2452/TencentCloudDdnsCSharp/releases/download/v1.0.0/TencentCloudDdnsCSharp-v1.0.0-linux-arm64.zip
unzip TencentCloudDdnsCSharp-v1.0.0-linux-arm64.zip -d TencentCloudDdnsCSharp
cd TencentCloudDdnsCSharp
```

把命令里的版本号替换成实际发布版本，例如 `v1.0.7`。

如果发布包已经下载到 Windows，也可以通过 `scp` 复制到树莓派：

```powershell
scp .\TencentCloudDdnsCSharp-v1.0.0-linux-arm64.zip maqian@192.168.10.13:/home/maqian/apps/
```

## 准备配置

复制示例配置为真实运行配置：

```bash
cp conf/example.example.com.jsonc conf/ipv6.example.com.json
nano conf/ipv6.example.com.json
```

需要填写：

- `SecretId`
- `SecretKey`
- `Domain`
- `SubDomain`
- `RecordType`
- `IpProviders`

树莓派上如果使用本机 IPv6，建议优先使用 `LOCAL` Provider。Linux 的网卡名通常不是 `WLAN`，可以用下面的命令查看：

```bash
ip -6 addr
ip link
```

常见无线网卡名可能是 `wlan0`，有线网卡名可能是 `eth0`。示例：

```json
{
  "Provider": "LOCAL",
  "AdapterName": "wlan0",
  "Prefix": "2409:"
}
```

如果不确定 `AdapterName`，可以先只配置 `Prefix`，让程序扫描所有可用网卡后按评分选择。

## 控制台验证

第一次迁移时，先不要安装服务，建议先在控制台模式跑一次：

```bash
chmod +x ./TencentCloudDdnsCSharp
./TencentCloudDdnsCSharp -c
```

重点确认：

- 配置是否成功加载。
- 解析到的 IPv6 是否是你希望写入 DNSPod 的地址。
- DNSPod 返回的是创建、更新，还是 `ip not changed, skip`。

日志文件位于：

```bash
Logs/$(date +%F)/INFO.LOG
```

可以用下面的命令查看：

```bash
tail -f "Logs/$(date +%F)/INFO.LOG"
```

## 安装为 systemd 服务

控制台验证通过后，安装并启动服务：

```bash
chmod +x ./*.sh
sudo ./InstallLinux.sh
```

默认服务名是 `TencentCloudDdns`，默认运行用户是执行 `sudo` 前的用户，例如 `maqian`。

常用命令：

```bash
sudo ./StartLinux.sh
sudo ./StopLinux.sh
sudo systemctl status TencentCloudDdns --no-pager
journalctl -u TencentCloudDdns -f
```

如果想指定服务用户：

```bash
sudo SERVICE_USER=maqian ./InstallLinux.sh
```

## 卸载服务

卸载只会删除 systemd service，不会删除程序目录和 `conf/*.json` 配置：

```bash
sudo ./UninstallLinux.sh
```

## 更新版本

更新时建议：

1. 停止服务：`sudo ./StopLinux.sh`
2. 备份 `conf/*.json`
3. 解压新的 `linux-arm64` 发布包覆盖程序文件
4. 放回真实配置
5. 控制台验证：`./TencentCloudDdnsCSharp -c`
6. 重启服务：`sudo ./StartLinux.sh`

## 安全提醒

不要把树莓派密码、腾讯云密钥或真实 `conf/*.json` 放进 Git、README、Release 包或聊天记录里。公开仓库只保留 `.jsonc` 示例模板。
