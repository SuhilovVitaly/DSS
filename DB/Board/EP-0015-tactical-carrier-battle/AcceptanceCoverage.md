# EP-0015 — карта покрытия критериев

Критерий истории → тикеты. Статусы заполняются при исполнении.

| История | Критерий | Тикеты | Статус |
|---|---|---|---|
| EP-0015-US-0001-content-and-loader | AC-01 | EP-0015-US-0001-TK-0001-content-data, EP-0015-US-0001-TK-0002-content-definitions | technical backend done; content sync PASS |
| EP-0015-US-0001-content-and-loader | AC-02 | EP-0015-US-0001-TK-0001-content-data | technical backend done; content sync PASS |
| EP-0015-US-0001-content-and-loader | AC-03 | EP-0015-US-0001-TK-0002-content-definitions, EP-0015-US-0001-TK-0003-content-loader | PASS: loader isolation and structured Client reason/no Start/log via US-0007 TK-0002 |
| EP-0015-US-0001-content-and-loader | AC-04 | EP-0015-US-0001-TK-0004-content-sync | done: 29 JSON values/order checked independently |
| EP-0015-US-0002-deterministic-core | AC-01 | EP-0015-US-0002-TK-0001-py-math | done: Motion and source guard PASS |
| EP-0015-US-0002-deterministic-core | AC-02 | EP-0015-US-0002-TK-0002-reference-fixtures | done under D-USER-14/15; missing original fields explicit in header |
| EP-0015-US-0002-deterministic-core | AC-03 | EP-0015-US-0002-TK-0003-mulberry32 | done: controls and independent assembly trace PASS |
| EP-0015-US-0002-deterministic-core | AC-04 | EP-0015-US-0002-TK-0004-geometry | Windows + Linux 1088 bitwise vectors PASS |
| EP-0015-US-0002-deterministic-core | AC-05 | EP-0015-US-0002-TK-0005-limits-and-chance | Motion implementation and 34 shot references PASS; integration dependent stories |
| EP-0015-US-0002-deterministic-core | AC-06 | EP-0015-US-0002-TK-0006-platform-determinism | done for available D-USER-15 vectors; golden battles remain US-0004 |
| EP-0015-US-0003-battle-rules | AC-01 | EP-0015-US-0003-TK-0001-battle-assembly | done: every assembly trace field and RNG42 PASS |
| EP-0015-US-0003-battle-rules | AC-02 | EP-0015-US-0003-TK-0002-movement | done: all 19 independent movement slices PASS |
| EP-0015-US-0003-battle-rules | AC-03 | EP-0015-US-0003-TK-0003-salvo | done: all 19 salvo slices, exact states/calls and edge cases PASS |
| EP-0015-US-0003-battle-rules | AC-04 | EP-0015-US-0003-TK-0004-turn-and-outcome | done: phase/outcome/statistics cases and 6443 fresh regression PASS |
| EP-0015-US-0004-battle-ai | AC-01 | EP-0015-US-0004-TK-0001-steering | done: steering/prediction/pursuit cases and first trace order PASS |
| EP-0015-US-0004-battle-ai | AC-02 | EP-0015-US-0004-TK-0002-striker-fighter-tail | done: modes/hunt/grid tie and RNG edge cases PASS |
| EP-0015-US-0004-battle-ai | AC-03 | EP-0015-US-0004-TK-0003-interceptor-commander | done: complete policy and independent first planning stage PASS |
| EP-0015-US-0004-battle-ai | AC-04 | EP-0015-US-0004-TK-0004-golden-battles | done under D-USER-15: 90 summaries + continuous trace Windows/Linux PASS |
| EP-0015-US-0005-session-contracts | AC-01 | EP-0015-US-0005-TK-0001-contracts | DTOs done; complete captures/options behavior in TK-0002 |
| EP-0015-US-0005-session-contracts | AC-02 | EP-0015-US-0005-TK-0002-engine-session | PASS: session commands; 274 Engine cases, including two complete seed-1 modes |
| EP-0015-US-0005-session-contracts | AC-03 | EP-0015-US-0005-TK-0003-world-guard, EP-0015-US-0005-TK-0006-session-tests | PASS: Engine/connection all phases + queued save; Client F9 and queued load blocked in US-0007 TK-0004 |
| EP-0015-US-0005-session-contracts | AC-04 | EP-0015-US-0005-TK-0004-connection-api | PASS: default API, local forwarding/concurrency/cancellation; 284 Engine + 283 Contracts |
| EP-0015-US-0005-session-contracts | AC-05 | EP-0015-US-0005-TK-0005-client-session-handle | PASS: forwarding, InterfaceLog, typed failures and both locales; 33 Client cases |
| EP-0015-US-0005-session-contracts | AC-06 | EP-0015-US-0005-TK-0006-session-tests | PASS: 90 independent rows x 2 connection modes; all378 Engine cases |
| EP-0015-US-0006-client-input-and-controls | AC-01 | EP-0015-US-0006-TK-0001-keyboard | Keyboard boundary PASS: 68 Client cases; actual consumers verified in US-0008; physical input pending |
| EP-0015-US-0006-client-input-and-controls | AC-02 | EP-0015-US-0006-TK-0002-input-settings | PASS: strict schema/conflicts/fallback/native tracker wiring; 103 Client cases |
| EP-0015-US-0006-client-input-and-controls | AC-03 | EP-0015-US-0006-TK-0003-mouse | Mouse boundary PASS: 121 Client cases; actual battle drag/pan PASS in US-0008; physical acceptance pending |
| EP-0015-US-0006-client-input-and-controls | AC-04 | EP-0015-US-0006-TK-0004-widgets-basic, EP-0015-US-0006-TK-0005-widgets-lists | Basic controls PASS (79 cases + 4 raster scales); lists/filter/tooltip button PASS (108 cases + 4 raster scales); full-height panel and movement controls integrated in US-0008; physical acceptance pending |
| EP-0015-US-0006-client-input-and-controls | AC-05 | EP-0015-US-0006-TK-0006-text-and-numbers, EP-0015-US-0006-TK-0007-linux-native-runtime | PASS: Windows113 Client + Linux97 Tactical Client, real glyph IDs/raster and invariant ToEven |
| EP-0015-US-0007-launch-and-setup | AC-01 | EP-0015-US-0007-TK-0001-modal-navigation | PASS: real headless modal pause/start/new/exit/opacity/camera; rich views and physical acceptance pending |
| EP-0015-US-0007-launch-and-setup | AC-02 | EP-0015-US-0007-TK-0002-battle-key-and-hint | PASS: F8/remap/click/hint + unavailable reason/no Start/log; final106 focused |
| EP-0015-US-0007-launch-and-setup | AC-03 | EP-0015-US-0007-TK-0003-setup-screen | PASS: real options/start, exact IDs/seed, keyboard/scroll and four raster layouts |
| EP-0015-US-0007-launch-and-setup | AC-04 | EP-0015-US-0007-TK-0004-battle-keys-guard | PASS: window polls all keys, blocks queued world events, F10 PNG and Escape confirmation |
| EP-0015-US-0008-battle-map | AC-01 | EP-0015-US-0008-TK-0001-battle-screen-and-camera | PASS: immutable frame, fit/resize, cursor zoom, real headless right pan and 0 |
| EP-0015-US-0008-battle-map | AC-02 | EP-0015-US-0008-TK-0002-panel-header | PASS: phase/loss/seed, outcome and actual refusal; pre/post-salvo snapshots |
| EP-0015-US-0008-battle-map | AC-03 | EP-0015-US-0008-TK-0003-battle-map-render | PASS: snapshot renderer, colors/states/ruler/grid and real toggles; actual selection ring and AI-all tooltip wired in TK-0004 |
| EP-0015-US-0008-battle-map | AC-04 | EP-0015-US-0008-TK-0004-movement-orders | PASS: AI proposals, stable 13×61 aim, actual sliders/keys/drag, edited rows only and actual Engine paths |
| EP-0015-US-0008-battle-map | AC-05 | EP-0015-US-0008-TK-0005-movement-animation | PASS: real delayed response, immutable path interpolation, 899/900 ms gate, queued dispatch guard and terminal UI branch |
| EP-0015-US-0009-fire-and-results | AC-01 | EP-0015-US-0009-TK-0001-fire-planning | PASS: actual defaults/candidate details, map toggle/dropdown/none and exact selected Engine shot |
| EP-0015-US-0009-fire-and-results | AC-02 | EP-0015-US-0009-TK-0002-results-phase | PASS: actual shot playback, pre/post values including sides and log, skip/replay, no duplicate commands, no-shot and cap branches |
| EP-0015-US-0009-fire-and-results | AC-03 | EP-0015-US-0009-TK-0005-journal-contract, EP-0015-US-0009-TK-0006-journal-events, EP-0015-US-0009-TK-0003-sides-and-log-tabs, EP-0015-US-0009-TK-0004-forecast-tab | sides and typed log PASS; Forecast tab pending |
| EP-0015-US-0010-battle-end | AC-01 | EP-0015-US-0010-TK-0001-battle-end-ui | not started |
| EP-0015-US-0010-battle-end | AC-02 | EP-0015-US-0010-TK-0002-sandbox-flow | not started |
| EP-0015-US-0011-legacy-removal-prep | AC-01 | EP-0015-US-0011-TK-0001-removal-inventory | not started |
| EP-0015-US-0011-legacy-removal-prep | AC-02 | EP-0015-US-0011-TK-0002-save-fixtures | not started |
| EP-0015-US-0011-legacy-removal-prep | AC-03 | EP-0015-US-0011-TK-0003-maneuver-input-move | not started |
| EP-0015-US-0011-legacy-removal-prep | AC-04 | EP-0015-US-0011-TK-0004-save-format-20 | not started |
| EP-0015-US-0012-legacy-client-removal | AC-01 | EP-0015-US-0012-TK-0001-client-map-removal-1, EP-0015-US-0012-TK-0002-client-map-removal-2 | not started |
| EP-0015-US-0012-legacy-client-removal | AC-02 | EP-0015-US-0012-TK-0003-client-panels-removal, EP-0015-US-0012-TK-0004-client-stores-removal | not started |
| EP-0015-US-0012-legacy-client-removal | AC-03 | EP-0015-US-0012-TK-0005-client-shell-removal, EP-0015-US-0012-TK-0006-client-assets-locale-removal | not started |
| EP-0015-US-0012-legacy-client-removal | AC-04 | EP-0015-US-0012-TK-0006-client-assets-locale-removal | not started |
| EP-0015-US-0012-legacy-client-removal | AC-05 | EP-0015-US-0012-TK-0001-client-map-removal-1, EP-0015-US-0012-TK-0002-client-map-removal-2, EP-0015-US-0012-TK-0003-client-panels-removal, EP-0015-US-0012-TK-0004-client-stores-removal, EP-0015-US-0012-TK-0005-client-shell-removal, EP-0015-US-0012-TK-0006-client-assets-locale-removal | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-01 | EP-0015-US-0013-TK-0001-local-client-removal | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-02 | EP-0015-US-0013-TK-0002-engine-removal-1, EP-0015-US-0013-TK-0003-engine-removal-2, EP-0015-US-0013-TK-0004-engine-removal-3, EP-0015-US-0013-TK-0005-engine-removal-4 | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-03 | EP-0015-US-0013-TK-0007-engine-content-removal | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-04 | EP-0015-US-0013-TK-0008-engine-noncombat-cleanup | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-05 | EP-0015-US-0013-TK-0009-debug-tuning-cleanup | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-06 | EP-0015-US-0013-TK-0010-legacy-save-dto-removal | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-07 | EP-0015-US-0013-TK-0001-local-client-removal, EP-0015-US-0013-TK-0002-engine-removal-1, EP-0015-US-0013-TK-0003-engine-removal-2, EP-0015-US-0013-TK-0004-engine-removal-3, EP-0015-US-0013-TK-0005-engine-removal-4, EP-0015-US-0013-TK-0006-scenario-module-cleanup, EP-0015-US-0013-TK-0007-engine-content-removal, EP-0015-US-0013-TK-0008-engine-noncombat-cleanup, EP-0015-US-0013-TK-0009-debug-tuning-cleanup, EP-0015-US-0013-TK-0010-legacy-save-dto-removal | not started |
| EP-0015-US-0013-legacy-engine-removal | AC-08 | EP-0015-US-0013-TK-0006-scenario-module-cleanup | not started |
| EP-0015-US-0014-legacy-foundation-removal | AC-01 | EP-0015-US-0014-TK-0001-motion-removal | not started |
| EP-0015-US-0014-legacy-foundation-removal | AC-02 | EP-0015-US-0014-TK-0002-contracts-dto-removal, EP-0015-US-0014-TK-0003-contracts-fields-removal | not started |
| EP-0015-US-0014-legacy-foundation-removal | AC-03 | EP-0015-US-0014-TK-0004-content-cleanup | not started |
| EP-0015-US-0014-legacy-foundation-removal | AC-04 | EP-0015-US-0014-TK-0005-tools-cleanup | not started |
| EP-0015-US-0014-legacy-foundation-removal | AC-05 | EP-0015-US-0014-TK-0001-motion-removal, EP-0015-US-0014-TK-0002-contracts-dto-removal, EP-0015-US-0014-TK-0003-contracts-fields-removal, EP-0015-US-0014-TK-0004-content-cleanup, EP-0015-US-0014-TK-0005-tools-cleanup | not started |
| EP-0015-US-0015-localization | AC-01 | EP-0015-US-0015-TK-0001-locale-coverage | not started |
| EP-0015-US-0016-balance-and-native-acceptance | AC-01 | EP-0015-US-0016-TK-0001-series-parity | not started |
| EP-0015-US-0016-balance-and-native-acceptance | AC-02 | EP-0015-US-0016-TK-0002-performance | not started |
| EP-0015-US-0016-balance-and-native-acceptance | AC-03 | EP-0015-US-0016-TK-0003-native-acceptance | not started |
| EP-0015-US-0017-project-documentation | AC-01 | EP-0015-US-0017-TK-0001-impact-inventory | not started |
| EP-0015-US-0017-project-documentation | AC-02 | EP-0015-US-0017-TK-0002-documentation-new-contract | not started |
| EP-0015-US-0017-project-documentation | AC-03 | EP-0015-US-0017-TK-0003-documentation-legacy-rewrite | not started |
| EP-0015-US-0017-project-documentation | AC-04 | EP-0015-US-0017-TK-0004-board-sync | not started |
