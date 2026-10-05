# Tactical button assets

Design standard: [Trade-based tactical buttons](../03-Design/TacticalButtons/STYLE.md).

The 43 button PNGs use the trade window's navy / off-white / cyan palette.
Keep filenames and dimensions stable. `.active.png` means hover, not an executing command.
Masters and the contact sheet: `DSS-Images/temp/tactical-buttons-20260913` (beside DSS).
The manifest and export script are documented in the design standard.


## EP-0007 — актуальное дополнение от 2026-10-04

GameSessionScreen.Countermeasures.cs проецирует CountermeasureSnapshot/DefenseSnapshot; CombatJournalPanel отображает CombatJournalEntry. CombatEffectStore отделяет физическое состояние от monotonic срока эффектов и устанавливает watermark при загрузке. CommandsPanel содержит шесть групп, ObjectInfoPanel показывает frozen breakdown. [Технический контракт и приёмка](../04-Engineering/CountermeasureCombat.md).
