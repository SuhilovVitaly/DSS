---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0010-postbattle-clarity
ticket: EP-0036-US-0010-TK-0004-jettison-ui
title: Выброс груза в окне лута
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0010-TK-0003-jettison-engine, EP-0036-US-0007-TK-0004-postbattle-map]
files_touched: 5
serves: [AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Выброс груза в окне лута

## Why

[EP-0036-US-0010-postbattle-clarity](../EP-0036-US-0010-postbattle-clarity.md). Выброс груза в окне лута. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs` | US7 TK4 единое окно US3/US6/US9. | Слот панели выброса и подсказка при CargoFull. |
| `src/DeepSpaceSaga.Client/UI/Demo/LootJettisonPanel.cs` | Новый. | Список груза, количество, подтверждение. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Loot ключи US3 TK5. | RU ключи выброса. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | То же. | EN ключи выброса. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0010Tk0004Tests.cs` | Новый. | Named tests ниже. |

Разрешены 5 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Панель отправляет JettisonCargoCommand через existing ExecuteDemoCampaign callback. Ключи Loot.Jettison.Title/Action/Confirm/Freed/NoCargo + reasons TK3.

## Implementation steps

1. В BattleLootWindow при отказе CargoFull показывать рядом «Не хватает N кг — выбросить груз» с переходом в LootJettisonPanel.
2. Панель показывает только подтверждённые Engine cargo stacks с массой; ввод количества с шагом товара; подтверждение «безвозвратно, без оплаты».
3. Pending блокирует повтор; reject показывает причину; после успеха таблица модулей/лома пересчитывает доступность из нового snapshot, локальный баланс трюма не меняется до подтверждения.
4. Load в LootOpen восстанавливает панель без автоматических команд.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0010Tk0004Tests.CargoFullOffersJettison`
- `DemoEp0036Us0010Tk0004Tests.ConfirmRequiredBeforeSend`
- `DemoEp0036Us0010Tk0004Tests.PendingBlocksRepeat`
- `DemoEp0036Us0010Tk0004Tests.SnapshotDrivesFreedMass`
- `DemoEp0036Us0010Tk0004Tests.RuEnKeyParity`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0010-TK-0004-jettison-ui/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0010Tk0004Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Scope correction
Include TacticalBattleScreen.Overlay.cs/Loot.cs to make the jettison button an actual modal input region, preventing map clicks through it. Quantity/step/mass and shortage derive from the new authoritative US10-TK3 projection.

## Execution
Added scrollable authoritative cargo list, item sell-step quantity controls, explicit permanent/no-payment confirmation, shared pending/receipt updates and mass-shortage hint. No optimistic cargo changes. Client/test build PASS after correcting shared trade helper reference (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us10-tk4-build-repeat.log). All four US10 tickets now ready for boundary tests; native NOT RUN.

## Independent review scope correction
Include BattleLootPresentation.cs to distinguish a new intent from a frozen command retry after Unavailable. A new item/quantity/action must not silently execute the old confirmed command. Add an explicit retry action with original cargo/quantity and a regression for changed selection after transport failure.

Review correction PASS: new intent is compared against the frozen retry payload excluding identity/revision; a mismatch is rejected without transport execution. Dedicated Retry shows original jettison item/quantity. Changed-selection regression plus affected shared retry/lifecycle UI: 11/11 PASS in us10-client-review.log. Original US10+prisoner/client corpus22/22 PASS in us10-client.log.
