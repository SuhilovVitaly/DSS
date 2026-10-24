# EP-0013 — инвентаризация документации, 2026-10-22

Основание: итоговый diff EP-0013, исходная revision2 спецификация и раздел6
[промта исполнения](../EpicExecutionPrompt.md). Все исходные90 Markdown были
прочитаны до реализации; после параллельной курации полностью перечитаны55
текущих документов, выбранные15 исходников и финальные изменения. Полный контракт
не подменяется кратким backend-обзором другого процесса.

| Область | Итоговое действие и текущий источник |
|---|---|
| Требования Engine | Нормативное дополнение [EngineRequirements](../../Documentation/01-Requirements/EngineRequirements.md): overlay, retiming, Save19/18, независимые оси |
| Границы релиза | [FirstReleaseRequirements](../../Documentation/01-Requirements/FirstReleaseRequirements.md): реализованные UI/файлы/controls отдельно от приёмки |
| Текущая механика | Полные новые [DebugTuningPanel](../../Documentation/04-Engineering/DebugTuningPanel.md) и [ManualManeuver](../../Documentation/04-Engineering/ManualManeuver.md), дополнение [PointAimCombat](../../Documentation/04-Engineering/PointAimCombat.md) |
| Карта/панели | [TacticalMapAndManeuvering](../../Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md), [CommandPanels](../../Documentation/02-FirstRelease/Mechanics/CommandPanels.md), [TacticalMapSpecification](../../Documentation/03-Design/TacticalMapSpecification.md): текущие назначения клавиш, захват мыши, cached UI |
| Экраны/продолжение | [GameSession](../../Documentation/02-FirstRelease/Screens/GameSession.md), [Save](../../Documentation/02-FirstRelease/Screens/Save.md), [Load](../../Documentation/02-FirstRelease/Screens/Load.md): порядок bootstrap, writer lifecycle, сброс ввода |
| Архитектура/инструкции | [CLAUDE](../../Documentation/00-Process/CLAUDE.md), [GameSessionScreenUI](../../Documentation/06-Tooling/GameSessionScreenUI.md): ответственность Contracts/Motion/Engine/Client, render/file границы |
| Навигация | [Documentation README](../../Documentation/README.md) с текущими контрактами, новый [GraphifyRebuild](../../Documentation/06-Tooling/GraphifyRebuild.md) с воспроизводимым workflow |
| Вне Documentation | Новый [Performance/EP0013](../../../tools/DeepSpaceSaga.Performance/EP0013.md), актуальный builder, комментарий PlayerCommand.Active для обеих осей и актуализированные integration assertions |
| Связанный Board | [EP12 Documentation](../EP-0012-combat-rebuild/Documentation.md): явная замена keyboard/live overlay частей, исторические результаты сохранены |
| Целевой Board | Контракт, индекс, семь историй,21 карточка, зависимости, [coverage](AcceptanceCoverage.md), [execution](ExecutionStatus.md), [publication](PublicationStatus.md) и финальные evidence |
| Остальные документы | Мировая генерация, экономика, trading, surveys, portrait pipeline, assets и старые эпики не меняют контракт из-за EP13; датированные результаты сохраняются как история |

## Параллельные изменения

В рабочем дереве одновременно идёт чужая курация Documentation, process/AGENTS,
старых Board и tool README. Она сохранена. В общих13 tracked документах EP13
добавляет только явно обозначенное нормативное дополнение поверх HEAD, а index
не включает чужую переписанную основную часть. В rebuild.py staged только EP13
изменения поверх HEAD; чужой выбор текущего report-spec сохранён в рабочем дереве.
Состав и хеши дополнений: `D:/DeepSpaceSaga/artifacts/validation/ep0013/documentation-notice-audit.json`.

Чужие новые `DebugTuning.md`, `SaveFormat.md`, `Graphify.md` не присваиваются
этому эпику и не публикуются в его коммите. Ограниченный backend-обзор
`DebugTuning.md` не является полной спецификацией UI; актуальные полные контракты
даны выше. Устаревшая строка текущей release-таблицы исправлена адресно.
Старые недатированные keyboard/writer описания в публикуемой HEAD-версии явно
помечены заменёнными новым нормативным дополнением. Старые run counts не
переписываются как якобы полученные новым кодом.

Graphify описывает фактически прочитанное рабочее дерево, включая параллельную
курацию. Его хеши не выдают это дерево за чистый опубликованный checkout.
Свежие semantic fragments не являются независимым review или runtime evidence.

## Источник истины и навигация

Текущие требования — EngineRequirements и два новых полных инженерных контракта;
точные37 исходных AC — [SourceSpecification](SourceSpecification.md), с явным
исправлением общего числа параметров29→25. US5-TK5 сохранён как алиас US7-TK1.
Семь историй/21 тикет и dependency DAG проверяются в финальном Board audit.
Результаты выполнения, SHA и оставшиеся human/physical/audio gates принадлежат
Board, а не каталогу Documentation.
