---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
ticket: EP-0001-US-0003-TK-0003-trade-result-texts
title: Сообщения котировки и ограничений
stage: approved
layer: content-data
depends_on: [EP-0001-US-0003-TK-0001-trade-execution-contract]
files_touched: 3
serves: [AC-05, AC-06]
created: 2026-09-21T09:33:31Z
revision: 1
---

# Сообщения котировки и ограничений

## Why

Игрок должен понимать, почему подтверждение заблокировано и какая часть продажи будет выполнена. End state: RU/EN locale содержат одинаковые ключи и placeholders для quote/receipt flow.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

2026-09-21T09:33:31Z — сообщение: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Отдельных решений о формулировках пользователь не давал.

## Assumptions

Понятные сообщения не показывают технические QuoteId/revision или скрытую кассу станции. Existing StationBudgetLimit/SuccessResult/PartialResult переиспользуются; unit labels уже принадлежат US-0001. Никакого нового UI layout.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Locale/English.json | :35 InvalidData, :38 StationBudgetLimit, :72–78 pending/results | Добавить перечисленные TradeUX keys, не переименовывать existing keys |
| src/DeepSpaceSaga.Client/Data/Locale/Russian.json | :35,:38,:72–78 соответствующие переводы | Те же ключи и placeholder contract |
| tests/DeepSpaceSaga.Client.Tests/LocalizationTests.cs | :14–49 RequiredKeys; :51–61 loader test | Keys/placeholder parity, actual string.Format для новых сообщений |

## Public API after the change

No API change. Все строки имеют префикс `TradeUX.`:

| Key | English | Russian |
|---|---|---|
| QuoteLoading | Updating trade quote… | Обновляем котировку… |
| QuoteStale | Market conditions changed. Review the new quote and confirm again. | Условия торговли изменились. Проверьте новую котировку и подтвердите снова. |
| QuoteRequired | A current quote is required to confirm this trade. | Для подтверждения сделки нужна актуальная котировка. |
| InvalidQuote | This quote does not match the selected trade. Request a new quote. | Котировка не соответствует выбранной сделке. Запросите новую. |
| QuoteUnavailable | Quote unavailable. No trade was sent. | Котировка недоступна. Сделка не отправлена. |
| StationCapacityLimit | The station has no more storage space for this item. | На складе станции нет места для этого товара. |
| PartialPreview | Will sell {0} of {1} for {2} tokens. The remainder stays in cargo. | Будет продано {0} из {1} за {2} токенов. Остаток останется в трюме. |
| ReceiptUnavailable | The confirmed trade amount is unavailable. | Подтверждённая сумма сделки недоступна. |
| FuelServiceOnly | Fuel is available through refuelling only. | Топливо доступно только через заправку. |

## Implementation steps

1. Добавить exact keys/values, UTF-8, корректные JSON commas. Placeholders PartialPreview: {0}=executable quantity, {1}=requested, {2}=quote total, все отформатированные клиентом. Другие новые строки не принимают аргументов.
2. Existing PartialResult сохраняет {0}=item,{1}=actual,{2}=total,{3}=requested; SuccessResult {0}=item,{1}=actual,{2}=total. Не удалять unused старые Trade.* keys — обратная совместимость.
3. Расширить LocalizationTests: проверить одинаковые ключи двух языков, expected placeholder sets, string.Format с различными quantity/total; проверить actual text, а не только наличие raw ключа.

## Out of scope

Production .cs, layout, экономические данные/profile rates, raw diagnostic IDs в UI, новые языки.

## Invariants

- Locale resolution/fallback: src/DeepSpaceSaga.Client/Localization.cs:14–25 и LoadLocaleFile(string).
- Existing partial/success placeholder semantics: TradeScreen.Render.cs:164–173; locale :76–77.
- Нет раскрытия hidden station Credits: src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:17–23.

## Tests

LocalizationTests:
- `Trade_quote_keys_exist_in_both_locales` — AC-05/06.
- `Trade_quote_messages_have_matching_expected_placeholders` — AC-05/06.
- `Partial_preview_and_result_format_requested_actual_and_total` — AC-06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только два JSON и matching test file; все steps выполнены.
- Named tests покрывают served criteria для текстовой части; ошибки формата отсутствуют.
- Build/format/tests пройдены либо исходные failures отдельно описаны.
- Existing keys/invariants и scope сохранены; никаких скрытых файлов/неразрешённых вопросов.
- Каждый текст можно проверить через Localization.LoadLocaleFile и string.Format.

## Self-containment check

Все значения ключей, placeholders, test APIs и команды приведены; implementer не выбирает новый UX или экономические правила.
