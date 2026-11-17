---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
ticket: EP-0036-US-0007-TK-0001-loot-schema
title: Схема каталога лута и фиксированный выход
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0003-TK-0001-contracts]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 1
---

# Схема каталога лута и фиксированный выход

## Why

[EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle.md). Схема каталога лута и фиксированный выход. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Engine/Content/DemoLootDefinitions.cs` | Новый typed каталог. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs` | Load:60, Read:66, encounters:84, fingerprint:88. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0007Tk0001Tests.cs` | Новый schema test. | Только шаги данного тикета. |

Разрешены 3 implementation files, включая тесты; один layer engine. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект: DemoLootDefinitions(schemaVersion=1,ScrapItemTypeId,DroneScrapQuantityByFrame,CarrierScrapQuantityByLayout,ModuleItems,ConsumableItems). ModuleItems map(category,catalogId)->itemTypeId; ConsumableItems map(ammo/decoy,catalogId)->itemTypeId. Load читает Demo/loot.json в общей fingerprint цепочке; ValidateReferences проверяет item mass>0, price>=0, qty>0 и наличие tactical IDs. Не считать новый API уже существующим.

## Implementation steps

1. Добавить typed schema и чтение optional loot.json до публикации registry
2. валидировать полное покрытие фактических layouts демо (weapon,armor,engines,shield,decoy), экземпляры не схлопывать по типу
3. отклонять дубликаты/неизвестные item IDs/negative quantities и inconsistent mass
4. старые отсутствующие секции означают недоступность, не бесплатный лут. Mapping модуля соответствует товару для продажи, установки нет.

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
- `DemoEp0036Us0007Tk0001Tests.UnknownMapping_RejectsCatalog`
- `DemoEp0036Us0007Tk0001Tests.DuplicateKey_RejectsCatalog`
- `DemoEp0036Us0007Tk0001Tests.FixedQuantityRoundtrip`
- `DemoEp0036Us0007Tk0001Tests.MissingSection_DisablesLoot`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0007-TK-0001-loot-schema/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0007Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
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
