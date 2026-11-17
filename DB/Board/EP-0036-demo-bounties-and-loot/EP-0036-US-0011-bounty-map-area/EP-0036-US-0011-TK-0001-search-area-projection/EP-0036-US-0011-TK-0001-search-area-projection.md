---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0011-bounty-map-area
ticket: EP-0036-US-0011-TK-0001-search-area-projection
title: Геометрия района поиска отслеживаемого bounty
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0001-TK-0002-engine]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Геометрия района поиска отслеживаемого bounty

## Why

[EP-0036-US-0011-bounty-map-area](../EP-0036-US-0011-bounty-map-area.md). Геометрия района поиска отслеживаемого bounty. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TargetBountyContracts.cs` | US1 TK1: BountyOrder(..., string? SearchRegion, ...). | Trailing optional BountySearchArea(X, Y, Radius). |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TargetBounty.cs` | US1 TK2 projection. | Заполнить SearchArea из content-региона группы. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs` | :54–57 definition.Region(X,Y,Radius) задаёт область размещения патруля. | Read-only helper региона по TargetGroupId, без изменения генерации. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0011Tk0001Tests.cs` | Новый. | Named tests ниже. |

Разрешены 4 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект: BountySearchArea(double X, double Y, double Radius); BountyOrder получает trailing SearchArea = null по умолчанию. Engine заполняет его только при Tracking=true и Eligibility=target_available (не victory_confirmed/paid/unavailable). Геометрия — статичный content-регион группы (definition.Region, радиус + PatrolRadius), не текущая позиция группы.

## Implementation steps

1. По TargetGroupId найти definition региона группы и спроецировать центр/радиус; радиус покрывает весь патрульный квадрат (Region.Radius + PatrolRadius·√2).
2. Не использовать текущие X/Y группы и не раскрывать необнаруженную группу (fog of war EP-0030/0035): одинаковый результат при обнаруженной и необнаруженной группе.
3. Tracking=false, Claimable, Paid, Unavailable → SearchArea=null. Повтор/Load дают идентичную геометрию (детерминизм от content+seed).

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0011Tk0001Tests.TrackedOpenBountyHasArea`
- `DemoEp0036Us0011Tk0001Tests.UntrackedOrClaimedHasNoArea`
- `DemoEp0036Us0011Tk0001Tests.AreaIndependentOfCurrentGroupPosition`
- `DemoEp0036Us0011Tk0001Tests.AreaCoversPatrolRoute`
- `DemoEp0036Us0011Tk0001Tests.LoadKeepsSameArea`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0011-TK-0001-search-area-projection/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0011Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Execution
Optional search area uses content Region center and Region.Radius + PatrolRadius*sqrt(2), only for tracked target_available bounties. No current/observed group coordinates are read. Old DTO defaults null. Engine/test build PASS after fixture field correction (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us11-tk1-build-repeat.log); tests deferred to US11 boundary.
