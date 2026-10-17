# DSS fresh structural and EP-0010 semantic graph

Fresh AST covers every selected .cs/.py file. Markdown headings and links are structural evidence. Fourteen curated EP-0010 mechanisms provide selective semantic coverage. Historical EP1-4 semantic fragments were replaced rather than silently reused. Token usage is unmeasured: template zeroes below are not measurements. No external API, game or native harness is run by this builder. Graph navigation does not establish runtime, FPS or human acceptance. HTML is an aggregated community view; JSON contains the complete graph. Recorded commit is pre-export HEAD; input hashes describe actual working-tree sources.

Reproduce from PowerShell: `& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe D:/DeepSpaceSaga/DSS/src/graphify-out/rebuild.py --force`. The explicit shrink override replaces historical semantic scope; inspect `semantic-coverage.json` before using it. `--dry-run` validates current source locations without exporting.

Audit: 992 unresolved AST endpoint edges were explicitly pruned; 2494 reference-only symbols have no local declaration location. The undirected export combines some relations and canonicalizes equivalent nodes; extraction.json retains all extracted relations. See diagnostics.json and the dedicated audit sidecars before interpreting absence of a path.

# Graph Report - DSS  (2026-10-16)

## Corpus Check
- 844 files · ~672,376 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 13087 nodes · 45407 edges · 416 communities (390 shown, 26 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 4956 edges (avg confidence: 0.83)
- Token cost: unmeasured host-agent usage (see cost.json)

## Graph Freshness
- Built from commit: `a1a4dd1f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Rebuild this selected corpus with `rebuild.py`; AST/Markdown extraction is offline and curated semantic evidence tokens are revalidated.

## Community Hubs (Navigation)
- Client interface 0
- Gameplay requirements 1
- Engine simulation 2
- Engine verification 3
- Client verification 4
- Engine simulation 5
- Client verification 6
- Client verification 7
- Client verification 8
- Engine verification 9
- Engine verification 10
- Client verification 11
- Engine simulation 12
- Client interface 13
- Client verification 14
- Engine verification 15
- Release requirements 16
- Client verification 17
- Client verification 18
- Engine verification 19
- Engine verification 20
- Client interface 21
- Engine verification 22
- Engine verification 23
- Client verification 24
- Client verification 25
- Engine verification 26
- Client verification 27
- Engine simulation 28
- Client interface 29
- Client interface 30
- Client interface 31
- Client interface 32
- Client verification 33
- Client verification 34
- Engine simulation 35
- Engine verification 36
- Client interface 37
- Client interface 38
- Engine simulation 39
- Client verification 40
- Engine verification 41
- Client interface 42
- Client verification 43
- Client interface 44
- Client interface 45
- Client verification 46
- Engine simulation 47
- Client interface 48
- Client verification 49
- Engine simulation 50
- EconomyBalance 51
- Release requirements 52
- Engine simulation 53
- Client interface 54
- Engine verification 55
- Motion.Tests 56
- Client interface 57
- Engine simulation 58
- Engine simulation 59
- Engine simulation 60
- Client interface 61
- Client interface 62
- Client interface 63
- Client interface 64
- Client interface 65
- Release requirements 66
- Client interface 67
- Contracts.Tests 68
- Client interface 69
- Engine verification 70
- Engine verification 71
- Client interface 72
- Client verification 73
- Client interface 74
- Client verification 75
- Client verification 76
- Client verification 77
- Engineering documentation 78
- Local session transport 79
- Client verification 80
- Client verification 81
- Engine simulation 82
- Release requirements 83
- Client verification 84
- Client verification 85
- Client interface 86
- Client verification 87
- Client interface 88
- Client verification 89
- Client verification 90
- Motion.Tests 91
- Engine verification 92
- Client verification 93
- Engine verification 94
- Client verification 95
- Engine verification 96
- Release requirements 97
- Motion 98
- Client verification 99
- Engine simulation 100
- Performance.Tests 101
- Client verification 102
- Client interface 103
- Release requirements 104
- Engine verification 105
- Engine simulation 106
- Motion 107
- Engine verification 108
- PortraitAssets 109
- Client verification 110
- Client verification 111
- Client verification 112
- Client interface 113
- Contracts.Tests 114
- Engine verification 115
- Client verification 116
- Client interface 117
- Engine simulation 118
- Client verification 119
- Engine verification 120
- Client interface 121
- 03 Design 122
- Engine verification 123
- Engine verification 124
- Engine verification 125
- Client verification 126
- Client interface 127
- Client interface 128
- Client verification 129
- Engine simulation 130
- EconomyBalance 131
- Client interface 132
- Client verification 133
- Contracts.Tests 134
- Engine verification 135
- Engine simulation 136
- Client verification 137
- Client verification 138
- CombatSim 139
- Gameplay requirements 140
- Client interface 141
- Client verification 142
- Motion.Tests 143
- Client verification 144
- Client verification 145
- Combat report planning 146
- Combat report planning 147
- Client verification 148
- Snapshot contracts 149
- Engine verification 150
- Client verification 151
- Client verification 152
- Engine verification 153
- Performance 154
- Release requirements 155
- Client verification 156
- Engine verification 157
- Client verification 158
- EconomyBalance 159
- Engine verification 160
- Engine verification 161
- Engine verification 162
- Client interface 163
- Client verification 164
- Client interface 165
- Client interface 166
- Contracts.Tests 167
- Engine verification 168
- Client verification 169
- Engine verification 170
- Performance 171
- Client interface 172
- Combat report planning 173
- Combat report planning 174
- Combat report planning 175
- Combat report planning 176
- Combat report planning 177
- Combat report planning 178
- Combat report planning 179
- Combat report planning 180
- Combat report planning 181
- Combat report planning 182
- Engine simulation 183
- Engine verification 184
- Engine verification 185
- Client verification 186
- Engine verification 187
- Engine verification 188
- Client verification 189
- 03 Design 190
- Client verification 191
- Client verification 192
- Client interface 193
- Client interface 194
- Client interface 195
- EconomyBalance.Tests 196
- Release requirements 197
- Client verification 198
- Client interface 199
- Client interface 200
- Client interface 201
- Contracts.Tests 202
- Snapshot contracts 203
- EconomyBalance.Tests 204
- EconomyBalance.Tests 205
- Engine verification 206
- Engine verification 207
- Engine verification 208
- Combat report planning 209
- Client interface 210
- 00 Process 211
- Client interface 212
- Client verification 213
- Client interface 214
- Client verification 215
- Client interface 216
- Client interface 217
- Snapshot contracts 218
- Client verification 219
- Local session transport 220
- EconomyBalance.Tests 221
- Client interface 222
- Client interface 223
- Engine verification 224
- Engine simulation 225
- Engine verification 226
- Client verification 227
- Engine verification 228
- Engine verification 229
- Client verification 230
- Client verification 231
- Client interface 232
- Client verification 233
- Engine verification 234
- EconomyBalance 235
- Client interface 236
- Client verification 237
- Snapshot contracts 238
- Engine simulation 239
- Engine verification 240
- Client verification 241
- Engine verification 242
- Engine verification 243
- Engine verification 244
- Contracts.Tests 245
- Engine simulation 246
- Engine verification 247
- Client verification 248
- Engine verification 249
- Motion.Tests 250
- Client verification 251
- Client verification 252
- Client verification 253
- Engine verification 254
- PortraitAssets 255
- Combat report planning 256
- Contracts.Tests 257
- Engine simulation 258
- Client verification 259
- Client verification 260
- Engine verification 261
- Engine verification 262
- Engine verification 263
- Engine verification 264
- Performance 265
- Combat report planning 266
- Combat report planning 267
- Combat report planning 268
- Client verification 269
- Release requirements 270
- Engineering documentation 271
- Engine simulation 272
- Engine verification 273
- PortraitAssets 274
- Client verification 275
- Client interface 276
- Contracts.Tests 277
- Contracts.Tests 278
- Client verification 279
- Client verification 280
- Engine verification 281
- Engine verification 282
- Engine verification 283
- Engine verification 284
- Engine verification 285
- Engine verification 286
- Engine verification 287
- Combat report planning 288
- Combat report planning 289
- Combat report planning 290
- Combat report planning 291
- EconomyBalance.Tests 292
- 00 Process 293
- Client verification 294
- Client interface 295
- Client verification 296
- Snapshot contracts 297
- Engine simulation 298
- Engine simulation 299
- Client verification 300
- Client verification 301
- Engine verification 302
- Engine verification 303
- Engine verification 304
- Engine verification 305
- Combat report planning 306
- Combat report planning 307
- Combat report planning 308
- Combat report planning 309
- Combat report planning 310
- Combat report planning 311
- Combat report planning 312
- Combat report planning 313
- 06 Tooling 314
- Engine verification 315
- Engine verification 316
- Release requirements 317
- Engineering documentation 318
- 05 Backlog 319
- Validation 320
- Client interface 321
- Client interface 322
- Client interface 323
- Contracts.Tests 324
- Engine verification 325
- Client verification 326
- Engine verification 327
- Engine verification 328
- Engine verification 329
- Engine verification 330
- Client verification 331
- 05 Backlog 332
- Engine simulation 333
- Client interface 334
- Client interface 335
- Client interface 336
- Snapshot contracts 337
- Snapshot contracts 338
- Engine verification 339
- Client verification 340
- Client verification 341
- Engine verification 342
- Engine verification 343
- Engine verification 344
- Engine verification 345
- Engine verification 346
- PortraitAssets 347
- Client interface 348
- Client verification 349
- Client verification 350
- Client verification 351
- Engine verification 352
- Engine verification 353
- Engine verification 354
- Release requirements 355
- Engine verification 356
- Snapshot contracts 357
- Snapshot contracts 358
- Engine verification 359
- Contracts.Tests 360
- Engine verification 361
- Engine verification 362
- Client verification 363
- Release requirements 364
- Release requirements 365
- Client interface 366
- Engine simulation 367
- Client verification 368
- Client verification 369
- Engine verification 370
- Engine verification 371
- Engine verification 372
- Engine verification 373
- Engine verification 374
- Engine verification 375
- Engine verification 376
- PortraitAssets 377
- Client interface 378
- 03 Design 379
- Client interface 380
- Client interface 381
- Client verification 382
- Client verification 383
- Release requirements 384
- Release requirements 385
- Release requirements 386
- Engine simulation 387
- Contracts.Tests 388
- Engine verification 389
- Engine verification 390
- Engine verification 391
- Release requirements 392
- Release requirements 393
- Release requirements 394
- Release requirements 395
- Release requirements 396
- Engineering documentation 397
- Client interface 398
- Client verification 399
- Engine verification 400
- Release requirements 401
- Release requirements 402
- Release requirements 403
- Release requirements 404
- Engineering documentation 405
- Engineering documentation 406
- Client verification 407
- Contracts.Tests 408
- Contracts.Tests 409
- Client interface 410
- Engine verification 411
- 00 Process 412
- 03 Design 413
- 06 Tooling 414
- Engine simulation 415

## God Nodes (most connected - your core abstractions)
1. `SimulationEngine` - 647 edges
2. `DeepSpaceSaga.Contracts` - 488 edges
3. `GameSessionScreen` - 481 edges
4. `AuthoritativeSnapshot` - 325 edges
5. `ObjectMotionSnapshot` - 319 edges
6. `ScenarioException` - 281 edges
7. `DeepSpaceSaga.Engine.Scenario` - 224 edges
8. `LinearMotionPredictor` - 224 edges
9. `SnapshotBuffer` - 192 edges
10. `DeepSpaceSaga.Client.Tests` - 181 edges

## Surprising Connections (you probably didn't know these)
- `Factory` --implements--> `IGameSessionFactory`  [EXTRACTED]
  tools/DeepSpaceSaga.Performance/CombatNativeEvidence.cs → src/DeepSpaceSaga.Client/IGameSessionFactory.cs
- `Factory` --implements--> `IGameSessionFactory`  [EXTRACTED]
  tools/DeepSpaceSaga.Performance/CombatReportsNativeEvidence.cs → src/DeepSpaceSaga.Client/IGameSessionFactory.cs
- `EvidenceFactory` --implements--> `IGameSessionFactory`  [EXTRACTED]
  tools/DeepSpaceSaga.Performance/SolarNativeEvidence.cs → src/DeepSpaceSaga.Client/IGameSessionFactory.cs
- `MalePortraitTests` --references--> `PortraitAssetRepository`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/MalePortraitTests.cs → src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs
- `PortraitGeneratorTests` --references--> `PortraitAssetRepository`  [EXTRACTED]
  tests/DeepSpaceSaga.Client.Tests/PortraitGeneratorTests.cs → src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Combat report delivery pipeline** — src_deepspacesaga_engine_simulationengine_combatreports_mechanism_outbox, src_deepspacesaga_engine_localclient_snapshotmailbox_mechanism_mailbox, src_deepspacesaga_client_snapshotbuffer_mechanism_buffer, src_deepspacesaga_client_combatreportinbox_mechanism_inbox, src_deepspacesaga_client_ui_screens_gamesession_combatreportstore_mechanism_history [INFERRED 0.85]

## Communities (416 total, 26 thin omitted)

### Community 0 - "Client interface 0"
Cohesion: 0.02
Nodes (163): CameraZoomTransition, DefenseSubmission, Encounter, FlightMapLabel, Kind, LabelWidth, Launcher, Lines (+155 more)

### Community 1 - "Gameplay requirements 1"
Cohesion: 0.01
Nodes (239): 10. Authoritative GameState, General JSON и Settings.json, 11. Сохранение, 12. Журналирование, 13. Критические ошибки и диагностика, 14. Идентификаторы объектов, 15. Генераторы случайных чисел, 16. Статус этой контрольной точки, 17. Раздельная архитектура движка и отрисовки (+231 more)

### Community 2 - "Engine simulation 2"
Cohesion: 0.03
Nodes (67): Events, IssuedQuote, MaximumBudget, Pose, QuoteId, RequestId, TradeCategory, ImmutableArray (+59 more)

### Community 3 - "Engine verification 3"
Cohesion: 0.05
Nodes (49): IDisposable, Func, IEnumerable, ImmutableArray, IReadOnlyList, Random, ResourceFieldAsteroidData, ResourceFieldCountData (+41 more)

### Community 4 - "Client verification 4"
Cohesion: 0.04
Nodes (61): DeepSpaceSaga.Client, SortedDictionary, BufferedSnapshot, PredictionDeltaMs, Dictionary, ImmutableArray, Queue, CombatReportInbox (+53 more)

### Community 5 - "Engine simulation 5"
Cohesion: 0.04
Nodes (85): CommandDefinitionDto, CommandDefinitionsFile, EconomyContentVersions, EngineSettingsFile, FactoryTypeDefinitionDto, FactoryTypesFile, Items, ItemTypeDefinitionDto (+77 more)

### Community 6 - "Client verification 6"
Cohesion: 0.08
Nodes (26): X, Y, SKCanvas, Action, DateTime, Fact, Func, H (+18 more)

### Community 7 - "Client verification 7"
Cohesion: 0.06
Nodes (36): DeepSpaceSaga.Motion.Tests, CameraState, List, X, Y, FutureTrajectoryProjector, ObjectMotionSnapshot, Span (+28 more)

### Community 8 - "Client verification 8"
Cohesion: 0.06
Nodes (43): Dictionary, IReadOnlyList, Label, List, SKBitmap, SKCanvas, SKPaint, SKRect (+35 more)

### Community 9 - "Engine verification 9"
Cohesion: 0.07
Nodes (30): Due, Ice, Pending, StationTravelCommand, FactoryTypeDefinition, ImmutableArray, RecipeDefinition, Steel (+22 more)

### Community 10 - "Engine verification 10"
Cohesion: 0.05
Nodes (45): NotSupportedException, Pricing, TradePriceReason, TradePriceStep, IReadOnlyList, PriceClampKind, Ceiling, Floor (+37 more)

### Community 11 - "Client verification 11"
Cohesion: 0.08
Nodes (26): EngineTradeConnection, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, List, SKBitmap (+18 more)

### Community 12 - "Engine simulation 12"
Cohesion: 0.06
Nodes (24): HostileLaunch, ManualDefenseContext, MarketRevision, QuoteContext, QuoteTerms, PlayerCommand, Func, CommandStartOutcome (+16 more)

### Community 13 - "Client interface 13"
Cohesion: 0.06
Nodes (43): DeepSpaceSaga.Client.UI.Screens.Load, H, SKRect, W, X, Y, LoadHit, LoadLayout (+35 more)

### Community 14 - "Client verification 14"
Cohesion: 0.09
Nodes (23): Enabled, CancellationToken, Dictionary, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, ImmutableArray (+15 more)

### Community 15 - "Engine verification 15"
Cohesion: 0.08
Nodes (17): RecipeMaterial, Fact, Func, IEnumerable, InlineData, IReadOnlyList, JsonObject, SimulationEngine (+9 more)

### Community 16 - "Release requirements 16"
Cohesion: 0.03
Nodes (79): Решения первого релиза, Требования первого релиза, Экипаж, пассажиры и жилой модуль, Цель, Актуализация EP-0008 — 2026-10-12, За рамками первого релиза, Требования первого релиза, Решение первого релиза (+71 more)

### Community 17 - "Client verification 17"
Cohesion: 0.06
Nodes (43): Callback, IOException, SendOrPostCallback, ConcurrentQueue, State, WindowThreadContext, NavigationPhase, Approach (+35 more)

### Community 18 - "Client verification 18"
Cohesion: 0.06
Nodes (21): SKPoint, SKRect, ObjectLabelGeometry, SKPoint, SKRect, SKSize, ObjectLabelLayout, ObjectLabelText (+13 more)

### Community 19 - "Engine verification 19"
Cohesion: 0.11
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, EngineCommandTests

### Community 20 - "Engine verification 20"
Cohesion: 0.06
Nodes (34): OrbitalMotionMath, Fact, IEnumerable, MemberData, Theory, ClusterCorrectnessCorpusTests, Fact, SimulationEngine (+26 more)

### Community 21 - "Client interface 21"
Cohesion: 0.05
Nodes (60): DisplayEntry, Maximum, Entry, Command, Dictionary, Id, IReadOnlyList, List (+52 more)

### Community 22 - "Engine verification 22"
Cohesion: 0.08
Nodes (20): CombatReportsSaveValidation, HashSet, IReadOnlyDictionary, JsonSerializerOptions, ScenarioException, ScenarioLoader, Fact, InlineData (+12 more)

### Community 23 - "Engine verification 23"
Cohesion: 0.07
Nodes (29): ChannelReader, Cts, LoopTask, Reader, HostileCombatPhase, FlankApproach, OrbitEntry, OrbitHold (+21 more)

### Community 24 - "Client verification 24"
Cohesion: 0.05
Nodes (48): IEnumerable, ImmutableArray, IReadOnlyList, List, SKCanvas, SKRect, CombatJournalPanel, Bounds (+40 more)

### Community 25 - "Client verification 25"
Cohesion: 0.11
Nodes (18): ActiveObjectId, SelectedObjectId, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, List (+10 more)

### Community 26 - "Engine verification 26"
Cohesion: 0.09
Nodes (12): CancellationToken, IAsyncEnumerable, Action, Fact, IEnumerable, InlineData, SimulationEngine, Task (+4 more)

### Community 27 - "Client verification 27"
Cohesion: 0.12
Nodes (17): Fact, GameSessionHandle, ImmutableArray, InlineData, RecordingConnection, SKBitmap, SKRect, SnapshotBuffer (+9 more)

### Community 28 - "Engine simulation 28"
Cohesion: 0.04
Nodes (37): CombatResult, DeepSpaceSaga.Contracts, DeepSpaceSaga.Engine.Scenario, DeepSpaceSaga.Engine.Combat, DeepSpaceSaga.Motion, DeepSpaceSaga.Engine, CharacterPortraits, NavigationComputerCommandTypes (+29 more)

### Community 29 - "Client interface 29"
Cohesion: 0.08
Nodes (30): DeepSpaceSaga.Client.UI.Screens.Finance, FinanceRow, FinanceLayout, ImmutableArray, IReadOnlyList, Key, MouseButton, SKBitmap (+22 more)

### Community 30 - "Client interface 30"
Cohesion: 0.08
Nodes (23): GL, GRBackendRenderTarget, GRContext, IInputContext, IMouse, RawImage, ScrollWheel, InterfaceLog (+15 more)

### Community 31 - "Client interface 31"
Cohesion: 0.07
Nodes (34): DeepSpaceSaga.Client.UI.Screens.Dialogue, Person, SKRect, DialogueLayout, GameSessionHandle, Key, MouseButton, Path (+26 more)

### Community 32 - "Client interface 32"
Cohesion: 0.09
Nodes (23): Alpha, RadiusMultiplier, ReticleGlow, SKCanvas, IReadOnlyList, SKCanvas, SKColor, SKImage (+15 more)

### Community 33 - "Client verification 33"
Cohesion: 0.10
Nodes (19): EconomicFixture, Fact, Func, H, InlineData, InvalidOperationException, ObjectDisposedException, OperationCanceledException (+11 more)

### Community 34 - "Client verification 34"
Cohesion: 0.05
Nodes (34): DeepSpaceSaga.Client.UI.Screens.GameSession.Controls, DeepSpaceSaga.Client.Tests, ModuleInitializer, CameraState, Dictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList (+26 more)

### Community 35 - "Engine simulation 35"
Cohesion: 0.07
Nodes (29): DefenseLaunch, Shot, DefenseSnapshot, PendingBurstShot, WeaponOperatorSnapshot, WeaponSkillType, CountermeasureDefense, TorpedoAttack (+21 more)

### Community 36 - "Engine verification 36"
Cohesion: 0.07
Nodes (37): ActualDays, StraightDays, InlineData, Task, Theory, Fact, InlineData, Theory (+29 more)

### Community 37 - "Client interface 37"
Cohesion: 0.08
Nodes (43): Button, CommandIconPair, Action, Dictionary, Func, HashSet, ImmutableArray, IReadOnlyDictionary (+35 more)

### Community 38 - "Client interface 38"
Cohesion: 0.07
Nodes (43): DeepSpaceSaga.Client.UI.Screens.Trade, InputFocus, QuoteKey, CancellationTokenSource, Entry, GameSessionHandle, IReadOnlyList, Key (+35 more)

### Community 39 - "Engine simulation 39"
Cohesion: 0.06
Nodes (44): DeepSpaceSaga.Engine.Dialogue, DialogueChoiceSnapshot, ImmutableDictionary, DialogueEvent, ImmutableArray, ImmutableDictionary, ImmutableHashSet, DialogueState (+36 more)

### Community 40 - "Client verification 40"
Cohesion: 0.07
Nodes (28): Fact, CombatReportHoverTests, CancellationToken, Channel, Fact, IAsyncEnumerable, Task, ValueTask (+20 more)

### Community 41 - "Engine verification 41"
Cohesion: 0.10
Nodes (26): DialogueAction, Abort, Choose, Start, DialogueCommand, Fact, ImmutableArray, InlineData (+18 more)

### Community 42 - "Client interface 42"
Cohesion: 0.08
Nodes (26): DeepSpaceSaga.Client.UI.Screens.MainMenu, Key, MouseButton, SKCanvas, SKPaint, SKRect, MainMenuScreen, SKRect (+18 more)

### Community 43 - "Client verification 43"
Cohesion: 0.10
Nodes (6): Fact, SKBitmap, SKColor, SKPaint, SKRect, StationToolbarTests

### Community 44 - "Client interface 44"
Cohesion: 0.08
Nodes (23): DeepSpaceSaga.Client.UI.Screens.GameMenu, SKRect, GameMenuButton, Load, MainMenu, None, Resume, Save (+15 more)

### Community 45 - "Client interface 45"
Cohesion: 0.07
Nodes (38): DeepSpaceSaga.Client.UI.Screens.GameSession, DateTimeOffset, SnapshotPrediction, SnapshotPrediction, AssemblyInformationalVersionAttribute, TacticalMapFrameProfile, TacticalMapFrameRecorder, TacticalMapProfileCapture (+30 more)

### Community 46 - "Client verification 46"
Cohesion: 0.07
Nodes (32): CombatEffect, Func, HashSet, ImmutableArray, IReadOnlyList, List, CombatEffect, TerminalTrail (+24 more)

### Community 47 - "Engine simulation 47"
Cohesion: 0.08
Nodes (35): CommandDefinition, Action, HashSet, ImmutableArray, GameDataRegistry, CatalogCompatibility, CatalogVersion, CombatRules (+27 more)

### Community 48 - "Client interface 48"
Cohesion: 0.09
Nodes (29): FileNotFoundException, Action, Image, Key, Label, List, MouseButton, Rect (+21 more)

### Community 49 - "Client verification 49"
Cohesion: 0.11
Nodes (19): NavigationTrajectoryProjector, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, IReadOnlyList, List (+11 more)

### Community 50 - "Engine simulation 50"
Cohesion: 0.08
Nodes (23): InterceptionMath, IReadOnlyList, CombatSaveValidation, CombatStateData, LauncherSaveData, ProjectileSaveData, IReadOnlyList, CountermeasureProjectileSaveData (+15 more)

### Community 51 - "EconomyBalance 51"
Cohesion: 0.10
Nodes (35): Category, Action, Command, Dictionary, Func, IEnumerable, ImmutableArray, ItemId (+27 more)

### Community 52 - "Release requirements 52"
Cohesion: 0.04
Nodes (51): Сверка реализации — 2026-10-10, Требования первого релиза, Минимальные товары-кандидаты, Уже существующая основа, Грузовой отсек, Требует решения, Решение первого релиза, Цель (+43 more)

### Community 53 - "Engine simulation 53"
Cohesion: 0.07
Nodes (17): StationEventData, StationMarketEventItemEffectData, StationMarketEventRouteEffectData, TradingMapEdgeData, TradingMapStateData, IReadOnlyList, StationEventRuntime, ImmutableArray (+9 more)

### Community 54 - "Client interface 54"
Cohesion: 0.13
Nodes (14): DeepSpaceSaga.Client.UI.Controls, TradeItemPresentation, SKCanvas, SKColor, SKPaint, SKRect, SKTextAlign, SKTypeface (+6 more)

### Community 55 - "Engine verification 55"
Cohesion: 0.12
Nodes (14): Fact, ScenarioArmorSchemaTests, ConcurrentQueue, Fact, CounterTraceListener, Messages, TradingMapSchemaTests, Fact (+6 more)

### Community 56 - "Motion.Tests 56"
Cohesion: 0.12
Nodes (11): ApproachFlyThroughPlan, FamilyFormula, Plan, Fact, InlineData, List, Theory, Time (+3 more)

### Community 57 - "Client interface 57"
Cohesion: 0.12
Nodes (18): IReadOnlyList, SKCanvas, SKColor, SKPaint, SKRect, GridPanel, GridSortColumn, BuyingCount (+10 more)

### Community 58 - "Engine simulation 58"
Cohesion: 0.07
Nodes (32): IReadOnlyList, CargoStackData, EngineIdentityCountersData, FocusData, GameStateData, MotionTimeMs, HullCellCoordinate, HullLayoutData (+24 more)

### Community 59 - "Engine simulation 59"
Cohesion: 0.08
Nodes (28): Consumed, Cost, Reserved, ImmutableArray, VoyageCargoRemainderSnapshot, VoyageFinanceSnapshot, VoyageFinanceStates, VoyageFuelSettlementSnapshot (+20 more)

### Community 60 - "Engine simulation 60"
Cohesion: 0.08
Nodes (19): MarketEventFlow, MarketItemLimits, ImmutableArray, StationMarketKnowledgeSnapshot, StationMarketStockBandSnapshot, IReadOnlyDictionary, StationSize, Huge (+11 more)

### Community 61 - "Client interface 61"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Hire, HireLayout, Key, MouseButton, SnapshotBuffer, HireScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 62 - "Client interface 62"
Cohesion: 0.09
Nodes (24): DeepSpaceSaga.Client.UI.Screens.Save, Action, Func, H, IReadOnlyList, Key, MouseButton, SKCanvas (+16 more)

### Community 63 - "Client interface 63"
Cohesion: 0.08
Nodes (46): ImmutableSortedDictionary, Dictionary, CharacterAppearance, Age, Colors, Gender, LibraryVersion, Parts (+38 more)

### Community 64 - "Client interface 64"
Cohesion: 0.11
Nodes (22): ActiveObjectId, CancellationToken, CancellationTokenSource, Channel, Exception, SelectedObjectId, Task, TradeJournal (+14 more)

### Community 65 - "Client interface 65"
Cohesion: 0.08
Nodes (42): Key, MouseButton, IScreen, ScreenEvent, CloseContracts, CloseDialogue, CloseFinance, CloseHire (+34 more)

### Community 66 - "Release requirements 66"
Cohesion: 0.04
Nodes (44): Сверка реализации — 2026-10-10, Назначение, Размер окна, Связанные механики, Функциональность первого релиза, Finance, Статус реализации (MVP), Сверка реализации — 2026-10-10 (+36 more)

### Community 67 - "Client interface 67"
Cohesion: 0.13
Nodes (22): H, SKRect, W, X, Y, SaveHit, SaveLayout, ListWidth (+14 more)

### Community 68 - "Contracts.Tests 68"
Cohesion: 0.08
Nodes (17): DeepSpaceSaga.Contracts.Tests, Fact, ArchitectureTests, Fact, InlineData, Theory, HostileCombatSnapshotTests, Fact (+9 more)

### Community 69 - "Client interface 69"
Cohesion: 0.09
Nodes (22): PoiGeometry, Stopwatch, PauseResumeDiagnostics, CameraState, IEnumerable, IReadOnlyList, SKCanvas, SKPath (+14 more)

### Community 70 - "Engine verification 70"
Cohesion: 0.21
Nodes (11): ArgumentNullException, Fact, ImmutableDictionary, InlineData, InstalledModuleRuntime, IReadOnlyList, SimulationEngine, SpaceObjectRuntime (+3 more)

### Community 71 - "Engine verification 71"
Cohesion: 0.15
Nodes (12): Fact, InlineData, Theory, ReviewRegressionTests, Fact, IEnumerable, InlineData, ItemTypeId (+4 more)

### Community 72 - "Client interface 72"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Contracts, ContractsLayout, Key, MouseButton, SnapshotBuffer, ContractsScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 73 - "Client verification 73"
Cohesion: 0.15
Nodes (13): pressedCount, releasedCount, Func, IKeyboard, Key, Span, KeyboardEdgeTracker, Fact (+5 more)

### Community 74 - "Client interface 74"
Cohesion: 0.10
Nodes (21): Row, SKRect, GameSessionHandle, ImmutableArray, Key, SKCanvas, SKRect, SnapshotBuffer (+13 more)

### Community 75 - "Client verification 75"
Cohesion: 0.14
Nodes (22): ControlledConnection, TradeQuoteRequest, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer (+14 more)

### Community 76 - "Client verification 76"
Cohesion: 0.11
Nodes (25): DeepSpaceSaga.Client.UI.Screens.Station, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task (+17 more)

### Community 77 - "Client verification 77"
Cohesion: 0.15
Nodes (14): DeepSpaceSaga.Client.UI.Screens, IEnumerable, Stack, ScreenStack, Count, Current, UnderCurrent, Fact (+6 more)

### Community 78 - "Engineering documentation 78"
Cohesion: 0.05
Nodes (39): Требования первого релиза, Approach: уточнение от 2026-09-20, Командные панели корабля, Цель, Engine, Актуализация EP-0008 — 2026-10-12, Актуализация EP-0009 — 2026-10-12, Maneuver (+31 more)

### Community 79 - "Local session transport 79"
Cohesion: 0.10
Nodes (23): CancellationToken, CancellationTokenSource, IAsyncEnumerable, SemaphoreSlim, SimulationEngine, ValueTask, LocalGameSessionConnection, MasterSeedWasMissingOnLoad (+15 more)

### Community 80 - "Client verification 80"
Cohesion: 0.15
Nodes (20): CancellationToken, Fact, Fixture, Func, GameSessionHandle, IAsyncEnumerable, InlineData, List (+12 more)

### Community 81 - "Client verification 81"
Cohesion: 0.12
Nodes (24): CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, List, SKBitmap, SKCanvas (+16 more)

### Community 82 - "Engine simulation 82"
Cohesion: 0.11
Nodes (9): DeepSpaceSaga.Engine.Rng, ModuleHitLocationRng, Counter, State, Random, RngStreamNames, RngStreamSeedDerivation, Fact (+1 more)

### Community 83 - "Release requirements 83"
Cohesion: 0.05
Nodes (39): Сверка реализации — 2026-10-10, Размер станции, Склад станции и локальный рынок, Ограничения торговли, Стартовая станция, Цена и состояние запаса, Модель станции, Требования к сохранению (+31 more)

### Community 84 - "Client verification 84"
Cohesion: 0.23
Nodes (5): MouseButton, Fact, InlineData, Theory, StationScreenTests

### Community 85 - "Client verification 85"
Cohesion: 0.07
Nodes (16): DeepSpaceSaga.Engine.Content, FireModeMath, FactorDefaults, Fact, AbandonedObjectContentTests, Fact, EnvironmentFieldContentTests, Fact (+8 more)

### Community 86 - "Client interface 86"
Cohesion: 0.12
Nodes (17): DeepSpaceSaga.Client.UI.Screens.Ship, Bottom, Left, Right, Top, ShipButton, Close, None (+9 more)

### Community 87 - "Client verification 87"
Cohesion: 0.23
Nodes (6): Fact, IReadOnlyList, FakeMotionPredictor, FakeTimestamp, Timestamp, ObjectTrailStoreTests

### Community 88 - "Client interface 88"
Cohesion: 0.11
Nodes (20): ScenarioRow, Func, H, IReadOnlyList, Key, Label, List, MouseButton (+12 more)

### Community 89 - "Client verification 89"
Cohesion: 0.18
Nodes (8): CameraState, FocusX, FocusY, PixelsPerWorldUnit, Fact, CameraStateTests, Fact, CameraStateZoomTests

### Community 90 - "Client verification 90"
Cohesion: 0.12
Nodes (17): Fact, InlineData, Theory, CombatTrajectoryTests, Snapshot, Fact, InlineData, Theory (+9 more)

### Community 91 - "Motion.Tests 91"
Cohesion: 0.17
Nodes (13): ApproachRoute, DurationMs, Length, Direction, X, Y, ApproachLineCaptureMath, X (+5 more)

### Community 92 - "Engine verification 92"
Cohesion: 0.16
Nodes (12): ScenarioMetadata, StartingScenario, Fact, ImmutableDictionary, InlineData, IReadOnlyList, Theory, StartingScenario (+4 more)

### Community 93 - "Client verification 93"
Cohesion: 0.19
Nodes (5): Fact, GameSessionScalePanelTests, Fact, GameSessionZoomTests, LogTailReader

### Community 94 - "Engine verification 94"
Cohesion: 0.12
Nodes (14): Fact, SimulationEngine, AuthoritativeHostileAccessTests, Fact, MovingTerritoryDataTests, Fact, InlineData, SimulationEngine (+6 more)

### Community 95 - "Client verification 95"
Cohesion: 0.11
Nodes (23): After, Before, English, ExpectedEconomy, FirstHour, ProfileId, Russian, StationId (+15 more)

### Community 96 - "Engine verification 96"
Cohesion: 0.09
Nodes (18): DeepSpaceSaga.Engine.Tests, ClusterLocalResumeTests, Settings, Fact, InlineData, Theory, CountermeasureSaveLoadTests, Fact (+10 more)

### Community 97 - "Release requirements 97"
Cohesion: 0.05
Nodes (38): 10. Проверка интересности торговой карты, 11. Информация, доступная игроку, 12. Рекомендуемая нарезка на пользовательские истории, 13. Definition of Done для всей инициативы, 1. Цель MVP, Сверка реализации — 2026-10-10, 2. Жёсткие границы MVP, 3. Игровой экономический интервал (+30 more)

### Community 98 - "Motion 98"
Cohesion: 0.15
Nodes (15): First, ReadOnlySpan, Second, ClosestX, ClosestY, IsArrived, Span, ApproachFlyThroughPlan (+7 more)

### Community 99 - "Client verification 99"
Cohesion: 0.14
Nodes (17): IDictionary, Fact, Func, GameSessionHandle, InlineData, Task, Theory, ValueTask (+9 more)

### Community 100 - "Engine simulation 100"
Cohesion: 0.12
Nodes (20): IEnumerable, IEnumerable, ImmutableArray, StationMarketEventCatalog, StationMarketEventDefinition, StationMarketEventIds, All, StationMarketEventItemEffectDefinition (+12 more)

### Community 101 - "Performance.Tests 101"
Cohesion: 0.11
Nodes (16): DeepSpaceSaga.Performance.Tests, IEnumerable, JsonElement, SolarMapEvidence, Fact, ClusterPerformanceReportTests, Root, Fact (+8 more)

### Community 102 - "Client verification 102"
Cohesion: 0.14
Nodes (14): predictor, Fact, CombatPaletteContentTests, InlineData, Task, Theory, CountermeasureUiFlowTests, buffer (+6 more)

### Community 103 - "Client interface 103"
Cohesion: 0.13
Nodes (22): Belt, Lod, CameraState, Dictionary, SKCanvas, SKPath, SKRect, X (+14 more)

### Community 104 - "Release requirements 104"
Cohesion: 0.05
Nodes (36): 10. Генерация новой игры, 11. Интеграция стартовых сценариев, 12. Полностью открытая карта и будущий туман войны, 13. Отображение и взаимодействие, 14. Архитектурные границы и модель данных, 15. Сохранение и продолжение, 16.1. Расхождения, которые нужно разрешить при подготовке эпика, 16. Проверенная основа и необходимые изменения (+28 more)

### Community 105 - "Engine verification 105"
Cohesion: 0.13
Nodes (21): Action, Command, Func, IReadOnlyList, Item, List, Quantity, Result (+13 more)

### Community 106 - "Engine simulation 106"
Cohesion: 0.10
Nodes (18): ModuleHitOutcome, BattleReportReporterSnapshot, LauncherCombatSnapshot, Id, Name, IReadOnlyDictionary, IReadOnlyList, ModuleId (+10 more)

### Community 107 - "Motion 107"
Cohesion: 0.17
Nodes (9): ClosestX, ClosestY, IsArrived, NavigationStepResult, NavigationWaypointMath, StagedNavigationStepResult, StagedNavigationStepResult, Fact (+1 more)

### Community 108 - "Engine verification 108"
Cohesion: 0.19
Nodes (10): Fact, IEnumerable, InlineData, ItemTypeId, Quantity, Registry, SimulationEngine, SpaceObjectRuntime (+2 more)

### Community 109 - "PortraitAssets 109"
Cohesion: 0.13
Nodes (13): FaceAssets, Dictionary, Left, Right, SKBitmap, SKPoint, SKPoint3, FaceEyeAssets (+5 more)

### Community 110 - "Client verification 110"
Cohesion: 0.12
Nodes (17): ExpectedProfile, IAsyncEnumerator, CancellationToken, Fact, Func, IReadOnlyDictionary, ItemId, MemberData (+9 more)

### Community 111 - "Client verification 111"
Cohesion: 0.16
Nodes (15): Key, SKCanvas, SKPaint, SKRect, Stopwatch, TextInputBox, CaretVisible, Length (+7 more)

### Community 112 - "Client verification 112"
Cohesion: 0.16
Nodes (10): IReadOnlyList, SKCanvas, SKColor, SKPaint, GridRenderer, Fact, InlineData, SKBitmap (+2 more)

### Community 113 - "Client interface 113"
Cohesion: 0.16
Nodes (14): CombatReportAssets, CombatReportFormatter, CombatReportStore, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+6 more)

### Community 114 - "Contracts.Tests 114"
Cohesion: 0.15
Nodes (14): ImmutableArray, CommandReasonCodes, CommandResult, CommandResultStatus, Cancelled, Deferred, Executed, Failed (+6 more)

### Community 115 - "Engine verification 115"
Cohesion: 0.13
Nodes (15): Fact, InlineData, Theory, GeneratedWorldPersistenceTests, Fact, InlineData, SimulationEngine, Theory (+7 more)

### Community 116 - "Client verification 116"
Cohesion: 0.12
Nodes (18): DeepSpaceSaga.Client.UI.Screens.TempCharacterImage, DeepSpaceSaga.Client.Portraits, JsonSerializerOptions, AppearanceSerializer, PortraitGenerator, PortraitRandom, Fact, Lazy (+10 more)

### Community 117 - "Client interface 117"
Cohesion: 0.14
Nodes (18): LabelMetrics, Dictionary, Func, HashSet, IReadOnlyDictionary, IReadOnlyList, IReadOnlySet, List (+10 more)

### Community 118 - "Engine simulation 118"
Cohesion: 0.19
Nodes (16): Dictionary, From, HashSet, IEnumerable, IReadOnlyDictionary, IReadOnlyList, To, TradingGraphGenerator (+8 more)

### Community 119 - "Client verification 119"
Cohesion: 0.19
Nodes (10): ArgumentException, Fact, InlineData, SKBitmap, SKCanvas, Theory, X, Y (+2 more)

### Community 120 - "Engine verification 120"
Cohesion: 0.24
Nodes (10): Fact, InlineData, JsonNode, JsonObject, Theory, CombatContentLoaderTests, ClassesPath, ModulesPath (+2 more)

### Community 121 - "Client interface 121"
Cohesion: 0.15
Nodes (13): DeepSpaceSaga.Client.UI.Assets, SKBitmap, SKCanvas, SKPaint, SKRect, XenonComboBox, HasAssets, Action (+5 more)

### Community 122 - "03 Design 122"
Cohesion: 0.06
Nodes (30): Сверка реализации — 2026-10-10, Стыковка, Решения первого релиза, Требования первого релиза, Цель, Актуализация EP-0004 — 2026-10-08, Статус реализации (MVP), Общие правила (+22 more)

### Community 123 - "Engine verification 123"
Cohesion: 0.19
Nodes (8): TradingMapLinkData, ArgumentOutOfRangeException, Fact, HashSet, ImmutableDictionary, IReadOnlyDictionary, IReadOnlyList, TradingGraphGeneratorTests

### Community 124 - "Engine verification 124"
Cohesion: 0.20
Nodes (8): Fact, InlineData, Theory, MarketKnowledgeTests, Fact, InlineData, Theory, RepeatableTradingVoyageTests

### Community 125 - "Engine verification 125"
Cohesion: 0.19
Nodes (13): DeepSpaceSaga.Engine.Trading, From, ImmutableArray, IReadOnlyList, To, EffectiveTradingRoute, TradingRouteEvaluator, TradingRouteModifier (+5 more)

### Community 126 - "Client verification 126"
Cohesion: 0.13
Nodes (20): IAsyncDisposable, CancellationToken, Fact, Func, GameSessionHandle, IAsyncEnumerable, InlineData, List (+12 more)

### Community 127 - "Client interface 127"
Cohesion: 0.15
Nodes (17): AssemblyInformationalVersionAttribute, Channel, Dictionary, Exception, IReadOnlyList, JsonSerializerOptions, SemaphoreSlim, Task (+9 more)

### Community 128 - "Client interface 128"
Cohesion: 0.18
Nodes (12): SKBitmap, SKCanvas, SKColor, SKPaint, SKRect, StationToolbar, HasLoadedCrewImage, HasLoadedExitButtonImage (+4 more)

### Community 129 - "Client verification 129"
Cohesion: 0.15
Nodes (13): Fact, InlineData, Theory, AiBasePresentationTests, CancellationToken, Fact, IAsyncEnumerable, SKRect (+5 more)

### Community 130 - "Engine simulation 130"
Cohesion: 0.23
Nodes (11): Func, SimulationClock, GameTimeMs, SimulationTimeMs, Speed, SimulationClockState, MotionTimeMs, Fact (+3 more)

### Community 131 - "EconomyBalance 131"
Cohesion: 0.16
Nodes (15): IEnumerable, ImmutableArray, BalanceViolation, MarketHealthEvaluator, Func, ImmutableArray, JsonSerializerOptions, TextWriter (+7 more)

### Community 132 - "Client interface 132"
Cohesion: 0.15
Nodes (18): ApproachRouteGeometryCache, ApproachRouteSample, ApproachRouteSignature, FutureTrajectoryPoint, ApproachRoute, CameraState, List, X (+10 more)

### Community 133 - "Client verification 133"
Cohesion: 0.19
Nodes (12): IEnumerable, IReadOnlyList, List, CombatReportStore, ExpandedSequence, KeyboardSelection, Reports, ScrollOffset (+4 more)

### Community 134 - "Contracts.Tests 134"
Cohesion: 0.17
Nodes (15): ImmutableArray, StationInventoryItemSnapshot, StationMarketEventSnapshot, StationMarketRouteEffectSnapshot, StationMarketStockState, Normal, Shortage, Surplus (+7 more)

### Community 135 - "Engine verification 135"
Cohesion: 0.21
Nodes (8): ImmutableArray, TradeQuoteSnapshot, ArgumentNullException, Fact, InlineData, SimulationEngine, Theory, TradeQuoteIssuanceTests

### Community 136 - "Engine simulation 136"
Cohesion: 0.18
Nodes (12): State, Value, TradingMapRandom, TradingMapRngData, From, IReadOnlyDictionary, IReadOnlyList, To (+4 more)

### Community 137 - "Client verification 137"
Cohesion: 0.15
Nodes (17): CancellationToken, Fact, Func, GameSessionHandle, IAsyncEnumerable, List, RecordingConnection, Task (+9 more)

### Community 138 - "Client verification 138"
Cohesion: 0.16
Nodes (14): CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, ImmutableArray, List, RecordingConnection, Task (+6 more)

### Community 139 - "CombatSim 139"
Cohesion: 0.14
Nodes (20): Any, BattleResult, _chance(), _clamp(), _defense(), _hit_module(), _make_ship(), Random (+12 more)

### Community 140 - "Gameplay requirements 140"
Cohesion: 0.07
Nodes (29): Экран станции, Сверка реализации — 2026-10-10, Стыковка, Экипаж корабля, Открытые вопросы, За рамками первого релиза, Уже существующая основа в коде, Решения первого релиза (+21 more)

### Community 141 - "Client interface 141"
Cohesion: 0.18
Nodes (12): ImmutableArray, SKBitmap, SKCanvas, SKRect, SKRectI, PersonSex, Female, Male (+4 more)

### Community 142 - "Client verification 142"
Cohesion: 0.11
Nodes (15): MapCluster, CameraState, IReadOnlyList, SKRect, TacticalMapSettings, MapViewGeometry, MapWorldBounds, Fact (+7 more)

### Community 143 - "Motion.Tests 143"
Cohesion: 0.19
Nodes (11): TorpedoRoute, CountermeasureGuidanceMath, CountermeasurePlan, Fact, InlineData, Theory, CountermeasureGuidanceTests, Fact (+3 more)

### Community 144 - "Client verification 144"
Cohesion: 0.15
Nodes (16): ItemStorageKind, Cargo, FuelTank, Good, Resource, TradeUnit, EnergyCell, Kilogram (+8 more)

### Community 145 - "Client verification 145"
Cohesion: 0.10
Nodes (17): Fact, Task, DefaultSystemContentTests, Settings, Fact, InlineData, Theory, FullClusterContentTests (+9 more)

### Community 146 - "Combat report planning 146"
Cohesion: 0.07
Nodes (26): Воспроизводимая приёмка боя и интерфейса, Входное техническое задание, Acceptance criteria, Acceptance self-review - 2026-10-16, Dependencies, Final self-review —2026-10-16, Gaps and backlog, Grounding и invariants (+18 more)

### Community 147 - "Combat report planning 147"
Cohesion: 0.07
Nodes (27): Входное техническое задание, Документация и честный статус поставки, Acceptance criteria, Dependencies, Execution result —2026-10-16, Execution scope amendment — 2026-10-16, Gaps and backlog, Grounding и invariants (+19 more)

### Community 148 - "Client verification 148"
Cohesion: 0.18
Nodes (9): Fact, H, VisibleRow, W, X, Y, ScenarioSelectLayoutTests, PanelLeft (+1 more)

### Community 149 - "Snapshot contracts 149"
Cohesion: 0.12
Nodes (20): ImmutableArray, StationRoutePresentation, StationRouteRow, ImmutableArray, TradingRouteAvailability, Available, Restricted, Unavailable (+12 more)

### Community 150 - "Engine verification 150"
Cohesion: 0.33
Nodes (6): Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageFuelLifecycleTests

### Community 151 - "Client verification 151"
Cohesion: 0.14
Nodes (18): Fact, Func, GameSessionHandle, InlineData, SKBitmap, SKCanvas, Task, Theory (+10 more)

### Community 152 - "Client verification 152"
Cohesion: 0.17
Nodes (14): CancellationToken, Fact, IAsyncEnumerable, InlineData, List, Task, Theory, ValueTask (+6 more)

### Community 153 - "Engine verification 153"
Cohesion: 0.25
Nodes (7): Fact, InlineData, SimulationEngine, Theory, ManualCountermeasureFireTests, Fact, PdBurstTests

### Community 154 - "Performance 154"
Cohesion: 0.14
Nodes (9): DeepSpaceSaga.Engine.LocalClient, ScenarioRepository, Fact, Task, AllScenarioSystemContentTests, Fact, CombatScenariosContentTests, EvidenceFactory (+1 more)

### Community 155 - "Release requirements 155"
Cohesion: 0.11
Nodes (27): 10. Проверки и границы приёмки, 11. Технические допущения декомпозиции, 12. Трассировка, 1. Назначение, 2. Размещение и компоновка, 3. Карточки и взаимодействие, 4. События первой версии, 5. Реплики и докладчики (+19 more)

### Community 156 - "Client verification 156"
Cohesion: 0.20
Nodes (7): CameraState, List, TrajectoryViewportGeometry, Fact, InlineData, Theory, TrajectoryViewportTests

### Community 157 - "Engine verification 157"
Cohesion: 0.31
Nodes (4): Fact, InlineData, Theory, TradingMapGeometryTests

### Community 158 - "Client verification 158"
Cohesion: 0.14
Nodes (19): Fact, Func, GameSessionHandle, InlineData, RecordingConnection, SKBitmap, SKCanvas, SnapshotBuffer (+11 more)

### Community 159 - "EconomyBalance 159"
Cohesion: 0.16
Nodes (18): Fact, ImmutableArray, Lazy, LongVoyageRunnerTests, BalanceConfigurationException, BalanceShipConfiguration, ImmutableArray, JsonSerializerOptions (+10 more)

### Community 160 - "Engine verification 160"
Cohesion: 0.26
Nodes (6): Fact, InlineData, SimulationEngine, Theory, CombatSaveLoadTests, Settings

### Community 161 - "Engine verification 161"
Cohesion: 0.22
Nodes (9): Fact, IEnumerable, InlineData, JsonObject, MemberData, Theory, StationMarketProfileContentTests, ProfilePath (+1 more)

### Community 162 - "Engine verification 162"
Cohesion: 0.28
Nodes (7): Fact, Func, InlineData, Lazy, SimulationEngine, Theory, TorpedoImpactTests

### Community 163 - "Client interface 163"
Cohesion: 0.15
Nodes (16): Angle, Radius, Seed, CameraState, Dictionary, Id, IEnumerable, IReadOnlyList (+8 more)

### Community 164 - "Client verification 164"
Cohesion: 0.22
Nodes (7): DeepSpaceSaga.Client.UI, SKColor, SpaceMapColorResolver, Fact, SKColor, SpaceMapColorResolverTests, Fallback

### Community 165 - "Client interface 165"
Cohesion: 0.15
Nodes (17): SKColor, CombatVisualSettings, Countermeasure, CountermeasureIntercept, CountermeasurePrediction, CountermeasureTrail, Default, DefenseRange (+9 more)

### Community 166 - "Client interface 166"
Cohesion: 0.14
Nodes (20): SKCanvas, SKColor, SKFontStyleWeight, SKPaint, SKTypeface, Stopwatch, MenuStyle, BackgroundFill (+12 more)

### Community 167 - "Contracts.Tests 167"
Cohesion: 0.17
Nodes (9): CargoStackSnapshot, DefenseLaunchPreview, ImmutableArray, InstalledModuleSnapshot, ModuleCommandSnapshot, Fact, DefenseLaunchPreviewTests, Fact (+1 more)

### Community 168 - "Engine verification 168"
Cohesion: 0.19
Nodes (9): Fact, InlineData, Theory, CombatBootstrapTests, Fact, SimulationEngine, FireModeRuntimeTests, Fact (+1 more)

### Community 169 - "Client verification 169"
Cohesion: 0.28
Nodes (5): Fact, InlineData, Key, Theory, SpeedPanelTests

### Community 170 - "Engine verification 170"
Cohesion: 0.17
Nodes (10): Fact, InlineData, Theory, AutomaticDefenseTests, Fact, CombatJournalTests, Fact, InlineData (+2 more)

### Community 171 - "Performance 171"
Cohesion: 0.17
Nodes (9): CombatReportsNativeEvidence, BindingFlags, CancellationToken, Channel, IAsyncEnumerable, List, ValueTask, Factory (+1 more)

### Community 172 - "Client interface 172"
Cohesion: 0.13
Nodes (16): Bitmap, LinkedListNode, CharacterVisualState, Dictionary, Image, Key, LinkedList, Path (+8 more)

### Community 173 - "Combat report planning 173"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Авторитетный контракт боевого доклада, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Non-goals, Planned ticket map (+16 more)

### Community 174 - "Combat report planning 174"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Первый доклад о входе противника в дальность, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 175 - "Combat report planning 175"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Доклады экипажа о фактических пусках, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 176 - "Combat report planning 176"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Подтверждённые исходы и повреждения, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 177 - "Combat report planning 177"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Сохранение отметок и чистая загрузка, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 178 - "Combat report planning 178"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals, Planned ticket map (+16 more)

### Community 179 - "Combat report planning 179"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, История карточек и локализованные доклады, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 180 - "Combat report planning 180"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Графические доклады в стиле DSS, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 181 - "Combat report planning 181"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Карточки аккордеона и чтение докладов, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 182 - "Combat report planning 182"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Правая колонка и жизненный цикл экрана, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 183 - "Engine simulation 183"
Cohesion: 0.17
Nodes (18): DialogueFile, QuestFile, HashSet, IEnumerable, ImmutableArray, JsonSerializerOptions, DialogueContentLoader, DialogueFile (+10 more)

### Community 184 - "Engine verification 184"
Cohesion: 0.33
Nodes (5): Fact, InlineData, JsonObject, Theory, ItemCatalogSchemaTests

### Community 185 - "Engine verification 185"
Cohesion: 0.22
Nodes (7): ScenarioFile, MigratedTradingEconomyContinuation, Fact, Func, OverflowException, SimulationEngine, MarketRevisionTests

### Community 186 - "Client verification 186"
Cohesion: 0.28
Nodes (9): SKRect, Fact, H, SKBitmap, SKColor, W, X, Y (+1 more)

### Community 187 - "Engine verification 187"
Cohesion: 0.26
Nodes (8): Fact, InlineData, IReadOnlyList, SimulationEngine, Theory, BasicCombatEndToEndTests, CombatRun, Source

### Community 188 - "Engine verification 188"
Cohesion: 0.40
Nodes (5): Fact, InlineData, SimulationEngine, Theory, RouteRiskIntegrationTests

### Community 189 - "Client verification 189"
Cohesion: 0.25
Nodes (9): ActiveSave, Route, Dictionary, End, Fact, IEnumerable, ImmutableArray, Task (+1 more)

### Community 190 - "03 Design 190"
Cohesion: 0.09
Nodes (18): Горизонтальные ориентиры, На что опирается конструкция, Пропорции женского лица: ориентиры и новый портрет в анфас, Изготовление совместимых компонентов, Проверка анфаса, Вертикальные ориентиры, Овал — объёмная анатомия, не эллипс, Что измерено на новом мастере (+10 more)

### Community 191 - "Client verification 191"
Cohesion: 0.22
Nodes (9): Placeholders, Dictionary, Lazy, Localization, Fact, InlineData, Key, Theory (+1 more)

### Community 192 - "Client verification 192"
Cohesion: 0.24
Nodes (10): CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, ControllableConnection, LastSpeed (+2 more)

### Community 193 - "Client interface 193"
Cohesion: 0.16
Nodes (14): AssemblyInformationalVersionAttribute, HashSet, IEnumerable, List, FrameStatistics, MapFrameContext, MapFrameCounts, MapFrameEvidence (+6 more)

### Community 194 - "Client interface 194"
Cohesion: 0.22
Nodes (12): Dictionary, Func, IEnumerable, LinkedList, SKBitmap, SKColor, CombatReportAssets, IllustrationCount (+4 more)

### Community 195 - "Client interface 195"
Cohesion: 0.24
Nodes (16): H, IReadOnlyList, W, X, Y, ScenarioSelectHit, ScenarioSelectLayout, ListLeft (+8 more)

### Community 196 - "EconomyBalance.Tests 196"
Cohesion: 0.21
Nodes (8): Action, Fact, Func, ImmutableArray, JsonObject, BalanceReportTests, Output, Packaged

### Community 197 - "Release requirements 197"
Cohesion: 0.09
Nodes (20): Сверка реализации — 2026-10-10, Боевые доклады экипажа — 2026-10-16, Назначение, Связанные механики, Функциональность первого релиза, Уже существующая функциональность, Актуализация EP-0004 — 2026-10-08, Боевой этап EP-0007 (+12 more)

### Community 198 - "Client verification 198"
Cohesion: 0.20
Nodes (11): Selected, CancellationToken, Fact, IAsyncEnumerable, List, Task, ValueTask, PoiMapSelectionTests (+3 more)

### Community 199 - "Client interface 199"
Cohesion: 0.19
Nodes (11): SKBitmap, SKCanvas, SKPaint, SKPoint, SKRect, GenericWindowTypeA, HasAssets, Fact (+3 more)

### Community 200 - "Client interface 200"
Cohesion: 0.23
Nodes (9): SKRect, SettingsButton, Exit, LanguageCombo, MonitorCombo, None, UiScaleCombo, SettingsLayout (+1 more)

### Community 201 - "Client interface 201"
Cohesion: 0.30
Nodes (12): Bottom, Left, Right, Top, StationButton, Contracts, Finance, Hire (+4 more)

### Community 202 - "Contracts.Tests 202"
Cohesion: 0.17
Nodes (13): ImmutableArray, AiBaseMapData, AiMapEnvironmentSnapshot, EnvironmentFieldData, PointOfInterestData, TerritoryMapData, OrbitalElements, Fact (+5 more)

### Community 203 - "Snapshot contracts 203"
Cohesion: 0.15
Nodes (22): CombatCommandTypes, CountermeasureTerminationKind, Intercept, LifetimeExpired, MissedCoastExpired, OwnerLost, TargetLost, HullCombatSnapshot (+14 more)

### Community 204 - "EconomyBalance.Tests 204"
Cohesion: 0.25
Nodes (10): Fact, ImmutableArray, InlineData, Theory, BalanceRunTests, Root, Scenario, Settings (+2 more)

### Community 205 - "EconomyBalance.Tests 205"
Cohesion: 0.36
Nodes (5): Fact, ImmutableArray, InlineData, Theory, StrategyBalanceEvaluatorTests

### Community 206 - "Engine verification 206"
Cohesion: 0.36
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, ApproachCommandTests

### Community 207 - "Engine verification 207"
Cohesion: 0.27
Nodes (8): Command, Fact, InlineData, Result, SimulationEngine, Theory, CargoCostAccountingTests, Func

### Community 208 - "Engine verification 208"
Cohesion: 0.25
Nodes (6): Fact, IEnumerable, ItemTypeId, Quantity, SimulationEngine, TradeSnapshotProjectionTests

### Community 209 - "Combat report planning 209"
Cohesion: 0.10
Nodes (17): Планируемые документы и ресурсы, EP-0010 — текущая документация и grounding, EP-0010 — документация и grounding, Grounding, Исторический grounding планирования, Исходный документ — история, superseded current summary выше, EP-0010 — инвентаризация влияния документации, Истории (+9 more)

### Community 210 - "Client interface 210"
Cohesion: 0.16
Nodes (11): DeepSpaceSaga.Client.UI.Screens.Defeat, IScreen, ScreenEvent, SKRect, DefeatLayout, Key, MouseButton, ScreenEvent (+3 more)

### Community 211 - "00 Process 211"
Cohesion: 0.09
Nodes (20): Сверка реализации — 2026-10-10, Architecture, Build, Test, Run, Client ↔ Engine data flow, Client navigation, Composition root (`Program.cs`), Content-driven simulation data, Current state (+12 more)

### Community 212 - "Client interface 212"
Cohesion: 0.17
Nodes (17): Hover, Size, Dictionary, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+9 more)

### Community 213 - "Client verification 213"
Cohesion: 0.16
Nodes (11): IGameSessionFactory, ValueTask, Fixture, Connection, DirectoryPath, Engine, Game, Handle (+3 more)

### Community 214 - "Client interface 214"
Cohesion: 0.16
Nodes (9): GameTimeDisplay, GameCalendar, Fact, InlineData, Theory, GameTimeDisplayTests, InlineData, Theory (+1 more)

### Community 215 - "Client verification 215"
Cohesion: 0.24
Nodes (8): CancellationToken, Exception, Fact, IAsyncEnumerable, Task, ValueTask, FullMapRenderEvidenceTests, ThrowingConnection

### Community 216 - "Client interface 216"
Cohesion: 0.16
Nodes (15): CameraState, End, IReadOnlyList, List, SKPoint, Stack, ObjectTrailGeometry, Points (+7 more)

### Community 217 - "Client interface 217"
Cohesion: 0.18
Nodes (18): TacticalMapSettings, ClusterCellPixels, ClusterPpu, CompactMarkerPpu, FitPaddingPixels, GridBaseCellPixels, GridFadePixels, GridMinimumPixels (+10 more)

### Community 218 - "Snapshot contracts 218"
Cohesion: 0.17
Nodes (16): ImmutableArray, CountermeasurePhase, Guiding, MissedCoast, CountermeasureSnapshot, DefenseCommandTypes, DefenseState, Guiding (+8 more)

### Community 219 - "Client verification 219"
Cohesion: 0.22
Nodes (10): StationTravelResult, CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, DelayedTravelConnection (+2 more)

### Community 220 - "Local session transport 220"
Cohesion: 0.23
Nodes (4): SaveSlotNaming, SaveSlotRepository, Fact, SaveSlotRepositoryTests

### Community 221 - "EconomyBalance.Tests 221"
Cohesion: 0.37
Nodes (4): Fact, Func, ImmutableArray, MarketHealthEvaluatorTests

### Community 222 - "Client interface 222"
Cohesion: 0.17
Nodes (13): IEnumerator, IReadOnlyList, MaxX, MaxY, MinX, MinY, ObjectTrailBuffer, Bounds (+5 more)

### Community 223 - "Client interface 223"
Cohesion: 0.23
Nodes (9): Dictionary, Func, HashSet, IReadOnlyList, IReadOnlySet, List, ObjectTrailPoint, ObjectTrailStore (+1 more)

### Community 224 - "Engine verification 224"
Cohesion: 0.30
Nodes (4): Fact, CommandDefinitionLoaderDirectoryTests, Fact, CommandDefinitionLoaderTests

### Community 225 - "Engine simulation 225"
Cohesion: 0.18
Nodes (16): Dictionary, IEnumerable, ImmutableArray, ImmutableDictionary, StationMarketEconomyDefinition, AllStockTargets, StationMarketProductionSource, Modules (+8 more)

### Community 226 - "Engine verification 226"
Cohesion: 0.27
Nodes (5): TradingEconomySaveMigration, Fact, InlineData, Theory, TradingEconomySaveSchemaTests

### Community 227 - "Client verification 227"
Cohesion: 0.16
Nodes (13): CancellationToken, Func, IAsyncEnumerable, List, TaskCompletionSource, ValueTask, RecordingConnection, Commands (+5 more)

### Community 228 - "Engine verification 228"
Cohesion: 0.27
Nodes (4): Fact, InlineData, Theory, ItemCatalogTests

### Community 229 - "Engine verification 229"
Cohesion: 0.25
Nodes (7): ArgumentException, Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageLedgerLifecycleTests

### Community 230 - "Client verification 230"
Cohesion: 0.21
Nodes (8): IReadOnlyList, SaveSlots, DateTime, SaveSlotInfo, Fact, SaveSlotsTests, Fact, SaveSlotInfoTests

### Community 231 - "Client verification 231"
Cohesion: 0.22
Nodes (9): SKCanvas, SKColor, SKPaint, SKRect, ProgressBar, Fact, InlineData, Theory (+1 more)

### Community 232 - "Client interface 232"
Cohesion: 0.19
Nodes (18): ApproachRoute, ObjectRenderState, Predicted, RenderMotion, ActiveEngineCommandType, ApproachRoute, DisplayName, HasAbsoluteOrbit (+10 more)

### Community 233 - "Client verification 233"
Cohesion: 0.23
Nodes (10): Fact, Task, ValueTask, CombatSessionFixture, Connection, DirectoryPath, Engine, Restored (+2 more)

### Community 234 - "Engine verification 234"
Cohesion: 0.22
Nodes (9): Fact, InlineData, Theory, EpicSolarIntegrationTests, Fact, InlineData, SimulationEngine, Theory (+1 more)

### Community 235 - "EconomyBalance 235"
Cohesion: 0.32
Nodes (5): BalanceCaseEvidence, ImmutableArray, BalanceLeader, BalanceStrategyIdentity, StrategyBalanceEvaluator

### Community 236 - "Client interface 236"
Cohesion: 0.23
Nodes (7): Dictionary, IReadOnlyList, PortraitAssetRepository, Parts, Root, Style, UnifiedPortraitAssets

### Community 237 - "Client verification 237"
Cohesion: 0.27
Nodes (6): Func, GameSessionHandle, Task, TaskCompletionSource, StationTradeNavigation, Task

### Community 238 - "Snapshot contracts 238"
Cohesion: 0.19
Nodes (18): BattleReportDamageSnapshot, BattleReportKind, EnemyInRange, EnemyShipHit, EnemyTorpedoIntercepted, EnemyTorpedoLaunch, PlayerCountermeasureLaunch, PlayerShipDamaged (+10 more)

### Community 239 - "Engine simulation 239"
Cohesion: 0.21
Nodes (9): CombatChanceMath, CountermeasureRng, Counter, State, CombatChanceRules, Fact, InlineData, Theory (+1 more)

### Community 240 - "Engine verification 240"
Cohesion: 0.32
Nodes (5): Fact, InlineData, SimulationEngine, Theory, CountermeasureLifetimeTests

### Community 241 - "Client verification 241"
Cohesion: 0.25
Nodes (10): CancellationToken, GameSessionHandle, IAsyncEnumerable, ValueTask, CombatReportsAcceptanceFixtures, Expected, Handle, Screen (+2 more)

### Community 242 - "Engine verification 242"
Cohesion: 0.25
Nodes (8): Fact, IEnumerable, MemberData, SimulationEngine, Theory, X, Y, FullMapCorrectnessCorpusTests

### Community 243 - "Engine verification 243"
Cohesion: 0.35
Nodes (7): Fact, Func, InlineData, Lazy, SimulationEngine, Theory, TorpedoLaunchTests

### Community 244 - "Engine verification 244"
Cohesion: 0.37
Nodes (4): Fact, InlineData, Theory, VoyageProfitAccountingTests

### Community 245 - "Contracts.Tests 245"
Cohesion: 0.16
Nodes (13): BattleReportVariant, ImmutableArray, CombatReportBatchSnapshot, BattleReportKind, BattleReportReporterRole, BattleReportSnapshot, CombatReportBatchSnapshot, Fact (+5 more)

### Community 246 - "Engine simulation 246"
Cohesion: 0.31
Nodes (11): Disc, Point, ImmutableArray, IReadOnlyList, AiTradePlacementValidator, Disc, PlacementCheck, PlacementConnectivity (+3 more)

### Community 247 - "Engine verification 247"
Cohesion: 0.25
Nodes (7): Final, Reports, BattleReportSnapshot, Fact, InvalidOperationException, CombatReportsEndToEndTests, CombatReportsScenarioFixtures

### Community 248 - "Client verification 248"
Cohesion: 0.26
Nodes (7): Func, CombatReportFormatter, CombatReportPresentation, Fact, InlineData, Theory, CombatReportFormatterTests

### Community 250 - "Motion.Tests 250"
Cohesion: 0.25
Nodes (8): Func, X, Y, CollisionPath, TorpedoCollisionMath, CollisionPath, Fact, TorpedoCollisionTests

### Community 251 - "Client verification 251"
Cohesion: 0.37
Nodes (5): ApproachRoute, Fact, InlineData, Theory, ApproachRouteProjectionTests

### Community 252 - "Client verification 252"
Cohesion: 0.27
Nodes (7): Fact, GameSessionHandle, InvalidOperationException, Key, ObjectDisposedException, Task, DefeatNavigationTests

### Community 253 - "Client verification 253"
Cohesion: 0.42
Nodes (5): Fact, InlineData, Task, Theory, ManualDefenseUiFlowTests

### Community 254 - "Engine verification 254"
Cohesion: 0.29
Nodes (5): Fact, DefeatTimeBoundaryTests, Fact, InvalidOperationException, PlayerDefeatTests

### Community 255 - "PortraitAssets 255"
Cohesion: 0.28
Nodes (5): SKBitmap, SKColor, SKPoint3, SKRectI, WholeFaceAssets

### Community 256 - "Combat report planning 256"
Cohesion: 0.12
Nodes (16): Assumptions, Code context, Decisions, Definition of Done, Вручную выполнить evidence/NativeAcceptance.md и evidence/Performance.md. Только во время execution., Execution evidence and user constraint - 2026-10-16, Execution scope/order adjustment - 2026-10-16, Визуальная приёмка и frame-time evidence (+8 more)

### Community 257 - "Contracts.Tests 257"
Cohesion: 0.31
Nodes (7): TorpedoRouteSegment, TorpedoSnapshot, TrailSegment, Fact, InlineData, Theory, CombatSnapshotTests

### Community 258 - "Engine simulation 258"
Cohesion: 0.24
Nodes (6): EnvironmentFieldGenerator, AiGenerationConfig, EnvironmentGenerationConfig, SolarSystemGeneration, Fact, AiBaseContentTests

### Community 259 - "Client verification 259"
Cohesion: 0.34
Nodes (6): Fact, HashSet, InlineData, SnapshotBuffer, Theory, GameSessionScaleFilterTests

### Community 260 - "Client verification 260"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SnapshotBuffer, Theory, GameSessionUiScaleTests

### Community 261 - "Engine verification 261"
Cohesion: 0.25
Nodes (6): Fact, CountermeasureBootstrapTests, Fact, InlineData, Theory, CountermeasureEndToEndTests

### Community 262 - "Engine verification 262"
Cohesion: 0.22
Nodes (11): Fact, InvalidOperationException, SimulationEngine, Task, ValueTask, Battle, Connection, DirectoryPath (+3 more)

### Community 263 - "Engine verification 263"
Cohesion: 0.37
Nodes (5): Fact, InlineData, SimulationEngine, Theory, VoyageFuelAccountingTests

### Community 264 - "Engine verification 264"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, VoyageSaveLoadContinuityTests

### Community 265 - "Performance 265"
Cohesion: 0.23
Nodes (5): CombatNativeEvidence, BindingFlags, IWindow, SKSurface, Factory

### Community 266 - "Combat report planning 266"
Cohesion: 0.12
Nodes (15): Иллюстрации дальности и торпедных пусков, Artifact evidence - 2026-10-16, Assumptions, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 267 - "Combat report planning 267"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Иллюстрации ПРО, перехвата и попадания, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 268 - "Combat report planning 268"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Code context, Уничтожение противника и повреждение игрока, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 269 - "Client verification 269"
Cohesion: 0.27
Nodes (7): DeepSpaceSaga.Client.UI.Portraits, SKBitmap, DialoguePortraitComposer, Fact, InlineData, Theory, DialoguePortraitComposerTests

### Community 270 - "Release requirements 270"
Cohesion: 0.12
Nodes (15): Сверка реализации — 2026-10-10, Экономический интервал, Размер станции, Термины, Формула котировки, События, Склад и бюджет, Acceptance criteria (+7 more)

### Community 271 - "Engineering documentation 271"
Cohesion: 0.12
Nodes (15): 10. [P2] Очередь полных снимков не ограничена, 1. [P1] Продолжения асинхронных переходов меняют UI из фонового потока, 2. [P1] Охрана станции не учитывает действующий маршрут Approach, 3. [P1] Ошибка симуляции превращается в нормальное завершение потока снимков, 4. [P2] Save/Load меняет курс после отмены Approach, 5. [P2] Повторная доставка торговой команды повторно меняет мир, 6. [P2] Торговые операции допускают переполнение денег и массы, 7. [P2] Результаты команд теряются, если UI пропускает снимок (+7 more)

### Community 272 - "Engine simulation 272"
Cohesion: 0.38
Nodes (7): Exception, IReadOnlyList, SolarSystemGenerationConfig, List, GeneratorRng, PlacementException, SolarSystemGenerator

### Community 273 - "Engine verification 273"
Cohesion: 0.23
Nodes (8): IReadOnlyCollection, IReadOnlyList, ShipDestructionMath, ShipModuleLife, Fact, InlineData, Theory, ShipDestructionRulesTests

### Community 274 - "PortraitAssets 274"
Cohesion: 0.33
Nodes (4): Landmarks, SKBitmap, Landmarks, ReferenceFeatureAssets

### Community 275 - "Client verification 275"
Cohesion: 0.23
Nodes (9): Dictionary, SKBitmap, UiAssetLoader, Fact, MemberData, Theory, TheoryData, UiAssetLoaderTests (+1 more)

### Community 277 - "Contracts.Tests 277"
Cohesion: 0.25
Nodes (9): ImmutableArray, AsteroidSurveySnapshot, ModuleHealthSnapshot, ResourceFractionSnapshot, ResourceSurveyReasonCodes, Fact, InlineData, Theory (+1 more)

### Community 278 - "Contracts.Tests 278"
Cohesion: 0.23
Nodes (9): ImmutableArray, VoyagePhases, VoyageRouteOptionSnapshot, VoyageSnapshot, State, Fact, InlineData, Theory (+1 more)

### Community 280 - "Client verification 280"
Cohesion: 0.33
Nodes (6): Fact, InlineData, Theory, TemporaryContent, Path, TorpedoLauncherContentTests

### Community 281 - "Engine verification 281"
Cohesion: 0.21
Nodes (8): Fact, InlineData, Theory, CombatReportInterceptTests, InlineData, SimulationEngine, Theory, PhaseThreeMultiHostileTests

### Community 282 - "Engine verification 282"
Cohesion: 0.29
Nodes (7): Fact, InlineData, List, SimulationEngine, SpaceObjectRuntime, Theory, CombatReportRangeTests

### Community 283 - "Engine verification 283"
Cohesion: 0.33
Nodes (5): Fact, InlineData, SimulationEngine, Theory, CombatWreckTests

### Community 284 - "Engine verification 284"
Cohesion: 0.41
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PhaseThreeSaveContinuationTests

### Community 285 - "Engine verification 285"
Cohesion: 0.36
Nodes (6): Fact, Func, InlineData, SimulationEngine, Theory, PhaseThreeSaveSchemaTests

### Community 286 - "Engine verification 286"
Cohesion: 0.28
Nodes (8): Direction, Fact, ImmutableArray, InlineData, Theory, X, Y, TorpedoHistoryTests

### Community 287 - "Engine verification 287"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, TradingEconomyContinuityTests

### Community 288 - "Combat report planning 288"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, E01 по физическому состоянию мира, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 289 - "Combat report planning 289"
Cohesion: 0.13
Nodes (14): Assumptions, Capture/restore и bootstrap baseline, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 290 - "Combat report planning 290"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Общая геометрия Object Info и докладов (+6 more)

### Community 291 - "Combat report planning 291"
Cohesion: 0.13
Nodes (11): Correct the available map viewport after adding reports, Evidence, Scope and steps, Complete HUD reservation and synchronize affected test fixtures, Evidence, Exact scope, Clear unrelated HUD hover/press states when reports consume pointer input, Evidence (+3 more)

### Community 292 - "EconomyBalance.Tests 292"
Cohesion: 0.27
Nodes (8): DeepSpaceSaga.EconomyBalance.Tests, DeepSpaceSaga.EconomyBalance, Fact, LongVoyageDiagnosticsTests, ImmutableArray, ClusterEconomyAssessment, ClusterEconomyEvaluator, ClusterEconomyFinding

### Community 293 - "00 Process 293"
Cohesion: 0.13
Nodes (14): `00-Process/`, `01-Requirements/`, `02-FirstRelease/`, `03-Design/`, `04-Engineering/`, `05-Backlog/`, `06-Tooling/`, Documentation System (+6 more)

### Community 294 - "Client verification 294"
Cohesion: 0.18
Nodes (11): Effects, Max, Min, Priority, Profiles, Chance, Dictionary, Fact (+3 more)

### Community 295 - "Client interface 295"
Cohesion: 0.27
Nodes (6): LocalGameSessionFactory, QuickSavePath, SaveDirectory, ScenariosDirectory, SettingsPath, Program

### Community 296 - "Client verification 296"
Cohesion: 0.33
Nodes (6): Fact, ImmutableArray, InlineData, Theory, DualDefensePanelLayoutTests, Modules

### Community 297 - "Snapshot contracts 297"
Cohesion: 0.26
Nodes (10): SessionOutcomeSnapshot, CanSave, Running, SaveDeniedReason, SessionOutcomeState, Defeated, Running, Fact (+2 more)

### Community 298 - "Engine simulation 298"
Cohesion: 0.36
Nodes (6): ClusterGenerationConfig, Func, ImmutableArray, IReadOnlyList, ClusterGenerationResult, StationClusterGenerator

### Community 299 - "Engine simulation 299"
Cohesion: 0.40
Nodes (4): From, IReadOnlyDictionary, To, TradingMapDataValidation

### Community 300 - "Client verification 300"
Cohesion: 0.36
Nodes (5): Fact, SKBitmap, SKColor, SKRect, NinePatchTests

### Community 301 - "Client verification 301"
Cohesion: 0.29
Nodes (7): Fact, InlineData, Theory, TemporaryContent, Root, SettingsPath, TetrarchClassContentTests

### Community 302 - "Engine verification 302"
Cohesion: 0.34
Nodes (5): Fact, InlineData, JsonNode, Theory, CountermeasureContentLoaderTests

### Community 303 - "Engine verification 303"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, HostileOrbitRuntimeTests

### Community 304 - "Engine verification 304"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, HostileTorpedoFireTests

### Community 305 - "Engine verification 305"
Cohesion: 0.41
Nodes (5): Fact, InlineData, SimulationEngine, Theory, MarketSaveLoadContinuityTests

### Community 306 - "Combat report planning 306"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence — 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 307 - "Combat report planning 307"
Cohesion: 0.14
Nodes (13): Докладчики и безопасные имена участников, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 308 - "Combat report planning 308"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, E05 успешного перехвата, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 309 - "Combat report planning 309"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 310 - "Combat report planning 310"
Cohesion: 0.14
Nodes (13): Ограниченная история и раскрытая карточка, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 311 - "Combat report planning 311"
Cohesion: 0.14
Nodes (13): Assumptions, Ввод и навигация по карточкам, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 312 - "Combat report planning 312"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Чистая смена сессии и приоритет Defeat, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 313 - "Combat report planning 313"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 314 - "06 Tooling 314"
Cohesion: 0.14
Nodes (13): Артефакты, Сверка реализации — 2026-10-10, Результаты и ограничения, Пять проверенных трасс, Инструментарий и воспроизведение, Корпус и свежесть, EP-0007 — полный rebuild Graphify, operator-rating-launch (+5 more)

### Community 315 - "Engine verification 315"
Cohesion: 0.27
Nodes (6): Fact, InlineData, InvalidOperationException, SimulationEngine, Theory, CombatReportsSaveLoadTests

### Community 316 - "Engine verification 316"
Cohesion: 0.38
Nodes (5): Fact, InlineData, SimulationEngine, Theory, DualDefenseArbitrationTests

### Community 317 - "Release requirements 317"
Cohesion: 0.15
Nodes (12): Сверка реализации — 2026-10-10, Требования первого релиза, Уже существующая основа, Тактическая карта и маневрирование, Сближение Approach, Команды первого релиза, Цель, Актуализация EP-0004 — 2026-10-08 (+4 more)

### Community 318 - "Engineering documentation 318"
Cohesion: 0.15
Nodes (12): Сверка реализации — 2026-10-10, EP-0002 implementation evidence, Актуализация EP-0004 — 2026-10-08, Epic integration review, US-0001 — seeded system start, US-0002 — coherent orbital world, US-0003 — playable asteroid belts, US-0004 — preserved scenario starts (+4 more)

### Community 319 - "05 Backlog 319"
Cohesion: 0.15
Nodes (12): ТЗ-02A: masterSeed для New Game и save/load continuation, ТЗ-03: загрузка всех тематических JSON из Settings.json, ТЗ-04: CommandResult и ShipEvent в authoritative snapshot, ТЗ-05: authoritative command inbox, validation и Deferred attempts, ТЗ-06: полный набор Engine commands первой итерации, ТЗ-07: стартовый корабль из 3 платформ и 12 occupied cells, ТЗ-08: fuel state for Engine module, ТЗ-09: ActiveCycle duration, command factors and fixed-point normalization (+4 more)

### Community 320 - "Validation 320"
Cohesion: 0.15
Nodes (12): Сверка реализации — 2026-10-10, EP-0003 implementation and review evidence, Актуализация EP-0004 — 2026-10-08, Final epic review and regression, US-0001 — Local trading cluster, US-0002 — Distinct orbital districts, US-0003 — Resource surroundings, US-0004 — Current travel estimates (+4 more)

### Community 321 - "Client interface 321"
Cohesion: 0.33
Nodes (7): SKPaintStyle, SKColor, SKFontStyleWeight, SKPaint, SKTextAlign, SKTypeface, XenonStyle

### Community 322 - "Client interface 322"
Cohesion: 0.24
Nodes (7): SKBitmap, SKCanvas, SKRect, ImagePanel, HasLoadedImage, Fact, ImagePanelTests

### Community 323 - "Client interface 323"
Cohesion: 0.28
Nodes (6): SKCanvas, SKRect, XenonWindowChrome, HasChromeAssets, Fact, XenonWindowChromeTests

### Community 324 - "Contracts.Tests 324"
Cohesion: 0.32
Nodes (5): ShipEvent, ShipEventReasonCodes, ShipEventTypes, Fact, ShipEventTests

### Community 325 - "Engine verification 325"
Cohesion: 0.38
Nodes (5): AttackAiData, Fact, InlineData, Theory, HostileAttackAiTests

### Community 327 - "Engine verification 327"
Cohesion: 0.35
Nodes (5): Fact, List, SimulationEngine, SpaceObjectRuntime, CombatReportLaunchTests

### Community 328 - "Engine verification 328"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, DefensePreviewProjectionTests

### Community 330 - "Engine verification 330"
Cohesion: 0.41
Nodes (5): Fact, Func, InlineData, Theory, RangedTorpedoLaunchTests

### Community 331 - "Client verification 331"
Cohesion: 0.38
Nodes (4): DeepSpaceSaga.Client.UI.Screens.Settings, Fact, SKBitmap, SettingsScreenTests

### Community 332 - "05 Backlog 332"
Cohesion: 0.17
Nodes (11): ТЗ-01: CommandResult в authoritative snapshot, ТЗ-02: минимальный ShipEvent / watch-log DTO в snapshot, ТЗ-03: канонические Engine commands вместо `engine.cancel-all`, ТЗ-04: authoritative параметры target для match commands, ТЗ-05: загрузка `factory-types.json` и `recipes.json` из Settings, ТЗ-06: factor model для command definitions, ТЗ-07: fuel state для Engine module, ТЗ-08: стартовый корабль из 9 модулей и 12 occupied cells (+3 more)

### Community 333 - "Engine simulation 333"
Cohesion: 0.23
Nodes (5): RealizedCostCredits, Remaining, CargoAcquisitionSources, ArgumentOutOfRangeException, OverflowException

### Community 334 - "Client interface 334"
Cohesion: 0.24
Nodes (8): SmoothState, Dictionary, HashSet, List, SKPoint, SKRect, ObjectLabelSmoother, SmoothState

### Community 335 - "Client interface 335"
Cohesion: 0.23
Nodes (6): Exception, Func, HashSet, Task, ValueTask, SessionConnectionLoader

### Community 336 - "Client interface 336"
Cohesion: 0.39
Nodes (6): SKBitmap, SKCanvas, SKColor, SKRect, GenericButtonTypeA, HasAssets

### Community 337 - "Snapshot contracts 337"
Cohesion: 0.33
Nodes (8): ImmutableArray, ClusterResourceBinding, ClusterStationData, ClusterTradeLink, StationClusterData, StationClusterMapSnapshot, Fact, ClusterGeographyContractTests

### Community 338 - "Snapshot contracts 338"
Cohesion: 0.23
Nodes (9): StationDistrict, Administration, Dock, Habitation, Market, ImmutableArray, TimedContractState, IReadOnlyList (+1 more)

### Community 339 - "Engine verification 339"
Cohesion: 0.41
Nodes (4): Fact, InlineData, Theory, StationSizeFactorsTests

### Community 340 - "Client verification 340"
Cohesion: 0.53
Nodes (3): Fact, SnapshotBuffer, GameSessionDockAutoOpenTests

### Community 341 - "Client verification 341"
Cohesion: 0.26
Nodes (7): Fact, InlineData, SKBitmap, SKColor, SKRect, Theory, GenericButtonTypeATests

### Community 342 - "Engine verification 342"
Cohesion: 0.47
Nodes (4): Fact, SimulationEngine, SpaceObjectRuntime, CombatReportIdentityTests

### Community 343 - "Engine verification 343"
Cohesion: 0.36
Nodes (5): Fact, CombatRulesContentTests, ClientRoot, ModesPath, RulesPath

### Community 344 - "Engine verification 344"
Cohesion: 0.47
Nodes (3): Fact, SimulationEngine, ObjectImageResolutionTests

### Community 345 - "Engine verification 345"
Cohesion: 0.27
Nodes (7): Fact, IEnumerable, InlineData, MemberData, Theory, PhaseThreeSaveVersionTests, OldVersions

### Community 346 - "Engine verification 346"
Cohesion: 0.45
Nodes (4): Fact, SimulationEngine, Task, SaveLoadContinuityTests

### Community 347 - "PortraitAssets 347"
Cohesion: 0.38
Nodes (4): SKBitmap, SKPath, SKPoint, HairAssets

### Community 348 - "Client interface 348"
Cohesion: 0.36
Nodes (8): CombatReportPresentation, CombatReportFormatter, CombatReportStore, ImmutableArray, SKPaint, SKRect, CombatReportCardLayout, CombatReportPanelLayout

### Community 349 - "Client verification 349"
Cohesion: 0.47
Nodes (3): SKColor, Fact, GameSessionScreenTrailColorTests

### Community 350 - "Client verification 350"
Cohesion: 0.36
Nodes (6): Fact, Fixture, InlineData, Task, Theory, RangedTorpedoUiTests

### Community 351 - "Client verification 351"
Cohesion: 0.53
Nodes (3): InlineData, Theory, TetrarchLoadoutTests

### Community 352 - "Engine verification 352"
Cohesion: 0.40
Nodes (5): Fact, InvalidOperationException, SimulationEngine, TargetInvocationException, CombatReportOutboxTests

### Community 353 - "Engine verification 353"
Cohesion: 0.51
Nodes (3): Fact, SimulationEngine, PhaseThreeJournalTests

### Community 354 - "Engine verification 354"
Cohesion: 0.49
Nodes (3): Fact, SimulationEngine, PortFeeScheduleTests

### Community 355 - "Release requirements 355"
Cohesion: 0.20
Nodes (9): За рамками первого релиза, Команды, Требования первого релиза, Уже существующая основа, Asteroid ice stock, Добыча льда, Решение первого релиза, Параметры Drilling Unit (+1 more)

### Community 356 - "Engine verification 356"
Cohesion: 0.44
Nodes (4): InternalsVisibleToAttribute, Fact, SimulationEngine, EconomyBalanceSeamTests

### Community 357 - "Snapshot contracts 357"
Cohesion: 0.31
Nodes (7): ImmutableArray, JsonSerializerOptions, List, Type, Utf8JsonReader, Utf8JsonWriter, ImmutableArrayDefaultJsonConverter

### Community 358 - "Snapshot contracts 358"
Cohesion: 0.40
Nodes (7): SimulationSpeed, Speed0, Speed1, Speed2, Speed3, Speed4, SimulationSpeedExtensions

### Community 359 - "Engine verification 359"
Cohesion: 0.38
Nodes (4): ModuleDamageMath, ModuleDamageResult, Fact, ModuleDamageTests

### Community 360 - "Contracts.Tests 360"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, StationMarketKnowledgeSnapshotTests

### Community 361 - "Engine verification 361"
Cohesion: 0.49
Nodes (4): Fact, InlineData, Theory, TradingContinuationReviewTests

### Community 362 - "Engine verification 362"
Cohesion: 0.47
Nodes (3): Fact, SimulationEngine, VoyageLifecycleTests

### Community 363 - "Client verification 363"
Cohesion: 0.33
Nodes (5): DeepSpaceSaga.Client.UI.Screens.ScenarioSelect, Fact, InlineData, Theory, ScenarioSelectScreenTests

### Community 364 - "Release requirements 364"
Cohesion: 0.22
Nodes (8): Примечания, Сверка реализации — 2026-10-10, Назначение, Архитектурные требования, Оформление, Связанные механики, Функциональность первого релиза, ScenarioSelect

### Community 365 - "Release requirements 365"
Cohesion: 0.22
Nodes (8): Требования к выполнению, Будущие экраны первого релиза, Проверка, Цель, Область применения, Уже существующие экраны, требующие переименования, Layout/helper classes, Screen Naming Convention Refactor

### Community 368 - "Client verification 368"
Cohesion: 0.64
Nodes (3): InlineData, Theory, CountermeasureContentTests

### Community 369 - "Client verification 369"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, DefeatScreenTests

### Community 370 - "Engine verification 370"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, AuthoritativeOrbitRuntimeTests

### Community 376 - "Engine verification 376"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, VoyagePersistenceTests

### Community 378 - "Client interface 378"
Cohesion: 0.43
Nodes (6): Data, Lazy, Data, GameInfo, Title, Version

### Community 379 - "03 Design 379"
Cohesion: 0.25
Nodes (7): Геометрия и состояния, Характер, Шаблон для следующей кнопки, Палитра, Файлы и воспроизводимость, Смысл символов, Deep Space Saga — стиль кнопок тактической карты

### Community 381 - "Client interface 381"
Cohesion: 0.50
Nodes (5): SKBitmap, SKCanvas, SKPaint, SKRect, NinePatch

### Community 382 - "Client verification 382"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, TacticalMapLabelBoundsTests

### Community 383 - "Client verification 383"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, VoyageFuelContentTests

### Community 384 - "Release requirements 384"
Cohesion: 0.29
Nodes (6): Решения первого релиза, Требования первого релиза, Предварительные условия, Диалоги с представителями станций, Цель, MVP-вариант

### Community 385 - "Release requirements 385"
Cohesion: 0.29
Nodes (6): Назначение, За рамками первого релиза, Связанные механики, Функциональность первого релиза, Hire, Статус реализации (MVP)

### Community 386 - "Release requirements 386"
Cohesion: 0.29
Nodes (6): Примечания, Назначение, Оформление, Связанные механики, Функциональность первого релиза, MainMenu

### Community 387 - "Engine simulation 387"
Cohesion: 0.62
Nodes (3): IReadOnlyList, PointOfInterestGenerator, PoiTemplate

### Community 388 - "Contracts.Tests 388"
Cohesion: 0.62
Nodes (3): AuthoritativeSnapshot, Fact, CombatReportBatchTests

### Community 389 - "Engine verification 389"
Cohesion: 0.33
Nodes (5): InlineData, ITestOutputHelper, Theory, CombatBalanceReport, Trait

### Community 392 - "Release requirements 392"
Cohesion: 0.33
Nodes (5): Решения первого релиза, Требования первого релиза, Экипаж корабля и диалоги, Требует решения, Цель

### Community 393 - "Release requirements 393"
Cohesion: 0.33
Nodes (5): Назначение, Связанные механики, Функциональность первого релиза, Contracts, Статус реализации (MVP)

### Community 394 - "Release requirements 394"
Cohesion: 0.33
Nodes (5): Назначение, Архитектурные требования, Оформление и локализация, Функциональность первого релиза, GameMenu

### Community 395 - "Release requirements 395"
Cohesion: 0.33
Nodes (5): Визуальный стиль, Назначение, Будущие кандидаты настроек, Функциональность первого релиза, Settings

### Community 396 - "Release requirements 396"
Cohesion: 0.33
Nodes (5): Назначение, Размер окна, Связанные механики, Функциональность первого релиза, Ship

### Community 397 - "Engineering documentation 397"
Cohesion: 0.33
Nodes (5): Автоматические проверки, Воспроизведение и причина, Проверка на сохранённых сценах, Изменения, Рывки карты: GPU-кэш масок, 24 сентября 2026

### Community 399 - "Client verification 399"
Cohesion: 0.60
Nodes (3): InlineData, Theory, PiratePresentationTests

### Community 400 - "Engine verification 400"
Cohesion: 0.40
Nodes (4): InlineData, Task, Theory, PhaseThreeDeterminismTests

### Community 401 - "Release requirements 401"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Cargo

### Community 402 - "Release requirements 402"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Character Communication

### Community 403 - "Release requirements 403"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Dialog

### Community 404 - "Release requirements 404"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Loot

### Community 405 - "Engineering documentation 405"
Cohesion: 0.40
Nodes (4): Контроль производительности, Исправления по результатам ревью, Границы хранения и совместимость, Проверки

### Community 406 - "Engineering documentation 406"
Cohesion: 0.40
Nodes (4): Новое окно торговли, Код и данные, Проверка, Поведение

## Knowledge Gaps
- **1468 isolated node(s):** `CharacterPortraits`, `CombatCommandTypes`, `TorpedoLauncherCommandTypes`, `CommandReasonCodes`, `DefenseCommandTypes` (+1463 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2850 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **26 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DeepSpaceSaga.Contracts` connect `Engine simulation 28` to `Client interface 0`, `Engine simulation 2`, `Engine verification 3`, `Client verification 4`, `Client verification 6`, `Client verification 7`, `Client verification 8`, `Engine verification 9`, `Engine verification 10`, `Client verification 11`, `Engine simulation 12`, `Client interface 13`, `Client verification 14`, `Engine verification 15`, `Client verification 17`, `Client verification 18`, `Engine verification 19`, `Engine verification 20`, `Client interface 21`, `Engine verification 22`, `Engine verification 23`, `Client verification 24`, `Client verification 25`, `Engine verification 26`, `Client verification 27`, `Client interface 29`, `Client interface 30`, `Client interface 31`, `Client verification 33`, `Client verification 34`, `Engine simulation 35`, `Engine verification 36`, `Client interface 37`, `Client interface 38`, `Engine simulation 39`, `Client verification 40`, `Engine verification 41`, `Client verification 43`, `Client interface 45`, `Client verification 46`, `Engine simulation 47`, `Client verification 49`, `Engine simulation 50`, `EconomyBalance 51`, `Engine simulation 53`, `Client interface 54`, `Engine verification 55`, `Engine simulation 58`, `Engine simulation 59`, `Engine simulation 60`, `Client interface 61`, `Client interface 62`, `Client interface 64`, `Contracts.Tests 68`, `Client interface 69`, `Engine verification 70`, `Engine verification 71`, `Client interface 72`, `Client interface 74`, `Client verification 75`, `Client verification 76`, `Local session transport 79`, `Client verification 80`, `Client verification 81`, `Client verification 84`, `Client verification 85`, `Client verification 87`, `Client interface 88`, `Client verification 90`, `Motion.Tests 91`, `Engine verification 92`, `Client verification 93`, `Engine verification 94`, `Client verification 95`, `Engine verification 96`, `Client verification 99`, `Engine simulation 100`, `Performance.Tests 101`, `Client verification 102`, `Client interface 103`, `Engine verification 105`, `Engine simulation 106`, `Engine verification 108`, `Client verification 110`, `Client interface 113`, `Contracts.Tests 114`, `Engine verification 115`, `Client interface 117`, `Client verification 119`, `Engine verification 120`, `Engine verification 124`, `Engine verification 125`, `Client verification 126`, `Client interface 127`, `Client interface 128`, `Client verification 129`, `Engine simulation 130`, `EconomyBalance 131`, `Client interface 132`, `Client verification 133`, `Contracts.Tests 134`, `Engine verification 135`, `Client verification 137`, `Client verification 138`, `Client verification 142`, `Motion.Tests 143`, `Client verification 145`, `Snapshot contracts 149`, `Engine verification 150`, `Client verification 151`, `Client verification 152`, `Engine verification 153`, `Performance 154`, `Client verification 156`, `Client verification 158`, `EconomyBalance 159`, `Engine verification 160`, `Engine verification 162`, `Client interface 163`, `Client verification 164`, `Contracts.Tests 167`, `Engine verification 168`, `Client verification 169`, `Engine verification 170`, `Performance 171`, `Engine simulation 183`, `Engine verification 185`, `Client verification 186`, `Engine verification 187`, `Engine verification 188`, `Client verification 189`, `Client verification 192`, `Client interface 193`, `Client interface 194`, `Client verification 198`, `Contracts.Tests 202`, `Snapshot contracts 203`, `EconomyBalance.Tests 204`, `EconomyBalance.Tests 205`, `Engine verification 206`, `Engine verification 207`, `Engine verification 208`, `Client verification 213`, `Client interface 214`, `Client verification 215`, `Snapshot contracts 218`, `Client verification 219`, `Local session transport 220`, `EconomyBalance.Tests 221`, `Client interface 223`, `Engine verification 226`, `Engine verification 229`, `Client verification 230`, `Client interface 232`, `Client verification 233`, `Engine verification 234`, `EconomyBalance 235`, `Client verification 237`, `Snapshot contracts 238`, `Engine verification 240`, `Client verification 241`, `Engine verification 242`, `Engine verification 243`, `Engine verification 244`, `Engine simulation 246`, `Engine verification 247`, `Client verification 248`, `Motion.Tests 250`, `Client verification 251`, `Client verification 253`, `Engine verification 254`, `Contracts.Tests 257`, `Engine simulation 258`, `Client verification 259`, `Client verification 260`, `Engine verification 261`, `Engine verification 262`, `Engine verification 263`, `Engine verification 264`, `Performance 265`, `Client verification 269`, `Engine simulation 272`, `Engine verification 273`, `Contracts.Tests 277`, `Contracts.Tests 278`, `Client verification 280`, `Engine verification 281`, `Engine verification 282`, `Engine verification 283`, `Engine verification 284`, `Engine verification 285`, `Engine verification 286`, `Engine verification 287`, `EconomyBalance.Tests 292`, `Client interface 295`, `Client verification 296`, `Snapshot contracts 297`, `Engine simulation 298`, `Engine verification 303`, `Engine verification 304`, `Engine verification 305`, `Engine verification 315`, `Engine verification 316`, `Contracts.Tests 324`, `Engine verification 325`, `Client verification 326`, `Engine verification 327`, `Engine verification 328`, `Engine verification 329`, `Engine verification 330`, `Client interface 335`, `Snapshot contracts 337`, `Snapshot contracts 338`, `Client verification 340`, `Engine verification 342`, `Engine verification 345`, `Engine verification 346`, `Client interface 348`, `Client verification 350`, `Client verification 351`, `Engine verification 352`, `Engine verification 353`, `Engine verification 354`, `Engine verification 356`, `Snapshot contracts 357`, `Snapshot contracts 358`, `Engine verification 359`, `Contracts.Tests 360`, `Engine verification 361`, `Engine verification 362`, `Client interface 366`, `Engine simulation 367`, `Client verification 368`, `Engine verification 370`, `Engine verification 371`, `Engine verification 372`, `Engine verification 373`, `Engine verification 374`, `Engine verification 375`, `Engine verification 376`, `Client verification 382`, `Engine simulation 387`, `Engine verification 390`, `Engine verification 391`, `Client verification 399`, `Engine verification 400`, `Contracts.Tests 408`, `Contracts.Tests 409`, `Engine verification 411`?**
  _High betweenness centrality (0.332) - this node is a cross-community bridge._
- **Why does `SimulationEngine` connect `Engine simulation 2` to `Engine verification 3`, `Client verification 7`, `Client verification 8`, `Client verification 11`, `Engine simulation 12`, `Engine verification 15`, `Client verification 17`, `Engine verification 20`, `Engine verification 23`, `Client verification 24`, `Engine verification 26`, `Engine simulation 28`, `Client verification 33`, `Engine simulation 35`, `Engine verification 36`, `Engine simulation 39`, `Client verification 40`, `Engine verification 41`, `Client verification 46`, `Engine simulation 47`, `Engine simulation 50`, `EconomyBalance 51`, `Engine simulation 53`, `Engine verification 55`, `Engine simulation 58`, `Engine simulation 59`, `Engine simulation 60`, `Local session transport 79`, `Client verification 81`, `Engine simulation 82`, `Client verification 85`, `Engine verification 92`, `Engine verification 94`, `Client verification 95`, `Engine verification 96`, `Client verification 99`, `Performance.Tests 101`, `Client interface 103`, `Engine simulation 106`, `Contracts.Tests 114`, `Engine verification 115`, `Engine verification 124`, `Engine simulation 130`, `Client verification 142`, `Client verification 144`, `Client verification 145`, `Engine verification 150`, `Engine verification 153`, `Performance 154`, `Client verification 158`, `EconomyBalance 159`, `Engine verification 168`, `Engine verification 170`, `Performance 171`, `Client verification 189`, `Contracts.Tests 202`, `EconomyBalance.Tests 204`, `Client verification 213`, `Client verification 233`, `Engine simulation 239`, `Engine verification 240`, `Engine simulation 246`, `Engine verification 247`, `Client verification 253`, `Engine verification 254`, `Engine simulation 258`, `Engine verification 261`, `Performance 265`, `Snapshot contracts 297`, `Engine verification 304`, `Contracts.Tests 324`, `Engine verification 325`, `Client verification 326`, `Engine verification 328`, `Engine verification 329`, `Engine verification 330`, `Engine simulation 333`, `Snapshot contracts 337`, `Snapshot contracts 338`, `Snapshot contracts 358`, `Client verification 368`, `Engine verification 370`, `Engine verification 373`, `Engine verification 374`, `Engine verification 375`, `Engine verification 376`, `Client verification 383`, `Engine verification 390`, `Engine verification 391`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **Why does `GameSessionScreen` connect `Client interface 0` to `Client verification 4`, `Client verification 6`, `Client verification 7`, `Client verification 8`, `Client verification 14`, `Client verification 17`, `Client verification 18`, `Client verification 24`, `Client verification 25`, `Client interface 30`, `Client interface 31`, `Client interface 32`, `Client verification 33`, `Client verification 34`, `Client interface 37`, `Client verification 40`, `Client interface 45`, `Client verification 46`, `Client interface 48`, `Client verification 49`, `Client interface 65`, `Client interface 69`, `Local session transport 79`, `Client verification 80`, `Client verification 81`, `Client verification 87`, `Client verification 89`, `Client verification 90`, `Client verification 93`, `Client verification 99`, `Performance.Tests 101`, `Client verification 102`, `Client interface 103`, `Engine simulation 106`, `Client verification 112`, `Client interface 113`, `Client interface 117`, `Client verification 119`, `Client verification 126`, `Client interface 127`, `Client verification 129`, `Client interface 132`, `Client verification 133`, `Client verification 137`, `Client verification 138`, `Client verification 142`, `Motion.Tests 143`, `Client verification 151`, `Client verification 152`, `Performance 154`, `Client verification 156`, `Client verification 158`, `Client interface 163`, `Client interface 165`, `Client verification 169`, `Performance 171`, `Client interface 194`, `Client verification 198`, `Client verification 213`, `Client interface 214`, `Client verification 215`, `Client interface 216`, `Client interface 222`, `Client interface 223`, `Client interface 232`, `Client verification 241`, `Client verification 248`, `Client verification 252`, `Client verification 253`, `Client verification 259`, `Client verification 260`, `Performance 265`, `Client verification 279`, `Client verification 326`, `Client verification 340`, `Client verification 349`, `Snapshot contracts 358`, `Client interface 380`, `Client verification 382`?**
  _High betweenness centrality (0.092) - this node is a cross-community bridge._
- **Are the 107 inferred relationships involving `SimulationEngine` (e.g. with `.AiContentKeepsHumanMarkets()` and `.SavedLegacySessionDoesNotGenerate()`) actually correct?**
  _`SimulationEngine` has 107 INFERRED edges - model-reasoned connections that need verification._
- **Are the 86 inferred relationships involving `GameSessionScreen` (e.g. with `.AiBaseSelectableAndIdentified()` and `.HumanScientificMilitaryNotAi()`) actually correct?**
  _`GameSessionScreen` has 86 INFERRED edges - model-reasoned connections that need verification._
- **Are the 80 inferred relationships involving `AuthoritativeSnapshot` (e.g. with `.Coalesced_snapshots_preserve_first_receipt_and_expire_while_map_is_hidden()` and `.Pause_and_acceleration_do_not_change_effect_lifetime()`) actually correct?**
  _`AuthoritativeSnapshot` has 80 INFERRED edges - model-reasoned connections that need verification._
- **Are the 178 inferred relationships involving `ObjectMotionSnapshot` (e.g. with `.CaptureCombatState()` and `.LaunchCountermeasure()`) actually correct?**
  _`ObjectMotionSnapshot` has 178 INFERRED edges - model-reasoned connections that need verification._