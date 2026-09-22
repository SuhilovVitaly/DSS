---
epic: EP-0001-trading-system
story: EP-0001-US-0009-voyage-fuel-cost
title: Фактический расход топлива торгового рейса
stage: approved
dependencies: [EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0014-voyage-lifecycle]
created: 2026-09-21T12:38:06Z
source_request: "создай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0009-voyage-fuel-cost\\EP-0001-US-0009-voyage-fuel-cost.md"
current_review: complete
revision: 2
---

# Фактический расход топлива торгового рейса

## Входное техническое задание

Точное сообщение пользователя: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0009-voyage-fuel-cost\EP-0001-US-0009-voyage-fuel-cost.md». Контекст зафиксирован 2026-09-21T12:38:06Z UTC; время отправки сообщения недоступно.

Исходный scope: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:137–147,207–219`; `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:20–35,403–415,475–487`; эпик `../Documentation.md:97–104,147`. Все пути ниже относительны `D:/DeepSpaceSaga/DSS`; текущий код отделён от планируемых dependency API.

## User story

Как игрок, я хочу учитывать действительный расход топлива межстанционного рейса, чтобы сравнивать направления по затратам и доступному запасу. Перед принятым Undock Engine резервирует рассчитанное количество топлива из баков корабля. При успешном прибытии весь резерв становится расходом рейса; при прерывании расходуется только доля по progress, а остаток возвращается в исходные баки. Запас и сохранённая себестоимость топлива остаются согласованными, а повторная обработка команды или этапа рейса не создаёт второй расход.

## Acceptance criteria

- AC-01: Для выбранного materialized edge Engine вычисляет `reservedFuelKg = ceil(distanceKm × fuelMultiplierPermille / (shipEfficiencyKmPerKg × 1000))` только целочисленной checked-арифметикой. Положительная эффективность берётся из установленного engine module. Недостаточный суммарный fuel отклоняет Undock с `insufficient_voyage_fuel` до изменения docking/voyage/tank state; отсутствующая эффективность даёт `fuel_efficiency_unavailable`.
- AC-02: Принятый Undock атомарно переносит ровно `reservedFuelKg` из видимых баков в persisted reservation active voyage. Успешное прибытие расходует весь резерв один раз; конечный бак равен начальному минус резерв, а settlement сообщает reserved/consumed/returned kg и route fuel cost.
- AC-03: Прерывание при `ProgressPermille` в диапазоне 0..1000 расходует `ceil(reservedFuelKg × progress / 1000)` (при progress 0 — 0), возвращает остальное в те же module tanks и сохраняет границы capacity. Monetary cost относится только к consumed kg; conservation выполняется отдельно для kg и acquisition basis.
- AC-04: Повтор того же CommandId, повторный terminal callback и Save/Load до/после reservation или settlement не меняют tank/cost второй раз, не создают второй voyage и воспроизводят тот же результат. Settlement остаётся наблюдаемым после загрузки.
- AC-05: Refuel увеличивает persisted `FuelCostBasisCredits` на authoritative фактическую стоимость; legacy/scenario fuel без basis получает deterministic bootstrap по base price `item.fuel`. Reservation переносит пропорциональную basis, settlement относит consumed часть в `RouteFuelCostCredits`, а returned часть возвращает в бак. Топливо не попадает одновременно в cargo COGS.
- AC-06: Active voyage snapshot показывает reserved/projected-consumed/projected-cost, а последний settlement — фактические значения. Существующие fuel toolbar/Trade читают уменьшившийся authoritative `FuelAmountKg`; Client не пересчитывает формулу. Accelerate/Brake/Turn/Approach и иные отдельные engine-команды по-прежнему не списывают fuel и не меняют скорость из-за этой истории.

## Non-goals

Не реализовать Undock/state machine/route selection заново, физическую автопрокладку, расход отдельных engine-команд, transfer между баками, утечки/повреждения, обратную продажу топлива, cargo COGS, полный voyage ledger/Finance UI, net profit или balance sign-off. US-0014 владеет lifecycle/progress/save active voyage, US-0006 — повторяемым торговым рейсом, US-0011 — итоговой прибылью, US-0012 — общим save/load экономики, US-0013 — tuning. Новый торговый или маршрутный экран не создаётся.

## Dependencies

- `EP-0001-US-0014-voyage-lifecycle`: authoritative `navigation.undock`, persisted active voyage, terminal arrival/interruption и `ProgressPermille`. Story пока draft; runtime API отсутствует (`src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:83–96`, `src/DeepSpaceSaga.Contracts/NavigationComputerCommandTypes.cs:3–14`).
- `EP-0001-US-0006-repeatable-trading-voyage`: реальный цикл A → B → A поверх lifecycle; её consumer contract ожидает nullable `ActiveVoyage.ReservedFuelKg` (`EP-0001-US-0006-repeatable-trading-voyage.md:46–54`).
- Транзитивный вход из US-0004/US-0008: active voyage фиксирует `DistanceKm` и effective `FuelMultiplierPermille`; US-0004 уже задаёт edge-поля (`EP-0001-US-0004-seeded-trading-map.md:26–34`), US-0008 не списывает fuel и фиксирует effective terms при старте (`EP-0001-US-0008-route-risk-and-alternatives.md:27–36,63–64`).

### Required lifecycle contract — assumption A-01

До исполнения TK-0004 dependency предоставляет `ActiveVoyageData/ActiveVoyageSnapshot` со stable `VoyageId`, origin/destination, `DistanceKm`, captured `FuelMultiplierPermille`, `ProgressPermille`, state и exactly-once terminal transition. Успешный старт вызывает reservation до снятия docking; terminal arrival/interruption вызывает settlement до удаления active voyage. Если фактические type/file names отличаются, TK-0001/TK-0004 возвращаются в review, а scope молча не расширяется.

## Grounding

- Engine владеет состоянием, Client работает через Contracts boundary: `Documentation/00-Process/CLAUDE.md:21–54`; `Documentation/01-Requirements/EngineRequirements.md:362–419`.
- Fuel хранится в engine module в целых kg, не является cargo и не использует cargo mass: `EngineRequirements.md:4928–4954`; обратная продажа не введена: `:5128`.
- Undock сохраняет физическую скорость/вектор: `EngineRequirements.md:119–129`; Approach не меняет скорость и не выполняет docking: `:5335–5343`.
- Текущий runtime хранит `FuelAmountKg` и валидирует диапазон 0..capacity: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:930–995,1084–1105`; save пишет fuel amount: `:836–859`; snapshot публикует amount/capacity: `:625–635`.
- Refuel сейчас списывает credits и прибавляет fuel, но не хранит acquisition basis: `SimulationEngine.cs:2115–2144`. Поиск `FuelCostBasis/RouteFuel/ActiveVoyage` в `src`/`tests` не нашёл готовой модели.
- CommandId dedup и durable receipts уже существуют: `SimulationEngine.cs:130–143`; `SimulationEngine.CommandJournal.cs:8–54`. Новая exactly-once логика должна переиспользовать их, а terminal settlement иметь собственный voyage guard.
- Save schema — `ScenarioData.cs:52–78,291–302`; current version 8 описан в `:6–25`. Active voyage пока отсутствует, поэтому bump/migration номер принадлежит реализации US-0014/US-0012; US-0009 добавляет optional fuel fields в уже создаваемый voyage block.
- Денежные authoritative вычисления не используют float/double, half округляется away from zero: `EngineRequirements.md:5247–5265`.

## Invariants and assumptions

- A-02: Reservation — escrow: kg и соответствующая cost basis удаляются из module tank в момент принятого Undock. Поэтому существующий toolbar сразу показывает только доступный незарезервированный fuel; это не преждевременное consumption, потому что interruption возвращает остаток.
- A-03: `FuelEfficiencyKmPerKg` — optional positive `Int64` engine-module content field. Для `module.engine.basic` baseline равен 10 km/kg; это tuning parameter US-0013, не изменение speed/capacity. Все fuel-carrying propulsion modules текущего MVP обязаны иметь одинаковую положительную эффективность; mixed values отклоняют старт рейса вместо неявного выбора.
- A-04: При нескольких engine tanks reservation заполняется в стабильном порядке installed modules; persisted part хранит `ModuleId`, kg и basis, чтобы refund не менял выбранный бак. Суммарная capacity не превышается.
- A-05: Cost basis — общая целочисленная стоимость fuel в конкретном баке. При refuel добавляется точная стоимость. Legacy fuel получает `FuelAmountKg × item.fuel.BasePriceCredits`. Пропорциональное выделение округляется `AwayFromZero` и clamp-ится к доступной basis; полный reserve/consume переносит весь остаток, поэтому conservation точна.
- A-06: Projected consumption вычисляется из progress без поминутной мутации баков. Фактическая мутация выполняется один раз на terminal transition. `ProgressPermille=0` расходует 0; любое положительное значение использует ceiling, 1000 — весь reserve.
- A-07: `LastVoyageFuelSettlement` — persisted последний settlement для наблюдаемости и consumer US-0011; история нескольких рейсов/полный ledger остаются US-0011. Новый рейс не стирает receipt до нового terminal settlement.
- A-08: Missing efficiency разрешён при загрузке legacy content, но запрещает старт нового voyage. Это позволяет TK-0002 быть отдельным проходящим merge unit до content TK-0003.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0009-TK-0001-voyage-fuel-contract | Контракт reservation и settlement топлива | contracts | US-0014 contract | AC-02, AC-03, AC-04, AC-06 |
| EP-0001-US-0009-TK-0002-fuel-accounting-foundation | Эффективность и себестоимость топлива в баке | engine | none | AC-01, AC-05, AC-06 |
| EP-0001-US-0009-TK-0003-engine-efficiency-content | Baseline эффективности стартового двигателя | content-data | TK-0002 | AC-01 |
| EP-0001-US-0009-TK-0004-voyage-fuel-settlement | Authoritative reservation, refund и расход рейса | engine | TK-0001, TK-0002, TK-0003, US-0006, US-0014 | AC-01, AC-02, AC-03, AC-04, AC-05, AC-06 |

Dependency order: TK-0001 и TK-0002 могут выполняться независимо; затем TK-0003; TK-0004 после всех трёх и lifecycle dependencies. File limits: 5/5/2/5. Matching tests: Contracts.Tests / Engine.Tests / Client.Tests / Engine.Tests. План принят автоматическим workflow.

## Gaps and backlog

- G-01: US-0014 остаётся draft без тикетов/production API. TK-0001 и TK-0004 содержат required contract, но не выдают dependency за реализованную.
- G-02: US-0004 имеет неполный ticket set, US-0008/US-0006 имеют planning artifacts, не runtime. До исполнения TK-0004 нужно подтвердить materialized distance, captured multiplier и progress seam.
- G-03: Baseline 10 km/kg обеспечивает конкретное тестирование, но не является balance approval. Изменять его можно по evidence US-0013 без изменения формулы.
- G-04: Полный voyage ledger, отображение нескольких settlements, Finance/TradeJournal и защита от двойного COGS относятся к US-0011; текущий persisted last settlement — только authoritative handoff.
- G-05: SaveFormat migration policy для совокупного active voyage/economy состояния должна быть согласована US-0012. US-0009 не резервирует конкретный глобальный version number.
- G-06: `$requirements-engineer`, упомянутый `Documentation/00-Process/AGENTS.md:3–14`, отсутствует среди доступных skills; применён предоставленный DSS-StoryBuilder workflow. Блокирующих продуктовых вопросов нет.

## Decision and review log

- 2026-09-21T12:38:06Z — canonical epic/story/path IDs совпадают; ticket folders отсутствовали, следующий номер 0001. Короткие dependency IDs раскрыты до canonical IDs.
- Grounding: прочитаны обязательные process/architecture/requirements, эпик, story, dependencies, исходные concept/MVP и релевантные code/test surfaces. Production-код не изменялся.
- Plan-review: четыре тикета приняты автоматическим workflow; числовой baseline и escrow/rounding записаны как явные assumptions.
- Созданы TK-0001…TK-0004 в dependency order; layer, matching project, максимум пять implementation files и coverage записаны в каждом.
- 2026-09-21T12:38:06Z — complete: story и четыре canonical ticket artifacts подготовлены; implementation, dotnet tests/build и UI smoke не выполнялись.
