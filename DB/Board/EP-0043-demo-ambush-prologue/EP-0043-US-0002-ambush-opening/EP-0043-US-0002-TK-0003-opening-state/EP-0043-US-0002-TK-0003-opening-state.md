---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
ticket: EP-0043-US-0002-TK-0003-opening-state
title: Состояние пролога и разрешение на бой
stage: approved
layer: engine
depends_on: [EP-0043-US-0002-TK-0002-opening-dialogue-data, EP-0043-US-0001-TK-0002-speaker-projection, EP-0043-US-0002-TK-0001-opening-phase-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Состояние пролога и разрешение на бой

## Why

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [История](../EP-0043-US-0002-ambush-opening.md).

## Decisions

D-01 и D-02. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-01. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.cs:516` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:111` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:147` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs:52` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0002Tk0003Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Ввести optional состояние пролога в данных кампании с публичной проекцией Opening (из существующего BuildDemoCampaignProjection) и этапами Intro, ReadyForAmbush, Ambush и устойчивым ID боя после принятия. Инициализировать только свежий сценарий demo в паузе после подготовки мира и загрузки диалогов. Принятый enter-battle переводит в ReadyForAmbush. Существующий StartTacticalBattle с собственным крылом, initial, seed 1 и RequireBattleResults проводит переход в Ambush только после принятия. Хранить готовность к старту при отказе, не разрешать обход вступления прямым стартом тактического запроса. Добавить optional свойство состояния через partial DemoCampaignData в том же файле кампании с блоковыми объявлениями пространств имён, используя текущий capture; null legacy пропускает пролог.

## Out of scope

Новая тактика, баланс, постановка боя и изменение обычного профиля. Любые изменения вне allowlist.

## Invariants

Ровно один запуск боя на принятую команду. Точка интеграции — `src/DeepSpaceSaga.Engine/SimulationEngine.cs:516`.

## Tests

- AC-01 — `NewDemoStartsPausedWithEngineer`.
- AC-02 — `OnlyFinalChoiceArmsSingleAmbush`.
- AC-03 — `LegacySaveDoesNotStartIntro`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0002Tk0003Tests.NewDemoStartsPausedWithEngineer|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0002Tk0003Tests.OnlyFinalChoiceArmsSingleAmbush|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0002Tk0003Tests.LegacySaveDoesNotStartIntro' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0002Tk0003Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
