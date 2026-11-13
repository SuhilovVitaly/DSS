---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0013-command-panel
ticket: EP-0024-US-0013-TK-0004-panel-salvo
title: Полёт и итог залпа в панели
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0013-TK-0001-panel-frame]
files_touched: 4
serves: [US13-AC4]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Полёт и итог залпа в панели

## Why

Во время залпа панель показывает «ОГОНЬ», очередь, цель и полёт торпеды; после — исход, урон, состояние цели, действие ловушки и состояние своего дрона (эталон `2-fire-results`).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-19.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardSalvoBlock.cs` | :9 высоты 16 / 30; :11-41 отрисовка | В режиме — UiSpec §11.9: полёт (высота 51) и итог (высота 71); команд нет. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | `Panel.FlightTitle`, `FlightQueue`, `FlightTarget`, `FlightHint`, `ResultLine`, `SelfState` (UiSpec §11.12). |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | То же. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardSalvoBlockTests.cs` | прежние проверки | Режим: тексты полёта и итога в `Layout.Texts` (проба §11.13.3), высоты. |

## Public API after the change

`internal static float SalvoHeight(bool inFlight)` (режим).

## Implementation steps

1. Раскладка полёта.
2. Раскладка итога.
3. Строки.
4. Тесты.

## Out of scope

Правые окна итога (US-0011).

## Invariants

В Classic блок прежний.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardSalvoBlockTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardSalvoBlockTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us13-tk4" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardSalvoBlock.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US13-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Раскладка — UiSpec §11.9, строки — §11.12.

## Исполнение

Не начато.
