---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0005-torpedo-reload-notifications"
title: "Звук, журнал и Combat report о завершении перезарядки"
stage: "draft"
layer: "contracts-engine-client"
depends_on: []
files_touched: 17
serves: ["US20-AC5"]
created: "2026-10-21T12:18:30.532Z"
revision: 1
---

# TK-0005 — Звук, журнал и Combat report о завершении перезарядки

[История](../EP-0012-US-0020-bugs.md) · [Контракт](../../Documentation.md)

## Why / Decisions

D20-05: прямой запрос пользователя 2026-10-21. По завершении каждого цикла перезарядки торпедного аппарата игрока Engine формирует подтверждённое событие с идентификатором аппарата и физическим временем завершения. Игрок один раз слышит звуковой сигнал и получает сообщение «Торпедный аппарат <имя> перезаряжен» в боевом журнале и Combat reports. Событие не повторяется при новых кадрах, повторах доставки, паузе или загрузке уже завершённого цикла. Незавершённый сохранённый цикл уведомляет один раз после завершения. Готовность аппарата при старте новой игры сама по себе не является завершением перезарядки. Уведомления не раскрывают перезарядку NPC.

## Scope / Assumptions

Это один сквозной feature ticket по прямому поручению «сделай два тикета там же». A20-07: техническое исключение из типовой декомпозиции одного layer/<=5 файлов; используется конечный межслойный allowlist ниже с matching tests для каждого затронутого production проекта. Не размножать карточки без нового поручения. Все пути вне allowlist запрещены; недостающий consumer зафиксировать как gap до расширения реализации.

A20-05: речь о завершении перезарядки своего торпедного аппарата. Аппарат может оставаться недоступным по другой причине; сообщение не должно утверждать обратное. Уничтоженный до завершения аппарат не выдаёт ложную готовность. Existing alert settings/mute действуют; воспроизведение nonblocking. Два разных события не должны теряться из-за общего cooldown звука.

## Code context

| File | Current state / planned responsibility | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs](../../../../src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs) | CombatEventType / CombatJournalEntry: добавить факт завершения без сдвига существующих значений | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs](../../../../src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs) | BattleReportKind: добавить тип отчёта без сдвига старых значений | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | 122: ReadyAtMotionTimeMs устанавливается от пуска; источник deadline цикла | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs) | authoritative advancement: обработать deadline перезарядки, включая перескок несколькими tick | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs) | AppendCombatEvent — один факт, затем ObserveCombatReportFact | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs) | 61: ObserveCombatReportFact — сформировать captured отчёт игрока | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs) | Load: восстановить незавершённый цикл, не переиздавать завершённый | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs](../../../../src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs) | CombatAlertKind / queue / playback gate: отдельный короткий сигнал готовности | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs) | существующий nonblocking audio consumer: дедупликация сигнала по event identity | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs) | PrepareCombatReports — общий display-admitted момент факта | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs) | локализованное сообщение о конкретном аппарате | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs) | новый тип события в журнале | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | текст перезарядки RU | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | текст перезарядки EN | Только изменения для US20-AC5 |
| tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0020TK0005Tests.cs (NEW) | NEW: совместимость типов и сериализации | Только изменения для US20-AC5 |
| tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0005Tests.cs (NEW) | NEW: once-per-cycle, deadline, pause, save/load и privacy | Только изменения для US20-AC5 |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0005Tests.cs (NEW) | NEW: журнал/карточка/audio enqueue/dedup/localization | Только изменения для US20-AC5 |

## Public API after the change

Proposed: append CombatEventType.TorpedoReloadCompleted и BattleReportKind.PlayerTorpedoReloadCompleted без изменения существующих numeric IDs. Использовать существующие ModuleId/MotionTimeMs/Sequence и captured reporter; не выдумывать projectile для события без projectile. При необходимости уточнить nullable representation в перечисленных DTO с контрактными тестами. Один authoritative факт → журнал/надёжный report → scene-admitted звук; Client не определяет завершение по локальному таймеру.

## Implementation steps

1. Перепроверить source signatures и consumers на текущем baseline; перечисленные proposed API не считать существующими.
2. Последовательно обновить Contracts + tests, затем Engine + tests, затем Client/localization + tests в рамках одной карточки.
3. Проверить save/load и доставку события/скорости; не менять save format без доказанной необходимости.
4. Выполнить named checks и отдельно native/audio/UI acceptance.

## Tests

- Reload_before_at_after_deadline: до deadline нет события, на deadline один journal fact и один report с ModuleId; после нет дублей.
- Reload_two_launchers_two_cycles: независимые аппараты и повторные циклы не подавляют чужие записи; один сигнал на событие.
- Reload_pause_high_speed_and_disabled: пауза не завершает цикл; большой шаг через deadline не теряет событие; текст означает завершение перезарядки, не обещает возможность пуска отключённого/безоператорного аппарата.
- Reload_newgame_save_load_and_duplicate_delivery: initially ready молчит; load pending уведомляет позже, load completed и duplicate/out-of-order не повторяют звук/сообщения.
- Reload_presented_scene_and_privacy: три поверхности относятся к одному подтверждённому факту игрока; NPC reload не раскрывается.

План команд: для каждого проекта DeepSpaceSaga.Contracts.Tests, DeepSpaceSaga.Engine.Tests, DeepSpaceSaga.Client.Tests выполнить dotnet test tests/<project>/<project>.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0005Tests. Каждый процесс ограничить 300000ms с завершением дерева. Затем релевантные clock/encounter/report/serialization regressions, owning build, scoped format и git diff --check.

Native: выстрелить своим аппаратом, дождаться завершения; услышать сигнал и увидеть по одному сообщению в журнале и Combat reports с именем аппарата. Повторить при паузе, двух аппаратах, 10× и Load; mute проверить отдельно.

## Out of scope / Invariants

Не менять физические duration/range/damage, AI decisions, RNG или балансные значения. Engine остаётся владельцем фактов/часов. Старые enum IDs и допустимые save данные не переосмысливаются. Human/audio/performance acceptance не доказываются headless тестами.

## Definition of Done

US20-AC5 покрыт named tests и документацией; build/scoped format/diff проходят, save/load и совместимость проверены. Сейчас draft; implementation NOT STARTED; automated/native/product NOT RUN. Planning не разрешает запуск игры или commit/push.
