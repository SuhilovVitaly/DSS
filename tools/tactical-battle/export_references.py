#!/usr/bin/env python3
"""Convert independent V4 Markdown evidence; never simulate or derive battle outcomes."""
import argparse
import hashlib
import json
import math
import platform
import re
import struct
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
REFERENCE = REPO / "VData/Planning/V4.0/Reference"


def tables(text):
    result = []
    current = []
    for line in text.splitlines() + [""]:
        if line.startswith("|"):
            current.append([s.strip().replace("`", "") for s in line.strip().strip("|").split("|")])
        elif current:
            headers = current[0]
            rows = current[2:]
            if any(len(row) != len(headers) for row in rows):
                raise ValueError("Malformed Markdown table")
            result.append((headers, [dict(zip(headers, row)) for row in rows]))
            current = []
    return result


def integer(text):
    if not re.fullmatch(r"-?\d+", text):
        raise ValueError("Expected integer: " + text)
    return int(text)


def pair(text):
    return [integer(s.strip()) for s in text.split("/")]


def scalar(text):
    if text == "—":
        return None
    if re.fullmatch(r"-?\d+", text):
        return int(text)
    if re.fullmatch(r"-?\d+\.\d+", text):
        return float(text)
    return text


def battles(text):
    rows = tables(text)[0][1]
    result = []
    for r in rows:
        calls = [integer(s.strip()) for s in r["бросков"].split("+")]
        reason = r["причина"]
        side = "A" if "Скиталец" in reason else "B" if "Логово" in reason else None
        kind = "NoDrones" if "не осталось дронов" in reason else "CarrierDestroyed" if "уничтожен" in reason else "Unclassified"
        result.append(dict(seed=integer(r["сид"]), tactic=r["тактика A"], outcome=r["исход"], turns=integer(r["ходов"]),
                           losses=pair(r["потери A / B"]), carrierHull=pair(r["корпус носителей A / B"]),
                           shots=integer(r["выстрелов"]), hits=integer(r["попаданий"]), rearKills=integer(r["в корму"]),
                           systems=integer(r["систем"]), disabled=integer(r["выведено"]), assemblyCalls=calls[0],
                           turnCalls=calls[1], totalCalls=sum(calls), reason=dict(kind=kind, side=side, text=reason)))
    if len(result) != 90 or any(r["reason"]["kind"] == "Unclassified" for r in result):
        raise ValueError("Unexpected battle coverage/reason")
    return result


TRACE_FIELDS = {"id": "id", "сторона": "side", "раму": "frame", "точн": "accuracy", "ман": "maneuver",
                "x": "x", "y": "y", "курс": "course", "скорость": "speed", "корпус": "hull", "броня": "armor",
                "роль": "role", "поворот dc": "turn", "стрелок": "shooter", "цель": "target", "расст.": "distance",
                "угол": "angle", "шанс": "chance", "попал": "hit", "урон": "damage", "в корму": "rear",
                "гибель": "kill", "состояние": "status", "заклинило": "jammed", "двигатели": "engines"}


def trace_rows(rows):
    result = []
    for row in rows:
        unit = {}
        for key, value in row.items():
            unit[TRACE_FIELDS[key]] = ([float(v.strip()) for v in value.split(",")] if key == "двигатели" and value != "—" else scalar(value))
        result.append(unit)
    return result


def trace(text):
    parts = re.split(r"^## Ход (\d+)\s*$", text, flags=re.M)
    assembly = dict(calls=int(re.search(r"Бросков после сборки: (\d+)", parts[0])[1]), units=trace_rows(tables(parts[0])[0][1]))
    turns = []
    for i in range(1, len(parts), 2):
        body = parts[i + 1]
        salvo = re.search(r"Залп, бросков: (\d+) → (\d+)", body)
        item = dict(number=int(parts[i]), planningCalls=int(re.search(r"Планирование, бросков: (\d+)", body)[1]),
                    salvoCallsBefore=int(salvo[1]), salvoCallsAfter=int(salvo[2]), orders=[], positions=[], shots=[], states=[])
        for headers, rows in tables(body):
            kind = "orders" if "роль" in headers else "positions" if "x" in headers else "shots" if "стрелок" in headers else "states"
            if item[kind]:
                raise ValueError("Duplicate trace table: " + kind)
            item[kind] = trace_rows(rows)
        turns.append(item)
    if len(turns) != 19 or turns[-1]["salvoCallsAfter"] != 308:
        raise ValueError("Unexpected trace coverage")
    return dict(seed=1, tactic="dogfight", decimalPlaces=6, assembly=assembly, turns=turns)


def series(text):
    mapping = {"Схватка": "dogfight", "Налёт": "raid", "Защита": "defence", "Сбалансированная": "balanced", "Командир": "commander"}
    result = []
    for row in tables(text)[0][1]:
        result.append(dict(tactic=mapping[row["Тактика A"]], battles=integer(row["Боёв"]),
                           winPercent=float(row["Победа A, %"]), winMaxOneLossPercent=float(row["Победа с ≤ 1 потерей, %"]),
                           winNoLossPercent=float(row["Победа без потерь, %"]), defeatPercent=float(row["Поражение, %"]),
                           drawPercent=float(row["Ничья, %"]), averageTurns=float(row["Ходов"]),
                           averageLosses=[float(v.strip()) for v in row["Потери A / B"].split("/")],
                           carrierDeathPercent=[float(v.strip()) for v in row["Гибель носителя A / B, %"].split("/")]))
    return result


def bits(value):
    return struct.pack("<d", value).hex()


def geometry():
    result = []

    def add(operation, args, value):
        result.append(dict(operation=operation, arguments=[bits(a) for a in args], expected=bits(value)))

    angles = [-1e15, -720.0, -360.0, math.nextafter(-180.0, -math.inf), -180.0,
              math.nextafter(-180.0, math.inf), -0.0, 0.0, math.nextafter(180.0, -math.inf), 180.0,
              math.nextafter(180.0, math.inf), 360.0, 720.0, 1e15]
    for a in angles:
        n = math.fmod(a + 180.0, 360.0)
        if n < 0:
            n += 360.0
        add("norm", [a], n - 180.0)
        radians = a * 0.017453292519943295
        add("sin", [radians], math.sin(radians))
        add("cos", [radians], math.cos(radians))
    axes = [(y, x) for y in [-1.0, -0.0, 0.0, 1.0] for x in [-1.0, -0.0, 0.0, 1.0]]
    for y, x in axes:
        add("atan2", [y, x], math.atan2(y, x))
    for x, y in [(0.0, 0.0), (3.0, 4.0), (1e300, 1e300), (1e-300, 1e-300), (1e300, 1e-300),
                 (math.ulp(0.0), math.ulp(0.0))] + [(i * 0.123456789, (i + 1) * 0.987654321) for i in range(1, 1025)]:
        add("hypot", [x, y], math.hypot(x, y))
    return result


def documents():
    libc, version = platform.libc_ver()
    if sys.platform != "linux" or libc != "glibc":
        raise RuntimeError("Reference JSON must be exported on Linux/glibc")
    texts = {name: (REFERENCE / (name + ".md")).read_text(encoding="utf-8") for name in ["battles", "trace-seed-1", "series"]}
    header = dict(formatVersion=1, source="independent-markdown", exporterPython=platform.python_version(),
                  exporterLibc=libc + " " + version, exporterOs=platform.platform(),
                  sourceSha256={name + ".md": hashlib.sha256(text.replace("\r\n", "\n").encode()).hexdigest() for name, text in texts.items()},
                  geometryEncoding="little-endian IEEE-754 binary64 hex", tracePrecision="decimal-6",
                  missingCoverage=["original simulator environment", "unrounded trace IEEE bits", "all battle trig arguments",
                                   "per-weapon statistics", "roleChanges", "stage calls for battles other than seed 1",
                                   "exact series integer sums and timeout counts"])
    data = {"battles": battles(texts["battles"]), "trace-seed-1": trace(texts["trace-seed-1"]),
            "series": series(texts["series"]), "geometry": geometry()}
    return {name + ".json": dict(header=header, data=value) for name, value in data.items()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--emit-log", action="store_true", help="Emit reviewable JSON chunks for retrieval from the Linux run")
    args = parser.parse_args()
    for file, value in documents().items():
        text = json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
        path = REFERENCE / file
        if args.check:
            if json.loads(path.read_text(encoding="utf-8")) != value:
                raise ValueError("Reference differs: " + file)
        else:
            path.write_text(text, encoding="utf-8", newline="\n")
        if args.emit_log:
            for offset in range(0, len(text), 4000):
                print("DSS_EXPORT_PART " + json.dumps(dict(file=file, offset=offset, text=text[offset:offset + 4000]), ensure_ascii=True))
    print("4 independent reference JSON files " + ("verified" if args.check else "exported"))


if __name__ == "__main__":
    main()
