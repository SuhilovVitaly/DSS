# Сохранения и восстановление

Текущий writer: `SaveFormat.CurrentSaveFormatVersion = 19`. Save entrypoint
принимает 19 и 18; Save18 явно нормализуется в 19, остальные версии отвергаются
до публикации мира. Сценарий с `saveFormatVersion = 0` — отдельный New Game input,
а не разрешение загрузить произвольное старое сохранение.

Сохраняются materialized объекты/орбиты/карта, оба игровых времени и остатки,
master seed и RNG/counters, pending-команды/receipts, модульные циклы,
рынки/события/knowledge, cargo provenance, рейсы/reservations/ledger,
точечные projectiles, evasion, encounter и debug overlay. Load не переигрывает
события и не генерирует мир заново. Catalog/rules fingerprints должны совпасть.

Save19 добавляет optional debug tuning и provenance для retimed flight.
Save18 с полями, принадлежащими Save19, отвергается; отсутствующий debug overlay
не означает разрешение игнорировать остальные invariants. Проверка выполняется
до замены сессии; повреждённый источник не переписывается.

Program использует `Saves` рядом с executable, SaveSlotNaming задаёт имена файлов.
Local file Load запускает продолжение на паузе. UI Save/Load не изменяет Engine
самостоятельно и не выдаёт stale callback за разрешение перезаписать слот после Defeat.
Правила стартового tuning-файла — в [DebugTuning](DebugTuning.md).

Источники: [ScenarioData.cs](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs), [ScenarioLoader.cs](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs), [SimulationEngine.PointCombatPersistence.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs), [SimulationEngine.DebugTuningPersistence.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningPersistence.cs), [SaveSlotNaming.cs](../../src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs), [Program.cs](../../src/DeepSpaceSaga.Client/Program.cs).
