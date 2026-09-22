---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
ticket: EP-0001-US-0010-TK-0004-cargo-result-texts
title: Тексты стоимости и результата груза
stage: approved
layer: content-data
depends_on: [EP-0001-US-0010-TK-0001-cargo-result-contract]
files_touched: 3
serves: [AC-07]
created: 2026-09-21T12:39:58Z
revision: 1
---

# Тексты стоимости и результата груза

## Why

Trade history должен различать расход на покупку, выручку, себестоимость и gross result, включая недоказуемую legacy cost. End state: RU/EN locale предоставляет однозначные labels/templates; отсутствие стоимости не выглядит как zero или profit.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Layer `content-data` принадлежит Client и проверяется matching Client tests.

## Decisions

2026-09-21T12:39:58Z — сохранён запрос пользователя на tickets US-0010. Отдельных wording-решений пользователя нет.

## Assumptions

Термин `gross result` в Trade означает только `sale proceeds − realized cargo cost`; он намеренно не называется чистой прибылью рейса. Token/Credits formatting следует существующему Trade.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Locale/English.json | :140–198 содержит Trade result/status/reason strings | Добавить cargo cost/result labels и full/partial/unavailable templates |
| src/DeepSpaceSaga.Client/Data/Locale/Russian.json | :140–198 зеркальная RU секция | Семантически эквивалентные строки без обещания net voyage profit |
| tests/DeepSpaceSaga.Client.Tests/LocalizationTests.cs | :14–61 проверяет locale loading/keys | Presence, placeholder arity и RU/EN distinction regressions |

## Public API after the change

Новые locale keys:

```text
Trade.PurchaseCost
Trade.SaleProceeds
Trade.RealizedCargoCost
Trade.GrossCargoResult
Trade.CargoCostUnavailable
Trade.CargoResultKnown
Trade.CargoResultPartialKnown
Trade.CargoResultUnknown
Trade.CargoResultPartialUnknown
```

Known templates получают item, actual, requested, proceeds, realized cost, gross result в фиксированном порядке, unknown templates — item, actual, requested, proceeds и literal localized unavailable. Знак gross сохраняется; отрицательное число не маскируется.

## Implementation steps

1. Добавить одинаковый набор keys в оба JSON рядом с существующими Trade result strings; сохранить valid JSON/UTF-8.
2. English использует `Purchase cost`, `Sale proceeds`, `Realized cargo cost`, `Gross cargo result`; Russian — `Стоимость покупки`, `Выручка`, `Реализованная себестоимость`, `Результат груза`.
3. Unknown явно говорит `Cost unavailable` / `Себестоимость недоступна`; не выводить 0, `profit`, `net profit` или оценку.
4. Full/partial templates различают actual/requested; purchase и refuel используют existing/prerequisite messages и не требуют fake cost result.
5. Tests загружают оба real files и проверяют все keys, exact placeholder indexes/counts, отрицательное форматируемое значение и отсутствие слова net/чистая в новых gross labels.

## Out of scope

Render/layout, Engine calculation, Finance strings, route expenses, смена существующей терминологии всего Client.

## Invariants

- Existing Trade locale section и formatting: `English.json:140–198`, `Russian.json:140–198`.
- Client не получает скрытые station Credits и не рассчитывает authoritative values.
- Gross cargo result не включает fuel/fees/passengers: epic `../Documentation.md:102`, US-0011 boundary.

## Tests

- `Cargo_result_keys_exist_in_english_and_russian_with_matching_placeholders` — AC-07.
- `Unknown_cost_messages_are_explicit_and_never_render_zero_profit` — AC-07.
- `Gross_result_labels_do_not_claim_net_voyage_profit` — AC-07.
- `Existing_trade_locales_remain_valid_and_loadable` — regression.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все изменения только в двух locale files и одном matching test file.
- AC-07 покрыт named tests; placeholder arity совпадает между RU/EN.
- Tests/build/format проходят либо baseline failure указан отдельно.
- Unknown не выглядит как zero, gross не называется net voyage profit.
- JSON валиден; существующие keys не переименованы.

## Self-containment check

Полный список keys, semantics, placeholder data и запрещённые misleading формулировки приведены. UI-решения не требуются.

