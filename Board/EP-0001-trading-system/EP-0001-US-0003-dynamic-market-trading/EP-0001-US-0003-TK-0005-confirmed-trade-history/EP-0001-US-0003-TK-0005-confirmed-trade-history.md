---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
ticket: EP-0001-US-0003-TK-0005-confirmed-trade-history
title: Подтверждённые суммы и partial receipt
stage: approved
layer: client
depends_on: [EP-0001-US-0003-TK-0004-quoted-trade-controls]
files_touched: 3
serves: [AC-02, AC-04, AC-06, AC-07]
created: 2026-09-21T09:33:31Z
revision: 1
---

# Подтверждённые суммы и partial receipt

## Why

History сейчас вычисляет quantity*запомненная цена и покажет неверную сумму при динамической кривой. End state: status/history читают фактические значения одного authoritative receipt, повторное открытие/повтор result не добавляют сделок, интеграционный test связывает quote→command→receipt→UI.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj. Пути от D:/DeepSpaceSaga/DSS.

## Decisions

2026-09-21T09:33:31Z — сообщение: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Отдельных пользовательских решений о ledger нет.

## Assumptions

Per-session UI journal bound50 остаётся; это не Finance ledger и не история между новыми client sessions. Старый result без TradeReceipt показывает ReceiptUnavailable вместо придуманной суммы. Execution/retry correctness принадлежит TK-0002; клиент только сопоставляет и отображает.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | :61–89 TradeJournal, Track добавляет, Refresh находит result; TK-0004 добавляет binding в Entry | Dedup entries by CommandId, receipt binding validation, actual-total projection без повторного расчёта |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :164–173 EntryMessage quantity*UnitPrice; :196–212 history выводит EntryMessage | Receipt-only success/partial/history; internal formatting seam для теста, сохранить layout |
| tests/DeepSpaceSaga.Client.Tests/TradeScreenTests.cs | :13 class, существующие UI/format/navigation tests | Receipt-aware fixtures, journal/partial/stale/close regressions и actual Engine quote→execution→UI integration |

## Public API after the change

No public API change. Internal formatting seam: `internal string EntryMessage(TradeJournal.Entry entry)` вместо private, либо эквивалентный internal чистый formatter в том же Render file, не новый production file.
Entry сохраняет CommandId, ItemId, ModuleId, Mode, RequestedQuantity, ModuleLabel и optional QuoteId/MarketRevision/QuotedTotalCredits от TK-0004. Result.TradeReceipt содержит StationObjectId, ItemTypeId, QuoteId, QuotedMarketRevision, ResultMarketRevision, RequestedQuantity, ExecutedQuantity, TotalCredits, LimitReasons. `CommandResult.ExecutedQuantity` остаётся legacy partial field, но при наличии receipt отображение использует только TradeReceipt.

## Implementation steps

1. Сделать Track idempotent по CommandId. Повторная запись того же ID не создаёт вторую строку и не сбрасывает существующий terminal result. Сохранять chronological bound50; один актуальный pending entry не удаляется повтором его же ID. Refresh повторного immutable result не добавляет строки. Использовать SnapshotBuffer.FindCommandResult, не подписывать screen отдельно на Engine.
2. При terminal result сопоставить top-level CommandId/module/type и receipt item/QuoteId/quoted revision/requested с полями pending Entry. Mode сопоставляется с trade.buy/trade.sell/trade.refuel. Object/station authoritative binding проверяет Engine; Entry не хранит их, поэтому не добавлять фиктивную UI проверку или SubmittedQuote. Не сравнивать receipt со станцией текущего snapshot: игрок мог уже закрыть Trade или сменить станцию. Никакая смена текущего контекста не меняет исторический receipt.
3. Success display: quantity=TradeReceipt.ExecutedQuantity, total=TradeReceipt.TotalCredits, requested=TradeReceipt.RequestedQuantity. PartialResult при actual<requested, иначе SuccessResult; сумма форматируется как long без unit price умножения. LimitReasons отображать понятными TK-0003/0004 messages вместе с partial результатом; сохранять item/module labels и requested. Pending показывает existing SendingItem, rejected — existing Rejected + reason. Receipt с несовпадающим binding/невалидным shape => ReceiptUnavailable, не финансовый success. Legacy null receipt => ReceiptUnavailable, но rejection reason остаётся доступен.
4. Entry.UnitPrice и QuotedTotalCredits не использовать для confirmed sum. Не подменять старую сумму после новой котировки/смены snapshot, sorting или reopening. Рассинхронизация preview/actual допустима только как явный rejected/stale result; успешный fresh quote обязан совпасть с receipt total/executable, иначе показывать unavailable и регрессионный тест должен выявить нарушение.
5. Обновить старые TradeScreenTests fixtures с явными receipts там, где они моделируют новую quoted торговлю. Отдельно сохранить legacy-null rendering test. Test navigation: CloseTrade возвращает существующий ScreenEvent, UI не меняет CurrentSpeed и не добавляет новых screens. Проверить pending при закрытии/reopen через общий Handle.Trades; receipt появляется один раз.
6. Добавить integration fixture В ЭТОМ ЖЕ test file: root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..")); settingsPath=root/src/DeepSpaceSaga.Client/Settings.json; registry=EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath,out _,out _). ScenarioLoader.LoadFromFile(root/src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json) даёт валидный ship hull/loadout. Собрать новый ScenarioFile с metadata quoted-trade-test и новым GameStateData: masterSeed42, Speed0, GameTimeMs0, PlayerTokens1000000; SpaceObjects содержит только копии player и SPC-0002. Остальные optional GameStateData поля оставить default, не переносить generation requests или save compatibility исходного сценария. Station: MarketProfileId=market.industrial, MarketProfileFingerprint=null, Inventory=null, Credits=null, StationSize=Medium, ProducingModules=[], Events=[]; остальные служебные поля оставить. Ship: docked к SPC-0002, position=station+(1,1), speed0, Stationary, тот же module placement. PlayerTokens=1000000. Engine.LoadScenario материализует реальные profile defaults. Выбирать cargo module по поддержке trade.buy и engine module по trade.refuel, а не создавать другие module types. Тестовая копия не записывается на диск и не зависит от demo US-0001/TK-0004. Для partial Sell сначала купить3 units item.energy-cells; получить Sell quote3, взять стоимость первого unit из Curve, CaptureSaveStateForTests и immutable copy station с MarketBudgetCredits=этой стоимости (Credits не меньше budget); load copy, запросить НОВУЮ Sell quote3 и проверить partial1. Ни quote formula, ни fixture production files не копировать.
7. Fake IGameSessionConnection внутри test file делегирует GetTradeQuoteAsync→engine.GetTradeQuote; SendCommandAsync→engine.ReceiveCommand и даёт snapshot через engine.CaptureSnapshotForTests с frozen calendar/motion time. Client tests имеют InternalsVisibleTo в Engine.csproj:17. Не импортировать Engine в production Client. SnapshotBuffer.Update принимает output; completed controlled tasks избегают wall-clock waits. Купить партию, получить новую Sell quote той же партии, подтвердить; проверить balances и что EntryMessage использует оба разных фактических totals. Повтор command/result не меняет историю/баланс. Реальная anti-arbitrage failure — prerequisite failure US-0015, не подменять calculator stub.

## Out of scope

Новая долговременная база истории, Finance/cost-basis, удалённые котировки, transport/pause implementation, изменение Engine/calculator или сценариев, screenshot/QA artifacts без запроса.

## Invariants

- Session journal принадлежит Handle: GameSessionHandle.cs:87; closing screen не очищает его.
- Старое multiplication находится только в EntryMessage: TradeScreen.Render.cs:164–173; удалить его для receipt rendering полностью.
- SnapshotBuffer.Update(AuthoritativeSnapshot) и FindCommandResult — существующий client путь; Engine недоступен production render.
- Matching tests могут ссылаться на Engine internals: src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj:17, это не разрешение Client production зависимости.

## Tests

TradeScreenTests:
- `History_uses_receipt_total_when_curve_differs_from_unit_price_times_quantity` — AC-02/06/07.
- `Partial_receipt_shows_actual_requested_total_and_capacity_or_budget_reason` — AC-02/06.
- `Duplicate_receipt_and_track_do_not_duplicate_history` — AC-04/06.
- `Missing_or_mismatched_receipt_never_invents_confirmed_total` — AC-06.
- `Closing_and_reopening_trade_preserves_pending_then_completed_entry` — AC-06.
- `Existing_exit_quantity_controls_and_modal_snapshot_pause_are_preserved` — AC-06; screen doesn't advance frozen time, broader pause regression commands below.
- `Real_quote_execution_receipt_and_history_agree_for_buy_partial_sell_and_refuel` — AC-02/07; actual production calculator/executor, distinct totals, exact prefix.
- `Immediate_roundtrip_and_replayed_command_do_not_increase_balance` — AC-04/07; frozen time/no other income.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeScreenTests|FullyQualifiedName~TradeUxTests|FullyQualifiedName~LocalizationTests"
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~QuotedTradeExecutionTests|FullyQualifiedName~PauseSimulationTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps в трёх файлах, каждый served criterion проверен named tests.
- Status/history показывают authoritative quantity/total; нет восстановления суммы из unit price.
- End-to-end tests с реальным calculator/executor, named regressions/build/format проходят либо исходное падение явно записано; новые acceptance failures не скрыты.
- API/invariants/scope сохранены; нет hidden fixture files или неподтверждённых assumptions.
- Completion evidence: одна quote, одна command, один receipt, одна history entry; replay/close/reopen не создают вторую сделку.

## Self-containment check

Receipt semantics/fields, formatting, matching, retention, test transport и actual Engine APIs заданы. Formula не копируется. Все fixture helpers находятся в указанном test file; соседние production layers не редактируются.
