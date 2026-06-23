#!/usr/bin/env bash
set -euo pipefail

SERVICE_NAME="${SERVICE_NAME:-TencentCloudDdns}"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

sudo systemctl stop "$SERVICE_NAME" || true
sudo systemctl disable "$SERVICE_NAME" || true
sudo rm -f "$SERVICE_FILE"
sudo systemctl daemon-reload
echo "Service $SERVICE_NAME has been uninstalled. Application files and conf/*.json were left unchanged."
