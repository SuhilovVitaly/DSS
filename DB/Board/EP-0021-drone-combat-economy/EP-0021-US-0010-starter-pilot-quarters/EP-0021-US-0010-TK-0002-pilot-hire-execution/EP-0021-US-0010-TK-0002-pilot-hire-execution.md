---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0010-starter-pilot-quarters
ticket: EP-0021-US-0010-TK-0002-pilot-hire-execution
title: Найм с обязательной свободной каютой
stage: approved
layer: engine
depends_on: [EP-0021-US-0010-TK-0001-starter-pilot-offers, EP-0021-US-0009-TK-0004-shipyard-projection]
files_touched: 5
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Найм с обязательной свободной каютой

## Why

Часть [Найм пилотов и расселение по каютам](../EP-0021-US-0010-starter-pilot-quarters.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Shipyard.cs` | Поверхность, добавленная зависимостью EP-0021-US-0009-TK-0004-shipyard-projection. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CarrierPilots.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | LoadScenarioCore, BuildInstalledModuleProjection, BuildRuntimeModules, ApplyPendingCommands и TryStartNavigationCommand. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | GameStateData, SpaceObjectData, ShipCrewMemberData и ShipModuleData, строки 68–118, 186–323, 337–349 и 450–465. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/CarrierPilotHiringTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierPilotHiringTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Quote/Confirm HirePilot проверяет station offer, свободную исправную каюту {moduleId,index}, текущие visit/revision и Tokens. Списывает деньги и закрепляет pilotId за каютой атомарно, повтор возвращает receipt. Персонаж игрока не занимает пилотскую каюту. Публиковать Pilot DTO из Engine. Учесть пилотов в существующей логике рациона. На проверочном маршруте предусмотреть запас еды на первые три игровых дня.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `modules-livingquarters.json:4–34` содержит вариант на четыре каюты. `Data/TacticalBattle/pilots.json:2–24` задаёт пилотов. Hire сейчас представлен заглушкой.
- Четыре кандидата имеют характеристики из начального боя. Найм требует свободной каюты, визита и средств.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierPilotHiringTests.Ac01` покрывает **AC-01**. Четыре кандидата имеют характеристики из начального боя. Найм требует свободной каюты, визита и средств.
- `CarrierPilotHiringTests.Ac02` покрывает **AC-02**. Одна каюта на пилота. Повторы безопасны. Занятый жилой модуль нельзя демонтировать.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0010-TK-0002-pilot-hire-execution-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierPilotHiringTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
