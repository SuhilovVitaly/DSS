---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0012-loot-debug-commands
title: Отладочные команды для плейтеста лута
stage: draft
plan_status: draft
approval_kind: workflow
current_review: plan-review
revision: 1
created: 2026-11-17T19:41:16Z
priority: P1
workstream: tooling
dependencies: [EP-0036-US-0003-simple-loot, EP-0036-US-0006-module-salvage, EP-0036-US-0008-ship-engineer, EP-0036-US-0007-postbattle-loot-lifecycle]
implementation_status: not-started
planning_status: decomposed
source_request: "прочитай нарезанные тикеты эпика и реши что мы забыли и что нужно добавить"
---

# Отладочные команды для плейтеста лута

## Связь с техническим заданием

[Эпик и решение D-36-37](../Documentation.md). Шансы 25–42% и 50%-броски нельзя проверить вручную без десятков боёв, а числа content помечены A для плейтеста. В debug-консоли EP-0030 есть kill_drone и fog_of_war, но нет команд для лута.

## User story

Как тестировщик, я в debug-консоли задаю временный бонус инженера, исходы бросков уничтожения и попыток, смотрю состояние текущего сбора. Обычная игра и сохранения от этого не меняются.

## Grounding

- `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` — единая регистрация команд; `AppDebugOverrides.cs` — образец подтверждения через `SetFogOfWarRequest` (`src/DeepSpaceSaga.Contracts/FogOfWarContracts.cs`).
- `KillDroneDebugCommand.cs` — образец busy/pending и проверки боя.

## Acceptance criteria

- **AC-01:** `engineer_bonus [auto|0|10..15]` показывает или временно переопределяет бонус getter US8. Persisted навык не меняется, override не попадает в save.
- **AC-02:** `loot_force rolls|attempts <mode>` задаёт исход следующих бросков (auto/keep/destroy, auto/success/fail). Результат пишется в ledger обычным путём, повтор и Load его не перебрасывают, уже записанные результаты не меняются.
- **AC-03:** `loot_state` read-only печатает источники, попытки, остатки и свободную массу текущей LootOpen.
- **AC-04:** Команда подтверждается только Applied-ответом Engine. По умолчанию все режимы auto, обычный путь игры не зависит от этих команд.

## Approved ticket map

Planning map, не human approval implementation:

| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0012-TK-0001-loot-debug-overrides](EP-0036-US-0012-TK-0001-loot-debug-overrides/EP-0036-US-0012-TK-0001-loot-debug-overrides.md) | engine | EP-0036-US-0006-TK-0003-engine, EP-0036-US-0003-TK-0003-engine, EP-0036-US-0008-TK-0002-identity-and-skill | AC-01…04 |
| [EP-0036-US-0012-TK-0002-console-commands](EP-0036-US-0012-TK-0002-console-commands/EP-0036-US-0012-TK-0002-console-commands.md) | client | EP-0036-US-0012-TK-0001-loot-debug-overrides, EP-0036-US-0007-TK-0004-postbattle-map | AC-01…04 |

## Non-goals

Выдача предметов/денег командой, фиктивная победа по bounty (для неё есть kill_drone), сохранение debug-настроек.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
