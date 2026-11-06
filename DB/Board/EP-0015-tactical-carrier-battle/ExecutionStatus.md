# EP-0015 — статус исполнения

## Текущее состояние — 2026-11-06

Исполнение продолжается. Все четыре тикета US-0001 выполнены технически и проверены; 78/81 тикетов. Бой запускается из карты; форма подготовки, поле и приказы движения реализованы; планирование стрельбы и показ залпа реализованы; вкладки и прогноз реализованы; возврат из песочницы проверен; начинается подготовка удаления старого боя. Остальные 3 тикетов и завершающая документационная история остаются открытыми.

- Ветка `codex/ep-0015-tactical-carrier-battle`, remote `origin` = `https://github.com/SuhilovVitaly/DSS`.
- Отмена запрета GitHub зафиксирована служебным коммитом `22bb633145d479391b985eb73d148bb7cad7de94`, push и remote SHA проверены.
- Принятые уточнения D-USER-14/15: исходники — `VData/Planning/V4.0` Markdown; JSON создаются по таблицам; Python-симулятор не нужен. Служебный коммит `06c5fc69032de5b7b7fe94d19079b5284998846c`, push и remote SHA проверены. Прежний preflight-блокер отсутствующих исходников снят этими указаниями.
- История US-0017 уже существует, зависит от всех функциональных историй и выполняется последней.
- Linux route работает: GitHub Actions run 42086320971 PASS на Ubuntu 24.04 / CPython 3.12.3 / glibc 2.39. Это bootstrap/export + текущие Motion/content tests; геометрия дополнительно проверена на Windows/Linux в PlatformValidation.md; core battle/AI реализован и проверен 90 эталонами Windows/Linux; сессия/соединение и модальная навигация реализованы; вход F8 реализован; полные экраны ещё впереди.

## Проверки

[ContentValidation.md](ContentValidation.md): build PASS (0 warnings/errors), 5402/5402 теста в шести проектах, scoped format и diff check PASS. Длительные Engine/Economy corpora проверены полными разделёнными наборами по точным ID, без ослабления assertions. Это исходный регрессионный срез. Core/session parity уже PASS; полная нативная и продуктовая приёмка пока NOT RUN.

Продолжение: US-0017 TK-0003. TK-0001 exact Raid parity is open; independent performance and native checks continue. US-0005 и US-0007 технически завершены. US-0001 технически завершена, включая сообщение и лог ошибки данных на входе F8.

Reference repair: service fdc52ee5ebd1b16f398e3204a24fe4ee4d119e92 → Linux run 42090979851 PASS → schema-2 JSON imported. Numeric expectations unchanged; rearHits name corrected per Combat §7.1. Final repair SHA 0497bcbbc2fe748c73567690450eacc2aaaaf2b2, remote verified.

| Тикет | Коммит | Push | Remote | Проверки |
|---|---|---|---|---|
| EP-0015-US-0001-TK-0001-content-data | 8015f9852f2534a95f16fc8b8ee668932c7062a4 | PASS: remote SHA verified | origin | 29 JSON; 5402 PASS; build/format/diff PASS |
| EP-0015-US-0001-TK-0002-content-definitions | 8daec47976c5746d6aca54c1db3d66e187c32e24 | PASS: remote SHA verified | origin | 3 new tests PASS; build/format/diff PASS; unchanged baseline reused |
| EP-0015-US-0001-TK-0003-content-loader | 981a5f0921e6c37291400f4491f0e9984996aa9f | PASS: remote SHA verified | origin | 69 targeted PASS; Engine 2397, Client 2324, Economy 2 regression PASS; build/format/diff PASS |
| EP-0015-US-0001-TK-0004-content-sync | a0fb809b25dbec7461a17573cc8ece1e0b1881cf | PASS: remote SHA verified | origin | 71 Engine + 2 Client targeted PASS; content export/check/build/format/diff PASS |
| EP-0015-US-0002-TK-0001-py-math | 1926f73afffd6382c6e12d1286969da18555497a | PASS: remote SHA verified | origin | 280/280 Motion; build/format/diff PASS |
| EP-0015-US-0002-TK-0002-reference-fixtures | 8148aec3a43bf1df7c8fb8a3457af351623cbea8 | PASS: remote SHA verified | origin | Linux 42087024500 PASS; Windows 87 Engine; 90 battles/19 turns/1088 vectors; build/format/diff PASS |
| EP-0015-US-0002-TK-0003-mulberry32 | 53144cdbace23309b2f18889af102caf8fd6b680 | PASS: remote SHA verified | origin | 96 Engine + 280 Motion PASS; build/format/diff PASS |
| EP-0015-US-0002-TK-0004-geometry | 8a43baf53ac0ff3f88f518b769893ef7ec244283 | PASS: remote SHA verified | origin | Windows 1372 Motion + 96 Engine PASS; 1088 exact IEEE vectors; license copy/build/format/diff PASS |
| EP-0015-US-0002-TK-0005-limits-and-chance | fc29503a812f6793779d3564f22884a8cd0eecdb | PASS: remote SHA verified | origin | 1376 Motion PASS; 34 reference shot chances; build/format/diff PASS |
| EP-0015-US-0002-TK-0006-platform-determinism | 61c3c4410f0931d7f71fcfe6254ab99e80114ba7 | PASS: remote SHA verified | origin | Windows/Linux 1376 Motion; 1088 exact vectors; evidence/link/diff PASS |
| EP-0015-US-0003-TK-0001-battle-assembly | cdd2f3d6ba4ff648961fa1e4dcebf71f2abc4b09 | PASS: remote SHA verified | origin | 101 Engine + 1376 Motion PASS; independent assembly trace; build/format/diff PASS |
| EP-0015-US-0003-TK-0002-movement | 692e9f5501f7cb994d7d27c479990de6e07ef782 | PASS: remote SHA verified | origin | 104 Engine + 1376 Motion PASS; all 19 slices; build/format/diff PASS |
| EP-0015-US-0003-TK-0003-salvo | 9a505142e2b9c6f80a448c2174d893c007fada22 | PASS: remote SHA verified | origin | 122 Engine + 1376 Motion PASS; all 19 salvo states/calls; build/format/diff PASS |
| EP-0015-US-0003-TK-0004-turn-and-outcome | 834a6ce5f81d645a3a43dbc0bb4befb9222c9399 | PASS: remote SHA verified | origin | 130 targeted; 6443 fresh regression across 6 projects; build/format/diff PASS |
| EP-0015-US-0004-TK-0001-steering | 5c80365baec6e22ca1465f690c730f367d240194 | PASS: remote SHA verified | origin | 138 Engine + 19 source guard PASS; first trace pursuit; build/format/diff PASS |
| EP-0015-US-0004-TK-0002-striker-fighter-tail | d4a2ffbbc7e467c62508c68522d6c8d57c83ff8b | PASS: remote SHA verified | origin | 144 Engine + 19 source guard PASS; modes/hunt/grid/RNG; build/format/diff PASS |
| EP-0015-US-0004-TK-0003-interceptor-commander | cacd325503acb679244764a25acd28f13ab172f7 | PASS: remote SHA verified | origin | 154 Engine + 19 source guard PASS; all first-turn orders/58 calls; build/format/diff PASS |
| EP-0015-US-0004-TK-0004-golden-battles | 1f92fafffe9a6c121205d0c5a740edc5929b077a (code); evidence 682a9b758724922b6b91c49c2d523e078afd9020, remote verified | PASS: remote SHA verified; Linux 42098486879 PASS | origin | 245 Engine; 90 summaries + continuous trace on Windows/Linux; build/format/diff PASS |
| EP-0015-US-0005-TK-0001-contracts | 7b073f4fab7ee9ef8b8a6c665fa90f084c16d9cc | PASS: remote SHA verified | origin | 283 Contracts + 245 Engine PASS; Client 2324 cases covered across broad/isolated runs (one broad timeout retained); build/format/diff PASS |
| EP-0015-US-0005-TK-0002-engine-session | f36f714bce975686e34084eebdad12f83d1ca67b | PASS: remote SHA verified | origin | 274 Engine PASS; complete seed-1 via session both modes; build/format/diff PASS |
| EP-0015-US-0005-TK-0003-world-guard | ab56c05a1d666de24b414f0d7ff3ae7fc28bece2 | PASS: remote SHA verified | origin | 436 Engine PASS; exact world preservation/save bytes; build/format/diff PASS |
| EP-0015-US-0005-TK-0004-connection-api | dff18600fccd636aa4d8704a96263cdcdf4e00f5 | PASS: remote SHA verified | origin | 284 Engine + 283 Contracts PASS; concurrent tick boundary; build/format/diff PASS |
| EP-0015-US-0005-TK-0005-client-session-handle | 9eec6113effc3f5481eaf9651972d2103cb90786 | PASS: remote SHA verified | origin | 33 Client PASS; forwarding/log/refusals/locales; build/format/diff PASS |
| EP-0015-US-0005-TK-0006-session-tests | ad9817828448e70e051c6363f8bac7f4f277c052 | PASS: remote SHA verified; Linux42106901851 PASS | origin | 378 Engine PASS; 180 independent connection battles + phase save guards; build/format/diff PASS |
| EP-0015-US-0006-TK-0001-keyboard | 16bd3e4cdb9f540ae17068454859c3cdfa4fae53 | PASS: remote SHA verified | origin | 68 Client PASS; real headless PollKeyboard/focus routing; build/format/diff PASS |
| EP-0015-US-0006-TK-0002-input-settings | 13fdbcda3aa16f1b21d001e57780a4ec387c5e9a | PASS: remote SHA verified | origin | 103 Client PASS; strict settings/game-binding conflicts/fallback; build/format/diff PASS |
| EP-0015-US-0006-TK-0003-mouse | f5265b8c14579d2de49016f00c282a3ac15ef518 | PASS: remote SHA verified | origin | 121 Client PASS; native-handler headless routing/buttons/wheel; build/format/diff PASS |
| EP-0015-US-0006-TK-0004-widgets-basic | 427ba7e8f53d700247b936ebbc9043236d8a7dbd | PASS: remote SHA verified | origin | 79 Client PASS; four CPU raster scales; build/format/diff PASS |
| EP-0015-US-0006-TK-0005-widgets-lists | 172569fb43a5e102758ce49d38a8f24f326e18d2 (original); reflow correction 20b065d5edc1aead2b8f42dfe226bd1b2185908f | PASS: remote SHA verified | origin | 108 Client PASS; popup/wrap/filter/tooltip + 4 raster scales; glyph fix completed TK-0006; Linux42118204297 PASS; reflow reproduction/fix106 Client PASS; build/format/diff PASS |
| EP-0015-US-0006-TK-0006-text-and-numbers | cab5d313edd27e3ae1c8f50d24e79a69b7f25f6c (code), 4611628819a12ebb81b642f7ac89c5e5aef80088 (evidence), remote verified | PASS: remote SHA verified; Linux42113068311 PASS (42112081477 superseded native prerequisite failure) | origin | Windows113 Client +378 Engine PASS; explicit ToEven/glyph substitution; Linux97 Client +378 Engine +1376 Motion PASS |
| EP-0015-US-0006-TK-0007-linux-native-runtime | 4080197095f45cb7cce7ec48ff846d08867c61a7 (code), 55332e709e9367be86a351d4fce8d31b74b5e3ee (evidence), remote verified | PASS: remote verified; Linux42113068311 PASS | origin | Windows restore/build113 Client PASS; actual Linux .so transitive output verified; config/diff PASS |
| EP-0015-US-0007-TK-0001-modal-navigation | 6ac53a85fcc525cb73b007c6af9c3ae19bfee5e1 | PASS: remote verified; Linux42116152154 PASS | origin | 114 Client +378 Engine PASS; real modal lifecycle/opacity/camera; build/format/diff PASS |
| EP-0015-US-0007-TK-0002-battle-key-and-hint | b19f3d5bcc349ad636f6b288a40577b67fdc01f5 | PASS: remote SHA verified | origin | Linux42117903122 PASS; full2428 Client PASS pre-log; final106 focused PASS; build/format/diff PASS |
| EP-0015-US-0007-TK-0003-setup-screen | bfd7437560cb86511896e45756f2194e7a6f54e8 | PASS: remote SHA verified | origin | Linux42120143435 PASS; 115 Client + 378 Engine PASS; four raster layouts; build/format/diff PASS |
| EP-0015-US-0007-TK-0004-battle-keys-guard | b1bc2f694c7eae86e1b22312529156b5e4905cb8 | PASS: remote SHA verified | origin | Linux42120776299 PASS; 154 Client + 378 Engine PASS; F10 PNG and world-event guard; build/format/diff PASS |
| EP-0015-US-0008-TK-0001-battle-screen-and-camera | 013467b9c0d45ec49c606f051a79de7613141094 | PASS: remote SHA verified | origin | Linux42121522199 PASS; 158 Client +378 Engine +19 Motion guard PASS; build/format/diff PASS |
| EP-0015-US-0008-TK-0002-panel-header | 1ce00d3ff5e0c53e3f9354d7362bb31759922737 | PASS: remote SHA verified | origin | Linux42122019390 PASS; 160 Client +378 Engine +19 Motion guard PASS; real salvo and refusal raster; build/format/diff PASS |
| EP-0015-US-0008-TK-0003-battle-map-render | f03c9faa9cb5515ec04eb25accfb004153e7d7fb | PASS: remote SHA verified | origin | Linux42122702685 PASS; 162 Client +378 Engine +19 Motion guard PASS; 2 visually reviewed CPU rasters |
| EP-0015-US-0008-TK-0004-movement-orders | 2a36e9037c1461bc7bfd5e90c72e2dface34fe1e | PASS: remote SHA verified | origin | Linux42123739150 PASS; Client166 final; Engine378 + Motion guard19 on behavior; inspected final raster |
| EP-0015-US-0008-TK-0005-movement-animation | 2d129f7cb5d3677ed068f291f4f635bad11dd1d0 | PASS: remote SHA verified | origin | Linux42124499090 PASS; 168 Client +378 Engine +19 Motion guard PASS; delayed response and 900ms gate |
| EP-0015-US-0009-TK-0001-fire-planning | e9f258bf978e2198a758842ee94df8cbf38e9dc6 | PASS: remote SHA verified | origin | Linux42125251392 PASS; 170 Client +378 Engine +19 Motion guard PASS; inspected Fire CPU raster |
| EP-0015-US-0009-TK-0002-results-phase | 21a0a649e44a09c6b25a354ed63939fc72e1519a | PASS: remote SHA verified | origin | Linux42125911783 PASS; 173 Client +378 Engine +19 Motion guard PASS; two inspected CPU rasters |
| EP-0015-US-0009-TK-0005-journal-contract | aca445ca8c4f1be69e5fb35c2315c30d2c315801 | PASS: remote SHA verified | origin | Linux42126314471 PASS; 284 Contracts +173 Client +378 Engine +19 Motion guard PASS |
| EP-0015-US-0009-TK-0006-journal-events | 8ec240237f774f3d5fda70f33b815685ddbf0beb | PASS: remote SHA verified | origin | Linux42126749647 PASS; 380 Engine +173 Client +19 Motion guard PASS; exact retained journal facts |
| EP-0015-US-0009-TK-0003-sides-and-log-tabs | 7bb8ee0d3a6104686d7dfbd207b5a42849ca13ef | PASS: remote SHA verified | origin | Linux42127506099 PASS; 176 Client +380 Engine +19 Motion guard PASS; inspected sides/log rasters |
| EP-0015-US-0009-TK-0004-forecast-tab | ba7ad5f40371d1defd5f4107ee0400f89483d59f | PASS: remote SHA verified | origin | 178 Client +380 Engine +19 Motion guard PASS; inspected forecast raster |
| EP-0015-US-0010-TK-0001-battle-end-ui | 20f15a74eb8aac03e96cd59857b07168a2aa428d | PASS: remote SHA verified | origin | 180 Client +380 Engine +19 Motion guard PASS; actual restart and exit |
| EP-0015-US-0010-TK-0002-sandbox-flow | 54bbf54c5bf40704fb9ad0976754a56006e794c5 | PASS: remote SHA verified | origin | full Client2479; focused182 Client +380 Engine +19 Motion guard PASS; exact world save |
| EP-0015-US-0011-TK-0001-removal-inventory | ac565d15eb726dd4fff901d51a3434f621488103 | PASS: remote SHA verified | origin | 33 scenario/content checks; build/diff PASS; current legacy consumer matrix |
| EP-0015-US-0011-TK-0002-save-fixtures | 594696d50a04578224aae9f231348af4ba580dbc | PASS: remote SHA verified | origin | capture PASS; 413 Engine PASS including real Save18/19 fixtures |
| EP-0015-US-0011-TK-0003-maneuver-input-move | 8bed54de8c1780af6ef5468c00f61ddc8062ebe6 | PASS: remote SHA verified | origin | 218 Client +380 Engine +19 Motion guard PASS; held/remapped maneuver input |
| EP-0015-US-0011-TK-0004-save-format-20 | 051df719f8e0065d70e7aedc2844ded72e4819a8 | PASS: remote SHA verified | origin | 889 Engine + 184 Client PASS; build/format/diff PASS; legacy-only audit separately recorded |
| EP-0015-US-0012-TK-0001-client-map-removal-1 | bb267c7d9e9d08fcdf7f97b4142e60030f98278e | PASS: remote SHA verified | origin | 2241 Client selected PASS; solution build/format/diff PASS |
| EP-0015-US-0012-TK-0002-client-map-removal-2 | 882323d0341aa778f237f50d0b707be538d80919 | PASS: remote SHA verified | origin | 256 Client PASS; build/format/diff PASS |
| EP-0015-US-0012-TK-0003-client-panels-removal | 5639a5ff4d9429037f79d2e9fcd10347795edf5c | PASS: remote SHA verified | origin | Client 2170/2170; solution build/format/diff PASS |
| EP-0015-US-0012-TK-0004-client-stores-removal | b103e5dc3bc54b208d721989b76f9ab9f2c19ecd | PASS: remote SHA verified | origin | Client 2086/2086; solution build/format/diff PASS |
| EP-0015-US-0012-TK-0005-client-shell-removal | 3e4d81c0124fe17df28d5bf7fe0fea2caf81eaee | PASS: remote SHA verified | origin | Client 2026/2026; solution build/format/diff PASS |
| EP-0015-US-0012-TK-0006-client-assets-locale-removal | fbcb665527d4e6c62f3c891b32c530ea83fe5184 | PASS: remote SHA verified | origin | Client 2016/2016; assets absent; 546 RU/EN keys; build/format/diff PASS |
| EP-0015-US-0013-TK-0001-local-client-removal | 453afb5adc76890cb19d90cf9ffd6d314e2412ca | PASS: remote SHA verified | origin | 2012 Client + 561 Engine PASS; build/format/diff PASS |
| EP-0015-US-0013-TK-0002-engine-removal-1 | 6e3f23933b3a155bf9b8704d2073d6ccce80ee89 | PASS: remote SHA verified | origin | 877 Engine + 223 Client selected PASS; build/format/diff PASS |
| EP-0015-US-0013-TK-0003-engine-removal-2 | b28c992c6b2705958f411f506638304e9ccde6a5 | PASS: remote SHA verified | origin | Windows build/format; 2012 Client + 919 Engine + 2 EconomyBalance PASS |
| EP-0015-US-0013-TK-0004-engine-removal-3 | e8938a522eaff0602c50d22905825edb404450bd | PASS: remote SHA verified | origin | Windows build/format; Client 1996 + Engine 2189 + 3 corpus + EconomyBalance 61 PASS |
| EP-0015-US-0013-TK-0005-engine-removal-4 | 860ea1dcc130efcf146963239c6049548fec9093 | PASS: remote SHA verified | origin | Windows build/format; 945 Engine + 223 Client + 2 EconomyBalance PASS |
| EP-0015-US-0013-TK-0006-scenario-module-cleanup | 87fceef42fdedf6d5abd2c0f167742ebadf33ba5 | PASS: remote SHA verified | origin | Windows build/format; 39 Client + 101 Engine corrections PASS, prior broad suites recorded |
| EP-0015-US-0013-TK-0007-engine-content-removal | 418b5f9a30223fe264a1eec9ea55b97ae6163e2f | PASS: remote SHA verified | origin | Windows build/format; 544 Engine + 191 Client final targets; 44 EconomyBalance PASS |
| EP-0015-US-0013-TK-0008-engine-noncombat-cleanup | 367c0a7045717a1ff9ad8f24a731cc71edbe976c | PASS: remote SHA verified | origin | Windows build/format; 745 Engine + 239 Client PASS |
| EP-0015-US-0013-TK-0009-debug-tuning-cleanup | 932d0ee6f23f05cf22b362a47409a1623e8a15bf | PASS: remote SHA verified | origin | Windows build/format; 284 Contracts + 56 Engine + 185 Client PASS |
| EP-0015-US-0013-TK-0010-legacy-save-dto-removal | current ticket commit | SHA checked immediately after push | origin | Windows build/format; 637 Engine + 201 Client PASS; archival migration and defects |
| EP-0015-US-0014-TK-0001-motion-removal | bb1ee974544b16af5943722054d867c6bc680929 | PASS: remote SHA verified | origin | Motion 1267, Engine 764, Client 265 |
| EP-0015-US-0014-TK-0002-contracts-dto-removal | 078928b0212f0694118563755f67fb0d0f328980 | PASS: remote SHA verified | origin | Contracts 175, Motion 1267, Engine 2024, Client 1967, Economy 42, Performance 9 |
| EP-0015-US-0014-TK-0003-contracts-fields-removal | adff8335582e2cd37eed033a4c19bde3b43e4843 | PASS: remote SHA verified | origin | Contracts 175, Motion 1267, Engine 2024, Client 1961, Economy 42, Performance 9 |
| EP-0015-US-0014-TK-0004-content-cleanup | c6e619ba4f6e36522aaf1adea61a71f33953f14a | PASS: remote SHA verified | origin | Contracts 175, Motion 1267, Engine 2024, Client 1963, Economy 42, Performance 9 |
| EP-0015-US-0014-TK-0005-tools-cleanup | bfdcd900721edd5d6f8fc724a0ff85540595d459 | PASS: remote SHA verified | origin | Contracts 175, Motion 1267, Engine 2138, Client 1963, Economy 61, Performance 9 |
| EP-0015-US-0015-TK-0001-locale-coverage | 904163eb46ac5bf07a45aec24c689808bafbef46 | PASS: remote SHA verified | origin | Contracts 175, Engine 380, Client 167 |
| EP-0015-US-0016-TK-0001-series-parity | 2bbd4eac958c6e0eb936bebea730a219295e69d7 (diagnostic) | PASS: remote SHA verified | origin | 4/5 tactics exact; Raid FAIL identical across Windows and Ubuntu, AC-01 open |
| EP-0015-US-0016-TK-0002-performance | bda8d373a64e27e36c1a590e4b09b8be0d88e980 | PASS: remote SHA verified | origin | Release build + 167 Client; all benchmark thresholds PASS |
| EP-0015-US-0016-TK-0003-native-acceptance | 44d2bcd2e6745e1aea83f18221abe0fb4110b3f5 | PASS: remote SHA verified | origin | 4 native OpenGL cases x 31 checks; Russian, F8 to F5/F9; hardware input NOT RUN |
| EP-0015-US-0017-TK-0001-impact-inventory | c0bfa7a068d19ee51993ddc1b94b6aefee9cef53 | PASS: remote SHA verified | origin | functional diff and documentation ownership review PASS |
| EP-0015-US-0017-TK-0002-documentation-new-contract | current ticket commit | SHA checked immediately after push | origin | 75 relative links PASS, source and content review |
