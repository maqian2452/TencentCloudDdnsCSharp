#!/usr/bin/env bash
set -euo pipefail

SERVICE_NAME="${SERVICE_NAME:-TencentCloudDdns}"

sudo systemctl stop "$SERVICE_NAME"
sudo systemctl --no-pager status "$SERVICE_NAME" || true
