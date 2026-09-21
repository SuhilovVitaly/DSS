---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
ticket: EP-0001-US-0002-TK-0002-market-economy-schema
title: Валидируемая конфигурация потоков и сохранения
stage: approved
layer: engine
depends_on: [EP-0001-US-0001-TK-0005-profile-trade-presentation]
files_touched: 6
serves: [AC-01, AC-03, AC-05, AC-07]
created: 2026-09-21T08:59:06Z
revision: 2
---

# Валидируемая конфигурация потоков и сохранения

## Why

Почасовая экономика получает строгий data contract: rates, stock targets, пороги и budget policy. Save shape заранее предоставляет поля для невместившейся продукции и доступного торгового бюджета. Runtime consumption/bootstrap этих полей реализует TK-0003.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Пути ниже относительны D:/DeepSpaceSaga/DSS. prerequisite US-0001 должна быть реализована первой.

## Decisions

D-01, 2026-09-21T08:59:06Z: «сделай следующую стори». Технические assumptions ниже не выдаются за отдельные ответы пользователя.

D-02, revision 2: при реализации выяснилось, что предписанный шагом 8 бамп
SaveFormat.CurrentSaveFormatVersion 8→9 ломает второй жёстко закодированный литерал версии —
`Assert.Equal(8, save.SaveFormatVersion)` в tests/DeepSpaceSaga.Engine.Tests/StationEconomyGenerationTests.cs:543,
файл, которого не было в исходном allowlist. Пользователь явным ответом выбрал расширить allowlist
тикета вместо отката правки и заведения follow-up. files_touched поднят 5→6, файл добавлен в Code
context с узким разрешением «только литерал версии». Остальное содержимое файла не меняется;
предсуществующие в нём нарушения dotnet format не исправляются в рамках этого тикета.

## Assumptions

- Economy — optional field profile, schemaVersion профилей остаётся 1. Отсутствующий/null economy = статическая US-0001; неизвестные вложенные поля отклоняются.
- Один production source на станцию: Profile либо Modules. Нет автоматического выбора по наличию factory.
- Budget cap относится к MarketBudgetCredits; Credits может хранить нераспределённую выручку сверх лимита. Formula и операции — TK-0003.
- SaveFormat становится 9 после предусмотренной US-0001 версии 8. CatalogVersion/RulesVersion и legacy catalog fingerprint не меняются: profile fingerprint защищает новую экономику.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs | При начале grounding отсутствовал; в параллельной работе появился: :8–18 records соответствуют US-0001/TK-0001 API | Optional Economy, records/enum и canonical fingerprint payload |
| src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs | Текущий :73–108 registry load, :490–503 strict JSON; US-0001 добавляет profile DTO/loader | Только расширение вложенного profile DTO, presence/type validation |
| src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs | :59–92 validation registry; US-0001 добавляет StationMarketProfiles и ссылки | Семантическая validation economy для loader и direct Create |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | Текущий :23 SaveFormat7, :193 конец object record, :231–240 producing module; US-0001 планирует Save8/profile fields | Save9, MarketBudgetCredits, PendingOutput optional fields |
| tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs | :25–40 new save/version/fingerprint; US-0001/TK-0002 адаптирует baseline | Здесь же добавить экономические schema/fingerprint tests и обновить version expectation на 9 |
| tests/DeepSpaceSaga.Engine.Tests/StationEconomyGenerationTests.cs | :543 `Assert.Equal(8, save.SaveFormatVersion)` — второй жёстко закодированный литерал версии, не обнаруженный при planning | Только обновление этого литерала на 9; никакой другой правки файла (D-02, revision 2) |

## Public API after the change

Contracts не меняются. Добавить в конец внутреннего StationMarketProfileDefinition:

```csharp
StationMarketEconomyDefinition? Economy = null
```

В том же файле объявить:

```csharp
internal enum StationMarketProductionSource { Profile, Modules }
internal sealed record StationMarketTargetDefinition(string ItemTypeId, long TargetStock);
internal sealed record StationMarketEconomyDefinition(
    StationMarketProductionSource ProductionSource,
    ImmutableArray<StationMarketStockDefinition> HourlyInputs,
    ImmutableArray<StationMarketStockDefinition> HourlyOutputs,
    ImmutableArray<StationMarketStockDefinition> HourlyConsumption,
    ImmutableArray<StationMarketTargetDefinition> StockTargets,
    int ShortageThresholdPermille,
    int SurplusThresholdPermille,
    int BudgetRegenerationDivisorPerDay);
```

Quantity в Hourly* — целое число trade units за один игровой час; StationMarketStockDefinition(string ItemTypeId,long Quantity) уже предусмотрен US-0001.

В конец SpaceObjectData добавить [property: JsonPropertyName("marketBudgetCredits")] long? MarketBudgetCredits = null.
В конец StationProducingModuleData добавить [property: JsonPropertyName("pendingOutput")] IReadOnlyList<StationInventoryItemData>? PendingOutput = null.
SaveFormat.CurrentSaveFormatVersion=9. PendingOutput содержит ItemTypeId/Quantity уже произведённого, не recipe definition. Null/[] означает отсутствие остатка.

Новый optional JSON fragment profile:

```json
"economy": {
  "productionSource": "Profile",
  "hourlyInputs": [{ "itemTypeId": "item.water", "quantity": 4 }],
  "hourlyOutputs": [{ "itemTypeId": "item.ice", "quantity": 18 }],
  "hourlyConsumption": [],
  "stockTargets": [
    { "itemTypeId": "item.ice", "targetStock": 108 },
    { "itemTypeId": "item.water", "targetStock": 72 }
  ],
  "shortageThresholdPermille": 500,
  "surplusThresholdPermille": 1500,
  "budgetRegenerationDivisorPerDay": 24
}
```

Пример — сокращённый fixture-профиль, не полная shipping Mining. Если economy задан, все его поля обязательны. DTO обязаны отличать missing/null numbers/arrays от разрешённых пустых массивов.

## Implementation steps

1. Добавить schema types/optional fields. Canonical fingerprint US-0001 расширить economy только при Economy != null; сортировать все item lists ordinal, включить source, rates, targets, thresholds, divisor. При null сохранить прежние байты hashing payload, а не дописывать null-property.
2. EngineContentLoader принимает перечисленный JSON shape строго, source только Profile/Modules (case-sensitive), неизвестные поля/неправильная version — ContentException с path/profile/field/item.
3. Registry повторно проверяет direct Create: все targets имеют уникальные зарегистрированные Cargo IDs, BasePrice>0, TargetStock>0. Targets ровно покрывают InitialInventory (без отдельного Fuel); rates IDs принадлежат targets, положительны, без дублей внутри массива.
4. Profile: HourlyOutputs IDs ровно SupplyItemTypeIds, HourlyInputs и HourlyConsumption не пересекаются и их union ровно DemandItemTypeIds. Если outputs пуст (Transit), inputs должен быть пуст, весь спрос идёт в HourlyConsumption. Если outputs непуст, inputs непуст; один batch означает все inputs/outputs вместе.
5. Modules: HourlyInputs/HourlyOutputs пусты, HourlyConsumption является subset DemandItemTypeIds; конкретные recipe references и отсутствие double consumption проверит TK-0003 на станции.
6. Проверять 0<ShortageThresholdPermille<1000<SurplusThresholdPermille<2000, divisor>=24, InitialCredits>0 для economy-профиля. Не вводить bool enabled и не превращать absent economy в auto defaults.
7. Для каждого sizeFactor вычислить target=RoundAwayFromZero(baseTarget×factor/1000), target>0, maxStock=checked(2×target); scaled InitialInventory<=maxStock, scaled InitialCredits>0 и maxBudget=checked(2×scaled InitialCredits). Rates не масштабируются; каждый часовой input/output/consumption должен помещаться в max соответствующей позиции для всех четырёх размеров, чтобы объявленный профиль не имел неработающего size. Все overflow — contextual ContentException.
8. Save DTO tests проверяют budget и pending JSON roundtrip, не вызывая ещё не готовую runtime интерпретацию. В CatalogCompatibilityTests существующую проверку текущей версии перенести с 8 на 9; старые explicit legacy version fixtures не поднимать автоматически.
9. Не изменять EconomyTimeData.CurrentRulesVersion, Settings.catalogVersion или LegacyCatalogFingerprint. Их guards остаются активны. Проверить new metadata не меняет CatalogCompatibility, но меняет profile fingerprint.

## Out of scope

SimulationEngine runtime, календарные hooks, ScenarioLoader semantic checks, UI/Contracts, JSON поставки, изменение ставок сборов, миграции. Contextual проверки реально загружаемой станции и v9 payload выполняются в preflight TK-0003; этот тикет даёт сериализуемую форму и content validation.

## Invariants

Strict validation — Documentation/01-Requirements/EngineRequirements.md:224–261. PendingOutput — :3818,3861–3868. Integer/fixed-point money — :5263–5265. Existing catalog identity — src/DeepSpaceSaga.Engine/SimulationEngine.cs:196–204. Новая production config не превращается в копии recipe type data в save.

## Tests

В CatalogCompatibilityTests добавить (отдельные helpers внутри того же файла):

- Profile_economy_schema_accepts_five_role_shapes_and_modules_source (AC-01).
- Profile_economy_rejects_unknown_items_missing_fields_and_invalid_bounds (AC-01, AC-03): rates negative/zero/duplicate/Fuel, missing targets, over-cap initial stock, size rounding/overflow, source enum, invalid threshold/divisor, double inputs/consumption.
- Profile_without_economy_keeps_us0001_fingerprint (AC-01, AC-07): сохранить контрольный expected payload/hash из dependency до добавления optional поля, не вычислять expected тем же новым методом.
- Economy_fingerprint_covers_rates_targets_source_and_budget_policy (AC-01, AC-05, AC-07): reorder/rename стабильны, изменение любого economic поля меняет profile hash; catalog hash неизменен.
- Save_v9_roundtrips_market_budget_and_pending_output (AC-03, AC-07): JSON Serialize/LoadFromJson, количество/IDs не теряются.
- Legacy_save_shape_has_no_implicit_market_budget_or_pending_output (AC-07).
- Existing Real_scenarios_and_new_saves_use_compatible_catalog сохраняет смысл с version9.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CatalogCompatibilityTests|FullyQualifiedName~StationMarketProfileContentTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
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

Приведены полный shape нового economy, signatures после US-0001, exhaustive validation, hash compatibility и save fields. Все пять разрешённых файлов включая тест указаны. End state: корректная конфигурация загружается, повреждённая отклоняется, DTO новых runtime-полей сохраняет значения.
