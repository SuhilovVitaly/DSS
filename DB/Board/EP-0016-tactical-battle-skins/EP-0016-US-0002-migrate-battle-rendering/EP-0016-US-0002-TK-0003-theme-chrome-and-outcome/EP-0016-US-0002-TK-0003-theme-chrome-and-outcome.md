---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0003-theme-chrome-and-outcome
title: Оформление панелей, окон и результата боя
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0002-TK-0002-theme-field-and-salvo]
files_touched: 5
serves: [US2-AC1, US2-AC2, US2-AC4]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Оформление панелей, окон и результата боя

## Why

Дизайн боя включает итог, состояния ожидания, ошибки, кнопки и подтверждение выхода.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0002-migrate-battle-rendering.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-01/A-07. Общий базовый modal используется также при подготовке, область стилей включается только экраном активного боя.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | TacticalBattleFlow.cs:86, :147–204. Базовый рендер ожидания и подтверждение выхода используют общие стили. | Покрыть фон, текст, ошибки, затемнение и рамку модального подтверждения при активном scope. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs` | TacticalBattlePanelHeader.cs:13, :33, :40, :62–94. Фазы, потери, исход и сообщения. | Покрыть фазы, состояния и итог, сохранив Capture API и вычисление отображаемых фактов. |
| `src/DeepSpaceSaga.Client/UI/Controls/ShortcutButton.cs` | ShortcutButton.cs:37–48. Кнопки используют GenericButtonTypeA и ControlText.Tooltip. | В боевой области дать стилю заменить вид всех состояний и маркера. При отсутствии override оставить исходное оформление. |
| `src/DeepSpaceSaga.Client/UI/Controls/Toggle.cs` | Toggle.cs:34–44. Текущие рамка и цвет галочки зависят от enabled. | Роли рамки, галочки, подписи и состояния. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PanelHeaderTests.cs` | PanelHeaderTests.cs:14 и :63. Проверяет допуск потерь и причину отказа. | Растры панели, результата и подтверждения в двух скинах, проверка отсутствия эффекта в подготовке. |

## Public API after the change

No public API change. Рендер-область поставляет стили panel.phase, panel.outcome, modal и control.button/toggle согласно карте US-0002. Данные Capture и события остаются совместимыми.

## Implementation steps

1. Использовать существующие значения GenericWindowTypeA/GenericButtonTypeA для Classic. Добавить стилизованный путь при наличии override для рамки, фона/изображения, заголовка и текста, кнопок и маркеров. Сохранить расчёт Bounds и обработку нажатий. Подготовка использует прежнюю ветку.
2. Применить роли фаз, ожидания, ошибки и исходов с отдельными состояниями. Оформить полупрозрачное затемнение, подтверждение выхода, его кнопки, подсказки, переключатели и индикаторы фокуса/hover/disabled. Для виджетов без фазы pressed не вводить новое поведение.
3. Растровыми тестами показать отличия от Classic для итога и подтверждения, затем перерисовать подготовку тем же canvas без эффекта темы. Проверить, что правило скрытия потерь до прибытия сохраняется.

## Out of scope

Правила выхода и переходов, экран подготовки, поведение переиспользуемых контролов.

## Invariants

TacticalBattlePanelHeader.cs:17–42 управляет видимостью потерь и вычислением исхода. TacticalBattleFlow.cs:196–204 и существующие события задают назначение кнопок.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `PanelHeaderTests.Skin_styles_outcome_and_confirmation_without_styling_setup`
- `PanelHeaderTests.RealSalvoLossesStayHiddenUntilArrivalAndOutcomeUsesPlayerPerspective`
- `PanelHeaderTests.ActualEngineRefusalIsShownInPanelWithoutChangingPhaseOrSeed`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PanelHeaderTests.Skin_styles_outcome_and_confirmation_without_styling_setup|FullyQualifiedName~PanelHeaderTests.RealSalvoLossesStayHiddenUntilArrivalAndOutcomeUsesPlayerPerspective|FullyQualifiedName~PanelHeaderTests.ActualEngineRefusalIsShownInPanelWithoutChangingPhaseOrSeed" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs src/DeepSpaceSaga.Client/UI/Controls/ShortcutButton.cs src/DeepSpaceSaga.Client/UI/Controls/Toggle.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PanelHeaderTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US2-AC1, US2-AC2, US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Scope уже включён обычным экраном. Все точки оформления принадлежат указанным файлам.
