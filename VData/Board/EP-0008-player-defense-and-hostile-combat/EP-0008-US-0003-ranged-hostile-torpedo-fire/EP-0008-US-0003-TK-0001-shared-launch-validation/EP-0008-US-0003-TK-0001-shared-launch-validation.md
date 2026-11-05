---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0003-ranged-hostile-torpedo-fire
ticket: EP-0008-US-0003-TK-0001-shared-launch-validation
title: "Общий validated launch с разделением полномочий"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0002-TK-0002-dual-bootstrap"]
files_touched: 12
serves: ["AC-0001","AC-0002"]
created: 2026-10-07
revision: 1
---

# Общий validated launch с разделением полномочий

STATUS: IMPLEMENTED

## Why

Каждый враждебный вооружённый корабль запускает торпеду по игроку в радиусе своего аппарата. Игрок подчиняется той же дальности, но управление чужими модулями через player command остаётся запрещено. Этот тикет обеспечивает: общий validated launch с разделением полномочий.

## Decisions

Решения Q02–Q04,Q17,Q41,Q44 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0003-ranged-hostile-torpedo-fire.md): AC-0001, AC-0002. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/RangedTorpedoLaunchTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

### Execution scope — 2026-10-11

Dependency TK-0002 published as `ec24573d5ae1b7aa5d6e937f19e7f7cbdc33de5e`, remote verified.
Extend scope to Engine `TorpedoLaunchTests` and `TorpedoImpactTests` before edits:
their old projectile tests fire across the real scenario's 1118km separation, now
outside the agreed 200km launch range. Put those fixtures explicitly within range,
keeping motion/history/collision and command-identity assertions. The actual scenario
geometry remains unchanged; separate range tests exercise both sides and rejection.
Non-corpus Engine regression applies; full map corpus remains final epic gate.
The same out-of-range assumption occurs in `BasicCombatEndToEndTests`,
`CountermeasureEndToEndTests`, `CombatSaveLoadTests` and `CombatWreckTests`;
include those fixtures in this scope. Reuse one explicit in-range position transform
from `TorpedoImpactTests`, retaining real catalog/motion and all outcome assertions.
Isolated launch/history tests disable automatic defense explicitly, since their
shorter valid launch distance would otherwise add unrelated interceptions.
Additional scope: `TorpedoHistoryTests` launches the long-flight case at a valid
range against a faster receding target, retaining the full long-duration trail
assertion. The initial combined scope patch failed atomically; this note records
the same intended scope immediately after applying the test correction.
Client integration regression uses the same old far-away scenario through
`BasicCombatUiFlowTests` and `CombatSessionRoundtripTests` shared fixtures. Extend
scope to those two files before edits, placing targets explicitly within launch
range without changing production scenario geometry or UI assertions.

Шаг **8** [общего порядка](../../Tickets.md).
- [EP-0008-US-0002-TK-0002-dual-bootstrap](../../EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-TK-0002-dual-bootstrap/EP-0008-US-0002-TK-0002-dual-bootstrap.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Выделить внутренний validated execution для player и AI; public ownership guard оставить. Проверять дальность по позам на physical timestamp пуска и RangeKm в launcher. Сохранить CommandId idempotency, operator, busy, obstacle collisions и captured parameters. Internal actor не эмулирует пользовательский owner ID.
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

- [x] `Both_sides_obey_inclusive_range`
- [x] `Forged_npc_player_command_rejected`
- [x] `Active_torpedo_survives_range_exit`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution self-review — 2026-10-11

Shared launch validates module state/operator/busy/target and physical launch pose
against the module range. Player command ownership and deduplication remain outside
the shared executor. Internal Enemy-NPC actor targets only the live player and
never creates player command receipts/identities. Autonomous scheduling is TK-0002;
the internal actor seam is exercised directly here, without forged player commands.

Tests cover both sides at/below/above200km, alternate configured50km range, moving
target range entry/exit at launch time, active flight after range exit, Friend/Neutral
rejection, forged public ownership, and existing pause/idempotency/collision/file
roundtrips. Engine non-corpus **1750/1750 PASS** (`ep8-us3-tk1-engine-final.trx`).
Build0 warnings/errors, scoped format/diff PASS. Initial Engine35 failures were
obsolete unrestricted-range fixtures, corrected while retaining original assertions.
Client full regression initially14 failures, then1761/1762 PASS: the remaining
selection test clicked an overlapping ship label at the closer distance. Ordinary
camera zoom and an earlier midflight inspection separate objects; the failed case
now passes1/1 (`ep8-us3-tk1-selection.trx`). The final affected14-case regression is
recorded below after completion; no claim of a repeated full Client pass. Native
NOT RUN and final corpus OPEN. Self-review is not independent review or approval.

Final affected Client **14/14 PASS** (`ep8-us3-tk1-client-focused-final.trx`). All reproduced failures resolved.
