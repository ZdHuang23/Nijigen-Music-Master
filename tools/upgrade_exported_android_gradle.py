#!/usr/bin/env python3
"""Upgrade a Unity 2019.4 Gradle export so it can compile targetSdk / compileSdk 36.

Unity 2019.4 ships Gradle 5.1.1 and Android Gradle Plugin 3.4.0, which cannot
compile against API 36. Export the project from the Editor, then run:

    python3 tools/upgrade_exported_android_gradle.py build/gradle-export

Then, with JDK 17+ and Android SDK platform 36 installed:

    cd build/gradle-export
    ./gradlew :launcher:assembleRelease
    ./gradlew :launcher:bundleRelease
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
from typing import Optional

AGP_VERSION = "8.9.1"
GRADLE_VERSION = "8.11.1"
SDK = "36"


def replace_agp(text: str) -> str:
    return re.sub(
        r"com\.android\.tools\.build:gradle:[0-9][^'\"\s]*",
        f"com.android.tools.build:gradle:{AGP_VERSION}",
        text,
    )


def replace_sdk(text: str) -> str:
    text = re.sub(r"compileSdkVersion\s+\d+", f"compileSdkVersion {SDK}", text)
    text = re.sub(r"targetSdkVersion\s+\d+", f"targetSdkVersion {SDK}", text)
    text = re.sub(r"(?m)^[ \t]*useProguard\s+\S+\s*$", "", text)
    return text


def replace_jcenter(text: str) -> str:
    return text.replace("jcenter()", "mavenCentral()")


def ensure_namespace(text: str, namespace: str) -> str:
    if re.search(r"\bnamespace\s+['\"]", text):
        return text
    return re.sub(
        r"android\s*\{",
        f"android {{\n    namespace '{namespace}'",
        text,
        count=1,
    )


def application_id(text: str) -> Optional[str]:
    match = re.search(r"applicationId\s+['\"]([^'\"]+)['\"]", text)
    return match.group(1) if match else None


def manifest_package(root: Path) -> Optional[str]:
    for manifest in root.rglob("AndroidManifest.xml"):
        try:
            content = manifest.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        match = re.search(r"package=\"([^\"]+)\"", content)
        if match:
            return match.group(1)
    return None


def upgrade(root: Path) -> None:
    gradle_files = list(root.rglob("*.gradle"))
    if not gradle_files:
        raise SystemExit(f"No *.gradle files under {root}")

    launcher = root / "launcher" / "build.gradle"
    library = root / "unityLibrary" / "build.gradle"
    launcher_text = launcher.read_text(encoding="utf-8") if launcher.exists() else ""
    namespace = application_id(launcher_text) or manifest_package(root) or "com.stelledge.nijigenmusicmaster"
    library_namespace = manifest_package(root / "unityLibrary") or "com.unity3d.player"

    for path in gradle_files:
        text = path.read_text(encoding="utf-8")
        updated = replace_jcenter(replace_sdk(replace_agp(text)))
        if path == launcher:
            updated = ensure_namespace(updated, namespace)
        elif path == library:
            updated = ensure_namespace(updated, library_namespace)
        if updated != text:
            path.write_text(updated, encoding="utf-8")
            print(f"updated {path.relative_to(root)}")

    wrapper = root / "gradle" / "wrapper" / "gradle-wrapper.properties"
    wrapper.parent.mkdir(parents=True, exist_ok=True)
    wrapper.write_text(
        "\n".join(
            [
                "distributionBase=GRADLE_USER_HOME",
                "distributionPath=wrapper/dists",
                "distributionUrl=https\\://services.gradle.org/distributions/gradle-"
                + GRADLE_VERSION
                + "-bin.zip",
                "zipStoreBase=GRADLE_USER_HOME",
                "zipStorePath=wrapper/dists",
                "",
            ]
        ),
        encoding="utf-8",
    )
    print(f"updated {wrapper.relative_to(root)}")

    props = root / "gradle.properties"
    existing = props.read_text(encoding="utf-8") if props.exists() else ""
    extras = {
        "android.useAndroidX": "true",
        "android.enableJetifier": "true",
        "android.suppressUnsupportedCompileSdk": SDK,
        "org.gradle.jvmargs": "-Xmx4096M",
    }
    lines = [line for line in existing.splitlines() if line.strip()]
    keys = {line.split("=", 1)[0] for line in lines if "=" in line}
    for key, value in extras.items():
        if key not in keys:
            lines.append(f"{key}={value}")
        else:
            lines = [f"{key}={value}" if line.startswith(key + "=") else line for line in lines]
    props.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"updated {props.relative_to(root)}")
    print(f"namespace={namespace} agp={AGP_VERSION} gradle={GRADLE_VERSION} sdk={SDK}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export_dir", type=Path, help="Unity Gradle export directory")
    args = parser.parse_args()
    root = args.export_dir.resolve()
    if not root.is_dir():
        raise SystemExit(f"Not a directory: {root}")
    upgrade(root)


if __name__ == "__main__":
    try:
        main()
    except BrokenPipeError:
        sys.exit(0)
