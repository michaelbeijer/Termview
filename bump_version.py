"""Bump Supervertaler.Trados version across all version files.

Usage:
    python bump_version.py <old_version> <new_version>

Example:
    python bump_version.py 4.12.0 4.13.0

Updates:
    - plugin.xml (UTF-16 LE): plugin version attribute + assembly binding versions
    - pluginpackage.manifest.xml: <Version> element
    - Supervertaler.Trados.csproj: <Version> and <InformationalVersion>
"""
import os
import sys
import re

BASE_DIR = os.path.dirname(__file__)
SRC_DIR = os.path.join(BASE_DIR, "src", "Supervertaler.Trados")
PLUGIN_XML = os.path.join(SRC_DIR, "Supervertaler.Trados.plugin.xml")
MANIFEST_XML = os.path.join(SRC_DIR, "pluginpackage.manifest.xml")
CSPROJ = os.path.join(SRC_DIR, "Supervertaler.Trados.csproj")


def bump_plugin_xml(old_four, new_four):
    """Update plugin.xml (UTF-16 LE) — plugin version + assembly bindings."""
    with open(PLUGIN_XML, "rb") as f:
        raw = f.read()

    if raw[:2] == b'\xff\xfe':
        text = raw[2:].decode("utf-16-le")
    else:
        text = raw.decode("utf-16-le")

    # Plugin version attribute
    c1 = text.count(f'version="{old_four}"')
    text = text.replace(f'version="{old_four}"', f'version="{new_four}"')

    # Assembly binding references — match ANY version for Supervertaler.Trados
    # to catch stale entries that were added at an older version.
    asm_pattern = re.compile(r"Supervertaler\.Trados, Version=[\d.]+,")
    new_asm = f"Supervertaler.Trados, Version={new_four},"
    c2 = len(asm_pattern.findall(text))
    text = asm_pattern.sub(new_asm, text)

    with open(PLUGIN_XML, "wb") as f:
        f.write(b'\xff\xfe')
        f.write(text.encode("utf-16-le"))

    print(f"plugin.xml: {c1} plugin version + {c2} assembly binding refs updated")


def bump_manifest(old_four, new_four):
    """Update pluginpackage.manifest.xml <Version>."""
    with open(MANIFEST_XML, "r", encoding="utf-8") as f:
        text = f.read()

    text = text.replace(f"<Version>{old_four}</Version>", f"<Version>{new_four}</Version>")

    with open(MANIFEST_XML, "w", encoding="utf-8") as f:
        f.write(text)

    print(f"pluginpackage.manifest.xml: version updated to {new_four}")


def bump_csproj(old_three, new_three):
    """Update .csproj <Version> and <InformationalVersion>."""
    with open(CSPROJ, "r", encoding="utf-8") as f:
        text = f.read()

    for tag in ("Version", "InformationalVersion"):
        text = text.replace(f"<{tag}>{old_three}</{tag}>", f"<{tag}>{new_three}</{tag}>")

    with open(CSPROJ, "w", encoding="utf-8") as f:
        f.write(text)

    print(f"Supervertaler.Trados.csproj: version updated to {new_three}")


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)

    old_three = sys.argv[1]  # e.g. "4.12.0"
    new_three = sys.argv[2]  # e.g. "4.13.0"
    old_four = old_three + ".0"  # e.g. "4.12.0.0"
    new_four = new_three + ".0"  # e.g. "4.13.0.0"

    print(f"Bumping version: {old_three} -> {new_three}")
    bump_plugin_xml(old_four, new_four)
    bump_manifest(old_four, new_four)
    bump_csproj(old_three, new_three)
    print("Done.")


if __name__ == "__main__":
    main()
