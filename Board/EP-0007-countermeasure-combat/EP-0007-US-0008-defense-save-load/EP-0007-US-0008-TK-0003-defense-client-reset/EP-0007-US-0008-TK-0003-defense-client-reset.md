---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0008-defense-save-load
ticket: EP-0007-US-0008-TK-0003-defense-client-reset
title: "Загрузка истории без повторных анимаций"
stage: draft
layer: client
depends_on: ["EP-0007-US-0008-TK-0002-defense-persistence"]
files_touched: 5
serves: ["AC-0001","AC-0002"]
created: 2026-10-04
revision: 1
---

# Загрузка истории без повторных анимаций

STATUS: DRAFT

## Why

Как игрок, я сохраняю бой в любой фазе и после загрузки получаю то же продолжение без нового броска и повторных эффектов. Этот тикет обеспечивает: загрузка истории без повторных анимаций.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002 [истории](../EP-0007-US-0008-defense-save-load.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | Создаётся зависимостью EP-0007-US-0006-TK-0003-countermeasure-rendering | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs` | Создаётся зависимостью EP-0007-US-0007-TK-0002-collapsible-combat-journal | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/CountermeasureSessionRoundtripTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 31 [плана](../../Tickets.md).
- [EP-0007-US-0008-TK-0002-defense-persistence](../../EP-0007-US-0008-defense-save-load/EP-0007-US-0008-TK-0002-defense-persistence/EP-0007-US-0008-TK-0002-defense-persistence.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Session reset establishes event watermark, restores journal but not transient effects.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Реальный LocalSession save/load: показать сохранённые полёт/шанс/таймеры и историю; новые события после load рисуются ровноодинраз. Очистить старые tooltip, selectedmissingPR, pendingcommands; не блокировать живой launcher после reset.
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

- [ ] `Load_restores_history_without_effect_replay`
- [ ] `New_event_after_load_is_not_suppressed`
- [ ] `Save_load_preserves_selected_live_countermeasure`

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
