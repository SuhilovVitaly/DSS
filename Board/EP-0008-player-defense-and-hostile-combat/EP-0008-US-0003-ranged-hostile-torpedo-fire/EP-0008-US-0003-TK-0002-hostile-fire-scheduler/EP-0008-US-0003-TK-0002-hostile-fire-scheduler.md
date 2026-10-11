---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0003-ranged-hostile-torpedo-fire
ticket: EP-0008-US-0003-TK-0002-hostile-fire-scheduler
title: "Автопуск и повторная стрельба внутри физического интервала"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0003-TK-0001-shared-launch-validation"]
files_touched: 14
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Автопуск и повторная стрельба внутри физического интервала

STATUS: IMPLEMENTED

## Why

Каждый враждебный вооружённый корабль запускает торпеду по игроку в радиусе своего аппарата. Игрок подчиняется той же дальности, но управление чужими модулями через player command остаётся запрещено. Этот тикет обеспечивает: автопуск и повторная стрельба внутри физического интервала.

## Decisions

Решения Q02–Q04,Q17,Q41,Q44 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0003-ranged-hostile-torpedo-fire.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs` | Создаётся предыдущим тикетом EP-0008-US-0003-TK-0001-shared-launch-validation | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/HostileTorpedoFireTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

### Execution scope — 2026-10-11

Dependency published as `ccf67af0f8019d42ccadefe576397a898a9d852e`, live SHA verified.
The event scheduler uses the first eligible integer physical timestamp, stable
owner/module order and A07 next-integer-ms relaunch after termination. No clock or
save schema change is needed: the current processed cursor and existing termination
facts provide the anti-loop lower bound, including after current-time Save/Load.
Full non-corpus Engine and Client regression applies; map corpus/native remain final gates.

Extend scope before edits to existing directional-combat fixtures in
`TorpedoImpactTests`, `TorpedoLaunchTests`, `BasicCombatEndToEndTests`,
`CombatSaveLoadTests`, `CombatWreckTests`, `CountermeasureEndToEndTests`,
`RangedTorpedoLaunchTests` and Client `CombatSessionRoundtripTests` /
`BasicCombatUiFlowTests`. They previously assumed no NPC fire. Explicitly power off
the NPC torpedo launcher for those isolated player-shot/history assertions, leaving
enemy relation, defense weapons, damage and all original assertions intact. New
hostile scheduler tests power the weapon on and verify actual two-sided behavior.
Direct hostile launch/range tests are updated to distinguish explicit paused launch
from the now automatic launch during physical advance.
Also include Client `TorpedoPreviewTests` before editing: its moving/paused preview
fixture requires the same explicit power-off on the hostile torpedo module, while
preserving its player launch and reconciliation assertions.

Шаг **9** [общего порядка](../../Tickets.md).
- [EP-0008-US-0003-TK-0001-shared-launch-validation](../../EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0001-shared-launch-validation/EP-0008-US-0003-TK-0001-shared-launch-validation.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Включить все Enemy armed NPC, без привязки к SPC-0002 и сценарию. Ищется первая целая ms допустимого входа в200км, следующий пуск после освобождения. Использовать стабильный object/module order и event cursor, обрабатывать readiness/range при ускорении. Пауза, смерть/исчезновение игрока и неоперабельный модуль запрещают запуск. Не допустить нулевременного бесконечного цикла при spawn overlap.
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

- [x] `All_enemy_ships_fire_neutrals_do_not`
- [x] `Pause_has_no_auto_fire`
- [x] `Finishing_torpedo_allows_next_shot`
- [x] `Large_interval_matches_partitioned_fire`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution review — 2026-10-11

Hostile launch events participate in the same physical event loop as impact, defense
launch/contact and expiry. Idle worlds consider hostile range entry; the first
launch boundary also lets normal guidance scheduling initialize correctly. All
Enemy NPCs/modules are considered in ordinal order. The first launch is at the
first eligible integer ms after the processed cursor; after termination, A07 uses
floor(termination)+1. No separate cooldown, player command or player receipt is added.

Focused hostile tests11/11 PASS and broader combat tests283/283 PASS, followed by
an additional real hostile-launch/player-defense test in final regression. Tests
cover same-time multiple actors, nonenemies, pause/calendar-only advancement,
configured range entry, power/structure/operator guards, repeated shots, initial
overlap, interval partitioning and actual in-flight file Save/Load continuation.
Initial full regressions exposed74 Engine/13 Client old one-sided-combat assumptions;
fixtures now explicitly disable only the NPC torpedo module. Final affected Client
**30/30 PASS** (`ep8-us3-tk2-client-focused.trx`); a repeated clean full Client pass
is not claimed. Solution build0 warnings/errors, scoped format/diff PASS.
Native NOT RUN; final map corpus OPEN. Self-review, not independent approval.

Final non-corpus Engine **1762/1762 PASS** (`ep8-us3-tk2-engine-final.trx`).
Self-review identified an exhausted-ID corner case: a candidate could be rejected
after its readiness check. The scheduler now shares the side-effect-free ID
availability check with launch execution. Added maximum-sequence/case-collision
regression; final hostile/launch/save checks **70/70 PASS** (`ep8-us3-tk2-review.trx`).
This last check adds one test after the full run; no repeated full-suite count claimed.
