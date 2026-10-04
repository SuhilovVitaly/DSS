---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0001-assigned-weapon-operators
ticket: EP-0007-US-0001-TK-0005-combat-id-compatibility
title: "Совместимость ID торпед с загрузчиком"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0001-TK-0003-weapon-rating-content-schema"]
files_touched: 2
serves: ["AC-0003"]
created: 2026-10-04
revision: 1
---

# Совместимость ID торпед с загрузчиком

STATUS: DRAFT

## Why

Как игрок, я вижу, кто управляет каждым аппаратом и с каким навыком. Назначения задаются сценарием, а отсутствие оператора блокирует работу. Рейтинг торпеды фиксируется при пуске. Этот тикет обеспечивает: совместимость id торпед с загрузчиком.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0003 [истории](../EP-0007-US-0001-assigned-weapon-operators.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 2 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs` | Существует: tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs:1 | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 4 [плана](../../Tickets.md).
- [EP-0007-US-0001-TK-0003-weapon-rating-content-schema](../../EP-0007-US-0001-assigned-weapon-operators/EP-0007-US-0001-TK-0003-weapon-rating-content-schema/EP-0007-US-0001-TK-0003-weapon-rating-content-schema.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Единая OrdinalIgnoreCase уникальность generated IDs.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Закрыть выявленный prerequisite EP6: torpedo ID collision использует ==, loader — case-insensitive. Узкий фикс и реальный save/reload с существующим ID отличающегося регистра; не переписывать несвязанные генераторы.
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

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Планируемые проверки:

- [ ] `Case_variant_existing_id_does_not_break_save_reload`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
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
