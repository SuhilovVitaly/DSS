# EP-0013 ticket order

Original US5-TK5 docs becomes final US7-TK1. Push immediately after every commit under the updated epic section9; verify the target branch and remote, then confirm the remote branch SHA matches local HEAD.

Seven stories,21 tickets. Current implementation/acceptance evidence:
[ExecutionStatus](ExecutionStatus.md), [coverage](AcceptanceCoverage.md),
[per-ticket publication](PublicationStatus.md). Story/ticket stage denotes
implementation; epic human/physical/audio acceptance is tracked separately.
- [EP-0013-US-0001-TK-0001-contracts](EP-0013-US-0001-contracts-and-state/EP-0013-US-0001-TK-0001-contracts/EP-0013-US-0001-TK-0001-contracts.md) — Contracts
- [EP-0013-US-0001-TK-0002-tuning-state](EP-0013-US-0001-contracts-and-state/EP-0013-US-0001-TK-0002-tuning-state/EP-0013-US-0001-TK-0002-tuning-state.md) — Engine
- [EP-0013-US-0001-TK-0003-apply-command](EP-0013-US-0001-contracts-and-state/EP-0013-US-0001-TK-0003-apply-command/EP-0013-US-0001-TK-0003-apply-command.md) — Engine
- [EP-0013-US-0002-TK-0001-ships-immediate](EP-0013-US-0002-immediate-application/EP-0013-US-0002-TK-0001-ships-immediate/EP-0013-US-0002-TK-0001-ships-immediate.md) — Engine
- [EP-0013-US-0002-TK-0002-projectiles-retime](EP-0013-US-0002-immediate-application/EP-0013-US-0002-TK-0002-projectiles-retime/EP-0013-US-0002-TK-0002-projectiles-retime.md) — Engine
- [EP-0013-US-0002-TK-0003-reload-retime](EP-0013-US-0002-immediate-application/EP-0013-US-0002-TK-0003-reload-retime/EP-0013-US-0002-TK-0003-reload-retime.md) — Engine
- [EP-0013-US-0003-TK-0001-save-field](EP-0013-US-0003-save-and-startup/EP-0013-US-0003-TK-0001-save-field/EP-0013-US-0003-TK-0001-save-field.md) — Engine
- [EP-0013-US-0003-TK-0002-startup-file](EP-0013-US-0003-save-and-startup/EP-0013-US-0003-TK-0002-startup-file/EP-0013-US-0003-TK-0002-startup-file.md) — Engine + LocalClient
- [EP-0013-US-0004-TK-0001-file-writer](EP-0013-US-0004-tuning-files/EP-0013-US-0004-TK-0001-file-writer/EP-0013-US-0004-TK-0001-file-writer.md) — Client
- [EP-0013-US-0004-TK-0002-defaults-file](EP-0013-US-0004-tuning-files/EP-0013-US-0004-TK-0002-defaults-file/EP-0013-US-0004-TK-0002-defaults-file.md) — Client
- [EP-0013-US-0005-TK-0001-panel-layout](EP-0013-US-0005-tactical-panel/EP-0013-US-0005-TK-0001-panel-layout/EP-0013-US-0005-TK-0001-panel-layout.md) — Client
- [EP-0013-US-0005-TK-0002-panel-input](EP-0013-US-0005-tactical-panel/EP-0013-US-0005-TK-0002-panel-input/EP-0013-US-0005-TK-0002-panel-input.md) — Client
- [EP-0013-US-0005-TK-0003-panel-status-reset](EP-0013-US-0005-tactical-panel/EP-0013-US-0005-TK-0003-panel-status-reset/EP-0013-US-0005-TK-0003-panel-status-reset.md) — Client
- [EP-0013-US-0005-TK-0004-localization](EP-0013-US-0005-tactical-panel/EP-0013-US-0005-TK-0004-localization/EP-0013-US-0005-TK-0004-localization.md) — Client data
- [EP-0013-US-0006-TK-0001-speed-hold-contracts](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0001-speed-hold-contracts/EP-0013-US-0006-TK-0001-speed-hold-contracts.md) — Contracts
- [EP-0013-US-0006-TK-0002-hold-motion](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0002-hold-motion/EP-0013-US-0006-TK-0002-hold-motion.md) — Motion
- [EP-0013-US-0006-TK-0003-speed-hold-engine](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0003-speed-hold-engine/EP-0013-US-0006-TK-0003-speed-hold-engine.md) — Engine
- [EP-0013-US-0006-TK-0004-input-schema-v2](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0004-input-schema-v2/EP-0013-US-0006-TK-0004-input-schema-v2.md) — Client
- [EP-0013-US-0006-TK-0005-maneuver-input](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0005-maneuver-input/EP-0013-US-0006-TK-0005-maneuver-input.md) — Client
- [EP-0013-US-0006-TK-0006-hints-localization](EP-0013-US-0006-manual-maneuver/EP-0013-US-0006-TK-0006-hints-localization/EP-0013-US-0006-TK-0006-hints-localization.md) — Client + data
- [EP-0013-US-0007-TK-0001-docs](EP-0013-US-0007-project-documentation/EP-0013-US-0007-TK-0001-docs/EP-0013-US-0007-TK-0001-docs.md) — Documentation
