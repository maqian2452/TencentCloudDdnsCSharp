#!/usr/bin/env bash
set -euo pipefail

SERVICE_NAME="${SERVICE_NAME:-TencentCloudDdns}"
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_EXEC="$APP_DIR/TencentCloudDdnsCSharp"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"
SERVICE_USER="${SERVICE_USER:-${SUDO_USER:-$(id -un)}}"

if ! command -v systemctl >/dev/null 2>&1; then
  echo "systemctl was not found. This installer requires a Linux system with systemd."
  exit 1
fi

if [[ ! -f "$APP_EXEC" ]]; then
  echo "Application executable was not found: $APP_EXEC"
  exit 1
fi

chmod +x "$APP_EXEC"
mkdir -p "$APP_DIR/conf" "$APP_DIR/Logs"

if id "$SERVICE_USER" >/dev/null 2>&1; then
  SERVICE_GROUP="$(id -gn "$SERVICE_USER")"
  sudo chown -R "$SERVICE_USER:$SERVICE_GROUP" "$APP_DIR/conf" "$APP_DIR/Logs"
else
  echo "Service user does not exist: $SERVICE_USER"
  exit 1
fi

sudo tee "$SERVICE_FILE" >/dev/null <<SERVICE
[Unit]
Description=Tencent Cloud DNSPod DDNS client
Wants=network-online.target
After=network-online.target

[Service]
Type=simple
WorkingDirectory=$APP_DIR
ExecStart=$APP_EXEC
Restart=always
RestartSec=30
User=$SERVICE_USER
Environment=DOTNET_ENVIRONMENT=Production
SyslogIdentifier=$SERVICE_NAME

[Install]
WantedBy=multi-user.target
SERVICE

sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl restart "$SERVICE_NAME"
sudo systemctl --no-pager status "$SERVICE_NAME"
