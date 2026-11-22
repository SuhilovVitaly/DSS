---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0001-dialogue-context
ticket: EP-0043-US-0001-TK-0001-dialogue-contract
title: Контекст предыдущей реплики
stage: approved
layer: contracts
depends_on: []
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Контекст предыдущей реплики

## Why

D-01. Вступление, допрос и доклад инженера используют одно представление реплик. [История](../EP-0043-US-0001-dialogue-context.md).

## Decisions

D-01. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-03. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DialogueState.cs` | `src/DeepSpaceSaga.Contracts/DialogueState.cs:5` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0001Tk0001Tests.cs` | Новый файл в DeepSpaceSaga.Contracts.Tests. | Только именованные тесты тикета. |

## Public API after the change

Проектируемые члены и значения определены в разделе Implementation steps.

## Implementation steps

Добавить в DialogueState необязательное init-свойство PreviousUtterance типа DialogueUtteranceSnapshot? со SpeakerRole, SpeakerDisplayName, SpeakerPortraitImage, TextKey и Parameters. Сохранить позиционный конструктор. Старый JSON без поля читается с null.

## Out of scope

Озвучка и журнал полного диалога. Любые изменения вне allowlist.

## Invariants

Стабильный порядок реплик и изоляция сцен. Точка интеграции — `src/DeepSpaceSaga.Contracts/DialogueState.cs:5`.

## Tests

- AC-01 — `OldDialogueJsonHasNoPreviousUtterance`.
- AC-02 — `RoundTripKeepsPreviousSpeakerAndText`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0001Tk0001Tests.OldDialogueJsonHasNoPreviousUtterance|FullyQualifiedName=DeepSpaceSaga.Contracts.Tests.DemoEp0043Us0001Tk0001Tests.RoundTripKeepsPreviousSpeakerAndText' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DialogueState.cs
dotnet format whitespace tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Contracts.Tests/DemoEp0043Us0001Tk0001Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
