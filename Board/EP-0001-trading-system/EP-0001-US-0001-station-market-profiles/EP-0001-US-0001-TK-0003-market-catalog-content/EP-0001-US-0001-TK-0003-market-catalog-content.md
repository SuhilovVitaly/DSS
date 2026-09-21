---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
ticket: EP-0001-US-0001-TK-0003-market-catalog-content
title: Базовый каталог и локализованные единицы
stage: approved
layer: content-data
depends_on: [EP-0001-US-0001-TK-0002-profile-market-bootstrap]
files_touched: 5
serves: [AC-01, AC-06, AC-07]
created: 2026-09-21T08:33:04Z
revision: 1
---

# Базовый каталог и локализованные единицы

## Why

Профили должны ссылаться на единый полный базовый каталог, а Trade — показывать названия и единицы в обоих языках. Электроника уже согласована требованиями, но отсутствует в текущем JSON (src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json:2–73). AC-06 здесь покрывает словари; использование UI подключает TK-0005.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Пути ниже относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:33:04Z: «Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md».
Дополнительных ответов пользователя нет; значения electronics взяты из EngineRequirements.md:5128.

## Assumptions

- Базовых MVP-позиций 12, но в полном каталоге после изменения 13: legacy item.uranium-ore сохраняется с прежними параметрами.
- TradeUnit.Piece используется для electronics; «блок» — пользовательское название единицы, новый enum не нужен.
- CatalogVersion=1 и schemaVersion=2 не меняются: их текущие значения поддержаны loader/registry, а изменение экономического состава уже обнаруживает fingerprint (GameDataRegistry.cs:31–36, 71).
- LegacyCatalogFingerprint в Settings остаётся прежним. Добавление electronics намеренно делает старые economic identities несовместимыми; миграция вне scope.
- Зависимость от TK-0002 необходима из-за CatalogCompatibilityTests: там устраняется ложное требование равенства актуального и legacy fingerprint до расширения каталога.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json | :4–72 шесть Good; units/storage/prices явные; :75–76 версии | Sci-Fi displayName существующих товаров, добавить electronics |
| src/DeepSpaceSaga.Client/Data/Items/Resource/items-resource.json | :4–73 шесть ресурсов, uranium :52–61 | Только displayName пяти базовых ресурсов; legacy uranium оставить |
| src/DeepSpaceSaga.Client/Data/Locale/English.json | :147–170 item names/descriptions; :51,56,69–70 общие quantity formats | Имена 12 позиций, electronics description, новые unit/format keys |
| src/DeepSpaceSaga.Client/Data/Locale/Russian.json | :147–170 прежние имена/описания | Русские эквиваленты тех же ключей |
| tests/DeepSpaceSaga.Client.Tests/StationMarketCatalogContentTests.cs | Новый файл; тестовый проект ссылается на Client (:20), Engine friend access разрешён в src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj:17 | Проверка shipped catalog, locale formats и сохранённого legacy baseline |

## Public API after the change

No API change. Новый item объект:

```json
{
  "typeId": "item.electronics",
  "displayName": "Control Electronics",
  "unitMassKg": 1,
  "basePriceCredits": 150,
  "tradeCategory": "Good",
  "catalogCode": "ITM-3006",
  "tradeUnit": "Piece",
  "storageKind": "Cargo",
  "buyQuantityStep": 1,
  "sellQuantityStep": 1
}
```

Точные названия: displayName каталога английский, locale — соответствующий столбец. Для существующих ID имена ключей уже заданы в TradeItemPresentation.cs:4–18; новый — Trade.ItemElectronics.

| TypeId | English | Russian |
|---|---|---|
| item.ice | Cryo-Ice | Криолёд |
| item.iron-ore | Asteroid Iron Ore | Астероидная железная руда |
| item.silicon | Electronic Silicon | Электронный кремний |
| item.magnesium-ore | Magnesium Concentrate | Магниевый концентрат |
| item.carbon-ore | Carbon Regolith | Углеродный реголит |
| item.water | Purified Water | Очищенная вода |
| item.steel | Hull Alloy | Корпусной сплав |
| item.energy-cells | Energy Cells | Энергетические ячейки |
| item.fuel | Cryogenic Fuel | Криогенное топливо |
| item.protein-mass | Synthetic Protein Mass | Синтетическая белковая масса |
| item.food-rations | Sealed Rations | Герметичные пайки |
| item.electronics | Control Electronics | Управляющая электроника |

Новые ключи (не менять арность существующих format keys):

| Key | English | Russian |
|---|---|---|
| Trade.DescriptionElectronics | Control blocks for station systems and ship equipment. | Блоки управления станционными системами и корабельным оборудованием. |
| Trade.UnitKg | kg | кг |
| Trade.UnitRation | rations | рацион. |
| Trade.UnitCell | cells | ячеек |
| Trade.UnitBlock | blocks | блоков |
| Trade.UnitGeneric | units | ед. |
| Trade.QuantityWithUnit | Quantity, {0} | Количество, {0} |
| Trade.AmountWithUnit | {0} {1} | {0} {1} |
| Trade.QuantityMass | 1 {0} = {1} kg | 1 {0} = {1} кг |
| Trade.ConfirmBuyWithUnit | Buy {0} {1} for {2} | Купить {0} {1} за {2} |
| Trade.ConfirmSellWithUnit | Sell {0} {1} for {2} | Продать {0} {1} за {2} |

Для QuantityMass использовать отдельные singular ключи Trade.UnitRationSingle = ration/рацион; Trade.UnitCellSingle = cell/ячейка; Trade.UnitBlockSingle = block/блок; Trade.UnitGenericSingle = unit/ед. Trade.UnitKg подходит для обеих форм. В многострочных описаниях не заявлять новые механики.

## Implementation steps

1. Добавить electronics в конец itemTypes после food-rations, сохраняя порядок существующих элементов (ScenarioEngineTests.cs:248–250 использует индексы energy=2/rations=5), и переименовать только displayName указанных 11 существующих MVP items. Не менять старые ID, catalogCode (включая отсутствие кода у rations), цены, массы, units/storage/steps и версии.
2. Обновить две локализации по таблицам; проверять дубликаты JSON keys и полноту симметрии. Existing uranium keys/описания и icon paths не трогать.
3. Тестовый файл читает исходные JSON по repo-root от AppContext.BaseDirectory (пять parent уровней, как existing CatalogCompatibilityTests.cs:9–10), также проверяет copied Data в test output после build. Fixtures не записывают исходные файлы.
4. Проверить действительным EngineContentLoader.LoadRegistryFromSettingsFile, что все ссылки каталога/рецептов и стандартные сценарии валидны. Проверить electronics fields и неизменность economic tuples старых items.
5. Зафиксировать safety boundary совместимости: текущий catalog fingerprint изменился, settings legacy hash остался 0E0C8DCBDBB06051AC1D7DBA5321DA8DFD0A2CA29C7980AAA4F56486FA300555; current save roundtrip успешен, старый catalog stamp отклонён. Не обновлять hash ради прохождения теста.

## Out of scope

Engine/Contracts/Client C# production, Settings.json, профили/сценарии, картинки, новый TradeUnit enum, изменение старых экономических чисел, миграция saves. C# test file принадлежит matching Client test project и проверяет content.

## Invariants

Electronics параметры и шаг 1 — Documentation/01-Requirements/EngineRequirements.md:5128–5160. FuelTank — items-good.json:40–49. Uranium reference — Scenarios/Docked/scenario.json:165. Имена исключены из fingerprint — GameDataRegistry.cs:31–36. Recursive content copy — Client.csproj:264–271.

## Tests

StationMarketCatalogContentTests:

- Mvp_catalog_has_twelve_required_items_plus_legacy_uranium (AC-01): точный набор 13 ID, из них пять ресурсов MVP + семь товаров.
- Electronics_matches_approved_identity_mass_price_and_storage (AC-01).
- Existing_economic_tuples_and_legacy_references_are_unchanged (AC-01): baseline цены 10/5/40/30/30/30 для ice/iron/silicon/magnesium/uranium/carbon, Goods 14/40/50/10/110/20; все массы 1; units rations=Ration, energy=EnergyCell, остальные legacy=Kilogram.
- Both_locales_have_matching_scifi_names_and_unit_format_arguments (AC-06): exact names/keys, string.Format со всеми аргументами; singular/mass не смешаны.
- Catalog_addition_changes_identity_without_reapproving_legacy (AC-07): exact pinned hash, несовместимый old stamp отклонён, new save roundtrip.
- Shipped_catalog_and_locales_are_available_in_output (AC-01, AC-06).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~StationMarketCatalogContentTests
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~CatalogCompatibilityTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в Code context; files_touched не превышен.
- Каждый AC из serves покрыт указанными named tests в пределах вклада этого тикета.
- Named tests и проверки matching project проходят; конкретные исходные failures записаны отдельно, успех не заявляется при пропуске.
- Public API, invariants, out-of-scope и dependency contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы вне allowlist.
- Независимо проверяемый результат подтверждён тестовым выводом и diff; этот planning-документ сам по себе не является evidence реализации.

## Self-containment check

Даны все ID, имя/единица каждого базового товара, electronics object, ключи локализации и правило fingerprint. Пять файлов включают всю data/localization работу и один matching test. End state: shipped catalog и оба словаря готовы без добавления новых production API.


