---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0005-manual-countermeasure-fire
ticket: EP-0008-US-0005-TK-0002-manual-launch-execution
title: "Команда ручного пуска с безопасным повтором"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0005-TK-0001-manual-guidance"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Команда ручного пуска с безопасным повтором

STATUS: IMPLEMENTED / AUTOMATED PASS

## Why

Игрок выбирает вражескую торпеду и запускает ПРО с конкретного готового аппарата на расстоянии до200км. Он может рисковать при нулевом шансе или неизвестной встрече, повторять попытки и запускать две ПРО по одной цели. Этот тикет обеспечивает: команда ручного пуска с безопасным повтором.

## Decisions

Решения Q05–Q11,Q16,Q25–Q26,Q29,Q31,Q35,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0005-manual-countermeasure-fire.md): AC-0001, AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/ManualCountermeasureFireTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **17** [общего порядка](../../Tickets.md).
- [EP-0008-US-0005-TK-0001-manual-guidance](../EP-0008-US-0005-TK-0001-manual-guidance/EP-0008-US-0005-TK-0001-manual-guidance.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Реализовать defense.fire на общем spawn execution с LaunchMode. Проверять hostility по owner relation, captured fallback после потери owner; собственные/нейтральные/дружественные не допустимы. CommandId повтор не создаёт второй снаряд; stale target не заменяется selection. 0%/no-solution manual разрешены, auto flag не влияет. Ставить attempt только после успешного spawn; busy отдельный на(module,owner).
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

- [x] `Paused_manual_fire_creates_busy_without_time`
- [x] `Zero_chance_and_unreachable_manual_fire_allowed`
- [x] `Two_modules_can_manually_target_same_torpedo`
- [x] `Duplicate_or_rejected_fire_does_not_spawn`

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

Dependency `6b0d1f29d74c78c19a721df8e081466f25102333` published/remote verified. Real defense.fire dispatcher validates owner/module/readiness/operator, selected torpedo/current owner hostility with captured orphan fallback and inclusive manual range. Shared auto/manual spawn captures mode/accuracy/maneuverability/lifetime/deadline, records module/mode facts and marks attempt only on successful spawn. Per-module busy remains independent. Durable successful launch CommandIds reuse the persisted combat identity set. Manual no-solution re-plans use remaining captured lifetime and never invent ETA. No generic module cooldown introduced; catalog declaration follows TK-0003.

Full non-corpus Engine **1799/1799 PASS** (`ep8-us5-tk2-engine.trx`), final manual review **21/21 PASS** (`ep8-us5-tk2-engine-review.trx`), affected Client **20/20 PASS** (`ep8-us5-tk2-client.trx`). Covers pause/auto toggle, two modules, zero chance/unreachable, busy/invalid/own/friendly/neutral, exact range, operator/power/structure, receipt eviction, repeat after miss/reload, captured hostility after real owner destruction, valid custom third-object target, real-file positive-chance manual continuation and replay. File fixture initially used NewGame loader flag and was corrected to explicit Load mode. Solution build 0 warnings/errors; format verify/diff PASS.

Self-review complete. Lifetime event removal/arbitration is assigned US-0006; full new-format validation (including 0% manual save) and hostile-state persistence remain US-0011. Native/final corpus OPEN. These downstream gates are not claimed complete by this ticket.
