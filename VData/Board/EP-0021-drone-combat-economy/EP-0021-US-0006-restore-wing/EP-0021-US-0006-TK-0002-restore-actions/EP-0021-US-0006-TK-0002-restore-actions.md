---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0006-restore-wing
ticket: EP-0021-US-0006-TK-0002-restore-actions
title: Ремонт и боезапас в ангаре
stage: approved
layer: client
depends_on: [EP-0021-US-0006-TK-0001-station-services, EP-0021-US-0003-TK-0003-wing-ship-ui]
files_touched: 4
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Ремонт и боезапас в ангаре

## Why

Часть [Ремонт, заряды и замена после вылета](../EP-0021-US-0006-restore-wing.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs` | Точный тип ShipScreen, строка 18. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Действующий русский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Действующий английский ресурс локализации. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Client.Tests/WingRestorationScreenTests.cs` | Новый файл в рамках тикета. | Именованный класс WingRestorationScreenTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Показать корпус, статус, charges, цену и объяснение отказа. Подтверждать принятый quote; показывать свободный ангар и маршрут к закупке при уничтожении.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleSession.cs:118–147` фиксирует расход зарядов. Существующий мир использует реальные Tokens и квитанции (`SimulationEngine.TradeExecution.cs:94–110`).
- Станционная услуга восстанавливает hull, боеспособность и купленные заряды по авторитетной котировке. Недостаток средств не меняет дрона, повтор не списывает снова.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `WingRestorationScreenTests.Ac01` покрывает **AC-01**. Станционная услуга восстанавливает hull, боеспособность и купленные заряды по авторитетной котировке. Недостаток средств не меняет дрона, повтор не списывает снова.
- `WingRestorationScreenTests.Ac02` покрывает **AC-02**. После полной потери крыло восстанавливается торговлей и покупкой с конечного рынка, а Load и новый бой не чинят дронов.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0006-TK-0002-restore-actions-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.WingRestorationScreenTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
