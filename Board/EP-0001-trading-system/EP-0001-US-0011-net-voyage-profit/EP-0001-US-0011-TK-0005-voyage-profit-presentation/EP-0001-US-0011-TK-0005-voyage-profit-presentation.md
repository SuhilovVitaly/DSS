---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
ticket: EP-0001-US-0011-TK-0005-voyage-profit-presentation
title: Сводка в Finance и Trade history
stage: approved
layer: client
depends_on: [EP-0001-US-0011-TK-0001-voyage-finance-contract, EP-0001-US-0011-TK-0003-voyage-profit-realization, EP-0001-US-0011-TK-0004-voyage-profit-texts, EP-0001-US-0010-TK-0005-trade-cost-history]
files_touched: 5
serves: [AC-01, AC-03, AC-04]
created: 2026-09-21T12:56:24Z
revision: 1
---

# Сводка в Finance и Trade history

## Why

Игрок должен проверить один и тот же результат рейса в двух существующих точках: подробную разбивку в Finance и компактный итог рядом с подтверждёнными сделками в Trade history. Client обязан только отобразить snapshot, не пересчитывая profit, COGS, fuel или fee.

## Decisions

Пользователь не запросил новый экран. Сохраняются существующие Finance и TradeScreen; используются locale keys TK-0004 и authoritative values TK-0001/TK-0003.

## Assumptions

Finance показывает newest voyage по умолчанию и список/scroll последних records в пределах существующей панели; новая route/market screen не создаётся. Trade journal сохраняет current bounded trade entries и добавляет компактные voyage summary entries, дедуплицированные по `VoyageId`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs` | Trading summary — placeholder `:90–95`; render читает snapshot и показывает только current fee/debt `:178–203` | Заменить trading placeholder authoritative voyage list/detail; сохранить toolbar/close/modal behavior |
| `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs` | `TradeJournal` bounded50, связывает command results `:58–82`; US-0010 добавляет receipt fields | Добавить отдельные immutable voyage summary entries, merge/dedupe snapshot by `VoyageId`, общий bounded display order |
| `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs` | History рисует только trade entries `:173–215`; US-0010 заменяет total formatter | Нарисовать compact voyage separator/result row из authoritative DTO и locale, без arithmetic |
| `tests/DeepSpaceSaga.Client.Tests/FinanceScreenTests.cs` | Existing overlay/toolbar/input tests, 175 lines; voyage finance cases отсутствуют | Pixel/text-command or exposed presentation-model tests для breakdown/state/unknown/empty/scroll |
| `tests/DeepSpaceSaga.Client.Tests/TradeScreenTests.cs` | Existing Trade overlay/input tests, 273 lines; voyage summary отсутствует | Merge/dedupe/reopen/order/profit/loss/unavailable/partial remainder history tests |

## Public API after the change

No public API change. Client projection rules:

- Finance rows берутся прямо из выбранного `VoyageFinanceSnapshot`; отображаются route/state, Gross Sales, COGS, route fuel, assessed/paid/debt, optional nonzero event/passenger rows, net и unsold cargo.
- `CostOfGoodsSoldCredits`/`NetProfitCredits == null` показывают localized `Unavailable`; никаких zero или derived totals.
- Net sign выбирает presentation color/label only: positive profit, negative loss, zero neutral. Значение не пересчитывается.
- Trade history summary key — `VoyageId`; repeated snapshots обновляют одну строку при переходе `in_transit → awaiting_realization → finalized`, а не добавляют копии.
- Порядок voyage summaries соответствует `snapshot.VoyageFinances`; journal/history остаётся bounded 50 display entries newest-first. Reopen Trade сохраняет journal на `GameSessionHandle` и не дублирует уже seen IDs.

## Implementation steps

1. В Finance удалить только trading-results placeholder; Credits/station placeholders, toolbar, port next due/debt и close/pause mechanics не менять без отдельного owner ticket.
2. Построить маленькую private presentation projection из snapshot: selected/newest voyage, localized state/route, ordered component rows и unsold rows. Projection копирует values, не вычисляет net/COGS.
3. Добавить scroll/selection только если records не помещаются; использовать существующие `FinanceLayout` bounds и не добавлять новый screen/layout file. Empty/default показывает `Finance.NoVoyages`.
4. В `TradeJournal.Refresh` merge все authoritative `VoyageFinances` по stable ID. Existing trade receipt list и US-0010 formatting не изменять; summary entry содержит immutable DTO/current state.
5. В history drawing различать trade и voyage rows. Voyage row показывает route/state и authoritative net или unavailable; detailed component breakdown остаётся Finance. Profit/loss color выбирается по sign готового net.
6. Unknown COGS, partial Sell/unsold quantity, assessed/paid/debt и optional passenger/event rows покрыть tests. Passenger/event нулевые rows скрывать; ненулевые показывать раздельно.
7. Проверить repeated snapshot, close/reopen, A→B→A с повторным station ID, 51 entries retention и default immutable arrays. Не связывать identity со station name/time вместо `VoyageId`.
8. Headless UI tests проверяют текст/presentation state; ручной smoke: Station → Trade → Sell → Station → Finance, затем следующий Undock и повторное открытие.

## Out of scope

Engine arithmetic/posting, Contracts/locale, новый экран/график/таблица рынка, persistence UI, изменение modal pause, trade controls/quote, accounting export.

## Invariants

- Client не ссылается на Engine и не рассчитывает authoritative gameplay: `Documentation/00-Process/CLAUDE.md:22–56`.
- Finance получает snapshot из `SnapshotBuffer`: `FinanceScreen.cs:20,65–68,178–187`.
- Trade journal принадлежит session handle и переживает reopen: `GameSessionHandle.cs:86–87`; `TradeModel.cs:58–82`.
- Existing modal pause/toolbar/input сохраняются: `FinanceScreen.cs:97–127,165–187`; `TradeScreen.Render.cs` изменяет только history presentation.
- Пять files, production layer client, matching `DeepSpaceSaga.Client.Tests`.

## Tests

Named Finance tests:

- `Finance_shows_authoritative_profit_loss_and_break_even_components` (AC-01).
- `Finance_shows_unknown_cogs_and_net_as_unavailable_with_gross_sales` (AC-03).
- `Finance_separates_assessed_paid_and_outstanding_port_fee` (AC-04).
- `Finance_shows_unsold_cargo_without_adding_it_to_net` (AC-03).
- `Finance_shows_optional_passenger_and_event_rows_only_when_nonzero` (AC-04).
- `Finance_default_snapshot_shows_no_voyages_without_throwing` (AC-04).

Named Trade tests:

- `Trade_history_merges_one_voyage_summary_per_voyage_id` (AC-04).
- `Trade_history_updates_state_and_net_without_duplicate_on_repeated_snapshot` (AC-01/04).
- `Trade_history_uses_authoritative_net_and_never_recomputes_from_trade_entries` (AC-01).
- `Trade_history_marks_unknown_result_unavailable` (AC-03).
- `Trade_history_survives_reopen_and_caps_display_at_fifty` (AC-04).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FinanceScreenTests|FullyQualifiedName~TradeScreenTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Manual smoke:

```text
Dock A → Trade Sell (including partial case) → close to Station → Finance: compare receipt totals/components → next accepted Undock finalizes entry → Dock B → reopen Trade/Finance: same VoyageId appears once with same net.
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Оба существующих views, field-to-row mapping, null/sign/state semantics, journal identity/order/retention, five allowed files, named tests и smoke path заданы. Implementer не должен изобретать числа, открыть новый экран или искать accounting rules.
