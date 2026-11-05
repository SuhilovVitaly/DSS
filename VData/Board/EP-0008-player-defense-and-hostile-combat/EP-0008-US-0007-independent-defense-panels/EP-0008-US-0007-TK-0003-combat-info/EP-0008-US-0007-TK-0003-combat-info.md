---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0007-independent-defense-panels
ticket: EP-0008-US-0007-TK-0003-combat-info
title: "Расчёт шанса, lifetime и поведение выбранного врага"
stage: implemented
layer: client
depends_on: ["EP-0008-US-0007-TK-0002-panel-command-routing"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Расчёт шанса, lifetime и поведение выбранного врага

STATUS: IMPLEMENTED; self-review complete; native OPEN

## Why

Игрок отдельно управляет каждым аппаратом: пуск, автоматика, оператор и таймеры. Для выбранной торпеды обе панели показывают собственный шанс и его числовой расчёт. Этот тикет обеспечивает: расчёт шанса, lifetime и поведение выбранного врага.

## Decisions

Решения Q06,Q29,Q36,Q38,Q46 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0007-independent-defense-panels.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:18 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:15 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Создаётся предыдущим тикетом EP-0008-US-0007-TK-0002-panel-command-routing | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatInfoTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Execution scope update — 2026-10-11: baseline `300a4e561be2fe53513ed558c060a0c7d3aee0b2` published/remote verified. Add `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` to wire per-instance detail lines and project the shared predicted PR pose into selected info for the physical remaining-flight timer. Effective scope is five files including tests. Selected hostile phase/radius is already implemented and covered by US-0004-TK-0005; retain it.

Шаг **25** [общего порядка](../../Tickets.md).
- [EP-0008-US-0007-TK-0002-panel-command-routing](../EP-0008-US-0007-TK-0002-panel-command-routing/EP-0008-US-0007-TK-0002-panel-command-routing.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Каждый panel отображает accuracy−target maneuverability, frozen chance активной ПРО и remaining flight/reload с понятными label. Info selected NPC показывает phase и desired radius, не постоянный map label. У selected PR показывать launch mode/owner/module/target/lifetime; не скрывать доступность только из-за0%. Не выдавать skill как действующий множитель.
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

- [x] `Selected_enemy_info_has_ai_phase_and_radius`
- [x] `Each_panel_uses_own_preview`
- [x] `Flight_and_reload_timers_are_not_confused`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
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

## Execution evidence — 2026-10-11

Dependency `300a4e561be2fe53513ed558c060a0c7d3aee0b2` published/remote verified. Per-module selected-target preview displays authoritative accuracy, maneuverability and chance; active flight retains its captured chance. Remaining flight uses physical time and the earlier coast/global deadline; reload remains separately labeled. Selected PR includes mode/owner/module/target/captured parameters/maximum and remaining lifetime, uses shared predicted motion, and describes skill as metadata. Details wrap with measured widths; tall preferred panels remain reachable through clipped scrolling. Existing selected hostile phase/radius retained.

Initial focused37/38 exposed an incorrectly constructed test route (plannerVersion mistaken for elapsed time); corrected to explicit ElapsedMs. Full Client **1808/1808 PASS**, `ep8-us7-tk3-client.trx`, including five new combat-info cases and10 hostile selected-info regressions. Client build0 warnings/errors, scoped format verify and diff PASS. Self-review complete; localization follows TK-0004. Native/window acceptance remains OPEN.
