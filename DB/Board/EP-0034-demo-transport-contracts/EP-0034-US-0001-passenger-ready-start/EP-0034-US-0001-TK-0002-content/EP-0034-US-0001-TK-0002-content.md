---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0001-passenger-ready-start
ticket: EP-0034-US-0001-TK-0002-content
title: Контент: Готовый носитель с пассажирскими местами
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0034-US-0001-TK-0001-contracts]
files_touched: 6
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Контент: Готовый носитель с пассажирскими местами

## Why

Готовый носитель с пассажирскими местами. Тикет 2/5 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0001-passenger-ready-start.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0001-TK-0001-contracts. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` | Новый файл этого тикета; на момент grounding отсутствует. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json` | Планируемый файл; создаётся в `EP-0032-US-0004-TK-0002-content`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0001Tk0002Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

Версионированный JSON существующая starterCarrierManifest schemaVersion=1

## Implementation steps

1. Производный demo manifest содержит те же идентичности стартовой команды и крыла, плюс 4 места; не менять shared starter других сценариев.
2. Проверить размещение 12 кают (6 crew + 2 reserve + 4 passengers), массу и питание штатным loader; ссылка только из Demo scenario.

## Served acceptance criteria

- **AC-01:** В новой демо-игре видны экипаж, резерв и положительное число свободных пассажирских мест.
- **AC-02:** Комплектация допустима по корпусу, массе, размещению и жизнеобеспечению; другие сценарии и существующие saves не получают бесплатного расширения.
- **AC-03:** Проверить физический первый рейс и доступный заказ без предварительной покупки жилья.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `content-data`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие тесты (ещё не реализованы/не запущены):

- `DemoEp0034Us0001Tk0002Tests.Content_LoadsAndReferencesResolve` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0001Tk0002Tests.DemoLimits_AreSatisfied` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0001Tk0002Tests.LegacyScenario_DoesNotReceiveDemoGrant` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0001-TK-0002-content/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0034Us0001Tk0002Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0001Tk0002Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: В новой демо-игре видны экипаж, резерв и положительное число свободных пассажирских мест. AC-02: Комплектация допустима по корпусу, массе, размещению и жизнеобеспечению; другие сценарии и существующие saves не получают бесплатного расширения. AC-03: Проверить физический первый рейс и доступный заказ без предварительной покупки жилья. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Native запуск — только после проверки display layout, на меньшем мониторе с isolated profile/explicit index и `DSS_VALIDATION_SMALL_MONITOR=1`; иначе NOT RUN. Автотест не закрывает native, слуховую, независимую или продуктовую приёмку.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.

## Allowlist expansion (recorded before the change)

- `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0004Tk0002Tests.cs` — EP-0032 regression asserted that the demo references the shared `starter-carrier.json`; this ticket deliberately changes that contract. The test also read content from a hard-coded sibling checkout `D:/DeepSpaceSaga/DSS`, so in this checkout it would validate foreign content; it now resolves the content root from its own source path. Renamed to `DemoUsesFiveFrozenStationsAndDemoStarter`; the five-station/seed assertions are kept unchanged.

## Execution result (2026-11-17)

Status: implemented; **validation pending at story end** (owner cadence). No test PASS claimed.

- `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` — new schemaVersion 1 manifest, byte-identical to the shared starter except one module `MOD-PLAYER-LIVING-QUARTERS-03` (`living.quarters.mk1.four-cabins`, cell (4,1), the free hull cell between both existing quarters). Result: 12 cabins = 6 crew + 2 reserved + 4 free for passengers. Same crew IDs, wing, drones, hold (200 food rations = 24 meals for 12 people at 2 meals/day → ≥ 1 day full complement).
- `src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json` — only `starterCarrierManifest` now points to the demo manifest.
- `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0001Tk0002Tests.cs` — `Content_LoadsAndReferencesResolve`, `DemoLimits_AreSatisfied` (production engine load: placement/overlap validated, 12/6/2/4, module mass 2000 kg and 0 W, rations), `LegacyScenario_DoesNotReceiveDemoGrant` (Tutorial/Default stay at 8/0).
- `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0004Tk0002Tests.cs` — see expansion above.

Checks: `dotnet build` of Client.Tests and Engine.Tests (Release) — succeeded, 0 warnings/0 errors. Logs: `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0001-20261117/tk0002-*.log`.

Assumptions/limits:

- A-03: the Tetrarch class has no hull mass limit in the current engine; "mass" admissibility means the module is a real catalog module with its catalog mass, not a free weightless cabin. Life support = food rations (power draw of living quarters is 0 W).
- Shared starter, Tutorial and Default scenarios are unchanged; existing Save22 demo saves carry their expanded 8-cabin carrier and receive nothing (manifest is resolved only on a version-0 New Game). The cluster fingerprint does not include the manifest, so no save-version bump.
- Other EP-0032 tests still read `D:/DeepSpaceSaga/DSS` via `DemoEp0032Us0001Tk0002Tests.ContentRoot` (pre-existing); not changed here — recorded for the epic report.

## Story-end correction (2026-11-17)

Allowlist expansion (recorded before the change): `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs` and `tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs`. Their "real default scenario" tests load `Settings.defaultScenario`, which is the demo since EP-0032; the demo New Game now has 10 modules, 3 four-cabin quarters (12 cabins, 4 free) and 16 of 24 occupied hull cells. Expectations were updated to the new agreed contract (`Real_default_scenario_occupies_15_of_24_hull_cells` renamed to `..._16_of_24_...`, now also asserting cell (4,1)); the shared 8-cabin starter stays covered by Tutorial/Default assertions in `DemoEp0034Us0001Tk0002Tests` and the EP-0029 Tutorial tests.

Story-end results: see the story card «Story validation».

Story-end validation (2026-11-17): focused tests of this ticket PASS — see the story card «Story validation». Native/product acceptance remains OPEN.
