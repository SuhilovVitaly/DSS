---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0005-torpedo-reload-notifications"
title: "Звук, журнал и Combat report о завершении перезарядки"
stage: "implemented"
layer: "contracts-engine-client"
depends_on: []
files_touched: 17
serves: ["US20-AC5"]
created: "2026-10-21T12:18:30.532Z"
revision: 1
---

Current execution: implemented/published; native/product acceptance OPEN. Earlier planning-only statements below record the prior request. See [execution report](../ExecutionStatus.md).


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
| [src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs](../../../../../src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs) | CombatEventType / CombatJournalEntry: добавить факт завершения без сдвига существующих значений | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs](../../../../../src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs) | BattleReportKind: добавить тип отчёта без сдвига старых значений | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | 122: ReadyAtMotionTimeMs устанавливается от пуска; источник deadline цикла | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs) | authoritative advancement: обработать deadline перезарядки, включая перескок несколькими tick | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs) | AppendCombatEvent — один факт, затем ObserveCombatReportFact | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs) | 61: ObserveCombatReportFact — сформировать captured отчёт игрока | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs) | Load: восстановить незавершённый цикл, не переиздавать завершённый | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs](../../../../../src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs) | CombatAlertKind / queue / playback gate: отдельный короткий сигнал готовности | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs) | существующий nonblocking audio consumer: дедупликация сигнала по event identity | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs) | PrepareCombatReports — общий display-admitted момент факта | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs) | локализованное сообщение о конкретном аппарате | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs) | новый тип события в журнале | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | текст перезарядки RU | Только изменения для US20-AC5 |
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | текст перезарядки EN | Только изменения для US20-AC5 |
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

## Execution scope correction

Include SimulationEngine.Combat.cs (actual ordered scheduler), BattleReportSnapshot.cs (actual enum DTO), no BattleReportKind.cs exists. Existing saved launcher deadline and processed physical cursor define unfinished reload; no new persisted flag/schema required. Reload notification is delivered through reliable scene-admitted reports, with sound dedup by session/sequence.

Save/load probe reproduced strict journal rejection of new reload fact. Scope now includes Scenario/CountermeasureSaveData.cs to admit only own reload entries without a projectile, while preserving all validation of projectile facts.

Scope: migrate PointCombatUS0017TK0001Tests enum-count expectation to explicit stable numeric IDs plus appended reload kind.

## Execution result — 2026-10-21

Appended stable enum values; own reload is one authoritative journal fact with module identity/name at the exact deadline and one captured reliable report, without a fake projectile. Deadline is processed in chronological combat scheduler before equal-time launch work. Existing saved ReadyAtMotionTimeMs and processed physical cursor preserve pending/completed cycles without schema bump. Strict Save18 validation explicitly admits own reload facts. NPC cycles never emit this fact. Client localized journal/report text uses captured name; scene-admitted reports enqueue sound once by session/sequence, including two simultaneous completions, without threat-audio throttling. Audio queue preserves readiness cues; mute/session lifecycle still apply.

Validation: Engine reload+Save18+report filter 52/52; Client reload+localization+report+audio filter 90/90 (includes real Engine/local transport/admitted report/audio enqueue); Contracts reload+US17+report filter 10/10. Earlier contract report batch also18/18. Owning projects built in tests; scoped formatter applied to changed Engine/Client source. Checks capped300000ms. Self-review complete. Native audible playback and human UI acceptance NOT RUN/OPEN; headless enqueue is not audible evidence.
Full Client regression correction: actual scene admission can precede reliable report receipt. Integration test now awaits the report through the production PrepareCombatReports consumer before asserting one report and one sound. Targeted35/35 passed; no assertion removed.

## Actual implementation file scope

- `src/DeepSpaceSaga.Client/Data/Locale/English.json`
- `src/DeepSpaceSaga.Client/Data/Locale/Russian.json`
- `src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs`
- `src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs`
- `src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs`
- `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs`
- `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0005Tests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0020TK0005Tests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0005Tests.cs`
Final full-suite evidence: Client2256/2256 passed in87s after retaining detailed timeout diagnostics. Earlier verified run2255/2256 timed out waiting for reload report; no production delivery defect was proved by that intermittent run. Diagnostics now preserve actual engine reload deadline/journal/report and admitted state if it recurs. Test assertions remain unchanged.
