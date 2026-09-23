---
epic: EP-0001-trading-system
story: EP-0001-US-0006-repeatable-trading-voyage
ticket: EP-0001-US-0006-TK-0001-round-trip-engine-proof
title: Сквозная проверка двух торговых кругов
stage: approved
layer: engine
depends_on: []
files_touched: 2
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-09-21T11:05:34Z
revision: 1
---

# Сквозная проверка двух торговых кругов

## Why

Отдельно работающие Buy, Dock и движение ещё не доказывают повторяемый рейс. Нужна детерминированная проверка двух полных A → B → A через настоящие Engine handlers с сохранением cargo/Credits, локальности рынка и портовых обязательств.

## Decisions

Отдельных технических решений пользователя нет. Точный запрос создания тикетов и UTC фиксации находятся в story. Зависимости story не меняются.

## Assumptions

- Это integration-proof ticket слоя engine, без изменений production. Исполнять после US-0003/0004/0014 и transitive quote prerequisites. Отсутствие dependency не маскировать skipped test или fake success.
- Два круга доказывают повторяемость, а не прибыльность. Тесты не требуют положительного net profit и не рассчитывают COGS.
- Route fuel model принадлежит US-0009; nullable reservation до её подключения допустим только с явным ограничением evidence.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| tests/DeepSpaceSaga.Engine.Tests/RepeatableTradingVoyageTests.cs | Новый файл; existing engine seams: SimulationEngine.cs:130 ReceiveCommand, :1676 CaptureSnapshotForTests; docking sequence: DialogueTests.cs:20–30 | Сквозные assertions и негативные сценарии по шагам ниже |
| tests/DeepSpaceSaga.Engine.Tests/TradingVoyageFixture.cs | Новый helper; reference patterns ApproachCommandTests.cs:15–80 и DockCommandTests.CreateEngine/CreateRegistry | Построить fixtures и command-driven driver; никакого production helper или runtime mutation после старта |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj`.

## Public API after the change

No API change. Required prerequisites (planned consumer contract, не текущая реализация):
- Undock: ReceiveCommand(new PlayerCommand(id,sequence,playerId,navigationModuleId,"navigation.undock",TargetObjectId:destinationId)).
- Snapshot.ActiveVoyage nullable; properties VoyageId/OriginStationObjectId/DestinationStationObjectId/State и nullable ReservedFuelKg. Unique voyage до completion, затем null. Отказ terminal Rejected с непустым reason.
- Quote: SimulationEngine.GetTradeQuote(new TradeQuoteRequest(requestId,playerId,moduleId,commandType,itemId,quantity)); PlayerCommand добавляет QuoteId и MarketRevision. Команды trade.buy/trade.sell неизменны. TradeReceipt предоставляет StationObjectId/ItemTypeId/ExecutedQuantity/TotalCredits; RequestedQuantity и revisions из US-0003.
- Existing `CaptureSnapshotForTests(long gameTimeMs=0, SimulationSpeed? speed=null, long? simulationTimeMs=null)`, ReceiveDialogueCommand(DialogueCommand), LoadScenario(ScenarioFile,bool isSave=false). Использовать LoadScenario только при начальном создании fixture; повторная загрузка не является перелётом.

## Implementation steps

1. Fixture разрешает реальные Settings/catalog/commands/dialogue и materialized map US-0004, фиксирует seed1. Выбрать A=transit, B=первый по ObjectId её непосредственный сосед в economic graph, допускающий обе торговли. При отсутствии таких товаров явно fail с seed/профилями; не искать удобный seed. Smoke corpus: seeds1,17,42. Все setup overrides хранить в helper, production JSON не менять.
2. Для строгой бухгалтерской проверки отдельный controlled fixture: выбрать два Cargo item с наличием и свободным складом на A/B; quantity1 в каждую сторону, одна designated cargo module. Дать достаточно Credits/stock/budget/capacity и fuel, убрать crew/passenger/production deductions в setup этой fixture. Товары выбираются ordinal по зарегистрированным item IDs, исключая Fuel. Partial case запрашивает3, budget/capacity допускает2. Отдельный real-content test сохраняет реальные состояния и проверяет дельты только на атомарных command boundaries.
3. Общий flight driver использует команды, не SetPosition. После принятого Undock отдельно выполнить engine.accelerate до минимальной положительной скорости; затем navigation.approach, сохранить speed и ApproachRoute.DurationMs. Продвинуть физическое время до конца маршрута; проверить speed неизменна и IsDocked=false. Отдельной engine.speedSynchronization с target station погасить относительную скорость, затем navigation.dock. Для реального dialogue отправить truthful_id → accept_fee → continue через текущие InstanceId/Revision; проверять Enabled и terminal outcomes. Никакого прямого присваивания IsDocked/Cargo/Credits. Ограничить каждый command phase 5000 циклами, каждый leg 30 календарными сутками; timeout — диагностический fail, не fallback teleport. Проверять docking range после синхронизации; неспособность маршрута штатно завершиться — failure prerequisite.
4. Все snapshots используют явные монотонные calendar и physical timestamps, например calendarDelta=300*physicalDelta для speed1 fixture. Не использовать Thread.Sleep или ускорять ship speed вместе с календарём. Внутри paused dialogue время не продвигать. Повторить весь script с другим snapshot cadence; итог торговых receipts/cargo и порядок voyage IDs должны совпадать, сравнивая одинаковые command timestamps.
5. Script одного круга: quote Buy X at A → commit → Undock B → flight/Dock B → fresh quote Sell X → quote Buy Y → Undock A → flight/Dock A → fresh quote Sell Y. Выполнить два круга в одном Engine. На каждом stop сверить player docking id, DockedStationTrade.StationObjectId, quote binding, carried quantity и receipt. Если по реальным stock/budget quantity1 недоступна, не пропускать операцию: сообщить конкретный prerequisite/content failure.
6. После каждого trade зафиксировать before/after и assert cargo/stock delta=ExecutedQuantity, player Credits delta=±TotalCredits, сохранность прочего cargo. Rejected и duplicate ID в том же retained journal не меняют мир. Partial sale оставляет1 единицу; эту ветку проверять отдельно от полного unload script. После ухода отправить сохранённый quote A и новый unquoted trade; оба не меняют рынок/cargo/Credits. По прибытии B подставить quote A — rejection, затем свежий quote B исполняется.
7. Port case: создать долг в initial fixture и проверить rejected Undock сохраняет стоянку/долг/credits/no-new-voyage. Отдельная чистая fixture проходит уход перед next port due, продвигает время за эту границу в flight, проверяет отсутствие port_fee_renewed старой станции и DockedStationTrade=null. После Dock B проверить новый FirstPortFeeGameTimeMs/NextPortFeeDueGameTimeMs, одно списание initial fee и одно renewal по due. Повтор Undock/Dock command ID не создаёт ещё один voyage/fee. Если ReservedFuelKg присутствует, сохранить reservation и fuel до/после retry — они идентичны; не выводить расход из формулы US-0009.
8. Если prerequisite нарушает контракт, оставить тест red и вернуть finding в соответствующую story. Не исправлять US-0014/0003, не добавлять production files в этот merge unit. Ticket завершается только после зелёного evidence на реальных зависимостях.

## Out of scope

Изменения Engine/Contracts/Motion/Client/JSON, новый lifecycle/quote/executor, стоимость рейса, сохранения, баланс и performance artifacts. Нельзя записывать постоянные отчёты/скриншоты вместо assertions.

## Invariants

- Undock сохраняет вектор: Documentation/01-Requirements/EngineRequirements.md:121–129.
- Approach не меняет скорость и не Dock: тот же файл:5333–5341; existing ApproachCommandTests:15–80.
- Calendar/physical раздельны: SimulationEngine.EconomyTime.cs:12–49.
- Port renewal только docked player: SimulationEngine.PortFees.cs:15–45.
- Бюджет станции не раскрывается snapshot: Contracts/StationTradeSnapshot.cs:15–21. Internal test может проверить save/runtime state, Client — нет.

## Tests

Имена в RepeatableTradingVoyageTests:
- `Two_round_trips_complete_without_reloading_or_replacing_cargo` — AC-01/03, минимум8 подтверждённых сделок.
- `Real_mvp_content_completes_return_load_for_fixed_seed_corpus` — AC-01/03.
- `Flight_and_destination_reject_origin_market_quotes` — AC-02.
- `Partial_sale_and_duplicate_command_preserve_exact_remaining_cargo` — AC-03.
- `Port_debt_rejects_departure_without_creating_voyage` — AC-04.
- `Old_port_stops_billing_and_destination_starts_one_new_stay` — AC-04.
- `Duplicate_lifecycle_command_preserves_voyage_and_optional_fuel_reservation` — AC-04.
- `Snapshot_cadence_does_not_change_round_trip_receipts_or_approach_speed` — AC-01/03.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~RepeatableTradingVoyageTests|FullyQualifiedName~ApproachCommandTests|FullyQualifiedName~PortFeeScheduleTests|FullyQualifiedName~TradeCommandTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps реализованы в двух test files, prerequisites доступны; отсутствующий lifecycle не замокан.
- Все served criteria покрыты named tests с реальными Engine commands и bounded driver.
- Test/build/format проходят либо конкретный исходный несвязанный failure записан отдельно; red round-trip test не считается done.
- API/invariants/out-of-scope соблюдены; точные trade deltas доказаны, прибыль/fuel completeness не заявлены.
- Нет незаписанных assumptions, скрытой работы вне Code context или нерешённых блокирующих вопросов.

## Self-containment check

Consumer signatures, последовательность команд/диалога, fixture selection, ограничения времени и все assertions заданы. Dependencies обязаны предоставить описанный контракт до исполнения. Готовность доказательства определяется тестами; production fixes не прячутся в проверочном тикете.
