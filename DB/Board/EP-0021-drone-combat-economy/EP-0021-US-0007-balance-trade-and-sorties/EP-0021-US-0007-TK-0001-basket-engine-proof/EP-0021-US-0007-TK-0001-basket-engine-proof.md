---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0007-balance-trade-and-sorties
ticket: EP-0021-US-0007-TK-0001-basket-engine-proof
title: Воспроизводимый сбор эталонной четвёрки
stage: approved
layer: engine
depends_on: [EP-0021-US-0002-TK-0004-nearby-outfit-route, EP-0021-US-0003-TK-0002-fit-wing, EP-0021-US-0004-TK-0002-owned-wing-assembly, EP-0021-US-0006-TK-0001-station-services, EP-0021-US-0008-TK-0002-docked-start-bootstrap, EP-0021-US-0011-TK-0002-carrier-world-continuity, EP-0021-US-0012-TK-0001-campaign-launch]
files_touched: 1
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Воспроизводимый сбор эталонной четвёрки

## Why

Часть [Доступность стартового комплекта и баланс торговли](../EP-0021-US-0007-balance-trade-and-sorties.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Смета аналитическая. Замеры и калибровка входят в результат.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/CarrierStartBalanceTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierStartBalanceTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить независимые тесты состава по исходному initial + layouts, покупки и подтверждения на A/B, времени по рейсовому плану, оплаты из стартового баланса без продаж, Load между станциями, монетизации и восстановления. Разбить DefaultSeed, SeedCorpus и StressCase на точные методы.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TradeQuoteCalculator.cs:40–65` определяет цену каждой единицы при изменении запаса. `tools/DeepSpaceSaga.EconomyBalance/Program.cs:25–30,73–113` уже агрегирует метрики.
- Утверждаемый по данным старт обеспечивает полный набор, топливо, сборы и резерв в пределах указанного маршрута. Отчёт объясняет каждую цену, источник, массу, время и остаток Tokens.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierStartBalanceTests.Ac01` покрывает **AC-01**. Утверждаемый по данным старт обеспечивает полный набор, топливо, сборы и резерв в пределах указанного маршрута. Отчёт объясняет каждую цену, источник, массу, время и остаток Tokens.
- `CarrierStartBalanceTests.Ac02` покрывает **AC-02**. Сценарий сохранённой торговли и сценарий с боями сравниваются на одинаковых мирах, при дефиците и полной потере. Повторная выдача, моментальный арбитраж и скрытый soft lock проверены.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0007-TK-0001-basket-engine-proof-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierStartBalanceTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
