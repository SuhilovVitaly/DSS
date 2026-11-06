---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0002-theme-field-and-salvo
title: Оформление поля и анимации залпа
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0002-TK-0001-theme-shared-primitives]
files_touched: 5
serves: [US2-AC1, US2-AC2, US2-AC3, US2-AC4, US2-AC5]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Оформление поля и анимации залпа

## Why

Поле и анимация залпа должны принимать один активный скин через обычный экран боя.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0002-migrate-battle-rendering.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-07/A-08. Размеры рисунков, обводка, фон, прозрачность и fade визуальны. Геометрия и время доступности боевых фактов берутся из действующих данных.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | TacticalBattleScreen.cs:55, :86, :110–209. Render выбирает фазу, рисует поле, панель и оверлеи. | Передать выбранный скин в экран, открыть scope до ветвления Render, оформить фон, панель, разделитель и сводку. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | TacticalBattleMapRenderer.cs:27, :67, :120, :158, :199, :220, :250, :306, :380. | Перевести все карты, силуэты, прогнозы, маркеры и эффекты на визуальные роли. |
| `src/DeepSpaceSaga.Client/UI/Controls/ProgressBar.cs` | ProgressBar.cs:19–25, :40. Общие заливка, трек и рамка. | Чтение ролей трека и рамки только в scope боя, сохранить общую ветку и API. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs` | BattleMapRenderTests.cs:14 и :56. Реальные растры по сторонам, сетке и переключателям. | Добавить контрастные probes и проверку не только палитры, но и рисунка/размеров. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | ResultsPhaseTests.cs:14, :66, :84. Залп, повтор и задержка проверяются на текущей сессии. | Добавить raster-проверку пролёта, вспышки, промаха, урона и смены статуса. |

## Public API after the change

Сохранить public `TacticalBattleScreen(TacticalBattleFlow flow, float uiScale)` с Classic и добавить internal overload с `TacticalBattleSkin skin`. Один экран хранит один неизменяемый выбор. Внутренние `TacticalBattleMapRenderer.Own/Enemy/Inactive/Jammed` сохранить как совместимый штатный API для старых проверок до обновления всех потребителей. Зафиксировать адреса ролей поля и залпа в карте US-0002.

## Implementation steps

1. Открыть область стилей до CaptureFrame и держать её до конца Render, включая base.Render, подтверждение, подсказки и всплывающий список. Передать роли фона поля и панели, включая ресурс фона. Рендер не читает диск.
2. Перенести оформление сетки и линейки, кораблей и дронов по сторонам/состояниям, векторов, следов, зон, подписей и индикаторов. Оставить шкалу, мировые координаты, параметры расчёта зоны и точки выбора в текущей геометрии. Показывать центр/курс читаемо для альтернативных силуэтов.
3. Перенести вид залпа из DrawSalvo по ролям shot.projectile, shot.trail, shot.flash, shot.caption и shot.damage. Объявить torpedo.projectile и torpedo.trail как родственные роли схемы и проверить конфигурируемость в каталоге или тестовом пакете. Использовать shot для текущих выстрелов, а torpedo оставить для подтверждённого типа. Траектория визуализирует данные, время прихода и эффекты допуска берутся из текущей фазы.
4. Добавить стиль общего ProgressBar только в области боя и сохранить ветку без скина. Сравнить Classic и контрастный пакет по одинаковому снимку, с явными probes масштаба, цвета, PNG и формы. Проверить, что показ и повтор не добавляют выстрел.

## Out of scope

TacticalBattleResults.cs, Motion, DTO, наведение, таймеры допуска результатов и введение нового типа оружия.

## Invariants

Линия не меняет TacticalBattleView.Project и координаты hit-testing (TacticalBattleScreen.cs:310–430). Фаза и попадание приходят из TacticalBattleResults.cs:8–13, :29–64. Прежняя сетка и индикация сторон воспроизводятся Classic.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleMapRenderTests.Reference_skin_changes_field_units_and_paths_on_the_same_snapshot`
- `BattleMapRenderTests.MarkerColorsFollowPlayerAndInactiveAndJammedStatesRenderDifferently`
- `BattleMapRenderTests.Torpedo_visual_roles_resolve_without_reclassifying_current_shots`
- `ResultsPhaseTests.Skin_changes_salvo_visuals_without_changing_arrival_or_replay`
- `ResultsPhaseTests.ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleMapRenderTests.Reference_skin_changes_field_units_and_paths_on_the_same_snapshot|FullyQualifiedName~BattleMapRenderTests.MarkerColorsFollowPlayerAndInactiveAndJammedStatesRenderDifferently|FullyQualifiedName~BattleMapRenderTests.Torpedo_visual_roles_resolve_without_reclassifying_current_shots|FullyQualifiedName~ResultsPhaseTests.Skin_changes_salvo_visuals_without_changing_arrival_or_replay|FullyQualifiedName~ResultsPhaseTests.ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Controls/ProgressBar.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US2-AC1, US2-AC2, US2-AC3, US2-AC4, US2-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Точка входа и все визуальные методы указаны. Зависимость предоставляет scope и manifest; данные боя используются как раньше.
