---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
ticket: EP-0001-US-0011-TK-0004-voyage-profit-texts
title: Локализованные строки рейсовой прибыли
stage: approved
layer: content-data
depends_on: [EP-0001-US-0011-TK-0001-voyage-finance-contract]
files_touched: 3
serves: [AC-01, AC-03, AC-04]
created: 2026-09-21T12:56:24Z
revision: 1
---

# Локализованные строки рейсовой прибыли

## Why

Finance и Trade history должны показывать одинаковые понятные labels для всех authoritative компонентов, unknown COGS/net, долга и непроданного cargo. Тексты принадлежат Client content, а не Contracts/Engine и не должны быть hardcoded в экранах.

## Decisions

Пользователь дополнительных формулировок не задавал. Строки следуют понятиям AC-01/03/04 и не обещают passenger/event операции при нулевых значениях.

## Assumptions

Используется текущая convention flat JSON keys `Section.Name`; existing English/Russian locale files должны иметь идентичный набор ключей. Формат чисел/валюты остаётся в Client formatter и не записывается в locale values.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Trade keys уже существуют около `:124–136`; voyage finance keys отсутствуют | Добавить exact `Finance.Voyage*` и `Trade.Voyage*` keys, валидный JSON |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Зеркальный Trade namespace около `:124–136`; voyage finance keys отсутствуют | Добавить semantic-equivalent Russian values с теми же placeholders |
| `tests/DeepSpaceSaga.Client.Tests/LocalizationTests.cs` | Required Trade keys проверяются в `:20–32`; locale parity/format tests уже существуют | Добавить новый required-key set и placeholder parity assertions |

## Public API after the change

No CLR API change. Обязательные keys:

```text
Finance.VoyageTitle
Finance.VoyageRoute
Finance.VoyageState.InTransit
Finance.VoyageState.AwaitingRealization
Finance.VoyageState.Finalized
Finance.VoyageState.Interrupted
Finance.GrossSales
Finance.CostOfGoodsSold
Finance.RouteFuelCost
Finance.PortFeesAssessed
Finance.PortFeesPaid
Finance.PortFeeDebt
Finance.EventCosts
Finance.PassengerPayout
Finance.PassengerPenalty
Finance.NetProfit
Finance.Unavailable
Finance.UnsoldCargo
Finance.NoVoyages
Trade.VoyageSummary
Trade.VoyageProfit
Trade.VoyageLoss
Trade.VoyageResultUnavailable
```

`Finance.VoyageRoute`, `Finance.UnsoldCargo` и Trade summary keys принимают только positional placeholders, одинаковые в обеих локалях. Знак/цвет/числовое форматирование не кодируются текстом.

## Implementation steps

1. Добавить полный набор exact keys в English/Russian рядом с существующими Finance/Trade namespaces; сохранить JSON syntax/order conventions.
2. English/Russian различают assessed, paid и outstanding debt; `Unavailable` не переводится как zero/нет прибыли.
3. Passenger/event labels не включают утверждение, что mechanic всегда существует; UI показывает их только при ненулевом authoritative value.
4. Добавить keys в required list и test равенства placeholder indexes/count между двумя locale files.
5. Проверить JSON parse и отсутствие duplicate/missing keys; production code/экран в этом тикете не менять.

## Out of scope

Rendering/layout, выбор строк по состоянию, arithmetic, Contracts/Engine, новый язык, изменение существующих Trade captions.

## Invariants

- Runtime data JSON принадлежит Client и matching test project — `DeepSpaceSaga.Client.Tests`.
- Ключи двух локалей совпадают; placeholders совместимы.
- Тексты не рассчитывают числа и не скрывают unknown как 0.
- Только три файла таблицы.

## Tests

Named tests в `LocalizationTests`:

- `Voyage_finance_keys_exist_in_all_locales` (AC-01/03/04).
- `Voyage_finance_placeholders_match_between_locales` (AC-01/04).
- `Locale_files_remain_valid_json_without_duplicate_keys` (AC-04).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Exact key set, semantics, placeholder rule, three allowed files и named tests перечислены; implementer не выбирает тексты, namespaces или формат данных за пределами тикета.
