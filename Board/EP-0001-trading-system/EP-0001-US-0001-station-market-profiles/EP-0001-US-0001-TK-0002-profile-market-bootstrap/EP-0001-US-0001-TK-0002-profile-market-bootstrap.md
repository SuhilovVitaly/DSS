---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
ticket: EP-0001-US-0001-TK-0002-profile-market-bootstrap
title: Профильный рынок и безопасное восстановление
stage: approved
layer: engine
depends_on: [EP-0001-US-0001-TK-0001-market-profile-schema]
files_touched: 5
serves: [AC-03, AC-04, AC-05, AC-07]
created: 2026-09-21T08:33:04Z
revision: 2
---

# Профильный рынок и безопасное восстановление

## Why

Назначенный профиль должен определять реальный локальный склад и бюджет, с которыми работают существующие команды. После сделки Save/Load обязан сохранить этот рынок, а ошибочный профиль не должен повредить уже загруженный мир.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Пути в таблице относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:33:04Z: «Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md».
Других явных технических решений пользователя нет.

## Assumptions

- При marketProfileId позиции профиля + explicit inventory образуют полный ассортимент. Explicit quantity/credits имеют приоритет, профиль масштабирует только свои defaults.
- Supply/demand не запрещают обратное направление торговли; бюджет и наличие ограничивают существующие операции.
- Без marketProfileId сохраняется нынешний fallback, включая добавление незаданных catalog items.
- Save v8 нужен только для нового profile stamp. Старые v0–7 без профилей продолжают проходить существующие проверки; несовместимый каталог не мигрирует.
- Проверки текущего каталога и legacy baseline в CatalogCompatibilityTests разделяются до добавления electronics в TK-0003.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :23 SaveFormat=7; :85 SpaceObjectData, конец optional полей :193 | Дописать optional profile ID/fingerprint; SaveFormat 8 и комментарий |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | :59–73 parsing; :76–103 normalization; :131–138 версия/identity | Структурная проверка profile metadata, station-only, saved v8 requirements |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :196–254 preflight/bootstrap; :549–586 snapshot; :756–820 save; :1180–1259 credits/inventory; :3108–3171 runtime record | Profile resolution, bounded bootstrap, runtime fields, save roundtrip; существующие trade handlers не переписывать |
| tests/DeepSpaceSaga.Engine.Tests/StationEconomyGenerationTests.cs | Существующий файл: :54–78 real registry и список 12 ID; :81–98 проверка generated inventory; command pattern — TradeCommandTests.cs:183–201 | Самодостаточные profile/scenario fixtures и named regressions |
| tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs | :25–40 ожидает 7 и равенство current/legacy; :58–72 legacy fixture зависит от real catalog | Version expectation и независимые legacy-baseline fixtures, regression electronics fingerprint |

## Public API after the change

Contracts API не меняется. Новые optional поля в конце публичного Engine SpaceObjectData:

```csharp
[property: JsonPropertyName("marketProfileId")] string? MarketProfileId = null,
[property: JsonPropertyName("marketProfileFingerprint")] string? MarketProfileFingerprint = null
```

SpaceObjectRuntime (в SimulationEngine.cs) получает те же optional string-поля. SaveFormat.CurrentSaveFormatVersion = 8.

Dependency contract TK-0001: registry.StationMarketProfiles — TypeRegistry<StationMarketProfileDefinition>; GetIndex/GetDefinition; definition предоставляет InitialInventory (ItemTypeId/Quantity), InitialCredits, RefuelStockKg, SizeFactors (StationSize → int, 1000=1), Fingerprint (economic SHA-256). Registry validates references до bootstrap. Менять эти dependency-файлы здесь нельзя.

Для новых scenario.json допустим только marketProfileId без fingerprint. Для profile save v8 обязательны оба поля, explicit credits, stationSize и полный materialized inventory. Неизвестный профиль/неподходящий stamp — ScenarioException, контекст включает station ObjectId, profile ID и field.

## Implementation steps

1. Добавить поля в конец record, чтобы не сломать positional callers; поднять save version до 8. В ScenarioLoader запретить blank IDs/stamps, fingerprint без ID и любую profile metadata у не-Station. Saves до v8 с profile metadata отклонять: такого supported формата раньше не было.
2. В LoadScenario до мутации мира разрешить каждый профиль через registry. Неизвестный ID отклонять даже при полностью explicit inventory. New-game scenario с supplied fingerprint либо совпадает с текущим, либо отклоняется. isSave=true также требует stamp и полный resolved inventory независимо от saveFormatVersion.
3. ResolveStationSize выполнить до профильного bootstrap. Для профиля вычислять defaults по его SizeFactors: checked decimal(value) * factor / 1000, decimal.Round(..., 0, AwayFromZero), checked long. Никаких float/double и ценовых StationSizeFactors для stocks/budget.
4. Credits = explicit Credits, иначе scaled InitialCredits. Inventory для профиля = scaled InitialInventory + отдельная item.fuel строка scaled RefuelStockKg, затем per-item explicit overrides. Overrides не масштабируются; zero сохраняется, дополнительные зарегистрированные priced items допускаются. Проверять отрицательные/дублирующие explicit entries и отсутствие цены. Перечислять итог в стабильном ordinal ItemTypeId порядке.
5. Не запускать старый цикл добавления всех товаров для профильного рынка. Без профиля оставить прежнюю ветку credits/inventory без изменений. Пустой explicit массив означает отсутствие overrides в new-game, а не удаление профильного ассортимента.
6. Сохранить profile ID и fingerprint на runtime-станции. CaptureSaveState пишет их с explicit credits/size/всеми inventory rows, включая нулевые. Не подменять profile metadata при with-обновлениях торговых операций.
7. При load profile save v8 проверить fingerprint и наличие всех ожидаемых profile/fuel entries; запрещено молча достраивать отсутствующие строки defaults. Extras допустимы и сохраняются. Совпадающий save использует explicit values как есть, без повторного scaling/seed generation.
8. В CatalogCompatibilityTests заменить только hardcoded save version на 8 (или CurrentSaveFormatVersion с отдельным Assert=8). Реальный new save должен совпадать с текущим registry fingerprint, но не обязан совпадать с LegacyCatalogFingerprint. Перестроить Legacy_save_requires_exact_approved_baseline на локальный synthetic baseline registry: вычислить fingerprint, затем создать такой же registry с явно одобренным baseline. Добавить electronics к копии registry и убедиться, что старый baseline больше не даёт загружать anonymous legacy save. Не менять Settings legacy hash и не ослаблять production checks.
9. Покрыть below tests и весь Engine test project. Не добавлять MarketProfileId в network snapshot: имя роли уже есть в имени demo-станции, рынок виден по существующим Items.

10. В StationEconomyGenerationTests заменить статический список TradeableItemTypeIds (:74–79) на выборку всех priced items из того же loaded registry, по которому создан Engine. Generated_station_gets_credits_coefficient_and_inventory_within_documented_ranges продолжает проверять точное равенство наборов/длин и stock 20..500 каждой позиции. Не удалять проверку, не расширять диапазон и не исключать electronics. Это сохраняет legacy fallback invariant при расширении каталога в TK-0003. Новые профильные tests и helper fixtures размещаются в этом же разрешённом файле.

## Out of scope

Production JSON/Settings, UI, Contracts, цены, новые trade permissions, maxStock enforcement, replenishment, общий registry fingerprint профилей и миграции старой экономики. Не менять старые Default/Docked fixtures. Никаких изменений времени, скорости или модальных правил.

## Invariants

- Explicit stocks Default/Docked сохраняются: Documentation/01-Requirements/EngineRequirements.md:5315–5317.
- Ошибка не заменяет состояние: SimulationEngine.cs:230–232.
- Existing catalog stamp gate сохраняется: SimulationEngine.cs:196–204.
- Формула цены и consumed-resource правило не меняются: EngineRequirements.md:5227, 5253–5265.
- Snapshot не раскрывает Credits станции: src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:15–21.
- Save текущих stock/credits уже поддержан: SimulationEngine.cs:774–779; новые поля дополняют его.

## Tests

StationEconomyGenerationTests расширяется и создаёт собственный registry из TK-0001 API, пять разных profile fixtures, корабль с рабочими cargo/fuel модулями, достаточными Credits и свободной вместимостью. JSON/records размещаются внутри этого файла; новые fixture files запрещены.

- Profile_bootstrap_uses_only_profile_inventory_and_budget (AC-04): точный набор, размерный масштаб, никакого случайного uranium или посторонних items.
- Explicit_zero_extra_item_and_credits_override_profile (AC-04): 0, 777, extra priced item, Credits=0; overrides не масштабируются.
- Legacy_default_and_docked_keep_required_explicit_stocks (AC-04): реальные сценарии, Large и семь обязательных quantities из story invariants, профиль отсутствует.
- Invalid_profile_reference_preserves_running_world (AC-03): unknown ID, non-station metadata, malformed inventory, stamp mismatch; сравнить snapshot/save до и после исключения.
- Each_profile_supports_buy_sell_and_refuel (AC-05), theory по пяти fixture-профилям: ReceiveCommand(new PlayerCommand(...)), затем CaptureSnapshotForTests; Buy=1, Sell=1, Refuel=1 с уникальными CommandId. Assert Executed, executed quantity, точные дельты stock/cargo/tank/credits, не только наличие строки.
- Non_profile_item_cannot_be_traded (AC-05): priced item существует в каталоге, но отсутствует в station.Inventory; команда отклонена, состояние неизменно.
- Profile_save_roundtrip_preserves_post_trade_state (AC-07): capture/serialize/load v8, все ID/stamps/stock/credits равны; scale не применяется повторно.
- Changed_or_missing_profile_stamp_or_inventory_is_rejected (AC-03, AC-07).
- Legacy_unprofiled_save_remains_compatible_with_matching_catalog (AC-07).

CatalogCompatibilityTests:
- сохранить Real_scenarios_and_new_saves_use_compatible_catalog и Rename_is_compatible_but_price_and_mass_changes_are_not;
- переработать Legacy_save_requires_exact_approved_baseline;
- добавить Added_electronics_rejects_old_catalog_identity_without_rewriting_legacy_baseline (AC-07).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore
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

Указаны dependency API, precedence для new-game/save/legacy, схема v8, scaling, порядок inventory и правила ошибок. Пять файлов включают два тестовых файла и исправление известного test coupling к legacy catalog. End state: профиль назначается станции, существующие команды работают с его складом, после Save/Load результат сделки сохраняется.


