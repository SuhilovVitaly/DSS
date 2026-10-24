---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
ticket: EP-0001-US-0002-TK-0003-hourly-market-simulation
title: Почасовая экономика и ограниченный склад
stage: approved
layer: engine
depends_on: [EP-0001-US-0002-TK-0001-market-stock-snapshot, EP-0001-US-0002-TK-0002-market-economy-schema]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-05, AC-07]
created: 2026-09-21T08:59:06Z
revision: 1
---

# Почасовая экономика и ограниченный склад

## Why

Рынок изменяется по календарю независимо от частоты snapshot и ускорения, ограничивает склад/бюджет и сохраняет произведённый остаток. Это один Engine merge unit: hooks, mutations, projection и save используют общий набор правил.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Все относительные пути ниже от D:/DeepSpaceSaga/DSS. Сначала реализовать US-0001 и два prerequisite-тикета этой истории.

## Decisions

D-01, 2026-09-21T08:59:06Z: «сделай следующую стори». Отдельных пользовательских ответов о деталях реализации нет.

## Assumptions

- Profile hourly batch атомарен: все inputs есть и весь output помещается, иначе batch пропускается без списаний. Независимый consumption уменьшает каждый stock на min(stock,rate).
- Modules использует существующие recipe durations/inputs. Completed output выдаётся частично, остальное остаётся PendingOutput; новый цикл ждёт его полного опустошения.
- MarketBudgetCredits — доступная часть Credits, cap не уничтожает выручку. Credits не раскрывается и может быть выше cap. Сборы и dialogue не требуют изменений.
- Часовой tick привязан к абсолютной сетке GameCalendar.HourMs от 0; rates не масштабируются размером, targets/budget масштабируются по US-0001 SizeFactors.
- PendingOutput flush не запускается таймером UI; состояние изменяется только внутри Engine календарного прохода. Пока GameTimeMs не продвигается, повторные snapshots не выгружают output заново.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :196–339 bootstrap/cursors; :549–586 projection; :756–820 save; :873 save modules; :1302–1317 resolver; :1937–2048 trade; :3189 runtime producing module | Budget/runtime state, preflight, projection, bounded Sell, save/load hooks |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | :12–51 общий календарный loop/порядок | Включить next hour и ordered market effects в существующий loop |
| src/DeepSpaceSaga.Engine/SimulationEngine.ProductionTime.cs | :13–62 recipe start/completion без складского cap | Защита single source, pending-output lifecycle, bounded recipe output |
| src/DeepSpaceSaga.Engine/SimulationEngine.MarketTime.cs | Новый partial файл | Все новые market helpers: limits, hourly flow, band, budget и preflight |
| tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs | :13–43 clock seam/equivalent intervals; :74–116 roundtrip/production | Новые bounded market fixtures и named tests в этом файле; существующие tests сохраняются |

## Public API after the change

Новых публичных команд нет. Dependency TK-0001 расширяет StationInventoryItemSnapshot optional TargetStock/MaxStock/FreeStockCapacity/StockState. TK-0002 расширяет profile.Economy (Source, Inputs/Outputs/Consumption, StockTargets, thresholds, divisor), SpaceObjectData.MarketBudgetCredits и StationProducingModuleData.PendingOutput; SaveFormat=9.

В runtime record SpaceObjectRuntime (SimulationEngine.cs) добавить long? MarketBudgetCredits = null. В StationProducingModuleRuntime добавить ImmutableArray<StationInventoryItemRuntime> PendingOutput = default. Это только Engine runtime; сохранение использует стабильные item IDs, не индексы.

Сохраняемые поля v9:
- economy-enabled station: marketProfileId, marketProfileFingerprint (US-0001), marketBudgetCredits, explicit credits/stock/size;
- producing module: nextProductionDueGameTimeMs либо pendingOutput, не оба одновременно; pending quantity>0, элементы уникальны и принадлежат его recipe outputs, количество не выше одного recipe output batch.
- no economy → MarketBudgetCredits null; без pending → null/empty.
- общий календарный cursor уже восстанавливается из GameState.GameTimeMs, новый lastTick/системное время не нужны.

## Implementation steps

1. В новом partial реализовать helpers для поиска Economy по station.MarketProfileId, effective Target/Max, MaxBudget, stock state и validation. Target = RoundAwayFromZero(baseTarget×sizeFactor/1000); MaxStock=2×target; MaxBudget=2×RoundAwayFromZero(profile.InitialCredits×sizeFactor/1000). Проверять overflow checked decimal/long.
2. В LoadScenario preflight построить/проверить candidate world до замены текущего. Economy-enabled inventory должен иметь targets для каждого Cargo entry, в том числе explicit extras, stock в [0,max]. Не добавлять target или обрезать explicit values молча; missing target/overflow → ScenarioException с station/profile/item/field. Fuel excluded. Неотрицательные explicit Credits сохраняются целиком.
3. New game budget=min(Credits,MaxBudget). Saved v9 economy требует marketBudgetCredits в [0,min(Credits,MaxBudget)], полный explicit inventory/profile stamp. Reject неподходящую версию/stamp/missing budget, runtime metadata у non-Station/no-economy; сохраняя старые no-economy v0–8 paths. Explicit marketBudgetCredits в new-game без saveFormatVersion не принимать как балансный override.
4. Проверить productionSource: Profile не допускает producingModules с Active=true, due или pending; inactive idle metadata можно оставить. Modules требует все recipe input/output ID в bounded cargo targets и отсутствие Fuel; HourlyInputs/Outputs пусты по TK-0002. HourlyConsumption не может пересекать inputs ни одного объявленного recipe, даже inactive, чтобы активация не включала двойной расход. Profile batch и recipes никогда не работают для одного enabled рынка одновременно.
5. В EconomyTime loop IncludeBoundary(NextMarketHourTime()) только если есть хотя бы одна economy-enabled station. Boundary строго в (processed,target], overflow-safe; конец диапазона long.MaxValue не считать «достигнутым часом», если это sentinel. На каждом next после AdvanceMotionTo и CompleteProduction вызвать ApplyMarketHour(next) только при next%HourMs==0; затем FlushPendingOutputs, meal, fee, deadline, commit cursor. Перед StartAvailableProduction на каждой итерации также пытаться FlushPendingOutputs. Existing AdvanceMotionTo mapping не менять.
6. ApplyMarketHour обходит станции ordinal ObjectId, profile items ordinal ItemTypeId. Сначала HourlyConsumption min(stock,rate), затем Profile batch (либо ничего для Modules), затем budget refill. В Profile batch проверять все inputs, after-input/after-output amounts и capacity до единого commit. Inputs и outputs профиля disjoint по US-0001; не расходовать inputs при хотя бы одном полном output. Нет fractional rates и попыток догнать пропущенный из-за shortage batch.
7. Recipe path: StartAvailableProduction сначала ждёт PendingOutput empty, затем прежняя atomic all-inputs проверка и списание ровно раз; due остаётся recipe.CycleDurationMs. При completion объединять дубликаты output item IDs checked, выдать min(output,free capacity) и записать остаток. Сбросить due, не стартовать следующий цикл, пока pending непуст. Flush pending работает только для Active=true; выключенный модуль сохраняет pending до повторной активации. Flush уменьшает его на реально выданное; следующее completion не создаёт тот же output повторно. Empty/default immutable arrays обрабатывать безопасно.
8. Recipe input/output capacity ограничивать только для opted-in economy. Для старой no-economy станции сохранить прежний unbounded recipe behavior. Validate сохранённый pending против recipe и source; wrong ID, qty<=0, duplicate, pending+due, pending больше одного batch или pending в Profile mode → fail before world mutation. При проверке upper quantity учитывать агрегат duplicate recipe outputs; loader registry уже проверяет item references.
9. Sell: существующий whole-unit executedQty ограничить min(requested, cargo quantity, affordable budget units, free stock capacity), сохраняя прежние правила валидации запроса/наличия cargo. Для economic station affordable = MarketBudgetCredits/unitPrice; для остальных прежние Credits/unitPrice. Нулевой free capacity отклоняет без изменения состояния (reason "station_stock_full"); positive partial success возвращает ExecutedQuantity. Уменьшить Credits и MarketBudgetCredits на одну и ту же proceeds, увеличить stock только на executed.
10. Buy/Refuel: прежние проверки/cargo/tank mutations; Credits += полная cost, MarketBudgetCredits=min(MaxBudget,oldBudget+cost), вычислять как oldBudget+min(headroom,cost) без overflow. Прочие incoming credits (fees/dialogue) остаются в общей кассе, budget от них непосредственно не меняется; не модифицировать эти production files.
11. Budget refill: dailyGrant=floor(MaxBudget/BudgetRegenerationDivisorPerDay). Для часа h в 1..24 текущих календарных суток grant=floor(h×dailyGrant/24)−floor((h−1)×dailyGrant/24), где на границе полуночи h=24. Applied=min(grant,MaxBudget−Budget). Затем Budget+=Applied; Credits=max(Credits,newBudget): сначала используется имеющийся резерв, только нехватка покрывается refill. Никакого backfill пропущенных при cap grants. Это не MaxBudget/24 каждый час. Полный дневной прирост от replenishment не более floor(MaxBudget/24).
12. Projection для bounded Cargo: TargetStock/MaxStock/FreeStockCapacity и state из profile thresholds; compare 1000m×stock с threshold×target, строго < shortage / > surplus, равенство Normal. MaxSellableQuantity=min(Budget/unitPrice,free capacity). Fuel/legacy поля null; скрытые Budget/Credits не добавляются в snapshot.
13. CaptureSaveState пишет MarketBudgetCredits и PendingOutput. BuildSaveProducingModule / ResolveStationProducingModules переводят item indices↔IDs через registry. Сохранение после частичной выгрузки и load не генерирует output повторно. Existing profile fingerprint из TK-0002 проверять до восстановления. Не хранить копии profile/recipe type definitions в save.
14. Покрыть tests ниже и весь Engine project. Ошибки runtime checked arithmetic не должны оставлять половину market batch или trade; сначала вычислять candidate values, затем commit.

## Out of scope

Contract/data/Client edits; pricing curve/revision, ECS-фабрики корабля, изменение портовых платежей или движения, автоматическое пополнение Fuel, миграция несовместимых профилей. Не добавлять новый scheduler вместо AdvanceWorldTo.

## Invariants

- Реальное время не определяет market tick: SimulationClock.cs:51–60; Speed0 modal — EngineRequirements.md:1121–1133.
- Manual TravelStation уже использует AdvanceWorldTo и idempotent receipt: SimulationEngine.Time.cs:31–54.
- Recipe inputs/pending соответствуют EngineRequirements.md:3844–3868.
- Credits не исчезают из-за budget cap; station incoming paths зафиксированы SimulationEngine.cs:1957,2040, SimulationEngine.PortFees.cs:42–45, Dialogue/DialogueEffectTransaction.cs:53.
- Existing prices/consumed resources не переопределять новыми profile inputs (EngineRequirements.md:5227,5243–5265).

## Tests

В EconomyTimeContinuityTests добавить fixtures с profile.Economy, clock injection и готовым cargo/tank. Не создавать новые test files.

- Hour_boundary_applies_profile_batch_and_consumption_once (AC-02/04): H−1, H, H повторно, H+1; exact inputs/outputs и next-hour результат.
- Missing_input_or_output_capacity_skips_entire_profile_batch (AC-02/03): ни одного частичного списания.
- Transit_consumes_only_available_stock (AC-02/03).
- Modules_source_completes_once_without_profile_double_count (AC-03): mode conflict preflight rejected; действующий recipe единственный producer.
- Full_recipe_output_becomes_pending_and_blocks_next_cycle (AC-03/07): full stock, completion, pending; Buy освобождает часть, следующий календарный шаг выгружает только вместившееся; повторный snapshot не удваивает output.
- Pending_output_and_budget_survive_save_load_at_hour_boundary (AC-04/07): save до/на/после H и после partial unload; сравнить с непрерывным run.
- Normal_accelerated_chunked_and_manual_time_produce_identical_markets (AC-04): injected clock Speed1/Speed4 до одного GameTimeMs; один большой capture vs много малых; TravelStation Dock→Market→Dock с уникальными receipts, одинаковый горизонт, snapshot stock/budget/pending одинаковы. Использовать GameTimeMultiplier для real interval, не умножать MotionTimeMs на calendar factor.
- Paused_real_time_does_not_advance_market (AC-04): менять fake realMs при Speed0, повторять capture, ничего не меняется; отдельная explicit TravelStation продвигает ровно час.
- Daily_budget_grant_is_not_multiplied_twenty_four_times (AC-05): max19200 → daily800; 24 grants суммарно800; non-divisible case проверяет integer distribution; cap discards unused grants.
- Trade_budget_preserves_revenue_and_fee_reserve (AC-05): Budget≤MaxBudget≤возможные Credits; income сверх cap не теряется; Sell уменьшает обе суммы; refill сначала расходует свободную кассу и не удваивает её.
- Sell_respects_stock_headroom_and_reports_partial_quantity (AC-03/05): free=3/request=5, budget достаточен, executed=3; exact cargo/stock/credit deltas. Full stock rejects.
- Snapshot_stock_bands_and_limits_match_authoritative_state (AC-03/05): target100, stock49/50/150/151/200; shortage/normal/normal/surplus/surplus; Fuel/legacy null.
- Invalid_economy_save_does_not_replace_loaded_world (AC-07): bad stamp/missing budget/invalid pending/explicit overcap.
- Existing_legacy_production_and_motion_are_unchanged (AC-04): existing fixtures и clock mapping сохраняют результаты.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в разрешённых Code context файлах; лимит files_touched соблюдён.
- Каждый criterion из serves покрыт указанными named tests в пределах вклада тикета.
- Named tests и build/lint соответствующего layer проходят; исходные failures записаны с точной командой и сообщением.
- Public API, invariants, out-of-scope и prerequisite contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов и скрытых изменений за allowlist.
- End state проверяется тестом, diff или описанным наблюдаемым поведением; planning status не означает выполненный production-код.

## Self-containment check

Пять файлов покрывают все runtime touchpoints, включая trade/projection/save и существующие recipes. Даны batch semantics, порядок одинаковых timestamps, integer refill, pending lifecycle, backward compatibility и точные state-based tests. End state: каждый календарный час меняет ограниченный рынок один раз, а разные способы продвижения времени/SaveLoad дают одинаковый результат.
