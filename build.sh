#!/bin/bash
# Build, package, and deploy Termview for Trados Studio.
# Trados Studio must be CLOSED before running this script.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/Termview"
DIST_DIR="$SCRIPT_DIR/dist"
BUILD_DIR="$PROJECT_DIR/bin/Release"
DOTNET="${HOME}/.dotnet/dotnet"

PACKAGES_DIR="$LOCALAPPDATA/Trados/Trados Studio/18/Plugins/Packages"
UNPACKED_DIR="$LOCALAPPDATA/Trados/Trados Studio/18/Plugins/Unpacked/Termview"

echo "=== Building Termview ==="
"$DOTNET" build "$PROJECT_DIR/Termview.csproj" -c Release

echo ""
echo "=== Packaging Termview.sdlplugin (OPC format) ==="
mkdir -p "$DIST_DIR"
rm -f "$DIST_DIR/Termview.sdlplugin"
python "$SCRIPT_DIR/package_plugin.py" "$BUILD_DIR" "$DIST_DIR/Termview.sdlplugin"

echo ""
echo "=== Deploying to Trados Studio ==="

# Always wipe the Unpacked folder first so Trados re-extracts cleanly.
# If files are locked (Trados is still running), warn but continue —
# the new package will still be copied and will take effect after a restart.
if [ -d "$UNPACKED_DIR" ]; then
    echo "  Removing stale Unpacked/Termview..."
    if rm -rf "$UNPACKED_DIR" 2>/dev/null; then
        echo "  Unpacked folder cleaned."
    else
        echo "  WARNING: Could not fully remove Unpacked/Termview — Trados Studio may still be running."
        echo "  The new package was still copied. For a clean load, close Trados first, then run this script again."
    fi
fi

# Copy the new package.
mkdir -p "$PACKAGES_DIR"
cp "$DIST_DIR/Termview.sdlplugin" "$PACKAGES_DIR/Termview.sdlplugin"
echo "  Installed: $PACKAGES_DIR/Termview.sdlplugin"

echo ""
echo "=== Done — restart Trados Studio to load the updated plugin ==="
