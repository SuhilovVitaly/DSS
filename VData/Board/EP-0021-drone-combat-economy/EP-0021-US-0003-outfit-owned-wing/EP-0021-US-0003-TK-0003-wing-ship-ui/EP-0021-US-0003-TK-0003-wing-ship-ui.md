---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0003-outfit-owned-wing
ticket: EP-0021-US-0003-TK-0003-wing-ship-ui
title: Ангары и сборки на экране Ship
stage: approved
layer: client
depends_on: [EP-0021-US-0003-TK-0002-fit-wing, EP-0021-US-0009-TK-0005-shipyard-station-screen]
files_touched: 5
serves: [AC-03]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Ангары и сборки на экране Ship

## Why

Часть [Размещение и оснащение собственного крыла](../EP-0021-US-0003-outfit-owned-wing.md). Приёмка этой части покрывает AC-03 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs` | Точный тип ShipScreen, строка 18. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipLayout.cs` | Поверхность из Grounding истории. `TacticalBattleAssembly.cs:35–59` использует реальные компоненты и массу. `SimulationEngine.TradeExecution.cs:313–329` определяет сохранение basis груза. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Действующий русский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Действующий английский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/CarrierWingScreenTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierWingScreenTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Показать четыре отдельных слота, команды сборки, пилотов, модуль и остаток зарядов, массу и расчётные тактические параметры из проекции. Не вычислять готовность из текста; запускать действия через общий CarrierAction.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleAssembly.cs:35–59` использует реальные компоненты и массу. `SimulationEngine.TradeExecution.cs:313–329` определяет сохранение basis груза.
- Игрок видит состав, готовность, причину ограничения и свойства выбранной сборки.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierWingScreenTests.Ac03` покрывает **AC-03**. Игрок видит состав, готовность, причину ограничения и свойства выбранной сборки.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0003-TK-0003-wing-ship-ui-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierWingScreenTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
