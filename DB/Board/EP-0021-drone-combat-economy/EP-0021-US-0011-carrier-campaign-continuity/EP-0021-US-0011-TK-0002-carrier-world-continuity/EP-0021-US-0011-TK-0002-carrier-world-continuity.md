---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0011-carrier-campaign-continuity
ticket: EP-0021-US-0011-TK-0002-carrier-world-continuity
title: Крыло и квитанции в Save20
stage: approved
layer: engine
depends_on: [EP-0021-US-0011-TK-0001-carrier-save-identity, EP-0021-US-0005-TK-0002-settlement-engine, EP-0021-US-0002-TK-0004-nearby-outfit-route, EP-0021-US-0010-TK-0002-pilot-hire-execution, EP-0021-US-0003-TK-0002-fit-wing]
files_touched: 5
serves: [AC-01, AC-03]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Крыло и квитанции в Save20

## Why

Часть [Сохранение крыла, верфи и кампании](../EP-0021-US-0011-carrier-campaign-continuity.md). Приёмка этой части покрывает AC-01, AC-03 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | GameStateData, SpaceObjectData, ShipCrewMemberData и ShipModuleData, строки 68–118, 186–323, 337–349 и 450–465. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | LoadScenarioCore, BuildInstalledModuleProjection, BuildRuntimeModules, ApplyPendingCommands и TryStartNavigationCommand. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CarrierContinuation.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs` | Поверхность, добавленная зависимостью EP-0021-US-0005-TK-0002-settlement-engine. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/CarrierWorldContinuationTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierWorldContinuationTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить optional CarrierCampaign в GameStateData с origin marker, starter route, wing records, pilot↔cabin↔drone, statuses, basis и завершёнными service/sortie receipts. Capture/Load проверяют ссылки, уникальность, вместимость, hull/charges и отсутствие активного боя. Старые слоты получают пустое состояние. Использовать существующий запрет Save в бою. Валидировать связь с сохранённой базой пилотов, с реальной схемой hull и start station IDs; допускать отсутствие кампании у исторических миров.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.cs:229–266,1084–1212,1648–1657` проверяет сохранение и совместимость. `Scenario/SaveMigration20.cs:8–26` преобразует исторические версии.
- Слот сохраняет модули, крыло, каюты, назначения, остатки стартовых предложений, hull/charges и receipts. Некорректные ссылки отклоняются.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierWorldContinuationTests.Ac01` покрывает **AC-01**. Слот сохраняет модули, крыло, каюты, назначения, остатки стартовых предложений, hull/charges и receipts. Некорректные ссылки отклоняются.
- `CarrierWorldContinuationTests.Ac03` покрывает **AC-03**. Активная сессия боя временная, итог сохраняется после выхода.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0011-TK-0002-carrier-world-continuity-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierWorldContinuationTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
