---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0005-theme-information-tabs
title: Оформление сторон, журнала и прогноза
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0002-TK-0004-theme-order-controls]
files_touched: 5
serves: [US2-AC1, US2-AC2, US2-AC4]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Оформление сторон, журнала и прогноза

## Why

Журнал, прогноз и стороны должны выглядеть частью того же дизайна. Этот тикет закрывает полноту миграции.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0002-migrate-battle-rendering.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-07/A-08. Общий ScrollTextPanel сохраняет прежнее поведение вне боя. Допущенные данные берутся из текущего снимка.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs` | TacticalBattleSidesTab.cs:27–95. Карточки, подписи, полосы и прокрутка. | Стили карточек и индикаторов, исключить зависимости активной палитры от старых констант карты. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs` | TacticalBattleLogTab.cs:27–37. Заполняет и рисует панель журнала. | Передать отдельные роли журнала. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleForecastTab.cs` | TacticalBattleForecastTab.cs:30–40. Заполняет и рисует панель прогноза. | Передать отдельные роли прогноза. |
| `src/DeepSpaceSaga.Client/UI/Controls/ScrollTextPanel.cs` | ScrollTextPanel.cs:50, :99–113. Перенос, текст, граница, трек и thumb. | Передать роли панели, использовать одни и те же метрики для wrap, Draw и прокрутки. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs` | SidesAndLogTests.cs:13, :38, :72 проверяет выбор и допуск событий. | Добавить видимые probes вкладок и проверить те же факты в обоих скинах. |

## Public API after the change

Сохранить действующий публичный API ScrollTextPanel, добавить internal переопределение роли для Draw/подготовки текста или internal свойство экземпляра для роли. Наружные вызовы без scope используют прежний стиль.

## Implementation steps

1. Заменить цвета рамок, текста, скролла, полос и индикаторов вкладки сторон и выделения ролями tabs.sides. Применить общее оформление ProgressBar из TK-0002.
2. Связать табы журнала и прогноза с разными ролями, включая стиль текста и прокрутки. Менять wrap при смене используемых метрик с восстановлением позиции по тому же содержимому. Проверить клавиатурный и колёсный скролл.
3. Пройти карту US-0002 по всем вызовам Render и состояниям и приложить таблицу «элемент → роль → снимок/тест» к свидетельству выполнения. Для пропуска в другом тикете вернуть исправление в его разрешённую область. Сопоставить Classic с исходным raster baseline и проверить альтернативный скин.
4. Подтвердить независимость доступности потерь, событий и прогнозов от темы. Зафиксировать различия по платформе и статус нативной проверки отдельно от raster.

## Out of scope

Новые данные прогноза, изменение локализации журнала, допуск скрытых событий и изменение статусов предыдущих тикетов без evidence.

## Invariants

SidesAndLogTests.cs:38 и TacticalBattleForecastTab.cs:30–40 покрывают фазовую доступность. Локализация и смысл карточек по-прежнему определяются боевыми данными.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SidesAndLogTests.Reference_skin_changes_sides_log_and_forecast_without_changing_visible_facts`
- `SidesAndLogTests.LogAndSideCardsDoNotRevealTheAdmittedSalvoUntilArrival`
- `SidesAndLogTests.OwnSideCardSelectsActualDroneWithoutEditingItsOrder`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SidesAndLogTests.Reference_skin_changes_sides_log_and_forecast_without_changing_visible_facts|FullyQualifiedName~SidesAndLogTests.LogAndSideCardsDoNotRevealTheAdmittedSalvoUntilArrival|FullyQualifiedName~SidesAndLogTests.OwnSideCardSelectsActualDroneWithoutEditingItsOrder" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk5" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleForecastTab.cs src/DeepSpaceSaga.Client/UI/Controls/ScrollTextPanel.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US2-AC1, US2-AC2, US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Файлы вкладок и их общий виджет перечислены. Матрица US-0002 и все предыдущие миграции задают проверяемый охват.

## Исполнение

Стороны, журнал и прогноз читают отдельные роли. Карточки сохраняют штатные интерактивные области. В журнале и прогнозе перенос, шаг строк и прокрутка используют фактический шрифт. При изменении метрик сохраняется относительная позиция прокрутки или автоматическое следование за концом.

| Поверхность | Роли и потребитель | Проверка |
|---|---|---|
| Поле, сетка, линейка, единицы, подписи, полосы, края и обломки | `field.background/grid/ruler/unit/label/bar/offscreen` в TacticalBattleMapRenderer | BattleMapRenderTests, contrast-field.png и исходные растры |
| Движение, прогноз, секторы и наведение | `field.move.*`, `field.fire.*` в TacticalBattleMapRenderer | BattleMapRenderTests, MovementOrdersTests и FirePlanningTests |
| Полёт, след, краткие подписи, попадание и X | `shot.*` в TacticalBattleMapRenderer, `torpedo.*` зарезервированы | ResultsPhaseTests, BattleMapRenderTests |
| Панель, фазы, ожидание, ошибка, итог и подтверждение | `panel.*`, `modal.*` в TacticalBattleScreen, TacticalBattlePanelHeader и TacticalBattleModalScreen | PanelHeaderTests и BattleEndTests |
| Флажки, общие контролы, подсказки | `control.*` в общих виджетах под областью TacticalBattleScreen.Render | WidgetsListsTests, WidgetsBasicTests, PanelHeaderTests |
| Приказы | `orders.move.*`, `orders.fire.*` в TacticalBattleMovePanel и TacticalBattleFirePanel | MovementOrdersTests и FirePlanningTests |
| Информационные вкладки | `tabs.sides.*`, `tabs.log.*`, `tabs.forecast.*` в трёх вкладках и ScrollTextPanel | SidesAndLogTests, ForecastTabTests, contrast-sides/log/forecast PNG |

Сборка, scoped whitespace, staged diff check и самопроверка прошли. `dotnet test --no-restore --filter FullyQualifiedName~TacticalBattle`: 234/234 PASS. 23/23 общих PNG Classic совпали с исходными. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk5/focused.trx`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk5/classic-parity.json`. Снимки Contrast для вкладок подтверждают разные стили при одинаковых данных и выборе. Проверки допущенных фактов и выхода через реальные команды прошли. Нативный прогон и итоговая приёмка вынесены в US-0003 и финальную проверку.
