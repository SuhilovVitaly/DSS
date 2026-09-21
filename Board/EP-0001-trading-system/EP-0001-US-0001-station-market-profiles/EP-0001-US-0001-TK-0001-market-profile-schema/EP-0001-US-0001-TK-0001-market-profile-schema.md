---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
ticket: EP-0001-US-0001-TK-0001-market-profile-schema
title: Загрузка и валидация профилей рынка
stage: approved
layer: engine
depends_on: []
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-09-21T08:33:04Z
revision: 2
---

# Загрузка и валидация профилей рынка

## Why

Автор контента получает immutable реестр профилей и диагностируемый отказ до запуска. AC-02 здесь покрывает схему и возможность загрузить пять ролей; поставляемые значения добавляет TK-0004. AC-03 покрывает контентную валидацию; назначение станции добавляет TK-0002.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Все пути Code context относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:33:04Z: «Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md».
Это запрос planning; отдельных технических решений пользователя в этой задаче нет.

## Assumptions

- Профиль описывает bootstrap и специализацию. Supply/demand не исполняет flows, не меняет pricing и не задаёт отдельные разрешения направлений.
- Optional settings key обеспечивает совместимость старых fixtures. Если key задан, файл обязателен; пустой путь/отсутствующий файл — ошибка.
- Профильные количества задаются для Medium; размерные множители — обязательный конфиг, отдельный от существующих price factors.
- Нет зависимости на shipping electronics: тест использует собственный валидный каталог, включая item.electronics.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs | Новый файл; реестровый контракт ITypeDefinition используется в src/DeepSpaceSaga.Engine/Content/TypeRegistry.cs:6–7 | Только immutable profile/stock records и profile fingerprint |
| src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs | :73–108 загружает registry; :490–517 strict JSON/resolve; :540–549 TypeDataPaths | Optional path, DTO, загрузка и contextual validation профилей |
| src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs | :9–57 ctor/properties/Empty; :59–92 Create и reference validation | Новый реестр и повторная семантическая валидация для прямого Create |
| tests/DeepSpaceSaga.Engine.Tests/StationMarketProfileContentTests.cs | Новый файл; доступны internals по src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj:15 | Изолированные fixtures в temp и все named tests ниже |
| tests/DeepSpaceSaga.Engine.Tests/ItemCatalogTests.cs | :193–239 fixed count=12 и старые displayName реального каталога | Отделить Engine invariants от изменяемого shipping presentation до TK-0003 |

## Public API after the change

Изменений Contracts/IGameSessionConnection нет. Engine-internal API:

- В конец TypeDataPaths добавить optional string? StationMarketProfiles = null с JSON stationMarketProfiles.
- GameDataRegistry.StationMarketProfiles: TypeRegistry<StationMarketProfileDefinition>.
- В конец GameDataRegistry.Create добавить optional IEnumerable<StationMarketProfileDefinition>? stationMarketProfiles = null; старые calls и Empty сохраняются.
- internal sealed record StationMarketStockDefinition(string ItemTypeId, long Quantity).
- internal sealed record StationMarketProfileDefinition(string TypeId, string DisplayName, ImmutableArray<string> SupplyItemTypeIds, ImmutableArray<string> DemandItemTypeIds, ImmutableArray<StationMarketStockDefinition> InitialInventory, long InitialCredits, long RefuelStockKg, ImmutableDictionary<StationSize, int> SizeFactors) : ITypeDefinition.
- Read-only string Fingerprint на profile: SHA-256 uppercase hex от canonical JSON экономических полей. Сортировать supply/demand по ordinal ID, inventory по ItemTypeId, size factors по имени размера. Включить TypeId, все списки, quantities, InitialCredits, RefuelStockKg, SizeFactors; DisplayName исключить. Не менять CatalogCompatibility.
- internal static IReadOnlyList<StationMarketProfileDefinition> LoadStationMarketProfiles(string path) в EngineContentLoader; ссылки на товары проверяет registry после загрузки items.

JSON schema v1:

```json
{
  "schemaVersion": 1,
  "sizeFactors": { "Outpost": 500, "Medium": 1000, "Large": 1500, "Huge": 2000 },
  "profiles": [{
    "typeId": "market.mining",
    "displayName": "Mining",
    "supplyItemTypeIds": ["item.ice"],
    "demandItemTypeIds": ["item.water"],
    "initialInventory": [
      { "itemTypeId": "item.ice", "quantity": 162 },
      { "itemTypeId": "item.water", "quantity": 36 }
    ],
    "initialCredits": 9600,
    "refuelStockKg": 200
  }]
}
```

Пример сокращён только по числу товарных позиций; это fixture схемы, не финальный Mining. Все поля обязательны, включая пустой supply Transit; null не равен пустому массиву. Числа целые. DTO nullable numeric или required-presence check не должны превращать отсутствующее обязательное поле в 0.

## Implementation steps

1. Добавить records и canonical fingerprint в новый файл; коллекции копируются в immutable представление.
2. Расширить loader optional path. Объявленный файл читать тем же strict JSON режимом, schemaVersion только 1; непустой profiles. Ошибки содержат путь, profile ID, field и проблемный item ID. Неизвестные поля и enum size keys отклонять.
3. SizeFactors содержит ровно четыре StationSize ключа, положительные int, Medium = 1000. InitialCredits и refuelStockKg неотрицательны; quantities неотрицательны; проверить overflow масштабирования checked decimal → long на всех четырёх размерах с округлением AwayFromZero.
4. Registry создаёт ordinal реестр; запрещены пустые/дублирующие profile IDs и имена. Внутри каждого списка нет пустых/дублирующих item IDs, supply и demand не пересекаются; initialInventory содержит ровно их объединение без дубликатов. Demand непустой, supply может быть пустым.
5. Все товарные ссылки должны существовать, иметь положительную BasePriceCredits и StorageKind Cargo. Fuel не допускается в этих списках: refuelStockKg использует отдельно item.fuel, который обязан существовать, иметь Good/Kilogram/FuelTank и цену. Количество 0 допустимо.
6. Семантическая проверка действует и на прямой GameDataRegistry.Create. Loader дополняет ContentException путём исходного profile-файла. Не вшивать пять shipping ID или баланс в Engine.
7. Добавить tests; временные settings/catalog/profile fixtures полностью создаются внутри одного тестового файла и удаляются fixture Dispose.

8. Подготовить ItemCatalogTests к разрешённому расширению каталога: заменить Real_catalog_has_twelve_tradeable_items на Real_catalog_preserves_legacy_tradeable_identities — проверить наличие всех 12 прежних ID (ice, iron-ore, silicon, magnesium-ore, uranium-ore, carbon-ore, water, steel, energy-cells, fuel, protein-mass, food-rations с item. префиксом), уникальность ID и положительные цены всех записей. Не фиксировать верхний предел расширяемого registry. В двух Real_catalog_item_matches_documented_price_category_and_catalog_code / Real_catalog_food_rations_has_no_catalog_code_but_documented_price убрать сравнение изменяемого имени с исторической строкой, проверять непустое DisplayName; сохранить все ожидания Category/CatalogCode/BasePrice/UnitMass. Удалить устаревший комментарий, приравнивающий все quantities к kg. Точное shipping count=13, новые имена и неизменность экономических tuple проверит TK-0003 в matching Client tests. Это не разрешение игнорировать пропавшие legacy ID или менять экономику.

## Out of scope

ScenarioData, SimulationEngine, production content, цены, snapshots, UI, SaveFormat и production flows. Не менять TypeRegistry или другие файлы.

## Invariants

Strict validation до сессии — EngineRequirements.md:224–261; optional content loader pattern — EngineContentLoader.cs:96–108; ordinal registry — TypeRegistry.cs:26–45; деньги без float/double — EngineRequirements.md:5263–5265. Ссылки EngineRequirements разрешаются в Documentation/01-Requirements.

## Tests

Дополнительно AC-01: ItemCatalogTests.Real_catalog_preserves_legacy_tradeable_identities и существующие проверки цен/категорий/кодов. Этот вклад готовит Engine-проверки каталога, не добавляет shipping electronics.

В новом классе StationMarketProfileContentTests:

- Optional_profile_path_preserves_legacy_registry (AC-03): key отсутствует → пустой реестр.
- Five_profiles_load_as_immutable_definitions (AC-02): five fixture records, Transit с пустым supply, повторяемые fingerprints.
- Unknown_item_reports_file_profile_field_and_item (AC-03): неизвестные supply, demand, inventory и отсутствующий fuel; проверить конкретный контекст сообщения.
- Invalid_profile_schema_is_rejected (AC-03), theory: missing/null fields, неизвестные поля/version/size, duplicates, overlap, negative/overflow amounts, неполный union и Fuel в cargo.
- Direct_registry_creation_validates_profile_references (AC-03).
- Profile_fingerprint_is_order_independent_but_detects_economic_changes (AC-02): перестановки/rename совместимы; изменение stock/budget/fuel/size или item ID меняет fingerprint.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~StationMarketProfileContentTests|FullyQualifiedName~ItemCatalogTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
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

Даны пять разрешённых файлов, полная JSON schema, internal API и validation/error contract, rounding и fingerprint canonicalization. Тесты сами создают fixtures; shipping data и bootstrap станции не требуются для проверки этого merge unit. Независимый end state: корректный профиль загружается, некорректный content не допускает создание сессии.



