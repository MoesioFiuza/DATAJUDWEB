#!/usr/bin/env bash
# Atualiza DataWeb + cliente assíncrono do Scraper no servidor.
# Uso (no servidor, como root):
#   bash /opt/apps/dataweb/repo/infra/scraper-integration/deploy-async.sh

set -euo pipefail

DATAWEB_REPO="${DATAWEB_REPO:-/opt/apps/dataweb/repo}"
SCRAPER_REPO="${SCRAPER_REPO:-/opt/apps/scraper/repo}"
SCRAPER_CLIENT="${SCRAPER_REPO}/app/services/dataweb_client.py"

echo "==> Pull DataWeb"
cd "$DATAWEB_REPO"
git pull

echo "==> Rebuild DataWeb API"
cd "$DATAWEB_REPO/infra"
docker compose up -d --build dataweb-api

echo "==> Nginx (620s no /dataweb)"
if [ -f /etc/nginx/sites-available/dataweb ]; then
  if grep -q 'proxy_read_timeout 300s' /etc/nginx/sites-available/dataweb; then
    sed -i 's/proxy_read_timeout 300s/proxy_read_timeout 620s/g' /etc/nginx/sites-available/dataweb
    sed -i 's/proxy_send_timeout 300s/proxy_send_timeout 620s/g' /etc/nginx/sites-available/dataweb
  fi
  if ! grep -q 'proxy_connect_timeout 620s' /etc/nginx/sites-available/dataweb; then
    sed -i '/location \/dataweb {/,/^    }/ s/proxy_set_header X-Forwarded-Proto \$scheme;/proxy_set_header X-Forwarded-Proto \$scheme;\n        proxy_connect_timeout 620s;/' /etc/nginx/sites-available/dataweb || true
  fi
  nginx -t && systemctl reload nginx
fi

echo "==> Cliente assíncrono no Scraper"
if [ -d "$SCRAPER_REPO" ]; then
  mkdir -p "$(dirname "$SCRAPER_CLIENT")"
  cp "$DATAWEB_REPO/infra/scraper-integration/dataweb_client.py" "$SCRAPER_CLIENT"
  echo "Copiado para $SCRAPER_CLIENT"
  if [ -d "$SCRAPER_REPO/venv" ]; then
    "$SCRAPER_REPO/venv/bin/pip" install -q openpyxl requests || true
  fi
  if [ -f "$SCRAPER_REPO/infra/docker-compose.yml" ]; then
    cd "$SCRAPER_REPO/infra" && docker compose restart || true
  fi
else
  echo "Scraper não encontrado em $SCRAPER_REPO — copie dataweb_client.py manualmente."
fi

echo "==> Health"
curl -sf "http://127.0.0.1:5267/dataweb/health" && echo ""
echo "Deploy concluído."
