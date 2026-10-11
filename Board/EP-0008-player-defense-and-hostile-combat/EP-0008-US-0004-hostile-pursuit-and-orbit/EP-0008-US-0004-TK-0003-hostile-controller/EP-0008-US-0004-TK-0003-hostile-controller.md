---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0004-hostile-pursuit-and-orbit
ticket: EP-0008-US-0004-TK-0003-hostile-controller
title: "Авторитетное управление двигателем и фазами AI"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0004-TK-0002-relative-orbit-math"]
files_touched: 9
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Авторитетное управление двигателем и фазами AI

STATUS: IMPLEMENTED / AUTOMATED PASS

## Why

Противник начинает сближение после снятия стартовой паузы. Он управляет двигателем, выходит на движущуюся вместе с игроком орбиту радиусом половины дальности оружия и возвращается к преследованию, если удержание невозможно. Этот тикет обеспечивает: авторитетное управление двигателем и фазами ai.

## Decisions

Решения Q18–Q20,Q39–Q45,Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0004-hostile-pursuit-and-orbit.md): AC-0001, AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/RuntimeMotion.cs` | Существует: src/DeepSpaceSaga.Engine/RuntimeMotion.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/HostileOrbitRuntimeTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Execution scope update — 2026-10-11

Baseline `96e7113`, remote verified. Add `SimulationEngine.Combat.cs` for the minimum continuous segment adapter required when enabling accelerating NPC motion (the old speed bound can be zero). TK-0004 retains dedicated swept-collision/guidance/death integration acceptance. Existing deterministic combat fixtures in Engine/Client may explicitly disable NPC engines where the scenario tests stationary/linear targets; document exact changed paths and preserve those assertions. Current-format hostile persistence remains US-0011. RuntimeMotion and EconomyTime already dispatch through shared predictor and AdvanceMotionTo; change them only if needed.

Fixture scope before edits: `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs`, `HostileTorpedoFireTests.cs`, `CombatSaveLoadTests.cs`, and `tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs`. Existing stationary/linear target scenarios explicitly power off NPC engines; the new hostile-motion tests enable them deliberately.

Additional scope: `tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs` retains initial identity/loadout checks and replaces obsolete constant-velocity continuation with bounded hostile pursuit.

## Dependencies

Шаг **13** [общего порядка](../../Tickets.md).
- [EP-0008-US-0004-TK-0002-relative-orbit-math](../../EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0002-relative-orbit-math/EP-0008-US-0004-TK-0002-relative-orbit-math.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Инициализировать Enemy armed controller; двигательный модуль выбран стабильно и занят AI внутренне, без ослабления player ownership. Сегменты/phase обновляются внутри fixed physical boundaries и проецируются snapshot. При docked/неоперабельном двигателе не двигать, не обходить привязки станции; после readiness пересмотреть управление. Clock fast path должен учитывать hostile activity.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Другие production layers не реализуются в этой карточке; необходимое расширение оформляется до изменения.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [x] `Hostile_ai_starts_without_detection_gate`
- [x] `Engine_power_and_docking_gate_motion`
- [x] `Pause_freezes_hostile_controller`
- [x] `Orbit_direction_persists_between_phases`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002, AC-0003, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency `96e7113ded5f2bb1b88458ec543237dac87f73d9` published/remote verified. Every armed Enemy receives internal stable module selection and 100ms physical decisions, regardless of detection. Shared segments carry actual engine max speed/acceleration/turn; no fuel consumption or player receipts. Engine power/structure/readiness and docking prevent AI steering. Calendar-only/paused frames do not initialize or advance control, including a nonzero initial epoch. Split snapshots and Motion prediction match; direction survives phase changes. RuntimeMotion and EconomyTime required no edits. Minimal continuous collision adapter introduced together with activation to avoid invalid zero-speed bounds during acceleration.

Initial full non-corpus Engine: 1731 passed / 41 obsolete stationary-target failures. Updated explicit static/linear fixtures; final full affected combat filter **281/281 PASS** (`ep8-us4-tk3-engine-final.trx`), including all prior failures and 11 new runtime checks. Initial full Client: 1769 passed / 1 obsolete constant-velocity expectation; updated real pirate pursuit check; final affected Client **14/14 PASS** (`ep8-us4-tk3-client-regression.trx`). Solution build 0 warnings/errors, scoped format verify/diff PASS. Self-review fixed nonzero-epoch paused initialization with regression. Native/corpus and hostile current-format Save/Load remain downstream gates (US-0011 handles persistence). Not user APPROVED.
