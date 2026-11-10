---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0001-trade-combat-equipment
ticket: EP-0021-US-0001-TK-0002-equipment-binding
title: Связь товара с боевым определением
stage: approved
layer: engine
depends_on: [EP-0021-US-0001-TK-0001-equipment-skus]
files_touched: 3
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Связь товара с боевым определением

## Why

Часть [Торговля дронами и боевыми модулями](../EP-0021-US-0001-trade-combat-equipment.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalEquipmentGoods.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | Загрузка реестров и проверка профилей, строки 146–188, 230–346. Товарный loader в 510–567. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalEquipmentGoodsTests.cs` | Новый файл в рамках тикета. | Именованный класс TacticalEquipmentGoodsTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Создать internal TacticalEquipmentGoods.Resolve(string itemTypeId), возвращающий классифицированную запись {Kind, TacticalId}. Для рам задать plate.medium и 2×drive.standard. В активированном контенте проверять тип, ссылки и массу до публикации каталога. Исторические сценарии без боевого контента продолжают работать, а ошибочный набор даёт конкретную причину.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:37–110,134–164` подтверждает обычный товарный путь. Боевой каталог и эталонная сборка приведены в эпике.
- Шесть типов боевого товара имеют стабильную связь с каталогом боя, корректные цены, штучную массу и доступность в обычной Trade.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `TacticalEquipmentGoodsTests.Ac01` покрывает **AC-01**. Шесть типов боевого товара имеют стабильную связь с каталогом боя, корректные цены, штучную массу и доступность в обычной Trade.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0001-TK-0002-equipment-binding-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.TacticalEquipmentGoodsTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
