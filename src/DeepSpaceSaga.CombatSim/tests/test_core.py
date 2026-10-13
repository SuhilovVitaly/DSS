import copy
import unittest
from pathlib import Path

from combatsim import load_game_data, sample_battle


class GameDataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = load_game_data()

    def test_current_production_catalog_and_scenarios_are_resolved(self):
        self.assertEqual({"CombatMukha", "CombatShmel"}, set(self.data.scenarios))
        self.assertIn("ship.shmel", self.data.ship_classes)
        self.assertEqual(150, self.data.modules["module.torpedo.launcher.basic"]["torpedoDamage"])
        self.assertEqual(2, self.data.modules["module.countermeasure.launcher.basic"]["barrels"])
        self.assertEqual(1, self.data.modules["module.countermeasure.launcher.single"]["barrels"])
        self.assertEqual(50, self.data.rules["chance"]["base"])
        self.assertEqual(10, self.data.fire_modes["overheat"]["accuracyBonus"])
        for name, scenario in self.data.scenarios.items():
            self.assertEqual(3, sum(s["objectType"] == "NpcShip" for s in scenario["gameState"]["spaceObjects"]), name)

    def test_ship_data_changes_affect_sampler_without_shadow_design_json(self):
        data = self.data
        mutated = copy.deepcopy(data.modules)
        mutated["module.torpedo.launcher.basic"]["torpedoDamage"] = 0
        changed = type(data)(data.client_root, mutated, data.rules, data.fire_modes, data.ship_classes, data.scenarios)
        original = sample_battle(data, "CombatMukha", 1, max_seconds=120)
        altered = sample_battle(changed, "CombatMukha", 1, max_seconds=120)
        self.assertEqual("timeout", altered.winner)
        self.assertNotEqual(original, altered)

    def test_sampler_is_deterministic_per_seed_and_uses_both_scenarios(self):
        for name in self.data.scenarios:
            result = sample_battle(self.data, name, 7)
            self.assertEqual(result, sample_battle(self.data, name, 7))
            self.assertGreater(result.hostile_launches, 0)
            self.assertIn(result.winner, {"player", "hostiles", "timeout"})

    def test_missing_game_data_fails_clearly(self):
        with self.assertRaisesRegex(ValueError, "No production modules"):
            load_game_data(Path(__file__).parent / "missing")


if __name__ == "__main__":
    unittest.main()
