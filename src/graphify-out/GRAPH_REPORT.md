# DSS fresh structural and EP-0010 semantic graph

Fresh AST covers every selected .cs/.py file. Markdown headings and links are structural evidence. Fourteen curated EP-0010 mechanisms provide selective semantic coverage. Historical EP1-4 semantic fragments were replaced rather than silently reused. Token usage is unmeasured: template zeroes below are not measurements. No external API, game or native harness is run by this builder. Graph navigation does not establish runtime, FPS or human acceptance. HTML is an aggregated community view; JSON contains the complete graph. Recorded commit is pre-export HEAD; input hashes describe actual working-tree sources.

Reproduce from PowerShell: `& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe D:/DeepSpaceSaga/DSS/src/graphify-out/rebuild.py --force`. The explicit shrink override replaces historical semantic scope; inspect `semantic-coverage.json` before using it. `--dry-run` validates current source locations without exporting.

Audit: 1027 unresolved AST endpoint edges were explicitly pruned; 2606 reference-only symbols have no local declaration location. The undirected export combines some relations and canonicalizes equivalent nodes; extraction.json retains all extracted relations. See diagnostics.json and the dedicated audit sidecars before interpreting absence of a path.

# Graph Report - DSS  (2026-10-18)

## Corpus Check
- 878 files · ~702,623 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 13620 nodes · 47142 edges · 419 communities (398 shown, 21 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 5238 edges (avg confidence: 0.83)
- Token cost: unmeasured host-agent usage (see cost.json)

## Graph Freshness
- Built from commit: `246987bb`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Rebuild this selected corpus with `rebuild.py`; AST/Markdown extraction is offline and curated semantic evidence tokens are revalidated.

## Community Hubs (Navigation)
- Client interface 0
- Engine simulation 1
- Gameplay requirements 2
- Engine verification 3
- Engine verification 4
- Engine verification 5
- Engine simulation 6
- Client verification 7
- Engine simulation 8
- Engine verification 9
- Client verification 10
- Client verification 11
- Engine simulation 12
- Client interface 13
- Client interface 14
- Engine verification 15
- Client verification 16
- Motion.Tests 17
- Client verification 18
- Client interface 19
- Engine verification 20
- Engine verification 21
- Client interface 22
- Client verification 23
- Client verification 24
- Client interface 25
- Client verification 26
- Engineering documentation 27
- Client verification 28
- Client verification 29
- Client verification 30
- Client verification 31
- Engine simulation 32
- Client verification 33
- Client verification 34
- Release requirements 35
- Client interface 36
- Client verification 37
- Client verification 38
- Engine simulation 39
- Client interface 40
- Client interface 41
- Engine verification 42
- Client interface 43
- Motion.Tests 44
- Client interface 45
- Client interface 46
- Client verification 47
- Client interface 48
- Client verification 49
- EconomyBalance 50
- Client verification 51
- Client interface 52
- Client verification 53
- Client interface 54
- Client interface 55
- Client interface 56
- Client interface 57
- Motion.Tests 58
- Engine verification 59
- Client interface 60
- Client interface 61
- Client interface 62
- Client verification 63
- Engine verification 64
- Performance 65
- Client verification 66
- Local session transport 67
- Engine verification 68
- Client interface 69
- Client interface 70
- Client interface 71
- Client interface 72
- Client interface 73
- Engineering documentation 74
- Client verification 75
- Snapshot contracts 76
- Client verification 77
- Client verification 78
- Engine simulation 79
- Client interface 80
- Release requirements 81
- Client interface 82
- Client interface 83
- Engine simulation 84
- Client verification 85
- Client interface 86
- Client interface 87
- Motion.Tests 88
- Client verification 89
- Client verification 90
- Release requirements 91
- Release requirements 92
- Client verification 93
- Client interface 94
- Client verification 95
- Client interface 96
- Performance.Tests 97
- Release requirements 98
- Client verification 99
- Release requirements 100
- Engine verification 101
- Engine verification 102
- Engine verification 103
- Client interface 104
- Client interface 105
- Local session transport 106
- Engine verification 107
- Engine verification 108
- Motion 109
- Engine verification 110
- Engine verification 111
- PortraitAssets 112
- Client verification 113
- Contracts.Tests 114
- Engine verification 115
- Engine simulation 116
- Engine verification 117
- Engine verification 118
- Client verification 119
- Client interface 120
- Client verification 121
- Client verification 122
- Client verification 123
- Engine simulation 124
- Engine simulation 125
- Engine verification 126
- Client verification 127
- Client interface 128
- Client verification 129
- Client verification 130
- Engine simulation 131
- Engine simulation 132
- Engine verification 133
- Engine verification 134
- Engine verification 135
- Client verification 136
- Contracts.Tests 137
- Client verification 138
- EconomyBalance 139
- Client verification 140
- Client interface 141
- Engine verification 142
- Motion 143
- Client verification 144
- Engine verification 145
- CombatSim 146
- Gameplay requirements 147
- Motion 148
- Client interface 149
- Client verification 150
- Client verification 151
- Combat report planning 152
- Combat report planning 153
- Engine simulation 154
- Client verification 155
- Client interface 156
- Motion.Tests 157
- Engine verification 158
- Client verification 159
- Client verification 160
- Engine verification 161
- Release requirements 162
- EconomyBalance 163
- Contracts.Tests 164
- Performance 165
- Contracts.Tests 166
- Engine simulation 167
- Client verification 168
- Engine verification 169
- Engine verification 170
- Client interface 171
- Client verification 172
- Client verification 173
- Client interface 174
- Client verification 175
- Engine simulation 176
- Client verification 177
- Engine verification 178
- Client interface 179
- Combat report planning 180
- Combat report planning 181
- Combat report planning 182
- Combat report planning 183
- Combat report planning 184
- Combat report planning 185
- Combat report planning 186
- Combat report planning 187
- Combat report planning 188
- Combat report planning 189
- Contracts.Tests 190
- Client verification 191
- Client verification 192
- Engine verification 193
- Engine verification 194
- Engine verification 195
- Engine simulation 196
- EconomyBalance.Tests 197
- Engine verification 198
- 03 Design 199
- Client verification 200
- Client verification 201
- Client interface 202
- Client interface 203
- Client interface 204
- Client interface 205
- Contracts.Tests 206
- Engine verification 207
- EconomyBalance.Tests 208
- Engine verification 209
- 00 Process 210
- Release requirements 211
- Client verification 212
- Client interface 213
- EconomyBalance.Tests 214
- Engine verification 215
- Engine verification 216
- Engine verification 217
- Engine verification 218
- Engine verification 219
- Combat report planning 220
- Engine verification 221
- Client interface 222
- Release requirements 223
- Client interface 224
- Client interface 225
- Client verification 226
- Client interface 227
- Client interface 228
- Snapshot contracts 229
- Client verification 230
- Client verification 231
- Engine simulation 232
- EconomyBalance.Tests 233
- Client interface 234
- Client interface 235
- Engine verification 236
- Engine verification 237
- Client verification 238
- Client verification 239
- Engine verification 240
- Engine verification 241
- Engine verification 242
- Engine simulation 243
- Engine simulation 244
- Engine verification 245
- Engine verification 246
- Engine verification 247
- Client interface 248
- Contracts.Tests 249
- Client verification 250
- Contracts.Tests 251
- Client verification 252
- Engine verification 253
- Engine verification 254
- Engine simulation 255
- Release requirements 256
- 03 Design 257
- Client verification 258
- Snapshot contracts 259
- Motion.Tests 260
- Client verification 261
- Engine verification 262
- PortraitAssets 263
- Combat report planning 264
- Client interface 265
- Engine verification 266
- Contracts.Tests 267
- Client verification 268
- Client verification 269
- Engine verification 270
- Engine verification 271
- Engine verification 272
- Engine verification 273
- EconomyBalance 274
- Contracts.Tests 275
- Combat report planning 276
- Combat report planning 277
- Combat report planning 278
- Client verification 279
- Release requirements 280
- Engineering documentation 281
- PortraitAssets 282
- Client verification 283
- Client interface 284
- Contracts.Tests 285
- Client verification 286
- Client verification 287
- Engine verification 288
- Engine verification 289
- Engine verification 290
- Engine verification 291
- Engine verification 292
- Engine verification 293
- Engine verification 294
- Combat report planning 295
- Combat report planning 296
- Combat report planning 297
- Combat report planning 298
- EconomyBalance.Tests 299
- 00 Process 300
- Client verification 301
- Client interface 302
- Client verification 303
- Snapshot contracts 304
- Engine simulation 305
- Engine verification 306
- Client verification 307
- Client verification 308
- Client verification 309
- Contracts.Tests 310
- Engine verification 311
- Engine verification 312
- Engine verification 313
- Engine verification 314
- Engine verification 315
- Combat report planning 316
- Combat report planning 317
- Combat report planning 318
- Combat report planning 319
- Combat report planning 320
- Combat report planning 321
- Combat report planning 322
- Combat report planning 323
- 06 Tooling 324
- Engine simulation 325
- EP 0011 short fixes 326
- EP 0011 short fixes 327
- EP 0011 short fixes 328
- EP 0011 short fixes 329
- Engineering documentation 330
- 05 Backlog 331
- Validation 332
- Client interface 333
- Contracts.Tests 334
- Client verification 335
- Engine verification 336
- Engine verification 337
- Engine verification 338
- EP 0011 short fixes 339
- Client interface 340
- Client verification 341
- Client verification 342
- 05 Backlog 343
- Engine verification 344
- Engine verification 345
- Engine verification 346
- PortraitAssets 347
- Motion 348
- Client verification 349
- Client verification 350
- Client verification 351
- Client verification 352
- Engine verification 353
- Engine verification 354
- Engine verification 355
- Engine verification 356
- Client interface 357
- Release requirements 358
- Release requirements 359
- Engine verification 360
- Snapshot contracts 361
- Snapshot contracts 362
- Engine simulation 363
- Engine simulation 364
- Contracts.Tests 365
- Contracts.Tests 366
- Contracts.Tests 367
- Engine verification 368
- Engine verification 369
- Client verification 370
- Release requirements 371
- Client verification 372
- Client verification 373
- Engine verification 374
- Engine verification 375
- Engine verification 376
- Engine verification 377
- Engine verification 378
- PortraitAssets 379
- Client interface 380
- 03 Design 381
- Client interface 382
- Client verification 383
- EP 0011 short fixes 384
- Release requirements 385
- Release requirements 386
- Release requirements 387
- Engineering documentation 388
- Snapshot contracts 389
- Contracts.Tests 390
- Contracts.Tests 391
- Contracts.Tests 392
- Engine verification 393
- Engine verification 394
- Motion.Tests 395
- Release requirements 396
- Release requirements 397
- Release requirements 398
- Release requirements 399
- Release requirements 400
- Engineering documentation 401
- Client interface 402
- Client verification 403
- Client verification 404
- Engine verification 405
- Release requirements 406
- Release requirements 407
- Release requirements 408
- Release requirements 409
- Engineering documentation 410
- Engineering documentation 411
- Client verification 412
- Contracts.Tests 413
- Engine simulation 414
- Engine verification 415
- 00 Process 416
- 03 Design 417
- 06 Tooling 418

## God Nodes (most connected - your core abstractions)
1. `SimulationEngine` - 690 edges
2. `GameSessionScreen` - 509 edges
3. `DeepSpaceSaga.Contracts` - 505 edges
4. `ObjectMotionSnapshot` - 349 edges
5. `AuthoritativeSnapshot` - 342 edges
6. `ScenarioException` - 283 edges
7. `LinearMotionPredictor` - 237 edges
8. `SnapshotBuffer` - 235 edges
9. `DeepSpaceSaga.Engine.Scenario` - 229 edges
10. `DeepSpaceSaga.Client.Tests` - 185 edges

## Surprising Connections (you probably didn't know these)
- `Factory` --implements--> `IGameSessionFactory`  [EXTRACTED]
  tools/DeepSpaceSaga.Performance/CombatReportsNativeEvidence.cs → src/DeepSpaceSaga.Client/IGameSessionFactory.cs
- `MalePortraitTests` --references--> `PortraitAssetRepository`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/MalePortraitTests.cs → src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs
- `PortraitGeneratorTests` --references--> `PortraitAssetRepository`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/PortraitGeneratorTests.cs → src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs
- `UnifiedPortraitTests` --references--> `PortraitAssetRepository`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/UnifiedPortraitTests.cs → src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs
- `Scene` --references--> `SnapshotBuffer`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/TacticalMapViewTests.cs → src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Combat report delivery pipeline** — src_deepspacesaga_engine_simulationengine_combatreports_mechanism_outbox, src_deepspacesaga_engine_localclient_snapshotmailbox_mechanism_mailbox, src_deepspacesaga_client_snapshotbuffer_mechanism_buffer, src_deepspacesaga_client_combatreportinbox_mechanism_inbox, src_deepspacesaga_client_ui_screens_gamesession_combatreportstore_mechanism_history [INFERRED 0.85]

## Communities (419 total, 21 thin omitted)

### Community 0 - "Client interface 0"
Cohesion: 0.02
Nodes (147): CameraZoomTransition, Defense, DefenseSubmission, Encounter, FirstContact, FlightMapLabel, LabelWidth, Launcher (+139 more)

### Community 1 - "Engine simulation 1"
Cohesion: 0.02
Nodes (111): Consumed, Cost, DeepSpaceSaga.Engine.Trading, Events, IssuedQuote, QuoteId, RealizedCostCredits, Remaining (+103 more)

### Community 2 - "Gameplay requirements 2"
Cohesion: 0.01
Nodes (240): 10. Authoritative GameState, General JSON и Settings.json, 11. Сохранение, 12. Журналирование, 13. Критические ошибки и диагностика, 14. Идентификаторы объектов, 15. Генераторы случайных чисел, 16. Статус этой контрольной точки, 17. Раздельная архитектура движка и отрисовки (+232 more)

### Community 3 - "Engine verification 3"
Cohesion: 0.03
Nodes (61): AttackAiData, ShipCrewMemberData, HashSet, IReadOnlyDictionary, JsonSerializerOptions, ScenarioException, ScenarioLoader, TradingMapRngData (+53 more)

### Community 4 - "Engine verification 4"
Cohesion: 0.03
Nodes (66): DeepSpaceSaga.Engine.Scenario, DeepSpaceSaga.Engine.Content, CombatReactionSaveData, ScenarioGroupPlacement, StationClusterSaveValidation, Fact, AbandonedObjectContentTests, Fact (+58 more)

### Community 5 - "Engine verification 5"
Cohesion: 0.05
Nodes (50): IDisposable, Func, IEnumerable, ImmutableArray, IReadOnlyList, Random, ResourceFieldAsteroidData, ResourceFieldCountData (+42 more)

### Community 6 - "Engine simulation 6"
Cohesion: 0.03
Nodes (58): DeepSpaceSaga.Engine.Combat, DeepSpaceSaga.Engine, DeepSpaceSaga.Engine.Rng, DefenseLaunch, HostileLaunch, ManualDefenseContext, ModuleHitOutcome, Shot (+50 more)

### Community 7 - "Client verification 7"
Cohesion: 0.04
Nodes (73): DeepSpaceSaga.Client, PendingSceneFrame, BufferedSnapshot, PredictionDeltaMs, Dictionary, ImmutableArray, Queue, SortedDictionary (+65 more)

### Community 8 - "Engine simulation 8"
Cohesion: 0.04
Nodes (47): MarketRevision, QuoteContext, QuoteTerms, PlayerCommand, ImmutableArray, ModuleTypeDefinition, IReadOnlyList, ActiveCycleData (+39 more)

### Community 9 - "Engine verification 9"
Cohesion: 0.05
Nodes (50): Due, Ice, Pending, StationDistrict, Administration, Dock, Habitation, Market (+42 more)

### Community 10 - "Client verification 10"
Cohesion: 0.07
Nodes (26): X, Y, SKCanvas, Action, DateTime, Fact, Func, H (+18 more)

### Community 11 - "Client verification 11"
Cohesion: 0.05
Nodes (49): Dictionary, IReadOnlyList, Label, List, SKBitmap, SKCanvas, SKPaint, SKRect (+41 more)

### Community 12 - "Engine simulation 12"
Cohesion: 0.05
Nodes (73): CommandDefinitionDto, CommandDefinitionsFile, EconomyContentVersions, EngineSettingsFile, FactoryTypeDefinitionDto, FactoryTypesFile, Items, ItemTypeDefinitionDto (+65 more)

### Community 13 - "Client interface 13"
Cohesion: 0.07
Nodes (46): IEnumerator, IReadOnlyList, MaxX, MaxY, MinX, MinY, ObjectTrailBuffer, Bounds (+38 more)

### Community 14 - "Client interface 14"
Cohesion: 0.04
Nodes (52): PoiGeometry, CameraState, IEnumerable, IReadOnlyList, SKCanvas, SKPath, AiMapPresentation, PoiGeometry (+44 more)

### Community 15 - "Engine verification 15"
Cohesion: 0.06
Nodes (29): IReadOnlyCollection, IReadOnlyList, ShipDestructionMath, ShipModuleLife, ImmutableArray, RecipeDefinition, RecipeMaterial, Fact (+21 more)

### Community 16 - "Client verification 16"
Cohesion: 0.08
Nodes (25): EngineTradeConnection, MouseButton, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SKBitmap (+17 more)

### Community 17 - "Motion.Tests 17"
Cohesion: 0.07
Nodes (25): ApproachFlyThroughPlan, FamilyFormula, First, Plan, ReadOnlySpan, Second, Span, X (+17 more)

### Community 18 - "Client verification 18"
Cohesion: 0.09
Nodes (23): Enabled, CancellationToken, Dictionary, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, ImmutableArray (+15 more)

### Community 19 - "Client interface 19"
Cohesion: 0.07
Nodes (29): GL, GRBackendRenderTarget, GRContext, IInputContext, IMouse, RawImage, ScrollWheel, Exception (+21 more)

### Community 20 - "Engine verification 20"
Cohesion: 0.10
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, EngineCommandTests

### Community 21 - "Engine verification 21"
Cohesion: 0.08
Nodes (13): CancellationToken, IAsyncEnumerable, IEnumerable, Action, Fact, IEnumerable, InlineData, SimulationEngine (+5 more)

### Community 22 - "Client interface 22"
Cohesion: 0.07
Nodes (33): DeepSpaceSaga.Client.UI.Screens.Finance, DeepSpaceSaga.Client.UI.Screens, FinanceRow, FinanceLayout, ImmutableArray, IReadOnlyList, Key, MouseButton (+25 more)

### Community 23 - "Client verification 23"
Cohesion: 0.05
Nodes (50): AssemblyInformationalVersionAttribute, Channel, Dictionary, Exception, IReadOnlyList, JsonSerializerOptions, SemaphoreSlim, Task (+42 more)

### Community 24 - "Client verification 24"
Cohesion: 0.07
Nodes (18): SKPoint, SKRect, SKSize, ObjectLabelLayout, ObjectLabelText, StatusSquareAnimator, TacticalMapMarkerPolicy, Fact (+10 more)

### Community 25 - "Client interface 25"
Cohesion: 0.05
Nodes (60): DisplayEntry, Maximum, Entry, Command, Dictionary, Id, IReadOnlyList, List (+52 more)

### Community 26 - "Client verification 26"
Cohesion: 0.11
Nodes (18): ActiveObjectId, SelectedObjectId, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, List (+10 more)

### Community 27 - "Engineering documentation 27"
Cohesion: 0.03
Nodes (72): Решения первого релиза, Требования первого релиза, Экипаж, пассажиры и жилой модуль, Цель, Актуализация EP-0008 — 2026-10-12, За рамками первого релиза, Требования первого релиза, Решение первого релиза (+64 more)

### Community 28 - "Client verification 28"
Cohesion: 0.05
Nodes (47): IEnumerable, ImmutableArray, IReadOnlyList, List, SKCanvas, SKRect, CombatJournalPanel, Bounds (+39 more)

### Community 29 - "Client verification 29"
Cohesion: 0.12
Nodes (16): Fact, GameSessionHandle, ImmutableArray, InlineData, RecordingConnection, SKBitmap, SKRect, SnapshotBuffer (+8 more)

### Community 30 - "Client verification 30"
Cohesion: 0.05
Nodes (38): DeepSpaceSaga.Motion, DeepSpaceSaga.Client.Tests, DeepSpaceSaga.Client.UI.Screens.GameSession, ModuleInitializer, Fact, ClusterResourceMapTests, Fact, CombatPaletteContentTests (+30 more)

### Community 31 - "Client verification 31"
Cohesion: 0.10
Nodes (23): EconomicFixture, CancellationTokenSource, Channel, SemaphoreSlim, SimulationEngine, LocalGameSessionConnection, Fact, Func (+15 more)

### Community 32 - "Engine simulation 32"
Cohesion: 0.06
Nodes (27): MarketEventFlow, MarketItemLimits, MaximumBudget, ImmutableArray, StationMarketKnowledgeSnapshot, StationMarketStockBandSnapshot, StationEventData, StationEventPriceFactorData (+19 more)

### Community 33 - "Client verification 33"
Cohesion: 0.07
Nodes (38): ActiveSave, ImmutableArray, TradingRouteAvailability, Available, Restricted, Unavailable, TradingRouteRisk, Elevated (+30 more)

### Community 34 - "Client verification 34"
Cohesion: 0.10
Nodes (6): Fact, SKBitmap, SKColor, SKPaint, SKRect, StationToolbarTests

### Community 35 - "Release requirements 35"
Cohesion: 0.03
Nodes (64): Сверка реализации — 2026-10-10, Стыковка, Решения первого релиза, Требования первого релиза, Цель, Актуализация EP-0004 — 2026-10-08, Статус реализации (MVP), Сверка реализации — 2026-10-10 (+56 more)

### Community 36 - "Client interface 36"
Cohesion: 0.08
Nodes (42): Button, CommandIconPair, Action, Dictionary, Func, HashSet, ImmutableArray, IReadOnlyDictionary (+34 more)

### Community 37 - "Client verification 37"
Cohesion: 0.07
Nodes (37): Fact, Task, ValueTask, CombatSessionFixture, Connection, DirectoryPath, Engine, Restored (+29 more)

### Community 38 - "Client verification 38"
Cohesion: 0.08
Nodes (25): CombatReportResourcesDisposed, ReportPanel, ReportStore, CancellationToken, Channel, Fact, IAsyncEnumerable, Task (+17 more)

### Community 39 - "Engine simulation 39"
Cohesion: 0.06
Nodes (43): CommandDefinition, FactorDefaults, Action, HashSet, IEnumerable, ImmutableArray, GameDataRegistry, CatalogCompatibility (+35 more)

### Community 40 - "Client interface 40"
Cohesion: 0.08
Nodes (23): DeepSpaceSaga.Client.UI.Screens.GameMenu, SKRect, GameMenuButton, Load, MainMenu, None, Resume, Save (+15 more)

### Community 41 - "Client interface 41"
Cohesion: 0.11
Nodes (16): DeepSpaceSaga.Client.UI.Screens.Trade, TradeItemPresentation, SKCanvas, SKColor, SKPaint, SKRect, SKTextAlign, SKTypeface (+8 more)

### Community 42 - "Engine verification 42"
Cohesion: 0.08
Nodes (36): ActualDays, StraightDays, Fact, InlineData, Task, Theory, ClusterLocalResumeTests, Settings (+28 more)

### Community 43 - "Client interface 43"
Cohesion: 0.11
Nodes (28): DeepSpaceSaga.Client.UI.Screens.Save, H, SKRect, W, X, Y, SaveHit, SaveLayout (+20 more)

### Community 44 - "Motion.Tests 44"
Cohesion: 0.08
Nodes (23): DeepSpaceSaga.Motion.Tests, Span, X, Y, LinearMotionPredictor, ArgumentOutOfRangeException, Fact, InlineData (+15 more)

### Community 45 - "Client interface 45"
Cohesion: 0.07
Nodes (40): InputFocus, QuoteKey, CancellationTokenSource, Entry, GameSessionHandle, IReadOnlyList, Key, ScreenEvent (+32 more)

### Community 46 - "Client interface 46"
Cohesion: 0.09
Nodes (29): FileNotFoundException, Action, Image, Key, Label, List, MouseButton, Rect (+21 more)

### Community 47 - "Client verification 47"
Cohesion: 0.11
Nodes (19): NavigationTrajectoryProjector, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, IReadOnlyList, List (+11 more)

### Community 48 - "Client interface 48"
Cohesion: 0.08
Nodes (26): DeepSpaceSaga.Client.UI.Screens.MainMenu, Key, MouseButton, SKCanvas, SKPaint, SKRect, MainMenuScreen, SKRect (+18 more)

### Community 49 - "Client verification 49"
Cohesion: 0.13
Nodes (15): CameraState, List, X, Y, FutureTrajectoryPoint, FutureTrajectoryProjector, ObjectMotionSnapshot, IMotionPredictor (+7 more)

### Community 50 - "EconomyBalance 50"
Cohesion: 0.10
Nodes (36): Category, Action, Command, Dictionary, Func, IEnumerable, ImmutableArray, ItemId (+28 more)

### Community 51 - "Client verification 51"
Cohesion: 0.09
Nodes (26): IOException, Task, CancellationToken, Channel, Exception, Func, IAsyncEnumerable, ImmutableArray (+18 more)

### Community 52 - "Client interface 52"
Cohesion: 0.07
Nodes (32): LabelMetrics, SmoothState, SKPoint, SKRect, ObjectLabelGeometry, Dictionary, Func, HashSet (+24 more)

### Community 53 - "Client verification 53"
Cohesion: 0.08
Nodes (28): DeepSpaceSaga.Client.UI, GRContextOptions, SKColor, CombatVisualSettings, Countermeasure, CountermeasureIntercept, CountermeasurePrediction, CountermeasureTrail (+20 more)

### Community 54 - "Client interface 54"
Cohesion: 0.10
Nodes (29): IEqualityComparer, Scene, SourceScene, CancellationToken, CancellationTokenSource, Guid, IReadOnlyDictionary, Task (+21 more)

### Community 55 - "Client interface 55"
Cohesion: 0.12
Nodes (18): IReadOnlyList, SKCanvas, SKColor, SKPaint, SKRect, GridPanel, GridSortColumn, BuyingCount (+10 more)

### Community 56 - "Client interface 56"
Cohesion: 0.08
Nodes (31): DateTimeOffset, SnapshotPrediction, SnapshotPrediction, AssemblyInformationalVersionAttribute, TacticalMapFrameProfile, TacticalMapFrameRecorder, TacticalMapProfileCapture, TacticalMapProfileSummary (+23 more)

### Community 57 - "Client interface 57"
Cohesion: 0.12
Nodes (24): H, SKRect, W, X, Y, LoadHit, LoadLayout, ListHeight (+16 more)

### Community 58 - "Motion.Tests 58"
Cohesion: 0.14
Nodes (16): ApproachRoute, DurationMs, Length, Direction, X, Y, ApproachLineCaptureMath, ApproachRoute (+8 more)

### Community 59 - "Engine verification 59"
Cohesion: 0.11
Nodes (18): HullCellCoordinate, HullLayoutData, ScenarioMetadata, StartingScenario, Fact, InlineData, Theory, AuthoritativeOrbitRuntimeTests (+10 more)

### Community 60 - "Client interface 60"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Hire, HireLayout, Key, MouseButton, SnapshotBuffer, HireScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 61 - "Client interface 61"
Cohesion: 0.08
Nodes (46): ImmutableSortedDictionary, Dictionary, CharacterAppearance, Age, Colors, Gender, LibraryVersion, Parts (+38 more)

### Community 62 - "Client interface 62"
Cohesion: 0.08
Nodes (42): Key, MouseButton, IScreen, ScreenEvent, CloseContracts, CloseDialogue, CloseFinance, CloseHire (+34 more)

### Community 63 - "Client verification 63"
Cohesion: 0.19
Nodes (6): MouseButton, Fact, InlineData, Task, Theory, StationScreenTests

### Community 64 - "Engine verification 64"
Cohesion: 0.06
Nodes (26): CombatResult, DeepSpaceSaga.Engine.Tests, InlineData, ITestOutputHelper, Theory, Trait, CombatBalanceReport, Fact (+18 more)

### Community 65 - "Performance 65"
Cohesion: 0.09
Nodes (12): IGameSessionFactory, CancellationToken, IAsyncEnumerable, ValueTask, IGameSessionConnection, CombatNativeEvidence, BindingFlags, IWindow (+4 more)

### Community 66 - "Client verification 66"
Cohesion: 0.11
Nodes (18): Fact, Geometry, InlineData, Theory, CombatTrajectoryTests, Snapshot, Fact, InlineData (+10 more)

### Community 67 - "Local session transport 67"
Cohesion: 0.09
Nodes (16): DeepSpaceSaga.Engine.LocalClient, LocalGameSessionFactory, QuickSavePath, SaveDirectory, ScenariosDirectory, SettingsPath, Program, SaveSlotNaming (+8 more)

### Community 68 - "Engine verification 68"
Cohesion: 0.20
Nodes (11): ArgumentNullException, Fact, ImmutableDictionary, InlineData, InstalledModuleRuntime, IReadOnlyList, SimulationEngine, SpaceObjectRuntime (+3 more)

### Community 69 - "Client interface 69"
Cohesion: 0.10
Nodes (16): ReticleGlow, SKCanvas, List, IReadOnlyList, SKCanvas, SKImage, SKPaint, ReticleGlow (+8 more)

### Community 70 - "Client interface 70"
Cohesion: 0.13
Nodes (17): CombatReportAssets, CombatReportFormatter, CombatReportStore, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+9 more)

### Community 71 - "Client interface 71"
Cohesion: 0.11
Nodes (22): CombatEffect, Func, HashSet, ImmutableArray, IReadOnlyList, List, CombatEffect, TerminalTrail (+14 more)

### Community 72 - "Client interface 72"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Contracts, ContractsLayout, Key, MouseButton, SnapshotBuffer, ContractsScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 73 - "Client interface 73"
Cohesion: 0.10
Nodes (21): Row, SKRect, GameSessionHandle, ImmutableArray, Key, SKCanvas, SKRect, SnapshotBuffer (+13 more)

### Community 74 - "Engineering documentation 74"
Cohesion: 0.05
Nodes (39): Требования первого релиза, Approach: уточнение от 2026-09-20, Командные панели корабля, Цель, Engine, Актуализация EP-0008 — 2026-10-12, Актуализация EP-0009 — 2026-10-12, Maneuver (+31 more)

### Community 75 - "Client verification 75"
Cohesion: 0.15
Nodes (21): ControlledConnection, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task (+13 more)

### Community 76 - "Snapshot contracts 76"
Cohesion: 0.05
Nodes (20): DeepSpaceSaga.Contracts, CharacterPortraits, NavigationComputerCommandTypes, ScannerCommandTypes, ShipEngineCommandTypes, ShipEventReasonCodes, ShipEventTypes, PlayerRelation (+12 more)

### Community 77 - "Client verification 77"
Cohesion: 0.15
Nodes (13): IEnumerable, Stack, ScreenStack, Count, Current, UnderCurrent, Fact, Key (+5 more)

### Community 78 - "Client verification 78"
Cohesion: 0.12
Nodes (20): IGameSessionFactory, Action, Fact, GameSessionHandle, InlineData, InvalidOperationException, Key, ObjectDisposedException (+12 more)

### Community 79 - "Engine simulation 79"
Cohesion: 0.14
Nodes (14): AiBaseGenerator, EnvironmentFieldGenerator, IReadOnlyList, PointOfInterestGenerator, IReadOnlyList, AiGenerationConfig, EnvironmentGenerationConfig, PoiTemplate (+6 more)

### Community 80 - "Client interface 80"
Cohesion: 0.12
Nodes (17): DeepSpaceSaga.Client.UI.Screens.Ship, Bottom, Left, Right, Top, ShipButton, Close, None (+9 more)

### Community 81 - "Release requirements 81"
Cohesion: 0.05
Nodes (36): Схема модулей, Назначение, За рамками первого релиза, Требования первого релиза, Стартовые модули, Актуализация EP-0008 — 2026-10-12, Актуализация EP-0009 — 2026-10-12, Hull grid (+28 more)

### Community 82 - "Client interface 82"
Cohesion: 0.15
Nodes (14): SKBitmap, SKCanvas, SKColor, SKPaint, SKRect, StationToolbar, HasLoadedCrewImage, HasLoadedExitButtonImage (+6 more)

### Community 83 - "Client interface 83"
Cohesion: 0.12
Nodes (18): Action, Func, H, IReadOnlyList, Key, MouseButton, SKCanvas, SKPaint (+10 more)

### Community 84 - "Engine simulation 84"
Cohesion: 0.09
Nodes (31): ImmutableDictionary, DialogueEvent, ImmutableDictionary, ImmutableHashSet, PlayerCharacterState, QuestState, SecurityIncident, StationAccessState (+23 more)

### Community 85 - "Client verification 85"
Cohesion: 0.12
Nodes (23): CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task, TaskCompletionSource (+15 more)

### Community 86 - "Client interface 86"
Cohesion: 0.11
Nodes (20): ScenarioRow, Func, H, IReadOnlyList, Key, Label, List, MouseButton (+12 more)

### Community 87 - "Client interface 87"
Cohesion: 0.14
Nodes (18): ActiveObjectId, CancellationToken, CancellationTokenSource, Channel, Exception, SelectedObjectId, Task, TradeJournal (+10 more)

### Community 88 - "Motion.Tests 88"
Cohesion: 0.14
Nodes (11): HostileCombatSnapshot, HostileMotionSample, HostileOrbitMath, Fact, InlineData, Theory, FlankApproachTests, Fact (+3 more)

### Community 89 - "Client verification 89"
Cohesion: 0.19
Nodes (5): Fact, GameSessionScalePanelTests, Fact, GameSessionZoomTests, LogTailReader

### Community 90 - "Client verification 90"
Cohesion: 0.11
Nodes (23): After, Before, English, ExpectedEconomy, FirstHour, ProfileId, Russian, StationId (+15 more)

### Community 91 - "Release requirements 91"
Cohesion: 0.05
Nodes (35): Сверка реализации — 2026-10-10, Требования первого релиза, Минимальные товары-кандидаты, Уже существующая основа, Грузовой отсек, Требует решения, Решение первого релиза, Цель (+27 more)

### Community 92 - "Release requirements 92"
Cohesion: 0.05
Nodes (38): 10. Проверка интересности торговой карты, 11. Информация, доступная игроку, 12. Рекомендуемая нарезка на пользовательские истории, 13. Definition of Done для всей инициативы, 1. Цель MVP, Сверка реализации — 2026-10-10, 2. Жёсткие границы MVP, 3. Игровой экономический интервал (+30 more)

### Community 93 - "Client verification 93"
Cohesion: 0.14
Nodes (17): IDictionary, Fact, Func, GameSessionHandle, InlineData, Task, Theory, ValueTask (+9 more)

### Community 94 - "Client interface 94"
Cohesion: 0.13
Nodes (18): Person, SKRect, DialogueLayout, GameSessionHandle, Key, MouseButton, Path, ScreenEvent (+10 more)

### Community 95 - "Client verification 95"
Cohesion: 0.18
Nodes (8): CameraState, FocusX, FocusY, PixelsPerWorldUnit, Fact, CameraStateTests, Fact, CameraStateZoomTests

### Community 96 - "Client interface 96"
Cohesion: 0.11
Nodes (19): DeepSpaceSaga.Client.UI.Screens.Load, Action, Func, H, IReadOnlyList, Key, MouseButton, SKCanvas (+11 more)

### Community 97 - "Performance.Tests 97"
Cohesion: 0.11
Nodes (16): DeepSpaceSaga.Performance.Tests, IEnumerable, JsonElement, SolarMapEvidence, Fact, ClusterPerformanceReportTests, Root, Fact (+8 more)

### Community 98 - "Release requirements 98"
Cohesion: 0.05
Nodes (35): Гидропонная колония, 10. Интерес игрока, 11. Переиспользование существующего экрана торговли станции, 12. Финансовая оценка, 13. Первая рабочая версия, 1. Основная идея, Сверка реализации — 2026-10-10, 2. Что сохраняется от модели Battle Brothers (+27 more)

### Community 99 - "Client verification 99"
Cohesion: 0.13
Nodes (14): MouseButton, Fact, InlineData, Theory, AiBasePresentationTests, CancellationToken, Fact, IAsyncEnumerable (+6 more)

### Community 100 - "Release requirements 100"
Cohesion: 0.05
Nodes (36): 10. Генерация новой игры, 11. Интеграция стартовых сценариев, 12. Полностью открытая карта и будущий туман войны, 13. Отображение и взаимодействие, 14. Архитектурные границы и модель данных, 15. Сохранение и продолжение, 16.1. Расхождения, которые нужно разрешить при подготовке эпика, 16. Проверенная основа и необходимые изменения (+28 more)

### Community 101 - "Engine verification 101"
Cohesion: 0.16
Nodes (11): State, Value, TradingMapRandom, TradingMapLinkData, ArgumentOutOfRangeException, Fact, HashSet, ImmutableDictionary (+3 more)

### Community 102 - "Engine verification 102"
Cohesion: 0.20
Nodes (11): Fact, InlineData, Theory, CombatForecastEngineManeuverTests, Fact, Func, InlineData, Lazy (+3 more)

### Community 103 - "Engine verification 103"
Cohesion: 0.13
Nodes (21): Action, Command, Func, IReadOnlyList, Item, List, Quantity, Result (+13 more)

### Community 104 - "Client interface 104"
Cohesion: 0.13
Nodes (21): Belt, Lod, CameraState, Dictionary, SKCanvas, SKPath, SKRect, X (+13 more)

### Community 105 - "Client interface 105"
Cohesion: 0.15
Nodes (17): Dictionary, Guid, IReadOnlyList, Queue, NpcMotionObservation, NpcMotionObservationHistory, ObjectCount, SampleCount (+9 more)

### Community 106 - "Local session transport 106"
Cohesion: 0.13
Nodes (14): CancellationToken, Guid, IAsyncEnumerable, ValueTask, ICombatPresentationConnection, SimulationControlAcknowledgement, CancellationToken, Guid (+6 more)

### Community 107 - "Engine verification 107"
Cohesion: 0.24
Nodes (11): ContentException, Fact, InlineData, JsonNode, JsonObject, Theory, CombatContentLoaderTests, ClassesPath (+3 more)

### Community 108 - "Engine verification 108"
Cohesion: 0.18
Nodes (7): ImmutableArray, ModuleCategoryDefinition, Fact, SimulationEngine, InstalledModuleProjectionTests, Fact, ModuleDefinitionLoaderTests

### Community 109 - "Motion 109"
Cohesion: 0.17
Nodes (9): ClosestX, ClosestY, IsArrived, NavigationStepResult, NavigationWaypointMath, StagedNavigationStepResult, StagedNavigationStepResult, Fact (+1 more)

### Community 110 - "Engine verification 110"
Cohesion: 0.19
Nodes (7): Fact, Func, IEnumerable, IReadOnlyList, JsonObject, CatalogCompatibilityTests, SettingsPath

### Community 111 - "Engine verification 111"
Cohesion: 0.21
Nodes (8): Fact, IEnumerable, InlineData, ItemTypeId, Quantity, SimulationEngine, Theory, TradeCommandTests

### Community 112 - "PortraitAssets 112"
Cohesion: 0.13
Nodes (13): FaceAssets, Dictionary, Left, Right, SKBitmap, SKPoint, SKPoint3, FaceEyeAssets (+5 more)

### Community 113 - "Client verification 113"
Cohesion: 0.16
Nodes (10): IReadOnlyList, SKCanvas, SKColor, SKPaint, GridRenderer, Fact, InlineData, SKBitmap (+2 more)

### Community 114 - "Contracts.Tests 114"
Cohesion: 0.15
Nodes (14): ImmutableArray, CommandReasonCodes, CommandResult, CommandResultStatus, Cancelled, Deferred, Executed, Failed (+6 more)

### Community 115 - "Engine verification 115"
Cohesion: 0.11
Nodes (19): ImmutableArray, IReadOnlyDictionary, CombatContentValidation, CombatDamageRules, CombatRulesDefinition, FireModeDefinition, FireModesFile, ShipDestructionRules (+11 more)

### Community 116 - "Engine simulation 116"
Cohesion: 0.12
Nodes (7): Random, RngStreamSeedDerivation, SpaceObjectData, StationCrewMemberData, StationCrewMemberRuntime, Fact, MasterSeedTests

### Community 117 - "Engine verification 117"
Cohesion: 0.14
Nodes (13): Fact, IEnumerable, MemberData, SimulationEngine, Theory, X, Y, FullMapCorrectnessCorpusTests (+5 more)

### Community 118 - "Engine verification 118"
Cohesion: 0.19
Nodes (10): Fact, IEnumerable, InlineData, ItemTypeId, Quantity, Registry, SimulationEngine, SpaceObjectRuntime (+2 more)

### Community 119 - "Client verification 119"
Cohesion: 0.19
Nodes (8): Alpha, RadiusMultiplier, SKColor, SKPath, TacticalMapDepthRenderer, Fact, SKColor, TacticalMapDepthRendererTests

### Community 120 - "Client interface 120"
Cohesion: 0.12
Nodes (20): ApproachRouteGeometryCache, ApproachRouteSample, ApproachRouteSignature, ApproachRoute, CameraState, List, X, Y (+12 more)

### Community 121 - "Client verification 121"
Cohesion: 0.12
Nodes (18): DeepSpaceSaga.Client.UI.Screens.TempCharacterImage, DeepSpaceSaga.Client.Portraits, JsonSerializerOptions, AppearanceSerializer, PortraitGenerator, PortraitRandom, Fact, Lazy (+10 more)

### Community 122 - "Client verification 122"
Cohesion: 0.12
Nodes (17): ExpectedProfile, IAsyncEnumerator, CancellationToken, Fact, Func, IReadOnlyDictionary, ItemId, MemberData (+9 more)

### Community 123 - "Client verification 123"
Cohesion: 0.16
Nodes (15): Key, SKCanvas, SKPaint, SKRect, Stopwatch, TextInputBox, CaretVisible, Length (+7 more)

### Community 124 - "Engine simulation 124"
Cohesion: 0.14
Nodes (19): ItemCatalogValidation, ItemStorageKind, Cargo, FuelTank, ItemTypeDefinition, TradeCategory, Good, Resource (+11 more)

### Community 125 - "Engine simulation 125"
Cohesion: 0.19
Nodes (16): Dictionary, From, HashSet, IEnumerable, IReadOnlyDictionary, IReadOnlyList, To, TradingGraphGenerator (+8 more)

### Community 126 - "Engine verification 126"
Cohesion: 0.13
Nodes (13): Fact, InlineData, Theory, FullClusterContentTests, Fact, InlineData, Theory, CombatBootstrapTests (+5 more)

### Community 127 - "Client verification 127"
Cohesion: 0.17
Nodes (10): ArgumentException, Fact, InlineData, SKBitmap, SKCanvas, Theory, X, Y (+2 more)

### Community 128 - "Client interface 128"
Cohesion: 0.15
Nodes (13): DeepSpaceSaga.Client.UI.Assets, SKBitmap, SKCanvas, SKPaint, SKRect, XenonComboBox, HasAssets, Action (+5 more)

### Community 129 - "Client verification 129"
Cohesion: 0.13
Nodes (14): CameraState, Dictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList, SKCanvas, ClusterMapPresentation, ClusterStationPresentation (+6 more)

### Community 130 - "Client verification 130"
Cohesion: 0.11
Nodes (18): ImmutableArray, TradeQuoteRequest, TradeQuoteSnapshot, CancellationToken, Func, IAsyncEnumerable, List, TaskCompletionSource (+10 more)

### Community 131 - "Engine simulation 131"
Cohesion: 0.23
Nodes (11): Func, SimulationClock, GameTimeMs, SimulationTimeMs, Speed, SimulationClockState, MotionTimeMs, Fact (+3 more)

### Community 132 - "Engine simulation 132"
Cohesion: 0.17
Nodes (8): CancellationToken, Func, Guid, IAsyncEnumerable, Fact, InlineData, Theory, CombatPresentationTests

### Community 133 - "Engine verification 133"
Cohesion: 0.22
Nodes (11): ArgumentNullException, ArgumentOutOfRangeException, Fact, ImmutableArray, InlineData, OverflowException, Theory, TradeQuotePriceResult (+3 more)

### Community 134 - "Engine verification 134"
Cohesion: 0.15
Nodes (14): Fact, InlineData, Theory, GeneratedWorldPersistenceTests, InlineData, Task, Theory, LocalWorldSaveRoundtripTests (+6 more)

### Community 135 - "Engine verification 135"
Cohesion: 0.20
Nodes (8): Fact, InlineData, Theory, MarketKnowledgeTests, Fact, InlineData, Theory, RepeatableTradingVoyageTests

### Community 136 - "Client verification 136"
Cohesion: 0.15
Nodes (15): IAsyncDisposable, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, ImmutableArray, List, RecordingConnection (+7 more)

### Community 137 - "Contracts.Tests 137"
Cohesion: 0.16
Nodes (15): ImmutableArray, StationInventoryItemSnapshot, StationMarketEventSnapshot, StationMarketRouteEffectSnapshot, StationMarketStockState, Normal, Shortage, Surplus (+7 more)

### Community 138 - "Client verification 138"
Cohesion: 0.14
Nodes (21): Fact, Func, GameSessionHandle, InlineData, ITestOutputHelper, RecordingConnection, SKBitmap, SKCanvas (+13 more)

### Community 139 - "EconomyBalance 139"
Cohesion: 0.16
Nodes (15): IEnumerable, ImmutableArray, BalanceViolation, MarketHealthEvaluator, Func, ImmutableArray, JsonSerializerOptions, TextWriter (+7 more)

### Community 140 - "Client verification 140"
Cohesion: 0.23
Nodes (8): Fact, Guid, InlineData, SnapshotBuffer, Task, Theory, NpcObservedTrajectoryTests, RejectingPredictor

### Community 141 - "Client interface 141"
Cohesion: 0.14
Nodes (15): SKBitmap, SKCanvas, SKPaint, SKPoint, SKRect, GenericWindowTypeA, HasAssets, SKCanvas (+7 more)

### Community 142 - "Engine verification 142"
Cohesion: 0.26
Nodes (6): Fact, ImmutableDictionary, InlineData, IReadOnlyList, Theory, TradingMapGeometryTests

### Community 143 - "Motion 143"
Cohesion: 0.13
Nodes (24): CancellationToken, Direction, Func, ImmutableArray, X, Y, CombatForecast, CombatForecastLeg (+16 more)

### Community 144 - "Client verification 144"
Cohesion: 0.13
Nodes (19): CancellationToken, Fact, Func, GameSessionHandle, IAsyncEnumerable, InlineData, List, RecordingConnection (+11 more)

### Community 145 - "Engine verification 145"
Cohesion: 0.20
Nodes (10): Fact, InlineData, IReadOnlyList, SimulationEngine, Theory, BasicCombatEndToEndTests, CombatRun, Source (+2 more)

### Community 146 - "CombatSim 146"
Cohesion: 0.14
Nodes (20): Any, BattleResult, _chance(), _clamp(), _defense(), _hit_module(), _make_ship(), Random (+12 more)

### Community 147 - "Gameplay requirements 147"
Cohesion: 0.07
Nodes (29): Экран станции, Сверка реализации — 2026-10-10, Стыковка, Экипаж корабля, Открытые вопросы, За рамками первого релиза, Уже существующая основа в коде, Решения первого релиза (+21 more)

### Community 148 - "Motion 148"
Cohesion: 0.15
Nodes (14): GuidancePose, SnapshotPrediction, Direction, X, Y, GuidancePose, TorpedoGuidanceMath, InlineData (+6 more)

### Community 149 - "Client interface 149"
Cohesion: 0.18
Nodes (12): ImmutableArray, SKBitmap, SKCanvas, SKRect, SKRectI, PersonSex, Female, Male (+4 more)

### Community 150 - "Client verification 150"
Cohesion: 0.13
Nodes (19): Fact, Func, GameSessionHandle, InlineData, SKBitmap, SKCanvas, Task, Theory (+11 more)

### Community 151 - "Client verification 151"
Cohesion: 0.23
Nodes (6): Fact, HashSet, InlineData, Key, Theory, KeyboardEdgeTrackerTests

### Community 152 - "Combat report planning 152"
Cohesion: 0.07
Nodes (26): Воспроизводимая приёмка боя и интерфейса, Входное техническое задание, Acceptance criteria, Acceptance self-review - 2026-10-16, Dependencies, Final self-review —2026-10-16, Gaps and backlog, Grounding и invariants (+18 more)

### Community 153 - "Combat report planning 153"
Cohesion: 0.07
Nodes (27): Входное техническое задание, Документация и честный статус поставки, Acceptance criteria, Dependencies, Execution result —2026-10-16, Execution scope amendment — 2026-10-16, Gaps and backlog, Grounding и invariants (+19 more)

### Community 154 - "Engine simulation 154"
Cohesion: 0.15
Nodes (20): DialogueFile, QuestFile, HashSet, IEnumerable, ImmutableArray, JsonSerializerOptions, DialogueContentLoader, DialogueFile (+12 more)

### Community 155 - "Client verification 155"
Cohesion: 0.19
Nodes (7): CameraState, List, TrajectoryViewportGeometry, Fact, InlineData, Theory, TrajectoryViewportTests

### Community 156 - "Client interface 156"
Cohesion: 0.19
Nodes (17): H, IReadOnlyList, SKRect, W, X, Y, ScenarioSelectHit, ScenarioSelectLayout (+9 more)

### Community 157 - "Motion.Tests 157"
Cohesion: 0.19
Nodes (11): TorpedoRoute, CountermeasureGuidanceMath, CountermeasurePlan, Fact, InlineData, Theory, CountermeasureGuidanceTests, Fact (+3 more)

### Community 158 - "Engine verification 158"
Cohesion: 0.33
Nodes (6): Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageFuelLifecycleTests

### Community 159 - "Client verification 159"
Cohesion: 0.26
Nodes (12): Fact, Fixture, GameSessionHandle, InlineData, RecordingConnection, Task, Theory, Fixture (+4 more)

### Community 160 - "Client verification 160"
Cohesion: 0.17
Nodes (14): CancellationToken, Fact, IAsyncEnumerable, InlineData, List, Task, Theory, ValueTask (+6 more)

### Community 161 - "Engine verification 161"
Cohesion: 0.25
Nodes (7): Fact, InlineData, SimulationEngine, Theory, ManualCountermeasureFireTests, Fact, PdBurstTests

### Community 162 - "Release requirements 162"
Cohesion: 0.11
Nodes (27): 10. Проверки и границы приёмки, 11. Технические допущения декомпозиции, 12. Трассировка, 1. Назначение, 2. Размещение и компоновка, 3. Карточки и взаимодействие, 4. События первой версии, 5. Реплики и докладчики (+19 more)

### Community 163 - "EconomyBalance 163"
Cohesion: 0.16
Nodes (18): Exception, Fact, ImmutableArray, Lazy, LongVoyageRunnerTests, BalanceConfigurationException, ImmutableArray, JsonSerializerOptions (+10 more)

### Community 164 - "Contracts.Tests 164"
Cohesion: 0.17
Nodes (11): NotSupportedException, TradePriceStep, CancellationToken, Fact, IAsyncEnumerable, IEnumerable, JsonElement, Task (+3 more)

### Community 165 - "Performance 165"
Cohesion: 0.16
Nodes (9): CombatReportsNativeEvidence, BindingFlags, CancellationToken, Channel, IAsyncEnumerable, List, ValueTask, Factory (+1 more)

### Community 166 - "Contracts.Tests 166"
Cohesion: 0.13
Nodes (15): ImmutableArray, AiBaseMapData, AiMapEnvironmentSnapshot, EnvironmentFieldData, PointOfInterestData, TerritoryMapData, OrbitalElements, Fact (+7 more)

### Community 167 - "Engine simulation 167"
Cohesion: 0.18
Nodes (15): OrbitMapData, ImmutableArray, ClusterResourceBinding, ClusterStationData, ClusterTradeLink, StationClusterData, StationClusterMapSnapshot, ClusterGenerationConfig (+7 more)

### Community 168 - "Client verification 168"
Cohesion: 0.19
Nodes (9): Fact, H, VisibleRow, W, X, Y, ScenarioSelectLayoutTests, PanelLeft (+1 more)

### Community 169 - "Engine verification 169"
Cohesion: 0.26
Nodes (6): Fact, InlineData, SimulationEngine, Theory, CombatSaveLoadTests, Settings

### Community 170 - "Engine verification 170"
Cohesion: 0.22
Nodes (9): Fact, IEnumerable, InlineData, JsonObject, MemberData, Theory, StationMarketProfileContentTests, ProfilePath (+1 more)

### Community 171 - "Client interface 171"
Cohesion: 0.15
Nodes (16): Angle, Radius, Seed, CameraState, Dictionary, Id, IEnumerable, IReadOnlyList (+8 more)

### Community 172 - "Client verification 172"
Cohesion: 0.25
Nodes (8): predictor, buffer, Fact, InlineData, screen, SnapshotBuffer, Theory, InfoPanelTests

### Community 173 - "Client verification 173"
Cohesion: 0.27
Nodes (10): ImmutableArray, CombatPresentationFrame, CombatPresentationUpdate, CombatImpactSnapshot, Fact, Guid, InlineData, InvalidOperationException (+2 more)

### Community 174 - "Client interface 174"
Cohesion: 0.14
Nodes (20): SKCanvas, SKColor, SKFontStyleWeight, SKPaint, SKTypeface, Stopwatch, MenuStyle, BackgroundFill (+12 more)

### Community 175 - "Client verification 175"
Cohesion: 0.23
Nodes (9): CancellationToken, Fact, IAsyncEnumerable, List, Task, ValueTask, DialogueScreenTests, RecordingConnection (+1 more)

### Community 176 - "Engine simulation 176"
Cohesion: 0.12
Nodes (16): IReadOnlyList, CombatStateData, LauncherSaveData, ProjectileSaveData, IReadOnlyList, CountermeasureProjectileSaveData, CountermeasureStateData, DefenseLauncherSaveData (+8 more)

### Community 177 - "Client verification 177"
Cohesion: 0.28
Nodes (5): Fact, InlineData, Key, Theory, SpeedPanelTests

### Community 178 - "Engine verification 178"
Cohesion: 0.25
Nodes (5): ArgumentNullException, Fact, InlineData, Theory, TradeQuoteIssuanceTests

### Community 179 - "Client interface 179"
Cohesion: 0.13
Nodes (16): Bitmap, LinkedListNode, CharacterVisualState, Dictionary, Image, Key, LinkedList, Path (+8 more)

### Community 180 - "Combat report planning 180"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Авторитетный контракт боевого доклада, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Non-goals, Planned ticket map (+16 more)

### Community 181 - "Combat report planning 181"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Первый доклад о входе противника в дальность, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 182 - "Combat report planning 182"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Доклады экипажа о фактических пусках, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 183 - "Combat report planning 183"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Подтверждённые исходы и повреждения, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 184 - "Combat report planning 184"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Сохранение отметок и чистая загрузка, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 185 - "Combat report planning 185"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals, Planned ticket map (+16 more)

### Community 186 - "Combat report planning 186"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, История карточек и локализованные доклады, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 187 - "Combat report planning 187"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Графические доклады в стиле DSS, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 188 - "Combat report planning 188"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Карточки аккордеона и чтение докладов, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 189 - "Combat report planning 189"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Правая колонка и жизненный цикл экрана, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 190 - "Contracts.Tests 190"
Cohesion: 0.10
Nodes (15): DeepSpaceSaga.Contracts.Tests, AuthoritativeSnapshot, CombatJournalEntry, Fact, CountermeasureEventTests, AuthoritativeSnapshot, DialogueCommand, Fact (+7 more)

### Community 191 - "Client verification 191"
Cohesion: 0.16
Nodes (12): DeepSpaceSaga.Client.UI.Controls, SKCanvas, SKColor, SKPaint, SKRect, ProgressBar, Fact, InlineData (+4 more)

### Community 192 - "Client verification 192"
Cohesion: 0.23
Nodes (6): SKColor, SpaceMapColorResolver, Fact, SKColor, SpaceMapColorResolverTests, Fallback

### Community 193 - "Engine verification 193"
Cohesion: 0.33
Nodes (5): Fact, InlineData, JsonObject, Theory, ItemCatalogSchemaTests

### Community 194 - "Engine verification 194"
Cohesion: 0.20
Nodes (10): IReadOnlyList, PriceClampKind, Ceiling, Floor, None, StationPricing, Fact, InlineData (+2 more)

### Community 195 - "Engine verification 195"
Cohesion: 0.22
Nodes (7): ScenarioFile, MigratedTradingEconomyContinuation, Fact, Func, OverflowException, SimulationEngine, MarketRevisionTests

### Community 196 - "Engine simulation 196"
Cohesion: 0.25
Nodes (7): From, IReadOnlyDictionary, IReadOnlyList, To, X, Y, TradingMapGeometryGenerator

### Community 197 - "EconomyBalance.Tests 197"
Cohesion: 0.22
Nodes (10): Fact, ImmutableArray, InlineData, Theory, BalanceRunTests, Root, Scenario, Settings (+2 more)

### Community 198 - "Engine verification 198"
Cohesion: 0.40
Nodes (5): Fact, InlineData, SimulationEngine, Theory, RouteRiskIntegrationTests

### Community 199 - "03 Design 199"
Cohesion: 0.09
Nodes (18): Горизонтальные ориентиры, На что опирается конструкция, Пропорции женского лица: ориентиры и новый портрет в анфас, Изготовление совместимых компонентов, Проверка анфаса, Вертикальные ориентиры, Овал — объёмная анатомия, не эллипс, Что измерено на новом мастере (+10 more)

### Community 200 - "Client verification 200"
Cohesion: 0.22
Nodes (9): Placeholders, Dictionary, Lazy, Localization, Fact, InlineData, Key, Theory (+1 more)

### Community 201 - "Client verification 201"
Cohesion: 0.24
Nodes (10): CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, ControllableConnection, LastSpeed (+2 more)

### Community 202 - "Client interface 202"
Cohesion: 0.16
Nodes (13): SKBitmap, SKCanvas, SKColor, SKRect, GenericButtonTypeA, HasAssets, Fact, InlineData (+5 more)

### Community 203 - "Client interface 203"
Cohesion: 0.16
Nodes (14): AssemblyInformationalVersionAttribute, HashSet, IEnumerable, List, FrameStatistics, MapFrameContext, MapFrameCounts, MapFrameEvidence (+6 more)

### Community 204 - "Client interface 204"
Cohesion: 0.22
Nodes (12): Dictionary, Func, IEnumerable, LinkedList, SKBitmap, SKColor, CombatReportAssets, IllustrationCount (+4 more)

### Community 205 - "Client interface 205"
Cohesion: 0.28
Nodes (12): Bottom, Left, Right, Top, StationButton, Contracts, Finance, Hire (+4 more)

### Community 206 - "Contracts.Tests 206"
Cohesion: 0.21
Nodes (9): CargoStackSnapshot, DefenseLaunchPreview, ImmutableArray, InstalledModuleSnapshot, ModuleCommandSnapshot, Fact, DefenseLaunchPreviewTests, Fact (+1 more)

### Community 207 - "Engine verification 207"
Cohesion: 0.27
Nodes (7): Fact, InlineData, InvalidOperationException, SimulationEngine, Theory, DialogueTests, ContentPath

### Community 208 - "EconomyBalance.Tests 208"
Cohesion: 0.21
Nodes (8): Action, Fact, Func, ImmutableArray, JsonObject, BalanceReportTests, Output, Packaged

### Community 209 - "Engine verification 209"
Cohesion: 0.20
Nodes (8): Fact, AutomaticDefenseTests, Fact, CombatJournalTests, Fact, InlineData, Theory, CountermeasureResolutionTests

### Community 210 - "00 Process 210"
Cohesion: 0.09
Nodes (21): Сверка реализации — 2026-10-10, Architecture, Build, Test, Run, Client ↔ Engine data flow, Client navigation, Composition root (`Program.cs`), Content-driven simulation data, Current state (+13 more)

### Community 211 - "Release requirements 211"
Cohesion: 0.09
Nodes (22): 10. Проверки и доказательства, 11. Порядок реализации и Definition of Done, 1. Цель и ожидаемое поведение, Контекст кода для реализации, 2. Согласованное допущение о будущем, 3. Текущее устройство и причина дефекта, ТЗ: прогноз траекторий торпед и ПРО с учётом текущего манёвра цели, 4. Единое время и исходное состояние (+14 more)

### Community 212 - "Client verification 212"
Cohesion: 0.20
Nodes (11): Selected, CancellationToken, Fact, IAsyncEnumerable, List, Task, ValueTask, PoiMapSelectionTests (+3 more)

### Community 213 - "Client interface 213"
Cohesion: 0.23
Nodes (9): SKRect, SettingsButton, Exit, LanguageCombo, MonitorCombo, None, UiScaleCombo, SettingsLayout (+1 more)

### Community 214 - "EconomyBalance.Tests 214"
Cohesion: 0.36
Nodes (5): Fact, ImmutableArray, InlineData, Theory, StrategyBalanceEvaluatorTests

### Community 215 - "Engine verification 215"
Cohesion: 0.36
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, ApproachCommandTests

### Community 216 - "Engine verification 216"
Cohesion: 0.17
Nodes (11): Fact, SimulationEngine, AuthoritativeHostileAccessTests, Fact, ImmutableArray, InlineData, IReadOnlyList, Registry (+3 more)

### Community 217 - "Engine verification 217"
Cohesion: 0.27
Nodes (8): Command, Fact, InlineData, Result, SimulationEngine, Theory, CargoCostAccountingTests, Func

### Community 218 - "Engine verification 218"
Cohesion: 0.25
Nodes (6): Fact, CombatReactionDelayTests, Fact, InlineData, Theory, HostileTorpedoFireTests

### Community 219 - "Engine verification 219"
Cohesion: 0.25
Nodes (6): Fact, IEnumerable, ItemTypeId, Quantity, SimulationEngine, TradeSnapshotProjectionTests

### Community 220 - "Combat report planning 220"
Cohesion: 0.10
Nodes (17): Планируемые документы и ресурсы, EP-0010 — текущая документация и grounding, EP-0010 — документация и grounding, Grounding, Исторический grounding планирования, Исходный документ — история, superseded current summary выше, EP-0010 — инвентаризация влияния документации, Истории (+9 more)

### Community 221 - "Engine verification 221"
Cohesion: 0.29
Nodes (10): ChannelReader, Cts, LoopTask, Reader, CancellationToken, CancellationTokenSource, Fact, Task (+2 more)

### Community 222 - "Client interface 222"
Cohesion: 0.16
Nodes (11): DeepSpaceSaga.Client.UI.Screens.Defeat, IScreen, ScreenEvent, SKRect, DefeatLayout, Key, MouseButton, ScreenEvent (+3 more)

### Community 223 - "Release requirements 223"
Cohesion: 0.09
Nodes (20): Сверка реализации — 2026-10-10, Требования первого релиза, Уже существующая основа, Тактическая карта и маневрирование, Сближение Approach, Команды первого релиза, Цель, Актуализация EP-0004 — 2026-10-08 (+12 more)

### Community 224 - "Client interface 224"
Cohesion: 0.17
Nodes (17): Hover, Size, Dictionary, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+9 more)

### Community 225 - "Client interface 225"
Cohesion: 0.16
Nodes (9): GameTimeDisplay, GameCalendar, Fact, InlineData, Theory, GameTimeDisplayTests, InlineData, Theory (+1 more)

### Community 226 - "Client verification 226"
Cohesion: 0.24
Nodes (8): CancellationToken, Exception, Fact, IAsyncEnumerable, Task, ValueTask, FullMapRenderEvidenceTests, ThrowingConnection

### Community 227 - "Client interface 227"
Cohesion: 0.16
Nodes (15): CameraState, End, IReadOnlyList, List, SKPoint, Stack, ObjectTrailGeometry, Points (+7 more)

### Community 228 - "Client interface 228"
Cohesion: 0.18
Nodes (18): TacticalMapSettings, ClusterCellPixels, ClusterPpu, CompactMarkerPpu, FitPaddingPixels, GridBaseCellPixels, GridFadePixels, GridMinimumPixels (+10 more)

### Community 229 - "Snapshot contracts 229"
Cohesion: 0.16
Nodes (21): CombatCommandTypes, CountermeasureTerminationKind, Intercept, LifetimeExpired, MissedCoastExpired, OwnerLost, TargetLost, LaunchMode (+13 more)

### Community 230 - "Client verification 230"
Cohesion: 0.20
Nodes (11): NavigationPhase, Approach, EscapeDepart, EscapeTurn, CancellationToken, Fact, IAsyncEnumerable, Task (+3 more)

### Community 231 - "Client verification 231"
Cohesion: 0.22
Nodes (10): StationTravelResult, CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, DelayedTravelConnection (+2 more)

### Community 232 - "Engine simulation 232"
Cohesion: 0.23
Nodes (3): RngStreamNames, Fact, RngStreamNamesTests

### Community 233 - "EconomyBalance.Tests 233"
Cohesion: 0.37
Nodes (4): Fact, Func, ImmutableArray, MarketHealthEvaluatorTests

### Community 234 - "Client interface 234"
Cohesion: 0.16
Nodes (12): SKBitmap, SKCanvas, SKRect, ImagePanel, HasLoadedImage, SKBitmap, SKCanvas, SKPaint (+4 more)

### Community 235 - "Client interface 235"
Cohesion: 0.25
Nodes (10): IEnumerable, IReadOnlyList, List, CombatReportStore, ExpandedSequence, KeyboardSelection, Reports, ScrollOffset (+2 more)

### Community 236 - "Engine verification 236"
Cohesion: 0.30
Nodes (4): Fact, CommandDefinitionLoaderDirectoryTests, Fact, CommandDefinitionLoaderTests

### Community 237 - "Engine verification 237"
Cohesion: 0.27
Nodes (5): TradingEconomySaveMigration, Fact, InlineData, Theory, TradingEconomySaveSchemaTests

### Community 238 - "Client verification 238"
Cohesion: 0.22
Nodes (11): CancellationToken, GameSessionHandle, IAsyncEnumerable, Task, ValueTask, CombatReportsAcceptanceFixtures, Expected, Handle (+3 more)

### Community 239 - "Client verification 239"
Cohesion: 0.36
Nodes (8): Fact, H, SKBitmap, SKColor, W, X, Y, ScenarioSelectRowVisualsTests

### Community 240 - "Engine verification 240"
Cohesion: 0.38
Nodes (4): Fact, SimulationClock, SimulationEngine, DockCommandTests

### Community 241 - "Engine verification 241"
Cohesion: 0.27
Nodes (4): Fact, InlineData, Theory, ItemCatalogTests

### Community 242 - "Engine verification 242"
Cohesion: 0.25
Nodes (7): ArgumentException, Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageLedgerLifecycleTests

### Community 243 - "Engine simulation 243"
Cohesion: 0.21
Nodes (13): Pricing, TradePriceReason, ImmutableArray, IReadOnlyList, Pricing, SpreadReason, TradeQuoteCalculator, TradeQuoteDirection (+5 more)

### Community 244 - "Engine simulation 244"
Cohesion: 0.22
Nodes (11): IReadOnlyDictionary, StationSize, Huge, Large, Medium, Outpost, StationSizeFactors, Fact (+3 more)

### Community 245 - "Engine verification 245"
Cohesion: 0.34
Nodes (5): Fact, InlineData, ITestOutputHelper, Theory, CombatForecastTests

### Community 246 - "Engine verification 246"
Cohesion: 0.22
Nodes (9): Fact, InlineData, Theory, EpicSolarIntegrationTests, Fact, InlineData, SimulationEngine, Theory (+1 more)

### Community 247 - "Engine verification 247"
Cohesion: 0.23
Nodes (8): Final, Reports, BattleReportDamageSnapshot, BattleReportSnapshot, Fact, InvalidOperationException, CombatReportsEndToEndTests, CombatReportsScenarioFixtures

### Community 248 - "Client interface 248"
Cohesion: 0.23
Nodes (7): Dictionary, IReadOnlyList, PortraitAssetRepository, Parts, Root, Style, UnifiedPortraitAssets

### Community 249 - "Contracts.Tests 249"
Cohesion: 0.22
Nodes (7): Dictionary, ImmutableArray, Queue, SessionEventHistory, ShipEvent, Fact, ShipEventTests

### Community 250 - "Client verification 250"
Cohesion: 0.27
Nodes (7): Func, CombatReportFormatter, CombatReportPresentation, Fact, InlineData, Theory, CombatReportFormatterTests

### Community 251 - "Contracts.Tests 251"
Cohesion: 0.26
Nodes (9): ImmutableArray, HullCombatSnapshot, TorpedoRouteSegment, TorpedoSnapshot, TrailSegment, Fact, InlineData, Theory (+1 more)

### Community 252 - "Client verification 252"
Cohesion: 0.20
Nodes (9): Fact, CombatScenariosContentTests, Fact, InlineData, InvalidOperationException, SimulationEngine, Task, Theory (+1 more)

### Community 253 - "Engine verification 253"
Cohesion: 0.35
Nodes (7): Fact, Func, InlineData, Lazy, SimulationEngine, Theory, TorpedoLaunchTests

### Community 254 - "Engine verification 254"
Cohesion: 0.37
Nodes (4): Fact, InlineData, Theory, VoyageProfitAccountingTests

### Community 255 - "Engine simulation 255"
Cohesion: 0.31
Nodes (11): Disc, Point, ImmutableArray, IReadOnlyList, AiTradePlacementValidator, Disc, PlacementCheck, PlacementConnectivity (+3 more)

### Community 256 - "Release requirements 256"
Cohesion: 0.11
Nodes (17): 1. Причина проблемы, 2. Согласованные требования, 3.1. Продвижение Engine, 3.2. Данные боевых обновлений, 3. Техническое решение, 4.1. Единые часы сцены, 4.2. Попадания и промахи, 4.3. Короткие полёты и эффекты (+9 more)

### Community 257 - "03 Design 257"
Cohesion: 0.11
Nodes (17): Общие правила, Сверка реализации — 2026-10-10, Палитра, Цвета, Мышь, Клавиатура, Прогноз Approach, Актуализация EP-0004 — 2026-10-08 (+9 more)

### Community 258 - "Client verification 258"
Cohesion: 0.24
Nodes (7): IReadOnlyList, DateTime, SaveSlotInfo, Fact, SaveSlotsTests, Fact, SaveSlotInfoTests

### Community 259 - "Snapshot contracts 259"
Cohesion: 0.20
Nodes (17): BattleReportKind, EnemyInRange, EnemyShipHit, EnemyTorpedoIntercepted, EnemyTorpedoLaunch, PlayerCountermeasureLaunch, PlayerShipDamaged, PlayerTorpedoLaunch (+9 more)

### Community 260 - "Motion.Tests 260"
Cohesion: 0.25
Nodes (8): Func, X, Y, CollisionPath, TorpedoCollisionMath, CollisionPath, Fact, TorpedoCollisionTests

### Community 261 - "Client verification 261"
Cohesion: 0.42
Nodes (5): Fact, InlineData, Task, Theory, ManualDefenseUiFlowTests

### Community 262 - "Engine verification 262"
Cohesion: 0.34
Nodes (5): Fact, InlineData, SimulationEngine, Theory, CountermeasureLifetimeTests

### Community 263 - "PortraitAssets 263"
Cohesion: 0.28
Nodes (5): SKBitmap, SKColor, SKPoint3, SKRectI, WholeFaceAssets

### Community 264 - "Combat report planning 264"
Cohesion: 0.12
Nodes (16): Assumptions, Code context, Decisions, Definition of Done, Вручную выполнить evidence/NativeAcceptance.md и evidence/Performance.md. Только во время execution., Execution evidence and user constraint - 2026-10-16, Execution scope/order adjustment - 2026-10-16, Визуальная приёмка и frame-time evidence (+8 more)

### Community 265 - "Client interface 265"
Cohesion: 0.23
Nodes (9): DeepSpaceSaga.Client.UI.Screens.Station, ImmutableArray, StationRoutePresentation, StationRouteRow, Func, GameSessionHandle, Task, TaskCompletionSource (+1 more)

### Community 266 - "Engine verification 266"
Cohesion: 0.19
Nodes (9): DeepSpaceSaga.Engine.Dialogue, Action, List, SpaceObjectRuntime, StationSecuritySystem, Fact, InlineData, Theory (+1 more)

### Community 267 - "Contracts.Tests 267"
Cohesion: 0.23
Nodes (10): ImmutableArray, AsteroidSurveySnapshot, EngineManeuverSnapshot, ModuleHealthSnapshot, ResourceFractionSnapshot, ResourceSurveyReasonCodes, Fact, InlineData (+2 more)

### Community 268 - "Client verification 268"
Cohesion: 0.34
Nodes (6): Fact, HashSet, InlineData, SnapshotBuffer, Theory, GameSessionScaleFilterTests

### Community 269 - "Client verification 269"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SnapshotBuffer, Theory, GameSessionUiScaleTests

### Community 270 - "Engine verification 270"
Cohesion: 0.25
Nodes (6): Fact, CountermeasureBootstrapTests, Fact, InlineData, Theory, CountermeasureEndToEndTests

### Community 271 - "Engine verification 271"
Cohesion: 0.22
Nodes (11): Fact, InvalidOperationException, SimulationEngine, Task, ValueTask, Battle, Connection, DirectoryPath (+3 more)

### Community 272 - "Engine verification 272"
Cohesion: 0.37
Nodes (5): Fact, InlineData, SimulationEngine, Theory, VoyageFuelAccountingTests

### Community 273 - "Engine verification 273"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, VoyageSaveLoadContinuityTests

### Community 274 - "EconomyBalance 274"
Cohesion: 0.40
Nodes (5): BalanceCaseEvidence, ImmutableArray, BalanceLeader, BalanceStrategyIdentity, StrategyBalanceEvaluator

### Community 275 - "Contracts.Tests 275"
Cohesion: 0.18
Nodes (11): BattleReportVariant, BattleReportKind, BattleReportReporterRole, BattleReportSnapshot, CombatReportBatchSnapshot, Fact, IEnumerable, InlineData (+3 more)

### Community 276 - "Combat report planning 276"
Cohesion: 0.12
Nodes (15): Иллюстрации дальности и торпедных пусков, Artifact evidence - 2026-10-16, Assumptions, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 277 - "Combat report planning 277"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Иллюстрации ПРО, перехвата и попадания, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 278 - "Combat report planning 278"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Code context, Уничтожение противника и повреждение игрока, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 279 - "Client verification 279"
Cohesion: 0.27
Nodes (7): DeepSpaceSaga.Client.UI.Portraits, SKBitmap, DialoguePortraitComposer, Fact, InlineData, Theory, DialoguePortraitComposerTests

### Community 280 - "Release requirements 280"
Cohesion: 0.12
Nodes (15): Сверка реализации — 2026-10-10, Экономический интервал, Размер станции, Термины, Формула котировки, События, Склад и бюджет, Acceptance criteria (+7 more)

### Community 281 - "Engineering documentation 281"
Cohesion: 0.12
Nodes (15): 10. [P2] Очередь полных снимков не ограничена, 1. [P1] Продолжения асинхронных переходов меняют UI из фонового потока, 2. [P1] Охрана станции не учитывает действующий маршрут Approach, 3. [P1] Ошибка симуляции превращается в нормальное завершение потока снимков, 4. [P2] Save/Load меняет курс после отмены Approach, 5. [P2] Повторная доставка торговой команды повторно меняет мир, 6. [P2] Торговые операции допускают переполнение денег и массы, 7. [P2] Результаты команд теряются, если UI пропускает снимок (+7 more)

### Community 282 - "PortraitAssets 282"
Cohesion: 0.33
Nodes (4): Landmarks, SKBitmap, Landmarks, ReferenceFeatureAssets

### Community 283 - "Client verification 283"
Cohesion: 0.23
Nodes (9): Dictionary, SKBitmap, UiAssetLoader, Fact, MemberData, Theory, TheoryData, UiAssetLoaderTests (+1 more)

### Community 284 - "Client interface 284"
Cohesion: 0.23
Nodes (11): CameraState, ImmutableArray, IReadOnlyList, List, SKPath, CombatTrajectoryProjector, Geometry, EndMotionTimeMs (+3 more)

### Community 285 - "Contracts.Tests 285"
Cohesion: 0.23
Nodes (9): ImmutableArray, VoyagePhases, VoyageRouteOptionSnapshot, VoyageSnapshot, State, Fact, InlineData, Theory (+1 more)

### Community 287 - "Client verification 287"
Cohesion: 0.33
Nodes (6): Fact, InlineData, Theory, TemporaryContent, Path, TorpedoLauncherContentTests

### Community 288 - "Engine verification 288"
Cohesion: 0.21
Nodes (8): Fact, InlineData, Theory, CombatReportInterceptTests, InlineData, SimulationEngine, Theory, PhaseThreeMultiHostileTests

### Community 289 - "Engine verification 289"
Cohesion: 0.29
Nodes (7): Fact, InlineData, List, SimulationEngine, SpaceObjectRuntime, Theory, CombatReportRangeTests

### Community 290 - "Engine verification 290"
Cohesion: 0.33
Nodes (5): Fact, InlineData, SimulationEngine, Theory, CombatWreckTests

### Community 291 - "Engine verification 291"
Cohesion: 0.41
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PhaseThreeSaveContinuationTests

### Community 292 - "Engine verification 292"
Cohesion: 0.36
Nodes (6): Fact, Func, InlineData, SimulationEngine, Theory, PhaseThreeSaveSchemaTests

### Community 293 - "Engine verification 293"
Cohesion: 0.28
Nodes (8): Direction, Fact, ImmutableArray, InlineData, Theory, X, Y, TorpedoHistoryTests

### Community 294 - "Engine verification 294"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, TradingEconomyContinuityTests

### Community 295 - "Combat report planning 295"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, E01 по физическому состоянию мира, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 296 - "Combat report planning 296"
Cohesion: 0.13
Nodes (14): Assumptions, Capture/restore и bootstrap baseline, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 297 - "Combat report planning 297"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Общая геометрия Object Info и докладов (+6 more)

### Community 298 - "Combat report planning 298"
Cohesion: 0.13
Nodes (11): Correct the available map viewport after adding reports, Evidence, Scope and steps, Complete HUD reservation and synchronize affected test fixtures, Evidence, Exact scope, Clear unrelated HUD hover/press states when reports consume pointer input, Evidence (+3 more)

### Community 299 - "EconomyBalance.Tests 299"
Cohesion: 0.27
Nodes (8): DeepSpaceSaga.EconomyBalance.Tests, DeepSpaceSaga.EconomyBalance, Fact, LongVoyageDiagnosticsTests, ImmutableArray, ClusterEconomyAssessment, ClusterEconomyEvaluator, ClusterEconomyFinding

### Community 300 - "00 Process 300"
Cohesion: 0.13
Nodes (14): `00-Process/`, `01-Requirements/`, `02-FirstRelease/`, `03-Design/`, `04-Engineering/`, `05-Backlog/`, `06-Tooling/`, Documentation System (+6 more)

### Community 301 - "Client verification 301"
Cohesion: 0.18
Nodes (11): Effects, Max, Min, Priority, Profiles, Chance, Dictionary, Fact (+3 more)

### Community 302 - "Client interface 302"
Cohesion: 0.39
Nodes (7): pressedCount, releasedCount, Func, IKeyboard, Key, Span, KeyboardEdgeTracker

### Community 303 - "Client verification 303"
Cohesion: 0.33
Nodes (6): Fact, ImmutableArray, InlineData, Theory, DualDefensePanelLayoutTests, Modules

### Community 304 - "Snapshot contracts 304"
Cohesion: 0.26
Nodes (10): SessionOutcomeSnapshot, CanSave, Running, SaveDeniedReason, SessionOutcomeState, Defeated, Running, Fact (+2 more)

### Community 305 - "Engine simulation 305"
Cohesion: 0.40
Nodes (4): From, IReadOnlyDictionary, To, TradingMapDataValidation

### Community 307 - "Client verification 307"
Cohesion: 0.36
Nodes (5): Fact, SKBitmap, SKColor, SKRect, NinePatchTests

### Community 308 - "Client verification 308"
Cohesion: 0.29
Nodes (7): Fact, InlineData, Theory, TemporaryContent, Root, SettingsPath, TetrarchClassContentTests

### Community 309 - "Client verification 309"
Cohesion: 0.24
Nodes (8): CancellationToken, Func, IAsyncEnumerable, List, ValueTask, RecordingConnection, Commands, OnSend

### Community 311 - "Engine verification 311"
Cohesion: 0.34
Nodes (5): Fact, InlineData, JsonNode, Theory, CountermeasureContentLoaderTests

### Community 312 - "Engine verification 312"
Cohesion: 0.38
Nodes (5): Fact, InlineData, SimulationEngine, Theory, DualDefenseArbitrationTests

### Community 313 - "Engine verification 313"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, HostileOrbitRuntimeTests

### Community 314 - "Engine verification 314"
Cohesion: 0.41
Nodes (5): Fact, InlineData, SimulationEngine, Theory, MarketSaveLoadContinuityTests

### Community 315 - "Engine verification 315"
Cohesion: 0.39
Nodes (5): Fact, IEnumerable, InlineData, Theory, StationMarketEventCatalogTests

### Community 316 - "Combat report planning 316"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence — 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 317 - "Combat report planning 317"
Cohesion: 0.14
Nodes (13): Докладчики и безопасные имена участников, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 318 - "Combat report planning 318"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, E05 успешного перехвата, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 319 - "Combat report planning 319"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 320 - "Combat report planning 320"
Cohesion: 0.14
Nodes (13): Ограниченная история и раскрытая карточка, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 321 - "Combat report planning 321"
Cohesion: 0.14
Nodes (13): Assumptions, Ввод и навигация по карточкам, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 322 - "Combat report planning 322"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Чистая смена сессии и приоритет Defeat, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 323 - "Combat report planning 323"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 324 - "06 Tooling 324"
Cohesion: 0.14
Nodes (13): Артефакты, Сверка реализации — 2026-10-10, Результаты и ограничения, Пять проверенных трасс, Инструментарий и воспроизведение, Корпус и свежесть, EP-0007 — полный rebuild Graphify, operator-rating-launch (+5 more)

### Community 326 - "EP 0011 short fixes 326"
Cohesion: 0.17
Nodes (9): Истории, EP-0011 — Короткие фиксы, Scope и статус, Execution — 2026-10-18, US-0002 — Документация результата US-0001, Execution — 2026-10-18, Документация и Graphify, Scope revision before documentation work — 2026-10-18 (+1 more)

### Community 327 - "EP 0011 short fixes 327"
Cohesion: 0.15
Nodes (12): История наблюдений и определение тенденции, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 328 - "EP 0011 short fixes 328"
Cohesion: 0.15
Nodes (12): Интеграция наблюдаемого прогноза в карту, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 329 - "EP 0011 short fixes 329"
Cohesion: 0.15
Nodes (12): Регрессии и жизненный цикл прогноза, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 330 - "Engineering documentation 330"
Cohesion: 0.15
Nodes (12): Сверка реализации — 2026-10-10, EP-0002 implementation evidence, Актуализация EP-0004 — 2026-10-08, Epic integration review, US-0001 — seeded system start, US-0002 — coherent orbital world, US-0003 — playable asteroid belts, US-0004 — preserved scenario starts (+4 more)

### Community 331 - "05 Backlog 331"
Cohesion: 0.15
Nodes (12): ТЗ-02A: masterSeed для New Game и save/load continuation, ТЗ-03: загрузка всех тематических JSON из Settings.json, ТЗ-04: CommandResult и ShipEvent в authoritative snapshot, ТЗ-05: authoritative command inbox, validation и Deferred attempts, ТЗ-06: полный набор Engine commands первой итерации, ТЗ-07: стартовый корабль из 3 платформ и 12 occupied cells, ТЗ-08: fuel state for Engine module, ТЗ-09: ActiveCycle duration, command factors and fixed-point normalization (+4 more)

### Community 332 - "Validation 332"
Cohesion: 0.15
Nodes (12): Сверка реализации — 2026-10-10, EP-0003 implementation and review evidence, Актуализация EP-0004 — 2026-10-08, Final epic review and regression, US-0001 — Local trading cluster, US-0002 — Distinct orbital districts, US-0003 — Resource surroundings, US-0004 — Current travel estimates (+4 more)

### Community 333 - "Client interface 333"
Cohesion: 0.33
Nodes (7): SKPaintStyle, SKColor, SKFontStyleWeight, SKPaint, SKTextAlign, SKTypeface, XenonStyle

### Community 334 - "Contracts.Tests 334"
Cohesion: 0.27
Nodes (7): WeaponSkillType, CountermeasureDefense, TorpedoAttack, Fact, InlineData, Theory, WeaponOperatorSnapshotTests

### Community 335 - "Client verification 335"
Cohesion: 0.35
Nodes (6): Fact, InlineData, Task, Theory, PhaseThreeCombatInfoTests, Flight

### Community 336 - "Engine verification 336"
Cohesion: 0.35
Nodes (5): Fact, List, SimulationEngine, SpaceObjectRuntime, CombatReportLaunchTests

### Community 337 - "Engine verification 337"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, DefensePreviewProjectionTests

### Community 338 - "Engine verification 338"
Cohesion: 0.41
Nodes (5): Fact, Func, InlineData, Theory, RangedTorpedoLaunchTests

### Community 339 - "EP 0011 short fixes 339"
Cohesion: 0.17
Nodes (11): Входное техническое задание, Acceptance criteria, Approved ticket map, Assumptions, Decision / review log, Dependencies, Gaps and backlog, Implementation checkpoint — 2026-10-18 (+3 more)

### Community 340 - "Client interface 340"
Cohesion: 0.32
Nodes (9): CombatReportPresentation, DeepSpaceSaga.Client.UI.Screens.GameSession.Controls, CombatReportFormatter, CombatReportStore, ImmutableArray, SKPaint, SKRect, CombatReportCardLayout (+1 more)

### Community 341 - "Client verification 341"
Cohesion: 0.27
Nodes (7): DeepSpaceSaga.Client.UI.Screens.Dialogue, Fact, Func, GameSessionHandle, Task, ValueTask, DialogueIntegrationTests

### Community 342 - "Client verification 342"
Cohesion: 0.38
Nodes (4): DeepSpaceSaga.Client.UI.Screens.Settings, Fact, SKBitmap, SettingsScreenTests

### Community 343 - "05 Backlog 343"
Cohesion: 0.17
Nodes (11): ТЗ-01: CommandResult в authoritative snapshot, ТЗ-02: минимальный ShipEvent / watch-log DTO в snapshot, ТЗ-03: канонические Engine commands вместо `engine.cancel-all`, ТЗ-04: authoritative параметры target для match commands, ТЗ-05: загрузка `factory-types.json` и `recipes.json` из Settings, ТЗ-06: factor model для command definitions, ТЗ-07: fuel state для Engine module, ТЗ-08: стартовый корабль из 9 модулей и 12 occupied cells (+3 more)

### Community 344 - "Engine verification 344"
Cohesion: 0.47
Nodes (4): Fact, SimulationEngine, SpaceObjectRuntime, CombatReportIdentityTests

### Community 345 - "Engine verification 345"
Cohesion: 0.47
Nodes (3): Fact, SimulationEngine, ObjectImageResolutionTests

### Community 346 - "Engine verification 346"
Cohesion: 0.45
Nodes (4): Fact, SimulationEngine, Task, SaveLoadContinuityTests

### Community 347 - "PortraitAssets 347"
Cohesion: 0.38
Nodes (4): SKBitmap, SKPath, SKPoint, HairAssets

### Community 348 - "Motion 348"
Cohesion: 0.42
Nodes (4): Pose, Func, CurrentManeuverForecast, Pose

### Community 349 - "Client verification 349"
Cohesion: 0.47
Nodes (3): SKColor, Fact, GameSessionScreenTrailColorTests

### Community 350 - "Client verification 350"
Cohesion: 0.40
Nodes (5): Fact, InlineData, SnapshotBuffer, Theory, OrbitalClientPredictionTests

### Community 351 - "Client verification 351"
Cohesion: 0.36
Nodes (6): Fact, Fixture, InlineData, Task, Theory, RangedTorpedoUiTests

### Community 352 - "Client verification 352"
Cohesion: 0.53
Nodes (3): InlineData, Theory, TetrarchLoadoutTests

### Community 353 - "Engine verification 353"
Cohesion: 0.40
Nodes (5): Fact, InvalidOperationException, SimulationEngine, TargetInvocationException, CombatReportOutboxTests

### Community 354 - "Engine verification 354"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, HostileAttackAiTests

### Community 355 - "Engine verification 355"
Cohesion: 0.51
Nodes (3): Fact, SimulationEngine, PhaseThreeJournalTests

### Community 356 - "Engine verification 356"
Cohesion: 0.49
Nodes (3): Fact, SimulationEngine, PortFeeScheduleTests

### Community 357 - "Client interface 357"
Cohesion: 0.31
Nodes (6): Callback, SendOrPostCallback, ConcurrentQueue, State, WindowThreadContext, SynchronizationContext

### Community 358 - "Release requirements 358"
Cohesion: 0.20
Nodes (9): За рамками первого релиза, Команды, Требования первого релиза, Уже существующая основа, Asteroid ice stock, Добыча льда, Решение первого релиза, Параметры Drilling Unit (+1 more)

### Community 359 - "Release requirements 359"
Cohesion: 0.20
Nodes (9): Сверка реализации — 2026-10-10, Назначение, Архитектурные требования, За рамками первого релиза, Предварительные условия, Функциональность первого релиза, Статус реализации (MVP), Station (+1 more)

### Community 360 - "Engine verification 360"
Cohesion: 0.44
Nodes (4): InternalsVisibleToAttribute, Fact, SimulationEngine, EconomyBalanceSeamTests

### Community 361 - "Snapshot contracts 361"
Cohesion: 0.31
Nodes (7): ImmutableArray, JsonSerializerOptions, List, Type, Utf8JsonReader, Utf8JsonWriter, ImmutableArrayDefaultJsonConverter

### Community 362 - "Snapshot contracts 362"
Cohesion: 0.40
Nodes (7): SimulationSpeed, Speed0, Speed1, Speed2, Speed3, Speed4, SimulationSpeedExtensions

### Community 363 - "Engine simulation 363"
Cohesion: 0.24
Nodes (3): IReadOnlyList, CombatReportsSaveData, CombatReportsSaveValidation

### Community 365 - "Contracts.Tests 365"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, StationMarketKnowledgeSnapshotTests

### Community 366 - "Contracts.Tests 366"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, VoyageFinanceContractTests

### Community 367 - "Contracts.Tests 367"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, VoyageFuelContractTests

### Community 368 - "Engine verification 368"
Cohesion: 0.49
Nodes (4): Fact, InlineData, Theory, TradingContinuationReviewTests

### Community 369 - "Engine verification 369"
Cohesion: 0.47
Nodes (3): Fact, SimulationEngine, VoyageLifecycleTests

### Community 370 - "Client verification 370"
Cohesion: 0.33
Nodes (5): DeepSpaceSaga.Client.UI.Screens.ScenarioSelect, Fact, InlineData, Theory, ScenarioSelectScreenTests

### Community 371 - "Release requirements 371"
Cohesion: 0.22
Nodes (8): Требования к выполнению, Будущие экраны первого релиза, Проверка, Цель, Область применения, Уже существующие экраны, требующие переименования, Layout/helper classes, Screen Naming Convention Refactor

### Community 372 - "Client verification 372"
Cohesion: 0.64
Nodes (3): InlineData, Theory, CountermeasureContentTests

### Community 373 - "Client verification 373"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, DefeatScreenTests

### Community 374 - "Engine verification 374"
Cohesion: 0.47
Nodes (4): Fact, InlineData, Theory, CombatForecastLifetimeTests

### Community 380 - "Client interface 380"
Cohesion: 0.43
Nodes (6): Data, Lazy, Data, GameInfo, Title, Version

### Community 381 - "03 Design 381"
Cohesion: 0.25
Nodes (7): Геометрия и состояния, Характер, Шаблон для следующей кнопки, Палитра, Файлы и воспроизводимость, Смысл символов, Deep Space Saga — стиль кнопок тактической карты

### Community 383 - "Client verification 383"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, VoyageFuelContentTests

### Community 384 - "EP 0011 short fixes 384"
Cohesion: 0.29
Nodes (6): Решения пользователя, Реализация 2026-10-18 — supersedes planning-only scope, Технические предположения, Проверенные факты, EP-0011 — Grounding и технический контракт, Связь с требованиями / gaps

### Community 385 - "Release requirements 385"
Cohesion: 0.29
Nodes (6): Решения первого релиза, Требования первого релиза, Предварительные условия, Диалоги с представителями станций, Цель, MVP-вариант

### Community 386 - "Release requirements 386"
Cohesion: 0.29
Nodes (6): Назначение, За рамками первого релиза, Связанные механики, Функциональность первого релиза, Hire, Статус реализации (MVP)

### Community 387 - "Release requirements 387"
Cohesion: 0.29
Nodes (6): Примечания, Назначение, Оформление, Связанные механики, Функциональность первого релиза, MainMenu

### Community 388 - "Engineering documentation 388"
Cohesion: 0.29
Nodes (6): Проверки — 2026-10-10, Не выполнялось, Полная матрица экономической приёмки, Основная рабочая копия, Отдельная ветка EP-0004, Воспроизводимые probes

### Community 389 - "Snapshot contracts 389"
Cohesion: 0.52
Nodes (5): DialogueAction, Abort, Choose, Start, DialogueCommand

### Community 390 - "Contracts.Tests 390"
Cohesion: 0.43
Nodes (4): Fact, InstalledModuleSnapshot, ObjectMotionSnapshot, CombatForecastContractTests

### Community 391 - "Contracts.Tests 391"
Cohesion: 0.62
Nodes (3): AuthoritativeSnapshot, Fact, CombatReportBatchTests

### Community 392 - "Contracts.Tests 392"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, HostileCombatSnapshotTests

### Community 395 - "Motion.Tests 395"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, CombatRangeMathTests

### Community 396 - "Release requirements 396"
Cohesion: 0.33
Nodes (5): Решения первого релиза, Требования первого релиза, Экипаж корабля и диалоги, Требует решения, Цель

### Community 397 - "Release requirements 397"
Cohesion: 0.33
Nodes (5): Назначение, Связанные механики, Функциональность первого релиза, Contracts, Статус реализации (MVP)

### Community 398 - "Release requirements 398"
Cohesion: 0.33
Nodes (5): Назначение, Архитектурные требования, Оформление и локализация, Функциональность первого релиза, GameMenu

### Community 399 - "Release requirements 399"
Cohesion: 0.33
Nodes (5): Визуальный стиль, Назначение, Будущие кандидаты настроек, Функциональность первого релиза, Settings

### Community 400 - "Release requirements 400"
Cohesion: 0.33
Nodes (5): Назначение, Размер окна, Связанные механики, Функциональность первого релиза, Ship

### Community 401 - "Engineering documentation 401"
Cohesion: 0.33
Nodes (5): Автоматические проверки, Воспроизведение и причина, Проверка на сохранённых сценах, Изменения, Рывки карты: GPU-кэш масок, 24 сентября 2026

### Community 403 - "Client verification 403"
Cohesion: 0.40
Nodes (4): InlineData, Task, Theory, CountermeasureUiFlowTests

### Community 404 - "Client verification 404"
Cohesion: 0.60
Nodes (3): Fact, Task, SaveReliabilityTests

### Community 405 - "Engine verification 405"
Cohesion: 0.40
Nodes (4): InlineData, Task, Theory, PhaseThreeDeterminismTests

### Community 406 - "Release requirements 406"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Cargo

### Community 407 - "Release requirements 407"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Character Communication

### Community 408 - "Release requirements 408"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Dialog

### Community 409 - "Release requirements 409"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Loot

### Community 410 - "Engineering documentation 410"
Cohesion: 0.40
Nodes (4): Контроль производительности, Исправления по результатам ревью, Границы хранения и совместимость, Проверки

### Community 411 - "Engineering documentation 411"
Cohesion: 0.40
Nodes (4): Новое окно торговли, Код и данные, Проверка, Поведение

## Knowledge Gaps
- **1583 isolated node(s):** `CharacterPortraits`, `CombatCommandTypes`, `TorpedoLauncherCommandTypes`, `CommandReasonCodes`, `DefenseCommandTypes` (+1578 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 3031 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DeepSpaceSaga.Contracts` connect `Snapshot contracts 76` to `Client interface 0`, `Engine simulation 1`, `Engine verification 3`, `Engine verification 4`, `Engine verification 5`, `Engine simulation 6`, `Client verification 7`, `Engine simulation 8`, `Engine verification 9`, `Client verification 10`, `Client verification 11`, `Client interface 13`, `Client interface 14`, `Engine verification 15`, `Client verification 16`, `Client verification 18`, `Client interface 19`, `Engine verification 20`, `Engine verification 21`, `Client interface 22`, `Client verification 23`, `Client verification 24`, `Client interface 25`, `Client verification 26`, `Client verification 28`, `Client verification 29`, `Client verification 30`, `Client verification 31`, `Engine simulation 32`, `Client verification 33`, `Client verification 34`, `Client interface 36`, `Client verification 37`, `Client verification 38`, `Engine simulation 39`, `Client interface 41`, `Engine verification 42`, `Client interface 43`, `Motion.Tests 44`, `Client interface 45`, `Client verification 47`, `Client verification 49`, `EconomyBalance 50`, `Client verification 51`, `Client interface 52`, `Client verification 53`, `Client interface 54`, `Client interface 56`, `Motion.Tests 58`, `Engine verification 59`, `Client interface 60`, `Client verification 63`, `Engine verification 64`, `Performance 65`, `Client verification 66`, `Local session transport 67`, `Engine verification 68`, `Client interface 70`, `Client interface 71`, `Client interface 72`, `Client interface 73`, `Client verification 75`, `Client verification 78`, `Engine simulation 79`, `Client interface 82`, `Client interface 83`, `Engine simulation 84`, `Client verification 85`, `Client interface 86`, `Client interface 87`, `Motion.Tests 88`, `Client verification 89`, `Client verification 90`, `Client verification 93`, `Client interface 94`, `Client interface 96`, `Performance.Tests 97`, `Client verification 99`, `Engine verification 102`, `Engine verification 103`, `Client interface 104`, `Local session transport 106`, `Engine verification 107`, `Engine verification 108`, `Engine verification 111`, `Contracts.Tests 114`, `Engine simulation 116`, `Engine verification 117`, `Engine verification 118`, `Client interface 120`, `Client verification 122`, `Engine verification 126`, `Client verification 127`, `Client verification 129`, `Client verification 130`, `Engine simulation 131`, `Engine simulation 132`, `Engine verification 133`, `Engine verification 134`, `Engine verification 135`, `Client verification 136`, `Contracts.Tests 137`, `Client verification 138`, `EconomyBalance 139`, `Client verification 140`, `Motion 143`, `Client verification 144`, `Engine verification 145`, `Motion 148`, `Client verification 150`, `Engine simulation 154`, `Client verification 155`, `Motion.Tests 157`, `Engine verification 158`, `Client verification 159`, `Client verification 160`, `Engine verification 161`, `EconomyBalance 163`, `Contracts.Tests 164`, `Performance 165`, `Contracts.Tests 166`, `Engine simulation 167`, `Engine verification 169`, `Client interface 171`, `Client verification 173`, `Client verification 175`, `Engine simulation 176`, `Client verification 177`, `Engine verification 178`, `Contracts.Tests 190`, `Client verification 192`, `Engine verification 195`, `EconomyBalance.Tests 197`, `Engine verification 198`, `Client verification 201`, `Client interface 203`, `Client interface 204`, `Contracts.Tests 206`, `Engine verification 207`, `Engine verification 209`, `Client verification 212`, `EconomyBalance.Tests 214`, `Engine verification 215`, `Engine verification 216`, `Engine verification 217`, `Engine verification 218`, `Engine verification 219`, `Engine verification 221`, `Client interface 225`, `Client verification 226`, `Snapshot contracts 229`, `Client verification 230`, `Client verification 231`, `EconomyBalance.Tests 233`, `Client interface 235`, `Engine verification 237`, `Client verification 238`, `Client verification 239`, `Engine verification 240`, `Engine verification 242`, `Engine simulation 243`, `Engine verification 245`, `Engine verification 246`, `Engine verification 247`, `Contracts.Tests 249`, `Client verification 250`, `Contracts.Tests 251`, `Client verification 252`, `Engine verification 253`, `Engine verification 254`, `Engine simulation 255`, `Client verification 258`, `Snapshot contracts 259`, `Motion.Tests 260`, `Client verification 261`, `Engine verification 262`, `Client interface 265`, `Engine verification 266`, `Contracts.Tests 267`, `Client verification 268`, `Client verification 269`, `Engine verification 270`, `Engine verification 271`, `Engine verification 272`, `Engine verification 273`, `EconomyBalance 274`, `Client verification 279`, `Client interface 284`, `Contracts.Tests 285`, `Client verification 287`, `Engine verification 288`, `Engine verification 289`, `Engine verification 290`, `Engine verification 291`, `Engine verification 292`, `Engine verification 293`, `Engine verification 294`, `EconomyBalance.Tests 299`, `Client verification 303`, `Snapshot contracts 304`, `Engine verification 306`, `Contracts.Tests 310`, `Engine verification 312`, `Engine verification 313`, `Engine verification 314`, `Engine verification 315`, `Engine simulation 325`, `Contracts.Tests 334`, `Client verification 335`, `Engine verification 336`, `Engine verification 337`, `Engine verification 338`, `Client interface 340`, `Client verification 341`, `Engine verification 344`, `Engine verification 346`, `Motion 348`, `Client verification 350`, `Client verification 351`, `Client verification 352`, `Engine verification 353`, `Engine verification 354`, `Engine verification 355`, `Engine verification 356`, `Engine verification 360`, `Snapshot contracts 361`, `Snapshot contracts 362`, `Engine simulation 363`, `Engine simulation 364`, `Contracts.Tests 365`, `Contracts.Tests 366`, `Contracts.Tests 367`, `Engine verification 368`, `Engine verification 369`, `Client verification 372`, `Engine verification 374`, `Engine verification 375`, `Engine verification 376`, `Engine verification 377`, `Engine verification 378`, `Snapshot contracts 389`, `Contracts.Tests 392`, `Engine verification 393`, `Engine verification 394`, `Motion.Tests 395`, `Client verification 403`, `Client verification 404`, `Engine verification 405`, `Contracts.Tests 413`, `Engine verification 415`?**
  _High betweenness centrality (0.334) - this node is a cross-community bridge._
- **Why does `SimulationEngine` connect `Engine simulation 1` to `Engine verification 3`, `Engine verification 4`, `Engine verification 5`, `Engine simulation 6`, `Engine simulation 8`, `Engine verification 9`, `Client verification 11`, `Client interface 14`, `Engine verification 15`, `Client verification 16`, `Engine verification 21`, `Client verification 28`, `Client verification 31`, `Engine simulation 32`, `Client verification 33`, `Client verification 37`, `Engine simulation 39`, `Engine verification 42`, `Motion.Tests 44`, `Client verification 49`, `EconomyBalance 50`, `Client verification 53`, `Engine verification 59`, `Engine verification 64`, `Performance 65`, `Client verification 78`, `Engine simulation 84`, `Motion.Tests 88`, `Client verification 90`, `Client verification 93`, `Performance.Tests 97`, `Client interface 104`, `Local session transport 106`, `Contracts.Tests 114`, `Engine simulation 116`, `Engine verification 117`, `Engine verification 126`, `Client verification 130`, `Engine simulation 131`, `Engine simulation 132`, `Engine verification 134`, `Engine verification 135`, `Client verification 138`, `Engine verification 158`, `Engine verification 161`, `EconomyBalance 163`, `Performance 165`, `Contracts.Tests 166`, `Engine simulation 167`, `Client verification 173`, `Engine simulation 176`, `EconomyBalance.Tests 197`, `Engine verification 207`, `Engine verification 209`, `Engine verification 218`, `Engine verification 221`, `Client verification 230`, `Client verification 238`, `Engine verification 247`, `Contracts.Tests 249`, `Engine simulation 255`, `Client verification 261`, `Engine verification 270`, `Snapshot contracts 304`, `Engine verification 306`, `Engine verification 337`, `Engine verification 338`, `Motion 348`, `Snapshot contracts 362`, `Engine simulation 363`, `Client verification 372`, `Engine verification 376`, `Engine verification 377`, `Engine verification 378`, `Client verification 383`, `Snapshot contracts 389`, `Engine verification 393`, `Client verification 404`?**
  _High betweenness centrality (0.093) - this node is a cross-community bridge._
- **Why does `GameSessionScreen` connect `Client interface 0` to `Client verification 129`, `Engine verification 3`, `Client verification 261`, `Engine simulation 6`, `Client verification 7`, `Client verification 136`, `Client verification 138`, `Client verification 11`, `Client verification 268`, `Client interface 13`, `Client interface 14`, `Motion 143`, `Client verification 269`, `Client verification 140`, `Client verification 18`, `Client interface 19`, `Motion 148`, `Client verification 144`, `Client verification 150`, `Client verification 23`, `Client verification 26`, `Client verification 155`, `Client verification 28`, `Motion.Tests 157`, `Client verification 30`, `Client verification 31`, `Client verification 286`, `Client verification 159`, `Client verification 160`, `Client verification 37`, `Client verification 38`, `Performance 165`, `Client interface 171`, `Client verification 172`, `Client interface 46`, `Client verification 47`, `Client verification 175`, `Client verification 49`, `Client verification 177`, `Client verification 10`, `Client interface 52`, `Client verification 53`, `Client interface 54`, `Client interface 56`, `Client interface 62`, `Performance 65`, `Client verification 66`, `Client interface 69`, `Client interface 70`, `Client interface 71`, `Client interface 204`, `Client verification 78`, `Client verification 335`, `Client verification 212`, `Client verification 341`, `Client verification 89`, `Client verification 349`, `Client verification 350`, `Client verification 95`, `Client verification 93`, `Client interface 225`, `Client verification 226`, `Client verification 99`, `Client interface 227`, `Performance.Tests 97`, `Client verification 230`, `Client interface 104`, `Snapshot contracts 362`, `Client interface 235`, `Local session transport 106`, `Client verification 238`, `Client verification 113`, `Client verification 119`, `Client verification 250`, `Client interface 382`, `Client verification 127`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **Are the 116 inferred relationships involving `SimulationEngine` (e.g. with `.AiContentKeepsHumanMarkets()` and `.SavedLegacySessionDoesNotGenerate()`) actually correct?**
  _`SimulationEngine` has 116 INFERRED edges - model-reasoned connections that need verification._
- **Are the 87 inferred relationships involving `GameSessionScreen` (e.g. with `.AiBaseSelectableAndIdentified()` and `.HumanScientificMilitaryNotAi()`) actually correct?**
  _`GameSessionScreen` has 87 INFERRED edges - model-reasoned connections that need verification._
- **Are the 187 inferred relationships involving `ObjectMotionSnapshot` (e.g. with `.PrepareManualDefenseForecast()` and `.CaptureCombatState()`) actually correct?**
  _`ObjectMotionSnapshot` has 187 INFERRED edges - model-reasoned connections that need verification._
- **Are the 80 inferred relationships involving `AuthoritativeSnapshot` (e.g. with `.Coalesced_snapshots_preserve_first_receipt_and_expire_while_map_is_hidden()` and `.Pause_and_acceleration_do_not_change_effect_lifetime()`) actually correct?**
  _`AuthoritativeSnapshot` has 80 INFERRED edges - model-reasoned connections that need verification._