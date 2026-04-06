# GitHub Repository Setup

This file collects the GitHub page content that is easiest to prepare locally and paste into the repository settings after the first push.

## About section

### Description

Use this as the repository description:

```text
Windows DDNS client for Tencent Cloud DNSPod with IPv6 support, local/URL IP detection, Windows service mode, and automated GitHub Releases.
```

### Suggested topics

Add these topics in the GitHub repository settings:

- `ddns`
- `dynamic-dns`
- `dnspod`
- `tencent-cloud`
- `tencentcloud`
- `ipv6`
- `windows`
- `windows-service`
- `dotnet`
- `csharp`

### Website

Leave the website field empty for now unless you later add a project homepage or documentation site.

## Recommended pinned sections

If you want the repository home page to read well for first-time visitors, this order works nicely:

1. Root `README.md` for quick start and release usage
2. `TencentCloudDdnsCSharp/README.md` for implementation details
3. `docs/releases/v1.0.0.md` as the first public release notes source

## First release

### Release title

```text
v1.0.0 - Initial public release
```

### Release notes

The prepared release notes live in [docs/releases/v1.0.0.md](releases/v1.0.0.md).

If you create the first release manually in the GitHub web UI, paste that file into the release description.

If you publish by pushing a tag and let GitHub Actions create the release automatically, GitHub will generate notes from commits and PR metadata. You can then edit the release body and replace it with the prepared text if you want a cleaner first release page.

## First push checklist

1. Create an empty public repository on GitHub.
2. Push `main`.
3. Open the repository settings and fill in the About description and topics above.
4. Push tag `v1.0.0`.
5. Wait for the `release` workflow to finish.
6. Open the GitHub Release page and confirm the ZIP asset is attached.
7. Replace the auto-generated release body with the prepared notes if needed.
