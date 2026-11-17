---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0003-simple-loot
ticket: EP-0036-US-0003-TK-0001-contracts
title: Пообъектный контракт добычи
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: []
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 3
---

# Пообъектный контракт добычи

## Why

[EP-0036-US-0003-simple-loot](../EP-0036-US-0003-simple-loot.md). Пообъектный контракт добычи. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | Новый: команды и immutable проекция. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | DemoRewardsSnapshot пуст; DemoCampaignCommand:4, response:6. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0036Us0003Tk0001Tests.cs` | Новый contract test. | Только шаги данного тикета. |

Разрешены 3 implementation files, включая тесты; один layer contracts. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Спроектировать LootSessionSnapshot(BattleId,Phase,Objects,HasUnclaimed), LootObjectSnapshot(ObjectId,Kind,Consumables,Scrap,Modules,Pilot); stable IDs battle/object/slot. Команды CollectLootConsumablesCommand(CommandId,ExpectedRevision,BattleId,ObjectId,ItemTypeId,Quantity), AttemptScrapCommand(...BattleId,ObjectId), FinishBattleLootCommand(...BattleId,ConfirmDiscard). DemoRewardsSnapshot получает nullable Loot, response получает Rewards. Статусы Open/Closed; Scrap NotAttempted/Failed/Collected, typed reasons WrongBattle,NotVictory,LootClosed,InvalidTarget,AlreadyAttempted,CargoFull,ConfirmDiscardRequired. Pilot nullable содержит frozen SourcePilotId/DisplayName/Portrait/Alive/CaptureReason для US9; это не item и не участвует в50%-броске. Captured derive по Prisoners ledger source identity, без дублирующего mutable флага. Modules: ImmutableArray<ModuleLootSnapshot>; определить здесь полную read-model запись (InstanceId,Category,CatalogId,ItemTypeId,DisplayName,ConditionLabel,EstimatedValueTokens,ChanceBasisPoints,State,CanAttempt,Reason), чтобы contracts собирался до US6. US6 TK1 добавляет команду демонтажа и schema compatibility tests, не второй тип. Pilot: отдельный LootPilotSnapshot с указанными полями, не PrisonerSnapshot ещё не исполненного US9. никаких клиентских цен/шансов. Все loot-команды реализуют ILootCampaignCommand { string BattleId {get;} }, чтобы US7 мог отличить postbattle доступ от остальных campaign действий.

## Implementation steps

1. Определить DTO/команды и новые reason constants
2. подключить Rewards к snapshot/response с defaults старых данных
3. передавать остатки и authoritative canAct без скрытых уничтоженных предметов.

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
- `DemoEp0036Us0003Tk0001Tests.DtoRoundtrip_PreservesObjectIdentity`
- `DemoEp0036Us0003Tk0001Tests.MissingRewards_IsUnavailable`
- `DemoEp0036Us0003Tk0001Tests.CommandCarriesNoClientPriceOrChance`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0003-TK-0001-contracts/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0003Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist; для content-only не требуется. Длительный corpus делить по полным IDs, не расширять лимит. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах и owning layer, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants, нет второго источника inventory/денег.
- Тесты/build/format не выдаются за успешные без выполнения; native только при отдельной разрешённой проверке на меньшем мониторе.
- Временные результаты убраны по AGENTS; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite создатели новых файлов указаны. Общие интеграционные границы распределены US3 (loot ledger), US6 (module salvage), US7 (lifecycle), US8 (engineer), US9 (custody). Не исполнять scaffold без prerequisite; draft означает ожидаемую сверку составленного плана, не отсутствующую игровую реализацию.
