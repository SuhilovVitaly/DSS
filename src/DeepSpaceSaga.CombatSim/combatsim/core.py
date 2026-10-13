"""Small independent stochastic sampler; see README for deliberate abstractions."""

from __future__ import annotations

from dataclasses import dataclass
import math
import random

from .gamedata import GameData


@dataclass(frozen=True)
class BattleResult:
    scenario: str
    seed: int
    winner: str
    duration_seconds: int
    player_hits: int
    hostile_launches: int
    player_defense_launches: int


def _clamp(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


def _chance(rules: dict, attack: float, defense: float) -> float:
    configured = rules["chance"]
    return _clamp(configured["base"] + attack - defense, configured["min"], configured["max"])


def _make_ship(source: dict, data: GameData) -> dict:
    crew = {person["crewId"]: person for person in source.get("crew", [])}
    modules = []
    for item in source.get("modules", []):
        definition = data.modules[item["moduleTypeId"]]
        modules.append({
            "source": item,
            "definition": definition,
            "armor": data.rules["armorClasses"][item.get("armorClass", "none")],
            "structure": item["structurePoints"],
            "ready": 0,
        })
    return {"source": source, "crew": crew, "modules": modules, "destroyed": False}


def _rating(ship: dict, module: dict, kind: str, skill: str = "accuracySkill") -> float:
    operator = ship["crew"].get(module["source"].get("operatorCrewId"), {})
    return module["definition"].get(kind, 0) + operator.get(skill, 0)


def _defense(ship: dict) -> float:
    engine = next((m for m in ship["modules"] if m["definition"].get("type") == "module.engine"), None)
    if engine is None or engine["structure"] <= 0:
        return 0
    operator = ship["crew"].get(engine["source"].get("operatorCrewId"), {})
    return engine["definition"].get("maneuverability", 0) + operator.get("maneuverabilitySkill", 0)


def _hit_module(target: dict, data: GameData, rng: random.Random) -> None:
    alive = [m for m in target["modules"] if m["structure"] > 0]
    if not alive:
        target["destroyed"] = True
        return
    weights = [m["definition"]["massKg"] for m in alive]
    module = rng.choices(alive, weights=weights, k=1)[0]
    torpedo = data.modules["module.torpedo.launcher.basic"]
    if module["armor"] > 0 and rng.randrange(100) >= torpedo["armorPiercingPercent"]:
        module["armor"] = max(0, module["armor"] - torpedo["torpedoDamage"] * torpedo["armorDamagePercent"] / 100)
    else:
        module["structure"] = max(0, module["structure"] - torpedo["torpedoDamage"])
    critical = set(data.rules["shipDestruction"]["criticalRoles"])
    if module["structure"] <= 0 and module["definition"].get("combatRole") in critical:
        target["destroyed"] = True
    destroyed = sum(m["structure"] <= 0 for m in target["modules"])
    if destroyed / len(target["modules"]) >= data.rules["shipDestruction"]["destroyedModuleFraction"]:
        target["destroyed"] = True


def sample_battle(data: GameData, scenario_name: str, seed: int, max_seconds: int = 600) -> BattleResult:
    if scenario_name not in data.scenarios:
        raise ValueError(f"Unknown scenario: {scenario_name}")
    state = data.scenarios[scenario_name]["gameState"]
    ships = [_make_ship(ship, data) for ship in state["spaceObjects"]]
    player = next(ship for ship in ships if ship["source"]["objectId"] == state["playerShipObjectId"])
    enemies = [ship for ship in ships if ship is not player]
    rng = random.Random(seed)
    pending: list[tuple[int, dict, dict, dict]] = []
    player_hits = hostile_launches = defense_launches = 0
    mode = data.fire_modes["standard"]
    for second in range(max_seconds + 1):
        for due, owner, target, launcher in list(pending):
            if due > second:
                continue
            pending.remove((due, owner, target, launcher))
            if owner["destroyed"] or target["destroyed"]:
                continue
            defenses = [m for m in target["modules"] if m["structure"] > 0 and
                        m["definition"].get("type") == "module.countermeasure.launcher" and
                        m["source"].get("autoDefenseEnabled", False) and m["ready"] <= second]
            if defenses:
                defense = defenses[0]
                defense_launches += int(target is player)
                defense["ready"] = second + math.ceil(defense["definition"]["countermeasureReloadMs"] / 1000)
                attack = _rating(target, defense, "countermeasureAccuracy")
                maneuver = _rating(owner, launcher, "torpedoManeuverability", "maneuverabilitySkill")
                if rng.random() * 100 < _chance(data.rules, attack, maneuver):
                    continue
            attack = _rating(owner, launcher, "accuracy") + mode["accuracyBonus"]
            if rng.random() * 100 < _chance(data.rules, attack, _defense(target)):
                player_hits += int(target is player)
                _hit_module(target, data, rng)
        if player["destroyed"] or all(enemy["destroyed"] for enemy in enemies):
            winner = "hostiles" if player["destroyed"] else "player"
            return BattleResult(scenario_name, seed, winner, second, player_hits, hostile_launches, defense_launches)
        for owner in ships:
            if owner["destroyed"]:
                continue
            target = player if owner is not player else next((e for e in enemies if not e["destroyed"]), None)
            if target is None:
                continue
            dx = owner["source"]["positionX"] - target["source"]["positionX"]
            dy = owner["source"]["positionY"] - target["source"]["positionY"]
            initial_km = math.hypot(dx, dy) / 10
            distance_km = max(40, initial_km - second * 1.5)
            for launcher in owner["modules"]:
                definition = launcher["definition"]
                if definition.get("type") != "module.torpedo.launcher" or launcher["structure"] <= 0 or launcher["ready"] > second:
                    continue
                if distance_km > definition["torpedoRangeKm"]:
                    continue
                due = second + max(1, math.ceil(distance_km / definition["torpedoSpeedKmS"]))
                pending.append((due, owner, target, launcher))
                launcher["ready"] = second + math.ceil(definition["launcherReloadMs"] * mode["reloadMultiplier"] / 1000)
                hostile_launches += int(owner is not player)
    return BattleResult(scenario_name, seed, "timeout", max_seconds, player_hits, hostile_launches, defense_launches)
