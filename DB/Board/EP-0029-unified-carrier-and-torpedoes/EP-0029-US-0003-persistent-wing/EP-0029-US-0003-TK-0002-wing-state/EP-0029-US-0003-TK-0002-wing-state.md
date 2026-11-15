---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0003-persistent-wing
ticket: EP-0029-US-0003-TK-0002-wing-state
title: Хранение и проверка крыла
stage: approved
approval_kind: workflow-only
layer: engine
test_project: DeepSpaceSaga.Engine.Tests
depends_on: [EP-0029-US-0003-TK-0001-wing-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Хранение и проверка крыла

## Why

Игрок владеет четырьмя конкретными дронами, размещёнными по ангарным местам. Повреждение, оснащение, пилот и боезапас относятся к экземпляру дрона. Этот тикет обеспечивает **Хранение и проверка крыла**. Источники: [история](../EP-0029-US-0003-persistent-wing.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **engine**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Хранение и проверка крыла» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Хранение и проверка крыла» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.OwnedWing.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.OwnedWing.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Хранение и проверка крыла» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Хранение и проверка крыла» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0003Tk0002Tests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0003Tk0002Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0003Tk0002Tests ниже с независимым oracle. |

## Public API after the change

Проектируемые OwnedDroneData, OwnedWingValidator, BuildOwnedWingSnapshot.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. CarrierState хранит OwnedDroneData и назначения; initial creation и Load используют один validator. Валидировать ссылки catalogue, shield/engine/module slots, hull/ammo/charges bounds, отсутствие дублированных мест/пилотов, pilots принадлежат crew и имеют роль. Базовый способ оснащения в этом эпике — стартовый manifest; полноценная верфь остаётся EP-0021. Destroyed drone сохраняет tombstone, не боеготов, слот освобождается логически без выдачи нового.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-02, AC-03 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0003-TK-0001-wing-contract](../EP-0029-US-0003-TK-0001-wing-contract/EP-0029-US-0003-TK-0001-wing-contract.md) — завершить до этого тикета.

## Out of scope

Layers кроме engine; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Новый test class: **Ep0029Us0003Tk0002Tests**.

- `Ep0029Us0003Tk0002Tests.FifthDroneRejectedWithFourSlots` — атомарный отказ.
- `Ep0029Us0003Tk0002Tests.DuplicatePilotOrSlotRejected` — неизменный мир.
- `Ep0029Us0003Tk0002Tests.DestroyedDroneRetainsCrew` — 6 членов экипажа после потери.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0003Tk0002Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0003-TK-0002" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs src/DeepSpaceSaga.Engine/SimulationEngine.OwnedWing.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0003Tk0002Tests.cs
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

Added `OwnedDroneData` + `CarrierStateData.OwnedDrones` (save schema), `OwnedWingValidator` (drone/slot/pilot uniqueness, pilot-in-crew + Pilot role, hangar-slot capacity, hull/ammo/decoy bounds), the carrier runtime `_ownedWing` store, `SetOwnedWingForTests` seam, and `BuildOwnedWingProjection`/`BuildHangarSlots` (populates slot DroneId, destroyed drone → tombstone readiness reason). Catalogue-reference validation (frame/weapon/armor ids against the tactical content) is deferred to TK-0011, where the starter manifest first connects the tactical content loader — noted as a deliberate scope boundary.

- `dotnet test --filter "FullyQualifiedName~Ep0029Us0003Tk0002Tests"` — **3/3 passed** (`FifthDroneRejectedWithFourSlots`, `DuplicatePilotOrSlotRejected`, `DestroyedDroneRetainsCrew`) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-009-us0003tk0002/US-0003-TK-0002/focused.trx`.
- `dotnet build src/DeepSpaceSaga.Engine --no-restore` — 0 warnings, 0 errors; `dotnet format --verify-no-changes` clean; `git diff --check` clean.

Status: automated PASS (engine layer); native/product acceptance remains OPEN.

## Remediation 2026-11-15

Verified finding (remediation review P1, item 5): `OwnedWingValidator.Validate` checked numeric bounds only. Unknown layout/frame/weapon/armor/shield/decoy/engine references and over-capacity magazines, engine slots, shield slots/count and decoy charges were accepted, so a wing that cannot be assembled from the shipped tactical content passed every load gate (`SetOwnedWingForTests`, `ValidateCarrierStateForLoad`).

Fix: after the numeric bounds, `OwnedWingValidator.Validate` resolves `registry.TacticalBattle.Catalog` and validates every drone against it — `FrameId` exists in `Layouts`, the layout class exists in `Frames`, `WeaponId`/`ArmorId` exist, optional `ShieldId`/`DecoyId` exist, every `EngineIds` entry exists, and frame capacities bound the engine count, shield count, shield slots, decoy charges and magazine (`AmmoCapacity <= frame.AmmoCapacity`; `AmmoCurrent <= AmmoCapacity` stays in the numeric block). When the registry has no tactical content the validator keeps the legacy numeric-only behaviour, so worlds without battle content still load. Persisted engine-factor validation (`EngineFactors` count/range) was added together with the field in US-0006 TK-0002.

Named tests (`Ep0029Us0003Tk0002Tests`):
- `WingRejectsUnknownLayoutWeaponEngine` — baseline starter-shaped wing accepted; each unknown layout/weapon/armor/shield/decoy/engine reference rejected.
- `WingRejectsOverCapacityMagazineAndSlots` — magazine 13 on a 12-capacity frame, 3 shields on a 2-slot frame, shieldSlots 3, 3 decoy charges on a 2-charge decoy and 3 engines on a 2-slot frame are rejected.
- `ValidStarterWingPassesCatalogValidation` — the four starter drones pass with no readiness reasons.

Evidence (real content through `EngineContentLoader.CreateEngineFromScenarioFile(src/DeepSpaceSaga.Client/Settings.json, <temp scenario>)`):
- TDD red: `dotnet test tests/DeepSpaceSaga.Engine.Tests --filter "FullyQualifiedName~Ep0029Us0003Tk0002Tests"` → 2 failed / 4 passed (rejection paths threw nothing) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-042-us0003tk0002/red.stdout.txt`.
- Green: same filter → 6/6 passed — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-042-us0003tk0002/US-0003-TK-0002/focused.trx`, `.../focused.stdout.txt`.
- `dotnet build src/DeepSpaceSaga.Engine --no-restore` → 0 warnings, 0 errors (`.../engine-build.stdout.txt`); scoped `dotnet format --verify-no-changes` → exit 0 (`.../format.stdout.txt`).

Status: automated PASS (engine layer); native/product acceptance remains OPEN.

## Remediation 2026-11-15 (follow-up: null/blank equipment ids)

Verified finding (validation P3 after Batch R-A): a null `hangarModuleId`, `frameId`, `weaponId`, `armorId` or engine id reached a `Dictionary`/`ImmutableDictionary` probe and threw `ArgumentNullException` instead of the validator's `ScenarioException` contract. A malformed save or scenario therefore failed with an infrastructure exception rather than a clean carrier-state rejection.

Fix (narrow): `OwnedWingValidator.Validate` rejects a null/blank `hangarModuleId` before the capacity probe; `ValidateCatalogReferences` rejects null/blank `frameId`, `weaponId`, `armorId`, blank `shieldId`/`decoyId` and empty engine ids before any catalogue lookup. Null `shieldId`/`decoyId` keep their documented meaning ("no shield"/"no decoy") and stay valid.

Named test: `Ep0029Us0003Tk0002Tests.NullOrBlankEquipmentIdsRejectCleanly` — null hangar/frame/weapon/armor and a null engine entry now throw `ScenarioException`; blank hangar/frame/shield/decoy ids are rejected the same way; a first drone with `shieldId`/`decoyId` null and zero counts is still accepted.

Evidence:
- TDD red: `dotnet test tests/DeepSpaceSaga.Engine.Tests --filter "FullyQualifiedName~Ep0029Us0003Tk0002Tests"` → 1 failed / 6 passed; the new test hit `System.ArgumentNullException` (Parameter 'key') — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-050-us0003tk0002-followup/red.stdout.txt`.
- Green: same filter → 7/7 passed — `.../run-050-us0003tk0002-followup/US-0003-TK-0002/focused.trx`, `.../focused.stdout.txt`.
- `dotnet build src/DeepSpaceSaga.Engine --no-restore` → 0 warnings, 0 errors (`.../engine-build.stdout.txt`); scoped `dotnet format --verify-no-changes` → exit 0 (`.../format.stdout.txt`).

Status: automated PASS (engine layer); native/product acceptance remains OPEN.

