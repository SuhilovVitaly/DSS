---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0005-project-documentation
title: Документация механик
stage: draft
dependencies: [EP-0020-US-0003-evasion-calibration, EP-0020-US-0004-evasion-ui]
created: 2026-11-08T11:46:28Z
source_request: "Отразить механики эпика в канонической документации и Board."
current_review: epic
revision: 1
---

# Документация механик

## Входное техническое задание

Документы: `DB/Documentation/01-Requirements/EngineRequirements.md` (раздел «Тактический бой»), `04-Engineering/TacticalBattle.md`, `02-FirstRelease/Screens/TacticalBattle.md`, `VData/Planning/V4.0/Data/README.md`.

## User story

Как участник проекта, я хочу, чтобы требования, инженерные документы и экраны описывали действующие правила боя.

## Dependencies

[EP-0020-US-0003-evasion-calibration](../EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-evasion-calibration.md), [EP-0020-US-0004-evasion-ui](../EP-0020-US-0004-evasion-ui/EP-0020-US-0004-evasion-ui.md)

## Completion evidence

- Документы описывают сознание, уклонение, правило ИИ, открытие фазы и итоговый штраф.
- ExecutionStatus эпика записан.

## Acceptance criteria

- **US5-AC1.** EngineRequirements и TacticalBattle.md описывают правила D-02–D-10 с итоговыми числами из данных; Screens/TacticalBattle.md — выбор уклонения и клавишу E.
- **US5-AC2.** `ExecutionStatus.md` эпика содержит тикеты, коммиты, проверки и итог калибровки.

## Non-goals

Документация будущих механик (добавляется в эту же историю при их планировании).

## Invariants

Решения пользователя цитируются из Documentation.md, не перефразируются как новые.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0005-TK-0001-mechanics-documentation](EP-0020-US-0005-TK-0001-mechanics-documentation/EP-0020-US-0005-TK-0001-mechanics-documentation.md) | Документация уклонения и сознания | documentation | `EP-0020-US-0004-TK-0004-view-regression` | US5-AC1, US5-AC2 |
