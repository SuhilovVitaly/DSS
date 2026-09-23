---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
ticket: EP-0001-US-0007-TK-0003-market-event-content
title: Восемь событий и локализованные объяснения
stage: approved
layer: content-data
depends_on: [EP-0001-US-0007-TK-0002-market-event-catalog]
files_touched: 5
serves: [AC-01, AC-04, AC-05]
created: 2026-09-21T11:08:58Z
revision: 1
---

# Восемь событий и локализованные объяснения

## Why

Shipping game должен загружать ровно восемь согласованных событий из данных, а Trade — объяснять их на RU/EN. Этот тикет фиксирует начальный tuning baseline и cross-file completeness, не реализуя scheduler или UI.

Matching test project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Decisions

D-01, 2026-09-21T11:08:58Z: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md».

## Assumptions

- Числа ниже — initial balance inputs; US-0013 вправе менять chance/duration/multipliers с evidence, но не IDs/семантику.
- Localization keys находятся в существующем `TradeUX` namespace и описывают наблюдаемую причину/эффект, не точную формулу.
- Reactor/decompression/blockade/convoy/quarantine доступны всем пяти профилям; hydroponics failure только Hydroponic, scientific contract только Scientific/Military, repair boom — Mining/Industrial/Transit. Irrelevant item component для конкретного профиля равен no-op.
- `item.fuel` не входит в cargo market effects. Фраза «топливо дорожает» реализуется как route fuel multiplier в US-0008, не stock item.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Markets/station-market-events.json | Новый shipping content file | schemaVersion1 и exact eight definitions по таблице ниже |
| src/DeepSpaceSaga.Client/Settings.json | :3–12 typeData paths; stationMarketProfiles уже добавлен параллельной работой | Добавить `stationMarketEvents` path, не менять default scenario/economy versions |
| src/DeepSpaceSaga.Client/Data/Locale/English.json | Existing `TradeUX` keys around :65–100 | Добавить 24 keys: Name/Description/Effect для восьми IDs плюс 3 common event labels |
| src/DeepSpaceSaga.Client/Data/Locale/Russian.json | Matching RU `TradeUX` section | Полный RU набор тех же keys/placeholders |
| tests/DeepSpaceSaga.Client.Tests/StationMarketEventContentTests.cs | Новый test file | Packaged Settings load, exact semantic table, localization parity/format tests |

## Public API after the change

Новых C# API нет. Settings получает:

```json
"stationMarketEvents": "Data/Markets/station-market-events.json"
```

JSON definitions должны точно соответствовать таблице. `P/D/$` — production/demand/price permille; `Δ` — one-time activation stock delta. Неуказанные multipliers равны1000, delta0. Все eligible profiles: `market.mining`, `market.industrial`, `market.hydroponic`, `market.transit`, `market.scientific-military`.

| Definition ID | Eligible profiles | Priority | Chance/h | Duration h | Item effects | Route effect |
|---|---|---:|---:|---:|---|---|
| event.reactor-accident | all five | 90 | 4‰ | 6–18 | energy-cells P500/D1600/$1500 | none |
| event.decompression | all five | 80 | 5‰ | 4–12 | water D1600/$1350; food-rations D1500/$1300; steel D1300/$1200 | none |
| event.hydroponics-failure | hydroponic | 85 | 4‰ | 8–24 | food-rations P300/D1600/$1450; protein-mass P300/$1400 | none |
| event.pirate-blockade | all five | 100 | 3‰ | 6–18 | water, food-rations, steel, electronics: P700/D1300/$1250 | Unavailable, maxEdges1, time1000, fuel1400, risk.pirate-blockade |
| event.cargo-convoy | all five | 40 | 8‰ | 4–8 | water Δ+24/$800; food-rations Δ+24/$800; steel Δ+16/$850 | none |
| event.quarantine | all five | 95 | 3‰ | 8–24 | water D1400/$1250; food-rations D1500/$1300; energy-cells D1200/$1150 | Restricted, maxEdges1, time1500, fuel1200, risk.quarantine |
| event.repair-boom | mining, industrial, transit | 60 | 6‰ | 8–20 | steel P1300/$1150; iron-ore D1600/$1350; magnesium-ore D1500/$1300; electronics D1500/$1350 | none |
| event.scientific-contract | scientific-military | 70 | 5‰ | 6–16 | electronics P500/$1450; silicon D1700/$1500; energy-cells D1500/$1350 | none |

Every definition uses keys `TradeUX.Event.<PascalId>.Name`, `.Description`, `.Effect`; JSON stores suffix after `TradeUX.` or the full key consistently with TK-0002 loader choice. Client uses exactly one convention; test prohibits mixed prefixes.

Common keys: `TradeUX.ActiveEvents`, `TradeUX.EventRemainingHours` with one numeric placeholder, `TradeUX.EventPermanent`.

## Implementation steps

1. Создать strict JSON с `schemaVersion:1`, exact eight rows and numeric values above. Arrays definitions/profiles/item effects сортировать ordinal для reviewability, хотя fingerprint order-independent.
2. Use stable item IDs только из 12-item catalog. No `item.fuel`, category wildcard, raw RES/ITM codes, duplicate item effects or implicit defaults omitted where schema requires values.
3. Для всех definitions записать nonempty DisplayNameKey/DescriptionKey/EffectSummaryKey и eligible profiles точно по таблице, используя IDs `market.mining`, `market.industrial`, `market.hydroponic`, `market.transit`, `market.scientific-military`. Engine component applies production/demand multiplier only where profile actually has that flow; other component is no-op, not content error.
4. Blockade/quarantine route objects exactly as table. Остальные omit/null routeEffect; не добавлять edge/station IDs.
5. English/Russian names are concise nouns. Description answers «что случилось», Effect answers «что изменилось для торговли». Не обещать actual blocked edge before US-0008; use «часть направлений может быть ограничена».
6. `EventRemainingHours` rounds display upward from positive remaining game ms. Localization placeholder count/type identical in RU/EN; no embedded line breaks or manual `%` formatting.
7. Test loads real Settings through EngineContentLoader dependency and asserts registry has exact table. This verifies packaging/copy path, not only raw JSON parse.
8. Test reads both locale dictionaries and requires all 27 keys, nonempty distinct values for Name/Description/Effect, placeholder parity, and no raw definition ID shown as translated text.
9. Do not modify scenarios: scheduler materializes events from catalog in TK-0004.

## Out of scope

Engine lifecycle/effects, contracts/client code, scenario-authored demo event, graph/voyage behavior, tuned probabilities proof, extra locales, trend icon/assets. Не изменять item/profile catalogs или existing translation wording outside new keys.

## Invariants

- Content-driven simulation data is loaded through Settings and registry: Documentation/00-Process/CLAUDE.md:65–85.
- Exact eight names and semantics come from epic: Board/EP-0001-trading-system/Documentation.md:100.
- Events cannot fully disable economy: TradingSystemMvpStories.md:242–253.
- Fuel cargo market remains separate service: epic Documentation.md:97; US-0002 excludes Fuel from cargo hourly flow.

## Tests

В `StationMarketEventContentTests.cs`:

- `Packaged_settings_load_exact_eight_market_events` (AC-01).
- `Shipping_event_table_matches_reviewed_ids_chances_durations_and_effects` (AC-01/04): assert every value from table, not self-derived snapshot.
- `All_event_item_and_profile_references_resolve` (AC-01).
- `Only_blockade_and_quarantine_declare_bounded_route_effects` (AC-04).
- `English_and_russian_contain_complete_event_explanations` (AC-05).
- `Event_localization_placeholders_have_parity` (AC-05).
- `Packaged_client_output_contains_market_event_catalog` (AC-01): after build, declared relative file exists at expected content path; no source tree mutation.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~StationMarketEventContentTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Пять разрешённых файлов содержат exact table, Settings declaration и matching translations/tests.
- AC-01/04/05 content contribution покрыт named tests.
- Client tests/build/format проходят либо baseline failure записан отдельно с exact asset/path error.
- IDs, keys, values, route limits и no-Fuel invariant соблюдены.
- Нет scheduler/UI/graph changes.
- Shipping output реально содержит declared JSON.

## Self-containment check

Exact data table, keys, file paths, translation semantics and tests are specified. Implementer не выбирает числа или события. End state: packaged content loads all eight definitions and both locales can explain each event.
