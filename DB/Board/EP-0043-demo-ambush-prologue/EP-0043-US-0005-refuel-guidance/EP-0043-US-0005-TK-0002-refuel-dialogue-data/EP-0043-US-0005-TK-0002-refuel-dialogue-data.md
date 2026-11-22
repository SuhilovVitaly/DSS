---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0005-refuel-guidance
ticket: EP-0043-US-0005-TK-0002-refuel-dialogue-data
title: Содержание доклада инженера
stage: approved
layer: content-data
depends_on: [EP-0043-US-0005-TK-0001-story-target-contract]
files_touched: 4
serves: [AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Содержание доклада инженера

## Why

D-05 и D-06. Экипаж знает цель, но сначала игрок должен выбрать достижимую станцию. [История](../EP-0043-US-0005-refuel-guidance.md).

## Decisions

D-05 и D-06. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-04 и A-06. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Dialogues/demo-opening-refuel.json` | Новый файл. Аналог — src/DeepSpaceSaga.Client/Data/Dialogues/sensor-pirate-contact.json:1. | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json` | `src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json:6` | При необходимости изменить стартовую позицию, рыночный запас и стартовые средства в демо. |
| `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` | `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:129` | При необходимости скорректировать начальный остаток в существующем топливном баке. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0005Tk0002Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Существующий формат JSON. Новые ID, ключи и связи перечислены в разделе Implementation steps.

## Implementation steps

Определить однорепликовую диалоговую сцену dialogue.demo-opening-refuel, Engineer, Demo.Opening.RefuelBriefing, параметр stationName, Continue, canAbort=false, allowManualStart=false. До подключения сценарного вызова новый диалог не доступен вручную. Подготовить поставляемые стартовые данные для достижимости станции и доступности дозаправки. Сценарный тест следующего тикета подтверждает соотношение остатка, цены и двух плеч маршрута после генерации мира.

## Out of scope

Ввод новой экономики топлива и спасение пилотов. Любые изменения вне allowlist.

## Invariants

Отправление происходит только после команды игрока. Схема диалога следует примеру в Code context.

## Tests

- AC-02 — `EngineerBriefingUsesStationParameterAndOneAction`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0005Tk0002Tests.EngineerBriefingUsesStationParameterAndOneAction' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
