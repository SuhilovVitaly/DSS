---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0006-station-job-board
ticket: EP-0034-US-0006-TK-0006-feasible-deadlines
title: Выполнимые сроки транспортных заказов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0034-US-0006-TK-0003-engine, EP-0034-US-0005-TK-0001-transport-roundtrip]
files_touched: 4
serves: [AC-02]
created: 2026-11-17T16:10:00Z
revision: 1
---

# Выполнимые сроки транспортных заказов

## Why

Дефект, найденный при EP-0034-US-0005 ([TK-0001 A-02](../../EP-0034-US-0005-transport-continuation/EP-0034-US-0005-TK-0001-transport-roundtrip/EP-0034-US-0005-TK-0001-transport-roundtrip.md)): срок заказа считался от ETA на максимальной скорости (`etaMs = routeKm / speedKmS × 300 × 1000`) с линейным множителем из `jobs.json` (обычный 12000‰, срочный 8000‰, грузовой 12000‰) и минимумом 1 ч. Реальный командный полёт носителя SPC-0005 → SPC-0002 занимает ≈ 9 ч календаря, а срок обычного заказа на этом маршруте — 2,5 ч: заказ невыполним. На коротких маршрутах время полёта определяют накладные расходы манёвра (отстыковка, разгон, разворот на подходе, синхронизация скорости и курса, стыковка), которые линейный множитель ETA не покрывает. Нарушается AC-02 истории: «заказы … выполнимы текущим носителем». [Story](../EP-0034-US-0006-station-job-board.md).

## Decisions

Поручение оркестратора 2026-11-17 (в рамках автономного исполнения EP-0034 по `DB/Board/EpicExecutionPrompt.md`): исправить в Engine минимально и детерминированно; параметры — в `jobs.json` с валидацией; обычный и грузовой заказ выполнимы с запасом ≥ ~25% над реальным временем, срочный — выполним, но теснее обычного; выплаты не менять; принятые заказы и сохранённые доски сохраняют замороженные условия. Human approval баланса не заявляется: значения — baseline для плейтеста EP-0041.

## Problem

- Реальное время полёта на одном и том же маршруте отличается в разы в зависимости от направления (SPC-0002 → SPC-0005 ≈ 73 мин, обратно ≈ 542 мин), а ETA на максимальной скорости — 12,5 мин.
- В Motion/Engine нет функции оценки полного времени рейса с манёврами (есть только `ApproachRoute.DurationMs` уже построенного подхода и `TravelEstimateGameTimeMs` = тот же ETA на максимальной скорости), поэтому оценка — контентная надбавка поверх существующего ETA, без дублирования физики в клиенте.

## Acceptance criteria

- **TK6-AC-01.** Для каждой упорядоченной пары станций демо-кластера, которую может опубликовать доска (прямые связи кластера с известным риском, в обе стороны), реальный командный полёт стартового носителя (отстыковка, три импульса разгона, Approach, синхронизация скорости и курса, стыковка, подтверждённый диалог) укладывается в срок обычного и грузового заказа с запасом ≥ 25%, а в срок срочного — с запасом > 0 и меньше, чем у обычного.
- **TK6-AC-02.** Формула детерминирована и задаётся контентом: `deadline = max(minimum, ceil((maneuver + ceil(ETA × cruise / 1000)) × type / 1000))`; `maneuverGameTimeMs ≥ 0`, `cruisePermille ≥ 1000` валидируются при загрузке; отсутствие полей сохраняет прежнюю формулу (0 мс / 1000‰).
- **TK6-AC-03.** Уже опубликованные офферы и принятые обязательства сохранённой игры после Load сохраняют свои замороженные сроки; пересчёт идёт только для новых публикаций.
- **TK6-AC-04.** Выплаты не меняются (формула выплаты от ETA не зависит); регрессии историй EP-0034 без новых падений относительно baseline.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.StationJobs.cs` | `BuildStationJobOffer` считает срок inline от ETA максимальной скорости (`:116-142` на `34825dce`). | Вынести расчёт в `StationJobDeadlineMs` с надбавкой манёвра и масштабом крейсерской части; internal `StationJobDeadlineForTools` для проверок. |
| `src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs` | `DemoJobDeadlines(OrdinaryPermille, UrgentPermille, CargoPermille, MinimumGameTimeMs)` + валидация. | Опциональные `ManeuverGameTimeMs = 0`, `CruisePermille = 1000` и их валидация. Файл общий с EP-0033 — только аддитивная правка. |
| `src/DeepSpaceSaga.Client/Data/Demo/jobs.json` | `deadlines`: 12000/8000/12000‰, минимум 1 ч. | Новые значения сроков и параметры оценки рейса. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0034Us0006Tk0006Tests.cs` | Новый файл этого тикета. | Воспроизведение реальным полётом, формула, валидация, замороженные сроки при Load. |

Board: эта карточка, индекс истории, `Documentation.md` эпика (26 → 27 тикетов), `ExecutionStatus.md`. Отдельного `Tickets.md` у эпика нет (индекс тикетов — в story-картах и `Documentation.md`).

## Implementation steps

1. Тест-воспроизведение: для всех публикуемых пар реальный полёт против сроков, которые публикует доска (helper сверяется со всеми живыми офферами доски).
2. `StationJobDeadlineMs(deadlines, type, routeKm, speedKmS)`: оценка рейса = `maneuver + ceil(ETA × cruise / 1000)`, затем множитель типа, минимум. Целочисленная арифметика с `checked`, ETA — прежний (`routeKm / speedKmS × 300 × 1000`).
3. `jobs.json`: `maneuverGameTimeMs` 32 400 000 (9 ч), `cruisePermille` 3200, `ordinaryPermille` 1300, `urgentPermille` 1100, `cargoPermille` 1300, минимум 1 ч без изменений.
4. Load не трогает: `DemoCampaignData` валидирует только `DeadlineDurationMs > 0`, без пересчёта (проверено тестом).

## Tests

- `DemoEp0034Us0006Tk0006Tests.PublishedDeadlines_AreFeasibleForRealFlights` — TK6-AC-01 (падал на `34825dce`).
- `DemoEp0034Us0006Tk0006Tests.DeadlineFormula_AddsManeuverAllowanceAndCruiseScale` — TK6-AC-02.
- `DemoEp0034Us0006Tk0006Tests.InvalidFlightEstimate_RejectsContent` (×2) — TK6-AC-02.
- `DemoEp0034Us0006Tk0006Tests.SavedOffersAndObligations_KeepFrozenDeadlinesOnLoad` — TK6-AC-03.

## Execution result (2026-11-17)

Status: implemented, focused + story-area regressions run (evidence `D:/DeepSpaceSaga/artifacts/validation/EP-0034/tk0006-deadlines-20261117T155842/`). Native/product gates — OPEN (баланс сроков подтверждается плейтестом EP-0041).

Воспроизведение (`repro.log`, код `34825dce`, реальный полёт vs срок обычного/срочного/грузового, минуты календаря):

| Маршрут | Полёт | До: обыч./сроч./груз. | После: обыч./сроч./груз. |
|---|---:|---|---|
| SPC-0002 → SPC-0005 | 73,3 | 150 / 100 / 150 | 754 / 638 / 754 |
| SPC-0005 → SPC-0002 | 541,7 | **150 / 100 / 150** | 754 / 638 / 754 |
| SPC-0005 → SPC-0006 | 552,8 | 810 / **540** / 810 | 982,8 / 831,6 / 982,8 |
| SPC-0006 → SPC-0005 | 458,0 | 810 / 540 / 810 | 982,8 / 831,6 / 982,8 |
| SPC-0006 → SPC-0007 | 815,6 | 1130 / **753,3** / 1130 | 1093,7 / 925,5 / 1093,7 |
| SPC-0007 → SPC-0006 | 499,4 | 1130 / 753,3 / 1130 | 1093,7 / 925,5 / 1093,7 |
| SPC-0006 → SPC-0008 | 561,8 | 1000 / 666,7 / 1000 | 1048,7 / 887,3 / 1048,7 |
| SPC-0008 → SPC-0006 | 561,8 | 1000 / 666,7 / 1000 | 1048,7 / 887,3 / 1048,7 |
| SPC-0002 → SPC-0008 | 871,0 | 1800 / 1200 / 1800 | 1326 / 1122 / 1326 |
| SPC-0008 → SPC-0002 | 871,0 | 1800 / 1200 / 1800 | 1326 / 1122 / 1326 |
| SPC-0005 → SPC-0008 | 889,7 | 1810 / 1206,7 / 1810 | 1329,5 / 1124,9 / 1329,5 |
| SPC-0008 → SPC-0005 | 858,5 | 1810 / 1206,7 / 1810 | 1329,5 / 1124,9 / 1329,5 |
| SPC-0007 → SPC-0008 | 877,6 | 1970 / 1313,3 / 1970 | 1384,9 / 1171,9 / 1384,9 |
| SPC-0008 → SPC-0007 | 1030,1 | 1970 / 1313,3 / 1970 | 1384,9 / 1171,9 / 1384,9 |

До: 5 нарушений (SPC-0005 → SPC-0002 все три типа; срочный SPC-0005 → SPC-0006 и SPC-0006 → SPC-0007). После: минимальный запас обычного/грузового — 34% (SPC-0006 → SPC-0007), срочного — 13,5% (там же); срочный всегда теснее обычного.

Проверки (каждый процесс ≤ 300000 ms, корень `D:/DeepSpaceSaga/Dss-Main`, `-c Release`):

- `DemoEp0034Us0006Tk0006Tests`: до исправления 1 failed (`repro.log`), после — 5/5 (`own.log`); `DemoEp0034|DemoEp0032` Engine 50/50 (`engine-final.log`).
- Contracts `Demo.|ArchitectureTests|Ep0029` → 38 passed (`contracts.log`).
- Engine фильтр US-0004/0005 → 382 passed / 8 failed (`engine-reg.log`); 8 = baseline (`engine-failed.txt` совпадает с US-0005).
- Engine торговля → 316/0 (`engine-trade.log`); save/load → 270 passed / 21 failed = baseline (`engine-save-failed.txt`); `EconomyTimeContinuityTests` 7 + `ClusterVoyageIntegrationTests` 7 failed = baseline (`econ-cluster-failed.txt`).
- Client фильтр US-0004/0005 → 511 passed / 33 failed (`client-reg.log`): 32 = baseline (30 + 2 `NoAmmo`); 33-й — `DemoEp0032Us0004Tk0004Tests.PublicProfileUsesDemoForTutorialAndHidesDevelopmentScenarios`, не связан с тикетом: тест читает жёстко заданный соседний checkout `D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client`, который сейчас на ветке `codex/ep-0035-demo-pirate-encounters` (его demo `scenario.json` отличается от этой ветки), и не использует изменённые файлы (известная проблема hard-code пути из ExecutionStatus).
- Client save/load → 106 passed / 3 failed = baseline (`client-save-failed.txt`).
- `dotnet format --verify-no-changes` для трёх изменённых `.cs` — 0; `git diff --check` — чисто.

Изменённые тесты других историй: нет. Ожидания US-0002/0003/0004/0005/0006 опираются на `offer.DeadlineDurationMs` и порядок по сроку внутри типа, а не на старую формулу; все проходят без правок.

Assumptions:

- A-01 (technical): «реальный полёт» — пилот тестов US-0004/US-0005 (три импульса, Approach, синхронизация, стыковка) из пристыкованного состояния с курсом 0° (как после настоящей стыковки) в момент 0 демо. Пилот на максимальной скорости летит дольше из-за радиуса разворота (US-0002 TK-0002 A-04) — это решение игрока, срок его не покрывает.
- A-02 (technical): оценка — фиксированная надбавка + масштаб ETA, без учёта направления: обратные короткие маршруты получают щедрый срок (SPC-0002 → SPC-0005: 754 мин при полёте 73 мин). Геометрическая оценка разворота отложена до плейтеста EP-0041; функции полной оценки рейса в Motion нет.
- A-03 (technical): расстояния внутри кластера стабильны (A-03 TK-0003), поэтому проверка в момент 0 репрезентативна для времени жизни оффера.
- A-04 (technical): прежние калибровки множителей (US-0002 TK-0002 A-04: 12000/8000‰; US-0004: груз 2500 → 12000‰) заменены этим тикетом; их записи сохранены как история.
- A-05 (technical): формат сохранения и fingerprint кампании не меняются (fingerprint покрывает только `cluster.json`); новые поля `jobs.json` опциональны.
