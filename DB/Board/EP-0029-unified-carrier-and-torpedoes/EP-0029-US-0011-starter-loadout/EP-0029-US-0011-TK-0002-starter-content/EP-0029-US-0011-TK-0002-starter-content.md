---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0011-starter-loadout
ticket: EP-0029-US-0011-TK-0002-starter-content
title: Раскладка модулей, экипаж и 78 торпед
stage: approved
approval_kind: workflow-only
layer: content-data
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0011-TK-0001-starter-manifest]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Раскладка модулей, экипаж и 78 торпед

## Why

Новая учебная кампания начинает реальный бой на оснащённом модульном корабле. После него игрок открывает ровно тот же корабль, а не другой стартовый объект. Этот тикет обеспечивает **Раскладка модулей, экипаж и 78 торпед**. Источники: [история](../EP-0029-US-0011-starter-loadout.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **content-data**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json](../../../../../src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json) | Новый проектируемый файл; до выполнения отсутствует. | Только «Раскладка модулей, экипаж и 78 торпед» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/Scenarios/Tutorial/scenario.json](../../../../../src/DeepSpaceSaga.Client/Scenarios/Tutorial/scenario.json) | `src/DeepSpaceSaga.Client/Scenarios/Tutorial/scenario.json:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Раскладка модулей, экипаж и 78 торпед» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json](../../../../../src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json) | `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Раскладка модулей, экипаж и 78 торпед» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0011Tk0002Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0011Tk0002Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0011Tk0002Tests ниже с независимым oracle. |

## Public API after the change

Новый manifest и ссылки из двух scenario JSON.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Создать manifest точно по таблицам и координатам эпика. Имена/портреты/crew IDs/assignments фиксированы; Ирину назначить вместо Defense operator. Два four-cabins, два M bay, остальные пять прежних модулей. cargo добавляет42 item.drone-torpedo.t1, сохраняет energy1000/food200/fuel750/tokens2000. Создать четыре дрона IDs DRN-PLAYER-01..04. Не трогать Settings defaultScenario и стартовую географию.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-02, AC-03, AC-04 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0011-TK-0001-starter-manifest](../EP-0029-US-0011-TK-0001-starter-manifest/EP-0029-US-0011-TK-0001-starter-manifest.md) — завершить до этого тикета.

## Out of scope

Layers кроме content-data; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0011Tk0002Tests**.

- `Ep0029Us0011Tk0002Tests.StarterHasExactlySeventyEightTorpedoes` — 36+42.
- `Ep0029Us0011Tk0002Tests.EightCabinsSixCrewTwoReserved` — count по реальным modules.
- `Ep0029Us0011Tk0002Tests.StarterFootprintsDoNotOverlap` — independent cell oracle.
- `Ep0029Us0011Tk0002Tests.SameCrewPortraitsAsApproved` — точные asset paths.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0011Tk0002Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0011-TK-0002" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0011Tk0002Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 4 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-01, AC-02, AC-03, AC-04; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

Implementation status: **implemented** (automated PASS; native/product OPEN).

- TDD red: focused run before the manifest/scenario content existed failed 4/4 (missing `Data/Ships/starter-carrier.json`; old 12-cell/6-module/2-crew stubs) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-039-us0011tk0002/red.stdout.txt`.
- Content: new `Data/Ships/starter-carrier.json` — 24-cell 9×9 hull, 9 modules (2× four-cabin quarters, 2× medium bay), 6 crew, reservedCabins 2, 4 owned drones (12/12/6/6), container cargo energy 1000 / food 200 / `item.drone-torpedo.t1` 42; all SP at type max (bays 250, others 150), power On, Ready, no active cycle, no cargo cost metadata. `Tutorial` and `Default` scenario stubs now carry `scenarioMetadata.starterCarrierManifest`, name `Skitalets`, explicit Dunkan Su captain identity, and no inline hull/modules/crew; bodies remain byte-identical apart from metadata.
- Focused tests (4/4 passed, 0 failed):
  `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0011Tk0002Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-039-us0011tk0002/US-0011-TK-0002" --logger "trx;LogFileName=focused.trx"`
  Evidence: `run-039-us0011tk0002/focused.stdout.txt`, `run-039-us0011tk0002/US-0011-TK-0002/focused.trx`.
  Named: `StarterHasExactlySeventyEightTorpedoes` (12+12+6+6+42; literal per-drone oracle + production loader), `EightCabinsSixCrewTwoReserved` (2 real four-cabin modules = 8, crew 6, reserved 2), `StarterFootprintsDoNotOverlap` (independent 24-cell/15-occupied/9-free oracle, anchors exact), `SameCrewPortraitsAsApproved` (five exact W4 asset paths, files exist in source and output, captain keeps default-male portrait).
- Owning build PASS: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore` → 0 warnings, 0 errors (`run-039-us0011tk0002/client-build.stdout.txt`).
- Scoped format PASS: `dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0011Tk0002Tests.cs` → exit 0 (`run-039-us0011tk0002/format.stdout.txt`).
- Protect check: `TutorialScenarioTests` passed (28 passed in the boundary run); Tutorial/Default bodies remain identical apart from metadata.
- Expected boundary reds caused by the intentional starter change (recorded, not fixed here; owned by US-0013):
  `ScenarioEngineTests` 4 failures (`Can_load_real_scenario_file`, `Can_create_engine_from_real_settings_file`, `Real_default_scenario_occupies_6_of_12_hull_cells`, `Real_default_scenario_snapshot_has_player_crew_and_living_quarters_cabin_count`); `ModuleHealthBootstrapTests.Default_modules_have_structure`; `TetrarchLoadoutTests` 4 failures; pre-existing `ScenarioModuleCleanupTests.CurrentContentHasOnlyWorldShipIdentityAndNoLegacyDefinitions` ×2.

  `dotnet test tests/DeepSpaceSaga.Engine.Tests --filter "FullyQualifiedName~ScenarioEngineTests|FullyQualifiedName~ModuleHealthBootstrapTests"` → 5 failed / 70 passed (`run-039-us0011tk0002/engine-boundary.stdout.txt`).
  `dotnet test tests/DeepSpaceSaga.Client.Tests --filter "FullyQualifiedName~TutorialScenarioTests|FullyQualifiedName~TetrarchLoadoutTests|FullyQualifiedName~ScenarioModuleCleanupTests"` → 7 failed / 28 passed (`run-039-us0011tk0002/client-boundary.stdout.txt`).
- Serves AC-01…AC-04 (partially; full story coverage across TK-0001…TK-0003). Native/UI and product acceptance: **OPEN** — no native run was performed for this content ticket.
