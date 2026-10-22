# Солнечная система и кластеры

New Game создаёт seeded систему: Солнце, планеты, пояса с реальными астероидами
и отдельной декорацией, человеческие кластеры, ресурсы, базы ИИ, поля и POI.
Текущие диапазоны и enabledScenarios задаёт Data/Maps/solar-system.json.
Кластеры движутся согласованно; география и identity не зависят от viewport/LOD.
Орбитальные периоды календарные, движение использует общий motion baseline.
ETA оценивает расстояние и доступный двигатель, не обещает время полного Dock.
Load использует сохранённую геометрию; изменение content не перегенерирует
уже сохранённый мир. Территории/поля описаны в [AiMapEnvironment](../../04-Engineering/AiMapEnvironment.md).

Источники: [solar-system.json](../../../src/DeepSpaceSaga.Client/Data/Maps/solar-system.json), [Scenario](../../../src/DeepSpaceSaga.Engine/Scenario).
