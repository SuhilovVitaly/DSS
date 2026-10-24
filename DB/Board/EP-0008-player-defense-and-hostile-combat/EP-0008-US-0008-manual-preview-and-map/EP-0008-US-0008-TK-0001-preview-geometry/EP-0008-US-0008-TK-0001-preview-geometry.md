---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0008-manual-preview-and-map
ticket: EP-0008-US-0008-TK-0001-preview-geometry
title: "Геометрия прогноза ручного пуска конкретного аппарата"
stage: implemented
layer: client
depends_on: ["EP-0008-US-0007-TK-0004-combat-localization"]
files_touched: 6
serves: ["AC-0001","AC-0003"]
created: 2026-10-07
revision: 1
---

# Геометрия прогноза ручного пуска конкретного аппарата

STATUS: IMPLEMENTED; self-review complete; native OPEN

## Why

Hover над пуском показывает прогноз конкретного аппарата. Радиусы появляются только у соответствующей панели, а активная ПРО показывает шанс и остаток времени. Этот тикет обеспечивает: геометрия прогноза ручного пуска конкретного аппарата.

## Decisions

Решения Q30,Q38,Q45–Q46,Q50 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0008-manual-preview-and-map.md): AC-0001, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Создаётся предыдущим тикетом EP-0008-US-0007-TK-0002-panel-command-routing | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/ManualDefensePreviewTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

Execution scope update — 2026-10-11: baseline `74248c9f51fc1641548cb2f741a5c658f451e4a7` published/remote verified. Add `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` for the actual render/pose hook; add `src/DeepSpaceSaga.Contracts/DefenseLaunchPreview.cs` and `src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs` to publish the same known first target collision deadline that Engine passes to PlanManual. This avoids inventing a Client collision search over hidden objects or a second set of combat rules. Client geometry uses the confirmed snapshot time/pose and shared Motion; deadline is only a prediction, never a promised outcome. Tests exercise the real Engine command path and complete save-state immutability while hovering. Effective scope six files.

## Dependencies

Шаг **27** [общего порядка](../../Tickets.md).
- [EP-0008-US-0007-TK-0004-combat-localization](../../EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0004-combat-localization/EP-0008-US-0007-TK-0004-combat-localization.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Построить preview из confirmed launch pose/target route и module-captured constraints shared Motion. Gray/отдельный существующий preview style; unknown encounter вместо крестика. No gameplay side effects; stale preview target убрать до нового ответа snapshot. Не обещать contact после известного истечения target/PR.
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

- [x] `Hover_preview_matches_real_launch_geometry`
- [x] `Unreachable_preview_has_no_false_cross`
- [x] `Preview_does_not_change_rng_or_busy`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency `74248c9f51fc1641548cb2f741a5c658f451e4a7` published/remote verified. Actual Fire hover resolves the current owner/module button and matching authoritative selected-target preview. Shared PlanManual uses confirmed snapshot pose/time, that module speed/turn/lifetime, and the Engine projected first target-contact deadline. Geometry stops at the first500m contact or the bounded no-solution pursuit segment, never invents an infinite ray or later centre rendezvous. Gray configured preview includes authoritative chance; unknown encounter is explicit without a cross. Cache clears on pending/stale target/overlay/hover leave and never mutates world/RNG.

Seven new real-screen cases cover exact real Engine launch geometry and first collision6501ms at100/120/150%, whole save-state immutability, per-module constraints,0% eligibility, short lifetime/earlier target collision, overlay/stale target and raster color. Full Client **1818/1818 PASS**, Engine **1837/1837 PASS** (excluding final CorrectnessCorpus); TRX `ep8-us8-tk1-client.trx`, `ep8-us8-tk1-engine.trx`. Final hover identity/chance label review **38/38 PASS**, `ep8-us8-tk1-review.trx`. Solution build0warnings/errors, scoped format verify/diffPASS. Self-review complete; native/final corpus OPEN.
