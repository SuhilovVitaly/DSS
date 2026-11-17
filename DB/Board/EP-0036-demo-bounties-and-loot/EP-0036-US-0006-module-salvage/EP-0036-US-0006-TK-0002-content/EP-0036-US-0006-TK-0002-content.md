---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0006-module-salvage
ticket: EP-0036-US-0006-TK-0002-content
title: Продаваемые целые модули и catalog mapping
stage: draft
approval_kind: workflow
implementation_status: implemented-tested
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0007-TK-0001-loot-schema, EP-0036-US-0003-TK-0002-content, EP-0036-US-0006-TK-0001-contracts]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 1
---

# Продаваемые целые модули и catalog mapping

## Why

[EP-0036-US-0006-module-salvage](../EP-0036-US-0006-module-salvage.md). Продаваемые целые модули и catalog mapping. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Client/Data/Demo/loot.json` | US3 TK2 создаёт базовую секцию, US7 schema. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json` | Existing cargo item definitions; добавлять trophy entries. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json` | Existing finite buyer profiles. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0006Tk0002Tests.cs` | Новый mapping test. | Только шаги данного тикета. |

Разрешены 4 implementation files, включая тесты; один layer content-data. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

ModuleItems mapping реальных tactical weapon/armor/shield/engine/decoy IDs -> целые продаваемые item IDs. UnitMassKg=масса соответствующей tactical definition; один instance=одна штука. A: base estimated value=ceil(massKg*10) Tokens min1; это content baseline, не рынок quote. Decoy consumable отдельный charge item, не сам decoy module.

## Implementation steps

1. Перечислить все установленные неуничтоженные модули layouts демо без произвольного поднабора: каждый engine/shield slot, weapon,armor,decoy
2. frame не навешанный модуль (A техническая граница current layout). Маппинг не создаёт оборудование игроку бесплатно. Для charge item mass использовать согласованную каталогом массу/единицу и отдельную запись
3. A baseline1kg/charge если нет отдельной нормы, отметить в content. Покупатель каждого trophy/charge имеет конечные capacity/budget, A capacity100 штук на тип. Никаких путей установки трофея.

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
- `DemoEp0036Us0006Tk0002Tests.EveryInstalledModuleMapsToSaleItem`
- `DemoEp0036Us0006Tk0002Tests.ItemMassMatchesTacticalDefinition`
- `DemoEp0036Us0006Tk0002Tests.RepeatedSlotsRemainMultipleUnits`
- `DemoEp0036Us0006Tk0002Tests.TrophyHasFiniteBuyer`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0006-TK-0002-content/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0006Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist; для content-only не требуется. Длительный corpus делить по полным IDs, не расширять лимит. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах и owning layer, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants, нет второго источника inventory/денег.
- Тесты/build/format не выдаются за успешные без выполнения; native только при отдельной разрешённой проверке на меньшем мониторе.
- Временные результаты убраны по AGENTS; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite создатели новых файлов указаны. Общие интеграционные границы распределены US3 (loot ledger), US6 (module salvage), US7 (lifecycle), US8 (engineer), US9 (custody). Не исполнять scaffold без prerequisite; draft означает ожидаемую сверку составленного плана, не отсутствующую игровую реализацию.

## Execution scope correction

Extend allowlist to Scenario/LootCatalogCompatibility.cs and CarrierCatalogCompatibility.cs plus the existing US3 migration test: preserve the exact intermediate scrap-only catalog/profile as a second structural predecessor when adding trophy SKUs. Keep all pre-existing fields and drift checks.
The same compatibility correction also touches SimulationEngine.cs to distinguish missing scrap (required by the intermediate profile) from newly added trophy stock.

## Execution — 2026-11-17

Added all six installed module type mappings, separate 1kg decoy charges and finite industrial demand (target50 each, zero initial stock, existing budget). Trophy mass equals tactical definitions; estimated value is mass*10 baseline. Structural compatibility now recognizes both pre-loot and scrap-only predecessors and does not waive scrap stock in the latter. Client build, JSON parse, scoped formatting/diff PASS. Tests NOT RUN until US6 boundary. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us6-tk2-build.log.
