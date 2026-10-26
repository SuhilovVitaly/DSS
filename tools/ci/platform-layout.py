"""Run the native RU/EN layout matrix with a 300s process-tree cap per case."""

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys


sys.dont_write_bytecode = True

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("results", type=Path)
    parser.add_argument("--virtual-display", action="store_true")
    args = parser.parse_args()
    if not args.results.is_absolute():
        raise ValueError("results must be absolute")
    args.results.mkdir(parents=True, exist_ok=True)
    root = args.repository.resolve()
    spec = importlib.util.spec_from_file_location("baseline", Path(__file__).with_name("platform-baseline.py"))
    baseline = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(baseline)
    dll = root / "tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll"
    report = {"commit": subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip(),
              "platform": sys.platform, "binary_sha256": hashlib.sha256(dll.read_bytes()).hexdigest(), "cases": []}
    for locale in ["en", "ru"]:
        for scale, dimensions in [("0.8", "1920x1080"), ("1", "1280x720"), ("1.2", "1920x1080"), ("1.5", "1280x720")]:
            folder = args.results / f"{locale}-{scale}"
            folder.mkdir(exist_ok=True)
            command = ["dotnet", str(dll), str(root), str(folder / "layout.json"),
                       "--platform-layout-window", scale, dimensions, locale]
            if args.virtual_display:
                command = ["xvfb-run", "-a", "-s", "-screen 0 1920x1080x24", *command]
            code = baseline.run(command, folder / "native.log")
            result = json.loads((folder / "layout.json").read_text(encoding="utf-8")) if (folder / "layout.json").exists() else {}
            passed = code == 0 and result.get("passed") is True and len(result.get("captures", [])) == 6
            report["cases"].append({"locale": locale, "scale": scale, "dimensions": dimensions,
                                    "exit": code, "passed": passed, "report": str(folder / "layout.json")})
            (args.results / "matrix.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            if not passed:
                print(result.get("failure", "No native JSON report was produced"), flush=True)
                return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
