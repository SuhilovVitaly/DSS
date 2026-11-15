---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0011-starter-loadout
ticket: EP-0029-US-0011-TK-0001-starter-manifest
title: Один manifest корабля для двух стартов
stage: approved
approval_kind: workflow-only
layer: engine
test_project: DeepSpaceSaga.Engine.Tests
depends_on: [EP-0029-US-0001-TK-0004-module-validation, EP-0029-US-0002-TK-0003-pilot-catalog, EP-0029-US-0003-TK-0003-wing-mass, EP-0029-US-0004-TK-0005-ammo-exhaustion, EP-0029-US-0007-TK-0003-catalog-compatibility]
files_touched: 5
serves: [AC-01, AC-04]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Один manifest корабля для двух стартов

## Why

Новая учебная кампания начинает реальный бой на оснащённом модульном корабле. После него игрок открывает ровно тот же корабль, а не другой стартовый объект. Этот тикет обеспечивает **Один manifest корабля для двух стартов**. Источники: [история](../EP-0029-US-0011-starter-loadout.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **engine**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs) | Новый проектируемый файл; до выполнения отсутствует. | Только «Один manifest корабля для двух стартов» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs) | `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Один manifest корабля для двух стартов» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs) | `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Один manifest корабля для двух стартов» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs](../../../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs) | `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:1` — существующий owning file; передаёт settings-каталог как content root при New Game. | Только «Один manifest корабля для двух стартов» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0011Tk0001Tests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0011Tk0001Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0011Tk0001Tests ниже с независимым oracle. |

## Public API after the change

Проектируемые StarterCarrierManifest и optional scenarioMetadata.starterCarrierManifest.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Добавить optional starterCarrierManifest path в scenario metadata; loader разрешает строго относительно scenario/content root без traversal и materialize только version0/New Game. Manifest полон для модулей/crew/wing/cargo и не смешивается с дублирующими полями корабля: конфликт — отказ. После materialize save пишет expanded state, не ссылку для повторной выдачи. Default и Tutorial ссылаются на один manifest.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01, AC-04 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0001-TK-0004-module-validation](../../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-TK-0004-module-validation/EP-0029-US-0001-TK-0004-module-validation.md) — завершить до этого тикета.
- [EP-0029-US-0002-TK-0003-pilot-catalog](../../EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-TK-0003-pilot-catalog/EP-0029-US-0002-TK-0003-pilot-catalog.md) — завершить до этого тикета.
- [EP-0029-US-0003-TK-0003-wing-mass](../../EP-0029-US-0003-persistent-wing/EP-0029-US-0003-TK-0003-wing-mass/EP-0029-US-0003-TK-0003-wing-mass.md) — завершить до этого тикета.
- [EP-0029-US-0004-TK-0005-ammo-exhaustion](../../EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-TK-0005-ammo-exhaustion/EP-0029-US-0004-TK-0005-ammo-exhaustion.md) — завершить до этого тикета.
- [EP-0029-US-0007-TK-0003-catalog-compatibility](../../EP-0029-US-0007-save-continuity/EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md) — завершить до этого тикета.

## Out of scope

Layers кроме engine; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Новый test class: **Ep0029Us0011Tk0001Tests**.

- `Ep0029Us0011Tk0001Tests.DefaultAndTutorialResolveSameCarrier` — структурное равенство стартового корабля.
- `Ep0029Us0011Tk0001Tests.LoadNeverReappliesManifest` — 7 торпед не становятся42.
- `Ep0029Us0011Tk0001Tests.ConflictingInlineAndManifestReject` — нет двойных modules.
- `Ep0029Us0011Tk0001Tests.TraversalRejected` — path не выходит из content root.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0011Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0011-TK-0001" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs tests/DeepSpaceSaga.Engine.Tests/Carrier/Ep0029Us0011Tk0001Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 5 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-01, AC-04; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

Implementation status: **implemented** (automated PASS; native/product OPEN).

- Board amendment applied before code: allowlist 4→5 files (`EngineContentLoader.cs` added), DoD says 5 files; `Tickets.md` row updated. Process-only, no product change.
- TDD red: focused run before implementation failed for the expected missing API (`LoadFromFile` has no `contentRoot`, `ScenarioMetadata.StarterCarrierManifest` missing) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-038-us0011tk0001/red.stdout.txt`.
- Implementation: `StarterCarrierManifest` DTO (schema 1, `UnmappedMemberHandling.Disallow`, hull/modules/crew/carrierState required), `scenarioMetadata.starterCarrierManifest` (null omitted), New Game-only resolution in `ScenarioLoader.LoadFromFile` with content root, traversal/rooted/containment rejection, inline-conflict rejection, expansion before `ValidateAndNormalize`, metadata stripped; `EngineContentLoader` passes the settings directory as content root. Saves and `LoadFromJson` never resolve a manifest.
- Focused tests (7/7 passed, 0 failed):
  `dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0011Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-038-us0011tk0001/US-0011-TK-0001" --logger "trx;LogFileName=focused.trx"`
  Evidence: `run-038-us0011tk0001/focused.stdout.txt`, `run-038-us0011tk0001/US-0011-TK-0001/focused.trx`.
  Named: `DefaultAndTutorialResolveSameCarrier` (identical expanded carrier; 24 hull cells / 9 modules / 6 crew / 4 drones / 78 torpedoes), `LoadNeverReappliesManifest` (ammo 12→7 and hold 42→5 persist; stale reference not applied), `ConflictingInlineAndManifestReject`, `TraversalRejected`; plus rooted-path, missing-file and `LoadFromJson` rejection checks.
- Owning build PASS: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore` → 0 warnings, 0 errors (`run-038-us0011tk0001/engine-build.stdout.txt`).
- Scoped format PASS: `dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include <the 5 allowlisted files>` → exit 0 (`run-038-us0011tk0001/format.stdout.txt`).
- Serves AC-01/AC-04 (partially; story coverage across TK-0001…TK-0003). Native/UI and product acceptance: **OPEN** — no native run was performed for this engine ticket.

## Remediation 2026-11-15

Verified finding (remediation review P1, item 4, scale half): `ScenarioLoader.ValidateCrewAssignments` capped world crew `accuracySkill`/`maneuverabilitySkill` at the legacy module-operation range 0..30. The tactical skill scale is 0..100 (`Data/TacticalBattle/pilots.json` carries 55/52/53/55/59/54/50/51), so the starter manifest could not record the Board-approved pilot skills without a loader change.

Fix (narrow, reversible): the two range checks widen to 0..100 and the rejection messages say so; no other validation rule changed. Values above 100 and below 0 are still rejected. The starter manifest then carries the Board table (TK-0002 remediation), and owned drones read those values from the persistent crew record (US-0005 TK-0002 remediation).

Named tests (`Ep0029Us0011Tk0001Tests`, temp New Game scenarios):
- `CrewSkillsUpToHundredAreAccepted` — 100/100 (boundary) and 55/52 load and preserve the values.
- `CrewSkillAboveHundredIsRejected` — 101 on either skill still throws `ScenarioException`.

Evidence:
- TDD red: `dotnet test tests/DeepSpaceSaga.Engine.Tests --filter "FullyQualifiedName~Ep0029Us0011Tk0001Tests"` → 1 failed / 8 passed (`CrewSkillsUpToHundredAreAccepted` rejected 100/100 under the 0..30 cap) — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-045-us0011tk0001/red.stdout.txt`.
- Green: same filter → 9/9 passed — `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-045-us0011tk0001/US-0011-TK-0001/focused.trx`, `.../focused.stdout.txt`.
- `dotnet build src/DeepSpaceSaga.Engine --no-restore` → 0 warnings, 0 errors (`.../engine-build.stdout.txt`); scoped `dotnet format --verify-no-changes` → exit 0 (`.../format.stdout.txt`).

Status: automated PASS (engine layer); native/product acceptance remains OPEN.
