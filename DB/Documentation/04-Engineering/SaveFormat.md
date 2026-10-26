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

Live LauncherCombatSnapshot/DefenseSnapshot очищены от retired guided/mode/burst
параметров. LauncherCombatSaveState и DefenseCombatSaveState в ScenarioData
сохраняют прежнюю форму Save19/18 и строгую проверку устаревших значений;
они не являются DTO для UI. Переход не повышает версию writer и не удаляет
validation некорректных старых полей. Active flights, captured geometry,
reload deadlines, command receipts, debug tuning и named RNG продолжаются
после Load; повтор команды не создаёт второй projectile.

Program использует `AppPaths.Current.Saves` в пользовательских данных;
portable.txt переключает этот путь в каталог программы. SaveSlotNaming задаёт
имена файлов. Однократная миграция старых слотов копирует их без перезаписи
и удаления; [пути и migration marker](../06-Tooling/Ubuntu.md#настройки-и-данные).
contentRoot относится к конфигурации, не к Save19/18.
Local file Load запускает продолжение на паузе. UI Save/Load не изменяет Engine
самостоятельно и не выдаёт stale callback за разрешение перезаписать слот после Defeat.
Правила стартового tuning-файла — в [DebugTuning](DebugTuning.md).

Источники: [ScenarioData.cs](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs), [ScenarioLoader.cs](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs), [SimulationEngine.PointCombatPersistence.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs), [SimulationEngine.DebugTuningPersistence.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningPersistence.cs), [SaveSlotNaming.cs](../../../src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs), [Program.cs](../../../src/DeepSpaceSaga.Client/Program.cs).
