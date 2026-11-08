---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0004-evasion-ui
ticket: EP-0020-US-0004-TK-0002-fire-panel-evasion
title: Панель стрельбы, кольцо уклонения, строки
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0004-TK-0001-fire-orders-evasion]
files_touched: 6
serves: [US4-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Панель стрельбы, кольцо уклонения, строки

## Why

Вид выбора уклонения задан UiSpec и переносится буквально: координаты, цвета, размеры шрифтов, тексты.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Токен `PurpleDark #2A2145` и примитив `EvadeButton` добавляются в `BattleDesign`. Строки RU/EN — UiSpec §6.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | EP-0019: деталь огня, кнопка «Не стрелять» на всю ширину | Три состояния UiSpec §3; две половинные кнопки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | EP-0019: токены и примитивы | `PurpleDark`, `EvadeButton(state)` (§2), фиолетовый вариант `Tag` (§3.2); заголовок зоны действия с `FitSize` (§3.4). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | EP-0019: маркеры юнитов | Кольцо UiSpec §5. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | Ключи UiSpec §6. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | Ключи UiSpec §6. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePanelEvasionTests.cs` | нет | Растровые пробы UiSpec §7 и тексты. |

## Public API after the change

`BattleDesign.EvadeButton(SKCanvas, SKRect, string, EvadeButtonState)`.

## Implementation steps

1. Реализовать `EvadeButton`, фиолетовую плашку и `PurpleDark`.
2. Три состояния панели по таблицам UiSpec §3.
3. Кольцо на карте для своих дронов с планом уклонения.
4. Итог действий §3.4.
5. Строки RU/EN; `LocalizationCoverageTests`.
6. Растровые пробы UiSpec §7 для сцен «план уклонения» и «нет целей».

## Out of scope

Результаты залпа (TK-0003).

## Invariants

Состояние «план выстрела» отличается от EP-0019 только строкой кнопок и, при уклонениях, текстом итога.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `FirePanelEvasionTests.Evade_plan_panel_matches_ui_spec`
- `FirePanelEvasionTests.No_target_panel_matches_ui_spec`
- `FirePanelEvasionTests.Evade_button_states`
- `LocalizationCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us4-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Единственный источник вида — UiSpec §1–6.

## Исполнение

Не начато.
