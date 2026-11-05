---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
ticket: EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis
title: Сохраняемый cost basis груза
stage: approved
layer: engine
depends_on: []
files_touched: 5
serves: [AC-03, AC-04, AC-06]
created: 2026-09-21T12:39:58Z
revision: 1
---

# Сохраняемый cost basis груза

## Why

Cargo stack хранит только quantity, поэтому после Save/Load невозможно отличить покупку от бесплатного или неизвестного груза. End state: runtime/save stack хранит total cost basis и deterministic source set; new scenario получает bootstrap basis, legacy unknown не превращается в нулевую прибыль, save format 9 валидирует состояние.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Пути от `D:/DeepSpaceSaga/DSS`.

## Decisions

2026-09-21T12:39:58Z — зафиксирован исходный запрос пользователя из story. Решений пользователя о legacy migration не было; консервативное поведение записано как assumption.

## Assumptions

Cost basis — total Credits на stack. Source set сортируется ordinal и не содержит duplicates. Version0 означает new-game scenario; versions1–8 — legacy saves. Unknown history сохраняется как null + единственный source `legacy-unknown` и никогда не получает implicit zero/market revaluation.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :7–24 current save version8; :291–302 module cargo; :416–419 cargo item/quantity | Поднять version до9; добавить optional basis/source JSON fields с документацией |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | :129–141 version checks; :184–210 object/profile validation | Validate v9 cargo metadata и допустимые source values; legacy/version0 остаются различимы |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :836–878 save projection; :1063–1081 load; :3311–3326 runtime stack | Runtime fields, bootstrap/legacy resolution и exact save projection |
| tests/DeepSpaceSaga.Engine.Tests/StationEconomyGenerationTests.cs | :543 ожидает save version8; содержит scenario/save fixtures | Обновить version expectation и добавить bootstrap/source/save roundtrip cases в существующий fixture surface |
| tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs | :36–37 фиксирует version8/current save | Version9 compatibility, legacy unknown и invalid v9 metadata regressions |

## Public API after the change

Engine scenario schema:

```csharp
public sealed record CargoStackData(
    string ItemTypeId,
    long Quantity,
    long? CostBasisCredits = null,
    IReadOnlyList<string>? AcquisitionSources = null);
```

Internal runtime:

```csharp
internal sealed record CargoStackRuntime(
    int ItemTypeIndex,
    long Quantity,
    long? CostBasisCredits = null,
    ImmutableArray<string> AcquisitionSources = default);
```

Allowed sources: `bootstrap`, `purchased`, `produced`, `mined`, `dialogue-grant`, `legacy-unknown`. Sources сериализуются distinct в ordinal order. `SaveFormat.CurrentSaveFormatVersion = 9`.

## Implementation steps

1. Расширить DTO/runtime optional полями, не меняя первые два positional parameters; version поднять 8→9.
2. Version0 scenario: если quantity>0 и metadata отсутствует, получить `BasePriceCredits`; требуется значение >0, checked умножить на quantity, source=`bootstrap`. Explicit metadata принимает known nonnegative total + непустые allowed sources; `legacy-unknown` в new scenario запрещён.
3. Legacy save version1–8: existing metadata валидировать; отсутствующее metadata превратить в runtime null + [`legacy-unknown`] без пересчёта по catalog. Это состояние можно сохранить в v9 как единственное исключение nullable basis.
4. Version9: quantity>=0; known stack требует nonnegative basis и непустые allowed sources без `legacy-unknown`; unknown требует null basis и ровно [`legacy-unknown`]. Quantity0 сохраняет basis0 или удаляется существующей нормализацией, но не может иметь положительный basis.
5. Save projection записывает runtime basis/source exactly. Invalid blank/duplicate/unknown source, negative cost, mixed legacy source, missing v9 fields и arithmetic overflow дают `ScenarioException` до замены мира.
6. Обновить два существующих version assertions и добавить roundtrip/validation tests без правки demo JSON. Catalog fingerprint semantics остаются прежними.

## Out of scope

Изменение basis при покупке/продаже/consumption, receipt fields, UI, реальная production/mining команда, переоценка legacy cargo, US-0012 economy-wide migration.

## Invariants

- Save capture уже проходит через `BuildSaveCargo`: `SimulationEngine.cs:836–878`.
- Load resolves catalog item index once: `SimulationEngine.cs:1063–1081`.
- New saves use one centralized version: `ScenarioData.cs:7–24`; loader rejects future versions: `ScenarioLoader.cs:129–141`.
- Explicit cargo не должен становиться free profit: epic `../Documentation.md:102`.

## Tests

В двух разрешённых test files:

- `New_scenario_explicit_cargo_gets_checked_bootstrap_basis_and_source` — AC-03.
- `Explicit_produced_and_mined_basis_roundtrips_without_source_loss` — AC-03.
- `Save_format_9_roundtrips_known_and_legacy_unknown_cargo_cost_metadata` — AC-04/06.
- `Legacy_save_without_basis_stays_unknown_instead_of_zero_or_current_price` — AC-04.
- `Version_9_rejects_missing_negative_overflow_duplicate_or_unknown_cost_metadata` — AC-03/04.
- `Current_save_format_is_9_and_catalog_compatibility_behavior_is_unchanged` — AC-04.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~StationEconomyGenerationTests|FullyQualifiedName~CatalogCompatibilityTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены в пяти разрешённых файлах; version9 и compatibility tests согласованы.
- AC-03/04/06 покрыты named tests; invalid input не создаёт частично загруженный мир.
- Tests/build/format проходят либо baseline failure записан отдельно.
- No hidden repricing: version0 bootstrap и legacy unknown различаются тестом и serialized state.
- Public schema/defaults, catalog compatibility и out-of-scope соблюдены.

## Self-containment check

Schema, sources, version-specific resolution, validation и exact test targets определены. Implementer не должен выбирать migration valuation или искать дополнительные files.

