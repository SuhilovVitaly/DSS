---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0011-bounty-map-area
ticket: EP-0036-US-0011-TK-0002-starmap-area
title: Район цели bounty на звёздной карте
stage: draft
approval_kind: workflow
implementation_status: complete
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0011-TK-0001-search-area-projection, EP-0036-US-0001-TK-0003-client]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Район цели bounty на звёздной карте

## Why

[EP-0036-US-0011-bounty-map-area](../EP-0036-US-0011-bounty-map-area.md). Район цели bounty на звёздной карте. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BountySearchAreaPresentation.cs` | Новый. | Круг района, подпись цели/награды. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | :1322 PirateGroupsPresentation.Draw — слой маркеров групп. | Один вызов отрисовки районов перед маркерами групп. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | bounty.* ключи US1 TK4. | RU ключи района. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | То же. | EN ключи района. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0011Tk0002Tests.cs` | Новый. | Named tests ниже. |

Разрешены 5 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Читает Rewards.Bounties[].SearchArea. Ключи bounty.map.area_label ("Район поиска: {target} · {reward}"), bounty.map.legend.

## Implementation steps

1. Для каждого BountyOrder с SearchArea рисовать полупрозрачный круг в мировых координатах карты (тот же transform, что у маркеров групп) с подписью цели и награды; цвет отличается от маркеров угроз.
2. Не рисовать позицию группы — её показывает только существующий маркер EP-0035 при обнаружении.
3. Переключение Tracking на доске (US1 TK3) отражается на карте после подтверждённого snapshot; Claimable/Paid снимают район.
4. Hover/клик не отправляют команд; Load восстанавливает районы.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0011Tk0002Tests.TrackedBountyDrawsArea`
- `DemoEp0036Us0011Tk0002Tests.UntrackedNotDrawn`
- `DemoEp0036Us0011Tk0002Tests.NoGroupPositionLeak`
- `DemoEp0036Us0011Tk0002Tests.ClaimedRemovesArea`
- `DemoEp0036Us0011Tk0002Tests.RuEnKeyParity`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0011-TK-0002-starmap-area/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0011Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Execution
Star map renders a translucent blue static search circle before pirate markers, with target/reward text and viewport clipping. Only authoritative tracked/available areas are used; no input commands or hidden position fallback. Client/test build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us11-tk2-build.log). Both US11 tickets implemented; boundary checks next.
