---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
title: Авторитетная котировка и последовательная цена партии
stage: approved
dependencies: [EP-0001-US-0001-station-market-profiles, EP-0001-US-0002-market-replenishment]
created: 2026-09-21T15:10:56Z
source_request: "создай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0015-authoritative-market-quotes\\EP-0001-US-0015-authoritative-market-quotes.md"
current_review: complete
revision: 1
---

# Авторитетная котировка и последовательная цена партии

## Входное техническое задание

Точное сообщение пользователя сохранено в `source_request`; 2026-09-21T15:10:56Z — время регистрации в planning log, а не утверждение о времени отправки. Выбрана существующая US-0015; ID эпика/story в пути, frontmatter, имени папки и файла совпадают. Корень относительных путей ниже — `D:/DeepSpaceSaga/DSS`.

Исходный продуктовый контекст: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:98–121`, `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:189–225,451–457`, решения эпика `Board/EP-0001-trading-system/Documentation.md:90–104,655–691,745–756`. Engine остаётся источником истины (`Documentation/01-Requirements/EngineRequirements.md:362–419`); деньги и коэффициенты используют decimal/fixed-point и `MidpointRounding.AwayFromZero` (`EngineRequirements.md:5247–5265`).

## User story

Как игрок, я хочу получить перед сделкой свежую и объяснимую authoritative-котировку, чтобы сравнить дефицит и избыток и понимать точную стоимость выбранной партии. Цена каждой целой единицы учитывает base price, профиль станции, активные события, текущий `stockRatio` и направление сделки; следующая единица считается после виртуального изменения запаса предыдущей. Котировка содержит `QuoteId`, `MarketRevision`, исполнимый максимум, run-length curve, итог и причины цены/ограничений. Изменение stock, market budget или ценовых событий увеличивает revision и делает ранее выданный token stale. Client получает готовую сумму через `IGameSessionConnection` и не повторяет экономическую формулу.

## Acceptance criteria

- AC-01: Для bounded-market item дефицит, норма и избыток дают разные цены по формуле эпика: `stockFactor = clamp(1 + 0.70 × (1 − stockRatio), 0.65, 1.70)`, buy spread `1.15`, sell spread `0.85`, финальная граница `0.50×..3.00× BasePrice`, один финальный round AwayFromZero; authoritative путь не использует float/double.
- AC-02: Quote партии моделирует последовательные целые единицы: Buy/Refuel уменьшает виртуальный stock, Sell увеличивает; `TotalCredits` равен checked-сумме `Curve`, соседние одинаковые цены сжимаются в run-length segments. Для quantity 1 результат совпадает с первой unit quote, а немедленная котировка Buy→Sell той же партии не обещает прибыль.
- AC-03: Quote bound к RequestId/QuoteId, станции, кораблю, модулю, command type, item, requested quantity и текущей revision. Она сообщает `ExecutableQuantity`, `MaximumQuantity`, `DisabledReason`, limit reasons и price reasons, не раскрывая market budget/Credits станции.
- AC-04: Успешное изменение stock, market budget или ценовых событий повышает revision станции ровно один раз на атомарное изменение; rejected/no-op не повышает. Quote старой revision и вытесненный token валидатор отклоняет как stale без мутации мира.
- AC-05: Revision profile-market сохраняется и восстанавливается; legacy station без профиля сохраняет `MarketRevision = null`. Runtime quote cache и tokens не сохраняются и очищаются на load, поэтому pre-load QuoteId после загрузки не принимается и не переиспользуется.
- AC-06: `StationTradeSnapshot` публикует только revision, а `GetTradeQuoteAsync` возвращает один и тот же authoritative DTO через contract и LocalClient. Повтор того же RequestId с тем же binding идемпотентен; collision с другим binding отвергается.
- AC-07: Формула и issuer предоставляют точный prerequisite API для US-0003: `SimulationEngine.GetTradeQuote`, `TryValidateTradeQuote`, `NextMarketRevision` и `CommitMarketRevision`. Client preview/confirm и фактическое атомарное исполнение остаются в US-0003 и обязаны использовать этот payload без пересчёта цены.

## Non-goals

Эта история не исполняет Buy/Sell/Refuel, не расширяет `CommandResult`, не создаёт trade receipt и не меняет Trade UI или локализации — это US-0003. В `PlayerCommand` добавляется только quote binding, необходимый валидатору; receipt/reason codes остаются в US-0003. Не вводятся production/consumption rates (US-0002), lifecycle событий (US-0007), удалённые точные цены (US-0016), ledger/cost basis (US-0010/0011), балансный retuning (US-0013), новый экран или продажа топлива из бака. Requirements и epic-документ не изменяются.

## Dependencies

- `EP-0001-US-0001-station-market-profiles` предоставляет profile identity, station/profile factors и каталог base prices.
- `EP-0001-US-0002-market-replenishment` предоставляет `TargetStock`, `MaxStock`, `FreeStockCapacity`, `MarketBudgetCredits` и центральные мутации почасового рынка. На момент grounding обе зависимости имеют approved planning artifacts, но их production-ready состояние перед implementation требуется проверить.
- Downstream `EP-0001-US-0003-dynamic-market-trading` уже зафиксировал consumer seam этой story. Сигнатуры ниже совместимы с его assumption A-01; добавочные `PriceReasons` идут trailing-полем и не меняют обязательные поля.

## Grounding

- Текущий snapshot содержит одну `UnitPriceCredits`, stock и `MaxSellableQuantity`, но не revision или quote DTO: `src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:10–40`. `PlayerCommand` пока не несёт QuoteId/revision: `src/DeepSpaceSaga.Contracts/PlayerCommand.cs:6–50`.
- `IGameSessionConnection` имеет command/snapshot/save methods, но не request-response quote seam: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10–55`.
- Projection и execution независимо вычисляют одну цену через `ResolveStationPriceFactors`/`StationPricing`: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:560–598,1598–1630,2051–2055`. Buy/Sell/Refuel умножают её на quantity и не строят curve: `SimulationEngine.cs:2057–2167`.
- `StationPricing.ComputeUnitPriceCredits` уже применяет decimal/fixed-point и один final round, но не stock factor, spread, clamp или последовательность: `src/DeepSpaceSaga.Engine/Content/StationPricing.cs:1–27`; базовые проверки находятся в `tests/DeepSpaceSaga.Engine.Tests/StationPricingTests.cs:14–73`.
- Trade client сейчас сам рассчитывает maximum и `quantity × UnitPriceCredits`: `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs:9–55`; submit отправляет только item/quantity: `TradeScreen.cs:138–145`. Это будет удалено consumer-тикетами US-0003, не этой story.
- `LocalGameSessionConnection.SendCommandAsync` — прямой cancellable adapter к Engine: `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:102–110`; matching integration coverage расположено в `tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs:20–46`.
- Runtime station хранит inventory/events/profile, но не market revision: `SimulationEngine.cs:3233–3299`; save projection переносит station inventory/events/profile: `SimulationEngine.cs:785–813`. QuoteId/MarketRevision поиском в production не обнаружены — это gap, а не скрытый готовый API.

## Invariants and assumptions

- A-01: Profile market начинает с revision `1`; legacy station без `MarketProfileId` публикует null revision. Missing revision в legacy save profile-market детерминированно инициализируется `1`; отрицательная/переполненная revision отклоняется до замены мира.
- A-02: Каждая unit price считается по stock перед виртуальным переносом единицы. Buy и Refuel двигают stock `−1`, Sell `+1`; stockFactor отсутствует (`1.0`) у legacy/Fuel row без positive TargetStock. Refuel использует buy spread и может сжаться в constant curve.
- A-03: Итоговый множитель clamp `0.50..3.00` применяется после station/profile, event, stock и spread factors, до единственного округления. Цена не может стать меньше `1 Credit` для positive BasePrice; invalid/nonpositive base price даёт disabled quote, а не бесплатную торговлю.
- A-04: `MaximumQuantity` — наибольший prefix по текущим authoritative ресурсам. Buy/Refuel выше maximum disabled и не partial; Sell может иметь `ExecutableQuantity < RequestedQuantity` только из-за station budget или free stock capacity. Недостаток cargo/invalid binding disabled целиком, как требует US-0003.
- A-05: Price reasons — stable codes + fixed-point factor/source, не локализованный текст и не скрытые суммы. Минимальные codes: `base_price`, `station_profile`, `stock_shortage|stock_normal|stock_surplus`, `event`, `buy_spread|sell_spread`, `price_floor|price_ceiling`. UI mapping принадлежит US-0003.
- A-06: Cache bounded 1024 issued quotes. Повтор identical RequestId возвращает тот же immutable quote; тот же RequestId с другим binding даёт disabled `request_id_conflict`. Eviction и revision change делают token stale. Cache и runtime session nonce не входят в save/deterministic world state.
- A-07: Один market transaction вызывает `NextMarketRevision` до commit и `CommitMarketRevision` после всех fallible вычислений. Multiple item/budget changes одного hourly station update дают один increment. `CommitMarketRevision` инвалидирует все quotes станции; future US-0007 event mutator обязан пользоваться тем же helper.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0015-TK-0001-trade-quote-contract | Контракт authoritative-котировки | contracts | US-0001, US-0002 | AC-03, AC-05, AC-06, AC-07 |
| EP-0001-US-0015-TK-0002-sequential-price-curve | Последовательная кривая цены | engine | TK-0001; US-0001, US-0002 | AC-01, AC-02, AC-03 |
| EP-0001-US-0015-TK-0003-market-revision-lifecycle | Жизненный цикл market revision | engine | TK-0001; US-0002 | AC-04, AC-05, AC-07 |
| EP-0001-US-0015-TK-0004-authoritative-quote-issuer | Выдача и валидация котировки | engine | TK-0002, TK-0003 | AC-02, AC-03, AC-04, AC-05, AC-06, AC-07 |
| EP-0001-US-0015-TK-0005-local-quote-transport | LocalClient transport котировки | local-client | TK-0001, TK-0004 | AC-03, AC-06, AC-07 |

Порядок: `TK-0001 → (TK-0002, TK-0003) → TK-0004 → TK-0005`; допустимо последовательно `0001 → 0002 → 0003 → 0004 → 0005`. Implementation files: `5 / 4 / 5 / 4 / 2`; каждый тикет указывает matching test project.

## Gaps and backlog

- G-01: US-0001/US-0002 пока нельзя считать implemented только по approved planning. Реализация US-0015 начинается после сверки фактических signatures dependency; несоответствие возвращает тикет в review, а не расширяет allowlist молча.
- G-02: Lifecycle восьми событий ещё отсутствует; TK-0003 создаёт обязательный revision helper и regression seam, а US-0007 подключает будущие event start/end к нему.
- G-03: Remote/network implementation `IGameSessionConnection` отсутствует. Default interface method сохраняет совместимость текущих test doubles; production LocalClient реализуется TK-0005.
- G-04: Численные коэффициенты — утверждённый baseline эпика, но их изменение по balance evidence принадлежит US-0013. Здесь они тестируются буквально.
- G-05: Полноценный price UI и локализация reasons принадлежат US-0003; US-0015 заканчивается проверяемым transport payload, не дублирующим client economics.
- Блокирующих вопросов нет.

## Decision and review log

- 2026-09-21T15:10:56Z: зарегистрирован точный запрос пользователя; выбрана явно указанная US-0015.
- Grounding: обязательные process/architecture/requirements sources, epic/story, relevant code/tests и consumer story US-0003 проверены. Старых ticket folders в US-0015 не было; первый номер — 0001.
- Plan-review: пять тикетов укладываются в layer и limit пять файлов; A-01…A-07 приняты безопасными explicit assumptions. Отдельное подтверждение плана по workflow не требуется.
- Созданы TK-0001…TK-0005 в dependency order; production-код, requirements и epic не изменялись.
- Artifact validation: canonical paths/IDs, metadata, file counts, dependency closure и AC coverage проверены; unresolved blocking questions отсутствуют.
