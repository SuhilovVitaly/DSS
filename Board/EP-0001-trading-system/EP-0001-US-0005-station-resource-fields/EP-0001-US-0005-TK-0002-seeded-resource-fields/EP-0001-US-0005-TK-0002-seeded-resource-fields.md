---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
ticket: EP-0001-US-0005-TK-0002-seeded-resource-fields
title: Конфигурация, генерация и сохранение полей
stage: approved
layer: engine
depends_on: [EP-0001-US-0005-TK-0001-resource-survey-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-05, AC-07]
created: 2026-09-21T09:56:58Z
revision: 1
---

# Конфигурация, генерация и сохранение полей

## Why

Создать воспроизводимое пространственное окружение после размещения станций и продолжать его из save без повторной генерации. End state: seeded New Game получает весь configured набор permanent asteroids, а invalid content/placement не заменяет текущий мир.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Paths относительно D:/DeepSpaceSaga/DSS.
Внешняя prerequisite: EP-0001-US-0004-seeded-trading-map, materialized карта. У неё ещё не создан bootstrap ticket; не записывать несуществующий ID и не реализовывать карту здесь.

## Decisions

2026-09-21T09:56:58Z — запрос: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0005-station-resource-fields\EP-0001-US-0005-station-resource-fields.md». Иных технических решений пользователя нет.

## Assumptions

Поля включаются только New Game + materialized TradingMap + configured thematic JSON. Existing field state всегда восстанавливается, не генерируется. Без state у save не происходит retrofit. Permanent/Stationary/IsKnown=true, actual composition скрыт. NextObjectNumber scoped к данной генерации, respawn/global allocator вне scope. Balancing rules и будущие scan jobs сохраняются с manifest, не отдельными файлами.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs | :15–50 три bootstrap entry points, :604–633 settings/type paths | Optional thematic path, strict load и ConfigureStationResourceFields перед LoadScenario |
| src/DeepSpaceSaga.Engine/Scenario/StationResourceFields.cs | Новый файл; seed primitives в Engine/Rng, схема SpaceObjectData существующая | Все DTO/validator/pure generator/stateful RNG helper ниже |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :51–73 GameStateData, SpaceObjectData уже содержит mass/composition | Один optional GameStateData.StationResourceFields в конце record; не менять base composition/version |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :196–240 seed/preflight; :448–479 snapshot; :777–815 save; :1166–1182 image | Configure helper, after-map generation, state storage/restore, neutral image/unknown survey projection |
| tests/DeepSpaceSaga.Engine.Tests/StationResourceFieldTests.cs | Новый файл; LoadScenario/CaptureSaveStateForTests/CaptureSnapshotForTests уже доступны | Самодостаточные config/map/profile fixtures, placement/determinism/save/invalid-load tests |

## Public API after the change

Settings: optional `typeData.stationResourceFields` path. Отсутствие означает disabled; объявленные null/blank/missing file — ContentException. GameStateData: `[JsonPropertyName("stationResourceFields")] StationResourceFieldsState? StationResourceFields = null`.

В новом файле namespace DeepSpaceSaga.Engine.Scenario, public sealed records с JsonPropertyName camelCase на КАЖДОМ поле и IReadOnlyList коллекциями:
```csharp
StationResourceFieldConfig(int SchemaVersion, int FirstObjectNumber,
 int MaxPlacementAttempts, double InnerRadiusKm, double OuterRadiusKm,
 double StationClearanceKm, double AsteroidSpacingKm, double CorridorHalfWidthKm,
 IReadOnlyList<ResourceFieldRoleData> Roles, IReadOnlyList<ResourceFieldKindData> FieldKinds,
 IReadOnlyList<ResourceMassBandData> MassBands, ResourceSurveyRulesData StructuralScan);
ResourceFieldRoleData(string MarketProfileId, IReadOnlyList<ResourceFieldCountData> Fields);
ResourceFieldCountData(string FieldKindId, int AsteroidCount);
ResourceFieldKindData(string FieldKindId, IReadOnlyList<ResourceVariantData> Variants);
ResourceVariantData(string VariantId, string CompositionType, int Weight,
 IReadOnlyList<ResourceFractionData> Resources);
ResourceFractionData(string ItemTypeId, int Permille);
ResourceMassBandData(string BandId, long MinKg, long MaxKg, int Weight);
ResourceSurveyRulesData(double RangeKm, long DurationGameTimeMs, int SuccessChancePercent);
ResourceFieldRngData(string Name, ulong Seed, ulong Counter);
ResourceFieldAsteroidData(string ObjectId, string StationObjectId, string FieldKindId,
 string VariantId, IReadOnlyList<ResourceFractionData> Resources, bool CompositionKnown = false);
ResourceSurveyJobData(string CommandId, string ObjectId, string ModuleId, string TargetObjectId,
 long StartedGameTimeMs, long DueGameTimeMs, long LastValidatedSimulationTimeMs);
StationResourceFieldsState(int SchemaVersion, StationResourceFieldConfig Rules,
 int NextObjectNumber, IReadOnlyList<ResourceFieldAsteroidData> Asteroids,
 IReadOnlyList<ResourceFieldRngData> RngStreams, IReadOnlyList<ResourceSurveyJobData> Surveys);
```
Config/state schemaVersion=1. New internal Engine entry `void ConfigureStationResourceFields(StationResourceFieldConfig? config)` stores validated immutable copy, invoked before initial LoadScenario.
New helper `StationResourceFields.Generate(GameStateData materialized, ulong masterSeed, StationResourceFieldConfig config, GameDataRegistry registry)` returns GameStateData with appended SpaceObjects and manifest; `ValidateSaved(GameStateData state, GameDataRegistry registry)` returns normalized validated state without RNG draws. Public DTOs coexist with internal static helper in the new file.

Dependency US-0004: GameStateData.TradingMap.Rules.Stations(ObjectId,MarketProfileId), TradingMap.Edges(FromStationObjectId,ToStationObjectId); actual station positions in SpaceObjects. No map geometry/role creation here.

## Implementation steps

1. Loader strict-deserializes config, resolves relative path against Settings directory, validates references against existing registry.ItemTypes/StationMarketProfiles before constructing engine. Wire all three bootstrap paths; ReadJson/default content behavior remains. Configure requires no graphics and does not add a field to GameDataRegistry. Saved Rules are used for saved fields even if latest config differs; current catalog compatibility/profile checks stay active.
2. Validate schema/nulls/IDs/unique case-sensitive kind/variant/profile IDs; references all resolve; role entry exactly once for each five MVP market profiles, counts positive, no duplicate kind per role. Fractions unique Resource/Cargo item IDs, positive permille, checked sum1000. Base composition exactly Ice/Silicate/Iron. Variant weights >=0, at least one positive; mass bands valid disjoint inclusive intervals within1000000..1000000000, nonnegative weights and positive total. Finite geometry, 0<clearance<=inner<outer, spacing/corridor>0, maxAttempts>0, firstObjectNumber>=1 and count/last ID arithmetic checked. Survey range/duration positive, chance0..100. Write Trace.TraceInformation for loaded weighted tables (raw weights/total), no generated QA files.
3. Canonical order station ObjectId ordinal → FieldKindId ordinal → object ordinal; ID=`SPC-{number:D4}` starting FirstObjectNumber, checked increment. Reject ANY collision with existing ObjectIds (case-insensitive); do not silently shift the ID range. Store next unused number. Keep source ship/stations/asteroids and trading map bitwise equivalent as data; append only generated asteroids.
4. For each station derive stationSeed=RngStreamSeedDerivation.DeriveStreamSeed(masterSeed,"ResourceFields:"+stationId). Three streams names `ResourceFields:{stationId}:Geometry`, `...:Composition`, `...:Mass`; seed=DeriveStreamSeed(stationSeed,purpose) where purpose is exactly Geometry/Composition/Mass. Use RngStreamNames.CreateDeterministicRandom(seed), skip1000 NextDouble initially, counter10000; each NextDouble increments checked counter by10. Mass takes weighted band draw then floor(draw*(max-min+1))+min. Composition takes weighted variant draw, zero weights excluded. Canonical weight order is band ID/variant ID ordinal, so JSON order irrelevant. RNG helper must also restore seed/counter, skipping counter/10; normalize nonmultiple counters upward with Trace.TraceWarning, overflow rejects. Expose internal `ResourceFieldRandom` with constructor(ResourceFieldRngData), `double NextDouble()`, `ResourceFieldRngData Capture()`; this is reused by TK-0004. New missing streams warn once. Scanner streams use name ResourceSurvey:{ObjectId}:{ModuleId}, seed=DeriveStreamSeed(masterSeed,name), not these three station streams; TK-0004 creates them on the first completed attempt.
5. Per asteroid attempt: two geometry draws; angle=2*pi*u, radius=sqrt(inner²+v*(outer²-inner²)); world coordinates=station+(cos,sin)*radiusKm*10. Test clearance from ALL stations; spacing from all existing/generated asteroids; and distance to each closed economic edge segment >=CorridorHalfWidthKm. Segment-distance uses clamped projection t=dot(P-A,B-A)/|B-A|²; duplicate endpoints invalid upstream. Exactly maxAttempts tries, no count reduction/fallback overlap. Exhaustion => ScenarioException including station/kind/ordinal; stage result locally so existing world remains unchanged. Candidate generation never uses screen pixels.
6. Generate SpaceObjectData: ObjectType Asteroid, PersistenceType Permanent, Name=null, SpeedMps0, DirectionDegrees0, MovementType Stationary, IsKnown=true, Modules empty, actual mass/composition as drawn. Manifest holds actual fractions/variant and CompositionKnown=false; Surveys initially empty. Base existing image resolver selects actual saved sprite. For outgoing snapshot of unrevealed generated asteroids substitute neutral regular asteroid image deterministically (same existing resolver with CompositionType Silicate) and Survey(MassKg,false,false,null,empty). Do not expose field kind, profile, fractions or actual ice image through DisplayName/Image. Leave existing non-field asteroid behavior unchanged.
7. In LoadScenario: after US-0004 map is materialized and masterSeed resolved, before runtime objects and commit, restore existing field state via ValidateSaved; otherwise generate only if !isSave && SaveFormatVersion==0 && TradingMap!=null && config!=null. Save has neither state nor retroactive generation. Stage `_stationResourceFields` with runtime and assign together in existing commit lock; CaptureSaveState writes current state next to materialized objects.
8. ValidateSaved verifies schema, masterSeed present, config, manifest unique IDs, owning five stations/profiles, permanent/known/stationary asteroid IDs, mass bounds, actual base composition agrees with variant, stored fractions equal variant, exact configured station/kind counts and canonical ID assignments, nextObjectNumber beyond allocated range, stream names/seeds/counters match allowed namespaces, annulus/station/corridor clearances and spacing between permanent generated fields. Initial spacing against legacy moving asteroids is generation-only: their later motion cannot invalidate a good save. No regenerate/resize/reprice. Surveys shape: unique command, module and target, existing player/module and manifest target, nonnegative timestamps, due>start, no job for CompositionKnown target. Detailed scanner eligibility resumes in TK-0004. Saved actual fields override latest config; legacy no-state still works. Physical asteroid removal/respawn is not introduced.

## Out of scope

StructuralScan runtime (TK-0004), UI, generation/repair of US-0004 map, asteroid motion/orbits, markets/production/cargo, global object allocator, broad RNG retrofit, requirements edits.

## Invariants

- Base composition/mass/knowledge: EngineRequirements.md:1227–1267,1461–1574.
- Managed independent streams and counters: EngineRequirements.md:314–348; fixed FNV derivation API already exists.
- Engine current world survives bad load: SimulationEngine.cs:230–232.
- World units100m and no obstacle avoidance: EngineRequirements.md:5337–5341; placement corridors are explicit geometry constraints, not physics.

## Tests

StationResourceFieldTests:
- `Profiles_generate_exact_counts_and_resource_fractions` — AC-01.
- `Same_seed_and_reordered_inputs_generate_identical_fields` / `Other_rng_streams_do_not_change_fields` — AC-02.
- `Mass_draws_follow_configured_bands_and_saved_counters` — AC-02/05; exact draws/replay, no statistical flakiness.
- `All_objects_respect_annulus_station_clearance_spacing_and_corridors` — AC-03, seeds0..255 and ulong.MaxValue with deterministic US-0004 map fixture.
- `Impossible_placement_and_id_collision_leave_loaded_world_unchanged` — AC-02/03/05.
- `Save_load_preserves_all_fields_without_rng_or_regeneration` / `Legacy_asteroid_motion_does_not_invalidate_saved_fields` — AC-05.
- `Bad_manifest_seed_fraction_or_reference_rejects_atomically` — AC-05.
- `No_map_no_config_and_legacy_save_do_not_generate_fields` — AC-07.
- `Fields_do_not_mutate_station_inventory_budget_or_player_cargo` — AC-07; compare state with identical world/seed before/after field generation, excluding generated objects only.
- `Unrevealed_field_snapshot_has_neutral_image_and_no_resources` — knowledge precondition for TK-0004.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~StationResourceFieldTests|FullyQualifiedName~ObjectImageResolutionTests|FullyQualifiedName~ScenarioLoaderTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps только в пяти файлах, US-0004 contract доступен до исполнения; отсутствие prerequisite не скрывается.
- Все served criteria покрыты named tests; full configured count/geometry и save equality проверены.
- Tests/build/format проходят либо исходные failures явно зафиксированы; невыполненный geometry AC не помечается done.
- API/invariants/scope соблюдены; нет hidden changes, генерации старого save, утечки состава или незаписанных assumptions.
- Result проверяем по snapshot/save и tests без UI, рынки не изменены генератором.

## Self-containment check

Полная схема, режимы bootstrap, точные seed/counter/ID/geometry algorithms и validator obligations заданы. Новые DTO/validator/generator сгруппированы в одном thematic Engine file; не нужны дополнительные registry/settings files или поиск неизвестных API.
