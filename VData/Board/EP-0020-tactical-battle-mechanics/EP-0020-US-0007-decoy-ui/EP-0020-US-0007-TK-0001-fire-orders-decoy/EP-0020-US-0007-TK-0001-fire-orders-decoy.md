---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0007-decoy-ui
ticket: EP-0020-US-0007-TK-0001-fire-orders-decoy
title: Модель приказов: ловушка, клавиша D, заряды на карточке
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0006-TK-0004-session-decoy, EP-0020-US-0004-TK-0004-view-regression]
files_touched: 5
serves: [US7-AC1, US7-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Модель приказов: ловушка, клавиша D, заряды на карточке

## Why

Пуск ловушки — флаг приказа огня, независимый от цели и несовместимый с уклонением.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-20: клавиша D (физическая клавиша; на русской раскладке В). Состояние строки: 0 — нет зарядов, 1 — доступна, 2 — включена; при уклонении строка не показывается.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs` | после US-0004 TK-0001: `Evade`, `ToggleEvade`, `BuildTargets` | `Decoy` в строке плана; `ToggleDecoy(droneId)`; `ToggleEvade` снимает `Decoy`; `BuildTargets` отправляет `Decoy`; доступность по `DecoyCharges`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | клавиши фазы огня | D → `ToggleDecoy(selected)`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | после US-0004 TK-0001 | Признак «ловушка включена» для значка UiSpec §10. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | — | Переключение, несовместимость, запрос. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/KeyboardRoutingTests.cs` | — | Клавиша D. |

## Public API after the change

`TacticalBattleFireOrders.ToggleDecoy(string droneId)`; `TacticalFirePlanRow.Decoy`; `DecoyRowState`.

## Implementation steps

1. Расширить модель и запрос.
2. Клавиша D.
3. Признак для карточки.
4. Тесты.

## Out of scope

Отрисовка (TK-0002).

## Invariants

Без нажатия D запрос совпадает с M1.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `FirePlanningTests.Decoy_toggle_reaches_request_and_target_choice_keeps_it`
- `FirePlanningTests.Evade_clears_decoy_and_decoy_is_unavailable_while_evading`
- `FirePlanningTests.Decoy_is_unavailable_without_charges`
- `KeyboardRoutingTests.D_toggles_decoy_only_in_fire_phase`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePlanningTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~PilotCardModelTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us7-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC1, US7-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Файлы существуют после EP-0019 и US-0004; строки сверить.

## Исполнение

Не начато.
