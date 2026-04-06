# TencentCloudDdnsCSharp Application Notes

[简体中文](README.zh-CN.md) | English

This document focuses on implementation details for the application project inside `TencentCloudDdnsCSharp/`.

## Runtime modes

- `-c` runs the application in console mode for debugging and verification
- no argument starts the generic host in service-oriented mode
- the bundled batch files can install, start, stop, and uninstall the Windows service

The process entry point is in `Program.cs`, and the worker host is built on the .NET Generic Host stack.

## Configuration loading

The runtime config loader watches `conf/*.json` and ignores `conf/*.jsonc`.

Key behaviors:

- scans configs during startup
- watches file changes with `FileSystemWatcher`
- applies a short debounce to avoid partial-write parsing
- creates one worker per active config identity
- de-duplicates configs by record identity so equivalent tasks do not start twice

## Configuration validation

The config model validates the most important fields before a worker starts.

Validation includes:

- `RecordType` must be `A` or `AAAA`
- `IntervalMinutes`, `Ttl`, and `RecordId` are checked for basic validity
- provider-specific fields are validated per provider type
- default record line and identity rules are normalized before scheduling

## IP provider strategy

The project supports multiple IP providers and tries them in order until one succeeds.

Supported providers:

- `URL` reads the current public IP from an external endpoint
- `LOCAL` resolves an address from local network adapters

For IPv6 deployments, using a public `URL` provider first is often safer than relying only on local adapters, especially on machines that also expose `fdxx:` ULA addresses.

## DNSPod API flow

DNS updates are implemented through Tencent Cloud API v3 request signing with `TC3-HMAC-SHA256`.

The client currently uses these DNSPod operations:

- `DescribeRecordList`
- `CreateRecord`
- `ModifyDynamicDNS`

Important behaviors:

- a missing record can be treated as a normal branch instead of an exceptional failure
- `RecordId` can bind a worker to one exact record
- if a configured `RecordId` does not exist, the worker warns and skips instead of creating the wrong record

## Worker execution flow

Each config file maps to one `RecordWorker`.

The normal loop is:

1. resolve the current public IP
2. query existing records from DNSPod
3. narrow the target by `RecordId` or by host, type, and line
4. create a record when missing and allowed
5. skip when the IP value is unchanged
6. call dynamic update when the IP has changed

The worker also guards against ambiguous matches, so it does not silently update the wrong record when multiple records match one config.

## Logging

Logs are written into daily folders under `Logs/`.

Typical characteristics:

- one folder per day
- append-based file logging
- the main log file name is `INFO.LOG`
- exceptions and stack traces are written to the same log stream

## Troubleshooting tips

- If public DNS still does not show the new record immediately, wait for propagation and recursive cache refresh.
- If `LOCAL` returns the wrong IPv6 address, add `AdapterName` or `Prefix`, or place a `URL` provider first.
- If the wrong existing record could be matched, set `RecordId` explicitly.
- Keep example files as `.jsonc` so they are not auto-loaded as active runtime configs.

## Debugging workflow

When you debug locally, prefer this order:

1. create a local `conf/*.json` from the sample template
2. run the app with `-c`
3. verify that the resolved IP is the one you expect
4. confirm whether the worker reports `record created`, `ip not changed, skip`, or a warning branch
5. query public DNS separately if the API call succeeded but recursive DNS has not refreshed yet

Useful commands:

```powershell
dotnet run -- -c
TencentCloudDdnsCSharp.exe -c
nslookup -type=AAAA ipv6.example.com 8.8.8.8
```

What to look for in logs:

- worker startup messages that confirm the config was loaded
- resolved IP messages that show which provider succeeded
- create or update messages that include the target value
- warnings for missing `RecordId`, multiple matches, or IP resolution failure
