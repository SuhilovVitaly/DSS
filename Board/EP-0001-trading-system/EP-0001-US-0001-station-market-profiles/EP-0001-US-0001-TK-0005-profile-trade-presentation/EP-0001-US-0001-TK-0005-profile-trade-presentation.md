---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
ticket: EP-0001-US-0001-TK-0005-profile-trade-presentation
title: Названия и единицы в существующем Trade
stage: approved
layer: client
depends_on: [EP-0001-US-0001-TK-0003-market-catalog-content, EP-0001-US-0001-TK-0004-five-market-demo]
files_touched: 3
serves: [AC-05, AC-06]
created: 2026-09-21T08:33:04Z
revision: 1
---

# Названия и единицы в существующем Trade

## Why

Готовые профильные markets и каталог должны корректно отображаться в существующем Trade: electronics получает имя, а количество товара обозначается его торговой единицей отдельно от массы. AC-05 здесь покрывает UI-путь и отсутствие смешивания рынков; реальное исполнение покрыто TK-0004.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Пути Code context относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:33:04Z: «Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md».
Отдельных решений об UI пользователь не давал.

## Assumptions

- Используется существующий ID → locale mapping; в snapshot уже есть ItemTypeId и UnitMassKg (Contracts/StationTradeSnapshot.cs:23–40). Для фиксированных 12 MVP IDs допустим presentation-only unit label; новые domain enum/Contracts не требуются.
- Неизвестному ID — generic units, не угадывать килограммы по UnitMassKg.
- У electronics нет обязательного icon asset: сохранить существующий null/frame fallback. Генерация ассета не входит.
- Supply/demand не требует новой колонки/панели: локальные различия видны по existing rows/stock, роль названа demo-станцией.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeItemPresentation.cs | :4–18 name mapping, :22–36 description; :40–54 icon fallback | Electronics name/description и presentation helpers для единиц |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :71–73 строки рынка; :110–121 unit mass/quantity; :137–159 preview/confirm; :165–173 journal | Использовать имена и unit labels в существующих местах без новой экономической логики |
| tests/DeepSpaceSaga.Client.Tests/TradeUxTests.cs | :14–29 snapshot fixtures; :68–73 Fuel separation; :88–98 refresh/empty; :100 и далее recording connection/render/click fixture | Новые cases отображения и переключения snapshot, сохранить existing regressions |

## Public API after the change

No API change. Engine/session DTO и команды не меняются. Добавить только internal helpers в TradeItemPresentation:

```csharp
internal static string ItemUnitLabel(string itemTypeId, bool singular = false);
internal static string FormatQuantity(string itemTypeId, long quantity);
internal static string FormatUnitMass(string itemTypeId, long unitMassKg);
```

Mapping:
- item.food-rations → Trade.UnitRation / Trade.UnitRationSingle.
- item.energy-cells → Trade.UnitCell / Trade.UnitCellSingle.
- item.electronics → Trade.UnitBlock / Trade.UnitBlockSingle.
- item.ice, item.iron-ore, item.silicon, item.magnesium-ore, item.carbon-ore, item.water, item.steel, item.protein-mass, item.fuel и legacy item.uranium-ore → Trade.UnitKg для обеих форм.
- unknown → Trade.UnitGeneric / Trade.UnitGenericSingle.
- ItemDisplayName electronics → Trade.ItemElectronics; ItemDescription electronics → Trade.DescriptionElectronics; ItemImagePath electronics остаётся null.

Dependency locale contract TK-0003: ключи выше, плюс Trade.QuantityWithUnit "{0}", Trade.AmountWithUnit "{0} {1}", Trade.QuantityMass "1 {0} = {1} kg/кг", Trade.ConfirmBuyWithUnit "Buy/Купить {0} {1} for/за {2}", Trade.ConfirmSellWithUnit аналогично. Форматирование CultureInfo.CurrentCulture, параметры quantity/unit/credits передаются в этом порядке. Единицы — подписи, не вычислительные коэффициенты.

## Implementation steps

1. Дополнить mapping и helpers. Никаких disk reads/catalog loading в render loop, только существующая Localization.Get.
2. В market rows stock показывать через FormatQuantity(item.ItemTypeId, item.StockQuantity); cargo quantity column, если отображается, также снабдить той же unit label. Цену/сортировку/числа snapshot не изменять.
3. В detail panel заменить generic UnitMass на FormatUnitMass: например «1 блок = 1 кг», «1 рацион = 1 кг». Label quantity — Trade.QuantityWithUnit с plural unit; Fuel оставляет свой service text и kg.
4. Preview cargo count и confirm Buy/Sell показывают выбранную единицу; масса, свободная вместимость и tank capacity остаются kg. Refuel confirmation остаётся существующим. Journal executed/requested quantity можно передать существующему string format как FormatQuantity, не вычисляя новую цену; итог остаётся текущим journal behavior.
5. Обработать длинные новые имена в текущих прямоугольниках (existing painter clipping/fit либо локальное ограничение текста в Render), без смены layout/screen. Никаких новых production files.
6. Добавить tests ниже в имеющийся TradeUxTests, используя существующие RecordingConnection/Fixture и Skia render. Проверять command payload и числовые preview, чтобы единица не поменяла количество.
7. После TK-0004 выполнить ручной smoke пяти рынков на English/Russian, проверить electronics/rations/cells/Fuel, отсутствие обрезания кнопок и смешивания соседних строк. Можно использовать временные render artifacts, не добавляя их в repo.

## Out of scope

Изменения TradeModel pricing/max calculation, Contracts, Engine, JSON/locales, assets, source scenario, новые UI-панели/экраны. Не показывать бюджет станции напрямую и не добавлять client economic rules.

## Invariants

- Authoritative Items только текущей docked station: src/DeepSpaceSaga.Engine/SimulationEngine.cs:549–586.
- Fuel отделён от cargo: tests/DeepSpaceSaga.Client.Tests/TradeUxTests.cs:68–73.
- Масса не равна единице количества: Documentation/01-Requirements/EngineRequirements.md:5128–5160; snapshot UnitMassKg — Contracts/StationTradeSnapshot.cs:40.
- Trade sends quantity независимо от presentation: TradeScreen.cs:144 в той же папке UI.
- Existing Station → Trade и nested modal не меняются: Documentation/01-Requirements/EngineRequirements.md:5100–5102.

## Tests

Добавить в TradeUxTests:

- Electronics_uses_localized_name_description_and_block_unit (AC-06): helper не возвращает item.electronics/raw locale key, null icon допустим.
- Item_units_distinguish_quantity_from_mass (AC-06), theory: resource kg, rations, cells, electronics, unknown generic; FormatUnitMass с 3 кг остаётся «1 <unit> = 3 kg/кг», количество не масштабируется.
- Profile_snapshot_refresh_replaces_market_rows (AC-05): два snapshot с разными station IDs/items (пять профильных sets из TK-0004 можно параметризовать), старые rows исчезают, stock совпадает, нет синтетических items. Использовать Model.Refresh, существующие selection rules.
- Profile_item_buy_and_sell_send_one_trade_unit (AC-05, AC-06): electronics/rations/cells, выбрать cargo module и qty1, клик Confirm, recording connection получает Quantity=1 и верные ItemTypeId/CommandType; snapshot не меняется до ответа.
- Fuel_remains_refuel_only_after_profile_refresh (AC-06): Fuel только отдельная вкладка/режим, tank target и Quantity кг неизменны.
- Profile_item_render_handles_long_names_and_unit_labels (AC-06): Render/клики для длинных имён, форматированные строки корректны; pixel-perfect golden не вводить. Визуальное отсутствие наложений подтвердить smoke отдельно.

Оба locale JSON проверяет TK-0003; UI test читает существующий active locale без сброса глобального Localization Lazy. Не менять process-global язык между параллельными xUnit tests.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeUxTests|FullyQualifiedName~TradeScreenTests|FullyQualifiedName~StationMarket"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Финальная интеграционная проверка истории после всех пяти тикетов:

```text
dotnet test D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet build D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Не заявлять visual/runtime success на основании одного build. Воспроизводимые исходные failures записать отдельно с командой и сообщением.

## Definition of Done

- Все implementation steps выполнены только в Code context; files_touched не превышен.
- Каждый AC из serves покрыт указанными named tests в пределах вклада этого тикета.
- Named tests и проверки matching project проходят; конкретные исходные failures записаны отдельно, успех не заявляется при пропуске.
- Public API, invariants, out-of-scope и dependency contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы вне allowlist.
- Независимо проверяемый результат подтверждён тестовым выводом и diff; этот planning-документ сам по себе не является evidence реализации.

## Self-containment check

Все presentation mappings, locale keys, подписи quantity/mass, разрешённые места UI и тестовые fixtures определены; economic behavior остаётся dependency. Три файла образуют отдельный Client merge unit. End state: существующий Trade отображает профильный ассортимент, electronics и правильные единицы, отправляя прежние authoritative команды.

