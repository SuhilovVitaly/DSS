---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
ticket: EP-0001-US-0015-TK-0005-local-quote-transport
title: LocalClient transport котировки
stage: approved
layer: local-client
depends_on: [EP-0001-US-0015-TK-0001-trade-quote-contract, EP-0001-US-0015-TK-0004-authoritative-quote-issuer]
files_touched: 2
serves: [AC-03, AC-06, AC-07]
created: 2026-09-21T15:10:56Z
revision: 1
---

# LocalClient transport котировки

## Why

Client не может вызывать Engine напрямую. LocalClient должен реализовать новый request-response method тем же cancellable/disposal-safe способом, что существующие session operations, и вернуть DTO без пересчёта или преобразования. Matching integration test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Decisions

D-01, 2026-09-21T15:10:56Z: пользователь запросил planning указанной story; дополнительных transport-решений не поступало. Сохраняется архитектурная граница `Client → IGameSessionConnection → LocalGameSessionConnection → Engine`.

## Assumptions

- Engine `GetTradeQuote` синхронно и сам защищает world state lock; LocalClient не вводит второй market cache или scheduler.
- Cancellation проверяется до обращения к Engine. После получения immutable DTO операция завершена; отмена не может откатить или изменить рынок, поскольку quote read-only.
- Disposal проверяется так же, как SendCommand/Travel; exception types остаются стандартными.
- Matching coverage размещается в Client.Tests, потому что отдельного `DeepSpaceSaga.Engine.LocalClient.Tests` в solution нет; это существующий pattern `LocalSessionIntegrationTests.cs`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs | :13–20 adapter fields; :102–125 direct command/travel methods; :194–211 disposal | Реализовать GetTradeQuoteAsync как thin adapter |
| tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs | :20–46 real engine/connection integration; далее save/load/cancellation coverage | Добавить quote roundtrip, cancellation, disposed, concurrency/load tests |

## Public API after the change

```csharp
public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(
    TradeQuoteRequest request,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    ObjectDisposedException.ThrowIf(_disposed, this);
    return ValueTask.FromResult(_engine.GetTradeQuote(request));
}
```

Метод возвращает exact immutable object/value, сформированный Engine. Он не читает snapshot, не рассчитывает Max/price, не подменяет DisabledReason и не отправляет gameplay command.

## Implementation steps

1. Добавить method рядом с SendCommand/Travel, соблюдая cancellation-before-disposal pattern, принятый в текущем adapter.
2. Не использовать `_saveGate` и не останавливать engine loop: thread safety принадлежит `_engine.GetTradeQuote` world lock.
3. Проверить через реальную `SimulationEngine` profile-market fixture, что contract method и direct Engine method возвращают одинаковые binding/revision/curve/total/reasons и не меняют snapshot state.
4. Проверить pre-cancelled token, disposed connection и parallel snapshot publication/quote request без deadlock.
5. Save/load integration: quote до загрузки не принимается новой connection; новый request получает новый QuoteId и сохранённую revision.

## Out of scope

Client TradeModel/TradeScreen, GameSessionHandle orchestration, command submit/receipt, UI loading state, network transport, retries и automatic refresh. Они принадлежат US-0003 или будущему network adapter.

## Invariants

- Client зависит только от public contract: `Documentation/00-Process/CLAUDE.md:22–56`.
- Render loop не ждёт Engine synchronously; quote вызывается user-action path, не per-frame: `CLAUDE.md:58–86`.
- Текущий adapter pattern: `LocalGameSessionConnection.cs:102–125,128–147`.
- Quote read-only; market revision меняется только transaction helpers TK-0003.

## Tests

В `LocalSessionIntegrationTests`:

- `Local_connection_returns_exact_authoritative_quote_without_world_mutation` — AC-03/06.
- `Quote_request_honors_precancelled_token_and_disposed_connection` — transport contract.
- `Quote_request_during_snapshot_loop_completes_without_deadlock` — concurrency.
- `Save_load_preserves_revision_but_invalidates_old_quote_token` — AC-06/07.
- `Repeated_request_id_roundtrips_same_quote_and_conflict_is_preserved` — AC-06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~LocalSessionIntegrationTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine.LocalClient\DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в двух разрешённых файлах.
- AC-03/06/07 покрыты named integration tests в пределах transport contribution.
- Named tests/build/format проходят либо baseline failure записано отдельно.
- LocalClient возвращает exact Engine DTO и не содержит economic rules/cache.
- API/invariants/out-of-scope соблюдены; cancellation/disposal/concurrency проверены.
- Нет незаписанных assumptions, blocking questions или скрытой работы.

## Self-containment check

Exact method body, lock ownership, integration fixture location и failure semantics заданы. Implementer не должен искать отдельный test project или проектировать Client preview.

