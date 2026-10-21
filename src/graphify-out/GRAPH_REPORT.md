# DSS fresh structural and selective EP-0012 semantic graph

Fresh AST covers every selected .cs/.py file. Markdown headings and links are structural evidence. Retained EP10 report-delivery mechanisms and three fresh EP12 host-agent chunks provide selective semantic coverage; see ep0012-semantic.json and semantic-coverage.json. Historical EP1-4 semantic fragments were replaced rather than silently reused. Token usage is unmeasured: template zeroes below are not measurements. No external API, game or native harness is run by this builder. Graph navigation does not establish runtime, FPS or human acceptance. HTML is an aggregated community view; JSON contains the complete graph. Recorded commit is pre-export HEAD; input hashes describe actual working-tree sources.

Reproduce from PowerShell: `& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe D:/DeepSpaceSaga/DSS/src/graphify-out/rebuild.py --force`. The explicit shrink override replaces historical semantic scope; inspect `semantic-coverage.json` before using it. `--dry-run` validates current source locations without exporting.

Audit: 1112 unresolved AST endpoint edges were explicitly pruned; 2886 reference-only symbols have no local declaration location. Canonicalization produced 794 self-loops, removed from the derived graph. The undirected export combines some relations and canonicalizes equivalent nodes; extraction.json retains original non-loop relationships. See diagnostics.json and the dedicated audit sidecars before interpreting absence of a path.

# Graph Report - DSS  (2026-10-21)

## Corpus Check
- 1103 files · ~853,837 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 16038 nodes · 50778 edges · 514 communities (498 shown, 16 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 5665 edges (avg confidence: 0.83)
- Token cost: unmeasured host-agent usage (see cost.json)

## Graph Freshness
- Built from commit: `60418af1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Rebuild this selected corpus with `rebuild.py`; AST/Markdown extraction is offline and curated semantic evidence tokens are revalidated.

## Community Hubs (Navigation)
- Client interface 0
- Gameplay requirements 1
- Client verification 2
- Engine simulation 3
- Engine simulation 4
- Engine simulation 5
- Client verification 6
- Client verification 7
- Client interface 8
- Engine simulation 9
- Engineering documentation 10
- Engine verification 11
- Client verification 12
- Client interface 13
- Engine simulation 14
- Client verification 15
- Engine simulation 16
- Engine verification 17
- Release requirements 18
- Engine verification 19
- Client verification 20
- Engine verification 21
- Engine verification 22
- Client verification 23
- Client verification 24
- Client interface 25
- Client verification 26
- Client verification 27
- Client interface 28
- Client interface 29
- Engine simulation 30
- Client verification 31
- Client interface 32
- Engine simulation 33
- Engine verification 34
- Engine verification 35
- Client interface 36
- Client interface 37
- Client verification 38
- Client interface 39
- Client verification 40
- Engine verification 41
- Historical module combat 42
- Engine verification 43
- Client interface 44
- Engine simulation 45
- Client interface 46
- Client interface 47
- Client verification 48
- Engine simulation 49
- EconomyBalance 50
- Motion.Tests 51
- Engine verification 52
- Client verification 53
- Client interface 54
- Client interface 55
- Client verification 56
- Client verification 57
- Historical module combat 58
- Point combat contract 59
- Point combat contract 60
- Engine simulation 61
- Engineering documentation 62
- Client interface 63
- Point combat contract 64
- Point combat contract 65
- Client interface 66
- Motion.Tests 67
- Motion.Tests 68
- Point combat contract 69
- Point combat contract 70
- Point combat contract 71
- Point combat contract 72
- Client verification 73
- Client interface 74
- Client interface 75
- Engine simulation 76
- Client interface 77
- Client interface 78
- Engine verification 79
- Client interface 80
- Client interface 81
- Client interface 82
- Client interface 83
- Point combat contract 84
- Local session transport 85
- Motion.Tests 86
- Engine simulation 87
- Point combat contract 88
- Engine verification 89
- Engine verification 90
- Engine verification 91
- Client interface 92
- Client verification 93
- Client interface 94
- Release requirements 95
- Client interface 96
- Client interface 97
- Point combat contract 98
- Point combat contract 99
- Point combat contract 100
- Client verification 101
- Client verification 102
- Point combat contract 103
- Point combat contract 104
- Point combat contract 105
- Point combat contract 106
- Client verification 107
- Motion.Tests 108
- Client verification 109
- Engine simulation 110
- Snapshot contracts 111
- Engineering documentation 112
- Release requirements 113
- Engine verification 114
- Client interface 115
- Client interface 116
- Client verification 117
- Engine verification 118
- Engine simulation 119
- Client verification 120
- Client verification 121
- Client interface 122
- Engine verification 123
- Client verification 124
- Engine verification 125
- Release requirements 126
- Client interface 127
- Motion 128
- Client verification 129
- Client verification 130
- Performance.Tests 131
- Release requirements 132
- Engine simulation 133
- Engine simulation 134
- Client interface 135
- Engine verification 136
- Client verification 137
- Engine simulation 138
- Motion 139
- Engine verification 140
- PortraitAssets 141
- Client interface 142
- Client verification 143
- Contracts.Tests 144
- Engine verification 145
- Engine verification 146
- Engine verification 147
- Engine verification 148
- Client verification 149
- Client verification 150
- Client verification 151
- Engine verification 152
- Engine verification 153
- Client verification 154
- Client interface 155
- Release requirements 156
- Client interface 157
- Client interface 158
- Engine verification 159
- Point combat contract 160
- Point combat contract 161
- Client verification 162
- Client interface 163
- Client verification 164
- Client verification 165
- EconomyBalance 166
- Gameplay requirements 167
- EconomyBalance 168
- Contracts.Tests 169
- Engine verification 170
- Engine simulation 171
- Client interface 172
- CombatSim 173
- Client interface 174
- Snapshot contracts 175
- Contracts.Tests 176
- Client interface 177
- Client interface 178
- Engine verification 179
- Client verification 180
- Client verification 181
- Client verification 182
- Combat report planning 183
- Combat report planning 184
- Engine simulation 185
- Engine simulation 186
- Client interface 187
- Client interface 188
- Engine verification 189
- Engine verification 190
- Engine verification 191
- Engine verification 192
- Client verification 193
- Point combat contract 194
- Point combat contract 195
- Release requirements 196
- Client interface 197
- Snapshot contracts 198
- Client verification 199
- Contracts.Tests 200
- Engine simulation 201
- Client verification 202
- Engine verification 203
- Historical module combat 204
- Contracts.Tests 205
- Client interface 206
- Release requirements 207
- Client interface 208
- Client verification 209
- PortraitAssets 210
- Client interface 211
- Client verification 212
- Contracts.Tests 213
- Engine verification 214
- Motion 215
- Client verification 216
- Client verification 217
- Engine verification 218
- Combat report planning 219
- Combat report planning 220
- Combat report planning 221
- Combat report planning 222
- Combat report planning 223
- Combat report planning 224
- Combat report planning 225
- Combat report planning 226
- Combat report planning 227
- Combat report planning 228
- Client verification 229
- Snapshot contracts 230
- Contracts.Tests 231
- Engine simulation 232
- Engine verification 233
- Engine verification 234
- Engine verification 235
- Performance 236
- 00 Process 237
- Snapshot contracts 238
- EconomyBalance.Tests 239
- Engine verification 240
- Engine verification 241
- Client verification 242
- Client interface 243
- Release requirements 244
- 03 Design 245
- Client interface 246
- Client interface 247
- Client interface 248
- Engine verification 249
- Motion.Tests 250
- EconomyBalance.Tests 251
- Engine verification 252
- Engine verification 253
- Client verification 254
- Client interface 255
- Client interface 256
- Client interface 257
- Snapshot contracts 258
- Client verification 259
- Motion 260
- Client verification 261
- Client verification 262
- EconomyBalance.Tests 263
- Engine verification 264
- Historical module combat 265
- Combat report planning 266
- Client interface 267
- Client interface 268
- Client interface 269
- Client verification 270
- Engine verification 271
- Engine verification 272
- EconomyBalance.Tests 273
- Engine verification 274
- Engine verification 275
- Client interface 276
- Client interface 277
- Client interface 278
- Client verification 279
- Client verification 280
- Client verification 281
- Client verification 282
- Engine verification 283
- Engine verification 284
- Engine verification 285
- Engine verification 286
- Historical module combat 287
- Client verification 288
- Client verification 289
- Client verification 290
- Motion 291
- Client verification 292
- Client verification 293
- Engine verification 294
- Engine verification 295
- Engine verification 296
- Historical module combat 297
- Engine verification 298
- Engine simulation 299
- Contracts.Tests 300
- Client verification 301
- Contracts.Tests 302
- Motion.Tests 303
- Client verification 304
- Client verification 305
- Client verification 306
- Engine verification 307
- Engine verification 308
- Historical module combat 309
- Point combat contract 310
- Engine simulation 311
- Client verification 312
- Client interface 313
- Client interface 314
- Snapshot contracts 315
- Engine verification 316
- Contracts.Tests 317
- Client verification 318
- PortraitAssets 319
- Historical module combat 320
- Combat report planning 321
- EP 0011 short fixes 322
- Contracts.Tests 323
- Engine verification 324
- Client verification 325
- Engine verification 326
- Engine verification 327
- Engine verification 328
- Engine verification 329
- Engine verification 330
- Engine verification 331
- EconomyBalance 332
- Client interface 333
- Combat report planning 334
- Combat report planning 335
- Combat report planning 336
- Point combat contract 337
- Client verification 338
- Release requirements 339
- Engineering documentation 340
- Engine verification 341
- PortraitAssets 342
- Client verification 343
- Engine simulation 344
- Client verification 345
- Client verification 346
- Contracts.Tests 347
- Engine verification 348
- Engine verification 349
- Engine verification 350
- Engine verification 351
- Engine verification 352
- Historical module combat 353
- Combat report planning 354
- Combat report planning 355
- Combat report planning 356
- Combat report planning 357
- Client verification 358
- EconomyBalance.Tests 359
- 00 Process 360
- Client interface 361
- Client verification 362
- Client verification 363
- Client verification 364
- Snapshot contracts 365
- Client verification 366
- Client verification 367
- Client verification 368
- Contracts.Tests 369
- Engine verification 370
- Engine verification 371
- Engine verification 372
- Engine verification 373
- Engine verification 374
- Engine verification 375
- Combat report planning 376
- Combat report planning 377
- Combat report planning 378
- Combat report planning 379
- Combat report planning 380
- Combat report planning 381
- Combat report planning 382
- Combat report planning 383
- EP 0011 short fixes 384
- Client interface 385
- Client verification 386
- Client verification 387
- Engine verification 388
- Engine verification 389
- Engine verification 390
- Historical module combat 391
- Historical module combat 392
- Historical module combat 393
- EP 0011 short fixes 394
- EP 0011 short fixes 395
- EP 0011 short fixes 396
- EP 0011 short fixes 397
- Engine verification 398
- Release requirements 399
- 05 Backlog 400
- Client interface 401
- Client interface 402
- Client interface 403
- Client verification 404
- Client verification 405
- Engine verification 406
- Historical module combat 407
- EP 0011 short fixes 408
- EP 0011 short fixes 409
- EP 0011 short fixes 410
- EP 0011 short fixes 411
- EP 0011 short fixes 412
- EP 0011 short fixes 413
- Point combat contract 414
- Point combat contract 415
- Point combat contract 416
- Point combat contract 417
- Client verification 418
- Client verification 419
- Contracts.Tests 420
- 05 Backlog 421
- Client interface 422
- Engine verification 423
- Client verification 424
- Client verification 425
- Engine verification 426
- Engine verification 427
- Engine verification 428
- Engine verification 429
- PortraitAssets 430
- EP 0011 short fixes 431
- EP 0011 short fixes 432
- Point combat contract 433
- Point combat contract 434
- Point combat contract 435
- Point combat contract 436
- Point combat contract 437
- Client interface 438
- Client verification 439
- Client verification 440
- Client verification 441
- Client verification 442
- Client verification 443
- Engine verification 444
- Engine verification 445
- Engine verification 446
- Historical module combat 447
- Point combat contract 448
- Release requirements 449
- Engine verification 450
- Snapshot contracts 451
- Snapshot contracts 452
- Motion.Tests 453
- Contracts.Tests 454
- Contracts.Tests 455
- Engine verification 456
- Engine verification 457
- Engine verification 458
- Engine verification 459
- Motion.Tests 460
- EP 0011 short fixes 461
- Client verification 462
- Release requirements 463
- Client interface 464
- Client verification 465
- Client verification 466
- Engine verification 467
- Engine verification 468
- Engine verification 469
- Client interface 470
- 03 Design 471
- Client interface 472
- Client verification 473
- Client verification 474
- Contracts.Tests 475
- Contracts.Tests 476
- Engine verification 477
- Engine verification 478
- Release requirements 479
- Release requirements 480
- Release requirements 481
- Engineering documentation 482
- Client verification 483
- Client verification 484
- Client verification 485
- Client verification 486
- Client verification 487
- Contracts.Tests 488
- Engine verification 489
- Engine verification 490
- PortraitAssets 491
- Release requirements 492
- Release requirements 493
- Release requirements 494
- Release requirements 495
- Release requirements 496
- Client interface 497
- Client verification 498
- Client verification 499
- Contracts.Tests 500
- Release requirements 501
- Release requirements 502
- Release requirements 503
- Release requirements 504
- Engineering documentation 505
- Engineering documentation 506
- Snapshot contracts 507
- Client verification 508
- Contracts.Tests 509
- 00 Process 510
- Client interface 511
- 03 Design 512
- 06 Tooling 513

## God Nodes (most connected - your core abstractions)
1. `SimulationEngine` - 718 edges
2. `GameSessionScreen` - 596 edges
3. `DeepSpaceSaga.Contracts` - 556 edges
4. `ObjectMotionSnapshot` - 384 edges
5. `AuthoritativeSnapshot` - 354 edges
6. `ScenarioException` - 277 edges
7. `SnapshotBuffer` - 246 edges
8. `LinearMotionPredictor` - 244 edges
9. `DeepSpaceSaga.Engine.Scenario` - 239 edges
10. `DeepSpaceSaga.Client.Tests` - 213 edges

## Surprising Connections (you probably didn't know these)
- `AdvanceCombatTo` --implements--> `Point aim combat contract`  [INFERRED]
  src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs → Documentation/04-Engineering/PointAimCombat.md
- `ApplyModuleHit` --implements--> `Point aim combat contract`  [INFERRED]
  src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs → Documentation/04-Engineering/PointAimCombat.md
- `ResolveCountermissileBlast` --implements--> `Point aim combat contract`  [INFERRED]
  src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs → Documentation/04-Engineering/PointAimCombat.md
- `ResolvePointDetonations` --implements--> `Point aim combat contract`  [INFERRED]
  src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs → Documentation/04-Engineering/PointAimCombat.md
- `DestroyShip` --implements--> `Own versus observed privacy`  [INFERRED]
  src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs → Documentation/04-Engineering/PointAimCombat.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Combat report delivery pipeline** — src_deepspacesaga_engine_simulationengine_combatreports_mechanism_outbox, src_deepspacesaga_engine_localclient_snapshotmailbox_mechanism_mailbox, src_deepspacesaga_client_snapshotbuffer_mechanism_buffer, src_deepspacesaga_client_combatreportinbox_mechanism_inbox, src_deepspacesaga_client_ui_screens_gamesession_combatreportstore_mechanism_history [INFERRED 0.85]
- **Captured owner flight and public foreign observation privacy boundary** — documentation_04_engineering_pointaimcombat_own_versus_observed_privacy, src_deepspacesaga_contracts_combatsnapshot_deepspacesaga_contracts_ownprojectilesnapshot, src_deepspacesaga_contracts_combatsnapshot_deepspacesaga_contracts_observedprojectilesnapshot, src_deepspacesaga_engine_simulationengine_pointcombat_projectobservedprojectile [INFERRED 0.95]

## Communities (514 total, 16 thin omitted)

### Community 0 - "Client interface 0"
Cohesion: 0.01
Nodes (190): Aim, AlertSignal, CameraZoomTransition, CombatAlertKind, Exact physical blast radius with independent minimum marker, MapCluster, ObjectInfoPanel, ObservedThreatClass (+182 more)

### Community 1 - "Gameplay requirements 1"
Cohesion: 0.01
Nodes (242): 10. Authoritative GameState, General JSON и Settings.json, 11. Сохранение, 12. Журналирование, 13. Критические ошибки и диагностика, 14. Идентификаторы объектов, 15. Генераторы случайных чисел, 16. Статус этой контрольной точки, 17. Раздельная архитектура движка и отрисовки (+234 more)

### Community 2 - "Client verification 2"
Cohesion: 0.04
Nodes (77): DeepSpaceSaga.Client, PendingSceneFrame, BufferedSnapshot, PredictionDeltaMs, Dictionary, Func, CurrentSpeed, EffectivePredictionDeltaMs (+69 more)

### Community 3 - "Engine simulation 3"
Cohesion: 0.03
Nodes (59): EvasionState, Events, HostileLaunch, IssuedQuote, Projectile, QuoteId, RequestId, Queue (+51 more)

### Community 4 - "Engine simulation 4"
Cohesion: 0.05
Nodes (46): Dictionary, From, HashSet, IEnumerable, IReadOnlyDictionary, IReadOnlyList, State, To (+38 more)

### Community 5 - "Engine simulation 5"
Cohesion: 0.04
Nodes (38): PlayerCommand, ImmutableArray, ModuleTypeDefinition, IsPointLauncher, ActiveCycleData, HashSet, ImmutableArray, List (+30 more)

### Community 6 - "Client verification 6"
Cohesion: 0.03
Nodes (61): DeepSpaceSaga.Client.UI.Screens.GameSession.Controls, DeepSpaceSaga.Motion, DeepSpaceSaga.Client.Tests, DeepSpaceSaga.Client.UI, DeepSpaceSaga.Client.UI.Screens.GameSession, ModuleInitializer, ForecastCache, SKColor (+53 more)

### Community 7 - "Client verification 7"
Cohesion: 0.07
Nodes (26): X, Y, SKCanvas, Action, DateTime, Fact, Func, H (+18 more)

### Community 8 - "Client interface 8"
Cohesion: 0.05
Nodes (60): IEnumerator, IReadOnlyList, MaxX, MaxY, MinX, MinY, ObjectTrailBuffer, Bounds (+52 more)

### Community 9 - "Engine simulation 9"
Cohesion: 0.04
Nodes (57): CatalogCompatibilityData, CombatReactionSaveData, CombatReportsSaveValidation, Combat report save validation, IReadOnlyList, CombatSaveValidation, CombatStateData, LauncherSaveData (+49 more)

### Community 10 - "Engineering documentation 10"
Cohesion: 0.02
Nodes (103): Решения первого релиза, Требования первого релиза, Экипаж, пассажиры и жилой модуль, Current EP-0012 contract — 2026-10-21, Цель, Актуализация EP-0008 — 2026-10-12, За рамками первого релиза, Требования первого релиза (+95 more)

### Community 11 - "Engine verification 11"
Cohesion: 0.07
Nodes (30): Due, Ice, Pending, StationTravelCommand, FactoryTypeDefinition, ImmutableArray, RecipeDefinition, Steel (+22 more)

### Community 12 - "Client verification 12"
Cohesion: 0.04
Nodes (48): SKRect, IEnumerable, ValueTask, PointCombatSessionFixture, Connection, Engine, Fact, InlineData (+40 more)

### Community 13 - "Client interface 13"
Cohesion: 0.04
Nodes (56): AimShadow, CounterAimPreview, EvadeInputPreview, PointAimState, WeaponPanelInstance, Key, HashSet, ImmutableArray (+48 more)

### Community 14 - "Engine simulation 14"
Cohesion: 0.05
Nodes (76): CommandDefinitionDto, CommandDefinitionsFile, EconomyContentVersions, EngineSettingsFile, FactoryTypeDefinitionDto, FactoryTypesFile, Items, ItemTypeDefinitionDto (+68 more)

### Community 15 - "Client verification 15"
Cohesion: 0.08
Nodes (27): EngineTradeConnection, CancellationToken, Entry, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, List (+19 more)

### Community 16 - "Engine simulation 16"
Cohesion: 0.03
Nodes (45): DeepSpaceSaga.Contracts, DeepSpaceSaga.Engine.Scenario, DeepSpaceSaga.Engine.Combat, DeepSpaceSaga.Engine, CharacterPortraits, NavigationComputerCommandTypes, ScannerCommandTypes, ShipEventReasonCodes (+37 more)

### Community 17 - "Engine verification 17"
Cohesion: 0.05
Nodes (45): CommandTrace, DeepSpaceSaga.CombatAcceptance, Last, Pose, RunResult, Dictionary, Guid, IReadOnlyList (+37 more)

### Community 18 - "Release requirements 18"
Cohesion: 0.02
Nodes (84): Сверка реализации — 2026-10-10, Требования первого релиза, Минимальные товары-кандидаты, Уже существующая основа, Грузовой отсек, Требует решения, Решение первого релиза, Цель (+76 more)

### Community 19 - "Engine verification 19"
Cohesion: 0.06
Nodes (33): ChannelReader, Cts, LoopTask, Reader, Time, Fact, ObjectInteractionStateTests, CancellationToken (+25 more)

### Community 20 - "Client verification 20"
Cohesion: 0.09
Nodes (22): Enabled, CancellationToken, Dictionary, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, ImmutableArray (+14 more)

### Community 21 - "Engine verification 21"
Cohesion: 0.10
Nodes (6): Fact, InlineData, SimulationClock, SimulationEngine, Theory, EngineCommandTests

### Community 22 - "Engine verification 22"
Cohesion: 0.08
Nodes (13): StationClusterSaveValidation, CancellationToken, IAsyncEnumerable, Action, Fact, IEnumerable, InlineData, SimulationEngine (+5 more)

### Community 23 - "Client verification 23"
Cohesion: 0.05
Nodes (34): MouseButton, MapCluster, CameraState, IReadOnlyList, SKRect, TacticalMapSettings, MapViewGeometry, MapWorldBounds (+26 more)

### Community 24 - "Client verification 24"
Cohesion: 0.07
Nodes (19): SKPoint, SKRect, SKSize, ObjectLabelLayout, ObjectLabelText, UnknownLabel, StatusSquareAnimator, TacticalMapMarkerPolicy (+11 more)

### Community 25 - "Client interface 25"
Cohesion: 0.05
Nodes (60): DisplayEntry, Maximum, Entry, Command, Dictionary, Id, IReadOnlyList, List (+52 more)

### Community 26 - "Client verification 26"
Cohesion: 0.11
Nodes (18): ActiveObjectId, SelectedObjectId, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, IEnumerable, List (+10 more)

### Community 27 - "Client verification 27"
Cohesion: 0.12
Nodes (16): Fact, GameSessionHandle, ImmutableArray, InlineData, RecordingConnection, SKBitmap, SKRect, SnapshotBuffer (+8 more)

### Community 28 - "Client interface 28"
Cohesion: 0.08
Nodes (31): DeepSpaceSaga.Client.UI.Screens.Finance, FinanceRow, FinanceLayout, ImmutableArray, IReadOnlyList, Key, MouseButton, SKBitmap (+23 more)

### Community 29 - "Client interface 29"
Cohesion: 0.08
Nodes (23): GL, GRBackendRenderTarget, GRContext, IInputContext, IWindow, RawImage, ScrollWheel, SKSurface (+15 more)

### Community 30 - "Engine simulation 30"
Cohesion: 0.05
Nodes (34): DeepSpaceSaga.Engine.Rng, ModuleHitOutcome, NpcDodgeThreat, ImmutableArray, CombatJournalEntry, ModuleHitLocationRng, Counter, State (+26 more)

### Community 31 - "Client verification 31"
Cohesion: 0.10
Nodes (19): EconomicFixture, Fact, Func, H, InlineData, InvalidOperationException, ObjectDisposedException, OperationCanceledException (+11 more)

### Community 32 - "Client interface 32"
Cohesion: 0.07
Nodes (43): Button, CommandIconPair, Dictionary, Func, Guid, HashSet, IEnumerable, ImmutableArray (+35 more)

### Community 33 - "Engine simulation 33"
Cohesion: 0.05
Nodes (44): DeepSpaceSaga.Engine.Dialogue, DialogueChoiceSnapshot, ImmutableDictionary, DialogueEvent, ImmutableArray, ImmutableDictionary, ImmutableHashSet, DialogueState (+36 more)

### Community 34 - "Engine verification 34"
Cohesion: 0.10
Nodes (25): Fact, InlineData, Theory, AutomaticDefenseTests, Fact, InlineData, Theory, CountermeasureBootstrapTests (+17 more)

### Community 35 - "Engine verification 35"
Cohesion: 0.10
Nodes (18): Fact, InlineData, SimulationClock, SimulationEngine, Theory, ApproachCommandTests, Fact, InlineData (+10 more)

### Community 36 - "Client interface 36"
Cohesion: 0.09
Nodes (29): DeepSpaceSaga.Client.UI.Screens.Dialogue, IScreen, Person, SKRect, DialogueLayout, GameSessionHandle, Key, MouseButton (+21 more)

### Community 37 - "Client interface 37"
Cohesion: 0.07
Nodes (41): DeepSpaceSaga.Client.UI.Screens.Trade, InputFocus, QuoteKey, CancellationTokenSource, Entry, GameSessionHandle, IReadOnlyList, Key (+33 more)

### Community 38 - "Client verification 38"
Cohesion: 0.07
Nodes (26): DeepSpaceSaga.Client.UI.Screens.MainMenu, Key, MouseButton, SKCanvas, SKPaint, SKRect, MainMenuScreen, SKRect (+18 more)

### Community 39 - "Client interface 39"
Cohesion: 0.06
Nodes (36): LabelMetrics, SmoothState, SKPoint, SKRect, ObjectLabelGeometry, Dictionary, Func, HashSet (+28 more)

### Community 40 - "Client verification 40"
Cohesion: 0.10
Nodes (6): Fact, SKBitmap, SKColor, SKPaint, SKRect, StationToolbarTests

### Community 41 - "Engine verification 41"
Cohesion: 0.10
Nodes (25): ItemTypeDefinition, Dictionary, IEnumerable, ImmutableArray, ImmutableDictionary, StationMarketEconomyDefinition, AllStockTargets, StationMarketProductionSource (+17 more)

### Community 42 - "Historical module combat 42"
Cohesion: 0.03
Nodes (66): 1. Цель, 2.1 Переопределяется (правила EP-0008 перестают действовать), 2.2 Переиспользуется без изменения смысла, 2.3 Статус EP-0008 после EP-0009, 2. Что EP-0009 делает с EP-0008, 3.1 Технические допущения (из симулятора; автор явно не утверждал), 3. Решения (источник — обсуждение с автором 2026-10-07…11), 3.2 Порядок событий одного физического времени (дополняет таблицу EP-0008) (+58 more)

### Community 43 - "Engine verification 43"
Cohesion: 0.10
Nodes (24): DialogueAction, Abort, Choose, Start, DialogueCommand, Fact, ImmutableArray, InlineData (+16 more)

### Community 44 - "Client interface 44"
Cohesion: 0.09
Nodes (28): ActiveObjectId, CancellationToken, CancellationTokenSource, Channel, Exception, SelectedObjectId, Task, TradeJournal (+20 more)

### Community 45 - "Engine simulation 45"
Cohesion: 0.07
Nodes (21): MarketEventFlow, MarketItemLimits, MaximumBudget, ImmutableArray, StationMarketKnowledgeSnapshot, StationMarketStockBandSnapshot, IReadOnlyDictionary, StationSize (+13 more)

### Community 46 - "Client interface 46"
Cohesion: 0.06
Nodes (62): SKColor, CombatVisualSettings, Countermeasure, CountermeasureIntercept, CountermeasurePrediction, CountermeasureTrail, Default, DefenseRange (+54 more)

### Community 47 - "Client interface 47"
Cohesion: 0.11
Nodes (28): DeepSpaceSaga.Client.UI.Screens.Save, H, SKRect, W, X, Y, SaveHit, SaveLayout (+20 more)

### Community 48 - "Client verification 48"
Cohesion: 0.11
Nodes (19): NavigationTrajectoryProjector, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, InlineData, IReadOnlyList, List (+11 more)

### Community 49 - "Engine simulation 49"
Cohesion: 0.06
Nodes (33): LauncherCombatSnapshot, DefenseSnapshot, WeaponOperatorSnapshot, WeaponSkillType, CountermeasureDefense, TorpedoAttack, IReadOnlyList, CountermeasureProjectileSaveData (+25 more)

### Community 50 - "EconomyBalance 50"
Cohesion: 0.09
Nodes (36): Category, Action, Command, Dictionary, Func, IEnumerable, ImmutableArray, ItemId (+28 more)

### Community 51 - "Motion.Tests 51"
Cohesion: 0.11
Nodes (13): ApproachFlyThroughPlan, FamilyFormula, Plan, X, Y, Fact, InlineData, List (+5 more)

### Community 52 - "Engine verification 52"
Cohesion: 0.08
Nodes (33): ActualDays, StraightDays, InlineData, Task, Theory, ClusterLocalResumeTests, Settings, Fact (+25 more)

### Community 53 - "Client verification 53"
Cohesion: 0.08
Nodes (28): QueuedCue, Channel, Task, CombatAlertAudio, CombatAlertCue, CombatAlertKind, CombatStarted, Danger (+20 more)

### Community 54 - "Client interface 54"
Cohesion: 0.12
Nodes (20): Alpha, RadiusMultiplier, ReticleGlow, IReadOnlyList, SKCanvas, SKColor, SKImage, SKPaint (+12 more)

### Community 55 - "Client interface 55"
Cohesion: 0.09
Nodes (30): FileNotFoundException, IDisposable, Action, Image, Key, Label, List, MouseButton (+22 more)

### Community 56 - "Client verification 56"
Cohesion: 0.10
Nodes (27): IOException, CancellationToken, Channel, Exception, Func, IAsyncEnumerable, ImmutableArray, SnapshotMailbox (+19 more)

### Community 57 - "Client verification 57"
Cohesion: 0.14
Nodes (14): CameraState, List, X, Y, FutureTrajectoryPoint, FutureTrajectoryProjector, ObjectMotionSnapshot, Fact (+6 more)

### Community 58 - "Historical module combat 58"
Cohesion: 0.04
Nodes (52): EP-0009 published ticket ledger, Продолжение2026-10-16: EP-0010, EP-0009 canonical documentation, Выполненная работа и проверки, Контракт и блокер, Условие продолжения, Проверенные факты, EP-0009 — проверка базы исполнения (+44 more)

### Community 59 - "Point combat contract 59"
Cohesion: 0.04
Nodes (51): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Save18 subcomponent automated review (+43 more)

### Community 60 - "Point combat contract 60"
Cohesion: 0.04
Nodes (51): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+43 more)

### Community 61 - "Engine simulation 61"
Cohesion: 0.10
Nodes (22): Consumed, Cost, DeepSpaceSaga.Engine.Trading, Reserved, TradingMapEdgeData, TradingMapStateData, IReadOnlyList, IEnumerable (+14 more)

### Community 62 - "Engineering documentation 62"
Cohesion: 0.04
Nodes (51): Общие правила, Сверка реализации — 2026-10-10, Компактные плашки — 2026-10-18, Палитра — 2026-10-18, Внешний линейный прогноз игрока — 2026-10-18, Правила отображения, Палитра — траектории и тактические обозначения, Цвета (+43 more)

### Community 63 - "Client interface 63"
Cohesion: 0.12
Nodes (18): IReadOnlyList, SKCanvas, SKColor, SKPaint, SKRect, GridPanel, GridSortColumn, BuyingCount (+10 more)

### Community 64 - "Point combat contract 64"
Cohesion: 0.04
Nodes (50): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Story execution review — 2026-10-20 (+42 more)

### Community 65 - "Point combat contract 65"
Cohesion: 0.04
Nodes (50): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+42 more)

### Community 66 - "Client interface 66"
Cohesion: 0.13
Nodes (13): DeepSpaceSaga.Client.UI.Controls, TradeItemPresentation, SKCanvas, SKColor, SKPaint, SKRect, SKTextAlign, SKTypeface (+5 more)

### Community 67 - "Motion.Tests 67"
Cohesion: 0.14
Nodes (16): ApproachRoute, DurationMs, Length, Direction, X, Y, ApproachLineCaptureMath, ApproachRoute (+8 more)

### Community 68 - "Motion.Tests 68"
Cohesion: 0.08
Nodes (19): Span, X, Y, ArgumentOutOfRangeException, Fact, InlineData, Theory, AbsoluteOrbitMathTests (+11 more)

### Community 69 - "Point combat contract 69"
Cohesion: 0.04
Nodes (49): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+41 more)

### Community 70 - "Point combat contract 70"
Cohesion: 0.04
Nodes (49): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+41 more)

### Community 71 - "Point combat contract 71"
Cohesion: 0.04
Nodes (49): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+41 more)

### Community 72 - "Point combat contract 72"
Cohesion: 0.04
Nodes (49): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+41 more)

### Community 73 - "Client verification 73"
Cohesion: 0.06
Nodes (24): DeepSpaceSaga.Engine.Content, Final, Reports, CommandDefinition, FactorDefaults, Fact, AbandonedObjectContentTests, Fact (+16 more)

### Community 74 - "Client interface 74"
Cohesion: 0.09
Nodes (23): DeepSpaceSaga.Client.UI.Screens.GameMenu, SKRect, GameMenuButton, Load, MainMenu, None, Resume, Save (+15 more)

### Community 75 - "Client interface 75"
Cohesion: 0.12
Nodes (24): H, SKRect, W, X, Y, LoadHit, LoadLayout, ListHeight (+16 more)

### Community 76 - "Engine simulation 76"
Cohesion: 0.08
Nodes (38): Action, HashSet, IEnumerable, ImmutableArray, GameDataRegistry, CatalogCompatibility, CatalogVersion, CombatRules (+30 more)

### Community 77 - "Client interface 77"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Hire, HireLayout, Key, MouseButton, SnapshotBuffer, HireScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 78 - "Client interface 78"
Cohesion: 0.08
Nodes (24): PoiGeometry, Stopwatch, PauseResumeDiagnostics, CameraState, IEnumerable, IReadOnlyList, SKCanvas, SKPath (+16 more)

### Community 79 - "Engine verification 79"
Cohesion: 0.15
Nodes (14): Fact, Func, IEnumerable, ImmutableDictionary, InlineData, IReadOnlyList, JsonNode, SimulationEngine (+6 more)

### Community 80 - "Client interface 80"
Cohesion: 0.08
Nodes (46): ImmutableSortedDictionary, Dictionary, CharacterAppearance, Age, Colors, Gender, LibraryVersion, Parts (+38 more)

### Community 81 - "Client interface 81"
Cohesion: 0.08
Nodes (42): Key, MouseButton, IScreen, ScreenEvent, CloseContracts, CloseDialogue, CloseFinance, CloseHire (+34 more)

### Community 82 - "Client interface 82"
Cohesion: 0.12
Nodes (17): SKBitmap, SKCanvas, SKColor, SKPaint, SKRect, StationToolbar, HasLoadedCrewImage, HasLoadedExitButtonImage (+9 more)

### Community 83 - "Client interface 83"
Cohesion: 0.11
Nodes (28): Dictionary, IReadOnlyList, Label, List, SKBitmap, SKCanvas, SKPaint, SKRect (+20 more)

### Community 84 - "Point combat contract 84"
Cohesion: 0.04
Nodes (44): Входное техническое задание, Итог технического исполнения — 2026-10-21, Automated story acceptance — 2026-10-20, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants (+36 more)

### Community 85 - "Local session transport 85"
Cohesion: 0.10
Nodes (22): CancellationToken, Guid, IAsyncEnumerable, ValueTask, ICombatPresentationConnection, SimulationControlAcknowledgement, CancellationToken, CancellationTokenSource (+14 more)

### Community 86 - "Motion.Tests 86"
Cohesion: 0.11
Nodes (17): HostileCombatPhase, FlankApproach, OrbitEntry, OrbitHold, Pursuit, Retreat, HostileCombatSnapshot, HostileMotionSample (+9 more)

### Community 87 - "Engine simulation 87"
Cohesion: 0.12
Nodes (16): AiBaseGenerator, EnvironmentFieldGenerator, IReadOnlyList, PointOfInterestGenerator, IReadOnlyList, AiGenerationConfig, EnvironmentGenerationConfig, PoiTemplate (+8 more)

### Community 88 - "Point combat contract 88"
Cohesion: 0.04
Nodes (43): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Исполнение и self-review — 2026-10-20 (+35 more)

### Community 89 - "Engine verification 89"
Cohesion: 0.14
Nodes (6): Fact, InlineData, Theory, CombatReportsSaveSchemaTests, Fact, ScenarioLoaderTests

### Community 90 - "Engine verification 90"
Cohesion: 0.21
Nodes (11): ArgumentNullException, Fact, ImmutableDictionary, InlineData, InstalledModuleRuntime, IReadOnlyList, SimulationEngine, SpaceObjectRuntime (+3 more)

### Community 91 - "Engine verification 91"
Cohesion: 0.24
Nodes (11): Fact, Func, IEnumerable, InlineData, InstalledModuleRuntime, Lazy, List, SimulationEngine (+3 more)

### Community 92 - "Client interface 92"
Cohesion: 0.14
Nodes (15): DeepSpaceSaga.Client.UI.Screens.Contracts, ContractsLayout, Key, MouseButton, SnapshotBuffer, ContractsScreen, IsCrewTooltipVisible, IsFoodRationsTooltipVisible (+7 more)

### Community 93 - "Client verification 93"
Cohesion: 0.10
Nodes (26): DeepSpaceSaga.Client.UI.Screens, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task (+18 more)

### Community 94 - "Client interface 94"
Cohesion: 0.09
Nodes (27): DateTimeOffset, SnapshotPrediction, SnapshotPrediction, SnapshotPrediction, AssemblyInformationalVersionAttribute, TacticalMapFrameProfile, TacticalMapFrameRecorder, TacticalMapProfileCapture (+19 more)

### Community 95 - "Release requirements 95"
Cohesion: 0.04
Nodes (43): 1. Причина проблемы, 2. Согласованные требования, 3.1. Продвижение Engine, 3.2. Данные боевых обновлений, 3. Техническое решение, 4.1. Единые часы сцены, 4.2. Попадания и промахи, 4.3. Короткие полёты и эффекты (+35 more)

### Community 96 - "Client interface 96"
Cohesion: 0.11
Nodes (20): GameSessionHandle, ImmutableArray, Key, Row, SKCanvas, SKRect, SnapshotBuffer, StationScreen (+12 more)

### Community 97 - "Client interface 97"
Cohesion: 0.14
Nodes (16): CombatReportAssets, CombatReportFormatter, CombatReportStore, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+8 more)

### Community 98 - "Point combat contract 98"
Cohesion: 0.05
Nodes (40): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+32 more)

### Community 99 - "Point combat contract 99"
Cohesion: 0.05
Nodes (40): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+32 more)

### Community 100 - "Point combat contract 100"
Cohesion: 0.05
Nodes (40): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+32 more)

### Community 101 - "Client verification 101"
Cohesion: 0.10
Nodes (22): AssemblyInformationalVersionAttribute, HashSet, IEnumerable, List, FrameStatistics, MapFrameContext, MapFrameCounts, MapFrameEvidence (+14 more)

### Community 102 - "Client verification 102"
Cohesion: 0.18
Nodes (8): buffer, Fact, ImmutableArray, screen, SnapshotBuffer, Task, ObjectInfoPanelTests, SettingsPath

### Community 103 - "Point combat contract 103"
Cohesion: 0.05
Nodes (39): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+31 more)

### Community 104 - "Point combat contract 104"
Cohesion: 0.05
Nodes (39): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+31 more)

### Community 105 - "Point combat contract 105"
Cohesion: 0.05
Nodes (39): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+31 more)

### Community 106 - "Point combat contract 106"
Cohesion: 0.05
Nodes (39): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+31 more)

### Community 107 - "Client verification 107"
Cohesion: 0.15
Nodes (21): ControlledConnection, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, List, SnapshotBuffer, Task (+13 more)

### Community 108 - "Motion.Tests 108"
Cohesion: 0.09
Nodes (19): DeepSpaceSaga.Motion.Tests, Func, CurrentManeuverForecast, Pose, ArgumentException, Fact, InlineData, Theory (+11 more)

### Community 109 - "Client verification 109"
Cohesion: 0.15
Nodes (13): IEnumerable, Stack, ScreenStack, Count, Current, UnderCurrent, Fact, Key (+5 more)

### Community 110 - "Engine simulation 110"
Cohesion: 0.09
Nodes (28): LaunchProfileSnapshot, OwnProjectileSnapshot, EvasionSnapshot, Save18 geometric and history continuation integrity, IEnumerable, IReadOnlyList, EncounterSaveData, EvasionSaveData (+20 more)

### Community 111 - "Snapshot contracts 111"
Cohesion: 0.09
Nodes (34): CombatReportPresentation, CombatReportFormatter, CombatReportStore, ImmutableArray, SKPaint, SKRect, CombatReportCardLayout, CombatReportPanelLayout (+26 more)

### Community 112 - "Engineering documentation 112"
Cohesion: 0.05
Nodes (38): Сверка реализации — 2026-10-10, Стыковка, Решения первого релиза, Требования первого релиза, Цель, Актуализация EP-0004 — 2026-10-08, Статус реализации (MVP), Проверки и ограничения приёмки (+30 more)

### Community 113 - "Release requirements 113"
Cohesion: 0.05
Nodes (39): Сверка реализации — 2026-10-10, Размер станции, Склад станции и локальный рынок, Ограничения торговли, Стартовая станция, Цена и состояние запаса, Модель станции, Требования к сохранению (+31 more)

### Community 114 - "Engine verification 114"
Cohesion: 0.15
Nodes (14): Fact, InlineData, Theory, CombatForecastEngineManeuverTests, Fact, InlineData, SimulationEngine, Theory (+6 more)

### Community 115 - "Client interface 115"
Cohesion: 0.12
Nodes (17): DeepSpaceSaga.Client.UI.Screens.Ship, Bottom, Left, Right, Top, ShipButton, Close, None (+9 more)

### Community 116 - "Client interface 116"
Cohesion: 0.12
Nodes (18): Action, Func, H, IReadOnlyList, Key, MouseButton, SKCanvas, SKPaint (+10 more)

### Community 117 - "Client verification 117"
Cohesion: 0.24
Nodes (5): MouseButton, Fact, InlineData, Theory, StationScreenTests

### Community 118 - "Engine verification 118"
Cohesion: 0.20
Nodes (6): RecipeMaterial, Fact, ItemTypeId, Quantity, SimulationEngine, StationEconomyBatch2Tests

### Community 119 - "Engine simulation 119"
Cohesion: 0.15
Nodes (19): Func, IEnumerable, ImmutableArray, IReadOnlyList, Random, ResourceFieldAsteroidData, ResourceFieldCountData, ResourceFieldKindData (+11 more)

### Community 120 - "Client verification 120"
Cohesion: 0.10
Nodes (24): After, Before, English, ExpectedEconomy, FirstHour, ProfileId, Russian, StationId (+16 more)

### Community 121 - "Client verification 121"
Cohesion: 0.13
Nodes (18): IDictionary, IGameSessionFactory, Fact, Func, GameSessionHandle, InlineData, Task, Theory (+10 more)

### Community 122 - "Client interface 122"
Cohesion: 0.11
Nodes (20): ScenarioRow, Func, H, IReadOnlyList, Key, Label, List, MouseButton (+12 more)

### Community 123 - "Engine verification 123"
Cohesion: 0.16
Nodes (12): ScenarioMetadata, StartingScenario, Fact, ImmutableDictionary, InlineData, IReadOnlyList, Theory, StartingScenario (+4 more)

### Community 124 - "Client verification 124"
Cohesion: 0.19
Nodes (5): Fact, GameSessionScalePanelTests, Fact, GameSessionZoomTests, LogTailReader

### Community 125 - "Engine verification 125"
Cohesion: 0.13
Nodes (14): Fact, SimulationEngine, ClusterResourcePlacementTests, Fact, GenerationInputSchemaTests, Fact, KnownMapProjectionTests, Fact (+6 more)

### Community 126 - "Release requirements 126"
Cohesion: 0.05
Nodes (38): 10. Проверка интересности торговой карты, 11. Информация, доступная игроку, 12. Рекомендуемая нарезка на пользовательские истории, 13. Definition of Done для всей инициативы, 1. Цель MVP, Сверка реализации — 2026-10-10, 2. Жёсткие границы MVP, 3. Игровой экономический интервал (+30 more)

### Community 127 - "Client interface 127"
Cohesion: 0.11
Nodes (19): DeepSpaceSaga.Client.UI.Screens.Load, Action, Func, H, IReadOnlyList, Key, MouseButton, SKCanvas (+11 more)

### Community 128 - "Motion 128"
Cohesion: 0.15
Nodes (15): ReadOnlySpan, Second, ClosestX, ClosestY, First, IsArrived, Span, ApproachFlyThroughPlan (+7 more)

### Community 129 - "Client verification 129"
Cohesion: 0.14
Nodes (15): Task, Fact, InlineData, Task, Theory, CombatReportsAcceptanceTests, CancellationToken, Channel (+7 more)

### Community 130 - "Client verification 130"
Cohesion: 0.09
Nodes (20): DeepSpaceSaga.Engine.LocalClient, ScenarioRepository, Fact, Task, AllScenarioSystemContentTests, Fact, InlineData, Theory (+12 more)

### Community 131 - "Performance.Tests 131"
Cohesion: 0.11
Nodes (16): DeepSpaceSaga.Performance.Tests, IEnumerable, JsonElement, SolarMapEvidence, Fact, ClusterPerformanceReportTests, Root, Fact (+8 more)

### Community 132 - "Release requirements 132"
Cohesion: 0.05
Nodes (36): 10. Генерация новой игры, 11. Интеграция стартовых сценариев, 12. Полностью открытая карта и будущий туман войны, 13. Отображение и взаимодействие, 14. Архитектурные границы и модель данных, 15. Сохранение и продолжение, 16.1. Расхождения, которые нужно разрешить при подготовке эпика, 16. Проверенная основа и необходимые изменения (+28 more)

### Community 133 - "Engine simulation 133"
Cohesion: 0.19
Nodes (12): More, Func, SimulationClock, GameTimeMs, SimulationTimeMs, Speed, SimulationClockState, MotionTimeMs (+4 more)

### Community 134 - "Engine simulation 134"
Cohesion: 0.11
Nodes (16): RealizedCostCredits, Remaining, CargoAcquisitionSources, ImmutableArray, ImmutableHashSet, IReadOnlyList, VoyageFinancesForTests, VoyageAmountKind (+8 more)

### Community 135 - "Client interface 135"
Cohesion: 0.13
Nodes (10): LocalGameSessionFactory, QuickSavePath, SaveDirectory, ScenariosDirectory, SettingsPath, Program, SaveSlotNaming, SaveSlotRepository (+2 more)

### Community 136 - "Engine verification 136"
Cohesion: 0.13
Nodes (21): Action, Command, Func, IReadOnlyList, Item, List, Quantity, Result (+13 more)

### Community 137 - "Client verification 137"
Cohesion: 0.16
Nodes (10): IReadOnlyList, SKCanvas, SKColor, SKPaint, GridRenderer, Fact, InlineData, SKBitmap (+2 more)

### Community 138 - "Engine simulation 138"
Cohesion: 0.14
Nodes (5): Random, RngStreamNames, RngStreamSeedDerivation, Fact, RngStreamNamesTests

### Community 139 - "Motion 139"
Cohesion: 0.17
Nodes (9): ClosestX, ClosestY, IsArrived, NavigationStepResult, NavigationWaypointMath, StagedNavigationStepResult, StagedNavigationStepResult, Fact (+1 more)

### Community 140 - "Engine verification 140"
Cohesion: 0.19
Nodes (10): Fact, IEnumerable, InlineData, ItemTypeId, Quantity, Registry, SimulationEngine, SpaceObjectRuntime (+2 more)

### Community 141 - "PortraitAssets 141"
Cohesion: 0.13
Nodes (13): FaceAssets, Dictionary, Left, Right, SKBitmap, SKPoint, SKPoint3, FaceEyeAssets (+5 more)

### Community 142 - "Client interface 142"
Cohesion: 0.14
Nodes (14): DeepSpaceSaga.Client.UI.Assets, SKRect, SKBitmap, SKCanvas, SKPaint, SKRect, XenonComboBox, HasAssets (+6 more)

### Community 143 - "Client verification 143"
Cohesion: 0.12
Nodes (17): ExpectedProfile, IAsyncEnumerator, CancellationToken, Fact, Func, IReadOnlyDictionary, ItemId, MemberData (+9 more)

### Community 144 - "Contracts.Tests 144"
Cohesion: 0.15
Nodes (14): ImmutableArray, CommandReasonCodes, CommandResult, CommandResultStatus, Cancelled, Deferred, Executed, Failed (+6 more)

### Community 145 - "Engine verification 145"
Cohesion: 0.25
Nodes (11): ContentException, Fact, InlineData, JsonNode, JsonObject, Theory, CombatContentLoaderTests, ClassesPath (+3 more)

### Community 146 - "Engine verification 146"
Cohesion: 0.15
Nodes (13): ImmutableArray, ModuleCategoryDefinition, InlineData, Theory, PointCombatUS0002TK0001Tests, InlineData, Theory, PointCombatUS0002TK0002Tests (+5 more)

### Community 147 - "Engine verification 147"
Cohesion: 0.12
Nodes (14): Fact, InlineData, Theory, FullClusterContentTests, Fact, InlineData, Theory, CombatBootstrapTests (+6 more)

### Community 148 - "Engine verification 148"
Cohesion: 0.13
Nodes (15): Fact, InlineData, Theory, GeneratedWorldPersistenceTests, Fact, InlineData, SimulationEngine, Theory (+7 more)

### Community 149 - "Client verification 149"
Cohesion: 0.13
Nodes (19): Fixture, CancellationToken, Func, GameSessionHandle, IAsyncEnumerable, InlineData, List, RecordingConnection (+11 more)

### Community 150 - "Client verification 150"
Cohesion: 0.12
Nodes (18): Session, CombatInputSettings, Fact, Func, GameSessionHandle, HashSet, InlineData, InvalidDataException (+10 more)

### Community 151 - "Client verification 151"
Cohesion: 0.16
Nodes (15): Key, SKCanvas, SKPaint, SKRect, Stopwatch, TextInputBox, CaretVisible, Length (+7 more)

### Community 152 - "Engine verification 152"
Cohesion: 0.21
Nodes (12): TradePriceReason, ArgumentNullException, ArgumentOutOfRangeException, Fact, ImmutableArray, InlineData, OverflowException, Theory (+4 more)

### Community 153 - "Engine verification 153"
Cohesion: 0.14
Nodes (12): OrbitalMotionMath, Fact, MovingTerritoryDataTests, Fact, InlineData, SimulationEngine, Theory, SeededAiBasesTests (+4 more)

### Community 154 - "Client verification 154"
Cohesion: 0.17
Nodes (10): ArgumentException, Fact, InlineData, SKBitmap, SKCanvas, Theory, X, Y (+2 more)

### Community 155 - "Client interface 155"
Cohesion: 0.14
Nodes (19): Belt, Lod, CameraState, Dictionary, SKCanvas, SKColor, SKPath, SKRect (+11 more)

### Community 156 - "Release requirements 156"
Cohesion: 0.07
Nodes (31): Point aiming,manual defense and engine evasion input contract, Сверка реализации — 2026-10-10, Требования первого релиза, Уже существующая основа, Тактическая карта и маневрирование, Сближение Approach, Current EP-0012 contract — 2026-10-21, Команды первого релиза (+23 more)

### Community 157 - "Client interface 157"
Cohesion: 0.15
Nodes (17): AssemblyInformationalVersionAttribute, Channel, Dictionary, Exception, IReadOnlyList, JsonSerializerOptions, SemaphoreSlim, Task (+9 more)

### Community 158 - "Client interface 158"
Cohesion: 0.18
Nodes (9): CameraState, FocusX, FocusY, PixelsPerWorldUnit, CameraZoomTransition, Active, Fact, CameraStateZoomTests (+1 more)

### Community 159 - "Engine verification 159"
Cohesion: 0.19
Nodes (8): Fact, InlineData, Theory, MarketKnowledgeTests, Fact, InlineData, Theory, RepeatableTradingVoyageTests

### Community 160 - "Point combat contract 160"
Cohesion: 0.06
Nodes (29): Входное техническое задание, Итог технического исполнения — 2026-10-21, Decision / review log, Decisions / Assumptions, Dependencies, Gaps and backlog, Non-goals / Invariants, Ticket map (+21 more)

### Community 161 - "Point combat contract 161"
Cohesion: 0.08
Nodes (27): Complete2000-battle production corpus, Dodge balance FAIL:96.6% wins and P90 7.493333 real minutes, EP-0012 production balance — 2026-10-21, Fixed public-observation-v1 input policy, Confirmed defects and verification, EP-0012 native evidence — 2026-10-21, Remaining acceptance, Reproduction (+19 more)

### Community 162 - "Client verification 162"
Cohesion: 0.15
Nodes (15): IAsyncDisposable, CancellationToken, Fact, GameSessionHandle, IAsyncEnumerable, ImmutableArray, List, RecordingConnection (+7 more)

### Community 163 - "Client interface 163"
Cohesion: 0.14
Nodes (11): JsonSerializerOptions, AppearanceSerializer, CharacterVisualState, PortraitGenerator, PortraitRandom, Fact, Fact, Lazy (+3 more)

### Community 164 - "Client verification 164"
Cohesion: 0.23
Nodes (8): Fact, Guid, InlineData, SnapshotBuffer, Task, Theory, NpcObservedTrajectoryTests, RejectingPredictor

### Community 165 - "Client verification 165"
Cohesion: 0.15
Nodes (15): CombatInputSettings, Fact, Func, GameSessionHandle, HashSet, InlineData, Key, SKBitmap (+7 more)

### Community 166 - "EconomyBalance 166"
Cohesion: 0.16
Nodes (15): IEnumerable, ImmutableArray, BalanceViolation, MarketHealthEvaluator, Func, ImmutableArray, JsonSerializerOptions, TextWriter (+7 more)

### Community 167 - "Gameplay requirements 167"
Cohesion: 0.06
Nodes (30): Экран станции, Сверка реализации — 2026-10-10, Стыковка, Экипаж корабля, Открытые вопросы, За рамками первого релиза, Уже существующая основа в коде, Решения первого релиза (+22 more)

### Community 168 - "EconomyBalance 168"
Cohesion: 0.14
Nodes (20): Exception, Fact, ImmutableArray, InlineData, Lazy, Theory, LongVoyageRunnerTests, BalanceConfigurationException (+12 more)

### Community 169 - "Contracts.Tests 169"
Cohesion: 0.17
Nodes (15): ImmutableArray, StationInventoryItemSnapshot, StationMarketEventSnapshot, StationMarketRouteEffectSnapshot, StationMarketStockState, Normal, Shortage, Surplus (+7 more)

### Community 170 - "Engine verification 170"
Cohesion: 0.21
Nodes (8): ImmutableArray, TradeQuoteSnapshot, ArgumentNullException, Fact, InlineData, SimulationEngine, Theory, TradeQuoteIssuanceTests

### Community 171 - "Engine simulation 171"
Cohesion: 0.14
Nodes (12): TradeCategory, StationEventData, StationEventPriceFactorData, StationMarketEventItemEffectData, StationMarketEventRouteEffectData, StationEventPriceFactorRuntime, StationEventRuntime, ImmutableArray (+4 more)

### Community 172 - "Client interface 172"
Cohesion: 0.13
Nodes (18): Angle, Border, Fill, Radius, Seed, CameraState, Dictionary, IEnumerable (+10 more)

### Community 173 - "CombatSim 173"
Cohesion: 0.14
Nodes (20): Any, BattleResult, _chance(), _clamp(), _defense(), _hit_module(), _make_ship(), Random (+12 more)

### Community 174 - "Client interface 174"
Cohesion: 0.14
Nodes (17): ApproachRouteGeometryCache, ApproachRouteSample, ApproachRouteSignature, ApproachRoute, CameraState, List, X, Y (+9 more)

### Community 175 - "Snapshot contracts 175"
Cohesion: 0.12
Nodes (21): DeepSpaceSaga.Client.UI.Screens.Station, ImmutableArray, StationRoutePresentation, StationRouteRow, ImmutableArray, TradingRouteAvailability, Available, Restricted (+13 more)

### Community 176 - "Contracts.Tests 176"
Cohesion: 0.16
Nodes (11): NotSupportedException, TradePriceStep, CancellationToken, Fact, IAsyncEnumerable, IEnumerable, JsonElement, Task (+3 more)

### Community 177 - "Client interface 177"
Cohesion: 0.18
Nodes (12): ImmutableArray, SKBitmap, SKCanvas, SKRect, SKRectI, PersonSex, Female, Male (+4 more)

### Community 178 - "Client interface 178"
Cohesion: 0.17
Nodes (15): IEnumerable, ImmutableArray, IReadOnlyList, List, SKCanvas, SKRect, CombatJournalPanel, Bounds (+7 more)

### Community 179 - "Engine verification 179"
Cohesion: 0.15
Nodes (13): Fact, InlineData, Theory, CombatRulesContentTests, ClientRoot, ModesPath, RulesPath, Fact (+5 more)

### Community 180 - "Client verification 180"
Cohesion: 0.15
Nodes (16): ItemStorageKind, Cargo, FuelTank, Good, Resource, TradeUnit, EnergyCell, Kilogram (+8 more)

### Community 181 - "Client verification 181"
Cohesion: 0.23
Nodes (6): Fact, HashSet, InlineData, Key, Theory, KeyboardEdgeTrackerTests

### Community 182 - "Client verification 182"
Cohesion: 0.14
Nodes (12): Func, GameSessionHandle, MouseButton, SKBitmap, SKCanvas, Task, ValueTask, X (+4 more)

### Community 183 - "Combat report planning 183"
Cohesion: 0.07
Nodes (26): Воспроизводимая приёмка боя и интерфейса, Входное техническое задание, Acceptance criteria, Acceptance self-review - 2026-10-16, Dependencies, Final self-review —2026-10-16, Gaps and backlog, Grounding и invariants (+18 more)

### Community 184 - "Combat report planning 184"
Cohesion: 0.07
Nodes (27): Входное техническое задание, Документация и честный статус поставки, Acceptance criteria, Dependencies, Execution result —2026-10-16, Execution scope amendment — 2026-10-16, Gaps and backlog, Grounding и invariants (+19 more)

### Community 185 - "Engine simulation 185"
Cohesion: 0.15
Nodes (20): DialogueFile, QuestFile, HashSet, IEnumerable, ImmutableArray, JsonSerializerOptions, DialogueContentLoader, DialogueFile (+12 more)

### Community 186 - "Engine simulation 186"
Cohesion: 0.14
Nodes (9): MarketRevision, QuoteContext, QuoteTerms, ImmutableArray, IssuedQuote, QuoteContext, QuoteTerms, StationObjectId (+1 more)

### Community 187 - "Client interface 187"
Cohesion: 0.16
Nodes (16): Func, HashSet, ImmutableArray, IReadOnlyList, List, PointCombatVisualSettings, Started, CombatEffectStore (+8 more)

### Community 188 - "Client interface 188"
Cohesion: 0.19
Nodes (17): H, IReadOnlyList, SKRect, W, X, Y, ScenarioSelectHit, ScenarioSelectLayout (+9 more)

### Community 189 - "Engine verification 189"
Cohesion: 0.18
Nodes (13): ShipEngineCommandTypes, TurnDirection, Left, Right, Fact, InlineData, Theory, PointCombatUS0017TK0004Tests (+5 more)

### Community 190 - "Engine verification 190"
Cohesion: 0.19
Nodes (10): ModuleDamageMath, ModuleDamageResult, Fact, InlineData, SimulationEngine, Theory, BasicCombatEndToEndTests, CombatRun (+2 more)

### Community 191 - "Engine verification 191"
Cohesion: 0.19
Nodes (8): IEnumerable, InlineData, Theory, PointCombatUS0002TK0004Tests, Fact, CommandDefinitionLoaderDirectoryTests, Fact, CommandDefinitionLoaderTests

### Community 192 - "Engine verification 192"
Cohesion: 0.33
Nodes (6): Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageFuelLifecycleTests

### Community 193 - "Client verification 193"
Cohesion: 0.09
Nodes (20): Fact, InlineData, Task, Theory, BasicCombatUiFlowTests, InlineData, Task, Theory (+12 more)

### Community 194 - "Point combat contract 194"
Cohesion: 0.07
Nodes (27): 1. Цель, 2.1 Переопределяется, 2.2 Переиспользуется без изменения смысла, 2.3 Статус EP-0009 после EP-0012, 2. Что EP-0012 делает с текущим боем, 3.1 Механика, 3.2 Интерфейс, 3. Решения (+19 more)

### Community 195 - "Point combat contract 195"
Cohesion: 0.07
Nodes (25): Исполнение — 2026-10-20, Истории, Current execution status — 2026-10-21, Границы и статус, EP-0012 — Ребилд тактического боя, Входное техническое задание, Итог технического исполнения — 2026-10-21, Automated story review (+17 more)

### Community 196 - "Release requirements 196"
Cohesion: 0.07
Nodes (25): Сверка реализации — 2026-10-10, Боевые доклады экипажа — 2026-10-16, Назначение, Связанные механики, Функциональность первого релиза, Current EP-0012 contract — 2026-10-21, Уже существующая функциональность, Актуализация EP-0004 — 2026-10-08 (+17 more)

### Community 197 - "Client interface 197"
Cohesion: 0.14
Nodes (16): LabelWidth, IReadOnlyList, SKCanvas, SKRect, CombatSummaryPanel, Bounds, CanContinue, ContinueRect (+8 more)

### Community 198 - "Snapshot contracts 198"
Cohesion: 0.12
Nodes (27): PointAimState, CombatCommandTypes, CountermeasureTerminationKind, Intercept, LifetimeExpired, MissedCoastExpired, OwnerLost, TargetLost (+19 more)

### Community 199 - "Client verification 199"
Cohesion: 0.20
Nodes (7): CameraState, List, TrajectoryViewportGeometry, Fact, InlineData, Theory, TrajectoryViewportTests

### Community 200 - "Contracts.Tests 200"
Cohesion: 0.13
Nodes (15): ImmutableArray, AiBaseMapData, AiMapEnvironmentSnapshot, EnvironmentFieldData, PointOfInterestData, TerritoryMapData, OrbitalElements, Fact (+7 more)

### Community 201 - "Engine simulation 201"
Cohesion: 0.18
Nodes (15): OrbitMapData, ImmutableArray, ClusterResourceBinding, ClusterStationData, ClusterTradeLink, StationClusterData, StationClusterMapSnapshot, ClusterGenerationConfig (+7 more)

### Community 202 - "Client verification 202"
Cohesion: 0.19
Nodes (9): Fact, H, VisibleRow, W, X, Y, ScenarioSelectLayoutTests, PanelLeft (+1 more)

### Community 203 - "Engine verification 203"
Cohesion: 0.22
Nodes (9): Fact, IEnumerable, InlineData, JsonObject, MemberData, Theory, StationMarketProfileContentTests, ProfilePath (+1 more)

### Community 204 - "Historical module combat 204"
Cohesion: 0.09
Nodes (20): Правила боя как данные, Evidence, Tickets, Canonical context and execution, EP-0009-US-0001-TK-0001-module-health-contracts, Execution evidence — 2026-10-12, Grounded scope before implementation — 2026-10-12, Canonical context and execution (+12 more)

### Community 205 - "Contracts.Tests 205"
Cohesion: 0.11
Nodes (15): DeepSpaceSaga.Contracts.Tests, Fact, InstalledModuleSnapshot, ObjectMotionSnapshot, CombatForecastContractTests, AuthoritativeSnapshot, CombatJournalEntry, Fact (+7 more)

### Community 206 - "Client interface 206"
Cohesion: 0.14
Nodes (19): Damage, Dictionary, Guid, ImmutableArray, IReadOnlyList, SKCanvas, SKRect, Started (+11 more)

### Community 207 - "Release requirements 207"
Cohesion: 0.11
Nodes (26): 10. Проверки и границы приёмки, 11. Технические допущения декомпозиции, 12. Трассировка, 1. Назначение, 2. Размещение и компоновка, 3. Карточки и взаимодействие, 4. События первой версии, 5. Реплики и докладчики (+18 more)

### Community 208 - "Client interface 208"
Cohesion: 0.14
Nodes (20): Hit, Action, IReadOnlyList, List, PointCombatVisualSettings, SKCanvas, SKRect, Action (+12 more)

### Community 209 - "Client verification 209"
Cohesion: 0.25
Nodes (8): predictor, buffer, Fact, InlineData, screen, SnapshotBuffer, Theory, InfoPanelTests

### Community 210 - "PortraitAssets 210"
Cohesion: 0.17
Nodes (9): Dictionary, IReadOnlyList, PortraitAssetRepository, Parts, Root, Style, UnifiedPortraitAssets, SKBitmap (+1 more)

### Community 211 - "Client interface 211"
Cohesion: 0.14
Nodes (20): SKCanvas, SKColor, SKFontStyleWeight, SKPaint, SKTypeface, Stopwatch, MenuStyle, BackgroundFill (+12 more)

### Community 212 - "Client verification 212"
Cohesion: 0.18
Nodes (9): SKColor, SpaceMapColorResolver, TacticalMapPalette, Fact, PirateScenarioTests, Fact, InlineData, Theory (+1 more)

### Community 213 - "Contracts.Tests 213"
Cohesion: 0.17
Nodes (9): CargoStackSnapshot, DefenseLaunchPreview, ImmutableArray, InstalledModuleSnapshot, ModuleCommandSnapshot, Fact, DefenseLaunchPreviewTests, Fact (+1 more)

### Community 214 - "Engine verification 214"
Cohesion: 0.21
Nodes (7): ScenarioFile, MigratedTradingEconomyContinuation, Fact, Func, OverflowException, SimulationEngine, MarketRevisionTests

### Community 215 - "Motion 215"
Cohesion: 0.14
Nodes (22): CancellationToken, Direction, ImmutableArray, X, Y, CombatForecast, CombatForecastLeg, CombatForecastOptions (+14 more)

### Community 216 - "Client verification 216"
Cohesion: 0.16
Nodes (14): Action, Fact, Func, GameSessionHandle, InlineData, MouseButton, SKBitmap, SKCanvas (+6 more)

### Community 217 - "Client verification 217"
Cohesion: 0.28
Nodes (5): Fact, InlineData, Key, Theory, SpeedPanelTests

### Community 218 - "Engine verification 218"
Cohesion: 0.18
Nodes (12): Fact, Func, IEnumerable, InlineData, SimulationEngine, Theory, PointCombatUS0004TK0001Tests, Fact (+4 more)

### Community 219 - "Combat report planning 219"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Авторитетный контракт боевого доклада, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Non-goals, Planned ticket map (+16 more)

### Community 220 - "Combat report planning 220"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Первый доклад о входе противника в дальность, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 221 - "Combat report planning 221"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Доклады экипажа о фактических пусках, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 222 - "Combat report planning 222"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Подтверждённые исходы и повреждения, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 223 - "Combat report planning 223"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Сохранение отметок и чистая загрузка, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 224 - "Combat report planning 224"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals, Planned ticket map (+16 more)

### Community 225 - "Combat report planning 225"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, История карточек и локализованные доклады, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 226 - "Combat report planning 226"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Dependencies, Графические доклады в стиле DSS, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 227 - "Combat report planning 227"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Acceptance criteria, Карточки аккордеона и чтение докладов, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 228 - "Combat report planning 228"
Cohesion: 0.08
Nodes (24): Входное техническое задание, Правая колонка и жизненный цикл экрана, Acceptance criteria, Dependencies, Gaps and backlog, Grounding и invariants, Implementation self-review - 2026-10-16, Non-goals (+16 more)

### Community 229 - "Client verification 229"
Cohesion: 0.28
Nodes (9): ImmutableArray, CombatPresentationFrame, CombatPresentationUpdate, Fact, Guid, InlineData, InvalidOperationException, Theory (+1 more)

### Community 230 - "Snapshot contracts 230"
Cohesion: 0.14
Nodes (20): LaunchMode, Auto, Manual, ImmutableArray, CountermeasurePhase, Guiding, MissedCoast, CountermeasureSnapshot (+12 more)

### Community 231 - "Contracts.Tests 231"
Cohesion: 0.14
Nodes (13): ImmutableArray, VoyagePhases, VoyageRouteOptionSnapshot, VoyageSnapshot, State, Fact, InlineData, Theory (+5 more)

### Community 232 - "Engine simulation 232"
Cohesion: 0.16
Nodes (17): ImmutableArray, IReadOnlyDictionary, CombatBlastRules, CombatChanceRules, CombatContentValidation, CombatDamageRules, CombatNpcRules, CombatRulesDefinition (+9 more)

### Community 233 - "Engine verification 233"
Cohesion: 0.33
Nodes (5): Fact, InlineData, JsonObject, Theory, ItemCatalogSchemaTests

### Community 234 - "Engine verification 234"
Cohesion: 0.20
Nodes (10): IReadOnlyList, PriceClampKind, Ceiling, Floor, None, StationPricing, Fact, InlineData (+2 more)

### Community 235 - "Engine verification 235"
Cohesion: 0.20
Nodes (6): Fact, MasterSeedTests, Fact, SimulationEngine, Task, SaveLoadContinuityTests

### Community 236 - "Performance 236"
Cohesion: 0.16
Nodes (9): CombatReportsNativeEvidence, BindingFlags, CancellationToken, Channel, IAsyncEnumerable, List, ValueTask, Factory (+1 more)

### Community 237 - "00 Process 237"
Cohesion: 0.08
Nodes (22): Сверка реализации — 2026-10-10, Architecture, Build, Test, Run, Client ↔ Engine data flow, Client navigation, Composition root (`Program.cs`), Content-driven simulation data, Current EP-0012 contract — 2026-10-21 (+14 more)

### Community 238 - "Snapshot contracts 238"
Cohesion: 0.15
Nodes (24): CombatEventType, BurstShotSkipped, CountermissileDetonated, Destroyed, Expired, Hit, Intercept, Launch (+16 more)

### Community 239 - "EconomyBalance.Tests 239"
Cohesion: 0.22
Nodes (10): Fact, ImmutableArray, InlineData, Theory, BalanceRunTests, Root, Scenario, Settings (+2 more)

### Community 240 - "Engine verification 240"
Cohesion: 0.40
Nodes (5): Fact, InlineData, SimulationEngine, Theory, RouteRiskIntegrationTests

### Community 241 - "Engine verification 241"
Cohesion: 0.28
Nodes (6): ConcurrentQueue, Fact, CounterTraceListener, Messages, TradingMapSchemaTests, TraceListener

### Community 242 - "Client verification 242"
Cohesion: 0.25
Nodes (9): ActiveSave, Route, Dictionary, End, Fact, IEnumerable, ImmutableArray, Task (+1 more)

### Community 243 - "Client interface 243"
Cohesion: 0.17
Nodes (12): DeepSpaceSaga.Client.UI.Screens.Defeat, ScreenEvent, SKRect, DefeatLayout, IReadOnlyList, MouseButton, ScreenEvent, SKCanvas (+4 more)

### Community 244 - "Release requirements 244"
Cohesion: 0.08
Nodes (22): Point Aim Combat, Назначение, Архитектурные требования, Оформление и локализация, Функциональность первого релиза, Current EP-0012 contract — 2026-10-21, GameMenu, Примечания (+14 more)

### Community 245 - "03 Design 245"
Cohesion: 0.09
Nodes (18): Горизонтальные ориентиры, На что опирается конструкция, Пропорции женского лица: ориентиры и новый портрет в анфас, Изготовление совместимых компонентов, Проверка анфаса, Вертикальные ориентиры, Овал — объёмная анатомия, не эллипс, Что измерено на новом мастере (+10 more)

### Community 246 - "Client interface 246"
Cohesion: 0.16
Nodes (21): IEnumerable, ImmutableArray, Key, MouseButton, CombatInputSettings, Cancel, CancelMouse, CountermissileAim (+13 more)

### Community 247 - "Client interface 247"
Cohesion: 0.17
Nodes (11): Dictionary, Func, IDisposable, Lazy, LocaleScope, Localization, Fact, InlineData (+3 more)

### Community 248 - "Client interface 248"
Cohesion: 0.22
Nodes (12): Dictionary, Func, IEnumerable, LinkedList, SKBitmap, SKColor, CombatReportAssets, IllustrationCount (+4 more)

### Community 249 - "Engine verification 249"
Cohesion: 0.24
Nodes (9): State, Fact, IEnumerable, InlineData, InvalidOperationException, SimulationEngine, Task, Theory (+1 more)

### Community 250 - "Motion.Tests 250"
Cohesion: 0.28
Nodes (7): ArgumentException, ArgumentOutOfRangeException, CombatForecastStatus, Fact, InlineData, Theory, PointCombatUS0018TK0004Tests

### Community 251 - "EconomyBalance.Tests 251"
Cohesion: 0.21
Nodes (8): Action, Fact, Func, ImmutableArray, JsonObject, BalanceReportTests, Output, Packaged

### Community 252 - "Engine verification 252"
Cohesion: 0.24
Nodes (6): Fact, IEnumerable, ItemTypeId, Quantity, SimulationEngine, TradeSnapshotProjectionTests

### Community 253 - "Engine verification 253"
Cohesion: 0.26
Nodes (8): NpcPointAim, Fact, IEnumerable, ImmutableArray, InlineData, SimulationEngine, Theory, PointCombatUS0007TK0002Tests

### Community 254 - "Client verification 254"
Cohesion: 0.20
Nodes (11): Selected, CancellationToken, Fact, IAsyncEnumerable, List, Task, ValueTask, PoiMapSelectionTests (+3 more)

### Community 255 - "Client interface 255"
Cohesion: 0.19
Nodes (11): SKBitmap, SKCanvas, SKPaint, SKPoint, SKRect, GenericWindowTypeA, HasAssets, Fact (+3 more)

### Community 256 - "Client interface 256"
Cohesion: 0.23
Nodes (9): SKRect, SettingsButton, Exit, LanguageCombo, MonitorCombo, None, UiScaleCombo, SettingsLayout (+1 more)

### Community 257 - "Client interface 257"
Cohesion: 0.30
Nodes (12): Bottom, Left, Right, Top, StationButton, Contracts, Finance, Hire (+4 more)

### Community 258 - "Snapshot contracts 258"
Cohesion: 0.21
Nodes (6): CancellationToken, IAsyncEnumerable, ValueTask, IGameSessionConnection, EvidenceFactory, SolarNativeEvidence

### Community 259 - "Client verification 259"
Cohesion: 0.15
Nodes (15): TradeQuoteRequest, CancellationToken, Func, IAsyncEnumerable, List, TaskCompletionSource, ValueTask, RecordingConnection (+7 more)

### Community 260 - "Motion 260"
Cohesion: 0.23
Nodes (8): X, Y, BlastMath, ArgumentOutOfRangeException, Fact, InlineData, Theory, PointCombatUS0003TK0001Tests

### Community 261 - "Client verification 261"
Cohesion: 0.17
Nodes (12): Dictionary, Fact, Func, GameSessionHandle, InlineData, SKBitmap, SKCanvas, Task (+4 more)

### Community 262 - "Client verification 262"
Cohesion: 0.20
Nodes (10): Fact, InlineData, List, SKBitmap, SKCanvas, SnapshotBuffer, Theory, PointCombatUS0013TK0005Tests (+2 more)

### Community 263 - "EconomyBalance.Tests 263"
Cohesion: 0.36
Nodes (5): Fact, ImmutableArray, InlineData, Theory, StrategyBalanceEvaluatorTests

### Community 264 - "Engine verification 264"
Cohesion: 0.27
Nodes (8): Command, Fact, InlineData, Result, SimulationEngine, Theory, CargoCostAccountingTests, Func

### Community 265 - "Historical module combat 265"
Cohesion: 0.10
Nodes (16): Каталог модулей и кораблей, Evidence, Story review — 2026-10-12, Tickets, Canonical context and execution, EP-0009-US-0002-TK-0001-weapon-catalog, Canonical context and execution, EP-0009-US-0002-TK-0002-engine-catalog (+8 more)

### Community 266 - "Combat report planning 266"
Cohesion: 0.10
Nodes (17): Планируемые документы и ресурсы, EP-0010 — текущая документация и grounding, EP-0010 — документация и grounding, Grounding, Исторический grounding планирования, Исходный документ — история, superseded current summary выше, EP-0010 — инвентаризация влияния документации, Истории (+9 more)

### Community 267 - "Client interface 267"
Cohesion: 0.17
Nodes (17): Hover, Size, Dictionary, SKBitmap, SKCanvas, SKColor, SKPaint, SKRect (+9 more)

### Community 268 - "Client interface 268"
Cohesion: 0.18
Nodes (12): Dictionary, ImmutableArray, Queue, SortedDictionary, CombatReportInbox, PendingCount, RecentCount, ImmutableArray (+4 more)

### Community 269 - "Client interface 269"
Cohesion: 0.18
Nodes (18): TacticalMapSettings, ClusterCellPixels, ClusterPpu, CompactMarkerPpu, FitPaddingPixels, GridBaseCellPixels, GridFadePixels, GridMinimumPixels (+10 more)

### Community 270 - "Client verification 270"
Cohesion: 0.22
Nodes (10): StationTravelResult, CancellationToken, Fact, IAsyncEnumerable, Task, TaskCompletionSource, ValueTask, DelayedTravelConnection (+2 more)

### Community 271 - "Engine verification 271"
Cohesion: 0.28
Nodes (7): ItemCatalogValidation, Fact, InlineData, JsonObject, Theory, PointCombatUS0001TK0005Tests, ModulesPath

### Community 272 - "Engine verification 272"
Cohesion: 0.19
Nodes (8): State, Fact, InlineData, SimulationEngine, Theory, PointCombatUS0004TK0003Tests, Fact, SetFireModeTests

### Community 273 - "EconomyBalance.Tests 273"
Cohesion: 0.37
Nodes (4): Fact, Func, ImmutableArray, MarketHealthEvaluatorTests

### Community 274 - "Engine verification 274"
Cohesion: 0.17
Nodes (10): Fact, IEnumerable, MemberData, Theory, ClusterCorrectnessCorpusTests, Fact, InlineData, SimulationEngine (+2 more)

### Community 275 - "Engine verification 275"
Cohesion: 0.18
Nodes (9): Fact, InlineData, SimulationEngine, Theory, VoyageLifecycleTests, Fact, InlineData, Theory (+1 more)

### Community 276 - "Client interface 276"
Cohesion: 0.17
Nodes (9): GameTimeDisplay, GameCalendar, Fact, InlineData, Theory, GameTimeDisplayTests, InlineData, Theory (+1 more)

### Community 277 - "Client interface 277"
Cohesion: 0.25
Nodes (10): IEnumerable, IReadOnlyList, List, CombatReportStore, ExpandedSequence, KeyboardSelection, Reports, ScrollOffset (+2 more)

### Community 278 - "Client interface 278"
Cohesion: 0.18
Nodes (16): CameraState, ImmutableArray, IReadOnlyList, List, SKPath, CombatTrajectoryProjector, Geometry, BlastRadiusKm (+8 more)

### Community 279 - "Client verification 279"
Cohesion: 0.25
Nodes (6): Func, GameSessionHandle, Task, TaskCompletionSource, StationTradeNavigation, Task

### Community 280 - "Client verification 280"
Cohesion: 0.18
Nodes (9): ValueTask, Fixture, Connection, DirectoryPath, Engine, Game, Handle, Screens (+1 more)

### Community 281 - "Client verification 281"
Cohesion: 0.36
Nodes (8): Fact, H, SKBitmap, SKColor, W, X, Y, ScenarioSelectRowVisualsTests

### Community 282 - "Client verification 282"
Cohesion: 0.30
Nodes (5): Fact, JsonNode, Random, ContentFixture, StationResourceFieldContentTests

### Community 283 - "Engine verification 283"
Cohesion: 0.27
Nodes (4): Fact, InlineData, Theory, ItemCatalogTests

### Community 284 - "Engine verification 284"
Cohesion: 0.34
Nodes (6): Fact, IEnumerable, InlineData, SimulationEngine, Theory, PointCombatUS0008TK0001Tests

### Community 285 - "Engine verification 285"
Cohesion: 0.24
Nodes (8): Fact, IEnumerable, InlineData, SimulationClock, SimulationEngine, Task, Theory, PointCombatUS0008TK0002Tests

### Community 286 - "Engine verification 286"
Cohesion: 0.25
Nodes (7): ArgumentException, Fact, InlineData, OverflowException, SimulationEngine, Theory, VoyageLedgerLifecycleTests

### Community 287 - "Historical module combat 287"
Cohesion: 0.10
Nodes (16): Интерфейс модулей и журнал, Evidence, Tickets, Actual scope before editing, Canonical context and execution, EP-0009-US-0010-TK-0001-defeat-from-module-rule, Evidence and self-review, Actual scope before editing (+8 more)

### Community 288 - "Client verification 288"
Cohesion: 0.21
Nodes (8): IReadOnlyList, SaveSlots, DateTime, SaveSlotInfo, Fact, SaveSlotsTests, Fact, SaveSlotInfoTests

### Community 289 - "Client verification 289"
Cohesion: 0.22
Nodes (9): SKCanvas, SKColor, SKPaint, SKRect, ProgressBar, Fact, InlineData, Theory (+1 more)

### Community 290 - "Client verification 290"
Cohesion: 0.34
Nodes (7): Fact, InlineData, ITestOutputHelper, Task, Theory, Trait, PointCombatUS0019TK0003Tests

### Community 291 - "Motion 291"
Cohesion: 0.25
Nodes (9): Func, X, Y, CollisionPath, TorpedoCollisionMath, TorpedoContact, CollisionPath, Fact (+1 more)

### Community 292 - "Client verification 292"
Cohesion: 0.23
Nodes (10): Fact, Task, ValueTask, CombatSessionFixture, Connection, DirectoryPath, Engine, Restored (+2 more)

### Community 293 - "Client verification 293"
Cohesion: 0.22
Nodes (10): Action, Fact, GameSessionHandle, InlineData, InvalidOperationException, Key, ObjectDisposedException, Task (+2 more)

### Community 294 - "Engine verification 294"
Cohesion: 0.22
Nodes (9): Fact, InlineData, Theory, EpicSolarIntegrationTests, Fact, InlineData, SimulationEngine, Theory (+1 more)

### Community 295 - "Engine verification 295"
Cohesion: 0.23
Nodes (9): Fact, IEnumerable, InvalidOperationException, MemberData, SimulationEngine, Theory, X, Y (+1 more)

### Community 296 - "Engine verification 296"
Cohesion: 0.39
Nodes (6): Fact, InlineData, SimulationEngine, Theory, PointCombatUS0006TK0002Tests, Threat

### Community 297 - "Historical module combat 297"
Cohesion: 0.11
Nodes (15): Боевые сценарии, Evidence, Tickets, Actual scope before editing, Canonical context and execution, EP-0009-US-0009-TK-0001-scenario-combat-mukha, Evidence and self-review, Actual scope before editing (+7 more)

### Community 298 - "Engine verification 298"
Cohesion: 0.17
Nodes (9): DeepSpaceSaga.Engine.Tests, Fact, ModuleHealthProjectionTests, Fact, InlineData, Theory, PointCombatUS0003TK0005Tests, Fact (+1 more)

### Community 299 - "Engine simulation 299"
Cohesion: 0.20
Nodes (12): Pricing, ImmutableArray, IReadOnlyList, Pricing, SpreadReason, TradeQuoteCalculator, TradeQuoteDirection, Buy (+4 more)

### Community 300 - "Contracts.Tests 300"
Cohesion: 0.22
Nodes (7): Dictionary, ImmutableArray, Queue, SessionEventHistory, ShipEvent, Fact, ShipEventTests

### Community 301 - "Client verification 301"
Cohesion: 0.27
Nodes (7): Func, CombatReportFormatter, CombatReportPresentation, Fact, InlineData, Theory, CombatReportFormatterTests

### Community 302 - "Contracts.Tests 302"
Cohesion: 0.29
Nodes (9): ImmutableArray, TorpedoRoute, TorpedoRouteSegment, TorpedoSnapshot, TrailSegment, Fact, InlineData, Theory (+1 more)

### Community 303 - "Motion.Tests 303"
Cohesion: 0.23
Nodes (8): BlastThreatMath, BlastThreatResult, ArgumentException, ArgumentOutOfRangeException, Fact, InlineData, Theory, PointCombatUS0006TK0001Tests

### Community 304 - "Client verification 304"
Cohesion: 0.25
Nodes (9): ArgumentException, Fact, InlineData, ITestOutputHelper, Task, Theory, Trait, CombatForecastCacheTests (+1 more)

### Community 305 - "Client verification 305"
Cohesion: 0.25
Nodes (10): CancellationToken, GameSessionHandle, IAsyncEnumerable, ValueTask, CombatReportsAcceptanceFixtures, Expected, Handle, Screen (+2 more)

### Community 306 - "Client verification 306"
Cohesion: 0.23
Nodes (9): Fact, InlineData, SKBitmap, SKCanvas, SnapshotBuffer, Task, Theory, PointCombatUS0013TK0003Tests (+1 more)

### Community 307 - "Engine verification 307"
Cohesion: 0.27
Nodes (7): Fact, Func, IEnumerable, InlineData, SimulationEngine, Theory, PointCombatUS0003TK0002Tests

### Community 308 - "Engine verification 308"
Cohesion: 0.37
Nodes (4): Fact, InlineData, Theory, VoyageProfitAccountingTests

### Community 309 - "Historical module combat 309"
Cohesion: 0.11
Nodes (13): Evidence, HP в модулях, Tickets, Canonical context and execution, EP-0009-US-0003-TK-0001-module-health-runtime, Implementation evidence — 2026-10-12, Canonical context and execution, EP-0009-US-0003-TK-0002-module-damage (+5 more)

### Community 310 - "Point combat contract 310"
Cohesion: 0.12
Nodes (14): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+6 more)

### Community 311 - "Engine simulation 311"
Cohesion: 0.31
Nodes (11): Disc, Point, ImmutableArray, IReadOnlyList, AiTradePlacementValidator, Disc, PlacementCheck, PlacementConnectivity (+3 more)

### Community 312 - "Client verification 312"
Cohesion: 0.31
Nodes (6): Placeholders, Fact, InlineData, Key, Theory, LocalizationTests

### Community 313 - "Client interface 313"
Cohesion: 0.22
Nodes (13): Scene, SourceScene, Guid, IReadOnlyDictionary, CombatForecastCache, AllocatedBytes, Hits, Misses (+5 more)

### Community 314 - "Client interface 314"
Cohesion: 0.24
Nodes (8): CameraState, Dictionary, IEnumerable, IReadOnlyDictionary, IReadOnlyList, SKCanvas, ClusterMapPresentation, ClusterStationPresentation

### Community 315 - "Snapshot contracts 315"
Cohesion: 0.18
Nodes (13): CombatEncounterCommandTypes, CombatEncounterSnapshot, CombatEncounterState, Active, Idle, Lost, Won, EnemyShipStatusSnapshot (+5 more)

### Community 316 - "Engine verification 316"
Cohesion: 0.33
Nodes (6): CombatEncounterStats, Fact, IEnumerable, SimulationClock, SimulationEngine, PointCombatUS0008TK0003Tests

### Community 317 - "Contracts.Tests 317"
Cohesion: 0.19
Nodes (10): CombatImpactSnapshot, AuthoritativeSnapshot, CombatImpactSnapshot, CombatJournalEntry, Fact, CombatEventTests, Fact, InlineData (+2 more)

### Community 318 - "Client verification 318"
Cohesion: 0.31
Nodes (7): Fact, HashSet, InlineData, SnapshotBuffer, Theory, GameSessionScaleFilterTests, Buffer

### Community 319 - "PortraitAssets 319"
Cohesion: 0.28
Nodes (5): SKBitmap, SKColor, SKPoint3, SKRectI, WholeFaceAssets

### Community 320 - "Historical module combat 320"
Cohesion: 0.12
Nodes (13): Сохранения и приёмка, Evidence, Tickets, Actual scope before editing, Canonical context and execution, EP-0009-US-0011-TK-0001-save-format-17, Evidence and self-review, Actual scope and result (+5 more)

### Community 321 - "Combat report planning 321"
Cohesion: 0.12
Nodes (16): Assumptions, Code context, Decisions, Definition of Done, Вручную выполнить evidence/NativeAcceptance.md и evidence/Performance.md. Только во время execution., Execution evidence and user constraint - 2026-10-16, Execution scope/order adjustment - 2026-10-16, Визуальная приёмка и frame-time evidence (+8 more)

### Community 322 - "EP 0011 short fixes 322"
Cohesion: 0.12
Nodes (16): Правила отображения, Палитра — траектории и тактические обозначения, Code context — продуктовый инвентарь; execution allowlists в дочерних пакетах, Decisions / Assumptions, Definition of Done, Палитра — фон, подписи и дополнительные слои, Execution authorization — 2026-10-18, Палитра — объекты (+8 more)

### Community 323 - "Contracts.Tests 323"
Cohesion: 0.23
Nodes (10): ImmutableArray, AsteroidSurveySnapshot, EngineManeuverSnapshot, ModuleHealthSnapshot, ResourceFractionSnapshot, ResourceSurveyReasonCodes, Fact, InlineData (+2 more)

### Community 324 - "Engine verification 324"
Cohesion: 0.25
Nodes (8): AttackAiData, InlineData, Theory, PointCombatUS0019TK0001Tests, Fact, InlineData, Theory, HostileAttackAiTests

### Community 325 - "Client verification 325"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SnapshotBuffer, Theory, GameSessionUiScaleTests

### Community 326 - "Engine verification 326"
Cohesion: 0.31
Nodes (5): Fact, DefeatTimeBoundaryTests, Fact, InvalidOperationException, PlayerDefeatTests

### Community 327 - "Engine verification 327"
Cohesion: 0.35
Nodes (6): Fact, IEnumerable, InlineData, SimulationEngine, Theory, PointCombatUS0003TK0003Tests

### Community 328 - "Engine verification 328"
Cohesion: 0.34
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PointCombatUS0017TK0002Tests

### Community 329 - "Engine verification 329"
Cohesion: 0.41
Nodes (4): Fact, InlineData, Theory, WeaponOperatorRuntimeTests

### Community 330 - "Engine verification 330"
Cohesion: 0.37
Nodes (5): Fact, InlineData, SimulationEngine, Theory, VoyageFuelAccountingTests

### Community 331 - "Engine verification 331"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, VoyageSaveLoadContinuityTests

### Community 332 - "EconomyBalance 332"
Cohesion: 0.40
Nodes (5): BalanceCaseEvidence, ImmutableArray, BalanceLeader, BalanceStrategyIdentity, StrategyBalanceEvaluator

### Community 333 - "Client interface 333"
Cohesion: 0.17
Nodes (13): Bitmap, LinkedListNode, Dictionary, Image, Key, LinkedList, Path, SKBitmap (+5 more)

### Community 334 - "Combat report planning 334"
Cohesion: 0.12
Nodes (15): Иллюстрации дальности и торпедных пусков, Artifact evidence - 2026-10-16, Assumptions, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 335 - "Combat report planning 335"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Иллюстрации ПРО, перехвата и попадания, Code context, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 336 - "Combat report planning 336"
Cohesion: 0.12
Nodes (15): Artifact evidence - 2026-10-16, Assumptions, Code context, Уничтожение противника и повреждение игрока, Decisions, Definition of Done, Implementation steps, Invariants (+7 more)

### Community 337 - "Point combat contract 337"
Cohesion: 0.15
Nodes (12): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0010-TK-0001-combat-visuals-v3, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+4 more)

### Community 338 - "Client verification 338"
Cohesion: 0.27
Nodes (7): DeepSpaceSaga.Client.UI.Portraits, SKBitmap, DialoguePortraitComposer, Fact, InlineData, Theory, DialoguePortraitComposerTests

### Community 339 - "Release requirements 339"
Cohesion: 0.12
Nodes (15): Сверка реализации — 2026-10-10, Экономический интервал, Размер станции, Термины, Формула котировки, События, Склад и бюджет, Acceptance criteria (+7 more)

### Community 340 - "Engineering documentation 340"
Cohesion: 0.12
Nodes (15): 10. [P2] Очередь полных снимков не ограничена, 1. [P1] Продолжения асинхронных переходов меняют UI из фонового потока, 2. [P1] Охрана станции не учитывает действующий маршрут Approach, 3. [P1] Ошибка симуляции превращается в нормальное завершение потока снимков, 4. [P2] Save/Load меняет курс после отмены Approach, 5. [P2] Повторная доставка торговой команды повторно меняет мир, 6. [P2] Торговые операции допускают переполнение денег и массы, 7. [P2] Результаты команд теряются, если UI пропускает снимок (+7 more)

### Community 341 - "Engine verification 341"
Cohesion: 0.23
Nodes (8): IReadOnlyCollection, IReadOnlyList, ShipDestructionMath, ShipModuleLife, Fact, InlineData, Theory, ShipDestructionRulesTests

### Community 342 - "PortraitAssets 342"
Cohesion: 0.33
Nodes (4): Landmarks, SKBitmap, Landmarks, ReferenceFeatureAssets

### Community 343 - "Client verification 343"
Cohesion: 0.23
Nodes (9): Dictionary, SKBitmap, UiAssetLoader, Fact, MemberData, Theory, TheoryData, UiAssetLoaderTests (+1 more)

### Community 344 - "Engine simulation 344"
Cohesion: 0.21
Nodes (9): X, Y, LaunchScatterRng, Counter, State, Fact, InlineData, Theory (+1 more)

### Community 346 - "Client verification 346"
Cohesion: 0.33
Nodes (6): Fact, InlineData, Theory, TemporaryContent, Path, TorpedoLauncherContentTests

### Community 347 - "Contracts.Tests 347"
Cohesion: 0.18
Nodes (11): BattleReportKind, BattleReportReporterRole, BattleReportSnapshot, BattleReportVariant, CombatReportBatchSnapshot, Fact, IEnumerable, InlineData (+3 more)

### Community 348 - "Engine verification 348"
Cohesion: 0.32
Nodes (5): Fact, MapEnvironmentSaveTests, Fact, SimulationEngine, SeededAbandonedObjectsTests

### Community 349 - "Engine verification 349"
Cohesion: 0.38
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PhaseThreeSaveContinuationTests

### Community 350 - "Engine verification 350"
Cohesion: 0.42
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PointCombatUS0007TK0003Tests

### Community 351 - "Engine verification 351"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, TradingEconomyContinuityTests

### Community 352 - "Engine verification 352"
Cohesion: 0.37
Nodes (4): Fact, InlineData, Theory, WeaponOperatorSchemaTests

### Community 353 - "Historical module combat 353"
Cohesion: 0.13
Nodes (11): Мухи и Шмели на ИИ EP-0008, Evidence, Tickets, Canonical context and execution, EP-0009-US-0008-TK-0001-npc-fire-cadence, Canonical context and execution, EP-0009-US-0008-TK-0002-flank-approach-math, Actual scope before editing (+3 more)

### Community 354 - "Combat report planning 354"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, E01 по физическому состоянию мира, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 355 - "Combat report planning 355"
Cohesion: 0.13
Nodes (14): Assumptions, Capture/restore и bootstrap baseline, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+6 more)

### Community 356 - "Combat report planning 356"
Cohesion: 0.13
Nodes (14): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Общая геометрия Object Info и докладов (+6 more)

### Community 357 - "Combat report planning 357"
Cohesion: 0.13
Nodes (11): Correct the available map viewport after adding reports, Evidence, Scope and steps, Complete HUD reservation and synchronize affected test fixtures, Evidence, Exact scope, Clear unrelated HUD hover/press states when reports consume pointer input, Evidence (+3 more)

### Community 358 - "Client verification 358"
Cohesion: 0.18
Nodes (11): Chance, Effects, Max, Min, Priority, Profiles, Dictionary, Fact (+3 more)

### Community 359 - "EconomyBalance.Tests 359"
Cohesion: 0.27
Nodes (8): DeepSpaceSaga.EconomyBalance.Tests, DeepSpaceSaga.EconomyBalance, Fact, LongVoyageDiagnosticsTests, ImmutableArray, ClusterEconomyAssessment, ClusterEconomyEvaluator, ClusterEconomyFinding

### Community 360 - "00 Process 360"
Cohesion: 0.13
Nodes (14): `00-Process/`, `01-Requirements/`, `02-FirstRelease/`, `03-Design/`, `04-Engineering/`, `05-Backlog/`, `06-Tooling/`, Documentation System (+6 more)

### Community 361 - "Client interface 361"
Cohesion: 0.44
Nodes (8): pressedCount, releasedCount, Dictionary, Func, IKeyboard, Key, Span, KeyboardEdgeTracker

### Community 363 - "Client verification 363"
Cohesion: 0.33
Nodes (6): Fact, ImmutableArray, InlineData, Theory, DualDefensePanelLayoutTests, Modules

### Community 364 - "Client verification 364"
Cohesion: 0.28
Nodes (6): SKRect, GameSessionRightColumnLayout, Fact, InlineData, Theory, CombatReportRightColumnTests

### Community 365 - "Snapshot contracts 365"
Cohesion: 0.26
Nodes (10): SessionOutcomeSnapshot, CanSave, Running, SaveDeniedReason, SessionOutcomeState, Defeated, Running, Fact (+2 more)

### Community 366 - "Client verification 366"
Cohesion: 0.31
Nodes (6): CancellationToken, Fact, IAsyncEnumerable, ValueTask, ApproachCameraIntegrationTests, Connection

### Community 367 - "Client verification 367"
Cohesion: 0.36
Nodes (5): Fact, SKBitmap, SKColor, SKRect, NinePatchTests

### Community 368 - "Client verification 368"
Cohesion: 0.29
Nodes (7): Fact, InlineData, Theory, TemporaryContent, Root, SettingsPath, TetrarchClassContentTests

### Community 370 - "Engine verification 370"
Cohesion: 0.34
Nodes (5): Fact, InlineData, JsonNode, Theory, CountermeasureContentLoaderTests

### Community 371 - "Engine verification 371"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, HostileOrbitRuntimeTests

### Community 372 - "Engine verification 372"
Cohesion: 0.37
Nodes (3): Fact, SimulationEngine, InstalledModuleProjectionTests

### Community 373 - "Engine verification 373"
Cohesion: 0.41
Nodes (5): Fact, InlineData, SimulationEngine, Theory, MarketSaveLoadContinuityTests

### Community 374 - "Engine verification 374"
Cohesion: 0.29
Nodes (6): Fact, IEnumerable, InlineData, SimulationEngine, Theory, PointForecastRuntimeParityTests

### Community 375 - "Engine verification 375"
Cohesion: 0.39
Nodes (5): Fact, IEnumerable, InlineData, Theory, StationMarketEventCatalogTests

### Community 376 - "Combat report planning 376"
Cohesion: 0.14
Nodes (13): Неизменяемые данные доклада, Assumptions, Code context, Decisions, Definition of Done, Execution evidence — 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 377 - "Combat report planning 377"
Cohesion: 0.14
Nodes (13): Докладчики и безопасные имена участников, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 378 - "Combat report planning 378"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, E05 успешного перехвата, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 379 - "Combat report planning 379"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 380 - "Combat report planning 380"
Cohesion: 0.14
Nodes (13): Ограниченная история и раскрытая карточка, Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 381 - "Combat report planning 381"
Cohesion: 0.14
Nodes (13): Assumptions, Ввод и навигация по карточкам, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 382 - "Combat report planning 382"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Чистая смена сессии и приоритет Defeat, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants (+5 more)

### Community 383 - "Combat report planning 383"
Cohesion: 0.14
Nodes (13): Assumptions, Code context, Decisions, Definition of Done, Execution evidence - 2026-10-16, Implementation steps, Invariants, Out of scope (+5 more)

### Community 384 - "EP 0011 short fixes 384"
Cohesion: 0.15
Nodes (10): Истории, EP-0011 — Короткие фиксы, Palette execution — 2026-10-18, Scope и статус, Execution — 2026-10-18, US-0002 — Документация результата US-0001, Execution — 2026-10-18, Документация и Graphify (+2 more)

### Community 385 - "Client interface 385"
Cohesion: 0.22
Nodes (7): Exception, Func, HashSet, Task, ValueTask, SessionConnectionLoader, OperationCanceledException

### Community 386 - "Client verification 386"
Cohesion: 0.36
Nodes (6): Action, Fact, InlineData, InvalidDataException, Theory, CombatVisualSettingsTests

### Community 387 - "Client verification 387"
Cohesion: 0.40
Nodes (5): Fact, InlineData, Theory, PointCombatUS0011TK0003Tests, Player

### Community 388 - "Engine verification 388"
Cohesion: 0.27
Nodes (6): Fact, InlineData, InvalidOperationException, SimulationEngine, Theory, CombatReportsSaveLoadTests

### Community 389 - "Engine verification 389"
Cohesion: 0.31
Nodes (6): Fact, IEnumerable, InlineData, SimulationEngine, Theory, PointCombatUS0009TK0005Tests

### Community 390 - "Engine verification 390"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, TradingEconomySaveSchemaTests

### Community 391 - "Historical module combat 391"
Cohesion: 0.15
Nodes (9): Броски навыков, Evidence, Tickets, Canonical context and execution, EP-0009-US-0004-TK-0001-chance-math, Canonical context and execution, EP-0009-US-0004-TK-0002-torpedo-hit-roll, Canonical context and execution (+1 more)

### Community 392 - "Historical module combat 392"
Cohesion: 0.15
Nodes (9): Canonical context and execution, EP-0009-US-0005-TK-0001-launcher-cooldown, Canonical context and execution, EP-0009-US-0005-TK-0002-pd-burst, Canonical context and execution, EP-0009-US-0005-TK-0003-pd-reload-and-attempts, Темп стрельбы, Evidence (+1 more)

### Community 393 - "Historical module combat 393"
Cohesion: 0.15
Nodes (9): Canonical context and execution, EP-0009-US-0006-TK-0001-mode-runtime, Canonical context and execution, EP-0009-US-0006-TK-0002-set-mode-command, Canonical context and execution, EP-0009-US-0006-TK-0003-mode-selector-ui, Режимы торпедного аппарата, Evidence (+1 more)

### Community 394 - "EP 0011 short fixes 394"
Cohesion: 0.15
Nodes (12): История наблюдений и определение тенденции, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 395 - "EP 0011 short fixes 395"
Cohesion: 0.15
Nodes (12): Интеграция наблюдаемого прогноза в карту, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 396 - "EP 0011 short fixes 396"
Cohesion: 0.15
Nodes (12): Регрессии и жизненный цикл прогноза, Assumptions, Code context, Decisions, Definition of Done, Execution 2026-10-18, Implementation steps, Invariants (+4 more)

### Community 397 - "EP 0011 short fixes 397"
Cohesion: 0.15
Nodes (11): Критерии приёмки, Approved ticket map, Dependencies / gaps, Execution, Non-goals / Invariants, US-0003 — Читаемая палитра тактической карты, Acceptance criteria, Approved ticket map (+3 more)

### Community 398 - "Engine verification 398"
Cohesion: 0.26
Nodes (9): CombatResult, InlineData, ITestOutputHelper, Theory, X, Y, CombatResult, Finished (+1 more)

### Community 399 - "Release requirements 399"
Cohesion: 0.15
Nodes (12): Требования первого релиза, Approach: уточнение от 2026-09-20, Командные панели корабля, Current EP-0012 contract — 2026-10-21, Цель, Engine, Актуализация EP-0008 — 2026-10-12, Актуализация EP-0009 — 2026-10-12 (+4 more)

### Community 400 - "05 Backlog 400"
Cohesion: 0.15
Nodes (12): ТЗ-02A: masterSeed для New Game и save/load continuation, ТЗ-03: загрузка всех тематических JSON из Settings.json, ТЗ-04: CommandResult и ShipEvent в authoritative snapshot, ТЗ-05: authoritative command inbox, validation и Deferred attempts, ТЗ-06: полный набор Engine commands первой итерации, ТЗ-07: стартовый корабль из 3 платформ и 12 occupied cells, ТЗ-08: fuel state for Engine module, ТЗ-09: ActiveCycle duration, command factors and fixed-point normalization (+4 more)

### Community 401 - "Client interface 401"
Cohesion: 0.33
Nodes (7): SKPaintStyle, SKColor, SKFontStyleWeight, SKPaint, SKTextAlign, SKTypeface, XenonStyle

### Community 402 - "Client interface 402"
Cohesion: 0.24
Nodes (7): SKBitmap, SKCanvas, SKRect, ImagePanel, HasLoadedImage, Fact, ImagePanelTests

### Community 403 - "Client interface 403"
Cohesion: 0.28
Nodes (6): SKCanvas, SKRect, XenonWindowChrome, HasChromeAssets, Fact, XenonWindowChromeTests

### Community 405 - "Client verification 405"
Cohesion: 0.28
Nodes (8): Action, Fact, InlineData, InvalidDataException, JsonObject, Theory, PointCombatUS0010TK0001Tests, Source

### Community 406 - "Engine verification 406"
Cohesion: 0.35
Nodes (5): Fact, InlineData, SimulationEngine, Theory, LocalClusterGenerationTests

### Community 407 - "Historical module combat 407"
Cohesion: 0.17
Nodes (9): Документация (обязательная завершающая история), Evidence, Tickets, Canonical context and execution, EP-0009-US-0012-TK-0001-update-project-documents, Result, Canonical context and execution, EP-0009-US-0012-TK-0002-refresh-graph-and-links (+1 more)

### Community 408 - "EP 0011 short fixes 408"
Cohesion: 0.17
Nodes (11): Входное техническое задание, Acceptance criteria, Approved ticket map, Assumptions, Decision / review log, Dependencies, Gaps and backlog, Implementation checkpoint — 2026-10-18 (+3 more)

### Community 409 - "EP 0011 short fixes 409"
Cohesion: 0.17
Nodes (11): Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change, Палитра объектов и runtime-настройки (+3 more)

### Community 410 - "EP 0011 short fixes 410"
Cohesion: 0.17
Nodes (11): Навигация, рамки и следы, Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 411 - "EP 0011 short fixes 411"
Cohesion: 0.17
Nodes (11): Боевые линии и радиусы, Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 412 - "EP 0011 short fixes 412"
Cohesion: 0.17
Nodes (11): Code context, Decisions / Assumptions, Definition of Done, Evidence, Подписи, HP и сетка, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 413 - "EP 0011 short fixes 413"
Cohesion: 0.17
Nodes (11): Планеты, поля, AI и указатели, Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 414 - "Point combat contract 414"
Cohesion: 0.17
Nodes (11): Уточнение исполнения — 2026-10-20, Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0001-TK-0001-projectile-contracts, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 415 - "Point combat contract 415"
Cohesion: 0.17
Nodes (11): Уточнение исполнения — 2026-10-20, Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0001-TK-0004-combat-rules-schema, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 416 - "Point combat contract 416"
Cohesion: 0.17
Nodes (11): Atomic catalog staging decision, Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0002-TK-0001-torpedo-launchers-v3, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 417 - "Point combat contract 417"
Cohesion: 0.17
Nodes (11): Atomic catalog staging decision, Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0002-TK-0003-engines-v3, Implementation steps, Out of scope / Invariants, Public API after the change (+3 more)

### Community 418 - "Client verification 418"
Cohesion: 0.38
Nodes (4): DeepSpaceSaga.Client.UI.Screens.Settings, Fact, SKBitmap, SettingsScreenTests

### Community 419 - "Client verification 419"
Cohesion: 0.23
Nodes (9): DispatchProxy, MethodInfo, MouseProxy, Fact, IMouse, Task, Vector2, MouseProxy (+1 more)

### Community 420 - "Contracts.Tests 420"
Cohesion: 0.30
Nodes (8): Own versus observed privacy, ObservedProjectileSnapshot, DestroyShip, ProjectObservedProjectile, InlineData, JsonException, Theory, PointCombatUS0001TK0001Tests

### Community 421 - "05 Backlog 421"
Cohesion: 0.17
Nodes (11): ТЗ-01: CommandResult в authoritative snapshot, ТЗ-02: минимальный ShipEvent / watch-log DTO в snapshot, ТЗ-03: канонические Engine commands вместо `engine.cancel-all`, ТЗ-04: authoritative параметры target для match commands, ТЗ-05: загрузка `factory-types.json` и `recipes.json` из Settings, ТЗ-06: factor model для command definitions, ТЗ-07: fuel state для Engine module, ТЗ-08: стартовый корабль из 9 модулей и 12 occupied cells (+3 more)

### Community 422 - "Client interface 422"
Cohesion: 0.39
Nodes (6): SKBitmap, SKCanvas, SKColor, SKRect, GenericButtonTypeA, HasAssets

### Community 423 - "Engine verification 423"
Cohesion: 0.41
Nodes (4): Fact, InlineData, Theory, StationSizeFactorsTests

### Community 424 - "Client verification 424"
Cohesion: 0.53
Nodes (3): Fact, SnapshotBuffer, GameSessionDockAutoOpenTests

### Community 425 - "Client verification 425"
Cohesion: 0.26
Nodes (7): Fact, InlineData, SKBitmap, SKColor, SKRect, Theory, GenericButtonTypeATests

### Community 426 - "Engine verification 426"
Cohesion: 0.47
Nodes (4): Fact, SimulationEngine, SpaceObjectRuntime, CombatReportIdentityTests

### Community 427 - "Engine verification 427"
Cohesion: 0.47
Nodes (3): Fact, SimulationEngine, ObjectImageResolutionTests

### Community 428 - "Engine verification 428"
Cohesion: 0.36
Nodes (5): Fact, InlineData, SimulationEngine, Theory, PhaseThreeSaveSchemaTests

### Community 429 - "Engine verification 429"
Cohesion: 0.47
Nodes (4): InlineData, SimulationEngine, Theory, PointCombatUS0018TK0002Tests

### Community 430 - "PortraitAssets 430"
Cohesion: 0.38
Nodes (4): SKBitmap, SKPath, SKPoint, HairAssets

### Community 431 - "EP 0011 short fixes 431"
Cohesion: 0.18
Nodes (10): Палитра — интеграционные регрессии, Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change (+2 more)

### Community 432 - "EP 0011 short fixes 432"
Cohesion: 0.18
Nodes (10): Палитра — интеграционные регрессии, Code context, Decisions / Assumptions, Definition of Done, Evidence, Implementation steps, Out of scope / Invariants, Public API after the change (+2 more)

### Community 433 - "Point combat contract 433"
Cohesion: 0.18
Nodes (10): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0003-TK-0002-torpedo-point-launch, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+2 more)

### Community 434 - "Point combat contract 434"
Cohesion: 0.18
Nodes (10): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0009-TK-0003-save-format-18, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+2 more)

### Community 435 - "Point combat contract 435"
Cohesion: 0.18
Nodes (10): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+2 more)

### Community 436 - "Point combat contract 436"
Cohesion: 0.18
Nodes (10): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0015-TK-0001-update-documents-1, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+2 more)

### Community 437 - "Point combat contract 437"
Cohesion: 0.18
Nodes (10): Code context, Decisions / Assumptions, Definition of Done, EP-0012-US-0019-TK-0004-client-combat-regressions, Implementation steps, Out of scope / Invariants, Public API after the change, Status / Evidence (+2 more)

### Community 438 - "Client interface 438"
Cohesion: 0.29
Nodes (6): Callback, SendOrPostCallback, ConcurrentQueue, State, WindowThreadContext, SynchronizationContext

### Community 439 - "Client verification 439"
Cohesion: 0.29
Nodes (8): DeepSpaceSaga.Client.UI.Screens.TempCharacterImage, DeepSpaceSaga.Client.Portraits, Lazy, MalePortraitTests, TemporaryPacks, FemaleRoot, MaleRoot, Root

### Community 440 - "Client verification 440"
Cohesion: 0.45
Nodes (5): Fact, InlineData, Task, Theory, HostileSelectedInfoTests

### Community 441 - "Client verification 441"
Cohesion: 0.36
Nodes (4): CancellationToken, IAsyncEnumerable, ValueTask, PanelEngineConnection

### Community 442 - "Client verification 442"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, PointCombatUS0013TK0004Tests

### Community 443 - "Client verification 443"
Cohesion: 0.53
Nodes (3): InlineData, Theory, TetrarchLoadoutTests

### Community 444 - "Engine verification 444"
Cohesion: 0.40
Nodes (5): Fact, InvalidOperationException, SimulationEngine, TargetInvocationException, CombatReportOutboxTests

### Community 445 - "Engine verification 445"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, PointCombatUS0018TK0005Tests

### Community 446 - "Engine verification 446"
Cohesion: 0.49
Nodes (3): Fact, SimulationEngine, PortFeeScheduleTests

### Community 447 - "Historical module combat 447"
Cohesion: 0.20
Nodes (7): Ручная ПРО EP-0008 под очередь, Evidence, Tickets, Canonical context and execution, EP-0009-US-0007-TK-0001-manual-fire-burst, Canonical context and execution, EP-0009-US-0007-TK-0002-defense-panels-burst-ui

### Community 448 - "Point combat contract 448"
Cohesion: 0.20
Nodes (9): Atomic catalog staging correction, Consumer prerequisite correction, Порядок исполнения и coverage, Итог DAG — 2026-10-21, Execution correction 2026-10-20, Legacy consumer migration before runtime deletion, Migration prerequisite correction, Save18 prerequisite correction (+1 more)

### Community 449 - "Release requirements 449"
Cohesion: 0.20
Nodes (9): За рамками первого релиза, Команды, Требования первого релиза, Уже существующая основа, Asteroid ice stock, Добыча льда, Решение первого релиза, Параметры Drilling Unit (+1 more)

### Community 450 - "Engine verification 450"
Cohesion: 0.44
Nodes (4): InternalsVisibleToAttribute, Fact, SimulationEngine, EconomyBalanceSeamTests

### Community 451 - "Snapshot contracts 451"
Cohesion: 0.31
Nodes (7): ImmutableArray, JsonSerializerOptions, List, Type, Utf8JsonReader, Utf8JsonWriter, ImmutableArrayDefaultJsonConverter

### Community 452 - "Snapshot contracts 452"
Cohesion: 0.40
Nodes (7): SimulationSpeed, Speed0, Speed1, Speed2, Speed3, Speed4, SimulationSpeedExtensions

### Community 453 - "Motion.Tests 453"
Cohesion: 0.29
Nodes (5): CombatRangeMath, Fact, InlineData, Theory, CombatRangeMathTests

### Community 454 - "Contracts.Tests 454"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, StationMarketKnowledgeSnapshotTests

### Community 455 - "Contracts.Tests 455"
Cohesion: 0.42
Nodes (4): Fact, InlineData, Theory, VoyageFinanceContractTests

### Community 456 - "Engine verification 456"
Cohesion: 0.40
Nodes (4): Fact, InlineData, Theory, DisabledModuleTests

### Community 457 - "Engine verification 457"
Cohesion: 0.56
Nodes (3): Fact, SimulationEngine, PointCombatUS0003TK0004Tests

### Community 458 - "Engine verification 458"
Cohesion: 0.58
Nodes (3): Fact, SimulationEngine, RationScheduleTests

### Community 459 - "Engine verification 459"
Cohesion: 0.49
Nodes (4): Fact, InlineData, Theory, TradingContinuationReviewTests

### Community 460 - "Motion.Tests 460"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, PointCombatUS0007TK0001Tests

### Community 461 - "EP 0011 short fixes 461"
Cohesion: 0.22
Nodes (8): Решения пользователя, Реализация 2026-10-18 — supersedes planning-only scope, Расширение палитры — 2026-10-18T10:03:20Z, Технические предположения, Проверенные факты, EP-0011 — Grounding и технический контракт, Связь с требованиями / gaps, Palette execution — 2026-10-18

### Community 462 - "Client verification 462"
Cohesion: 0.33
Nodes (5): DeepSpaceSaga.Client.UI.Screens.ScenarioSelect, Fact, InlineData, Theory, ScenarioSelectScreenTests

### Community 463 - "Release requirements 463"
Cohesion: 0.22
Nodes (8): Требования к выполнению, Будущие экраны первого релиза, Проверка, Цель, Область применения, Уже существующие экраны, требующие переименования, Layout/helper classes, Screen Naming Convention Refactor

### Community 465 - "Client verification 465"
Cohesion: 0.64
Nodes (3): InlineData, Theory, CountermeasureContentTests

### Community 466 - "Client verification 466"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, DefeatScreenTests

### Community 467 - "Engine verification 467"
Cohesion: 0.56
Nodes (3): Fact, SimulationEngine, AuthoritativeHostileAccessTests

### Community 468 - "Engine verification 468"
Cohesion: 0.44
Nodes (4): Fact, InlineData, Theory, AuthoritativeOrbitRuntimeTests

### Community 470 - "Client interface 470"
Cohesion: 0.43
Nodes (6): Data, Lazy, Data, GameInfo, Title, Version

### Community 471 - "03 Design 471"
Cohesion: 0.25
Nodes (7): Геометрия и состояния, Характер, Шаблон для следующей кнопки, Палитра, Файлы и воспроизводимость, Смысл символов, Deep Space Saga — стиль кнопок тактической карты

### Community 472 - "Client interface 472"
Cohesion: 0.50
Nodes (5): SKBitmap, SKCanvas, SKPaint, SKRect, NinePatch

### Community 473 - "Client verification 473"
Cohesion: 0.43
Nodes (4): Fact, InlineData, Theory, MapBackgroundPaletteTests

### Community 474 - "Client verification 474"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, VoyageFuelContentTests

### Community 475 - "Contracts.Tests 475"
Cohesion: 0.39
Nodes (4): Fact, InlineData, Theory, PointCombatUS0001TK0002Tests

### Community 476 - "Contracts.Tests 476"
Cohesion: 0.36
Nodes (5): CombatJournalEntry, Fact, InlineData, Theory, PointCombatUS0004TK0002Tests

### Community 478 - "Engine verification 478"
Cohesion: 0.43
Nodes (5): InlineData, Task, Theory, LocalWorldSaveRoundtripTests, Settings

### Community 479 - "Release requirements 479"
Cohesion: 0.29
Nodes (6): Решения первого релиза, Требования первого релиза, Предварительные условия, Диалоги с представителями станций, Цель, MVP-вариант

### Community 480 - "Release requirements 480"
Cohesion: 0.29
Nodes (6): Назначение, За рамками первого релиза, Связанные механики, Функциональность первого релиза, Hire, Статус реализации (MVP)

### Community 481 - "Release requirements 481"
Cohesion: 0.29
Nodes (6): Примечания, Назначение, Оформление, Связанные механики, Функциональность первого релиза, MainMenu

### Community 482 - "Engineering documentation 482"
Cohesion: 0.29
Nodes (6): Проверки — 2026-10-10, Не выполнялось, Полная матрица экономической приёмки, Основная рабочая копия, Отдельная ветка EP-0004, Воспроизводимые probes

### Community 483 - "Client verification 483"
Cohesion: 0.62
Nodes (3): Fact, Task, CombatPresentationResumeTests

### Community 484 - "Client verification 484"
Cohesion: 0.48
Nodes (4): Fact, Task, DefaultSystemContentTests, Settings

### Community 485 - "Client verification 485"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, LocalClusterContentTests

### Community 487 - "Client verification 487"
Cohesion: 0.52
Nodes (3): Fact, Task, PhaseThreeLocaleTests

### Community 488 - "Contracts.Tests 488"
Cohesion: 0.62
Nodes (3): AuthoritativeSnapshot, Fact, CombatReportBatchTests

### Community 489 - "Engine verification 489"
Cohesion: 0.57
Nodes (3): Fact, ScenarioException, EngineContentLoaderSaveFileTests

### Community 490 - "Engine verification 490"
Cohesion: 0.38
Nodes (4): Fact, InlineData, Theory, PointCombatUS0018TK0003Tests

### Community 492 - "Release requirements 492"
Cohesion: 0.33
Nodes (5): Решения первого релиза, Требования первого релиза, Экипаж корабля и диалоги, Требует решения, Цель

### Community 493 - "Release requirements 493"
Cohesion: 0.33
Nodes (5): Разделение ресурсов, Требования первого релиза, Решение первого релиза, Электричество и Energy Cells, Границы MVP

### Community 494 - "Release requirements 494"
Cohesion: 0.33
Nodes (5): Назначение, Связанные механики, Функциональность первого релиза, Contracts, Статус реализации (MVP)

### Community 495 - "Release requirements 495"
Cohesion: 0.33
Nodes (5): Визуальный стиль, Назначение, Будущие кандидаты настроек, Функциональность первого релиза, Settings

### Community 496 - "Release requirements 496"
Cohesion: 0.33
Nodes (5): Назначение, Размер окна, Связанные механики, Функциональность первого релиза, Ship

### Community 498 - "Client verification 498"
Cohesion: 0.53
Nodes (3): InlineData, Theory, PhaseThreeCombatInfoTests

### Community 499 - "Client verification 499"
Cohesion: 0.40
Nodes (4): InlineData, Task, Theory, PhaseThreeCombatUiFlowTests

### Community 500 - "Contracts.Tests 500"
Cohesion: 0.60
Nodes (3): Fact, ObjectMotionSnapshot, PointCombatUS0006TK0002Tests

### Community 501 - "Release requirements 501"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Cargo

### Community 502 - "Release requirements 502"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Character Communication

### Community 503 - "Release requirements 503"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Dialog

### Community 504 - "Release requirements 504"
Cohesion: 0.40
Nodes (4): Назначение, Связанные механики, Функциональность первого релиза, Loot

### Community 505 - "Engineering documentation 505"
Cohesion: 0.40
Nodes (4): Контроль производительности, Исправления по результатам ревью, Границы хранения и совместимость, Проверки

### Community 506 - "Engineering documentation 506"
Cohesion: 0.40
Nodes (4): Новое окно торговли, Код и данные, Проверка, Поведение

### Community 507 - "Snapshot contracts 507"
Cohesion: 0.70
Nodes (4): NavigationPhase, Approach, EscapeDepart, EscapeTurn

## Knowledge Gaps
- **2968 isolated node(s):** `CharacterPortraits`, `CombatEncounterCommandTypes`, `ManeuverCommandTypes`, `CombatCommandTypes`, `TorpedoLauncherCommandTypes` (+2963 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 4591 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **16 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DeepSpaceSaga.Contracts` connect `Engine simulation 16` to `Client interface 0`, `Client verification 2`, `Engine simulation 3`, `Engine simulation 5`, `Client verification 6`, `Client verification 7`, `Client interface 8`, `Engine simulation 9`, `Engine verification 11`, `Client verification 12`, `Client interface 13`, `Engine simulation 14`, `Client verification 15`, `Engine verification 17`, `Engine verification 19`, `Client verification 20`, `Engine verification 21`, `Engine verification 22`, `Client verification 23`, `Client verification 24`, `Client interface 25`, `Client verification 26`, `Client verification 27`, `Client interface 28`, `Client interface 29`, `Engine simulation 30`, `Client verification 31`, `Client interface 32`, `Engine simulation 33`, `Engine verification 34`, `Engine verification 35`, `Client interface 36`, `Client interface 37`, `Client interface 39`, `Client verification 40`, `Engine verification 43`, `Client interface 44`, `Engine simulation 45`, `Client interface 47`, `Client verification 48`, `Engine simulation 49`, `EconomyBalance 50`, `Engine verification 52`, `Client verification 53`, `Client verification 56`, `Client verification 57`, `Engine simulation 61`, `Client interface 66`, `Motion.Tests 67`, `Motion.Tests 68`, `Client verification 73`, `Engine simulation 76`, `Client interface 77`, `Client interface 78`, `Engine verification 79`, `Client interface 82`, `Client interface 83`, `Local session transport 85`, `Motion.Tests 86`, `Engine simulation 87`, `Engine verification 89`, `Engine verification 90`, `Engine verification 91`, `Client interface 92`, `Client verification 93`, `Client interface 94`, `Client interface 96`, `Client interface 97`, `Client verification 101`, `Client verification 102`, `Client verification 107`, `Motion.Tests 108`, `Engine simulation 110`, `Snapshot contracts 111`, `Engine verification 114`, `Client interface 116`, `Client verification 117`, `Engine verification 118`, `Client verification 120`, `Client verification 121`, `Client interface 122`, `Engine verification 123`, `Client verification 124`, `Engine verification 125`, `Client interface 127`, `Client verification 129`, `Client verification 130`, `Performance.Tests 131`, `Engine simulation 133`, `Engine simulation 134`, `Client interface 135`, `Engine verification 136`, `Engine verification 140`, `Client verification 143`, `Contracts.Tests 144`, `Engine verification 145`, `Engine verification 147`, `Engine verification 148`, `Client verification 149`, `Client verification 150`, `Engine verification 152`, `Engine verification 153`, `Client verification 154`, `Client interface 155`, `Client interface 157`, `Engine verification 159`, `Client verification 162`, `Client verification 164`, `Client verification 165`, `EconomyBalance 166`, `EconomyBalance 168`, `Contracts.Tests 169`, `Engine verification 170`, `Engine simulation 171`, `Client interface 172`, `Client interface 174`, `Snapshot contracts 175`, `Contracts.Tests 176`, `Client interface 178`, `Client verification 182`, `Engine simulation 185`, `Engine simulation 186`, `Client interface 187`, `Engine verification 189`, `Engine verification 190`, `Engine verification 191`, `Engine verification 192`, `Client verification 193`, `Client interface 197`, `Snapshot contracts 198`, `Client verification 199`, `Contracts.Tests 200`, `Engine simulation 201`, `Contracts.Tests 205`, `Client interface 206`, `Client verification 212`, `Contracts.Tests 213`, `Engine verification 214`, `Motion 215`, `Client verification 216`, `Client verification 217`, `Engine verification 218`, `Client verification 229`, `Snapshot contracts 230`, `Contracts.Tests 231`, `Engine verification 235`, `Performance 236`, `Snapshot contracts 238`, `EconomyBalance.Tests 239`, `Engine verification 240`, `Client verification 242`, `Client interface 243`, `Client interface 247`, `Client interface 248`, `Engine verification 249`, `Motion.Tests 250`, `Engine verification 252`, `Engine verification 253`, `Client verification 254`, `Snapshot contracts 258`, `Client verification 261`, `Client verification 262`, `EconomyBalance.Tests 263`, `Engine verification 264`, `Client interface 268`, `Client verification 270`, `Engine verification 272`, `EconomyBalance.Tests 273`, `Engine verification 274`, `Engine verification 275`, `Client interface 276`, `Client interface 277`, `Client interface 278`, `Client verification 279`, `Client verification 280`, `Client verification 281`, `Client verification 282`, `Engine verification 284`, `Engine verification 285`, `Engine verification 286`, `Client verification 288`, `Client verification 290`, `Motion 291`, `Client verification 292`, `Engine verification 294`, `Engine verification 295`, `Engine verification 296`, `Engine verification 298`, `Engine simulation 299`, `Contracts.Tests 300`, `Client verification 301`, `Contracts.Tests 302`, `Client verification 304`, `Client verification 305`, `Client verification 306`, `Engine verification 307`, `Engine verification 308`, `Point combat contract 310`, `Engine simulation 311`, `Client interface 313`, `Client interface 314`, `Snapshot contracts 315`, `Engine verification 316`, `Contracts.Tests 317`, `Client verification 318`, `Contracts.Tests 323`, `Engine verification 324`, `Client verification 325`, `Engine verification 326`, `Engine verification 327`, `Engine verification 328`, `Engine verification 329`, `Engine verification 330`, `Engine verification 331`, `EconomyBalance 332`, `Client verification 338`, `Engine verification 341`, `Engine simulation 344`, `Client verification 346`, `Engine verification 348`, `Engine verification 349`, `Engine verification 350`, `Engine verification 351`, `EconomyBalance.Tests 359`, `Client verification 363`, `Snapshot contracts 365`, `Client verification 366`, `Contracts.Tests 369`, `Engine verification 371`, `Engine verification 372`, `Engine verification 373`, `Engine verification 374`, `Engine verification 375`, `Client interface 385`, `Client verification 387`, `Engine verification 388`, `Engine verification 389`, `Engine verification 390`, `Engine verification 398`, `Engine verification 406`, `Client verification 419`, `Contracts.Tests 420`, `Client verification 424`, `Engine verification 426`, `Engine verification 428`, `Engine verification 429`, `Client verification 440`, `Client verification 442`, `Client verification 443`, `Engine verification 444`, `Engine verification 445`, `Engine verification 446`, `Engine verification 450`, `Snapshot contracts 451`, `Snapshot contracts 452`, `Motion.Tests 453`, `Contracts.Tests 454`, `Contracts.Tests 455`, `Engine verification 456`, `Engine verification 457`, `Engine verification 458`, `Engine verification 459`, `Motion.Tests 460`, `Client interface 464`, `Client verification 465`, `Engine verification 467`, `Engine verification 468`, `Engine verification 469`, `Client verification 473`, `Contracts.Tests 475`, `Engine verification 477`, `Engine verification 478`, `Client verification 483`, `Client verification 484`, `Client verification 487`, `Engine verification 489`, `Engine verification 490`, `Client verification 498`, `Client verification 499`, `Snapshot contracts 507`, `Contracts.Tests 509`?**
  _High betweenness centrality (0.332) - this node is a cross-community bridge._
- **Why does `GameSessionScreen` connect `Client interface 0` to `Client verification 129`, `Client verification 2`, `Client verification 386`, `Client verification 261`, `Client verification 6`, `Client verification 262`, `Client interface 8`, `Client verification 137`, `Client verification 12`, `Client interface 13`, `Client verification 20`, `Client interface 277`, `Client verification 150`, `Client verification 23`, `Client verification 280`, `Client verification 149`, `Client verification 26`, `Client interface 155`, `Client verification 154`, `Client interface 29`, `Client interface 158`, `Engine simulation 30`, `Client verification 31`, `Client verification 162`, `Client interface 36`, `Client verification 293`, `Client verification 164`, `Client interface 39`, `Client verification 424`, `Client verification 165`, `Client interface 172`, `Client verification 301`, `Client interface 46`, `Client verification 48`, `Client verification 305`, `Client interface 178`, `Client verification 306`, `Client verification 53`, `Client interface 54`, `Client verification 182`, `Client interface 313`, `Client verification 57`, `Client interface 187`, `Client interface 314`, `Snapshot contracts 315`, `Engine verification 189`, `Client verification 318`, `Snapshot contracts 452`, `Client interface 197`, `Snapshot contracts 198`, `Client verification 325`, `Client interface 78`, `Client interface 206`, `Client interface 208`, `Client interface 81`, `Client verification 209`, `Client interface 83`, `Client verification 212`, `Motion 215`, `Client verification 216`, `Client verification 345`, `Client verification 217`, `Client interface 94`, `Client interface 97`, `Client verification 483`, `Client verification 101`, `Client verification 102`, `Client interface 248`, `Client verification 121`, `Client verification 124`, `Client verification 254`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Why does `SimulationEngine` connect `Engine simulation 3` to `Client interface 0`, `Client verification 2`, `Engine simulation 5`, `Client verification 6`, `Engine simulation 9`, `Client verification 12`, `Client verification 15`, `Engine simulation 16`, `Engine verification 17`, `Engine verification 19`, `Engine verification 22`, `Client verification 23`, `Client interface 28`, `Engine simulation 30`, `Client verification 31`, `Engine simulation 33`, `Engine verification 34`, `Engine verification 43`, `Engine simulation 45`, `Engine simulation 49`, `EconomyBalance 50`, `Engine verification 52`, `Client interface 55`, `Engine simulation 61`, `Client verification 73`, `Engine simulation 76`, `Engine verification 79`, `Local session transport 85`, `Motion.Tests 86`, `Engine simulation 87`, `Engine simulation 110`, `Snapshot contracts 111`, `Engine simulation 119`, `Client verification 120`, `Client verification 121`, `Engine verification 123`, `Engine verification 125`, `Client verification 129`, `Client verification 130`, `Performance.Tests 131`, `Engine simulation 133`, `Engine simulation 134`, `Engine simulation 138`, `Contracts.Tests 144`, `Engine verification 147`, `Engine verification 148`, `Engine verification 153`, `Client interface 155`, `Engine verification 159`, `EconomyBalance 168`, `Engine simulation 171`, `Client verification 180`, `Client verification 182`, `Engine simulation 186`, `Engine verification 192`, `Contracts.Tests 200`, `Engine simulation 201`, `Client verification 212`, `Client verification 229`, `Engine verification 235`, `EconomyBalance.Tests 239`, `Client verification 242`, `Engine verification 249`, `Client verification 262`, `Engine verification 272`, `Engine verification 274`, `Engine verification 275`, `Client verification 280`, `Client verification 282`, `Client verification 292`, `Contracts.Tests 300`, `Client verification 304`, `Client verification 306`, `Engine simulation 311`, `Snapshot contracts 315`, `Engine verification 316`, `Contracts.Tests 317`, `Engine verification 324`, `Engine verification 326`, `Engine verification 329`, `Engine verification 331`, `Engine simulation 344`, `Engine verification 348`, `Snapshot contracts 365`, `Client verification 366`, `Engine verification 398`, `Client verification 441`, `Engine verification 445`, `Snapshot contracts 452`, `Client verification 465`, `Engine verification 468`, `Engine verification 469`, `Client verification 474`, `Engine verification 478`, `Engine verification 489`?**
  _High betweenness centrality (0.079) - this node is a cross-community bridge._
- **Are the 112 inferred relationships involving `SimulationEngine` (e.g. with `.AiContentKeepsHumanMarkets()` and `.SavedLegacySessionDoesNotGenerate()`) actually correct?**
  _`SimulationEngine` has 112 INFERRED edges - model-reasoned connections that need verification._
- **Are the 96 inferred relationships involving `GameSessionScreen` (e.g. with `.AiBaseSelectableAndIdentified()` and `.HumanScientificMilitaryNotAi()`) actually correct?**
  _`GameSessionScreen` has 96 INFERRED edges - model-reasoned connections that need verification._
- **Are the 191 inferred relationships involving `ObjectMotionSnapshot` (e.g. with `.Records_authoritative_geometry_events_and_throttled_visuals_without_mutating_snapshot()` and `.Compact_omission_retains_static_target_until_explicit_removal_is_presented()`) actually correct?**
  _`ObjectMotionSnapshot` has 191 INFERRED edges - model-reasoned connections that need verification._
- **Are the 81 inferred relationships involving `AuthoritativeSnapshot` (e.g. with `.DockedBuffer()` and `.DecorationsDoNotAffectBoundaryOrEntities()`) actually correct?**
  _`AuthoritativeSnapshot` has 81 INFERRED edges - model-reasoned connections that need verification._