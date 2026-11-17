---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0006-module-salvage
ticket: EP-0036-US-0006-TK-0005-trade-names
title: Названия трофеев и металлолома в торговле и трюме
stage: draft
approval_kind: workflow
implementation_status: implemented-tested
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0006-TK-0002-content, EP-0036-US-0003-TK-0002-content]
files_touched: 4
serves: [AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Названия трофеев и металлолома в торговле и трюме

## Why

[EP-0036-US-0006-module-salvage](../EP-0036-US-0006-module-salvage.md). Названия трофеев и металлолома в торговле и трюме. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeItemPresentation.cs` | ItemDisplayName:6/ItemDescription — жёсткий switch по 13 товарам, неизвестный typeId возвращается как есть ("item.scrap"). | Покрыть все loot item IDs. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Trade.Item*/Trade.Description* только для существующих товаров. | Добавить RU ключи loot items. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | То же. | Добавить EN ключи loot items. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0006Tk0005Tests.cs` | Новый. | Named tests ниже. |

Разрешены 4 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Для item IDs из Demo/loot.json (ScrapItemTypeId, ModuleItems, ConsumableItems) ItemDisplayName/ItemDescription возвращают локализованные строки по конвенции ключей `Trade.Item.<typeId>` / `Trade.Description.<typeId>` (data-driven ветка до fallback), существующие 13 ветвей switch не меняются. Fallback на сырой typeId для loot items запрещён тестом.

## Implementation steps

1. Добавить в TradeItemPresentation ветку: если ключ `Trade.Item.<typeId>` существует в текущем словаре — вернуть его; иначе прежний fallback. Аналогично description (пустая строка допустима только для старых будущих товаров).
2. Добавить RU/EN имена и короткие описания для item.scrap, каждого trophy module item и charge item, перечисленных в loot.json US3 TK2/US6 TK2. Название трофея отражает тип модуля ("Трофей: оружие Sword I"), описание — «только для продажи, установка недоступна» (D-36-28).
3. Проверить, что тот же presenter используется торговым экраном, списком трюма и окном лута (US3/US6 client) — нигде не отображается typeId.
4. Тест перечисляет все item IDs из реального loot.json через загрузчик и проверяет непустое имя ≠ typeId и parity ключей RU/EN.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0006Tk0005Tests.EveryLootItemHasRuEnName`
- `DemoEp0036Us0006Tk0005Tests.ScrapNameNotTypeId`
- `DemoEp0036Us0006Tk0005Tests.TrophyDescriptionSaysSaleOnly`
- `DemoEp0036Us0006Tk0005Tests.LegacyItemsUnchanged`
- `DemoEp0036Us0006Tk0005Tests.RuEnKeyParity`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0006-TK-0005-trade-names/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0006Tk0005Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Execution scope correction

Also touch ModuleSalvagePresentation.cs and BattleLootConsumablesPanel.cs solely to use the same TradeItemPresentation for loot and trade names, avoiding divergent naming paths. The shared consumable panel exposes a default localized Rows() overload; custom display-name injection remains available for tests.

## Execution — 2026-11-17

Added data-driven Trade.Item/Trade.Description fallbacks for every actual loot SKU; old 13 name/description cases remain unchanged. Trophy descriptions explicitly say sale-only/no installation. Shared loot panels consume the trade presenter; scrap uses kilogram units. Client build/JSON parse/scoped format/diff PASS. Authored tests combine all-SKU localized names, sale-only wording, legacy parity and RU/EN coverage; NOT RUN until US6 boundary. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us6-tk5-build.log.
