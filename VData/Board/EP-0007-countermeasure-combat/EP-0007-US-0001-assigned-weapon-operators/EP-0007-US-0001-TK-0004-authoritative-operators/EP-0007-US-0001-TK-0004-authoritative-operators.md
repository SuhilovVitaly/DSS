---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0001-assigned-weapon-operators
ticket: EP-0007-US-0001-TK-0004-authoritative-operators
title: "Проверка оператора и фиксация рейтинга при пуске"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0009-TK-0001-legacy-combat-fixtures"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-04
revision: 1
---

# Проверка оператора и фиксация рейтинга при пуске

STATUS: DRAFT

## Why

Как игрок, я вижу, кто управляет каждым аппаратом и с каким навыком. Назначения задаются сценарием, а отсутствие оператора блокирует работу. Рейтинг торпеды фиксируется при пуске. Этот тикет обеспечивает: проверка оператора и фиксация рейтинга при пуске.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002, AC-0003 [истории](../EP-0007-US-0001-assigned-weapon-operators.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 4 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs` | Создаётся зависимостью EP-0007-US-0001-TK-0002-operator-schema | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 10 [плана](../../Tickets.md).
- [EP-0007-US-0009-TK-0001-legacy-combat-fixtures](../../EP-0007-US-0009-countermeasure-acceptance/EP-0007-US-0009-TK-0001-legacy-combat-fixtures/EP-0007-US-0009-TK-0001-legacy-combat-fixtures.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

ResolveWeaponOperator(owner,module,skillType), captured TorpedoRating, projected per-module operator status.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Добавить guard до создания торпеды/ID/RNG. База*навык/50 без раннего округления; навык0 допустим, наличие человека обязательно. Показать operator on launcher snapshot; сохранять знания по старому правилу. Непринятая команда без побочных эффектов. Смена параметров позднее не меняет активную торпеду.
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

- [ ] `Missing_operator_blocks_fire_without_side_effects`
- [ ] `Torpedo_rating_is_captured_at_launch`
- [ ] `Zero_skill_operator_has_zero_rating`
- [ ] `Operator_projection_is_authoritative`

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
