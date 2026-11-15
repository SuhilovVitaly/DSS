---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0002-station-supply
ticket: EP-0021-US-0002-TK-0003-supply-content
title: Стартовые объёмы и профильные партии
stage: approved
layer: content-data
depends_on: [EP-0021-US-0002-TK-0001-military-supply-schema, EP-0021-US-0009-TK-0001-shipyard-content, EP-0021-US-0001-TK-0001-equipment-skus, EP-0021-US-0011-TK-0001-carrier-save-identity]
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Стартовые объёмы и профильные партии

## Why

Часть [Ближайшие поставщики и пополнение запасов](../EP-0021-US-0002-station-supply.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json` | Профили Industrial и Scientific/Military, строки 68–127 и 233–282. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/EquipmentSupplyContentTests.cs` | Новый файл в рамках тикета. | Именованный класс EquipmentSupplyContentTests и его данные. |

## Public API after the change

Формат runtime-данных описан в Implementation steps. Общий API C# сохраняется.

## Implementation steps

Добавить explicitStockTargets для cargo/ангаров/кают и боевых SKU, которые допустимы в стартовой локальной выкладке. Добавить независимые партии Industrial и Scientific/Military по таблице эпика. Ёмкость выбранных стартовых позиций выдерживает весь набор даже после size scaling.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.MarketTime.cs:112–201` выполняет почасовой поток, `StationGeographyV2.cs:9–66` связывает стартовый кластер. Профили описаны в `Data/Markets/station-market-profiles.json:68–127,233–282`.
- В каждом валидном старте станция A предлагает грузовой модуль, мостик, четыре ангара и четыре места для пилотов, станция B предлагает весь боевой набор и четырёх кандидатов. Маршрут A→B выбирается из реально доступных соседей.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `EquipmentSupplyContentTests.Ac01` покрывает **AC-01**. В каждом валидном старте станция A предлагает грузовой модуль, мостик, четыре ангара и четыре места для пилотов, станция B предлагает весь боевой набор и четырёх кандидатов. Маршрут A→B выбирается из реально доступных соседей.
- `EquipmentSupplyContentTests.Ac02` покрывает **AC-02**. Боевые партии потребляют сырьё, учитывают ёмкость и не останавливают выпуск гражданских товаров при дефиците или переполнении.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0002-TK-0003-supply-content-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.EquipmentSupplyContentTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
