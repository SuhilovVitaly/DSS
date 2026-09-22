---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
ticket: EP-0001-US-0002-TK-0004-market-flow-content
title: Потоки пяти рынков и локализация состояний
stage: approved
layer: content-data
depends_on: [EP-0001-US-0002-TK-0003-hourly-market-simulation]
files_touched: 6
serves: [AC-01, AC-02, AC-05, AC-06]
created: 2026-09-21T08:59:06Z
revision: 2
---

# Потоки пяти рынков и локализация состояний

## Why

Пять профилей US-0001 получают заданные эпиком потоки и ограниченные склады; без shipped economy blocks готовая Engine механика не будет видна игроку. Локализация подготавливает отображение authoritative состояний в TK-0005.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Пути ниже относительны D:/DeepSpaceSaga/DSS. US-0001 уже должна создать profiles/demo; TK-0003 должен реализовать bounded economy.

## Decisions

D-01, 2026-09-21T08:59:06Z: «сделай следующую стори». Числа rates взяты из эпика Documentation.md:112–116, размерные коэффициенты/targets — :95,118. Пороговые значения и распределение inputs описаны как assumptions истории.

D-02, revision 2, 2026-09-22: при реализации выяснилось, что shipped economy блоки неизбежно ломают два существующих теста вне allowlist: `CatalogCompatibilityTests.Profile_without_economy_keeps_us0001_fingerprint` (TK-0002 закрепил «у shipped-профилей Economy == null на этой стадии») и `StationMarketDemoContentTests.Unknown_shipping_profile_item_blocks_session_creation` (подмена item.ice на item.missing теперь упирается в economy.hourlyOutputs раньше, чем в неизвестный item). Пользователь одобрил расширение allowlist до 6 файлов; golden-вектор US-0001 fingerprint не меняется.

## Assumptions

- Все пять shipped profiles используют productionSource Profile. У Transit HourlyInputs/Outputs пусты, весь спрос — независимое HourlyConsumption.
- У остальных четыре профиля production group использует весь список спроса как inputs одного batch, consumption пуст. Нет бесплатного output при нехватке любого входа.
- Цели и ставки исходной экономики не являются доказанным долгосрочным балансом. US-0013 проверит устойчивость, silicon supply и исчерпание Transit.
- Сохраняются все US-0001 initial stocks/credits/refuel/IDs/sizeFactors и demo geometry. Новые поля добавляются только economy block.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json | Плановый файл US-0001/TK-0004 с точной таблицей bootstrap пяти рынков; пока отсутствует в текущем checkout | Добавить economy каждого профиля по таблице ниже |
| src/DeepSpaceSaga.Client/Data/Locale/English.json | :22 stock header, :38 budget reason, :76–77 success/partial strings; US-0001 добавляет units | Только новые market state/capacity/remaining keys |
| src/DeepSpaceSaga.Client/Data/Locale/Russian.json | Соответствующий словарь локализации; US-0001/TK-0003 unit keys | Русские эквиваленты новых ключей |
| tests/DeepSpaceSaga.Client.Tests/MarketFlowContentTests.cs | Новый файл; client tests имеют Engine friend access (Engine.csproj:17) | Проверки shipped data, public session hour и locale formats |
| tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs | :590–599 цикл по shipped-профилям требует Economy == null (стадия TK-0002) | Только этот цикл: у shipped Economy задан, fingerprint ≠ legacy, без Economy = legacy; golden-вектор не трогать (D-02, revision 2) |
| tests/DeepSpaceSaga.Client.Tests/StationMarketDemoContentTests.cs | Unknown_shipping_profile_item_blocks_session_creation подменяет item.ice только в supply/inventory | Та же подмена в economy.hourlyOutputs[0]/stockTargets[0]; ничего больше (D-02, revision 2) |

## Public API after the change

No API change. JSON shape из TK-0002; все ключи ниже внутри optional economy объекта. Все abbreviated IDs в таблицах означают item.<name>.

Общие поля каждого блока: productionSource="Profile", shortageThresholdPermille=500, surplusThresholdPermille=1500, budgetRegenerationDivisorPerDay=24. Массивы Hourly* в JSON lowerCamelCase; каждый entry={itemTypeId,quantity}. stockTargets entry={itemTypeId,targetStock}. Не записывать maxStock/maxBudget отдельно: они вычисляются Engine как 2×effective target/initialBudget.

| Profile | hourlyOutputs | hourlyInputs | hourlyConsumption |
|---|---|---|---|
| market.mining | ice18, iron-ore24, magnesium-ore10, carbon-ore8 | water4, food-rations6, energy-cells8, steel2 | [] |
| market.industrial | steel16, energy-cells12, electronics4 | iron-ore20, magnesium-ore8, carbon-ore6, silicon8, water2, food-rations3 | [] |
| market.hydroponic | water24, protein-mass10, food-rations8 | ice20, energy-cells8, steel2, electronics1 | [] |
| market.transit | [] | [] | water8, food-rations12, energy-cells6, steel2, electronics2 |
| market.scientific-military | electronics5 | silicon7, energy-cells6, steel3, water2, food-rations4 | [] |

| Profile | stockTargets для Resource | stockTargets для Good |
|---|---|---|
| mining | ice/iron-ore/magnesium-ore/carbon-ore =108 каждый | water/food-rations/energy-cells/steel =72 каждый |
| industrial | iron-ore/magnesium-ore/carbon-ore/silicon =132 каждый | steel/energy-cells/electronics/water/food-rations =88 каждый |
| hydroponic | ice=120 | water/protein-mass/food-rations/energy-cells/steel/electronics =80 каждый |
| transit | нет | water/food-rations/energy-cells/steel/electronics =96 каждый |
| scientific-military | silicon=96 | electronics/energy-cells/steel/water/food-rations =64 каждый |

Targets в таблице — Medium. US-0001 demo Transit Large получает targets144/max288, все остальные сохраняют Medium. Fuel/uranium отсутствуют в rates/targets. RefuelStockKg остаётся как в US-0001.

Новые locale keys, сохранять existing keys/арность:

| Key | English | Russian |
|---|---|---|
| TradeUX.StockShortage | Shortage | Дефицит |
| TradeUX.StockNormal | Normal | Норма |
| TradeUX.StockSurplus | Surplus | Избыток |
| TradeUX.MarketStockSummary | {0} · target {1} · max {2} | {0} · цель {1} · максимум {2} |
| TradeUX.StationStorageLimit | Limited by station storage capacity | Ограничено свободным складом станции |
| TradeUX.PartialWithRemaining | {0}: {1} of {3} completed · {2} tokens · remaining {4} | {0}: выполнено {1} из {3} · {2} токенов · остаток {4} |

MarketStockSummary args = localized state, target quantity with unit, max quantity with unit. PartialWithRemaining сохраняет первые четыре аргумента старого PartialResult, добавляет requested−executed quantity с unit; raw amount не считается по stock delta.

## Implementation steps

1. Добавить строго все поля economy блоков, в указанном schema shape. Не менять schemaVersion=1, initialInventory/credits/refuel или профили supply/demand US-0001.
2. Проверить все effective sizes, max=2target, initial stock≤max, ставки ≤cap. Legacy settings/scenarios не трогать.
3. Добавить locale keys с одинаковым набором и placeholders. Не менять подписи единиц US-0001.
4. В тестовом файле читать реальные settings/profiles/demo. Проверить точное соответствие таблицам; создать Engine с fixed clock/real registry или использовать существующий LocalGameSessionConnection для public-boundary случая. Никаких записей в исходные JSON из tests.
5. Для сравнения первого часа каждой станции допустима in-memory вариация demo ScenarioFile: корабль уже docked к выбранной station, Speed0, GameTime0, профильные initial stocks исходные; session через new LocalGameSessionConnection(engine) и IGameSessionConnection.TravelStationAsync(new StationTravelCommand(uniqueId, StationDistrict.Market)). Это интеграционный fixture, shipped scenario не меняется. Данные docked new-game не выдаются за save; Engine ставит исходное расписание сборов.
6. Через результат TravelStation проверить GameTimeMs=HourMs, соответствующий DockedStationTrade и изменения ровно по таблице. Повтор того же CommandId возвращает то же время/stock. Fuel неизменен. Величины profile inputs/output не выводить из фактической реализации — expected явно следуют таблице.
7. Manual demo после TK-0005: dock к Mining в US-0001 demo, открыть Trade, затем закрыть оба модальных окна, дать пройти 1 игровой час и открыть Trade повторно. Переход в другой district можно проверить отдельно через существующую команду, не добавляя кнопку ожидания в Trade.

## Out of scope

Settings/catalog/scenario edits, production C#, recipes/factory-types, UI runtime, изменение legacy fingerprint или update baseline test snapshots вне allowlist. Нет балансного прогона на десять суток.

## Invariants

Baseline rates — Board/EP-0001-trading-system/Documentation.md:112–118. Integer quantities — EngineRequirements.md:5128–5160. Fuel не обычный товарный flow — эпик :97. Shipping bootstrap US-0001 остаётся неизменным; изменяется только subsequent calendar behavior.

## Tests

В MarketFlowContentTests:

- Shipping_market_rates_and_targets_match_epic_baseline (AC-01/02): exact sets/numbers для пяти ролей.
- Every_size_has_valid_stock_bounds_and_initial_budget (AC-01/05): все четыре sizes; max=2target, scaled initial stocks≤max, dailyGrant floor(maxBudget/24).
- Public_station_travel_applies_one_hour_of_shipping_market_flow (AC-02), theory пяти ролей: exact stock deltas, stock bounds, timestamp и idempotent repeat receipt.
- Shipping_fuel_and_legacy_scenarios_have_no_hourly_cargo_flow (AC-01/02): Fuel unchanged, Default/Docked profile assignment отсутствует.
- English_and_russian_market_keys_have_matching_arguments (AC-06): все placeholders, enum label key mapping.
- Market_content_is_loaded_from_shipped_data (AC-01): реальные output files доступны через существующие copy rules.

Примеры expected первого часа до сделки: Mining ice162→180/iron162→186/water36→32; Industrial steel132→148/silicon66→58; Hydroponic water120→144/ice60→40; Large Transit water144→136/rations144→132; Scientific electronics96→101/silicon48→41. Budget test без fees/trades: Medium Mining 9600→9633 при max19200/daily800. Эти числа основаны на bootstrap US-0001/TK-0004 и baseline выше.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketFlowContentTests|FullyQualifiedName~StationMarketDemoContentTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в разрешённых Code context файлах; лимит files_touched соблюдён.
- Каждый criterion из serves покрыт указанными named tests в пределах вклада тикета.
- Named tests и build/lint соответствующего layer проходят; исходные failures записаны с точной командой и сообщением.
- Public API, invariants, out-of-scope и prerequisite contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов и скрытых изменений за allowlist.
- End state проверяется тестом, diff или описанным наблюдаемым поведением; planning status не означает выполненный production-код.

## Self-containment check

Все пять наборов rates/targets, locale keys, tests и числовые примеры даны без поиска балансных решений. Четыре файла принадлежат content-data плюс matching Client tests. End state: shipped профили меняют рынки на разрешённой часовой границе, а словари готовы для UI.
