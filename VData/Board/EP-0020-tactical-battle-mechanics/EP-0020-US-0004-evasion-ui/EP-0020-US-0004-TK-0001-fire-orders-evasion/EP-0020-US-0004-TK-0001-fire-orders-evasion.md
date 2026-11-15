---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0004-evasion-ui
ticket: EP-0020-US-0004-TK-0001-fire-orders-evasion
title: Модель приказов: уклонение, клавиша E, сознание на карточке
stage: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0002-TK-0004-session-evasion, EP-0019-tactical-battle-ui-redesign]
files_touched: 6
serves: [US4-AC1, US4-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Модель приказов: уклонение, клавиша E, сознание на карточке

## Why

Выбор уклонения — третий вариант приказа огня. Он должен попадать в запрос и в карточку пилота.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-04: клавиша E (физическая клавиша, на русской раскладке — У). Фаза стрельбы может открыться, когда у игрока нет целей (D-10): строки создаются для всех своих активных дронов, даже с пустым списком.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs` | :8-16 строка плана, :25-43 Observe, :59-78 Choose/ClearAll, :81 BuildTargets | `Evade` в строке; `ToggleEvade(droneId)`; `Choose(target)` снимает уклонение; доступность по сознанию и `Rules.Evasion`; `BuildTargets` отправляет `Evade`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :272-282 клавиши фазы огня | E → `ToggleEvade(selected)`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | EP-0019: сознание-заглушка 100 | Сознание из `Unit.Consciousness`; тон `Red` при < минимума; значок «Уклонение» (тон Evade) вместо цели. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | — | Переключение, снятие, недоступность, запрос. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardModelTests.cs` | `Mind_is_placeholder_100` | Заменить на `Mind_comes_from_snapshot`; значок уклонения. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/KeyboardRoutingTests.cs` | — | Клавиша E в фазе огня и вне её. |

## Public API after the change

`TacticalBattleFireOrders.ToggleEvade(string droneId)`; `TacticalFirePlanRow.Evade`.

## Implementation steps

1. Расширить модель и запрос.
2. Клавиша E.
3. Карточка: сознание и значок.
4. Тесты.

## Out of scope

Отрисовка панели и карты (TK-0002).

## Invariants

Без нажатия E запрос совпадает с EP-0019.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `FirePlanningTests.Evade_toggle_reaches_request_and_choosing_target_clears_it`
- `FirePlanningTests.Evade_is_unavailable_below_min_consciousness`
- `FirePlanningTests.Fire_phase_without_targets_allows_evade_or_hold`
- `PilotCardModelTests.Mind_comes_from_snapshot`
- `KeyboardRoutingTests.E_toggles_evade_only_in_fire_phase`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePlanningTests|FullyQualifiedName~PilotCardModelTests|FullyQualifiedName~KeyboardRoutingTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us4-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC1, US4-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Файлы EP-0019 существуют после его исполнения; строки сверить.

## Исполнение

2026-11-08: rows содержат Evade/CanEvade, E без repeat/modifiers переключает выбранного дрона только в Fire, цель/hold снимают флаг; запрос передаёт Evade. Сознание и max/min берутся из snapshot/rules, ниже min красное значение при зелёной полосе. Значок уклонения фиолетовый, строки RU/EN из UiSpec §6. 28/28 FirePlanning/PilotCardModel/KeyboardRouting PASS; Client build и scoped format/diff-check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us4-tk1/final/tests.trx. Исходное Outcome ожидание Веры73/−102/disabled заменено на измеренное267/−46/встрою из-за M1 Engine, по A-12; остальные literal-сцены сохранены. Self-review по pending/phase/выбору/ресурсу и отсутствию client-side вычисления шанса выполнен. US4-AC1 и модельная часть AC2 покрыты; панель/кольца далее. Baseline MSB9008 относится к отсутствующему архивному ModApi.

## Актуальный scope до реализации

Добавлены PilotCardColumn.cs (отдельный цвет значения сознания и вид значка уклонения), BattleDesign.cs (PurpleDark) и обе Locale JSON: ключи UiSpec §6 вводятся сразу, чтобы первый UI commit не показывал fallback-ключи. Model test старого Outcome seed1 актуализируется по измеренному новому результату M1, как разрешает A-12; полная приёмка снимков остаётся в TK-0004.
