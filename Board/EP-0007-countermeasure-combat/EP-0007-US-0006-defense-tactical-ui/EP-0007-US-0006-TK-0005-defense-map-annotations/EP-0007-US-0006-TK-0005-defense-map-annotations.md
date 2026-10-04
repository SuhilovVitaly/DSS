---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0006-defense-tactical-ui
ticket: EP-0007-US-0006-TK-0005-defense-map-annotations
title: "Шанс, подсказка, радиус и статус пирата"
stage: draft
layer: client
depends_on: ["EP-0007-US-0006-TK-0004-defense-command-panels"]
files_touched: 4
serves: ["AC-0002","AC-0003"]
created: 2026-10-04
revision: 1
---

# Шанс, подсказка, радиус и статус пирата

STATUS: DRAFT

## Why

Как игрок, я вижу полёт противоракеты, шанс перехвата и состояние аппаратов; могу отключить автоматическую защиту. Этот тикет обеспечивает: шанс, подсказка, радиус и статус пирата.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0002, AC-0003 [истории](../EP-0007-US-0006-defense-tactical-ui.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 4 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | Создаётся зависимостью EP-0007-US-0006-TK-0003-countermeasure-rendering | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/DefenseMapAnnotationsTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 26 [плана](../../Tickets.md).
- [EP-0007-US-0006-TK-0004-defense-command-panels](../../EP-0007-US-0006-defense-tactical-ui/EP-0007-US-0006-TK-0004-defense-command-panels/EP-0007-US-0006-TK-0004-defense-command-panels.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Chance labels наPR/encounter; tooltip breakdown; selected pirate range100km.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. World radius масштабируется камерой; 5px body и50px explosion остаются экранными. Отображать Готов/Наведение/Перезарядка с countdown у pirate label; NoOperator/autooff не выдавать за готовность. Hover без изменения цели. Исчезнувшая точка не сохраняет старый tooltip.
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

- [ ] `Chance_shown_at_projectile_and_encounter`
- [ ] `Marker_tooltip_uses_frozen_breakdown`
- [ ] `Selected_pirate_shows_scaled_range`

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
