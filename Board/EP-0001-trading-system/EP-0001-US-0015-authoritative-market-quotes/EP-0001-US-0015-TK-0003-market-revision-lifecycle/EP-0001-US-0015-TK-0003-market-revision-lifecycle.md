---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
ticket: EP-0001-US-0015-TK-0003-market-revision-lifecycle
title: Жизненный цикл market revision
stage: approved
layer: engine
depends_on: [EP-0001-US-0015-TK-0001-trade-quote-contract, EP-0001-US-0002-TK-0003-hourly-market-simulation]
files_touched: 5
serves: [AC-04, AC-05, AC-07]
created: 2026-09-21T15:10:56Z
revision: 1
---

# Жизненный цикл market revision

## Why

Quote freshness требует сохранённой монотонной revision на каждый профильный рынок и единого commit helper для всех stock/budget/event mutations. Revision должна меняться один раз на транзакцию, не на каждую строку, и никогда — при rejection/no-op. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

D-01, 2026-09-21T15:10:56Z: исходный запрос записан в story. Начальное значение и lifecycle следуют assumptions A-01/A-07 story; отдельного пользовательского решения не поступало.

## Assumptions

- Profile market начинается с revision 1. Legacy/no-profile station хранит null и не получает synthetic revision.
- Missing revision у legacy save с profile мигрирует в 1; explicit negative/zero profile revision и non-null legacy revision invalid до world replacement.
- Одна атомарная операция станции (trade или один hourly pass) повышает revision на 1, даже если меняет несколько stock rows и budget. No-op не повышает.
- Existing trade mutation временно подключается к revision; US-0003 позже заменяет commit path, сохраняя helpers/signatures.
- Event lifecycle ещё не реализован. Helper принимает общий `stationObjectId`, поэтому US-0007 обязана вызывать его при start/end/effect change; текущий ticket тестирует explicit event-mutation seam без создания события.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :25–35 world fields; :560–598 projection; :785–813 save; :1995–2168 trade commits; :3233–3299 runtime station | MarketRevision runtime/load/save/projection; Next/Commit helpers; increment existing trade transaction |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | :12–51 central calendar boundary processing; US-0002 extends hourly market | Prepare one next revision and commit once iff station market changed |
| src/DeepSpaceSaga.Engine/SimulationEngine.ProductionTime.cs | :13–62 mutates multiple inventory rows through production | Return/aggregate changed flag; no per-row revision increment |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :137–207 station save fields end at MarketProfileId/MarketBudgetCredits | Add nullable marketRevision serialized field with migration semantics |
| tests/DeepSpaceSaga.Engine.Tests/MarketRevisionTests.cs | отсутствует | Initial/change/no-op/overflow/save-load/legacy tests |

## Public API after the change

Internal runtime/save shape:

```csharp
// trailing field on station SpaceObjectData
[property: JsonPropertyName("marketRevision")] long? MarketRevision = null

// trailing field on SpaceObjectRuntime
long? MarketRevision = null
```

Required partial-class seam for TK-0004 and US-0003:

```csharp
private long NextMarketRevision(string stationObjectId);
private void CommitMarketRevision(string stationObjectId, long nextRevision);
```

`NextMarketRevision` выполняет lookup/profile/revision checks и `checked(current + 1)`, но не мутирует. `CommitMarketRevision` не делает fallible arithmetic/lookup: caller передаёт уже разрешённый station index либо helper сохраняет безопасно подготовленный index; в любом случае после начала world commit он присваивает prepared value и вызывает partial hook инвалидатора quote cache. Чтобы точная сигнатура US-0003 с `stationObjectId` сохранилась, допустим private prepared dictionary/index, но нельзя делать после первой state assignment новый lookup, который может бросить.

Projection: `new StationTradeSnapshot(stationId, items, station.MarketRevision)`. Save хранит revision; runtime quote tokens/cache не сохраняются.

## Implementation steps

1. Добавить nullable revision в schema/runtime/projection/save. При load: profile market missing revision→1; profile explicit `<1` reject; legacy profile null + revision non-null reject. Все validation происходит до `_objects` replacement.
2. Реализовать prepare/commit seam. Overflow в Next вызывает zero-effect failure; Commit присваивает ровно prepared next и вызывает invalidation hook без арифметики.
3. В существующем trade path подготовить next только после всех validations, до mutation; commit revision после staged assignments. Rejected/no-op не меняет revision. US-0003 сможет использовать тот же seam без изменения contract.
4. В US-0002 hourly pass aggregate stock/budget changed per station. Production/consumption/budget recovery одной границы дают один increment. Empty interval/no effective delta/blocked output не меняют revision.
5. `SimulationEngine.ProductionTime` сообщает факт фактического stock delta caller-у; не увеличивает revision внутри `ChangeStationStock` каждой строки.
6. Добавить explicit internal hook для будущего event state commit и тест, что event-affecting caller повышает revision/invalidates один раз. Не реализовывать lifecycle/каталог US-0007.
7. Проверить save/load continuity, legacy migration, max-value overflow и snapshot null/positive shape.

## Out of scope

QuoteId/cache implementation кроме invalidation hook, price formula, event scheduling/content, command binding/receipt, UI, полная save migration US-0012 и изменение economy rates.

## Invariants

- Absolute calendar time остаётся источником экономики: `EngineRequirements.md:236–252`; MotionTime не меняется.
- Save уже переносит inventory/events/profile: `SimulationEngine.cs:785–813`; cache никогда не сериализуется.
- Текущие trade assignments происходят после validation: `SimulationEngine.cs:2057–2167`; новый revision commit не должен нарушить stage-before-commit.
- Future US-0003 ожидает точные helpers `NextMarketRevision`/`CommitMarketRevision`: `Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-dynamic-market-trading.md`, раздел Integration contract.

## Tests

`MarketRevisionTests`:

- `Profile_market_starts_at_one_and_legacy_market_projects_null` — AC-05.
- `Successful_trade_increments_once_but_rejected_trade_and_noop_do_not` — AC-04.
- `Hourly_multi_item_stock_and_budget_change_increments_once_per_station` — AC-04.
- `Production_without_effect_does_not_increment_revision` — AC-04.
- `Prepared_revision_overflow_leaves_world_unchanged` — AC-04.
- `Revision_round_trips_save_load_but_runtime_invalidation_state_does_not` — AC-05.
- `Legacy_profile_save_missing_revision_migrates_to_one_and_invalid_values_are_rejected` — AC-05.
- `Event_change_seam_commits_one_revision_without_implementing_event_lifecycle` — AC-04/07.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketRevisionTests|FullyQualifiedName~StationEconomyBatch2Tests|FullyQualifiedName~TradeCommandTests|FullyQualifiedName~SaveLoadContinuityTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все изменения только в пяти разрешённых файлах; `files_touched` соблюдён.
- AC-04/05/07 покрыты named tests.
- Focused tests/build/format проходят либо baseline failure записано отдельно.
- Revision меняется один раз на effective transaction, сохраняется и не маскирует overflow/no-op.
- API/invariants/out-of-scope соблюдены; quote cache не попал в save.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы вне Code context.

## Self-containment check

Заданы initial/migration rules, exact helpers, commit ordering, все текущие mutation points и event hook. Implementer не должен искать, что считать одним revision change, или расширять save/UI scope.

