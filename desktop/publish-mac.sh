#!/usr/bin/env bash
# publish-mac.sh — Build y empaqueta Merkatto Desktop para macOS (desarrollo / testing)
# Uso: ./desktop/publish-mac.sh 1.0.0
# Ejecutar desde la raíz del repo.
#
# Requisitos: dotnet tool install -g vpk, npm

set -euo pipefail

VERSION="${1:?Usage: $0 <version>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PUBLISH_DIR="$ROOT/desktop/publish-mac"
RELEASE_DIR="$ROOT/desktop/releases-mac"

echo "=== Merkatto Desktop Publish v$VERSION (macOS) ==="

echo "[1/4] Building Angular..."
(cd "$ROOT/frontend" && npm run build)

WWWROOT="$ROOT/backend/src/Merkatto.Desktop/wwwroot"
rm -rf "$WWWROOT"
cp -r "$ROOT/frontend/dist/merkatto-web/browser" "$WWWROOT"
echo "   SPA copied to wwwroot"

echo "[2/4] Publishing .NET (osx-arm64, self-contained)..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$ROOT/backend/src/Merkatto.Desktop" \
    -c Release \
    -r osx-arm64 \
    --self-contained \
    -p:PackageVersion="$VERSION" \
    -p:AssemblyVersion="$VERSION" \
    -p:FileVersion="$VERSION" \
    -o "$PUBLISH_DIR"

echo "[3/4] Bundling with vpk..."
mkdir -p "$RELEASE_DIR"

# Locate vpk.dll regardless of installed version
VPK_DLL="$(find "$HOME/.dotnet/tools/.store/vpk" -name "vpk.dll" -path "*/net10.0/*" 2>/dev/null | sort -V | tail -1)"
if [ -z "$VPK_DLL" ]; then
  VPK_DLL="$(find "$HOME/.dotnet/tools/.store/vpk" -name "vpk.dll" 2>/dev/null | sort -V | tail -1)"
fi
echo "   Using vpk: $VPK_DLL"

dotnet "$VPK_DLL" bundle \
    --packId     Merkatto \
    --packVersion "$VERSION" \
    --packDir    "$PUBLISH_DIR" \
    --mainExe    Merkatto.Desktop \
    --outputDir  "$RELEASE_DIR" \
    --channel    osx

echo "[4/4] Done!"
echo "Bundle in: $RELEASE_DIR"
