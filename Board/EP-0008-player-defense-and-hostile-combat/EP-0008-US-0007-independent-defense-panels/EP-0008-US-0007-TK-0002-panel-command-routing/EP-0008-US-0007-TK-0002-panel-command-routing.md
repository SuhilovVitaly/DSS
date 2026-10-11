---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0007-independent-defense-panels
ticket: EP-0008-US-0007-TK-0002-panel-command-routing
title: "Ручные команды и независимый pending каждого аппарата"
stage: implemented
layer: client
depends_on: ["EP-0008-US-0007-TK-0001-panel-instances"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Ручные команды и независимый pending каждого аппарата

STATUS: IMPLEMENTED (automated checks; self-review)

## Why

Игрок отдельно управляет каждым аппаратом: пуск, автоматика, оператор и таймеры. Для выбранной торпеды обе панели показывают собственный шанс и его числовой расчёт. Этот тикет обеспечивает: ручные команды и независимый pending каждого аппарата.

## Decisions

Решения Q06,Q29,Q36,Q38,Q46 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0007-independent-defense-panels.md): AC-0001, AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/ManualDefenseUiFlowTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Execution scope update — 2026-10-11: baseline `5dfef548496649749f274cc884dff0db7e5a51c7` published/remote verified. Real screen now passes owner and instance callbacks to TK-0001 panels. Full Client tests found the old CountermeasureUiFlowTests locating a singleton caption/command; before edit, add `tests/DeepSpaceSaga.Client.Tests/CountermeasureUiFlowTests.cs` to address the intended module key explicitly. GameSessionScreen.Combat.cs needs no production edit; ownerless compatibility helpers are not used for instance dispatch.

Шаг **24** [общего порядка](../../Tickets.md).
- [EP-0008-US-0007-TK-0001-panel-instances](../../EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0001-panel-instances/EP-0008-US-0007-TK-0001-panel-instances.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Подключить module-specific fire/toggle/selection/preview callback. pending dictionary по module и CommandId; double-click блокирует только данный аппарат, второй остаётся доступным. Engine result снимает только соответствующий pending, selection не меняет target отправленной команды. Проверить реальные клики через LocalSession, pause и overlay routing.
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

- [x] `Two_paused_clicks_launch_from_two_modules`
- [x] `Double_click_does_not_duplicate_launch`
- [x] `Selection_change_preserves_captured_target`
- [x] `Panel_click_does_not_select_map_object`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
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

Dependency `5dfef548496649749f274cc884dff0db7e5a51c7` published/remote verified. Real GameSessionScreen supplies owner identity, module-specific Fire/toggle/status/operator callbacks and wheel routing. Fire requires matching authoritative owner/module/selected-torpedo preview;0% stays eligible. Per-owner/module pending retains CommandId and captured target, survives successful transport until matching final receipt, ignores unrelated/deferred receipts and independently handles sync/async/cancel failures. Existing overlay priority retained.

Initial focused **128/128 PASS**; full Client **1793/1796** exposed three scale variants of the old singleton-caption test, fixed to explicit module identity and unchanged second-auto assertion. Final affected panel/real UI flows **152/152 PASS** (`ep8-us7-tk2-review.trx`), including15 new manual-flow cases and real LocalSession double clicks from both modules at paused physical1ms with100%/150% UI scale. Coverage includes coalesced receipts, per-module pending, zero chance, selection change, stale owner/module/target/preview/power, overlay and target preservation. Client build 0 warnings/errors, scoped format verify and diff PASS. No repeated clean full-suite claim. Self-review complete; text/formula/timers follow TK-0003/0004; native remains OPEN.
