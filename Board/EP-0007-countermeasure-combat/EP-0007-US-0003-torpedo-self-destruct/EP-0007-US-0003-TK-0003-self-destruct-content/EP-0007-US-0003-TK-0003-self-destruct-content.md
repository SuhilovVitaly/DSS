---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0003-torpedo-self-destruct
ticket: EP-0007-US-0003-TK-0003-self-destruct-content
title: "Команда самоуничтожения в каталоге"
stage: draft
layer: content-data
depends_on: ["EP-0007-US-0003-TK-0002-self-destruct-execution"]
files_touched: 2
serves: ["AC-0001"]
created: 2026-10-04
revision: 1
---

# Команда самоуничтожения в каталоге

STATUS: DRAFT

## Why

Как игрок, я прекращаю затянувшуюся атаку кнопкой в панели аппарата. Торпеда взрывается без урона и освобождает оператора, даже на паузе. Преследовавшая её противоракета теряет цель. Этот тикет обеспечивает: команда самоуничтожения в каталоге.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001 [истории](../EP-0007-US-0003-torpedo-self-destruct.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 2 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Commands/TorpedoLauncher/commands.json` | Существует: src/DeepSpaceSaga.Client/Data/Commands/TorpedoLauncher/commands.json:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandContentTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 14 [плана](../../Tickets.md).
- [EP-0007-US-0003-TK-0002-self-destruct-execution](../../EP-0007-US-0003-torpedo-self-destruct/EP-0007-US-0003-TK-0002-self-destruct-execution/EP-0007-US-0003-TK-0002-self-destruct-execution.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

torpedo.selfDestruct в группе торпедного аппарата.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Выбор объекта карты не требуется; клиент подставляет active projectile ID из snapshot. Команда не меняет Fire.
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

- [ ] `Self_destruct_command_available_without_map_selection`

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
