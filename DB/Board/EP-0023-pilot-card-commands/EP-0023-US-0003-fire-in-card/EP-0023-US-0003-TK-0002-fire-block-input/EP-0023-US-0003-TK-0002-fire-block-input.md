---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0003-fire-in-card
ticket: EP-0023-US-0003-TK-0002-fire-block-input
title: Блок стрельбы: цели, ловушка, «Не стрелять», уклонение и прокрутка
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0003-TK-0001-fire-block-render]
files_touched: 8
serves: [US3-AC2, US3-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Блок стрельбы: цели, ловушка, «Не стрелять», уклонение и прокрутка

## Why

Команды стрельбы из карточки должны давать тот же план, что и правая панель, и существовать только у выбранного дрона.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-07, A-10.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs` | регистрация команд (TK-0001) | Прокрутка списка целей: первая видимая строка, сброс при смене группы целей (как `PhasePanel.Fire.cs:18-29`). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :313-317 колесо | Колесо над списком целей карточки прокручивает его (раньше прокрутки колонки). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePanelDecoyTests.cs` | кнопка ловушки панели через `ButtonBounds("decoy")` | Перевести сценарии на `CardCommandBounds("decoy")`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePanelEvasionTests.cs` | кнопка уклонения панели | Перевести на `CardCommandBounds("evade")`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | выбор цели строками панели и «Не стрелять» | Перевести на `CardCommandBounds("target:<id>")`, `CardCommandBounds("hold")`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapFireTests.cs` | выбор цели на карте | Без изменения смысла; проверить, что карточка отражает выбор. |

Дополнительно: PilotCardColumn.cs связывает выбранный FirePlanRow с прокруткой; PilotCardFireBlockTests.cs проверяет смену группы и равенство запросов при вводе из карточки, с карты и с клавиатуры.

## Public API after the change

Без нового публичного API сверх US-0001-TK-0003.

## Implementation steps

1. Связать команды блока с `_fireOrders` (через маршрутизацию US-0001-TK-0003).
2. Прокрутка списка целей колесом.
3. Перевести тесты планирования стрельбы, ловушки и уклонения на команды карточки; тест: на невыбранных карточках `HitCommand` ничего не находит.
4. Тест равенства планов: одинаковые действия через карточку и через клавиши/карту дают одинаковые `BuildTargets()`.

## Out of scope

Удаление кнопок из правой панели (US-0005).

## Invariants

Блокировки ввода и выбор по умолчанию (`Observe`) не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `FirePanelDecoyTests`
- `FirePanelEvasionTests`
- `FirePlanningTests`
- `MapFireTests`
- `PilotCardCommandsInputTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePanelDecoyTests|FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~FirePlanningTests|FullyQualifiedName~MapFireTests|FullyQualifiedName~PilotCardCommandsInputTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us3-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC2, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Действия и их вызовы — в критериях US3-AC3; тестовый доступ — `CardCommandBounds`.

## Исполнение

US3-AC2/AC3: список целей прокручивается до колонки и сбрасывается при смене группы. Клики цели, ловушки, уклонения и отказа от огня проходят через существующие приказы; сравнение с картой и E/D дало одинаковые FireRequest.Targets. Недоступные команды не регистрируются. Build, 44/44 тестов, scoped format, diff check и самопроверка diff: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us3-tk2/. Снятие правых дубликатов следует в US-0005.
