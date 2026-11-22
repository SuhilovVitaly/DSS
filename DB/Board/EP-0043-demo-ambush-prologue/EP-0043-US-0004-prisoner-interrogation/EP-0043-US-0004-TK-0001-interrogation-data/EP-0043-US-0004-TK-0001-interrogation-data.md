---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0004-prisoner-interrogation
ticket: EP-0043-US-0004-TK-0001-interrogation-data
title: Последовательность реплик допроса
stage: approved
layer: content-data
depends_on: [EP-0043-US-0003-TK-0003-capture-exit-gate]
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Последовательность реплик допроса

## Why

D-03 и D-05. Захваченный пират раскрывает происхождение маяка и судьбу пилотов. [История](../EP-0043-US-0004-prisoner-interrogation.md).

## Decisions

D-03 и D-05. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02 и A-05. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Dialogues/demo-opening-interrogation.json` | Новый файл. Аналог — src/DeepSpaceSaga.Client/Data/Dialogues/sensor-pirate-contact.json:1. | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0004Tk0001Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Существующий формат JSON. Новые ID, ключи и связи перечислены в разделе Implementation steps.

## Implementation steps

Создать обязательный на карте диалог с ролями Captain и Prisoner, canAbort=false и allowManualStart=false. Последовательность ключей Demo.Opening.InterrogateBeacon, BeaconDecoy, AskPilots, PrisonersAtOutpost (с префиксом Demo.Opening для каждого ключа). Последний узел даёт выбор reveal-outpost с ключом Demo.Opening.ShowOutpost. Подтверждение завершает допрос и открывает показ координат.

## Out of scope

Общие механики допроса и взаимодействия с пленными. Любые изменения вне allowlist.

## Invariants

Отметка аванпоста появляется только после раскрытия игроку. Схема диалога следует примеру в Code context.

## Tests

- AC-01 — `InterrogationAlternatesCaptainAndPrisoner`.
- AC-02 — `InterrogationAlternatesCaptainAndPrisoner`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0004Tk0001Tests.InterrogationAlternatesCaptainAndPrisoner' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
