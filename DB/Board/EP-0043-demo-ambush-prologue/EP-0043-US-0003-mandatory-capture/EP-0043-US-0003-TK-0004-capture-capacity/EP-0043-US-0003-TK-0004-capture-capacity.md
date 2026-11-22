---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0003-mandatory-capture
ticket: EP-0043-US-0003-TK-0004-capture-capacity
title: Стартовая свободная каюта
stage: approved
layer: content-data
depends_on: [EP-0043-US-0003-TK-0001-capture-projection]
files_touched: 2
serves: [AC-01]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Стартовая свободная каюта

## Why

D-03 и D-04. Допрос требует хотя бы одного реально взятого на борт участника засады. [История](../EP-0043-US-0003-mandatory-capture.md).

## Decisions

D-03 и D-04. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` | `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:1` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0003Tk0004Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Существующий формат JSON. Новые ID, ключи и связи перечислены в разделе Implementation steps.

## Implementation steps

Проверить стартовый состав, пассажиров и повреждения кают через реальную проекцию. Зафиксировать минимум одну свободную каюту к штатному первому бою через стартовые данные только при необходимости. Не вводить особый резерв и не менять правила ротации экипажа или вместимости обычных кораблей.

## Out of scope

Переработка общей добычи, вместимости и сценариев поражения. Любые изменения вне allowlist.

## Invariants

Арест относится к одному бою и занимает обычную каюту ровно один раз. Точка интеграции — `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:1`.

## Tests

- AC-01 — `FreshDemoOpeningHasAtLeastOneFreeCabin`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0003Tk0004Tests.FreshDemoOpeningHasAtLeastOneFreeCabin' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
