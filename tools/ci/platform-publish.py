"""Verify self-contained Linux package and run it in Ubuntu 24.04 without .NET."""

import argparse
import importlib.util
import json
import os
from pathlib import Path
import sys
import tarfile
import uuid


sys.dont_write_bytecode = True

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("results", type=Path)
    args = parser.parse_args()
    if not args.results.is_absolute():
        raise ValueError("results must be absolute")
    args.results.mkdir(parents=True, exist_ok=False)
    os.chdir(args.repository)
    spec = importlib.util.spec_from_file_location("baseline", Path(__file__).with_name("platform-baseline.py"))
    baseline = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(baseline)
    output = args.results
    if baseline.run(["bash", "tools/publish-linux.sh", str(output)], output / "publish.log"):
        return 1
    archive = output / "DeepSpaceSaga-linux-x64.tar.gz"
    extracted = output / "extracted"
    extracted.mkdir()
    with tarfile.open(archive) as bundle:
        binary = bundle.getmember("DeepSpaceSaga-linux-x64/DeepSpaceSaga.Client")
        if not binary.mode & 0o111:
            raise ValueError("Archive lost executable permission")
        bundle.extractall(extracted, filter="data")
    package = extracted / "DeepSpaceSaga-linux-x64"
    native = package / "libSkiaSharp.so"
    held = native.with_suffix(".disabled")
    native.rename(held)
    try:
        if baseline.run(["bash", "tools/publish-linux.sh", "--verify-only", str(package)], output / "missing-native.log") == 0:
            raise ValueError("Incomplete native package was accepted")
    finally:
        held.rename(native)
    evidence = output / "clean-ubuntu"
    evidence.mkdir(mode=0o777)
    evidence.chmod(0o777)
    script = r"""
set -eu
if command -v dotnet; then echo 'Unexpected .NET installation' >&2; exit 3; fi
printf 'dotnet command absent; no external runtime installed\n' > /evidence/runtime-absence.txt
apt-get update -qq
DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends ca-certificates libicu74 libssl3t64 zlib1g libfontconfig1 libgl1-mesa-dri libglx-mesa0 libegl1 libglfw3 libxrandr2 libxcursor1 libxinerama1 libxi6 libwayland-client0 libwayland-cursor0 libwayland-egl1 libxkbcommon0 xvfb xauth
useradd --create-home --uid 1001 dss
runuser -u dss -- env HOME=/home/dss XDG_DATA_HOME=/evidence/data XDG_CONFIG_HOME=/evidence/config DSS_INTERFACE_LOG_PATH=/evidence/Interface.log DSS_DIAGNOSTICS_DIRECTORY=/evidence/diagnostics DSS_STARTUP_EVIDENCE_PATH=/evidence/startup.json LIBGL_ALWAYS_SOFTWARE=true ALSOFT_DRIVERS=null COREHOST_TRACE=1 COREHOST_TRACEFILE=/evidence/hosttrace.log xvfb-run -a -s '-screen 0 1920x1080x24' /opt/dss/DeepSpaceSaga.Client
"""
    container = "dss-ep0015-" + uuid.uuid4().hex
    command = ["docker", "run", "--rm", "--name", container, "--mount", f"type=bind,src={package},dst=/opt/dss,readonly",
               "--mount", f"type=bind,src={evidence},dst=/evidence", "ubuntu:24.04", "bash", "-c", script]
    try:
        code = baseline.run(command, output / "clean-ubuntu.log")
    finally:
        baseline.run(["docker", "rm", "--force", container], output / "container-cleanup.log")
    result = json.loads((evidence / "startup.json").read_text(encoding="utf-8")) if (evidence / "startup.json").exists() else {}
    passed = (code == 0 and result.get("passed") is True and result.get("nativeGpu") is True
              and result.get("content", "").rstrip("/") == "/opt/dss"
              and result.get("data", "").startswith("/evidence/")
              and result.get("config", "").startswith("/evidence/") and not result.get("portable")
              and (evidence / "runtime-absence.txt").exists())
    (output / "summary.json").write_text(json.dumps({"passed": passed, "archive": str(archive), "exit": code,
        "read_only_mount_requested": True, "runtime_preinstalled": False if (evidence / "runtime-absence.txt").exists() else None, "startup": result}, indent=2), encoding="utf-8")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
