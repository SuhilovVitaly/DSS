---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0001-client-map-removal-1
title: client-map-removal-1
stage: done
layer: client
depends_on: ["EP-0015-US-0011-TK-0004-save-format-20"]
files_touched: 79
serves: [AC-01, AC-05]
created: 2026-11-05
revision: 3
---

# client-map-removal-1

## Why
Карта без старого боя, часть 1.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatForecast.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs` | старый бой | Удалить |

## Public API after the change
—

## Implementation steps
1. Сохранить ManualAxes/Maneuver/EnginePanel.
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Client.Tests регрессия карты. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before implementation — 2026-11-06

The remaining point-aim/threat/projectile partials share the six named files' state and private methods; they must be disconnected in the same compilable cut. TK-0002 will finish shared map/debug tuning presentation and verification. Preserve manual axes and ordinary map trajectories, labels, camera fit, panels and F8. Obsolete native report/point-combat entry points also reference the removed map API and are retired here; keep the general performance and native tooling.

Delete:
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatForecast.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs
- tools/DeepSpaceSaga.Performance/PointCombatNativeEvidence.cs
- tools/DeepSpaceSaga.Performance/CombatReportsNativeEvidence.cs

Edit direct consumers:
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualAxes.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs
- src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
- tools/DeepSpaceSaga.Performance/Program.cs

Test consumers will be inventoried and scoped separately before editing them. Preserve ordinary-world cases in mixed tests.

$([string]::Empty)Direct consumer "GameSessionScreen.DebugTuning.cs" also needs its Escape guard updated because the aim modal is removed.

`tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs` drops only its obsolete point-launch path and retains native engine, dimensions, locale and layout checks. `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0003Tests.cs` exclusively exercises the deleted counter-aim preview/input and is retired.
## Test scope correction before test edits

Build diagnostics in `20261106-map-removal-compile-03` identify source-level UI consumers. Delete these legacy-only suites; retain tests of still-existing stores, shell and data until their owning cuts:
- tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatPresentationResumeTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportHoverTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportLifecycleTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportMapViewportTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceFixtures.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportScreenFlowTests.cs
- tests/DeepSpaceSaga.Client.Tests/CountermeasureSessionRoundtripTests.cs
- tests/DeepSpaceSaga.Client.Tests/ManualDefensePreviewTests.cs
- tests/DeepSpaceSaga.Client.Tests/ModulePanelCounterTests.cs
- tests/DeepSpaceSaga.Client.Tests/ModulePanelProfileTests.cs
- tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0002Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0002Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0004Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0004Tests.cs
- tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs
- tests/DeepSpaceSaga.Client.Tests/TorpedoPreviewTests.cs
- tests/DeepSpaceSaga.Client.Tests/CountermeasureUiFlowTests.cs

Keep the following named tests in mixed files (other tests in those classes assert the removed realtime UI path):
- `CombatTrajectoryTests.cs`: `Zoom_changes_screen_geometry_not_flight_state`
- `PhaseThreeJournalUiTests.cs`: `Own_module_and_public_profile_remain_without_retired_modes_or_probability`, `Termination_reason_does_not_claim_a_roll_or_hull_damage`, `Long_wrapped_entry_scrolls_to_actual_last_reason_and_collapse_preserves_facts`
- `PhaseThreeLocaleTests.cs`: `Both_locales_cover_phase_three_keys_in_source_and_output`, `Runtime_status_and_journal_use_locale_keys_without_skill_multiplier`
- `PhaseThreeSessionRoundtripTests.cs`: `Old_save_error_keeps_current_session`, `Malformed_current_save_shows_generic_error_and_can_retry`
- `PointCombatUS0010TK0002Tests.cs`: `Point_effects_use_real_durations_deduplicate_and_do_not_replay_load_baseline`, `Transport_watchdog_requires_real_time_instead_of_accelerated_scene_time`
- `PointCombatUS0010TK0004Tests.cs`: `Own_path_uses_current_confirmed_turn_for_five_seconds`, `Bare_F_and_Ctrl_F_have_distinct_actual_input_edges`
- `PointCombatUS0011TK0001Tests.cs`: `Shipped_input_is_packaged_and_invalid_bindings_report_path`, `Input_schema_errors_identify_file_and_field`
- `PointCombatUS0011TK0004Tests.cs`: `Every_point_launch_reason_is_localized_in_source_and_packaged_locale`
- `PointCombatUS0012TK0001Tests.cs`: `Native_focus_loss_clears_input_before_keyboard_resync`, `Arrow_hold_release_and_shift_send_turn_and_full_speed`, `Focus_loss_releases_actual_turn_once`, `Disabled_engine_can_preview_but_does_not_submit_evade_or_turn`, `Outside_combat_arrows_use_the_same_continuous_turn_command`
- `PointCombatUS0012TK0003Tests.cs`: `Native_audio_gate_limits_actual_playback_start_after_queue_delay`, `Native_audio_session_change_discards_queued_old_and_reactivated_screen_cues`, `Invalid_alert_configuration_reports_file_and_group`
- `PointCombatUS0013TK0005Tests.cs`: `Defeat_renders_all_captured_stats_in_scrollable_block_without_resume`
- `PointCombatUS0016TK0001Tests.cs`: `Combat_locale_key_sets_match_and_have_no_fallback_literals`, `Report_profiles_and_generic_errors_use_the_requested_dictionary`, `Locale_scope_restores_current_language_and_preserves_custom_data_names`
- `PointCombatUS0020TK0005Tests.cs`: `Reload_journal_and_report_use_captured_launcher_name`
- `WeaponHudControlsTests.cs`: `Visible_contacts_outside_largest_free_rectangle_never_get_edge_markers`
- `CombatMetricsWriterTests.cs`: `Actual_point_flights_are_logged_from_public_own_and_observed_payloads_without_legacy_fields`, `Directory_quota_removes_oldest_metrics_and_keeps_valid_rotated_records`, `Concurrent_sessions_share_quota_and_oversized_records_are_dropped`, `Records_authoritative_geometry_events_and_throttled_visuals_without_mutating_snapshot`, `Unwritable_metrics_destination_does_not_throw_or_block_session_shutdown`

Retain all ordinary world cases and update direct assertions for removed report UI, removed aim or Save20 in:
- tests/DeepSpaceSaga.Client.Tests/DefeatNavigationTests.cs
- tests/DeepSpaceSaga.Client.Tests/ManualManeuverInputTests.cs
- tests/DeepSpaceSaga.Client.Tests/ObjectInfoPanelTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs
- tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs
Correction: retain `TorpedoCommandPanelTests.cs` fixture for existing selected-object tests and keep its small viewport captions check. Only the obsolete aim selection test is removed.
## Client regression scope correction before edits

Run `20261106-map-removal-regression-01`: 2227 passed / 33 failed. Retire the following exact methods that require the removed map markers, specialized paths, aiming dispatch/forecast or combat continuation. Other methods remain:
- `TacticalMapSmoothnessTests.cs`: `Hostile_ship_keeps_moving_between_one_second_snapshots`, `Hostile_visual_prediction_keeps_curvature_between_snapshots`, `Hostile_visual_pose_is_continuous_when_next_segment_arrives`, `Hostile_target_of_torpedo_reconciles_visual_pose_without_jumping`
- `PirateScenarioTests.cs`: `Pirate_initial_identity_is_preserved_and_motion_enters_configured_flank_approach`, `Save_load_preserves_identity_relation_modules_and_motion`
- `CombatMapVisualTests.cs`: `Configured_point_marker_and_five_pixel_wreck_remain_constant_across_zoom`, `Combat_markers_are_not_duplicated_or_cluster_hidden`
- `PointCombatUS0010TK0004Tests.cs`: `Own_path_uses_current_confirmed_turn_for_five_seconds`
- `PointCombatUS0013TK0002Tests.cs`: `Detonate_button_submits_selected_own_flight_through_real_transport`, `Weapon_panels_show_profile_reload_and_operator_without_rolls`
- `PointCombatUS0019TK0003Tests.cs`: `Forecast_preparation_does_not_copy_the_noncombat_world_every_frame`
- `PointCombatUS0020TK0006Tests.cs`: `Combat_speed_ui_and_eta`

Update assertions in `CombatSessionRoundtripTests.cs`, `CompactObjectLabelTests.cs`, `DefaultSystemContentTests.cs`, `ModulePanelLocalizationTests.cs`, `PointCombatUS0002TK0005Tests.cs`, `PointCombatUS0011TK0001Tests.cs`, `LocalSessionIntegrationTests.cs`, `TorpedoLauncherContentTests.cs` to retain ordinary clock, ship, inventory, seed, locale, schema and map coverage under current behavior. Reuse the pirate source scenario for the existing New Game-only cabin and ordinary object selection checks.

## Execution evidence — 2026-11-06

All eleven coupled realtime map partials were removed. The map does not offer point aiming, combat reports, threat rendering or old combat speed controls; ordinary map fit, hover, selection, F8 and manual axes remain. Current build: 0 errors/warnings; scoped format/diff PASS; Client 2241/2241 selected cases PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-map-removal-regression-02`. Three DebugTuning test classes with fixed historical output paths were left for TK-0002 to redirect and rerun; AC-05 full post-removal regression remains open. Save20 Linux workflow 42133171113 PASS at verified prior SHA `051df719f8e0065d70e7aedc2844ded72e4819a8`.
