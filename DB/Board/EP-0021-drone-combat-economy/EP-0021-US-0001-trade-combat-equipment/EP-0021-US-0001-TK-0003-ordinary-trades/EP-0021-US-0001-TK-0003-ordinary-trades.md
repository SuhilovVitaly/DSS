---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0001-trade-combat-equipment
ticket: EP-0021-US-0001-TK-0003-ordinary-trades
title: Покупка и перепродажа штатной котировкой
stage: approved
layer: engine
depends_on: [EP-0021-US-0001-TK-0001-equipment-skus]
files_touched: 1
serves: [AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Покупка и перепродажа штатной котировкой

## Why

Часть [Торговля дронами и боевыми модулями](../EP-0021-US-0001-trade-combat-equipment.md). Приёмка этой части покрывает AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/CombatEquipmentTradeTests.cs` | Новый файл в рамках тикета. | Именованный класс CombatEquipmentTradeTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить проверку обычных Buy/Sell на fixture с ограниченным складом и установленным cargo. Проверить границу массы, актуальность визита/revision, частичный Sell, отказ без проводок и повтор CommandId. Production-путь существует, отдельный обработчик боевых товаров не требуется.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:37–110,134–164` подтверждает обычный товарный путь. Боевой каталог и эталонная сборка приведены в эпике.
- Подтверждённый Buy/Sell согласованно меняет деньги, остатки и груз, пишет квитанцию и себестоимость. Недостаточный грузовой объём и повтор сделки безопасны.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CombatEquipmentTradeTests.Ac02` покрывает **AC-02**. Подтверждённый Buy/Sell согласованно меняет деньги, остатки и груз, пишет квитанцию и себестоимость. Недостаточный грузовой объём и повтор сделки безопасны.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0001-TK-0003-ordinary-trades-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CombatEquipmentTradeTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
