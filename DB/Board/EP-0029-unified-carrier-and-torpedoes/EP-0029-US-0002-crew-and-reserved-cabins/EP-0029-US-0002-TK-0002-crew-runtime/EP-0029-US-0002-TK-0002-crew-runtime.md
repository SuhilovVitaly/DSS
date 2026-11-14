---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0002-crew-and-reserved-cabins
ticket: EP-0029-US-0002-TK-0002-crew-runtime
title: Единая запись экипажа и назначения кают
stage: approved
approval_kind: workflow-only
layer: engine
test_project: DeepSpaceSaga.Engine.Tests
depends_on: [EP-0029-US-0002-TK-0001-crew-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Единая запись экипажа и назначения кают

## Why

Игрок узнаёт тех же четырёх женщин-пилотов на корабле и в бою. Два свободных места обозначены как резерв для пленных и не заняты фиктивными персонажами. Этот тикет обеспечивает **Единая запись экипажа и назначения кают**. Источники: [история](../EP-0029-US-0002-crew-and-reserved-cabins.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **engine**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs) | `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Единая запись экипажа и назначения кают» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Единая запись экипажа и назначения кают» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Единая запись экипажа и назначения кают» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Единая запись экипажа и назначения кают» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0002Tk0002Tests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0002Tk0002Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0002Tk0002Tests ниже с независимым oracle. |

## Public API after the change

Проектируемые поля crew и CabinShortage в carrier snapshot.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Расширить ShipCrewMemberData и CrewMemberRuntime optional полями пола/портрета/роли/инициативы. Устойчивые crewId, проверка уникальности на корабле, reservedCabins=2 в CarrierState; formula free=max(0,operationalCabins-crewCount-reserved). При повреждении жилья показывать дефицит, не удалять людей; новые назначения запрещены. Резерв не даёт новый экипаж.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-02, AC-03 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0002-TK-0001-crew-contract](../EP-0029-US-0002-TK-0001-crew-contract/EP-0029-US-0002-TK-0001-crew-contract.md) — завершить до этого тикета.

## Out of scope

Layers кроме engine; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Новый test class: **Ep0029Us0002Tk0002Tests**.

- `Ep0029Us0002Tk0002Tests.EightCabinsSixPeopleTwoReserved` — free=0.
- `Ep0029Us0002Tk0002Tests.DuplicateCrewRejected` — загрузка атомарно отвергается.
- `Ep0029Us0002Tk0002Tests.HousingDamagePreservesCrew` — дефицит показан, все 6 ID остались.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0002Tk0002Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0002-TK-0002" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0002Tk0002Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 5 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-01, AC-02, AC-03; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

Extended `ShipCrewMemberData` + `CrewMemberRuntime` with optional gender/portraitAssetId/role/initiative (passed through load and save), `CarrierStateData.ReservedCabins=2`, and built the crew + cabin-reservation projection in `SimulationEngine.Carrier.cs`. Crew-id uniqueness is already enforced by `ResolveShipCrew` (duplicate → `ScenarioException`, atomic load rejection). Cabin formula: `TotalCabins` = operational cabins (SP>0, !Destroyed living modules), `FreeCabins = max(0, total - occupied - reserved)`; housing damage lowers total below occupied (deficit shown) without removing crew.

- `dotnet test --filter "FullyQualifiedName~Ep0029Us0002Tk0002Tests"` — **3/3 passed** (`EightCabinsSixPeopleTwoReserved`, `DuplicateCrewRejected`, `HousingDamagePreservesCrew`) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-006-us0002tk0002/US-0002-TK-0002/focused.trx`.
- Regression: `SaveLoadContinuityTests` + `ScenarioLoaderTests` — **74/74 passed** (crew schema extension is backward-compatible).
- `dotnet build src/DeepSpaceSaga.Engine --no-restore` — 0 warnings, 0 errors; `dotnet format --verify-no-changes` clean; `git diff --check` clean.

Status: automated PASS (engine layer); native/product acceptance remains OPEN.
