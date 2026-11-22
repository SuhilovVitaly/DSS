---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0003-mandatory-capture
ticket: EP-0043-US-0003-TK-0001-capture-projection
title: Контракт обязательного захвата
stage: approved
layer: contracts
depends_on: [EP-0043-US-0002-TK-0003-opening-state]
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Контракт обязательного захвата

## Why

D-03 и D-04. Допрос требует хотя бы одного реально взятого на борт участника засады. [История](../EP-0043-US-0003-mandatory-capture.md).

## Decisions

D-03 и D-04. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs:24` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0003Tk0001Tests.cs` | Новый файл в DeepSpaceSaga.Contracts.Tests. | Только именованные тесты тикета. |

## Public API after the change

Проектируемые члены и значения определены в разделе Implementation steps.

## Implementation steps

Добавить к LootSessionSnapshot optional init bool RequiresPrisonerBeforeExit со значением false по умолчанию и BattleLootReasons.PrisonerRequired = loot_prisoner_required. Сохранить текущий контракт ArrestPilotCommand и идентичность источника. Добавить публичную константу для нового вида источника EscapePod, если он нужен UI.

## Out of scope

Переработка общей добычи, вместимости и сценариев поражения. Любые изменения вне allowlist.

## Invariants

Арест относится к одному бою и занимает обычную каюту ровно один раз. Точка интеграции — `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs:24`.

## Tests

- AC-01 — `LegacyLootDoesNotRequirePrisoner`.
- AC-02 — `MandatoryCaptureRoundTrips`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0003Tk0001Tests.LegacyLootDoesNotRequirePrisoner|FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0003Tk0001Tests.MandatoryCaptureRoundTrips' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/BattleLootContracts.cs
dotnet format whitespace tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0003Tk0001Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
