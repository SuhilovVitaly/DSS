---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
ticket: EP-0043-US-0002-TK-0001-opening-phase-contract
title: Фаза пролога для запуска и Continue
stage: approved
layer: contracts
depends_on: [EP-0043-US-0001-TK-0001-dialogue-contract]
files_touched: 2
serves: [AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Фаза пролога для запуска и Continue

## Why

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [История](../EP-0043-US-0002-ambush-opening.md).

## Decisions

D-01 и D-02. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-01. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:11` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0002Tk0001Tests.cs` | Новый файл в DeepSpaceSaga.Contracts.Tests. | Только именованные тесты тикета. |

## Public API after the change

Проектируемые члены и значения определены в разделе Implementation steps.

## Implementation steps

Добавить optional init свойство Opening типа DemoOpeningSnapshot? в DemoCampaignSnapshot. Проектируемый snapshot содержит Phase и BattleId; этапы Intro, ReadyForAmbush, Ambush, Loot, Interrogation, FuelBriefingPending, Guidance. Раскрывать только фазу и фактический ID принятого боя. Клиент использует ReadyForAmbush для восстановления ожидающего запуска.

## Out of scope

Новая тактика, баланс, постановка боя и изменение обычного профиля. Любые изменения вне allowlist.

## Invariants

Ровно один запуск боя на принятую команду. Точка интеграции — `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:11`.

## Tests

- AC-02 — `LegacyCampaignHasNoOpeningPhase`.
- AC-03 — `OpeningPhaseProjectsOnlyPublicState`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0002Tk0001Tests.LegacyCampaignHasNoOpeningPhase|FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0002Tk0001Tests.OpeningPhaseProjectsOnlyPublicState' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DemoCampaign.cs
dotnet format whitespace tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0002Tk0001Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
