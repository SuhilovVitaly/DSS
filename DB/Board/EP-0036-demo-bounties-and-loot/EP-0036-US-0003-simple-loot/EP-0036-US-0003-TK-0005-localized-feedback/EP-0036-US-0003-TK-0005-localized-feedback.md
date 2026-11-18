---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0003-simple-loot
ticket: EP-0036-US-0003-TK-0005-localized-feedback
title: RU/EN общего окна лута и отказов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0003-TK-0001-contracts]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 3
---

# RU/EN общего окна лута и отказов

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

[EP-0036-US-0003-simple-loot](../EP-0036-US-0003-simple-loot.md). RU/EN общего окна лута и отказов. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Existing dictionary. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Existing dictionary. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0003Tk0005Tests.cs` | Новый parity test. | Только шаги данного тикета. |

Разрешены 3 implementation files, включая тесты; один layer content-data. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Ключи модульной таблицы и общего окна, объектов, сбор ракет/ловушек/лома, состояния попытки, нехватка трюма, оставить добычу, exit warning, closed session; placeholders одинаковы.

## Implementation steps

1. Дать краткие RU/EN тексты для всех stable loot reasons, названий кнопок и предупреждения о безвозвратно оставленном
2. не показывать destroyed items, class names/IDs. Также добавить keys отчёта US4: награда к получению/выплачена, взято/оставлено, неудачные попытки, пленные, реальные потери, оценка не доход. Этот тикет также владеет module keys US6: название, целый/сохранность, примерная стоимость, шанс демонтажа, уже разобрано/неудача. US6 client только потребляет эти ключи.

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
- `DemoEp0036Us0003Tk0005Tests.KeysExistInBothLocales`
- `DemoEp0036Us0003Tk0005Tests.ReasonsHaveHumanText`
- `DemoEp0036Us0003Tk0005Tests.ExitWarningHasRemainingCount`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0003-TK-0005-localized-feedback/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0003Tk0005Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
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

## Execution — 2026-11-17

Added RU/EN shared loot window, source labels, all stable reasons, module conditions/chances/estimated-value caveat, permanent-discard warning with count and battle report labels. Added human item names for all trophy/consumable SKUs. JSON parsing/Client build/scoped format/diff PASS; locale tests authored, not run at ticket level. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us3-tk5-build.log.
