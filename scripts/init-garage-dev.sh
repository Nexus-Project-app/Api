#!/usr/bin/env bash
# Bootstrap Garage for local dev:
#   - initialises node layout
#   - creates access key + nexus-attachments bucket
#   - writes credentials to /opt/nexus/secrets/
# Usage: bash scripts/init-garage-dev.sh
set -euo pipefail

SECRETS_DIR="/opt/nexus/secrets"
BUCKET="nexus-attachments"
KEY_NAME="nexus-dev"

echo "==> Waiting for Garage to be ready..."
for i in $(seq 1 30); do
  if docker exec garage /garage status &>/dev/null; then
    break
  fi
  echo "    attempt $i/30..."
  sleep 2
done
docker exec garage /garage status

echo ""
echo "==> Fetching node ID..."
NODE_ID=$(docker exec garage /garage node id 2>/dev/null | grep -oE '^[0-9a-f]+' | head -1)
if [ -z "$NODE_ID" ]; then
  echo "ERROR: could not retrieve node ID. Is Garage running?"
  exit 1
fi
echo "    Node: $NODE_ID"

echo ""
echo "==> Assigning layout (zone=local, capacity=10G)..."
docker exec garage /garage layout assign -z local -c 10G "$NODE_ID" || true
docker exec garage /garage layout apply --version 1 || true

echo ""
echo "==> Creating access key '$KEY_NAME'..."
# Check if a key with this name already exists (list all keys and grep)
EXISTING_KEY=$(docker exec garage /garage key list 2>/dev/null | grep "$KEY_NAME" | grep -oE 'GK[A-Za-z0-9]+' | head -1 || true)

if [ -n "$EXISTING_KEY" ]; then
  echo "    Key '$KEY_NAME' already exists ($EXISTING_KEY) — deleting and recreating..."
  docker exec garage /garage key delete --yes "$EXISTING_KEY" 2>/dev/null || true
fi

KEY_OUTPUT=$(docker exec garage /garage key create "$KEY_NAME" 2>&1)
echo "$KEY_OUTPUT"
ACCESS_KEY=$(echo "$KEY_OUTPUT" | grep -i "key id"   | grep -oE 'GK[A-Za-z0-9]+')
SECRET_KEY=$(echo "$KEY_OUTPUT" | grep -i "secret key" | grep -oE '[0-9a-f]{64}')

if [ -z "$ACCESS_KEY" ] || [ -z "$SECRET_KEY" ]; then
  echo "ERROR: could not parse access key/secret. Check output above."
  exit 1
fi

echo ""
echo "==> Creating bucket '$BUCKET'..."
docker exec garage /garage bucket create "$BUCKET" 2>/dev/null || echo "    (bucket may already exist)"

echo ""
echo "==> Granting read/write access (by key ID) + public read on '$BUCKET'..."
docker exec garage /garage bucket allow "$BUCKET" --read --write --key "$ACCESS_KEY"
docker exec garage /garage bucket allow "$BUCKET" --read --anonymous 2>/dev/null || true

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APPSETTINGS="$SCRIPT_DIR/../src/Web.Api/appsettings.Development.json"

echo ""
echo "==> Writing credentials to appsettings.Development.json ..."
# Use python3 to do an in-place JSON merge (avoids jq dependency)
python3 - "$APPSETTINGS" "$ACCESS_KEY" "$SECRET_KEY" <<'PYEOF'
import sys, json

path, access_key, secret_key = sys.argv[1], sys.argv[2], sys.argv[3]
with open(path, "r") as f:
    data = json.load(f)

data.setdefault("Storage", {})
data["Storage"]["AccessKey"] = access_key
data["Storage"]["SecretKey"] = secret_key

with open(path, "w") as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
    f.write("\n")
PYEOF
echo "    Written to $APPSETTINGS"

# Also try writing to secrets dir if we have permission (no sudo required in that case)
if mkdir -p "$SECRETS_DIR" 2>/dev/null; then
  echo -n "$ACCESS_KEY" > "$SECRETS_DIR/garage_access_key"
  echo -n "$SECRET_KEY" > "$SECRETS_DIR/garage_secret_key"
  echo "    Also written to $SECRETS_DIR/"
fi

echo ""
echo "====================================================="
echo " Garage dev setup complete!"
echo "====================================================="
echo " Access Key : $ACCESS_KEY"
echo " Secret Key : $SECRET_KEY"
echo " Endpoint   : http://localhost:3900"
echo " Bucket     : $BUCKET"
echo "====================================================="
echo ""
echo "appsettings.Development.json updated with credentials."
echo "Start the API with:"
echo "  cd Api && ~/.dotnet/dotnet run --project src/Web.Api"
