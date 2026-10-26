"""Compare the same commit on Windows/Ubuntu; cap each process tree at 300s.

Usage: python tools/ci/platform-baseline.py <absolute-results-directory>
Every test case discovered by VSTest is assigned to exactly one partition.
Slow corpus theory rows run separately; result names must match discovery.
"""

import argparse
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
    if code:
        print("\n".join(log.read_text(encoding="utf-8-sig", errors="replace").splitlines()[-40:]), flush=True)
    return code


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results_directory", type=Path)
    parser.add_argument("--project", help="Exact test project name; omission runs all projects")
    parser.add_argument("--skip-build", action="store_true", help="Use an already built Release baseline")
    parser.add_argument("--skip-client-without-display", action="store_true",
                        help="Explicitly report Client NOT RUN when Linux has neither DISPLAY nor WAYLAND_DISPLAY")
    args = parser.parse_args()
    output = args.results_directory
    if not output.is_absolute():
        raise ValueError("results-directory must be absolute")
    output.mkdir(parents=True, exist_ok=False)
    root = Path(__file__).resolve().parents[2]
    os.chdir(root)
    report = {"commit": subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip(),
              "platform": sys.platform, "projects": []}
    preparation = [("sdk", ["dotnet", "--info"]),
                          ("restore", ["dotnet", "restore", "DeepSpaceSaga.sln"]),
                          ("build", ["dotnet", "build", "DeepSpaceSaga.sln", "-c", "Release", "--no-restore"])]
    for name, command in preparation[:1] if args.skip_build else preparation:
        report[name] = run(command, output / f"{name}.log")
        if report[name]:
            (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            return 1
    projects = sorted(root.glob("tests/*.Tests/*.csproj")) + sorted(root.glob("tools/*.Tests/*.csproj"))
    if args.project:
        projects = [p for p in projects if p.stem == args.project]
    if not projects:
        raise ValueError("No matching test projects")
    failed = False
    for project in projects:
        name = project.stem
        if (args.skip_client_without_display and sys.platform.startswith("linux")
                and name == "DeepSpaceSaga.Client.Tests"
                and not (os.environ.get("DISPLAY") or os.environ.get("WAYLAND_DISPLAY"))):
            reason = "No DISPLAY or WAYLAND_DISPLAY; Client tests require a graphical session in check-linux.sh"
            print(f"{name}: NOT RUN: {reason}", flush=True)
            report["projects"].append({"project": name, "status": "NOT RUN", "reason": reason})
            (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            continue
        folder = output / name
        folder.mkdir()
        base = ["dotnet", "test", str(project), "-c", "Release", "--no-build", "--no-restore"]
        discovery = folder / "discovery.log"
        code = run(base + ["--list-tests", "--results-directory", str(folder)], discovery)
        listed = discovery.read_text(encoding="utf-8-sig").split("The following Tests are available:", 1)
        names = sorted(set(line.strip() for line in listed[-1].splitlines()
                           if line.startswith("    DeepSpaceSaga."))) if len(listed) == 2 else []
        row = {"project": name, "discovery": code, "cases": len(names), "partitions": []}
        (folder / "cases.json").write_text(json.dumps(names, indent=2), encoding="utf-8")
        if code or not names:
            failed = True
        else:
            selections = []
            pending = []
            for case in names:
                if "CorpusTests." in case or ".CombatBalanceReport." in case:
                    if pending:
                        selections.append(pending)
                        pending = []
                    selections.append([case])
                else:
                    pending.append(case)
                    if len(pending) == 100:
                        selections.append(pending)
                        pending = []
            if pending:
                selections.append(pending)
            for part, selection in enumerate(selections):
                def escape(value):
                    return re.sub(r"([\\()&|=!~])", r"\\\1", value)
                filter_text = "|".join("DisplayName=" + escape(case) for case in selection)
                # Keep arbitrary theory data (including nested JSON/quotes) out of
                # the dotnet/MSBuild command line. Only VSTest parses this value.
                settings = folder / f"part-{part:03}.runsettings"
                document = ET.Element("RunSettings")
                configuration = ET.SubElement(document, "RunConfiguration")
                ET.SubElement(configuration, "TestCaseFilter").text = filter_text
                ET.ElementTree(document).write(settings, encoding="utf-8", xml_declaration=True)
                trx = f"part-{part:03}.trx"
                code = run(base + ["--settings", str(settings), "--logger", "trx;LogFileName=" + trx,
                                   "--results-directory", str(folder)], folder / f"part-{part:03}.log")
                counts = {}
                path = folder / trx
                if path.exists():
                    document = ET.parse(path)
                    counters = document.find(".//{*}Counters")
                    counts = counters.attrib if counters is not None else {}
                    actual = sorted(result.get("testName") for result in document.findall(".//{*}UnitTestResult"))
                    if actual != sorted(selection):
                        code = 1
                        (folder / f"part-{part:03}-coverage.json").write_text(
                            json.dumps({"expected": selection, "actual": actual}, indent=2), encoding="utf-8")
                    # A successful process with zero selected tests is not evidence.
                    if int(counts.get("executed", "0")) == 0:
                        code = 1
                else:
                    code = code or 1
                row["partitions"].append({"exit": code, "cases": selection, "counts": counts})
                failed |= code != 0
        report["projects"].append(row)
        (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return int(failed)


if __name__ == "__main__":
    sys.exit(main())
