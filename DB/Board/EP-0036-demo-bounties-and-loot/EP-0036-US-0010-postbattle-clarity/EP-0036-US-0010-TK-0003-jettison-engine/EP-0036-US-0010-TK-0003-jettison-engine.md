---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0010-postbattle-clarity
ticket: EP-0036-US-0010-TK-0003-jettison-engine
title: Выброс груза во время сбора лута
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0007-TK-0003-postbattle-lifecycle, EP-0036-US-0003-TK-0003-engine]
files_touched: 5
serves: [AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Выброс груза во время сбора лута

## Why

[EP-0036-US-0010-postbattle-clarity](../EP-0036-US-0010-postbattle-clarity.md). Выброс груза во время сбора лута. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | US3 TK1/US6 TK1 loot commands, ILootCampaignCommand marker (US7 TK3). | Append JettisonCargoCommand и reasons. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.LootJettison.cs` | Новый. | PrepareJettisonCargo. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | US3 TK3 dispatch helpers. | Вызов jettison handler. |
| `src/DeepSpaceSaga.Engine/Scenario/BattleLootData.cs` | US3 TK3 ledger. | Jettison receipts (CommandId, item, qty) для идемпотентности и отчёта. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0010Tk0003Tests.cs` | Новый. | Named tests ниже. |

Разрешены 5 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект: JettisonCargoCommand(CommandId, ExpectedRevision, BattleId, ItemTypeId, long Quantity) : ILootCampaignCommand. Reasons: LootClosed, WrongBattle, NotCargoItem, NotEnoughQuantity, InvalidQuantity. Успех уменьшает существующий cargo stack и его cost basis через RemoveCargoCost (SimulationEngine.CargoCost.cs:42), денег не даёт, receipt хранится в BattleLootData.

## Implementation steps

1. Разрешить команду только в LootOpen текущего BattleId (gate US7 TK3 по ILootCampaignCommand); после FinishBattleLoot — LootClosed.
2. Выбрасываются только stacks storageKind=Cargo из трюма игрока (товары, трофеи, лом); пассажиры, пленные, экипаж, топливо и установленное оборудование недоступны (NotCargoItem).
3. Quantity > 0 и ≤ остатка, шаг sellQuantityStep товара; атомарно уменьшить stack и basis (unknown basis не превращается в ноль); выброшенное исчезает навсегда, в мире не появляется.
4. Повтор того же CommandId — тот же receipt без второго списания; Load сохраняет receipts и освобождённую массу.
5. Освобождённая масса сразу учитывается проверкой места US3/US6 (CargoFull до броска).

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0010Tk0003Tests.JettisonFreesMassForSalvage`
- `DemoEp0036Us0010Tk0003Tests.OnlyInLootOpenOfThisBattle`
- `DemoEp0036Us0010Tk0003Tests.PassengersAndFuelNotJettisonable`
- `DemoEp0036Us0010Tk0003Tests.DuplicateCommandNoDoubleRemoval`
- `DemoEp0036Us0010Tk0003Tests.UnknownBasisPreserved`
- `DemoEp0036Us0010Tk0003Tests.LoadKeepsReceipts`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0010-TK-0003-jettison-engine/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0010Tk0003Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.
