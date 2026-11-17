---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
ticket: EP-0036-US-0007-TK-0002-carrier-selfdestruct
title: Самоуничтожение носителя при потере собственного крыла
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: []
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 1
---

# Самоуничтожение носителя при потере собственного крыла

## Why

[EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle.md). Самоуничтожение носителя при потере собственного крыла. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

## Decisions

Нормативные D-36-01…28 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Для этого тикета: лут конкретного объекта после победы; одна попытка демонтажа/сбора лома; целый trophy без durability/установки; cargo по массе; шансы и 50%-броски определены пользователем. Ранее разрешённая декомпозиция не означает исполнение/приёмку. Последняя запись 2026-11-17T19:19:08Z — время записи, не отправки.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API проверять по prerequisite diff до исполнения.
- Балансовые A в шагах/каталоге — стартовый плейтестовый baseline, не утверждение доказанного баланса.
- Runtime rejects stale/duplicate identity через единый campaign pipeline; запись rejected receipt не равна выдаче добычи.
- Все изменения ограничены EP-0036 результатом; при непредвиденном drift пересмотреть этот allowlist до расширения diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | CheckVictory:47 сейчас TacticalNoDrones по стороне. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | OriginCarrierId:43, CarrierFor:116. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | Constructor:26–33 присваивает PlayerSide до BeginTurn. | Передать сторону в battle до первого хода. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | Existing battle projection, source carrier identity. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0007Tk0002Tests.cs` | Новый battle tests. | Только шаги данного тикета. |

Разрешены 5 implementation files, включая тесты; один layer engine. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект самоуничтожения: для каждого enemy carrier с исходным непустым assigned wing, all OriginCarrierId-owned drones Destroyed/Disabled => Hull=0, существующая typed причина TacticalCarrierDestroyed, внутренний source kind SelfDestructCarrier, фиксированный wreck source ID. Не добавлять новый TacticalOutcomeReason: использовать существующий TacticalCarrierDestroyed(c.Id); факт selfdestruct хранить в frozen loot source kind. не менять player carrier правило.

## Implementation steps

1. Добавить в TacticalBattle state явный SelfDestructEnemySide, заполнить из session.PlayerSide (противоположная сторона) в TacticalBattleSession constructor до BeginTurn. Никакого определения игрока по имени/первому carrier. Проверять после переходов состояния дронов до прежнего early victory
2. не использовать ActiveDrones всей стороны. При нескольких enemy carriers один гибнет независимо, surviving carrier продолжает бой. Disabled drones не уничтожать/пилота не убивать самоуничтожением. Сохранить D-35-26 carrier-loss->Disabled для уцелевших дронов, обеспечить конечность/порядок каскада. A: пустое исходное крыло не самоуничтожается автоматически (защита vacuous all)
3. demo carriers имеют wing. Selfdestruct wreck сохраняется для US3 только scrap, повторный tick не создаёт второй источник.

## Invariants

- Engine владеет шансом/RNG, capacity, деньгами и подтверждённым результатом; UI не вычисляет новый исход.
- Stable BattleId/ObjectId/module slot/command identity, phase и terminal attempt переживают Load. Не reroll и не дублировать cargo/postings.
- Текущий single inventory/PlayerCredits сохраняются; новый trophy только обычный продаваемый item, не отдельный wallet.
- Успешный salvage выдаёт исправный commodity; не вводить прочность 0–100%, ремонт или установку трофея.
- 50%-бросок касается предметов, не пилота. Самоуничтожение не создаёт неоговорённый AoE.
- CargoFull до попытки не тратит RNG/попытку. Generic catalog corruption отклоняется до публикации нового мира.

## Out of scope

Пытки/вербовка, установка трофеев, новая модульная повреждаемость/ремонт, мировые persistent вреки, произвольная фракционная симуляция, несвязанные исправления и публикация. Арест реализуется отдельно US9, инженер US8; подключать их API по prerequisites, не копировать правила.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0007Tk0002Tests.OneCarrierOwnWingDisabled_SelfDestructs`
- `DemoEp0036Us0007Tk0002Tests.OtherCarrierLiveWingContinues`
- `DemoEp0036Us0007Tk0002Tests.DisabledPilotSurvives`
- `DemoEp0036Us0007Tk0002Tests.TickDuplicateNoSecondWreck`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0007-TK-0002-carrier-selfdestruct/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0007Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist; для content-only не требуется. Длительный corpus делить по полным IDs, не расширять лимит. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах и owning layer, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants, нет второго источника inventory/денег.
- Тесты/build/format не выдаются за успешные без выполнения; native только при отдельной разрешённой проверке на меньшем мониторе.
- Временные результаты убраны по AGENTS; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite создатели новых файлов указаны. Общие интеграционные границы распределены US3 (loot ledger), US6 (module salvage), US7 (lifecycle), US8 (engineer), US9 (custody). Не исполнять scaffold без prerequisite; draft означает ожидаемую сверку составленного плана, не отсутствующую игровую реализацию.

## Execution — 2026-11-17

Session explicitly sets enemy side before BeginTurn. CheckVictory self-destructs each enemy carrier only when its nonempty owned wing has no Air drone; survivor carriers and disabled pilots remain intact. HashSet records cause once; existing TacticalCarrierDestroyed outcome is reused. Combined squad test covers independent carrier continuation, disabled survivor and repeated-check idempotency, plus player-side B. Engine build/scoped format/diff PASS; tests NOT RUN until US7 completion. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us7-tk2-build.log.
US7 Engine boundary: 10/12 passed; two squad fixtures used a schema-only placeholder tactic standard absent in real content. Fixtures now select an actual catalog tactic. Production unchanged; repeat after corrections.
