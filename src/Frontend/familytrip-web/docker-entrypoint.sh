#!/bin/sh
set -e

LOCAL_ENV="src/environments/environment.local.ts"
TOKEN="${MAPBOX_API_KEY:-}"

cat > "$LOCAL_ENV" <<EOF
export const environmentLocal = {
  mapboxToken: '${TOKEN}'
};
EOF

exec "$@"
