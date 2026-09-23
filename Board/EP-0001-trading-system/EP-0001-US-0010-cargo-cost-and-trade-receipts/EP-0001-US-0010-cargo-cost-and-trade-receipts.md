---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
title: Проверяемая себестоимость и результат продажи груза
stage: approved
dependencies: [EP-0001-US-0003-dynamic-market-trading]
created: 2026-09-21T12:39:58Z
source_request: "создай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0010-cargo-cost-and-trade-receipts\\EP-0001-US-0010-cargo-cost-and-trade-receipts.md"
current_review: complete
revision: 1
---

# Проверяемая себестоимость и результат продажи груза

## Входное техническое задание

Точное сообщение пользователя сохранено в `source_request`. Время фиксации grounding: 2026-09-21T12:39:58Z UTC; это не утверждение о времени отправки сообщения. Выбор US-0010 однозначен, ID эпика/story в пути, frontmatter, папке и имени файла совпадают.

Источники: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:207–219`, `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:223–225,463–465`, `Documentation/01-Requirements/EngineRequirements.md:5160,5247–5265`, решения эпика `../Documentation.md:96,102–104,148–150`. Все относительные пути ниже считаются от `D:/DeepSpaceSaga/DSS`.

## User story

Как игрок, я хочу видеть фактическую стоимость приобретения груза и результат его продажи, чтобы отличать полученную прибыль от стоимости непроданного остатка. Покупки одного item по разным ценам объединяются в один проверяемый средневзвешенный cost basis; частичная продажа списывает basis только фактически исполненного количества. Authoritative receipt хранит заявленное и исполненное количество, фактическую сумму, реализованную себестоимость и результат продажи. Начальный explicit cargo получает bootstrap basis, а cargo с источником `purchased`, `produced` или `mined` сохраняет provenance; replay команды не создаёт вторую запись или списание. Старый cargo без доказуемой цены не трактуется как бесплатный: его cost/result показываются как unavailable до полного выбытия неизвестного stack.

## Acceptance criteria

- AC-01: Две успешные покупки одного item по разным фактическим суммам увеличивают quantity и общий cost basis на точные authoritative totals; средняя стоимость выводится из общего basis и количества, без округления каждой покупки заново.
- AC-02: Полная или частичная Sell списывает cost basis только `ExecutedQuantity`; receipt содержит `RequestedQuantity`, `ExecutedQuantity`, выручку, реализованную себестоимость и `grossResult = proceeds - realizedCost`. Округление пропорциональной себестоимости выполняется один раз `MidpointRounding.AwayFromZero`, а remaining basis равен старому basis минус списанная сумма.
- AC-03: Новый scenario explicit cargo без metadata получает `bootstrap` basis из catalog base price; сохранённый cargo round-trip хранит basis и непустой acquisition source. `produced`/`mined` metadata принимаются только с authoritative неотрицательной стоимостью; отсутствующие игровые handlers не симулируются этим story.
- AC-04: Save format повышен, новые saves валидируют cargo cost metadata. Legacy cargo с неизвестной исторической ценой помечается `legacy-unknown`, не получает скрытый нулевой basis и не публикует фиктивный gross result.
- AC-05: Все существующие удаления/добавления cargo сохраняют инварианты quantity/basis: питание и dialogue removal списывают пропорциональный basis, dialogue grant явно получает бесплатный source; invalid/overflow оставляют cargo и receipt неизменными.
- AC-06: Повтор CommandId возвращает тот же persisted receipt; после Save/Load он не меняет деньги, cargo, basis или число записей. Нереализованный остаток не включается в результат продажи.
- AC-07: Existing Trade history показывает actual/requested, purchase cost либо sale proceeds, realized cargo cost и gross result только из receipt. Legacy/invalid/unknown cost отображается как unavailable, а не пересчитывается из текущей котировки или `quantity × unit price`.

## Non-goals

- Route fuel, port fees, passenger payout/penalty, event costs и чистая прибыль рейса принадлежат US-0009/US-0011.
- Finance aggregation, unlimited audit archive и межрейсовая группировка не входят; используется существующий bounded Trade journal.
- Формула котировки, market revision и атомарное исполнение сделки принадлежат US-0015/US-0003; эта история расширяет их receipt и commit seam.
- Не добавлять новый торговый экран, клиентский расчёт себестоимости или переоценку остатка по текущему рынку.
- Реальные player production/mining handlers отсутствуют в проверенном коде; эта история задаёт source contract и persistence seam, но не изобретает производство или добычу.

## Dependencies

`EP-0001-US-0003-dynamic-market-trading` предоставляет quoted `TradeExecutionReceipt`, атомарный executor и receipt-only history. Конкретные prerequisites: `EP-0001-US-0003-TK-0001-trade-execution-contract`, `EP-0001-US-0003-TK-0002-atomic-quote-execution`, `EP-0001-US-0003-TK-0003-trade-result-texts` и `EP-0001-US-0003-TK-0005-confirmed-trade-history`. Approved planning не означает, что эти production APIs уже реализованы; implementer US-0010 сначала проверяет dependency signatures.

## Grounding and invariants

- Engine владеет механикой и состоянием; Contracts остаётся без зависимостей, Client не рассчитывает authoritative результат: `Documentation/00-Process/CLAUDE.md:22–56`.
- `CommandResult` сейчас имеет только optional `ExecutedQuantity`: `src/DeepSpaceSaga.Contracts/CommandResult.cs:56–71`; US-0003 TK-0001 добавляет `TradeExecutionReceipt`.
- Runtime cargo stack сейчас хранит только item index и quantity: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:3311–3326`; JSON cargo stack — только item id/quantity: `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:291–302,416–419`.
- Load/build/save cargo находится в `SimulationEngine.cs:1063–1081,836–878`; текущий save format равен 8 в `ScenarioData.cs:7–24`, а loader проверяет version/metadata в `ScenarioLoader.cs:129–141,184–210`.
- Buy прибавляет cargo и списывает деньги, Sell допускает partial и удаляет cargo: `SimulationEngine.cs:2033–2112`. После US-0003 quoted flow переносится в `SimulationEngine.TradeExecution.cs` по approved prerequisite ticket.
- Rations и dialogue effects независимо уменьшают/увеличивают cargo: `SimulationEngine.EconomyTime.cs:54–90`; `Dialogue/DialogueEffectTransaction.cs:95–123`. Эти paths обязаны сохранять cost-basis invariant.
- Текущий TradeJournal bounded50 и сохраняется в session handle, но confirmed message умножает quantity на cached unit price: `TradeModel.cs:58–83`; `TradeScreen.Render.cs:165–173`. US-0003 заменяет это receipt total, US-0010 добавляет cargo result fields.
- Целые деньги и fixed-point factors не используют float/double; midpoint rounding — away from zero: `EngineRequirements.md:5247–5265`.

## Assumptions

- A-01: Cost basis хранится как total Credits на stack, не как округлённая unit cost. `Known` basis неотрицателен; `legacy-unknown` использует null, никогда zero-by-default.
- A-02: Acquisition provenance — deterministic ordinal set строк: `bootstrap`, `purchased`, `produced`, `mined`, `dialogue-grant`, `legacy-unknown`. Смешанный stack хранит union источников; источник не определяет цену.
- A-03: Для new-game scenario без metadata bootstrap total равен checked `quantity × BasePriceCredits`; item без положительной catalog price требует explicit basis вместо нулевой подстановки. Save format 9 требует metadata, кроме явно `legacy-unknown` migration state.
- A-04: Legacy save format 1–8 без metadata загружается как `legacy-unknown`: продажа/consumption корректно меняют quantity, но COGS/gross result остаются null, пока неизвестный stack не исчезнет. Это консервативно и не переоценивает старый cargo.
- A-05: Для known basis partial removal использует `Round(totalBasis × removedQty / oldQty, AwayFromZero)` и clamp `[0,totalBasis]`; remaining basis получается вычитанием, поэтому полное выбытие списывает ровно исходный total.
- A-06: `produced` и `mined` source разрешены в persistence/helper только вместе с переданной authoritative cost. Current player-cargo producers/miners не найдены; wiring будущего handler — backlog, не скрытая работа.
- A-07: Purchase receipt использует `TotalCredits` как purchase cost. Sell receipt дополняется nullable `RealizedCargoCostCredits` и `GrossResultCredits`; Refuel и rejection оставляют их null. Known successful Sell требует оба значения.
- A-08: Existing command journal retention4096 и UI retention50 не расширяются. Dedupe продолжает возвращать immutable original receipt; долговременный ledger относится к US-0012/US-0011.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0010-TK-0001-cargo-result-contract | Себестоимость в authoritative receipt | contracts | US-0003 TK-0001 | AC-02, AC-06, AC-07 |
| EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis | Сохраняемый cost basis груза | engine | none | AC-03, AC-04, AC-06 |
| EP-0001-US-0010-TK-0003-weighted-cost-accounting | Средневзвешенное списание и результат продажи | engine | TK-0001, TK-0002; US-0003 TK-0002 | AC-01, AC-02, AC-05, AC-06 |
| EP-0001-US-0010-TK-0004-cargo-result-texts | Тексты стоимости и результата груза | content-data | TK-0001 | AC-07 |
| EP-0001-US-0010-TK-0005-trade-cost-history | Проверяемый результат в Trade history | client | TK-0001, TK-0003, TK-0004; US-0003 TK-0005 | AC-02, AC-06, AC-07 |

Implementation files: 2 / 5 / 5 / 3 / 3. Dependency order: TK-0001 и TK-0002 параллельно; затем TK-0003 и TK-0004; затем TK-0005.

## Gaps and backlog

- G-01: US-0003/US-0015 approved только как planning artifacts; до implementation US-0010 нет реального `TradeExecutionReceipt`/quoted executor. Несовпадение prerequisite signature возвращает ticket в review, а не расширяет files скрытно.
- G-02: В проверенном Engine нет player production/mining cargo handler. `produced`/`mined` source и persistence готовы к подключению, но end-to-end добыча/производство требует своей user story.
- G-03: Для pre-v9 cargo невозможно доказать историческую цену. Консервативный `legacy-unknown` предотвращает фиктивную прибыль; отдельная пользовательская migration/reconciliation policy может быть добавлена в US-0012.
- G-04: Bounded journal50 и command receipts4096 сохраняются. Unlimited финансовый ledger, Finance totals и voyage grouping — US-0011/US-0012.
- G-05: `requirements-engineer`, упомянутый repo guide, отсутствует в доступных skills; применён предоставленный workflow DSS-StoryBuilder.
- Блокирующих вопросов нет: неизвестные legacy costs не угадываются, а недоступные production/mining flows явно вынесены в backlog.

## Decision and review log

- 2026-09-21T12:39:58Z — записано точное сообщение пользователя; выбран US-0010.
- Grounding: mandatory docs, epic, story, dependency tickets и текущие code/test surfaces прочитаны; canonical IDs совпадают; ticket folders отсутствовали, первый номер `0001`.
- Plan-review: принят split из пяти ticket merge units с одним layer и не более пяти files каждый. A-01…A-08 позволяют не задавать неблокирующие вопросы.
- Созданы TK-0001…TK-0005 в dependency order; production code, epic Documentation и requirements не изменялись.
- Artifact validation: canonical paths/frontmatter, file limits, served AC coverage и dependency DAG проверены; workflow complete.
