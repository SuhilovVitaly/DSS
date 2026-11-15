---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0002-station-supply
ticket: EP-0021-US-0002-TK-0001-military-supply-schema
title: Параллельные партии военного снабжения
stage: approved
layer: engine
depends_on: [EP-0021-US-0001-TK-0002-equipment-binding]
files_touched: 4
serves: [AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Параллельные партии военного снабжения

## Why

Часть [Ближайшие поставщики и пополнение запасов](../EP-0021-US-0002-station-supply.md). Приёмка этой части покрывает AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs` | Профиль, stock targets и fingerprint, StationMarketEconomyDefinition и EconomyFingerprintPayload. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | Загрузка реестров и проверка профилей, строки 146–188, 230–346. Товарный loader в 510–567. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs` | Точная сигнатура ValidateStationMarketProfiles в текущем registry. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/EquipmentSupplySchemaTests.cs` | Новый файл в рамках тикета. | Именованный класс EquipmentSupplySchemaTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить optional equipmentBatches к экономике профиля. Каждая запись id, periodHours, inputs, outputs. Все позиции известны и имеют объявленный target; уникальные id и положительные quantities, стабильный порядок. Включить заполненное поле в fingerprint, пустое оставляет прежний fingerprint.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.MarketTime.cs:112–201` выполняет почасовой поток, `StationGeographyV2.cs:9–66` связывает стартовый кластер. Профили описаны в `Data/Markets/station-market-profiles.json:68–127,233–282`.
- Боевые партии потребляют сырьё, учитывают ёмкость и не останавливают выпуск гражданских товаров при дефиците или переполнении.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `EquipmentSupplySchemaTests.Ac02` покрывает **AC-02**. Боевые партии потребляют сырьё, учитывают ёмкость и не останавливают выпуск гражданских товаров при дефиците или переполнении.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0002-TK-0001-military-supply-schema-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.EquipmentSupplySchemaTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
