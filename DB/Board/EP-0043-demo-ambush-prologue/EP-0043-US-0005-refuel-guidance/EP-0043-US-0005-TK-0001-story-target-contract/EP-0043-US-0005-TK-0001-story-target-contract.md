---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0005-refuel-guidance
ticket: EP-0043-US-0005-TK-0001-story-target-contract
title: Публичные цели и рекомендуемая станция
stage: approved
layer: contracts
depends_on: [EP-0043-US-0004-TK-0002-interrogation-state]
files_touched: 2
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Публичные цели и рекомендуемая станция

## Why

D-05 и D-06. Экипаж знает цель, но сначала игрок должен выбрать достижимую станцию. [История](../EP-0043-US-0005-refuel-guidance.md).

## Decisions

D-05 и D-06. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-04 и A-06. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:11` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0005Tk0001Tests.cs` | Новый файл в DeepSpaceSaga.Contracts.Tests. | Только именованные тесты тикета. |

## Public API after the change

Проектируемые члены и значения определены в разделе Implementation steps.

## Implementation steps

Расширить DemoStorySnapshot посредством optional init свойства OpeningInvestigation. Проектируемый вложенный snapshot содержит OutpostX/OutpostY в координатах существующей карты, Stage, RecommendedStationId, RecommendedStationName и идентификаторы локализуемых целей. Вне раскрытой сцены оставить null. Не выдавать скрытые данные об аванпосте и будущих NPC.

## Out of scope

Ввод новой экономики топлива и спасение пилотов. Любые изменения вне allowlist.

## Invariants

Отправление происходит только после команды игрока. Точка интеграции — `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:11`.

## Tests

- AC-01 — `HiddenOutpostDoesNotAppearInOldOrPreRevealProjection`.
- AC-02 — `RevealedTargetsRoundTrip`.
- AC-03 — `RevealedTargetsRoundTrip`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0005Tk0001Tests.HiddenOutpostDoesNotAppearInOldOrPreRevealProjection|FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0005Tk0001Tests.RevealedTargetsRoundTrip' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DemoCampaign.cs
dotnet format whitespace tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0005Tk0001Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
