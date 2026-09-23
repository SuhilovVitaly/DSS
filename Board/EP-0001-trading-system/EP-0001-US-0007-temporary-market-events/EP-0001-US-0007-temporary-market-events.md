---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
title: Временные события меняют торговые возможности
stage: approved
dependencies: [EP-0001-US-0002-market-replenishment, EP-0001-US-0003-dynamic-market-trading]
created: 2026-09-21T11:08:58Z
source_request: "сделай тикеты для D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0007-temporary-market-events\\EP-0001-US-0007-temporary-market-events.md"
current_review: complete
revision: 1
---

# Временные события меняют торговые возможности

## Входное техническое задание

Точное сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md». Время фиксации grounding: 2026-09-21T11:08:58Z UTC; это не утверждение о времени отправки сообщения.

Исходный объём: Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:123–135,221–235; TradingSystemMvpStories.md:227–253,279–290,459–461. Продуктовые решения: ../Documentation.md:82,100,145–146,183,188. Требования к price-event schema и persistence: Documentation/01-Requirements/EngineRequirements.md:5288–5311. Все относительные пути ниже — от D:/DeepSpaceSaga/DSS.

## User story

Как игрок, я хочу видеть временную причину дефицита или избытка на текущей станции, чтобы менять груз и момент сделки по ситуации. В shipping-каталоге ровно восемь типов: авария реактора, разгерметизация, поломка гидропоники, пиратская блокада, грузовой конвой, карантин, ремонтный бум и научный контракт. Их начало и длительность воспроизводимы по masterSeed; одновременно на станции активно не более двух событий. Active event меняет authoritative производство, потребление/спрос, stock и/или цену, а после окончания не продолжает влиять на рынок. Trade показывает локализованные причину, оставшееся время и краткий эффект без клиентского пересчёта экономики. Blockade/quarantine также публикуют route-effect descriptor, который применяет зависимая US-0008 к конкретным рёбрам и рейсам.

## Acceptance criteria

- AC-01: Strict data-driven каталог содержит ровно восемь согласованных definition IDs, локализационные ключи, priority, eligible profiles, hourly activation chance, диапазон длительности и проверенные item/route effects. Неизвестные ссылки, недопустимые коэффициенты и не восемь shipping definitions отклоняются до старта.
- AC-02: На каждой целой границе игрового часа Engine детерминированно выбирает кандидатов по masterSeed/station/definition/hour, сортирует по priority и ID и активирует не более двух событий на станции. Один seed и конечный GameTimeMs дают одинаковые события при одном большом, дробном, ускоренном или save/load-прогоне; Speed0 от реального времени ничего не запускает.
- AC-03: В пределах [start,end) active events один раз применяют объявленные activation stock deltas и модифицируют authoritative hourly production/demand и quote price factor. Активация, завершение и каждое изменение stock инвалидируют market revision/quotes по контрактам US-0015/US-0003. После end прежний event не влияет и не вызывает повторного delta.
- AC-04: Для pirate blockade и quarantine active snapshot содержит bounded route-effect descriptor: максимум одно затрагиваемое incident edge, availability effect и optional travel/fuel/risk modifiers. Остальные шесть типов имеют route effect none. Выбор/блокировка конкретного ребра, проверка альтернативы и исполнение рейса принадлежат US-0008.
- AC-05: Existing TradeScreen показывает authoritative active events для текущей станции: локализованное имя, причину, краткий эффект и оставшееся игровое время. При 0/1/2 событиях layout, Buy/Sell/Refuel, filters, quantity controls, history, modal pause и возврат на Station сохраняются; отдельный экран и клиентская экономическая логика не добавляются.
- AC-06: Save хранит активные instance IDs, definition IDs, start/end и resolved effects вместе с catalog fingerprint. Load посреди события воспроизводит тот же snapshot/цену/stock и продолжает scheduler без повторной активации или stock delta; несовместимый fingerprint отклоняется до замены мира. Legacy scenario-authored price events продолжают работать.

## Non-goals

StoryBuilder не реализует production-код. US-0007 не выбирает конкретные route edges, не блокирует voyage и не доказывает альтернативную проходимость — это US-0008 после US-0004/0006. Не входят бой, пиратские NPC, радиация, повреждение, физическая прокладка пути, изменение скорости/Approach, удалённое market knowledge, рейсовое топливо/ledger, частотный tuning и десятидневный balance proof (US-0009/0010/0011/0013/0016). Не создаётся новый рынок или экран.

## Dependencies

US-0002 предоставляет optional profile.Economy, hourly market boundary, bounded stock и planned partial `SimulationEngine.MarketTime.cs`; US-0003/US-0015 предоставляют market revision, authoritative quote curve и invalidation. До исполнения TK-0004 signatures сверяются с реализованными prerequisites; расхождение возвращает тикет в review, а не расширяет allowlist.

US-0004/0006 намеренно не являются прямыми зависимостями: route topology и voyage отсутствуют в US-0007. US-0007 создаёт стабильный route-effect descriptor для consumer US-0008, которая уже зависит от US-0004, US-0006 и US-0007.

## Grounding

- Scenario schema уже имеет `StationEventData(EventId, DisplayName, Description, StartedGameTimeMs, DurationMs, PriceFactors)`, но комментарий прямо фиксирует schema/persistence-only и отсутствие lifecycle: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:245–289.
- `ResolveStationEvents` принимает explicit events, проверяет IDs/category/item и строит runtime; shipping scenario сейчас их не создаёт: src/DeepSpaceSaga.Engine/SimulationEngine.cs:1418–1471.
- Цена применяет только события, для которых `start <= processedWorldTime < start + duration`, в deterministic порядке `(StartedGameTimeMs, EventId)`: SimulationEngine.cs:1597–1630. Existing test подтверждает окно [500,1000): tests/DeepSpaceSaga.Engine.Tests/StationEconomyBatch2Tests.cs:39–45.
- Save уже сериализует station Events: SimulationEngine.cs:794–802,894–918; existing roundtrip test находится в StationEconomyBatch2Tests.cs:530–555. Однако scheduler/candidate generation отсутствуют.
- Общий календарный цикл обрабатывает boundaries в `(previous,target]`, отдельно отображая calendar time на motion time: SimulationEngine.EconomyTime.cs:12–51. US-0002/TK-0003 планирует добавить часовую market boundary и `SimulationEngine.MarketTime.cs`.
- Existing production path стартует/завершает recipes и меняет stock без event multipliers: SimulationEngine.ProductionTime.cs:9–63. Planned US-0002 hourly profile flow описан в TK-0003-hourly-market-simulation.md:58–73.
- `StationTradeSnapshot` содержит station ID и items, но active event projection отсутствует: src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:6–40; matching contract tests — tests/DeepSpaceSaga.Contracts.Tests/StationTradeSnapshotTests.cs.
- `TradeModel.Refresh` читает только `DockedStationTrade.Items`: src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs:108–127. Trade renderer уже имеет status/action panel без event block: TradeScreen.Render.cs:134–182.
- `EngineContentLoader` использует strict `UnmappedMemberHandling.Disallow`, загружает declared type-data paths и собирает registry до engine startup: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8–13,73–129. Registry валидирует cross-item profile references: GameDataRegistry.cs:73–110,157–180.
- Epic фиксирует exact eight, seed-derived start/duration, priority, максимум два события и bounded route impact: ../Documentation.md:100. MVP требует stable ID, duration, activation probability/condition, affected stations, modifiers, player text and completion: TradingSystemMvpStories.md:227–253.
- Tests/build не запускались: задача создаёт только planning artifacts. В рабочем дереве уже есть чужие незакоммиченные artifacts US-0002/0003/0004/0005; они не изменялись.

## Invariants and assumptions

- A-01: Shipping event catalog is strict and contains exactly eight definitions. Direct test registries may use a reduced valid catalog only through an explicit `requireCompleteShippingSet:false` test helper; runtime Settings path always requires the exact set.
- A-02: Activation is evaluated only on whole GameCalendar.HourMs boundaries. Candidate roll and duration derive from independent stable hashes of `(masterSeed, stationObjectId, definitionId, hourIndex, purpose)`; no shared `Random` order or persisted RNG cursor is needed.
- A-03: Active interval is half-open `[StartedGameTimeMs, EndsGameTimeMs)`. Generated duration is a positive whole number of game hours. At a boundary, expired events end before candidates activate and before the market batch, so the new set affects that boundary exactly once.
- A-04: At most two generated/finite events are active per station. Permanent legacy scenario event (`DurationMs=null`) is preserved and counts toward the same cap only when a new generated event is considered; invalid saves with more than two simultaneously active events are rejected. Event IDs are deterministic instance IDs and ordinal-unique per station.
- A-05: Item production/demand/price multipliers use fixed-point `1000==1.0`, multiply all applicable active effects in `(start,eventId,itemId)` order with checked decimal and round only at the owning market operation. Activation stock delta is applied once at activation, clamped only by the authoritative US-0002 stock bounds; the unapplied overflow is discarded and observable through the resulting capped stock, not queued as recipe output.
- A-06: Generated effects are fully resolved into save data and guarded by an event-catalog SHA-256 fingerprint. SaveFormat advances from the dependency baseline 9 to 10. Full cross-version migration remains US-0012; legacy non-event saves/scenarios stay readable by optional fields.
- A-07: `PriceFactors` remains the single input to the existing price path. TK-0004 maps catalog item price multipliers to existing runtime price factors instead of creating a second quote formula.
- A-08: Blockade/quarantine descriptor has `maxAffectedIncidentEdges=1`. It expresses desired route availability/time/fuel/risk impact; US-0008 deterministically chooses an edge and proves a remaining alternative. US-0007 does not claim the graph changed before that consumer is implemented.
- A-09: UI renders no countdown based on wall clock. It computes remaining display from authoritative `Snapshot.GameTimeMs` and event end; modal Speed0 therefore freezes the shown duration naturally.
- A-10: Localization keys, not user-facing English/Russian strings, cross the Contracts boundary. Legacy events without keys fall back to existing DisplayName/Description without failing.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0007-TK-0001-market-event-contract | Authoritative проекция активных событий | contracts | US-0002/TK-0001 | AC-04, AC-05, AC-06 |
| EP-0001-US-0007-TK-0002-market-event-catalog | Схема каталога и save-состояния событий | engine | US-0002/TK-0002; TK-0001 | AC-01, AC-04, AC-06 |
| EP-0001-US-0007-TK-0003-market-event-content | Восемь событий и локализованные объяснения | content-data | TK-0002 | AC-01, AC-04, AC-05 |
| EP-0001-US-0007-TK-0004-market-event-lifecycle | Seeded lifecycle и экономические эффекты | engine | TK-0001, TK-0002, TK-0003; US-0002/TK-0003; US-0003/US-0015 | AC-02, AC-03, AC-04, AC-06 |
| EP-0001-US-0007-TK-0005-trade-event-presentation | Причина и длительность события в Trade | client | TK-0001, TK-0003, TK-0004; US-0003/TK-0004 | AC-05 |

Dependency order: TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. TK-0001 и TK-0002 могут выполняться параллельно после prerequisites, но последовательный порядок безопасен. Implementation files: 2 / 5 / 5 / 5 / 5.

## Gaps and backlog

- G-01: US-0002/0003/0015 имеют approved planning, но их production APIs ещё нельзя считать merged. TK-0004 содержит точный integration contract и prerequisite gate.
- G-02: Route graph/application отсутствуют в текущем коде и принадлежат US-0004/0006/0008. Здесь completion для route-specific event означает валидный authoritative descriptor и UI explanation, не фактическую блокировку voyage.
- G-03: Exact chances, duration ranges and multipliers below are initial tuning assumptions. US-0013 may change values only with recorded balance evidence, preserving IDs and effect semantics.
- G-04: Existing schema permits permanent scenario events. Generated shipping events are finite; removal of permanent authored effects and general event scripting remain outside this story.
- G-05: Event catalog compatibility is a focused v10 guard. General deterministic migrations and full economy restore matrix remain US-0012.
- G-06: Нет блокирующих вопросов. Skill requirements-engineer, упомянутый process AGENTS.md, отсутствует среди доступных skills; применён обязательный DSS-StoryBuilder workflow.

## Decision and review log

- 2026-09-21T11:08:58Z: пользователь явно выбрал US-0007 указанным canonical path; точное сообщение записано выше.
- Grounding: path/folder/frontmatter IDs совпали; ticket folders отсутствовали, следующий номер 0001. Прочитаны process AGENTS.md, CLAUDE.md, EngineRequirements, epic Documentation.md, story и нужные code/test surfaces.
- Plan-review: пять tickets приняты автоматическим workflow. Route application оставлено US-0008 по dependency boundary; это explicit assumption A-08, не скрытое сокращение.
- Создан TK-0001: contracts projection, 2 файла. Следующий TK-0002.
- Создан TK-0002: strict catalog, save schema/fingerprint, 5 файлов. Следующий TK-0003.
- Создан TK-0003: exact eight JSON definitions, RU/EN keys and content tests, 5 файлов. Следующий TK-0004.
- Создан TK-0004: deterministic scheduler, hourly effects, quote invalidation and Save/Load, 5 файлов. Следующий TK-0005.
- Создан TK-0005: compact Trade presentation and UI regressions, 5 файлов.
- 2026-09-21T11:21:56Z — artifact validation passed: canonical story + five matching ticket folders/files; metadata IDs match paths; layers contracts/engine/content-data/engine/client; Code context counts equal 2/5/5/5/5 and stay within limit; AC-01…AC-06 covered; every dependency resolves and local graph is acyclic; required sections and whitespace checks pass.
- Workflow complete: изменены только planning artifacts US-0007. Production code, epic and requirements не изменялись; build/test/format не запускались, exact commands находятся в tickets.
