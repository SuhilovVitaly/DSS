---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0001-trade-combat-equipment
ticket: EP-0021-US-0001-TK-0001-equipment-skus
title: Штучный каталог оснащения
stage: approved
layer: content-data
depends_on: []
files_touched: 2
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Штучный каталог оснащения

## Why

Часть [Торговля дронами и боевыми модулями](../EP-0021-US-0001-trade-combat-equipment.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json` | Существующий item catalog, строки 1–89. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/CombatGoodsContentTests.cs` | Новый файл в рамках тикета. | Именованный класс CombatGoodsContentTests и его данные. |

## Public API after the change

Формат runtime-данных описан в Implementation steps. Общий API C# сохраняется.

## Implementation steps

Добавить item.drone.heavy.basic и item.drone.light.basic массой 780/620 кг (рама + плиты + два двигателя), item.drone.gun.sword.t1 / spear.t1 70/65 кг, item.drone.shield.t1 и item.drone.decoy.t1 по 10 кг. Good/Piece/Cargo, шаг 1, цены из сметы эпика. Проверить каждый ID, массу и цену относительно battle JSON.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:37–110,134–164` подтверждает обычный товарный путь. Боевой каталог и эталонная сборка приведены в эпике.
- Шесть типов боевого товара имеют стабильную связь с каталогом боя, корректные цены, штучную массу и доступность в обычной Trade.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CombatGoodsContentTests.Ac01` покрывает **AC-01**. Шесть типов боевого товара имеют стабильную связь с каталогом боя, корректные цены, штучную массу и доступность в обычной Trade.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0001-TK-0001-equipment-skus-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CombatGoodsContentTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
