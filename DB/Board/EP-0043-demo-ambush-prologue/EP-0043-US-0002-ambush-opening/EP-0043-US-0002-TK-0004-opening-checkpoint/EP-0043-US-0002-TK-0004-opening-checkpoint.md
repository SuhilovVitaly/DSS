---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
ticket: EP-0043-US-0002-TK-0004-opening-checkpoint
title: Восстановление начатого пролога
stage: approved
layer: local-client
depends_on: [EP-0043-US-0002-TK-0003-opening-state]
files_touched: 2
serves: [AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Восстановление начатого пролога

## Why

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [История](../EP-0043-US-0002-ambush-opening.md).

## Decisions

D-01 и D-02. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-01. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:232` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0002Tk0004Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Провести пролог через существующую политику checkpoint демо. Сохранить предбоевой снимок ReadyForAmbush до выдачи необратимого перехода. Для первого состояния и шага между репликами использовать действующую запись полного состояния диалога. При незавершённом бое восстановить его по существующему правилу предбоевого checkpoint. После ошибки записи сохранить повтор с той же identity. Не создавать отдельные слоты, протокол либо сохранение тактических ходов.

## Out of scope

Новая тактика, баланс, постановка боя и изменение обычного профиля. Любые изменения вне allowlist.

## Invariants

Ровно один запуск боя на принятую команду. Точка интеграции — `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:232`.

## Tests

- AC-03 — `ContinueRestoresCorrectOpeningPhase` и `FailedCheckpointCanBeRetriedWithoutSecondBattle`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0004Tests.ContinueRestoresCorrectOpeningPhase|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0004Tests.FailedCheckpointCanBeRetriedWithoutSecondBattle' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0002Tk0004Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
