---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0008-engine-only-start
ticket: EP-0021-US-0008-TK-0002-docked-start-bootstrap
title: Действительный визит и физический выход
stage: approved
layer: engine
depends_on: [EP-0021-US-0008-TK-0001-carrier-start-content, EP-0021-US-0009-TK-0003-shipyard-purchase]
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Действительный визит и физический выход

## Why

Часть [Новая игра на пустом носителе](../EP-0021-US-0008-engine-only-start.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioGroupPlacement.cs` | BindV2Start, строки 9–65. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/CarrierStartDockingTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierStartDockingTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Проверить V2 BindV2Start, привязку к орбите и реальный station quote на старте. На фикстуре визита с одним доступным мостиком купить его, пройти Undock/Approach/Dock и подтвердить новый visit. Если позиция или скорость докованного объекта при V2 расходится со станцией, скорректировать только BindV2Start.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `Scenarios/Default/scenario.json:10–11,29–198` задаёт 2 000 Tokens и оснащённый старт. `Scenarios/Docked/scenario.json:16–30` показывает действующий формат начальной стыковки.
- Обычная New Game запускается с единственным установленным двигателем, пустым крылом, без нанятых пилотов, с топливом и заданным бюджетом.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierStartDockingTests.Ac01` покрывает **AC-01**. Обычная New Game запускается с единственным установленным двигателем, пустым крылом, без нанятых пилотов, с топливом и заданным бюджетом.
- `CarrierStartDockingTests.Ac02` покрывает **AC-02**. Первый визит действителен сразу. После покупки мостика игрок может отплыть и состыковаться со второй станцией. На корпусе помещается весь комплект.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0008-TK-0002-docked-start-bootstrap-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierStartDockingTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
