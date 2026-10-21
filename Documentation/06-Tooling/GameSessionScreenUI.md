# Tactical button assets

## Current EP-0012 contract — 2026-10-21

Current combat uses the point harness and public own/observed DTOs. Guided/manual-defense example references below belong to earlier UI extraction work. Current native reproduction is documented in tools/DeepSpaceSaga.Performance/README.md and Board EP12 evidence/native.md. Managed screen callbacks/GPU captures do not prove physical OS input or user acceptance. [Point-aim combat contract](../04-Engineering/PointAimCombat.md).

## Актуализация EP-0008 — 2026-10-12

`GameSessionScreen.ManualDefense.cs` связывает preview/Fire с конкретным модулем, целью и pending CommandId; Countermeasures/CombatJournalPanel отображают snapshot/evidence. Панели ПРО создаются по экземплярам (7 общих панелей PlayerShipOnly), footer и MapToolbar учитывают занятые области. CombatEffectStore сохраняет2-секундный real-time срок и watermark после Load. `SkiaWindow` маршрутизирует Defeat/Load; ошибки загрузки recoverable. Native harness существует, но дальнейший запуск окна отложен пользователем.

[Текущий контракт](../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../04-Engineering/PhaseThreeCombatAcceptance.md).


Design standard: [Trade-based tactical buttons](../03-Design/TacticalButtons/STYLE.md).

The 43 button PNGs use the trade window's navy / off-white / cyan palette.
Keep filenames and dimensions stable. `.active.png` means hover, not an executing command.
Masters and the contact sheet: `DSS-Images/temp/tactical-buttons-20260913` (beside DSS).
The manifest and export script are documented in the design standard.


## EP-0007 — актуальное дополнение от 2026-10-04

GameSessionScreen.Countermeasures.cs проецирует CountermeasureSnapshot/DefenseSnapshot; CombatJournalPanel отображает CombatJournalEntry. CombatEffectStore отделяет физическое состояние от monotonic срока эффектов и устанавливает watermark при загрузке. CommandsPanel содержит шесть групп, ObjectInfoPanel показывает frozen breakdown. [Технический контракт и приёмка](../04-Engineering/CountermeasureCombat.md).
