---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0007-balance-trade-and-sorties
ticket: EP-0021-US-0007-TK-0003-tune-start-parameters
title: Калибровка цен, запасов и стартовых средств
stage: approved
layer: content-data
depends_on: [EP-0021-US-0007-TK-0002-balance-report]
files_touched: 5
serves: [AC-03]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Калибровка цен, запасов и стартовых средств

## Why

Часть [Доступность стартового комплекта и баланс торговли](../EP-0021-US-0007-balance-trade-and-sorties.md). Приёмка этой части покрывает AC-03 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Смета аналитическая. Замеры и калибровка входят в результат.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Scenarios/CarrierStart/scenario.json` | Поверхность из Grounding истории. `TradeQuoteCalculator.cs:40–65` определяет цену каждой единицы при изменении запаса. `tools/DeepSpaceSaga.EconomyBalance/Program.cs:25–30,73–113` уже агрегирует метрики. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json` | Существующий item catalog, строки 1–89. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json` | Профили Industrial и Scientific/Military, строки 68–127 и 233–282. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Items/Good/carrier-refit-goods.json` | Поверхность из Grounding истории. `TradeQuoteCalculator.cs:40–65` определяет цену каждой единицы при изменении запаса. `tools/DeepSpaceSaga.EconomyBalance/Program.cs:25–30,73–113` уже агрегирует метрики. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/CarrierStartCalibrationTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierStartCalibrationTests и его данные. |

## Public API after the change

Формат runtime-данных описан в Implementation steps. Общий API C# сохраняется.

## Implementation steps

Запустить матрицу и менять только цены, стартовый баланс и запасы. Записать значения до/после и нарушения по seed в Board. Не принимать 56 000 как доказанное число. Если маршрут или корпус являются причиной, вернуть к владельцу соответствующего контракта с точным seed. Параметры корпуса и двигателя сохранять. Различать реальное соблюдение порогов и аналитическую оценку до реализации.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TradeQuoteCalculator.cs:40–65` определяет цену каждой единицы при изменении запаса. `tools/DeepSpaceSaga.EconomyBalance/Program.cs:25–30,73–113` уже агрегирует метрики.
- После прогона стартовые настройки откалиброваны по порогам эпика или результат помечен как не прошедший с конкретной причиной.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierStartCalibrationTests.Ac03` покрывает **AC-03**. После прогона стартовые настройки откалиброваны по порогам эпика или результат помечен как не прошедший с конкретной причиной.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0007-TK-0003-tune-start-parameters-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierStartCalibrationTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
