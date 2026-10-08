---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0007-independent-defense-panels
ticket: EP-0008-US-0007-TK-0003-combat-info
title: "Расчёт шанса, lifetime и поведение выбранного врага"
stage: draft
layer: client
depends_on: ["EP-0008-US-0007-TK-0002-panel-command-routing"]
files_touched: 4
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Расчёт шанса, lifetime и поведение выбранного врага

STATUS: DRAFT

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

Шаг **24** [общего порядка](../../Tickets.md).
- [EP-0008-US-0007-TK-0002-panel-command-routing](../../EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0002-panel-command-routing/EP-0008-US-0007-TK-0002-panel-command-routing.md).
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

- [ ] `Selected_enemy_info_has_ai_phase_and_radius`
- [ ] `Each_panel_uses_own_preview`
- [ ] `Flight_and_reload_timers_are_not_confused`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
