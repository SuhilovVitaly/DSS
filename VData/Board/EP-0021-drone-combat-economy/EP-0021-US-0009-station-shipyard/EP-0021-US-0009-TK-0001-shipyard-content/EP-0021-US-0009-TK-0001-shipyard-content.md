---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0009-station-shipyard
ticket: EP-0021-US-0009-TK-0001-shipyard-content
title: Товары верфи и одноместный ангар
stage: approved
layer: content-data
depends_on: []
files_touched: 4
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Товары верфи и одноместный ангар

## Why

Часть [Покупка и установка модулей носителя на верфи](../EP-0021-US-0009-station-shipyard.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/module-types.json` | Категории мостика, двигателя, груза и жилья, строки 4–86. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Modules/Hangar/modules-hangar.json` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Items/Good/carrier-refit-goods.json` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/CarrierRefitGoodsContentTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierRefitGoodsContentTests и его данные. |

## Public API after the change

Формат runtime-данных описан в Implementation steps. Общий API C# сохраняется.

## Implementation steps

Создать module.hangar и module.hangar.drone.single, 2×2 клетки, 2 500 кг, 150 SP, питание 0. SKU: item.ship.cargo.basic→module.container.basic, item.ship.bridge.basic→module.bridge.navigation.computer.basic, item.ship.hangar.single→module.hangar.drone.single, item.ship.quarters.four→living.quarters.mk1.four-cabins. Цены по смете и массы по определениям. Товарные позиции используют SKU с Cargo/Good/Piece. Реальные размеры для существующих cargo/bridge/quarters при установке задать 2×2/1×1/2×2, существующие типы сценариев не перекладывать.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:195–276` адресует обычную покупку модулю, а `SimulationEngine.cs:1326–1390` строит установленные модули из сценария. `SimulationEngine.cs:2430–2480` связывает стыковку с навигационным путём.
- Верфь продаёт грузовой модуль, мостик, ангар и жилой модуль за Tokens. Первый модуль устанавливается без контейнера.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierRefitGoodsContentTests.Ac01` покрывает **AC-01**. Верфь продаёт грузовой модуль, мостик, ангар и жилой модуль за Tokens. Первый модуль устанавливается без контейнера.
- `CarrierRefitGoodsContentTests.Ac02` покрывает **AC-02**. Установка из конечного запаса проверяет визит, платёж и корпус. Повтор безопасен. Ангар вмещает одного дрона.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0009-TK-0001-shipyard-content-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierRefitGoodsContentTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
