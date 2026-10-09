# EP-0005 full rebuild audit

Source HEAD 2de330c78b1e3f1ec2154001d35c75a300d2fd6d plus final graph-ticket documentation changes; exact input hashes are in input-evidence.json.

**Token accounting:** host agent API did not expose usage. Generic zero token values below are placeholders, not a measured free run. No external LLM API was called.

**Health limitations:** diagnostics.json records unresolved AST references, duplicate IDs, self loops and same-endpoint edge collapse. graph.json is an undirected navigation projection; extraction.json retains provenance-distinct relationships. Documents and graph edges do not prove runtime acceptance.

# Graph Report - DSS-EP-0005  (2026-10-09)

## Corpus Check
- Large corpus: 800 files · ~1,038,794 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder.

## Summary
- 10314 nodes · 32559 edges · 329 communities (306 shown, 23 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 4174 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Game Session Rendering
- Client Graphics Dependencies
- Engine Scenario Content
- Authoritative Snapshot Prediction
- Simulation Engine Economy
- Simulation Speed Label Layout
- Economy Time Continuity
- Scenario Market Content Loading
- Shared Contract Serialization
- Local Session Integration
- Command Panel Tests
- Screen Event Tests
- Scenario Engine Tests
- Solar System Map Snapshot
- Load Screen
- Tactical Map Depth Renderer
- Game Session Object Interaction Tests
- Skia Window
- Content Exception
- Trade Screen Tests
- Engine Command Tests
- Trade Ux Tests
- Trade Quote Calculator Tests
- Finance Screen
- Canonical Engine requirements
- Scenario Exception
- Quoted Trade Execution Tests
- Game Data Registry
- Save Layout
- Station Market Profile Definition
- Local Cluster Generation Tests
- Space Object Runtime
- Object Motion Snapshot
- Map View Geometry Tests
- Station Toolbar Tests
- Commands Panel
- Approach Pursuit Math Tests
- Trade Painter
- Temp Character Image Screen
- Dialogue State
- Game Menu Screen
- Trade Screen
- Linear Motion Predictor
- Object Render State
- Camera State
- Trade Model
- Grid Panel
- Object Trail Store
- Approach Route
- Screen Event
- Scenario Select Screen
- Save Screen
- Combat Trajectory Tests
- Main Menu Screen
- Portrait Asset Repository
- Installed Module Runtime
- Screen Stack
- IGame Session Connection
- Trading Map Schema Tests
- Trading Map Bootstrap Tests
- Navigation Trajectory Projector
- Balance Report Tests
- Snapshot Mailbox
- Hire Screen
- Station Screen Tests
- Resource Field Random
- Route Risk Content Tests
- Player Command
- Simulation Clock
- Trading Map State Data
- Voyage State Data
- Object Label Tests
- Voyage Ui Fixture
- Item Catalog Schema Tests
- Object Info Panel
- Resource Survey Tests
- Combat Effect Store
- Keyboard Edge Tracker Tests
- Tactical Map State Updater
- Scenario Select Layout Tests
- Trading Economy Continuation Data
- Torpedo Preview Tests
- Controlled Connection
- Deep Space Saga Economy Balance
- Station Economy Batch2Tests
- Contracts Screen
- Cargo Stack Data
- Object Info Panel Tests
- Cluster Map Presentation
- Trajectory Viewport Tests
- Dialogue Definition
- Ship Screen
- Station Market Event Definition
- Station Resource Field Tests
- Trade Command Tests
- Trading Map Generation Data
- Game Session Scale Panel Tests
- Object Trail Buffer
- Cluster Voyage Fixture
- Pause Simulation Tests
- Deep Space Saga Client Portraits
- Tactical Map Snapshot State
- Dialogue Screen
- Future Trajectory Point
- Trading Voyage Fixture
- Generic Window Type A
- Game Session Handle
- Station Screen
- Station Toolbar
- Station Economy Generation Tests
- Balance Strategy Evidence
- Approach Pursuit Math
- IScreen Concepts
- Trading Map Geometry Tests
- Trade Quote Snapshot
- Market Knowledge Tests
- Balance Run Tests
- Text Input Box
- Torpedo Route
- Game State Data
- Voyage Fuel Lifecycle Tests
- Station Market Demo Content Tests
- Immutable Array Default Json Converter
- Station Inventory Item Snapshot
- Navigation Waypoint Math
- Tactical Map View Tests
- Render Motion
- Trading Graph Generator Tests
- Object Trail Geometry
- Cluster Case Evidence
- Variant Assets
- Menu Style
- Space Object Data
- Recording Connection
- Market Health Evaluator Tests
- Station Market Profile Content Tests
- Deep Space Saga Client UI
- Info Panel Tests
- Async Object Image Cache
- Station Market Catalog Content Tests
- Combat Bootstrap Tests
- Balance Driver
- Basic Combat Ui Flow Tests
- Speed Panel Tests
- Combat Content Loader Tests
- Portrait Composer
- Scenario Select Row Visuals Tests
- Command Result
- Delayed Travel Connection
- Dialogue Tests
- Torpedo Command Panel Tests
- Generic Button Type A
- Portrait Part
- Settings Screen
- Scenario File
- Route Risk Integration Tests
- Torpedo Impact Tests
- Presented frame consumers and performance
- Window Thread Context
- Controllable Connection
- Grid Renderer Tests
- Tactical Map Spatial Index
- Recording Connection
- Orbital Synchronization Tests
- Image Button
- Character Appearance
- Combat Impact Snapshot
- Space Map Color Resolver Tests
- Tactical Map Settings
- Station Event Runtime
- Weapon Operator Schema Tests
- Engine Trade Connection
- Strategy Balance Evaluator Tests
- Approach Command Tests
- Combat Run
- Save Slot Repository Tests
- Combat Visual Settings
- Combat Journal Entry
- Torpedo Guidance Tests
- Navigation Trajectory Projector
- Combat Save Load Tests
- Trade Journal
- Countermeasure Snapshot
- Localization Tests
- Dialogue Command
- Collision Path
- Station Resource Field Content Tests
- Trade Snapshot Projection Tests
- Voyage Ledger Lifecycle Tests
- August 9 small technical tasks
- Ship Event
- Station Screen
- Map Frame Evidence
- Command Definition Loader Directory Tests
- Repeatable Trading Voyage Ui Tests
- Automatic Defense Tests
- Cargo Cost Accounting Tests
- Countermeasure Rng
- Quote Terms
- Game Time Display
- Progress Bar Tests
- Tactical Map Revision
- Balance Hourly Sample
- Combat Session Fixture
- Orbital Docking Departure Tests
- Voyage Fuel Accounting Tests
- Voyage Profit Accounting Tests
- Installed Module Snapshot
- Settings Layout
- Game Session Docking Request Tests
- Recording Connection
- Dock Command Tests
- Dialogue Layout
- Tactical Map Marker Policy Tests
- Station Layout
- Trading Economy Save Schema Tests
- Game Session Scale Filter Tests
- Game Session Ui Scale Tests
- Voyage Save Load Continuity Tests
- Whole Face Assets
- Whole Head Assets
- Station Market Event Content Tests
- Portrait Style Profile
- Deep Space Saga Contracts csproj
- Trading Map Data Validation
- Defense Command Panel Tests
- Mechanics Panel Tests
- Torpedo Launcher Content Tests
- Combat Wreck Tests
- Torpedo History Tests
- Trading Economy Continuity Tests
- Revision driven scene cache ticket
- Tetrarch Class starting ship
- Reference Feature Assets
- Ui Asset Loader Tests
- Approach Camera Integration Tests
- Voyage Snapshot
- Recording Connection
- Recording Connection
- Nine Patch Tests
- Tactical Map Cluster Stability Tests
- Smoke Tests
- Market Save Load Continuity Tests
- Person Sex
- Countermeasure Content Loader Tests
- Recording Connection
- Temporary Content
- Torpedo Inspection Tests
- Countermeasure Bootstrap Tests
- Portrait Generator Tests
- Xenon Style
- Asteroid Survey Snapshot
- Combat State Data
- Object Label Layout
- Station Market Knowledge Snapshot
- Station Size Factors Tests
- Approach Camera Integration Tests
- Game Session Dock Auto Open
- Panel Engine Connection
- Legacy Connection
- Ration Schedule Tests
- Object Image Resolution Tests
- Localization Concepts
- Grid Renderer
- Game Session Screen Trail Color
- Dialogue Integration Tests
- Status Square Animator
- Combat Event Type
- Pirate Scenario Tests
- Settings Screen Tests
- Unified Portrait Tests
- Port Fee Schedule Tests
- Hair Assets
- Deep Space Saga Client csproj
- Tactical Pipeline Evidence
- Station District
- Tetrarch Loadout Tests
- Voyage Finance Contract Tests
- Voyage Fuel Contract Tests
- Trading Continuation Review Tests
- Voyage Lifecycle Tests
- Economy Balance Seam Tests
- Combat Save Validation
- Countermeasure Save Validation
- Snapshot Render Metadata Tests
- Content Fixture
- Deep Space Saga Performance Tests
- Tactical Pipeline Evidence
- Countermeasure Ui Flow Tests
- Nine Patch
- Deep Space Saga Client Tests
- Main Menu Screen Tests
- Scenario Select Screen Tests
- Deep Space Saga Contracts Tests
- Deep Space Saga Engine Tests
- Deep Space Saga Motion Tests
- Game Info
- Tactical Map Animation Kind
- Settings Button
- Station Route Presentation
- Countermeasure Termination Kind
- Countermeasure State Data
- Countermeasure Content Tests
- Default System Content Tests
- Local Cluster Content Tests
- Countermeasure Event Tests
- Deep Space Saga Economy Balance
- Cluster Voyage Integration Tests
- Xenon Combo Box
- Navigation Phase
- Combat Palette Content Tests
- Object Info Panel Tests
- Save Reliability Tests
- System Performance Report Tests
- Test Assembly Setup
- Entry Concepts
- Voyage Amount Kind
- Countermeasure Palette Tests
- Countermeasure Visual Settings Tests
- Image Button Tests
- Countermeasure Snapshot Tests
- Smoke Tests
- Object Info Panel State
- Trading Map Schema Tests cs
- Launch Mode
- Tactical Map Label Bounds Tests
- Tactical Map View Tests
- Object Label Text

## God Nodes (most connected - your core abstractions)
1. `GameSessionScreen` - 537 edges
2. `SimulationEngine` - 537 edges
3. `DeepSpaceSaga.Contracts` - 380 edges
4. `ObjectMotionSnapshot` - 302 edges
5. `AuthoritativeSnapshot` - 280 edges
6. `ScenarioException` - 232 edges
7. `LinearMotionPredictor` - 208 edges
8. `SnapshotBuffer` - 204 edges
9. `DeepSpaceSaga.Engine.Scenario` - 158 edges
10. `DeepSpaceSaga.Client.Tests` - 154 edges

## Surprising Connections (you probably didn't know these)
- `Canonical Engine requirements` --documents--> `Buffer`  [EXTRACTED]
  Documentation/01-Requirements/EngineRequirements.md → src/DeepSpaceSaga.Client/GameSessionHandle.cs
- `Canonical Engine requirements` --documents--> `Width`  [EXTRACTED]
  Documentation/01-Requirements/EngineRequirements.md → src/DeepSpaceSaga.Client/Portraits/PortraitModels.cs
- `Canonical Engine requirements` --documents--> `Height`  [EXTRACTED]
  Documentation/01-Requirements/EngineRequirements.md → src/DeepSpaceSaga.Client/Portraits/PortraitModels.cs
- `Female face proportions` --documents--> `PortraitGenerator`  [EXTRACTED]
  Documentation/03-Design/FemaleFaceProportions.md → src/DeepSpaceSaga.Client/Portraits/PortraitModels.cs
- `Canonical Engine requirements` --documents--> `Error`  [EXTRACTED]
  Documentation/01-Requirements/EngineRequirements.md → src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Immutable presentation pipeline stages** — board_ep_0005_optimization_documentation_update_prepare_draw, board_ep_0005_optimization_ep_0005_us_0002_tactical_map_render_pipeline_ep_0005_us_0002_tk_0001_frame_state_update_ep_0005_us_0002_tk_0001_frame_state_update_document, board_ep_0005_optimization_ep_0005_us_0002_tactical_map_render_pipeline_ep_0005_us_0002_tk_0002_scene_geometry_prepare_ep_0005_us_0002_tk_0002_scene_geometry_prepare_document, board_ep_0005_optimization_ep_0005_us_0002_tactical_map_render_pipeline_ep_0005_us_0002_tk_0003_read_only_map_painter_ep_0005_us_0002_tk_0003_read_only_map_painter_document [EXTRACTED 1.00]
- **F02 and F04-F13 plus R01-R04 audit remediation tickets** — board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tactical_map_audit_remediation_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0001_paused_authoritative_rebase_ep_0005_us_0001_tk_0001_paused_authoritative_rebase_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0002_visible_object_hit_testing_ep_0005_us_0001_tk_0002_visible_object_hit_testing_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0003_cluster_click_priority_ep_0005_us_0001_tk_0003_cluster_click_priority_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0004_important_label_placement_ep_0005_us_0001_tk_0004_important_label_placement_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0005_bounded_offscreen_work_ep_0005_us_0001_tk_0005_bounded_offscreen_work_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0006_paused_geometry_invalidation_ep_0005_us_0001_tk_0006_paused_geometry_invalidation_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0007_coherent_frame_diagnostics_ep_0005_us_0001_tk_0007_coherent_frame_diagnostics_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0008_layout_before_map_ep_0005_us_0001_tk_0008_layout_before_map_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0009_nonblocking_render_io_ep_0005_us_0001_tk_0009_nonblocking_render_io_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0010_deterministic_resource_lifetime_ep_0005_us_0001_tk_0010_deterministic_resource_lifetime_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0011_map_localization_ep_0005_us_0001_tk_0011_map_localization_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0012_stable_cluster_membership_ep_0005_us_0001_tk_0012_stable_cluster_membership_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0013_reconciled_route_join_ep_0005_us_0001_tk_0013_reconciled_route_join_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0014_stale_snapshot_policy_ep_0005_us_0001_tk_0014_stale_snapshot_policy_document, board_ep_0005_optimization_ep_0005_us_0001_tactical_map_audit_remediation_ep_0005_us_0001_tk_0015_free_viewport_cost_ep_0005_us_0001_tk_0015_free_viewport_cost_document [EXTRACTED 1.00]
- **Authoritative local-market transactions coordinate inventory, currency and fuel** — documentation_02_firstrelease_mechanics_trading_document, documentation_02_firstrelease_mechanics_stationinventory_document, documentation_02_firstrelease_mechanics_money_document, documentation_02_firstrelease_mechanics_fuel_document [EXTRACTED 1.00]
- **Shared linear dialogue format for crew and station representatives** — documentation_02_firstrelease_screens_dialog_document, documentation_02_firstrelease_mechanics_crewdialogues_document, documentation_02_firstrelease_mechanics_stationdialogues_document [EXTRACTED 1.00]
- **W4 and M4 dialogue portrait composition** — documentation_03_design_dialogueportraits_document, documentation_03_design_portraitgeneratorm4_document, documentation_03_design_portraitgeneratorw4_document [EXTRACTED 1.00]
- **EP-0005 clustering, displayed-frame interaction and stale snapshot handling** — documentation_02_firstrelease_screens_gamesession_document, documentation_02_firstrelease_mechanics_solarsystemmapconcept_world_cell_clusters, documentation_02_firstrelease_mechanics_tacticalmapandmaneuvering_presented_frame_input [INFERRED 0.85]
- **Separate raster cost, native presentation and acceptance evidence** — documentation_04_engineering_performance500_readme_document, documentation_04_engineering_performance500_gpu_jitter_2026_09_24_document, documentation_04_engineering_performance500_x100_smoothness_2026_09_16_document, tools_deepspacesaga_performance_readme_document [INFERRED 0.85]

## Communities (329 total, 23 thin omitted)

### Community 0 - "Game Session Rendering"
Cohesion: 0.01
Nodes (177): Map localization, CameraZoomTransition, CommandsPanel, Encounter, Free, Future, GeometryView, Kind (+169 more)

### Community 1 - "Client Graphics Dependencies"
Cohesion: 0.03
Nodes (30): DeepSpaceSaga.Client.UI.Screens.ScenarioSelect, DeepSpaceSaga.Client.UI.Screens.Contracts, DeepSpaceSaga.Client.UI.Screens.Save, DeepSpaceSaga.Client.UI.Assets, DeepSpaceSaga.Client.UI.Controls, DeepSpaceSaga.Client.UI.Screens.GameMenu, DeepSpaceSaga.Client.UI.Screens.GameSession.Controls, DeepSpaceSaga.Motion (+22 more)

### Community 2 - "Engine Scenario Content"
Cohesion: 0.03
Nodes (11): DeepSpaceSaga.Engine.Trading, DeepSpaceSaga.Engine.Scenario, DeepSpaceSaga.Engine.Tests, DeepSpaceSaga.Engine.Content, DeepSpaceSaga.Engine, DeepSpaceSaga.Engine.Rng, DeepSpaceSaga.Engine.LocalClient, system_linq (+3 more)

### Community 3 - "Authoritative Snapshot Prediction"
Cohesion: 0.03
Nodes (69): BufferedSnapshot, PredictionDeltaMs, Dictionary, Func, SnapshotBuffer, CurrentSpeed, EffectivePredictionDeltaMs, Latest (+61 more)

### Community 4 - "Simulation Engine Economy"
Cohesion: 0.02
Nodes (68): Events, Hulls, IssuedQuote, Launchers, MarketEventFlow, MarketItemLimits, MaximumBudget, Pose (+60 more)

### Community 5 - "Simulation Speed Label Layout"
Cohesion: 0.03
Nodes (81): Root developer guide shim, Canonical developer guide, Docking, navigation.dock, World-cell clustering with hysteresis, Tactical map and maneuvering, Presented-frame input and stale prediction, GameSession screen (+73 more)

### Community 6 - "Economy Time Continuity"
Cohesion: 0.06
Nodes (29): Due, Ice, Pending, FactoryTypeDefinition, StationInventoryItemData, StationProducingModuleData, Steel, TargetInvocationException (+21 more)

### Community 7 - "Scenario Market Content Loading"
Cohesion: 0.03
Nodes (56): After, Before, EngineSettingsFile, English, ExpectedEconomy, FirstHour, LoadedEngineContent, ProfileId (+48 more)

### Community 8 - "Shared Contract Serialization"
Cohesion: 0.04
Nodes (17): DeepSpaceSaga.Contracts, DeepSpaceSaga.Contracts.Tests, DeepSpaceSaga.Motion.Tests, DeepSpaceSaga.Client, DeepSpaceSaga.Performance.Tests, CharacterPortraits, NavigationComputerCommandTypes, ScannerCommandTypes (+9 more)

### Community 9 - "Local Session Integration"
Cohesion: 0.07
Nodes (28): EconomicFixture, IAsyncEnumerable, CancellationToken, CancellationTokenSource, IAsyncEnumerable, SemaphoreSlim, SimulationEngine, ValueTask (+20 more)

### Community 10 - "Command Panel Tests"
Cohesion: 0.07
Nodes (23): Enabled, Name, CancellationToken, Dictionary, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable (+15 more)

### Community 11 - "Screen Event Tests"
Cohesion: 0.08
Nodes (22): X, Y, DateTime, Fact, Func, H, IReadOnlyList, W (+14 more)

### Community 12 - "Scenario Engine Tests"
Cohesion: 0.07
Nodes (12): CancellationToken, IAsyncEnumerable, Action, Fact, IEnumerable, InlineData, SimulationEngine, Task (+4 more)

### Community 13 - "Solar System Map Snapshot"
Cohesion: 0.05
Nodes (48): Belt, EP-0002 implementation evidence, EP-0003 implementation and review evidence, Lod, CameraState, Dictionary, SKCanvas, SKPath (+40 more)

### Community 14 - "Load Screen"
Cohesion: 0.06
Nodes (40): H, SKRect, W, X, Y, LoadHit, LoadLayout, ListHeight (+32 more)

### Community 15 - "Tactical Map Depth Renderer"
Cohesion: 0.06
Nodes (28): Alpha, RadiusMultiplier, ReticleGlow, SKCanvas, SKColor, SKImage, SKPaint, SKPath (+20 more)

### Community 16 - "Game Session Object Interaction Tests"
Cohesion: 0.09
Nodes (18): ActiveObjectId, SelectedObjectId, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, List (+10 more)

### Community 17 - "Skia Window"
Cohesion: 0.06
Nodes (23): GL, GRBackendRenderTarget, GRContext, GRContextOptions, IInputContext, IMouse, IWindow, RawImage (+15 more)

### Community 18 - "Content Exception"
Cohesion: 0.05
Nodes (53): CommandDefinitionDto, CommandDefinitionsFile, EconomyContentVersions, FactoryTypeDefinitionDto, Items, ItemTypeDefinitionDto, ItemTypesFile, ModuleCategoryDefinitionDto (+45 more)

### Community 19 - "Trade Screen Tests"
Cohesion: 0.08
Nodes (23): EngineTradeConnection, SKRect, TradeLayout, MouseButton, Entry, Fact, GameSessionHandle, InlineData (+15 more)

### Community 20 - "Engine Command Tests"
Cohesion: 0.10
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, EngineCommandTests

### Community 21 - "Trade Ux Tests"
Cohesion: 0.11
Nodes (16): Fact, GameSessionHandle, ImmutableArray, InlineData, RecordingConnection, SKBitmap, SKRect, SnapshotBuffer (+8 more)

### Community 22 - "Trade Quote Calculator Tests"
Cohesion: 0.06
Nodes (34): Pricing, TradePriceReason, IReadOnlyList, PriceClampKind, Ceiling, Floor, None, StationPricing (+26 more)

### Community 23 - "Finance Screen"
Cohesion: 0.07
Nodes (28): FinanceRow, FinanceLayout, ImmutableArray, IReadOnlyList, Key, MouseButton, SKBitmap, SnapshotBuffer (+20 more)

### Community 24 - "Canonical Engine requirements"
Cohesion: 0.06
Nodes (69): Requirements engineering guidance, Documentation system, Approach shortest Dubins route, Asynchronous Engine / Contracts / Client boundary, 2000 ms real-time stale prediction limit, Coherent tactical frame and presented input, EP-0007 automatic countermeasure combat, Canonical Engine requirements (+61 more)

### Community 25 - "Scenario Exception"
Cohesion: 0.08
Nodes (15): HashSet, JsonSerializerOptions, ScenarioException, ScenarioLoader, StationClusterSaveValidation, Fact, InlineData, Theory (+7 more)

### Community 26 - "Quoted Trade Execution Tests"
Cohesion: 0.14
Nodes (15): ArgumentNullException, Fact, ImmutableDictionary, InlineData, InstalledModuleRuntime, SimulationEngine, SpaceObjectRuntime, Theory (+7 more)

### Community 27 - "Game Data Registry"
Cohesion: 0.07
Nodes (38): CommandDefinition, FactorDefaults, IEnumerable, GameDataRegistry, CatalogCompatibility, CatalogVersion, CommandDefinitions, Dialogues (+30 more)

### Community 28 - "Save Layout"
Cohesion: 0.08
Nodes (30): H, SKRect, W, X, Y, SaveLayout, ListWidth, H (+22 more)

### Community 29 - "Station Market Profile Definition"
Cohesion: 0.07
Nodes (30): Action, HashSet, Dictionary, IEnumerable, ImmutableArray, ImmutableDictionary, StationMarketEconomyDefinition, AllStockTargets (+22 more)

### Community 30 - "Local Cluster Generation Tests"
Cohesion: 0.06
Nodes (29): ScenarioGroupPlacement, Fact, SimulationEngine, ClusterResourcePlacementTests, Fact, GenerationInputSchemaTests, Fact, KnownMapProjectionTests (+21 more)

### Community 31 - "Space Object Runtime"
Cohesion: 0.06
Nodes (18): DefenseLaunch, ActiveCycleData, Contact, Projectile, Target, Contact, ImmutableArray, Projectile (+10 more)

### Community 32 - "Object Motion Snapshot"
Cohesion: 0.07
Nodes (25): ObjectMotionSnapshot, CountermeasureGuidanceMath, Span, X, Y, OrbitalMotionMath, Fact, ArchitectureTests (+17 more)

### Community 33 - "Map View Geometry Tests"
Cohesion: 0.06
Nodes (22): ITestOutputHelper, MapCluster, IReadOnlyList, SKCanvas, SKRect, CameraState, IReadOnlyList, SKRect (+14 more)

### Community 34 - "Station Toolbar Tests"
Cohesion: 0.08
Nodes (6): Fact, SKBitmap, SKColor, SKPaint, SKRect, StationToolbarTests

### Community 35 - "Commands Panel"
Cohesion: 0.06
Nodes (38): Button, CommandIconPair, Action, Dictionary, Func, ImmutableArray, IReadOnlyDictionary, IReadOnlyList (+30 more)

### Community 36 - "Approach Pursuit Math Tests"
Cohesion: 0.08
Nodes (13): ApproachFlyThroughPlan, FamilyFormula, Plan, X, Y, Fact, InlineData, List (+5 more)

### Community 37 - "Trade Painter"
Cohesion: 0.11
Nodes (14): Item catalog schema 2, September 12 trade UI, SKCanvas, TradeItemPresentation, SKCanvas, SKColor, SKPaint, SKRect (+6 more)

### Community 38 - "Temp Character Image Screen"
Cohesion: 0.07
Nodes (33): FileNotFoundException, JsonSerializerOptions, AppearanceSerializer, CharacterVisualState, Action, Image, Key, Label (+25 more)

### Community 39 - "Dialogue State"
Cohesion: 0.05
Nodes (39): DialogueChoiceSnapshot, ImmutableDictionary, DialogueEvent, ImmutableArray, ImmutableDictionary, ImmutableHashSet, DialogueState, PlayerCharacterState (+31 more)

### Community 40 - "Game Menu Screen"
Cohesion: 0.08
Nodes (20): SKRect, GameMenuButton, Load, MainMenu, None, Resume, Save, Settings (+12 more)

### Community 41 - "Trade Screen"
Cohesion: 0.05
Nodes (36): InputFocus, QuoteKey, CancellationTokenSource, Entry, GameSessionHandle, IReadOnlyList, Key, ScreenEvent (+28 more)

### Community 42 - "Linear Motion Predictor"
Cohesion: 0.11
Nodes (22): Repository agent guide shim, EP-0005 tactical map optimization, Update Prepare Draw, EP-0005 repository documentation impact inventory, Tactical map audit remediation story, Coherent frame diagnostics, Nonblocking render I/O, Deterministic resource lifetime (+14 more)

### Community 43 - "Object Render State"
Cohesion: 0.07
Nodes (40): ObjectRenderState, Predicted, ImmutableArray, ImmutableDictionary, SnapshotPrediction, VisualCorrection, TacticalMapFrameState, MotionTimeMs (+32 more)

### Community 44 - "Camera State"
Cohesion: 0.09
Nodes (19): Paused authoritative rebase, Visible object hit testing, Cluster click priority, Paused geometry invalidation, Layout before map, Stable cluster membership, Reconciled route join, Stale snapshot policy (+11 more)

### Community 45 - "Trade Model"
Cohesion: 0.05
Nodes (35): Maximum, Command, TradeMode, Buy, Refuel, Sell, TradeModel, ActiveEvents (+27 more)

### Community 46 - "Grid Panel"
Cohesion: 0.10
Nodes (18): IReadOnlyList, SKCanvas, SKColor, SKPaint, SKRect, GridPanel, GridSortColumn, BuyingCount (+10 more)

### Community 47 - "Object Trail Store"
Cohesion: 0.15
Nodes (14): Dictionary, Func, HashSet, IReadOnlyList, IReadOnlySet, List, ObjectTrailStore, Trails (+6 more)

### Community 48 - "Approach Route"
Cohesion: 0.12
Nodes (16): ApproachRoute, Length, ActiveEngineCycleMotion, Direction, X, Y, ApproachLineCaptureMath, ApproachRoute (+8 more)

### Community 49 - "Screen Event"
Cohesion: 0.04
Nodes (42): ScenarioSelect screen, MouseButton, ScreenEvent, CloseContracts, CloseDialogue, CloseFinance, CloseHire, CloseLoadWindow (+34 more)

### Community 50 - "Scenario Select Screen"
Cohesion: 0.06
Nodes (31): ScenarioRow, SKBitmap, SKCanvas, SKRect, ImagePanel, HasLoadedImage, ScenarioSelectHit, ScenarioSelectZone (+23 more)

### Community 51 - "Save Screen"
Cohesion: 0.06
Nodes (27): IReadOnlyList, SaveSlots, SaveHit, SaveZone, Close, Delete, None, Row (+19 more)

### Community 52 - "Combat Trajectory Tests"
Cohesion: 0.07
Nodes (22): MouseButton, Fact, InlineData, Theory, CombatTrajectoryTests, Snapshot, Fact, Task (+14 more)

### Community 53 - "Main Menu Screen"
Cohesion: 0.08
Nodes (19): MouseButton, SKCanvas, SKPaint, SKRect, MainMenuScreen, SKRect, MenuButton, Exit (+11 more)

### Community 54 - "Portrait Asset Repository"
Cohesion: 0.09
Nodes (25): Bitmap, Portrait asset preparation tooling, LinkedList, LinkedListNode, Dictionary, IReadOnlyList, PortraitAssetRepository, Parts (+17 more)

### Community 55 - "Installed Module Runtime"
Cohesion: 0.07
Nodes (21): RealizedCostCredits, Remaining, WeaponOperatorSnapshot, WeaponSkillType, CountermeasureDefense, TorpedoAttack, CargoAcquisitionSources, ShipModuleData (+13 more)

### Community 56 - "Screen Stack"
Cohesion: 0.11
Nodes (19): SKObject, Stack, ScreenStack, Count, Current, UnderCurrent, Fact, Key (+11 more)

### Community 57 - "IGame Session Connection"
Cohesion: 0.06
Nodes (14): IGameSessionFactory, LocalGameSessionFactory, QuickSavePath, SaveDirectory, ScenariosDirectory, SettingsPath, Program, CancellationToken (+6 more)

### Community 58 - "Trading Map Schema Tests"
Cohesion: 0.09
Nodes (18): Fact, InlineData, Theory, CountermeasureSaveLoadTests, Fact, ReviewRegressionTests, ConcurrentQueue, Fact (+10 more)

### Community 59 - "Trading Map Bootstrap Tests"
Cohesion: 0.10
Nodes (16): HullCellCoordinate, HullLayoutData, ScenarioMetadata, StartingScenario, Fact, InlineData, Theory, AuthoritativeOrbitRuntimeTests (+8 more)

### Community 60 - "Navigation Trajectory Projector"
Cohesion: 0.13
Nodes (13): ApproachRouteGeometryCache, ApproachRoute, ApproachRouteSignature, NavigationTrajectoryProjector, Fact, InlineData, IReadOnlyList, Task (+5 more)

### Community 61 - "Balance Report Tests"
Cohesion: 0.08
Nodes (22): Exception, Action, Fact, Func, ImmutableArray, JsonObject, BalanceReportTests, Output (+14 more)

### Community 62 - "Snapshot Mailbox"
Cohesion: 0.08
Nodes (25): IOException, Task, CancellationToken, Channel, Exception, Func, IAsyncEnumerable, ImmutableArray (+17 more)

### Community 63 - "Hire Screen"
Cohesion: 0.13
Nodes (14): HireLayout, Key, MouseButton, SnapshotBuffer, HireScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible, IsFuelTooltipVisible (+6 more)

### Community 64 - "Station Screen Tests"
Cohesion: 0.17
Nodes (6): MouseButton, SKRect, Fact, InlineData, Theory, StationScreenTests

### Community 65 - "Resource Field Random"
Cohesion: 0.09
Nodes (8): Random, RngStreamNames, RngStreamSeedDerivation, Random, ResourceFieldRandom, ResourceFieldRngData, Fact, RngStreamNamesTests

### Community 66 - "Route Risk Content Tests"
Cohesion: 0.08
Nodes (28): ActiveSave, Route, StationRouteRow, ImmutableArray, TradingRouteAvailability, Available, Restricted, Unavailable (+20 more)

### Community 67 - "Player Command"
Cohesion: 0.11
Nodes (11): MarketRevision, ImmutableArray, TradeExecutionReceipt, PlayerCommand, ResourceSurveyJobData, List, CommandStartOutcome, Started (+3 more)

### Community 68 - "Simulation Clock"
Cohesion: 0.12
Nodes (21): Compact orbital station cluster, Procedural Solar System map concept, Trading MVP and economic map generation, Economic graph before map geometry, Func, SimulationClock, GameTimeMs, SimulationTimeMs (+13 more)

### Community 69 - "Trading Map State Data"
Cohesion: 0.11
Nodes (17): TradingMapEdgeData, TradingMapStateData, IReadOnlyList, IEnumerable, ImmutableArray, List, From, ImmutableArray (+9 more)

### Community 70 - "Voyage State Data"
Cohesion: 0.08
Nodes (21): Consumed, Cost, Reserved, VoyageCargoRemainderSnapshot, VoyageFuelSettlementSnapshot, VoyageFuelReservationPartData, VoyageLedgerPostingData, VoyageStateData (+13 more)

### Community 71 - "Object Label Tests"
Cohesion: 0.11
Nodes (5): Fact, SKPoint, SKRect, SKSize, ObjectLabelTests

### Community 72 - "Voyage Ui Fixture"
Cohesion: 0.10
Nodes (22): IEnumerable, Func, GameSessionHandle, Task, TaskCompletionSource, StationTradeNavigation, Fact, GameSessionHandle (+14 more)

### Community 73 - "Item Catalog Schema Tests"
Cohesion: 0.13
Nodes (9): Fact, InlineData, JsonObject, Theory, ItemCatalogSchemaTests, Fact, InlineData, Theory (+1 more)

### Community 74 - "Object Info Panel"
Cohesion: 0.10
Nodes (24): PreparedRow, Dictionary, IReadOnlyList, Label, List, SKBitmap, SKCanvas, SKPaint (+16 more)

### Community 75 - "Resource Survey Tests"
Cohesion: 0.21
Nodes (11): Fact, Func, IEnumerable, InlineData, InstalledModuleRuntime, Lazy, List, SimulationEngine (+3 more)

### Community 76 - "Combat Effect Store"
Cohesion: 0.09
Nodes (23): CombatEffect, Func, HashSet, ImmutableArray, IReadOnlyList, List, CombatEffectStore, Active (+15 more)

### Community 77 - "Keyboard Edge Tracker Tests"
Cohesion: 0.13
Nodes (13): pressedCount, releasedCount, Func, IKeyboard, Key, Span, KeyboardEdgeTracker, Fact (+5 more)

### Community 78 - "Tactical Map State Updater"
Cohesion: 0.07
Nodes (29): Dictionary, HashSet, ImmutableArray, ImmutableDictionary, IReadOnlyDictionary, IReadOnlySet, List, SnapshotPrediction (+21 more)

### Community 79 - "Scenario Select Layout Tests"
Cohesion: 0.12
Nodes (18): H, IReadOnlyList, W, X, Y, ScenarioSelectLayout, ListLeft, ListWidth (+10 more)

### Community 80 - "Trading Economy Continuation Data"
Cohesion: 0.06
Nodes (22): List, SpaceObjectRuntime, StationSecuritySystem, IReadOnlyList, EngineIdentityCountersData, FocusData, SaveFormat, ShipPassengerData (+14 more)

### Community 81 - "Torpedo Preview Tests"
Cohesion: 0.09
Nodes (24): CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, List, SKBitmap, SKCanvas (+16 more)

### Community 82 - "Controlled Connection"
Cohesion: 0.12
Nodes (21): ControlledConnection, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task (+13 more)

### Community 83 - "Deep Space Saga Economy Balance"
Cohesion: 0.08
Nodes (10): DeepSpaceSaga.EconomyBalance.Tests, DeepSpaceSaga.EconomyBalance, DeepSpaceSaga.Engine.Dialogue, ResourceSurveyReasonCodes, StationRouteAvailabilityEffects, TradeItemCategories, VoyageFinanceStates, VoyagePhases (+2 more)

### Community 84 - "Station Economy Batch2Tests"
Cohesion: 0.16
Nodes (7): FactoryTypesFile, RecipeMaterial, Fact, ItemTypeId, Quantity, SimulationEngine, StationEconomyBatch2Tests

### Community 85 - "Contracts Screen"
Cohesion: 0.12
Nodes (14): ContractsLayout, Key, MouseButton, SnapshotBuffer, ContractsScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible, IsFuelTooltipVisible (+6 more)

### Community 86 - "Cargo Stack Data"
Cohesion: 0.10
Nodes (13): CargoStackData, InlineData, Theory, InlineData, SimulationEngine, Theory, CargoCostPersistenceTests, Fact (+5 more)

### Community 87 - "Object Info Panel Tests"
Cohesion: 0.16
Nodes (7): buffer, Fact, ImmutableArray, screen, SnapshotBuffer, ObjectInfoPanelTests, SettingsPath

### Community 88 - "Cluster Map Presentation"
Cohesion: 0.08
Nodes (18): CameraState, Dictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList, SKCanvas, ClusterMapPresentation, ClusterStationPresentation (+10 more)

### Community 89 - "Trajectory Viewport Tests"
Cohesion: 0.10
Nodes (17): ApproachRouteSample, ApproachRouteSignature, CameraState, ApproachRouteGeometryCache, ContinuationPointCount, ContinuationPoints, Endpoint, SampleCount (+9 more)

### Community 90 - "Dialogue Definition"
Cohesion: 0.10
Nodes (26): DialogueFile, QuestFile, HashSet, IEnumerable, ImmutableArray, JsonSerializerOptions, DialogueContentLoader, DialogueFile (+18 more)

### Community 91 - "Ship Screen"
Cohesion: 0.10
Nodes (16): Bottom, Left, Right, Top, ShipButton, Close, None, ShipLayout (+8 more)

### Community 92 - "Station Market Event Definition"
Cohesion: 0.09
Nodes (21): ImmutableArray, StationMarketEventsFile, IEnumerable, ImmutableArray, StationMarketEventCatalog, StationMarketEventDefinition, StationMarketEventIds, All (+13 more)

### Community 93 - "Station Resource Field Tests"
Cohesion: 0.20
Nodes (6): Fact, IEnumerable, InlineData, SimulationEngine, Theory, StationResourceFieldTests

### Community 94 - "Trade Command Tests"
Cohesion: 0.15
Nodes (10): InlineData, Theory, Fact, IEnumerable, InlineData, ItemTypeId, Quantity, SimulationEngine (+2 more)

### Community 95 - "Trading Map Generation Data"
Cohesion: 0.14
Nodes (14): Dictionary, From, HashSet, IEnumerable, IReadOnlyDictionary, IReadOnlyList, To, TradingGraphGenerator (+6 more)

### Community 96 - "Game Session Scale Panel Tests"
Cohesion: 0.17
Nodes (5): Fact, GameSessionScalePanelTests, Fact, GameSessionZoomTests, LogTailReader

### Community 97 - "Object Trail Buffer"
Cohesion: 0.10
Nodes (22): Bounded offscreen work, IEnumerator, IReadOnlyList, MaxX, MaxY, MinX, MinY, Frozen (+14 more)

### Community 98 - "Cluster Voyage Fixture"
Cohesion: 0.10
Nodes (21): ActualDays, StraightDays, Fact, InlineData, Theory, ClusterSaveStateTests, Action, Fact (+13 more)

### Community 99 - "Pause Simulation Tests"
Cohesion: 0.17
Nodes (12): ChannelReader, Cts, LoopTask, Reader, Fact, ObjectInteractionStateTests, CancellationToken, CancellationTokenSource (+4 more)

### Community 100 - "Deep Space Saga Client Portraits"
Cohesion: 0.08
Nodes (7): DeepSpaceSaga.Client.UI.Screens.TempCharacterImage, DeepSpaceSaga.Client.UI.Portraits, DeepSpaceSaga.Client.Portraits, system_buffers_binary, system_diagnostics_codeanalysis, system_security_cryptography, system_text

### Community 101 - "Tactical Map Snapshot State"
Cohesion: 0.11
Nodes (26): DateTimeOffset, CancellationToken, Func, ImmutableArray, SnapshotPrediction, Task, PresentedFrame, CancellationToken (+18 more)

### Community 102 - "Dialogue Screen"
Cohesion: 0.12
Nodes (17): IScreen, Person, GameSessionHandle, Key, MouseButton, Path, ScreenEvent, SKBitmap (+9 more)

### Community 103 - "Future Trajectory Point"
Cohesion: 0.11
Nodes (17): CameraState, IReadOnlyList, List, SKPath, CombatTrajectoryProjector, Geometry, CameraState, List (+9 more)

### Community 104 - "Trading Voyage Fixture"
Cohesion: 0.09
Nodes (21): Action, Command, Func, IReadOnlyList, Item, List, Quantity, Result (+13 more)

### Community 105 - "Generic Window Type A"
Cohesion: 0.10
Nodes (19): GameMenu screen, Centralized modal pause, SKBitmap, SKCanvas, SKPaint, SKPoint, SKRect, GenericWindowTypeA (+11 more)

### Community 106 - "Game Session Handle"
Cohesion: 0.11
Nodes (19): September 12 review fixes, September 12 code review, ActiveObjectId, CancellationToken, CancellationTokenSource, Channel, Exception, SelectedObjectId (+11 more)

### Community 107 - "Station Screen"
Cohesion: 0.08
Nodes (20): Row, Text, GameSessionHandle, ImmutableArray, Key, SKCanvas, SnapshotBuffer, StationScreen (+12 more)

### Community 108 - "Station Toolbar"
Cohesion: 0.12
Nodes (12): SKBitmap, SKCanvas, SKColor, SKPaint, SKRect, StationToolbar, HasLoadedCrewImage, HasLoadedExitButtonImage (+4 more)

### Community 109 - "Station Economy Generation Tests"
Cohesion: 0.16
Nodes (10): Fact, IEnumerable, InlineData, ItemTypeId, Quantity, Registry, SimulationEngine, SpaceObjectRuntime (+2 more)

### Community 110 - "Balance Strategy Evidence"
Cohesion: 0.14
Nodes (17): BalanceCaseEvidence, BalanceQuoteEvidence, BalanceStrategyEvidence, BatchCeilingQuantity, DepartureGameTimeMs, DepartureRoute, ReservedArrivalFeeCredits, RoundTripLegs (+9 more)

### Community 111 - "Approach Pursuit Math"
Cohesion: 0.15
Nodes (12): First, ReadOnlySpan, Second, Span, ApproachFlyThroughPlan, RemainingUnits, ApproachFlyThroughPlanStep, ApproachInterceptSolution (+4 more)

### Community 112 - "IScreen Concepts"
Cohesion: 0.12
Nodes (6): Key, SKCanvas, IScreen, Action, x, y

### Community 113 - "Trading Map Geometry Tests"
Cohesion: 0.23
Nodes (9): State, Value, TradingGraphPlan, TradingMapRandom, TradingMapRngData, Fact, InlineData, Theory (+1 more)

### Community 114 - "Trade Quote Snapshot"
Cohesion: 0.12
Nodes (11): NotSupportedException, ImmutableArray, TradePriceStep, TradeQuoteRequest, TradeQuoteSnapshot, Fact, IEnumerable, JsonElement (+3 more)

### Community 115 - "Market Knowledge Tests"
Cohesion: 0.16
Nodes (9): PortFeeSnapshot, Fact, InlineData, Theory, MarketKnowledgeTests, Fact, InlineData, Theory (+1 more)

### Community 116 - "Balance Run Tests"
Cohesion: 0.12
Nodes (14): Category, Fact, ImmutableArray, InlineData, Theory, BalanceRunTests, Root, Scenario (+6 more)

### Community 117 - "Text Input Box"
Cohesion: 0.14
Nodes (14): Key, SKCanvas, SKPaint, SKRect, Stopwatch, TextInputBox, CaretVisible, Length (+6 more)

### Community 118 - "Torpedo Route"
Cohesion: 0.14
Nodes (20): ImmutableArray, CombatCommandTypes, HullCombatSnapshot, LauncherCombatSnapshot, TorpedoRoute, TorpedoRoutePhase, Complete, Straight (+12 more)

### Community 119 - "Game State Data"
Cohesion: 0.15
Nodes (17): CatalogCompatibilityData, GameStateData, MotionTimeMs, Func, IEnumerable, ImmutableArray, IReadOnlyList, ResourceFieldAsteroidData (+9 more)

### Community 120 - "Voyage Fuel Lifecycle Tests"
Cohesion: 0.25
Nodes (7): IReadOnlyList, Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageFuelLifecycleTests

### Community 121 - "Station Market Demo Content Tests"
Cohesion: 0.10
Nodes (15): ExpectedProfile, IAsyncEnumerator, CancellationToken, Fact, Func, IReadOnlyDictionary, ItemId, MemberData (+7 more)

### Community 122 - "Immutable Array Default Json Converter"
Cohesion: 0.11
Nodes (20): JsonConverter, ImmutableArray, JsonSerializerOptions, List, Type, Utf8JsonReader, Utf8JsonWriter, ImmutableArrayDefaultJsonConverter (+12 more)

### Community 123 - "Station Inventory Item Snapshot"
Cohesion: 0.13
Nodes (14): SKColor, ImmutableArray, StationInventoryItemSnapshot, StationMarketEventSnapshot, StationMarketRouteEffectSnapshot, StationMarketStockState, Normal, Shortage (+6 more)

### Community 124 - "Navigation Waypoint Math"
Cohesion: 0.17
Nodes (6): NavigationStepResult, NavigationWaypointMath, StagedNavigationStepResult, StagedNavigationStepResult, Fact, NavigationWaypointMathTests

### Community 125 - "Tactical Map View Tests"
Cohesion: 0.17
Nodes (7): Fact, InlineData, SKBitmap, SKCanvas, Theory, Scene, TacticalMapViewTests

### Community 126 - "Render Motion"
Cohesion: 0.08
Nodes (25): CachedTrajectory, Approach route implementation, September 14 tactical map scaling, September 24 GPU jitter investigation, Performance500 historical CPU profiling, September 16 x100 smoothness, GameSessionScreen UI and pipeline, Documentation index (+17 more)

### Community 127 - "Trading Graph Generator Tests"
Cohesion: 0.18
Nodes (8): TradingMapLinkData, ArgumentOutOfRangeException, Fact, HashSet, ImmutableDictionary, IReadOnlyDictionary, IReadOnlyList, TradingGraphGeneratorTests

### Community 128 - "Object Trail Geometry"
Cohesion: 0.10
Nodes (22): Ship, CameraState, End, Height, IReadOnlyList, List, SKPoint, Stack (+14 more)

### Community 129 - "Cluster Case Evidence"
Cohesion: 0.10
Nodes (20): Fact, ImmutableArray, Lazy, LongVoyageRunnerTests, ImmutableArray, JsonSerializerOptions, TextWriter, ClusterBalanceCli (+12 more)

### Community 130 - "Variant Assets"
Cohesion: 0.12
Nodes (13): FaceAssets, Dictionary, Left, Right, SKBitmap, SKPoint, SKPoint3, FaceEyeAssets (+5 more)

### Community 131 - "Menu Style"
Cohesion: 0.09
Nodes (21): SKCanvas, SKColor, SKFontStyleWeight, SKPaint, SKRect, SKTypeface, Stopwatch, MenuStyle (+13 more)

### Community 132 - "Space Object Data"
Cohesion: 0.16
Nodes (9): SpaceObjectData, From, IReadOnlyDictionary, IReadOnlyList, To, X, Y, TradingMapGeometryGenerator (+1 more)

### Community 133 - "Recording Connection"
Cohesion: 0.11
Nodes (17): CancellationToken, Fact, Func, GameSessionHandle, IAsyncEnumerable, List, RecordingConnection, Task (+9 more)

### Community 134 - "Market Health Evaluator Tests"
Cohesion: 0.23
Nodes (6): Fact, LongVoyageDiagnosticsTests, Fact, Func, ImmutableArray, MarketHealthEvaluatorTests

### Community 135 - "Station Market Profile Content Tests"
Cohesion: 0.17
Nodes (9): Fact, IEnumerable, InlineData, JsonObject, MemberData, Theory, StationMarketProfileContentTests, ProfilePath (+1 more)

### Community 136 - "Deep Space Saga Client UI"
Cohesion: 0.10
Nodes (6): DeepSpaceSaga.Client.UI.Screens.Station, DeepSpaceSaga.Client.UI.Screens.Trade, DeepSpaceSaga.Client.UI.Screens.Finance, Stopwatch, PauseResumeDiagnostics, system_globalization

### Community 137 - "Info Panel Tests"
Cohesion: 0.21
Nodes (8): predictor, buffer, Fact, InlineData, screen, SnapshotBuffer, Theory, InfoPanelTests

### Community 138 - "Async Object Image Cache"
Cohesion: 0.14
Nodes (16): CancellationTokenSource, Dictionary, Func, Queue, SemaphoreSlim, SKBitmap, Task, AsyncObjectImageCache (+8 more)

### Community 139 - "Station Market Catalog Content Tests"
Cohesion: 0.13
Nodes (14): ItemStorageKind, Cargo, FuelTank, TradeUnit, EnergyCell, Kilogram, Piece, Ration (+6 more)

### Community 140 - "Combat Bootstrap Tests"
Cohesion: 0.15
Nodes (13): Fact, InlineData, Theory, FullClusterContentTests, Fact, InlineData, Theory, CombatBootstrapTests (+5 more)

### Community 141 - "Balance Driver"
Cohesion: 0.15
Nodes (12): Action, Command, Dictionary, Func, Result, BalanceDriver, CurrentSample, CurrentSave (+4 more)

### Community 142 - "Basic Combat Ui Flow Tests"
Cohesion: 0.11
Nodes (17): Func, GameSessionHandle, InlineData, SKBitmap, SKCanvas, Task, Theory, ValueTask (+9 more)

### Community 143 - "Speed Panel Tests"
Cohesion: 0.24
Nodes (5): Fact, InlineData, Key, Theory, SpeedPanelTests

### Community 144 - "Combat Content Loader Tests"
Cohesion: 0.19
Nodes (10): Fact, InlineData, JsonNode, JsonObject, Theory, CombatContentLoaderTests, ClassesPath, ModulesPath (+2 more)

### Community 145 - "Portrait Composer"
Cohesion: 0.18
Nodes (9): ImmutableArray, SKBitmap, SKCanvas, SKRect, SKRectI, PortraitComposer, Fact, SKBitmap (+1 more)

### Community 146 - "Scenario Select Row Visuals Tests"
Cohesion: 0.24
Nodes (9): SKRect, Fact, H, SKBitmap, SKColor, W, X, Y (+1 more)

### Community 147 - "Command Result"
Cohesion: 0.17
Nodes (6): CommandResult, EconomicFixture, Fact, InlineData, Theory, CommandResultTests

### Community 148 - "Delayed Travel Connection"
Cohesion: 0.15
Nodes (11): StationTravelCommand, StationTravelResult, CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask (+3 more)

### Community 149 - "Dialogue Tests"
Cohesion: 0.24
Nodes (8): Fact, InlineData, SimulationEngine, Theory, DialogueTests, ContentPath, SimulationClock, SimulationEngine

### Community 150 - "Torpedo Command Panel Tests"
Cohesion: 0.26
Nodes (12): Fact, Fixture, GameSessionHandle, InlineData, RecordingConnection, Task, Theory, Fixture (+4 more)

### Community 151 - "Generic Button Type A"
Cohesion: 0.13
Nodes (15): MainMenu screen, Settings screen, SKBitmap, SKCanvas, SKColor, SKRect, GenericButtonTypeA, HasAssets (+7 more)

### Community 152 - "Portrait Part"
Cohesion: 0.10
Nodes (20): Dictionary, PortraitPart, BaseColor, Category, ColorChannel, ColorMask, ColorMaskVersions, DisplayName (+12 more)

### Community 153 - "Settings Screen"
Cohesion: 0.18
Nodes (8): SKCanvas, SKRect, Action, IReadOnlyList, Key, SKCanvas, SKPaint, SettingsScreen

### Community 154 - "Scenario File"
Cohesion: 0.18
Nodes (7): ScenarioFile, MigratedTradingEconomyContinuation, Fact, Func, OverflowException, SimulationEngine, MarketRevisionTests

### Community 155 - "Route Risk Integration Tests"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SimulationEngine, Theory, RouteRiskIntegrationTests

### Community 156 - "Torpedo Impact Tests"
Cohesion: 0.28
Nodes (7): Fact, Func, InlineData, Lazy, SimulationEngine, Theory, TorpedoImpactTests

### Community 157 - "Presented frame consumers and performance"
Cohesion: 0.15
Nodes (12): Presented frame consumers and performance ticket, SnapshotPrediction, AssemblyInformationalVersionAttribute, TacticalMapFrameProfile, TacticalMapFrameRecorder, TacticalMapPipelineMetrics, TacticalMapProfileCapture, TacticalMapProfileSummary (+4 more)

### Community 158 - "Window Thread Context"
Cohesion: 0.09
Nodes (15): Callback, IAsyncDisposable, SendOrPostCallback, Func, HashSet, Task, ValueTask, SessionConnectionLoader (+7 more)

### Community 159 - "Controllable Connection"
Cohesion: 0.19
Nodes (10): CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, ControllableConnection, LastSpeed (+2 more)

### Community 160 - "Grid Renderer Tests"
Cohesion: 0.19
Nodes (5): Fact, InlineData, SKBitmap, Theory, GridRendererTests

### Community 161 - "Tactical Map Spatial Index"
Cohesion: 0.17
Nodes (12): Action, Dictionary, HashSet, List, X, Y, TacticalMapBounds, TacticalMapSpatialIndex (+4 more)

### Community 162 - "Recording Connection"
Cohesion: 0.14
Nodes (14): CancellationToken, Func, IAsyncEnumerable, List, TaskCompletionSource, ValueTask, RecordingConnection, Commands (+6 more)

### Community 163 - "Orbital Synchronization Tests"
Cohesion: 0.20
Nodes (9): Fact, InlineData, Theory, GeneratedWorldPersistenceTests, Fact, InlineData, SimulationEngine, Theory (+1 more)

### Community 164 - "Image Button"
Cohesion: 0.15
Nodes (17): Hover, Size, Dictionary, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+9 more)

### Community 165 - "Character Appearance"
Cohesion: 0.12
Nodes (11): ImmutableSortedDictionary, CharacterAppearance, Age, Colors, Gender, LibraryVersion, Parts, Race (+3 more)

### Community 166 - "Combat Impact Snapshot"
Cohesion: 0.12
Nodes (15): CombatEffect, TerminalTrail, CombatImpactSnapshot, TorpedoTerminationKind, Impact, Intercept, SelfDestruct, AuthoritativeSnapshot (+7 more)

### Community 167 - "Space Map Color Resolver Tests"
Cohesion: 0.22
Nodes (4): Fact, SKColor, SpaceMapColorResolverTests, Fallback

### Community 168 - "Tactical Map Settings"
Cohesion: 0.09
Nodes (20): TacticalMapSettings, ClusterCellPixels, ClusterHysteresis, ClusterPpu, CompactMarkerPpu, FitPaddingPixels, GridBaseCellPixels, GridFadePixels (+12 more)

### Community 169 - "Station Event Runtime"
Cohesion: 0.19
Nodes (9): TradeCategory, StationEventData, StationEventPriceFactorData, StationMarketEventItemEffectData, StationMarketEventRouteEffectData, StationEventPriceFactorRuntime, StationEventRuntime, ImmutableArray (+1 more)

### Community 170 - "Weapon Operator Schema Tests"
Cohesion: 0.17
Nodes (9): ShipCrewMemberData, Fact, InlineData, Theory, WeaponOperatorRuntimeTests, Fact, InlineData, Theory (+1 more)

### Community 171 - "Engine Trade Connection"
Cohesion: 0.16
Nodes (7): CancellationToken, IAsyncEnumerable, List, ValueTask, EngineTradeConnection, Commands, SpeedChanges

### Community 172 - "Strategy Balance Evaluator Tests"
Cohesion: 0.31
Nodes (5): Fact, ImmutableArray, InlineData, Theory, StrategyBalanceEvaluatorTests

### Community 173 - "Approach Command Tests"
Cohesion: 0.32
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, ApproachCommandTests

### Community 174 - "Combat Run"
Cohesion: 0.23
Nodes (8): Fact, InlineData, IReadOnlyList, SimulationEngine, Theory, BasicCombatEndToEndTests, CombatRun, Source

### Community 175 - "Save Slot Repository Tests"
Cohesion: 0.18
Nodes (4): SaveSlotNaming, SaveSlotRepository, Fact, SaveSlotRepositoryTests

### Community 176 - "Combat Visual Settings"
Cohesion: 0.15
Nodes (15): SKColor, CombatVisualSettings, Countermeasure, CountermeasureIntercept, CountermeasurePrediction, CountermeasureTrail, Default, DefenseRange (+7 more)

### Community 177 - "Combat Journal Entry"
Cohesion: 0.20
Nodes (12): ImmutableArray, IReadOnlyList, List, SKCanvas, SKRect, CombatJournalPanel, Bounds, Entries (+4 more)

### Community 178 - "Torpedo Guidance Tests"
Cohesion: 0.18
Nodes (10): Direction, X, Y, TorpedoGuidanceMath, InlineData, Theory, Fact, InlineData (+2 more)

### Community 179 - "Navigation Trajectory Projector"
Cohesion: 0.14
Nodes (10): IReadOnlyList, List, X, Y, ClosestX, ClosestY, IsArrived, ClosestX (+2 more)

### Community 180 - "Combat Save Load Tests"
Cohesion: 0.27
Nodes (6): Fact, InlineData, SimulationEngine, Theory, CombatSaveLoadTests, Settings

### Community 181 - "Trade Journal"
Cohesion: 0.14
Nodes (15): DisplayEntry, Entry, Dictionary, Id, IReadOnlyList, List, SnapshotBuffer, DisplayEntry (+7 more)

### Community 182 - "Countermeasure Snapshot"
Cohesion: 0.14
Nodes (18): Basic torpedo combat evidence, Countermeasure combat implementation, Countermeasure Graphify rebuild report, Data, ImmutableArray, CountermeasurePhase, Guiding, MissedCoast (+10 more)

### Community 183 - "Localization Tests"
Cohesion: 0.21
Nodes (8): Placeholders, InlineData, Theory, Fact, InlineData, Key, Theory, LocalizationTests

### Community 184 - "Dialogue Command"
Cohesion: 0.17
Nodes (10): DialogueAction, Abort, Choose, DialogueCommand, CancellationToken, IAsyncEnumerable, List, ValueTask (+2 more)

### Community 185 - "Collision Path"
Cohesion: 0.17
Nodes (12): InputFocus, None, Quantity, Search, Func, X, Y, CollisionPath (+4 more)

### Community 186 - "Station Resource Field Content Tests"
Cohesion: 0.25
Nodes (5): Fact, JsonNode, Random, ContentFixture, StationResourceFieldContentTests

### Community 187 - "Trade Snapshot Projection Tests"
Cohesion: 0.22
Nodes (6): Fact, IEnumerable, ItemTypeId, Quantity, SimulationEngine, TradeSnapshotProjectionTests

### Community 188 - "Voyage Ledger Lifecycle Tests"
Cohesion: 0.21
Nodes (7): ArgumentException, Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageLedgerLifecycleTests

### Community 189 - "August 9 small technical tasks"
Cohesion: 0.15
Nodes (18): Implementation requirement discrepancy tasks, August 9 small technical tasks, InterfaceLog, SKColor, SpaceMapColorResolver, CommandResultStatus, Cancelled, Deferred (+10 more)

### Community 190 - "Ship Event"
Cohesion: 0.16
Nodes (8): Dictionary, Queue, SessionEventHistory, ShipEvent, ShipEventReasonCodes, ShipEventTypes, Fact, ShipEventTests

### Community 191 - "Station Screen"
Cohesion: 0.32
Nodes (3): SKCanvas, SKCanvas, SKCanvas

### Community 192 - "Map Frame Evidence"
Cohesion: 0.13
Nodes (13): AssemblyInformationalVersionAttribute, HashSet, IEnumerable, List, FrameStatistics, MapFrameContext, MapFrameEvidence, Completed (+5 more)

### Community 193 - "Command Definition Loader Directory Tests"
Cohesion: 0.27
Nodes (4): Fact, CommandDefinitionLoaderDirectoryTests, Fact, CommandDefinitionLoaderTests

### Community 194 - "Repeatable Trading Voyage Ui Tests"
Cohesion: 0.16
Nodes (11): CancellationToken, IAsyncEnumerable, List, TaskCompletionSource, ValueTask, Connection, Commands, CurrentSnapshot (+3 more)

### Community 195 - "Automatic Defense Tests"
Cohesion: 0.19
Nodes (8): Fact, AutomaticDefenseTests, Fact, CombatJournalTests, Fact, InlineData, Theory, CountermeasureResolutionTests

### Community 196 - "Cargo Cost Accounting Tests"
Cohesion: 0.27
Nodes (7): Command, InlineData, Result, SimulationEngine, Theory, CargoCostAccountingTests, Func

### Community 197 - "Countermeasure Rng"
Cohesion: 0.14
Nodes (9): DeepSpaceSaga.Engine.Combat, CountermeasureRng, Counter, State, InterceptionMath, Fact, InlineData, Theory (+1 more)

### Community 198 - "Quote Terms"
Cohesion: 0.17
Nodes (7): QuoteTerms, StationPriceFactorSource, ImmutableArray, QuoteContext, QuoteTerms, StationPriceFactorSource, TradeTarget

### Community 199 - "Game Time Display"
Cohesion: 0.16
Nodes (9): GameTimeDisplay, GameCalendar, Fact, InlineData, Theory, GameTimeDisplayTests, InlineData, Theory (+1 more)

### Community 200 - "Progress Bar Tests"
Cohesion: 0.20
Nodes (9): SKCanvas, SKColor, SKPaint, SKRect, ProgressBar, Fact, InlineData, Theory (+1 more)

### Community 201 - "Tactical Map Revision"
Cohesion: 0.13
Nodes (14): TacticalMapRevision, All, Camera, None, Pose, Route, Selection, Settings (+6 more)

### Community 202 - "Balance Hourly Sample"
Cohesion: 0.27
Nodes (10): ImmutableArray, BalanceCanonical, BalanceEvent, BalanceFlow, BalanceHourlySample, BalanceMatrix, BalanceRoute, BalanceShipConfiguration (+2 more)

### Community 203 - "Combat Session Fixture"
Cohesion: 0.17
Nodes (10): Fact, Task, ValueTask, CombatSessionFixture, Connection, DirectoryPath, Engine, Restored (+2 more)

### Community 204 - "Orbital Docking Departure Tests"
Cohesion: 0.20
Nodes (9): Fact, InlineData, Theory, EpicSolarIntegrationTests, Fact, InlineData, SimulationEngine, Theory (+1 more)

### Community 205 - "Voyage Fuel Accounting Tests"
Cohesion: 0.26
Nodes (6): IReadOnlyList, Fact, InlineData, SimulationEngine, Theory, VoyageFuelAccountingTests

### Community 206 - "Voyage Profit Accounting Tests"
Cohesion: 0.32
Nodes (4): Fact, InlineData, Theory, VoyageProfitAccountingTests

### Community 207 - "Installed Module Snapshot"
Cohesion: 0.24
Nodes (6): CargoStackSnapshot, ImmutableArray, InstalledModuleSnapshot, ModuleCommandSnapshot, Fact, InstalledModuleSnapshotTests

### Community 208 - "Settings Layout"
Cohesion: 0.27
Nodes (3): SKRect, SettingsLayout, MouseButton

### Community 209 - "Game Session Docking Request Tests"
Cohesion: 0.22
Nodes (8): Fact, GameSessionHandle, ImmutableArray, RecordingConnection, Task, TestFixture, GameSessionDockingRequestTests, TestFixture

### Community 210 - "Recording Connection"
Cohesion: 0.20
Nodes (8): CancellationToken, Func, IAsyncEnumerable, List, ValueTask, RecordingConnection, Commands, OnSend

### Community 212 - "Dialogue Layout"
Cohesion: 0.31
Nodes (5): SKRect, DialogueLayout, SKCanvas, SKPaint, SKRect

### Community 213 - "Tactical Map Marker Policy Tests"
Cohesion: 0.27
Nodes (5): TacticalMapMarkerPolicy, Fact, InlineData, Theory, TacticalMapMarkerPolicyTests

### Community 214 - "Station Layout"
Cohesion: 0.45
Nodes (5): Bottom, Left, Right, Top, StationLayout

### Community 215 - "Trading Economy Save Schema Tests"
Cohesion: 0.28
Nodes (5): TradingEconomySaveMigration, Fact, InlineData, Theory, TradingEconomySaveSchemaTests

### Community 216 - "Game Session Scale Filter Tests"
Cohesion: 0.29
Nodes (6): Fact, HashSet, InlineData, SnapshotBuffer, Theory, GameSessionScaleFilterTests

### Community 217 - "Game Session Ui Scale Tests"
Cohesion: 0.31
Nodes (5): Fact, InlineData, SnapshotBuffer, Theory, GameSessionUiScaleTests

### Community 218 - "Voyage Save Load Continuity Tests"
Cohesion: 0.36
Nodes (4): Fact, InlineData, Theory, VoyageSaveLoadContinuityTests

### Community 219 - "Whole Face Assets"
Cohesion: 0.24
Nodes (5): SKBitmap, SKColor, SKPoint3, SKRectI, WholeFaceAssets

### Community 220 - "Whole Head Assets"
Cohesion: 0.22
Nodes (10): Dialogue portraits, Female face proportions, Legacy modular portrait generator, M4 male portraits, W1 whole-face portraits, W2 whole-head portraits, W4 whole portraits, FaceTextures (+2 more)

### Community 221 - "Station Market Event Content Tests"
Cohesion: 0.15
Nodes (12): Effects, Max, Min, Priority, Profiles, StationMarketEventRouteEffectDefinition, Chance, Dictionary (+4 more)

### Community 222 - "Portrait Style Profile"
Cohesion: 0.13
Nodes (15): PortraitAnchor, PortraitLayer, PortraitStyleProfile, Anchors, Gender, Height, Layers, LibraryVersion (+7 more)

### Community 223 - "Deep Space Saga Contracts csproj"
Cohesion: 0.17
Nodes (10): net8.0, Microsoft.NET.Sdk, net8.0, Microsoft.NET.Sdk, net8.0, Microsoft.NET.Sdk, net8.0, Microsoft.NET.Sdk (+2 more)

### Community 224 - "Trading Map Data Validation"
Cohesion: 0.31
Nodes (5): From, IReadOnlyDictionary, IReadOnlyList, To, TradingMapDataValidation

### Community 225 - "Defense Command Panel Tests"
Cohesion: 0.18
Nodes (10): Fact, Func, GameSessionHandle, RecordingConnection, Task, DefenseCommandPanelTests, Fixture, Connection (+2 more)

### Community 227 - "Torpedo Launcher Content Tests"
Cohesion: 0.27
Nodes (6): Fact, InlineData, Theory, TemporaryContent, Path, TorpedoLauncherContentTests

### Community 228 - "Combat Wreck Tests"
Cohesion: 0.28
Nodes (5): Fact, InlineData, SimulationEngine, Theory, CombatWreckTests

### Community 229 - "Torpedo History Tests"
Cohesion: 0.25
Nodes (8): Direction, Fact, ImmutableArray, InlineData, Theory, X, Y, TorpedoHistoryTests

### Community 230 - "Trading Economy Continuity Tests"
Cohesion: 0.34
Nodes (4): Fact, InlineData, Theory, TradingEconomyContinuityTests

### Community 231 - "Revision driven scene cache ticket"
Cohesion: 0.18
Nodes (14): Tactical map render pipeline story, Revision-driven scene cache ticket, Bounded revision dependencies and spatial fallback, Documentation and graph story, Documentation sync ticket, Graph rebuild ticket, Functional epic self-review, EP-0005 publication registry (+6 more)

### Community 232 - "Tetrarch Class starting ship"
Cohesion: 0.22
Nodes (14): Crew dialogues, Electricity and Energy Cells, Engine fuel, Ice mining, Drilling Unit mining cycle, Passenger cabin capacity gate, Passenger contracts, Station representative dialogues (+6 more)

### Community 233 - "Reference Feature Assets"
Cohesion: 0.26
Nodes (4): Landmarks, SKBitmap, Landmarks, ReferenceFeatureAssets

### Community 234 - "Ui Asset Loader Tests"
Cohesion: 0.21
Nodes (9): Dictionary, SKBitmap, UiAssetLoader, Fact, MemberData, Theory, TheoryData, UiAssetLoaderTests (+1 more)

### Community 235 - "Approach Camera Integration Tests"
Cohesion: 0.18
Nodes (7): BufferedSnapshot, IReadOnlyList, Label, LabelWidth, Lines, List, Value

### Community 236 - "Voyage Snapshot"
Cohesion: 0.21
Nodes (8): ImmutableArray, VoyageRouteOptionSnapshot, VoyageSnapshot, State, Fact, InlineData, Theory, VoyageSnapshotTests

### Community 237 - "Recording Connection"
Cohesion: 0.26
Nodes (7): CancellationToken, IAsyncEnumerable, List, ValueTask, RecordingConnection, Commands, OnSend

### Community 238 - "Recording Connection"
Cohesion: 0.25
Nodes (6): CancellationToken, IAsyncEnumerable, List, ValueTask, RecordingConnection, Commands

### Community 239 - "Nine Patch Tests"
Cohesion: 0.31
Nodes (5): Fact, SKBitmap, SKColor, SKRect, NinePatchTests

### Community 240 - "Tactical Map Cluster Stability Tests"
Cohesion: 0.20
Nodes (7): ArgumentException, Fact, InlineData, SKSurface, Theory, Scene, TacticalMapClusterStabilityTests

### Community 242 - "Market Save Load Continuity Tests"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SimulationEngine, Theory, MarketSaveLoadContinuityTests

### Community 243 - "Person Sex"
Cohesion: 0.21
Nodes (8): DialoguePortraitComposer, PersonSex, Female, Male, Fact, InlineData, Theory, DialoguePortraitComposerTests

### Community 244 - "Countermeasure Content Loader Tests"
Cohesion: 0.27
Nodes (5): Fact, InlineData, JsonNode, Theory, CountermeasureContentLoaderTests

### Community 245 - "Recording Connection"
Cohesion: 0.28
Nodes (6): CancellationToken, IAsyncEnumerable, List, ValueTask, RecordingConnection, Commands

### Community 246 - "Temporary Content"
Cohesion: 0.23
Nodes (7): Fact, InlineData, Theory, TemporaryContent, Root, SettingsPath, TetrarchClassContentTests

### Community 247 - "Torpedo Inspection Tests"
Cohesion: 0.28
Nodes (6): CancellationToken, IAsyncEnumerable, List, ValueTask, Connection, Commands

### Community 248 - "Countermeasure Bootstrap Tests"
Cohesion: 0.24
Nodes (6): Fact, CountermeasureBootstrapTests, Fact, InlineData, Theory, CountermeasureEndToEndTests

### Community 249 - "Portrait Generator Tests"
Cohesion: 0.24
Nodes (7): IDisposable, Fact, InvalidDataException, Lazy, PortraitGeneratorTests, TemporaryPack, Root

### Community 250 - "Xenon Style"
Cohesion: 0.30
Nodes (7): SKPaintStyle, SKColor, SKFontStyleWeight, SKPaint, SKTextAlign, SKTypeface, XenonStyle

### Community 251 - "Asteroid Survey Snapshot"
Cohesion: 0.30
Nodes (7): ImmutableArray, AsteroidSurveySnapshot, ResourceFractionSnapshot, Fact, InlineData, Theory, ResourceSurveySnapshotTests

### Community 252 - "Combat State Data"
Cohesion: 0.24
Nodes (8): IReadOnlyList, CombatStateData, LauncherSaveData, ProjectileSaveData, Dictionary, List, ModuleId, ObjectId

### Community 253 - "Object Label Layout"
Cohesion: 0.38
Nodes (5): Important label placement, SKPoint, SKRect, SKSize, ObjectLabelLayout

### Community 254 - "Station Market Knowledge Snapshot"
Cohesion: 0.31
Nodes (6): ImmutableArray, StationMarketKnowledgeSnapshot, Fact, InlineData, Theory, StationMarketKnowledgeSnapshotTests

### Community 255 - "Station Size Factors Tests"
Cohesion: 0.36
Nodes (4): Fact, InlineData, Theory, StationSizeFactorsTests

### Community 256 - "Approach Camera Integration Tests"
Cohesion: 0.36
Nodes (4): CancellationToken, IAsyncEnumerable, ValueTask, Connection

### Community 257 - "Game Session Dock Auto Open"
Cohesion: 0.47
Nodes (3): Fact, SnapshotBuffer, GameSessionDockAutoOpenTests

### Community 258 - "Panel Engine Connection"
Cohesion: 0.36
Nodes (4): CancellationToken, IAsyncEnumerable, ValueTask, PanelEngineConnection

### Community 259 - "Legacy Connection"
Cohesion: 0.36
Nodes (4): CancellationToken, IAsyncEnumerable, ValueTask, LegacyConnection

### Community 260 - "Ration Schedule Tests"
Cohesion: 0.40
Nodes (3): Fact, SimulationEngine, RationScheduleTests

### Community 261 - "Object Image Resolution Tests"
Cohesion: 0.40
Nodes (3): Fact, SimulationEngine, ObjectImageResolutionTests

### Community 262 - "Localization Concepts"
Cohesion: 0.24
Nodes (6): LocaleState, Dictionary, LocaleState, Localization, CurrentLanguage, Revision

### Community 263 - "Grid Renderer"
Cohesion: 0.33
Nodes (5): IReadOnlyList, SKCanvas, SKColor, SKPaint, GridRenderer

### Community 264 - "Game Session Screen Trail Color"
Cohesion: 0.42
Nodes (3): SKColor, Fact, GameSessionScreenTrailColorTests

### Community 265 - "Dialogue Integration Tests"
Cohesion: 0.29
Nodes (6): Fact, Func, GameSessionHandle, Task, ValueTask, DialogueIntegrationTests

### Community 266 - "Status Square Animator"
Cohesion: 0.36
Nodes (3): StatusSquareAnimator, InlineData, Theory

### Community 267 - "Combat Event Type"
Cohesion: 0.20
Nodes (9): CombatEventType, Destroyed, Expired, Hit, Intercept, Launch, Miss, SelfDestruct (+1 more)

### Community 269 - "Settings Screen Tests"
Cohesion: 0.36
Nodes (3): Fact, SKBitmap, SettingsScreenTests

### Community 270 - "Unified Portrait Tests"
Cohesion: 0.29
Nodes (5): Fact, Lazy, TemporaryPack, Root, UnifiedPortraitTests

### Community 271 - "Port Fee Schedule Tests"
Cohesion: 0.42
Nodes (3): Fact, SimulationEngine, PortFeeScheduleTests

### Community 272 - "Hair Assets"
Cohesion: 0.33
Nodes (4): SKBitmap, SKPath, SKPoint, HairAssets

### Community 273 - "Deep Space Saga Client csproj"
Cohesion: 0.22
Nodes (7): Silk.NET.Input, Silk.NET.OpenGL, Silk.NET.Windowing, SkiaSharp, net8.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk

### Community 275 - "Station District"
Cohesion: 0.22
Nodes (8): StationDistrict, Administration, Dock, Market, ImmutableArray, TimedContractState, IReadOnlyList, EconomyTimeData

### Community 276 - "Tetrarch Loadout Tests"
Cohesion: 0.50
Nodes (3): InlineData, Theory, TetrarchLoadoutTests

### Community 277 - "Voyage Finance Contract Tests"
Cohesion: 0.36
Nodes (4): Fact, InlineData, Theory, VoyageFinanceContractTests

### Community 278 - "Voyage Fuel Contract Tests"
Cohesion: 0.31
Nodes (4): Fact, InlineData, Theory, VoyageFuelContractTests

### Community 279 - "Trading Continuation Review Tests"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, TradingContinuationReviewTests

### Community 280 - "Voyage Lifecycle Tests"
Cohesion: 0.39
Nodes (3): Fact, SimulationEngine, VoyageLifecycleTests

### Community 281 - "Economy Balance Seam Tests"
Cohesion: 0.39
Nodes (4): InternalsVisibleToAttribute, Fact, SimulationEngine, EconomyBalanceSeamTests

### Community 285 - "Content Fixture"
Cohesion: 0.29
Nodes (5): JsonNode, ContentFixture, ScenarioPath, Settings, SettingsPath

### Community 286 - "Deep Space Saga Performance Tests"
Cohesion: 0.25
Nodes (6): Microsoft.NET.Sdk, net8.0, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 288 - "Countermeasure Ui Flow Tests"
Cohesion: 0.29
Nodes (5): deepspacesaga_client_tests_basiccombatuiflowtests_fixture, InlineData, Task, Theory, CountermeasureUiFlowTests

### Community 289 - "Nine Patch"
Cohesion: 0.52
Nodes (5): SKBitmap, SKCanvas, SKPaint, SKRect, NinePatch

### Community 290 - "Deep Space Saga Client Tests"
Cohesion: 0.29
Nodes (6): net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 291 - "Main Menu Screen Tests"
Cohesion: 0.33
Nodes (4): Fact, InlineData, Theory, MainMenuScreenTests

### Community 292 - "Scenario Select Screen Tests"
Cohesion: 0.33
Nodes (4): Fact, InlineData, Theory, ScenarioSelectScreenTests

### Community 293 - "Deep Space Saga Contracts Tests"
Cohesion: 0.29
Nodes (6): net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 294 - "Deep Space Saga Engine Tests"
Cohesion: 0.29
Nodes (6): net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 295 - "Deep Space Saga Motion Tests"
Cohesion: 0.29
Nodes (6): net8.0, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 296 - "Game Info"
Cohesion: 0.40
Nodes (5): Data, Lazy, GameInfo, Title, Version

### Community 297 - "Tactical Map Animation Kind"
Cohesion: 0.33
Nodes (5): TacticalMapAnimationKind, Active, EngineFlame, Selection, Status

### Community 298 - "Settings Button"
Cohesion: 0.33
Nodes (6): SettingsButton, Exit, LanguageCombo, MonitorCombo, None, UiScaleCombo

### Community 300 - "Countermeasure Termination Kind"
Cohesion: 0.33
Nodes (6): CountermeasureTerminationKind, Intercept, LifetimeExpired, MissedCoastExpired, OwnerLost, TargetLost

### Community 301 - "Countermeasure State Data"
Cohesion: 0.47
Nodes (4): IReadOnlyList, CountermeasureProjectileSaveData, CountermeasureStateData, DefenseLauncherSaveData

### Community 302 - "Countermeasure Content Tests"
Cohesion: 0.60
Nodes (3): InlineData, Theory, CountermeasureContentTests

### Community 303 - "Default System Content Tests"
Cohesion: 0.40
Nodes (4): Fact, Task, DefaultSystemContentTests, Settings

### Community 304 - "Local Cluster Content Tests"
Cohesion: 0.33
Nodes (4): Fact, InlineData, Theory, LocalClusterContentTests

### Community 305 - "Countermeasure Event Tests"
Cohesion: 0.40
Nodes (4): AuthoritativeSnapshot, CombatJournalEntry, Fact, CountermeasureEventTests

### Community 306 - "Deep Space Saga Economy Balance"
Cohesion: 0.33
Nodes (5): coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 307 - "Cluster Voyage Integration Tests"
Cohesion: 0.60
Nodes (3): InlineData, Theory, ClusterVoyageIntegrationTests

### Community 308 - "Xenon Combo Box"
Cohesion: 0.40
Nodes (4): SKBitmap, SKPaint, XenonComboBox, HasAssets

### Community 309 - "Navigation Phase"
Cohesion: 0.40
Nodes (4): NavigationPhase, Approach, EscapeDepart, EscapeTurn

### Community 312 - "Save Reliability Tests"
Cohesion: 0.60
Nodes (3): Fact, Task, SaveReliabilityTests

### Community 313 - "System Performance Report Tests"
Cohesion: 0.50
Nodes (3): Fact, SystemPerformanceReportTests, Root

### Community 315 - "Entry Concepts"
Cohesion: 0.50
Nodes (4): Entry, ConfirmedReceipt, HasKnownCargoResult, ResultMatchesBinding

### Community 316 - "Voyage Amount Kind"
Cohesion: 0.50
Nodes (4): VoyageAmountKind, EventCost, PassengerPayout, PassengerPenalty

### Community 322 - "Object Info Panel State"
Cohesion: 0.67
Nodes (3): ObjectInfoPanelState, Closed, Open

### Community 324 - "Launch Mode"
Cohesion: 0.67
Nodes (3): LaunchMode, Auto, Manual

## Ambiguous Edges - Review These
- `Canonical developer guide` → `Current local 1 Hz snapshot baseline`  [AMBIGUOUS]
  Documentation/00-Process/CLAUDE.md · relation: conceptually_related_to
- `Canonical Engine requirements` → `First release requirements`  [AMBIGUOUS]
  Documentation/01-Requirements/FirstReleaseRequirements.md · relation: conceptually_related_to
- `Load screen` → `Screen catalog`  [AMBIGUOUS]
  Documentation/02-FirstRelease/Screens/ScreenCatalog.md · relation: conceptually_related_to
- `Screen catalog` → `Ship screen`  [AMBIGUOUS]
  Documentation/02-FirstRelease/Screens/ScreenCatalog.md · relation: conceptually_related_to

## Knowledge Gaps
- **912 isolated node(s):** `PredictionDeltaMs`, `net8.0`, `Silk.NET.Input`, `Silk.NET.OpenGL`, `Silk.NET.Windowing` (+907 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2242 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **23 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Canonical developer guide` and `Current local 1 Hz snapshot baseline`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Canonical Engine requirements` and `First release requirements`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Load screen` and `Screen catalog`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Screen catalog` and `Ship screen`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `GameSessionScreen` connect `Game Session Rendering` to `Object Trail Geometry`, `Client Graphics Dependencies`, `Game Session Dock Auto Open`, `Authoritative Snapshot Prediction`, `Simulation Speed Label Layout`, `Recording Connection`, `Grid Renderer`, `Game Session Screen Trail Color`, `Dialogue Integration Tests`, `Command Panel Tests`, `Info Panel Tests`, `Local Session Integration`, `Solar System Map Snapshot`, `Basic Combat Ui Flow Tests`, `Tactical Map Depth Renderer`, `Game Session Object Interaction Tests`, `Skia Window`, `Tactical Pipeline Evidence`, `Screen Event Tests`, `Speed Panel Tests`, `Torpedo Command Panel Tests`, `Generic Button Type A`, `Canonical Engine requirements`, `Presented frame consumers and performance`, `Window Thread Context`, `Object Motion Snapshot`, `Map View Geometry Tests`, `Linear Motion Predictor`, `Object Render State`, `Camera State`, `Object Trail Store`, `Combat Visual Settings`, `Screen Event`, `Combat Journal Entry`, `Torpedo Guidance Tests`, `Combat Trajectory Tests`, `Screen Stack`, `Navigation Trajectory Projector`, `August 9 small technical tasks`, `Simulation Clock`, `Tactical Map Revision`, `Object Info Panel`, `Combat Effect Store`, `Tactical Map State Updater`, `Game Session Docking Request Tests`, `Torpedo Preview Tests`, `Object Info Panel Tests`, `Cluster Map Presentation`, `Game Session Scale Filter Tests`, `Game Session Ui Scale Tests`, `Game Session Scale Panel Tests`, `Object Trail Buffer`, `Defense Command Panel Tests`, `Mechanics Panel Tests`, `Tactical Map Snapshot State`, `Dialogue Screen`, `Revision driven scene cache ticket`, `Tetrarch Class starting ship`, `Generic Window Type A`, `Future Trajectory Point`, `Approach Camera Integration Tests`, `IScreen Concepts`, `Tactical Map Cluster Stability Tests`, `Tactical Map View Tests`, `Torpedo Route`, `Portrait Generator Tests`, `Object Label Layout`, `Render Motion`?**
  _High betweenness centrality (0.200) - this node is a cross-community bridge._
- **Why does `SimulationEngine` connect `Simulation Engine Economy` to `Approach Camera Integration Tests`, `Engine Scenario Content`, `Panel Engine Connection`, `Space Object Data`, `Simulation Speed Label Layout`, `Economy Time Continuity`, `Scenario Market Content Loading`, `Shared Contract Serialization`, `Local Session Integration`, `Combat Bootstrap Tests`, `Solar System Map Snapshot`, `Scenario Engine Tests`, `Balance Driver`, `Pirate Scenario Tests`, `Command Result`, `Station District`, `Dialogue Tests`, `Delayed Travel Connection`, `Finance Screen`, `Canonical Engine requirements`, `Scenario Exception`, `Trade Screen Tests`, `Game Data Registry`, `Snapshot Render Metadata Tests`, `Local Cluster Generation Tests`, `Space Object Runtime`, `Object Motion Snapshot`, `Map View Geometry Tests`, `Orbital Synchronization Tests`, `Combat Impact Snapshot`, `Dialogue State`, `Station Event Runtime`, `Linear Motion Predictor`, `Engine Trade Connection`, `Countermeasure State Data`, `Combat Journal Entry`, `Installed Module Runtime`, `Dialogue Command`, `Save Reliability Tests`, `Station Resource Field Content Tests`, `Trading Map Bootstrap Tests`, `Voyage Amount Kind`, `August 9 small technical tasks`, `Ship Event`, `Trading Map Schema Tests`, `Resource Field Random`, `Route Risk Content Tests`, `Player Command`, `Simulation Clock`, `Countermeasure Rng`, `Voyage State Data`, `Trading Map State Data`, `Quote Terms`, `Balance Hourly Sample`, `Combat Session Fixture`, `Trading Economy Continuation Data`, `Torpedo Preview Tests`, `Deep Space Saga Economy Balance`, `Cargo Stack Data`, `Countermeasure Bootstrap Tests`, `Pause Simulation Tests`, `Deep Space Saga Client Portraits`, `Trading Map Geometry Tests`, `Trade Quote Snapshot`, `Market Knowledge Tests`, `Balance Run Tests`, `Torpedo Route`, `Game State Data`, `Voyage Fuel Lifecycle Tests`, `Portrait Generator Tests`, `Combat State Data`, `Station Market Knowledge Snapshot`?**
  _High betweenness centrality (0.196) - this node is a cross-community bridge._
- **Why does `AuthoritativeSnapshot` connect `Authoritative Snapshot Prediction` to `Game Session Rendering`, `Simulation Engine Economy`, `Simulation Speed Label Layout`, `Economy Time Continuity`, `Scenario Market Content Loading`, `Local Session Integration`, `Command Panel Tests`, `Scenario Engine Tests`, `Solar System Map Snapshot`, `Game Session Object Interaction Tests`, `Trade Screen Tests`, `Engine Command Tests`, `Trade Ux Tests`, `Finance Screen`, `Canonical Engine requirements`, `Object Motion Snapshot`, `Map View Geometry Tests`, `Station Toolbar Tests`, `Dialogue State`, `Linear Motion Predictor`, `Trade Model`, `Object Trail Store`, `Combat Trajectory Tests`, `Trading Map Bootstrap Tests`, `Snapshot Mailbox`, `Hire Screen`, `Station Screen Tests`, `Route Risk Content Tests`, `Simulation Clock`, `Voyage State Data`, `Voyage Ui Fixture`, `Combat Effect Store`, `Tactical Map State Updater`, `Torpedo Preview Tests`, `Controlled Connection`, `Deep Space Saga Economy Balance`, `Contracts Screen`, `Cargo Stack Data`, `Object Info Panel Tests`, `Cluster Map Presentation`, `Cluster Voyage Fixture`, `Pause Simulation Tests`, `Tactical Map Snapshot State`, `Dialogue Screen`, `Trading Voyage Fixture`, `Station Screen`, `Station Toolbar`, `Market Knowledge Tests`, `Torpedo Route`, `Voyage Fuel Lifecycle Tests`, `Station Market Demo Content Tests`, `Station Inventory Item Snapshot`, `Tactical Map View Tests`, `Recording Connection`, `Info Panel Tests`, `Combat Bootstrap Tests`, `Balance Driver`, `Basic Combat Ui Flow Tests`, `Speed Panel Tests`, `Command Result`, `Delayed Travel Connection`, `Dialogue Tests`, `Route Risk Integration Tests`, `Torpedo Impact Tests`, `Controllable Connection`, `Recording Connection`, `Orbital Synchronization Tests`, `Combat Impact Snapshot`, `Engine Trade Connection`, `Approach Command Tests`, `Combat Run`, `Combat Journal Entry`, `Combat Save Load Tests`, `Dialogue Command`, `August 9 small technical tasks`, `Ship Event`, `Station Screen`, `Repeatable Trading Voyage Ui Tests`, `Combat Session Fixture`, `Orbital Docking Departure Tests`, `Installed Module Snapshot`, `Recording Connection`, `Dock Command Tests`, `Game Session Scale Filter Tests`, `Defense Command Panel Tests`, `Combat Wreck Tests`, `Torpedo History Tests`, `Approach Camera Integration Tests`, `Voyage Snapshot`, `Recording Connection`, `Recording Connection`, `Smoke Tests`, `Recording Connection`, `Torpedo Inspection Tests`, `Station Market Knowledge Snapshot`, `Approach Camera Integration Tests`, `Game Session Dock Auto Open`, `Panel Engine Connection`, `Legacy Connection`, `Ration Schedule Tests`, `Dialogue Integration Tests`, `Pirate Scenario Tests`, `Station District`, `Voyage Finance Contract Tests`, `Voyage Fuel Contract Tests`, `Snapshot Render Metadata Tests`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._