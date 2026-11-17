---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
ticket: EP-0036-US-0007-TK-0004-postbattle-map
title: Клики по тактическим остаткам и выход с предупреждением
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0007-TK-0003-postbattle-lifecycle, EP-0036-US-0003-TK-0004-client, EP-0036-US-0006-TK-0004-client]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 1
---

# Клики по тактическим остаткам и выход с предупреждением

## Why

[EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle.md). Клики по тактическим остаткам и выход с предупреждением. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | Escape:210 closes outcome; mouse input:349. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | Existing OverlayClick path. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | Existing geometry/render transform; add frozen source picking. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs` | Новый единый window composing US3/US6 panels. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0007Tk0004Tests.cs` | Новый input/lifecycle tests. | Только шаги данного тикета. |

Разрешены 5 implementation files, включая тесты; один layer client. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект LootOpen input branch: hit-test frozen sources by ObjectId через тот же map transform; open BattleLootWindow selected source. Callbacks ExecuteDemoCampaign/FinishBattleLoot через injected existing session handle. US9 добавляет arrest panel после этого тикета.

## Implementation steps

1. Сохранить tactical map после победы
2. не закрывать её прямым Escape пока есть LootOpen. Клик Disabled drone открывает modules+consumables, wreck drone/carrier scrap
3. подход/дистанция корабля не нужны. Window table включает требуемые колонки и composition panels, уничтоженные items не показывать. Любой выход (Escape/back/button) использует один confirm flow по HasUnclaimed: cancel остаётся, confirm вызывает FinishBattleLoot и закрывает только после success. Load frozen sources открывает ту же карту/окно без повторения бой/roll. Не мешать combat input ветке. Shared BattleLootWindow предоставляет arrest action slot US9 без реализации пленного здесь.

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
- `DemoEp0036Us0007Tk0004Tests.ClickFrozenDroneOpensItsWindow`
- `DemoEp0036Us0007Tk0004Tests.EscapeWarnsAndCancelKeepsLoot`
- `DemoEp0036Us0007Tk0004Tests.ConfirmExitWaitsReceipt`
- `DemoEp0036Us0007Tk0004Tests.LoadRestoresMapCoordinates`
- `DemoEp0036Us0007Tk0004Tests.CombatInputUnaffected`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0007-TK-0004-postbattle-map/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0007Tk0004Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
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
