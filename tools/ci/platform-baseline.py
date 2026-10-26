"""Compare the same commit on Windows/Ubuntu; cap each process tree at 300s.

Usage: python tools/ci/platform-baseline.py <absolute-results-directory>
Every test method discovered by VSTest is assigned to exactly one partition.
Theory rows stay together; no failures or unavailable graphics are silently skipped.
"""

import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import xml.etree.ElementTree as ET


def run(command, log):
    with log.open("w", encoding="utf-8") as stream:
        process = subprocess.Popen(command, stdout=stream, stderr=subprocess.STDOUT,
                                   start_new_session=os.name != "nt")
        try:
            code = process.wait(timeout=300)
        except subprocess.TimeoutExpired:
            if os.name == "nt":
                subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                               stdout=stream, stderr=subprocess.STDOUT, check=False)
            else:
                os.killpg(process.pid, signal.SIGKILL)
            process.wait()
            code = 124
    print(f"{log.name}: exit {code}", flush=True)
    return code


def main():
    output = Path(sys.argv[1])
    if not output.is_absolute():
        raise ValueError("results-directory must be absolute")
    output.mkdir(parents=True, exist_ok=False)
    root = Path(__file__).resolve().parents[2]
    os.chdir(root)
    report = {"commit": subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip(),
              "platform": sys.platform, "projects": []}
    for name, command in [("sdk", ["dotnet", "--info"]),
                          ("restore", ["dotnet", "restore", "DeepSpaceSaga.sln"]),
                          ("build", ["dotnet", "build", "DeepSpaceSaga.sln", "-c", "Release", "--no-restore"])]:
        report[name] = run(command, output / f"{name}.log")
        if report[name]:
            (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            return 1
    projects = sorted(root.glob("tests/*.Tests/*.csproj")) + sorted(root.glob("tools/*.Tests/*.csproj"))
    failed = False
    for project in projects:
        name = project.stem
        folder = output / name
        folder.mkdir()
        base = ["dotnet", "test", str(project), "-c", "Release", "--no-build", "--no-restore"]
        discovery = folder / "discovery.log"
        code = run(base + ["--list-tests", "--results-directory", str(folder)], discovery)
        listed = discovery.read_text(encoding="utf-8-sig").split("The following Tests are available:", 1)
        names = sorted(set(re.match(r"^\s{4}(\S+?)(?:\(|$)", line).group(1)
                           for line in listed[-1].splitlines()
                           if re.match(r"^\s{4}(\S+?)(?:\(|$)", line))) if len(listed) == 2 else []
        row = {"project": name, "discovery": code, "methods": len(names), "partitions": []}
        (folder / "methods.json").write_text(json.dumps(names, indent=2), encoding="utf-8")
        if code or not names:
            failed = True
        else:
            for index in range(0, len(names), 100):
                part = index // 100
                selection = names[index:index + 100]
                filter_text = "|".join("FullyQualifiedName=" + name for name in selection)
                trx = f"part-{part:03}.trx"
                code = run(base + ["--filter", filter_text, "--logger", "trx;LogFileName=" + trx,
                                   "--results-directory", str(folder)], folder / f"part-{part:03}.log")
                counts = {}
                path = folder / trx
                if path.exists():
                    document = ET.parse(path)
                    counters = document.find(".//{*}Counters")
                    counts = counters.attrib if counters is not None else {}
                    # A successful process with zero selected tests is not evidence.
                    if int(counts.get("executed", "0")) == 0:
                        code = 1
                else:
                    code = code or 1
                row["partitions"].append({"exit": code, "methods": selection, "counts": counts})
                failed |= code != 0
        report["projects"].append(row)
        (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return int(failed)


if __name__ == "__main__":
    sys.exit(main())
