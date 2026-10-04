---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0006-defense-tactical-ui
ticket: EP-0007-US-0006-TK-0004-defense-command-panels
title: "Панели аппаратов и информация противоракеты"
stage: draft
layer: client
depends_on: ["EP-0007-US-0006-TK-0003-countermeasure-rendering"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
created: 2026-10-04
revision: 1
---

# Панели аппаратов и информация противоракеты

STATUS: DRAFT

## Why

Как игрок, я вижу полёт противоракеты, шанс перехвата и состояние аппаратов; могу отключить автоматическую защиту. Этот тикет обеспечивает: панели аппаратов и информация противоракеты.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0002, AC-0003 [истории](../EP-0007-US-0006-defense-tactical-ui.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/DefenseCommandPanelTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 25 [плана](../../Tickets.md).
- [EP-0007-US-0006-TK-0003-countermeasure-rendering](../../EP-0007-US-0006-defense-tactical-ui/EP-0007-US-0006-TK-0003-countermeasure-rendering/EP-0007-US-0006-TK-0003-countermeasure-rendering.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Отдельная PR panel, enable/disable commands; отображение operator и frozen chance breakdown.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Нет ручного пуска PR. Toggle влияет только на новые запуски. Нетоператора отличается от skill0. Запретить PR как Fire target и при enable-state, и при dispatch; Engine guard обязателен в US4. Выбор PR дляinfo разрешён. Отображать состояние по snapshot без локального угадывания.
3. Добавить перечисленные ниже проверки реального поведения. Зафиксировать фактический результат, не объявлять планируемые тесты пройденными.

## Out of scope

Другие production layers, файлы внеallowlist, наступательный AI и новые игровые механики вне эпика. Commit/push не входят. Если требуется шестой файл, сначала отдельное согласованное расширениеscope или новый тикет.

## Invariants

- Числа/формулы/границы берутся из эпика; UI не решает исход боя.
- Physical gameplay time и monotonic effect time не смешиваются (EngineRequirements §2 и временной контракт эпика).
- Snapshot immutable; Client не обращается напрямую кEngine (CLAUDE Architecture).
- Непринятые команды и потеря цели до встречи не потребляют draw. Состояние/attempt/frozenchance переживают SaveLoad.
- Совместимость ID следует loader OrdinalIgnoreCase. Existing ordinary torpedo trails остаются скрытыми.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Планируемые проверки:

- [ ] `Auto_toggle_preserves_current_flight`
- [ ] `Operator_name_and_skill_shown_for_both_modules`
- [ ] `Countermeasure_selectable_but_fire_disabled`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

При scoped lint использовать dotnet format --verify-no-changes --include только изменённых C# файлов. Baseline failure документировать с точной командой; не расширять allowlist молча. Native проверка требуется там, где AC касается реального изображения/input; headless результат её не заменяет.

## Definition of Done

- [ ] Реализованы API/шаги вallowlist, covered критерии наблюдаемы.
- [ ] Named tests и сборка слоя пройдены; baseline ограничения записаны отдельно.
- [ ] Dependencies действительно готовы; нет временных заглушек или скрытой работы.
- [ ] Проверены pause/time domains/idempotency/persistence в применимой части.
- [ ] Diff reviewed; runtime/native статус описан честно.

## Self-containment check

Контракт, ограничения, файлы, зависимости и проверки перечислены здесь; формулы/таблица времени централизованы в связанном эпике. Не требуется восстанавливать смысл коротких ответов из чата. Новое обнаруженное несоответствие оформляется как gap, а не произвольное расширение работ.
