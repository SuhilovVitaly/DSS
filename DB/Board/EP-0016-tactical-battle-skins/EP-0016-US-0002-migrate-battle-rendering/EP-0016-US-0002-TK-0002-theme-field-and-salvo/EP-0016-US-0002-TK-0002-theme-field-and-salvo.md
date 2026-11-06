---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0002-theme-field-and-salvo
title: Оформление поля и анимации залпа
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0002-TK-0001-theme-shared-primitives]
files_touched: 7
serves: [US2-AC1, US2-AC2, US2-AC3, US2-AC4, US2-AC5]
created: 2026-11-06T08:02:55Z
revision: 2
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
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | TacticalBattleScreen.cs:88–200. Render рисует поле, панель и три флажка. | Передать скин, открыть scope до ветвления Render, оформить фон, панель и разделитель, привязать роли к флажкам. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | TacticalBattleMapRenderer.cs:27–176, :178–215, :270–331, :349–507. Прогноз, залп, наведение, подписи и метки. | Применить роли поля, состояний и эффектов к существующему рендеру. |
| `src/DeepSpaceSaga.Client/UI/Controls/ProgressBar.cs` | ProgressBar.cs:19–25, :40. Общие заливка, трек и рамка. | Чтение ролей трека и рамки только в scope боя, сохранить общую ветку и API. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs` | BattleMapRenderTests.cs:56–302. Растры прогноза, наведения, состояния и меток. | Расширить проверками двух скинов по матрице US-0002. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | ResultsPhaseTests.cs:14, :69, :87. Текущий таймлайн, повтор и пропуск. | Сравнить пролёт, вспышку, промах, урон и допуск результатов в двух скинах. |
| `src/DeepSpaceSaga.Client/UI/GridRenderer.cs` | Общая геометрия вложенной сетки для карты мира и боя. | Добавить необязательное оформление цвета линий без дублирования геометрии. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | Чтение свойств стиля синхронного рендера. | Применение формы и штрихов к точкам и линиям карты. |

## Public API after the change

Сохранить public `TacticalBattleScreen(TacticalBattleFlow flow, float uiScale)` с Classic и добавить internal overload с `TacticalBattleSkin skin`. Один экран хранит один неизменяемый выбор. Внутренние `TacticalBattleMapRenderer.Own/Enemy/Inactive/Jammed` сохранить как совместимый штатный API для старых проверок до обновления всех потребителей. Зафиксировать адреса ролей поля и залпа в карте US-0002.

## Implementation steps

1. Открыть область стилей до CaptureFrame и держать её до конца Render, включая base.Render, подтверждение, подсказки и всплывающий список. Передать роли фона поля и панели, включая ресурс фона. Рендер не читает диск.
2. Перенести роли карты по матрице US-0002, включая точку прогноза, кормовой сектор, стрелки вариантов и назначенной цели, шансы и счётчики обеих сторон, метки за границей карты, медицинский плюс и именной обломок. Сохранить расчёт зон, направлений стрелок, доступных целей, геометрию ввода и действие флажков. При общем использовании DrawTargetArrow применять роль по месту вызова для независимого оформления наведения и меток за краем.
3. Перенести вид залпа из DrawSalvo по ролям shot.projectile, shot.trail, shot.flash, shot.caption и shot.damage. Рисовать краткие метки, как в текущем залпе. Сравнить кадры до и при фатальном прилёте по `LethalImpact`, включая повтор. Сохранить отсутствие текстовой сводки урона на карте. Проверить разрешение torpedo.projectile и torpedo.trail тестовым пакетом, а текущие выстрелы рисовать по shot.
4. Обернуть каждый из трёх вызовов Toggle.Draw в `EnterRole` из TK-0001 с соответствующей ролью из карты. В TK-0003 Toggle подключит чтение `CurrentRole`. Добавить стиль общего ProgressBar в области боя и сохранить штатный путь остальных экранов. Сравнить Classic с исходными растрами и contrast на тех же сценах.

## Out of scope

TacticalBattleResults.cs, Motion, DTO, наведение, таймеры допуска результатов и введение нового типа оружия.

## Invariants

Координаты hit-testing и `TacticalBattleView.Project` определяются существующей геометрией. `VisibleUnits` меняет маркер в момент фатального прилёта, а `TacticalBattleResults.Capture` допускает сводные данные по завершении анимации. Сохраняются 1350 мс движения, 975 мс полёта, номинальный интервал 570 мс и предел 4500 мс. Повтор и пропуск сохраняют действующее поведение без повторной отправки команды.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleMapRenderTests.Reference_skin_changes_field_units_and_paths_on_the_same_snapshot`
- `BattleMapRenderTests.MarkerColorsFollowPlayerAndInactiveAndJammedStatesRenderDifferently`
- `BattleMapRenderTests.Torpedo_visual_roles_resolve_without_reclassifying_current_shots`
- `ResultsPhaseTests.Skin_changes_salvo_visuals_without_changing_arrival_or_replay`
- `ResultsPhaseTests.ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot`

Существующие регрессии US-0019 включены отдельным вызовом класса `BattleMapRenderTests` целиком (прогноз, секторы, счётчики, обломки, отсутствие сводки, стрелки и плюс).

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.BattleMapRenderTests|FullyQualifiedName~ResultsPhaseTests.TimelineCapsLongAutomaticSalvoAndUsesAlternatingMissOffsets" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk2" --logger "trx;LogFileName=field-regressions.trx"
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

## Исполнение

Поле, экран, оверлеи, боевые маркеры и залп используют выбранный immutable skin в одном Render. Сетка использует общий геометрический код. Роли затрагивают рисунок, размеры, ресурсы, подписи и линии; команды, данные боевых фактов и таймлайн сохранены.

- `dotnet test --no-restore --filter FullyQualifiedName~TacticalBattle`: 229/229 PASS, включая все существующие пробы карты, реальный залп и три новые проверки. Classic совпал побайтно в 23 общих PNG из исходного растра, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk2/classic-parity.json`. Растр Contrast: `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk2/contrast-field.png`.
- Production scoped whitespace и staged diff check PASS. Проверка whitespace тестов указала только существующие строки BattleMapRenderTests до добавленных тестов. Сборка и самопроверка пройдены.
- К US2-AC3: роли `field.move.forecast.endpoint`, `field.fire.sector.rear`, `field.fire.target.badge`, `field.fire.line.*`, `field.offscreen.*`, `field.label.disabled-cross` и `field.unit.drone.wreck` читаются в TacticalBattleMapRenderer. Поддерживаемые метрики: `arrow-length` для стрелки, `tick-size` для линейки, `tail-length` для следа, `flash-scale` для вспышки. US2-AC5 проверен на чтении ролей. Текущий снимок не имеет отдельной торпедной проекции.
- US2-AC1/2/4 покрыты для карты и скопа Render. Стили флажков, итога и остальной панели завершаются в следующих тикетах.
