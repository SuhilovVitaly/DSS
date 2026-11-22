---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0002-ambush-opening
ticket: EP-0043-US-0002-TK-0002-opening-dialogue-data
title: Два доклада в контенте демо
stage: approved
layer: content-data
depends_on: [EP-0043-US-0001-TK-0001-dialogue-contract]
files_touched: 4
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Два доклада в контенте демо

## Why

D-01 и D-02. Новый старт демо предваряет существующее столкновение. [История](../EP-0043-US-0002-ambush-opening.md).

## Decisions

D-01 и D-02. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-01. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Dialogues/demo-opening-ambush.json` | Новый файл. Аналог — src/DeepSpaceSaga.Client/Data/Dialogues/sensor-pirate-contact.json:1. | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/Data/Dialogues/sensor-pirate-contact.json` | `src/DeepSpaceSaga.Client/Data/Dialogues/sensor-pirate-contact.json:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` | `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0002Tk0002Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Существующий формат JSON. Новые ID, ключи и связи перечислены в разделе Implementation steps.

## Implementation steps

Создать dialogue.demo-opening-ambush с узлами engineer и scanner, canAbort=false, allowManualStart=false. В первом узле ключ Demo.Opening.BeaconLost и действие Dialogue.Continue, во втором Demo.Opening.ScannerAmbush и выбор enter-battle с Demo.Opening.ToBattle. Проверить реальные назначения и портреты стартового экипажа. Роль оператора определяется назначением на модуле сканера. Для инженера и капитана сейчас указан один путь к портрету. Подобрать для инженера уже имеющийся портрет персонажа с отличимым лицом, чтобы игрок различал собеседников. Существующий sensor-report JSON только источник формата.

## Out of scope

Новая тактика, баланс, постановка боя и изменение обычного профиля. Любые изменения вне allowlist.

## Invariants

Ровно один запуск боя на принятую команду. Схема диалога следует примеру в Code context.

## Tests

- AC-01 — `OpeningDialogueHasExactSequenceAndRoles`.
- AC-02 — `OpeningDialogueHasExactSequenceAndRoles`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0002Tk0002Tests.OpeningDialogueHasExactSequenceAndRoles' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
