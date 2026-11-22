---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0001-dialogue-context
ticket: EP-0043-US-0001-TK-0003-dialogue-layout
title: Портреты и две читаемые реплики
stage: approved
layer: client
depends_on: [EP-0043-US-0001-TK-0002-speaker-projection]
files_touched: 3
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Портреты и две читаемые реплики

## Why

D-01. Вступление, допрос и доклад инженера используют одно представление реплик. [История](../EP-0043-US-0001-dialogue-context.md).

## Decisions

D-01. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-03. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs:147` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueLayout.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueLayout.cs:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0001Tk0003Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Отобразить текущий и предыдущий текст с их собственными портретами, именами и параметрами локализации. Выделить текущую реплику. Для первой реплики сохранить портрет капитана как адресата там, где экран уже это делает. Обновлять кэш по ключам персонажа и ресурса, обеспечить полный доступ к обеим репликам при прокрутке и очистку при смене экземпляра.

## Out of scope

Озвучка и журнал полного диалога. Любые изменения вне allowlist.

## Invariants

Стабильный порядок реплик и изоляция сцен. Точка интеграции — `src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs:147`.

## Tests

- AC-01 — `TwoUtterancesUseIndependentPortraits`.
- AC-02 — `LongLocalizedUtterancesRemainReadable`.
- AC-03 — `LongLocalizedUtterancesRemainReadable`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0001Tk0003Tests.TwoUtterancesUseIndependentPortraits|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0001Tk0003Tests.LongLocalizedUtterancesRemainReadable' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueLayout.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0001Tk0003Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
