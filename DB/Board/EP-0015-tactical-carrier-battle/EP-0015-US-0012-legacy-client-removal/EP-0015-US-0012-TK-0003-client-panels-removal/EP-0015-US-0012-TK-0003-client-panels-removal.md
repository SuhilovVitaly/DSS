---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0003-client-panels-removal
title: client-panels-removal
stage: done
layer: client
depends_on: ["EP-0015-US-0012-TK-0002-client-map-removal-2"]
files_touched: 41
serves: [AC-02, AC-05]
created: 2026-11-05
revision: 3
---

# client-panels-removal

## Why
Панели без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/{CombatJournalPanel,CombatReportPanel,CombatReportPanelLayout,CombatSummaryPanel,ThreatPanel}.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/{CommandsPanel,ModuleDiagram,ObjectInfoPanel,DebugTuningPanel}.cs` | боевые части | Убрать по инвентаризации |

## Public API after the change
—

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Remove the five retired panels, their weapon-row record and the report-only column reservation. Preserve four ordinary command groups, command icons, scanner, ship info, engine limits, ordinary module structure and viewport input. The remaining Defeat shell is removed in TK-0005; carry only its existing stats rendering into that class to keep this cut compilable. Launcher settings used only by the old map labels are detached here and deleted in TK-0005. Debug tuning presentation was already cut in TK-0002.

Delete:
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanelLayout.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatSummaryPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ThreatPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionRightColumnLayout.cs

Edit direct consumers:
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.EnginePanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs
- src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs
- src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs
- tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs

Test dependencies will be enumerated before updating test files.
## Test scope correction before edits

Retire suites dedicated to the removed reports, journal, weapon panel or hostile projection:
- tests/DeepSpaceSaga.Client.Tests/CombatJournalPanelTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportPanelInputTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportPanelRenderTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportRightColumnTests.cs
- tests/DeepSpaceSaga.Client.Tests/HostileSelectedInfoTests.cs
- tests/DeepSpaceSaga.Client.Tests/PhaseThreeJournalUiTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0005Tests.cs
- tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs

In mixed files remove only these methods for obsolete behavior:
- `CombatVisualSettingsTests.cs`: `Rendering_does_not_read_palette_from_disk`
- `DualDefensePanelLayoutTests.cs`: `Two_defense_panels_have_distinct_owner_module_keys_and_command_hooks`, `Collapsing_one_preserves_other_and_identity_survives_module_enumeration_change`, `Small_viewport_keeps_both_panels_reachable`, `Very_short_viewport_scrolls_opened_panel_and_rejects_clicks_beyond_viewport`, `Click_rechecks_the_specific_module_without_reusing_cached_enablement`
- `ModulePanelTimeTests.cs`: `Priority_uses_command_identity_even_when_caption_changes`, `Actual_render_opens_a_weapon_before_navigation_in_small_viewport`, `Reload_tooltip_and_countdown_keep_double_speed_while_paused`
- `PhaseThreeLocaleTests.cs`: `Runtime_status_and_journal_use_locale_keys_without_skill_multiplier`
- `PointCombatUS0013TK0001Tests.cs`: `Own_hit_blinks_once_and_foreign_numeric_hit_is_ignored`, `Hit_indicator_expires_in_real_time_while_disabled_clock_stays_paused`
- `PointCombatUS0013TK0002Tests.cs`: `Weapon_reload_ring_uses_physical_deadline_with_tenth_second_precision`, `Pause_freezes_reload_and_never_optimistically_resets_ring`
- `PointCombatUS0013TK0004Tests.cs`: `Journal_and_reports_render_new_events_without_chance_or_roll`, `Journal_drops_foreign_module_payload_without_own_identity`, `Journal_own_health_is_visible_only_in_matching_identity_and_cache_is_invalidated`, `Actual_report_panel_wraps_public_facts_in_both_locales_and_scales`

Update live geometry, color fallback, long details scrolling, module structure and ordinary commands assertions in `CombatMapVisualTests.cs`, `CompactObjectLabelTests.cs`, `ModulePanelHealthTests.cs`, `PhaseThreeCombatInfoTests.cs`, `PointCombatUS0020TK0002Tests.cs`, `SpaceMapColorResolverTests.cs`. Existing report formatter/store tests remain for TK-0004.

Also use a short viewport for the existing ObjectInfoPanel image/scroll regression so it still exercises clipping after the unused report reservation is removed.
Regression scope: update `CommandsPanelSkeletonTests.cs` for exactly four groups, preserving geometry where no detail data is available; `PiratePresentationTests.cs` verifies ordinary portrait orientation independently of hostile relation; `ModulePanelEngineTests.cs` verifies current power-based unavailability and still checks fresh limits and zero-rate placeholders.

## Execution evidence — 2026-11-06

Removed legacy report/summary/journal/threat panels and weapon widgets; the map has four ordinary command groups, generic module structure, engine and scanner controls, and object information without the old report reservation. Kept the Defeat screen compilable pending its owning shell cut. All 2170 Client cases PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-panels-regression-02`. Solution build (0 warnings/errors), scoped format and diff PASS. Linux tactical workflows 42134978782 and 42135333382 PASS for map and tuning cuts. Native/human and full post-removal regression remain open.
