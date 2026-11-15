---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0005-compact-panel
ticket: EP-0023-US-0005-TK-0002-remove-panel-controls
title: Удаление элементов выбранного дрона из правой панели
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0005-TK-0001-compact-panel]
files_touched: 13
serves: [US5-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Удаление элементов выбранного дрона из правой панели

## Why

После переноса старые элементы панели — мёртвый код, который тесты и скины продолжают видеть.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Статические функции текстов (`FireSummary`, `ThreatText`, `DecoyText`, `FireCaption`, `DustText`) остаются — ими пользуется карточка.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs` | :10-41 `DrawMove`, `BeginDrag`, `Drag`, `EndDrag` | Удалить; формула значения ползунка уже общая (US-0002). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | :9-55 список целей и прокрутка, `:92-98` `DrawDecoy`, `:100-167` `DrawFire` | Удалить отрисовку и прокрутку; оставить статические функции текстов. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :41-62 `BeginDrag`, действия `straight`, `ai`, `decoy`, `hold`, `evade`, `target:` кнопок панели | Удалить ветки панели (команды идут из карточки). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :316 `_phasePanel.ScrollTargets` | Удалить вызов. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/Ep0019Acceptance.cs` | пробы панели EP-0019 | Перевести пробы деталей выбранного дрона на карточку (UiSpec §10). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/AnomalySceneSearch.cs` | поиск сцен по элементам панели | Перевести на карточку. |

Дополнительно: PhasePanel.cs очищает декларации прежних блоков; FirePanelDecoyTests.cs, FirePanelEvasionTests.cs, AnomalyFirePanelTests.cs и MovementOrdersTests.cs переводят оставшиеся пробы на карточку, сохраняя проверки цветов, команд и карты.

Итоговая Client suite обнаружила два оставшихся теста с координатами прежней панели. PhaseMoveInputTests.cs и PhaseFireInputTests.cs перенесены на реальные зоны карточки и кнопку компактной панели с сохранением проверок отправляемых приказов.

## Public API after the change

`PhasePanel.ButtonBounds` — только `primary`, `replay`, `new`.

## Implementation steps

1. Удалить элементы выбранного дрона из `PhasePanel` и связанные ветки ввода.
2. Перевести оставшиеся ссылки в тестах на карточку.
3. Полный прогон клиентских тестов боя (кроме `Ep0019View`/`Ep0020View`, обновляемых в US-0007).

## Out of scope

Снимки вида (US-0007).

## Invariants

Поведение клавиатуры и кнопки действия не меняется.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TacticalBattle (весь каталог, кроме Category=Ep0019View|Ep0020View)`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattle&Category!=Ep0019View&Category!=Ep0020View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us5-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Удаляемые места перечислены построчно; сохраняемые функции названы в Assumptions.

## Исполнение

US5-AC3: прежние обработчики, отрисовка и прокрутка деталей справа удалены. Проверки карты, выбранной карточки, зарядов и недоступных команд переведены на рабочую геометрию. Build, 70/70 тестов, scoped format, diff check и самопроверка: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us5-tk2-03/. Снимки обновляются в US-0007.

Итоговая Client suite воспроизвела два устаревших теста ввода. Исправленные пробы сохраняют проверку минимальной скорости, возврата ИИ, допущенной цели, отказа от огня и отправки фазы. Build, 15/15 тестов, scoped format и diff check: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/phase-input-review/.
