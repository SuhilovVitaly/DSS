"""Read current production combat content, never a copied design-data tree."""

from __future__ import annotations

from dataclasses import dataclass
import json
from pathlib import Path
from typing import Any


def default_client_root() -> Path:
    return Path(__file__).resolve().parents[2] / "DeepSpaceSaga.Client"


def _read(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


@dataclass(frozen=True)
class GameData:
    client_root: Path
    modules: dict[str, dict[str, Any]]
    rules: dict[str, Any]
    fire_modes: dict[str, dict[str, Any]]
    ship_classes: frozenset[str]
    scenarios: dict[str, dict[str, Any]]


def load_game_data(client_root: Path | None = None) -> GameData:
    root = (client_root or default_client_root()).resolve()
    module_files = sorted((root / "Data" / "Modules").glob("**/*.json"))
    if not module_files:
        raise ValueError(f"No production modules under {root}")
    modules: dict[str, dict[str, Any]] = {}
    for path in module_files:
        for definition in _read(path).get("moduleImplementations", []):
            type_id = definition["typeId"]
            if type_id in modules:
                raise ValueError(f"Duplicate module type: {type_id}")
            modules[type_id] = definition
    rules = _read(root / "Data" / "Combat" / "combat-rules.json")
    fire_modes = {
        mode["modeId"]: mode
        for mode in _read(root / "Data" / "Combat" / "fire-modes.json")["fireModes"]
    }
    ship_classes = frozenset(
        ship["typeId"] for ship in _read(root / "Data" / "Ships" / "ship-classes.json")["shipClasses"]
    )
    scenarios: dict[str, dict[str, Any]] = {}
    for name in ("CombatMukha", "CombatShmel"):
        scenario = _read(root / "Scenarios" / name / "scenario.json")
        state = scenario["gameState"]
        if state["playerShipObjectId"] not in {ship["objectId"] for ship in state["spaceObjects"]}:
            raise ValueError(f"Missing player ship in {name}")
        for ship in state["spaceObjects"]:
            if ship["shipClassId"] not in ship_classes:
                raise ValueError(f"Unknown ship class {ship['shipClassId']} in {name}")
            crew_ids = {crew["crewId"] for crew in ship.get("crew", [])}
            for module in ship.get("modules", []):
                if module["moduleTypeId"] not in modules:
                    raise ValueError(f"Unknown module {module['moduleTypeId']} in {name}")
                if module.get("operatorCrewId") and module["operatorCrewId"] not in crew_ids:
                    raise ValueError(f"Unknown operator {module['operatorCrewId']} in {name}")
        scenarios[name] = scenario
    return GameData(root, modules, rules, fire_modes, ship_classes, scenarios)
