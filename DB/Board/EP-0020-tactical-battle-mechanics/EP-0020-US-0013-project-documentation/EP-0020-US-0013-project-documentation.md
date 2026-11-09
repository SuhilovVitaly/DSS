---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0013-project-documentation
title: Документация механик
stage: done
dependencies: [EP-0020-US-0003-evasion-calibration, EP-0020-US-0004-evasion-ui, EP-0020-US-0006-decoy-resolution, EP-0020-US-0007-decoy-ui, EP-0020-US-0009-anomaly-resolution, EP-0020-US-0010-anomaly-ui, EP-0020-US-0012-build-card-ui]
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

[EP-0020-US-0003-evasion-calibration](../EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-evasion-calibration.md), [EP-0020-US-0004-evasion-ui](../EP-0020-US-0004-evasion-ui/EP-0020-US-0004-evasion-ui.md), [EP-0020-US-0006-decoy-resolution](../EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-decoy-resolution.md), [EP-0020-US-0007-decoy-ui](../EP-0020-US-0007-decoy-ui/EP-0020-US-0007-decoy-ui.md), [EP-0020-US-0009-anomaly-resolution](../EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-anomaly-resolution.md), [EP-0020-US-0010-anomaly-ui](../EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-anomaly-ui.md), [EP-0020-US-0012-build-card-ui](../EP-0020-US-0012-build-card-ui/EP-0020-US-0012-build-card-ui.md)

## Completion evidence

- Документы описывают сознание, уклонение, правило ИИ, открытие фазы и итоговый штраф.
- Документы описывают модуль ловушек, пуск, эффект, правило ИИ и раскрытие.
- Документы описывают аномалии: генерацию, поглощение, пыль, ИИ и вид.
- Документы описывают слоты сборки и карточку «Пилот и сборка».
- ExecutionStatus эпика записан.

## Acceptance criteria

- **US13-AC1.** EngineRequirements и TacticalBattle.md описывают правила D-02–D-10 с итоговыми числами из данных; Screens/TacticalBattle.md — выбор уклонения и клавишу E.
- **US13-AC2.** `ExecutionStatus.md` эпика содержит тикеты, коммиты, проверки, итог калибровки уклонения и отчёт о ловушках.
- **US13-AC3.** EngineRequirements и TacticalBattle.md описывают модуль ловушек, слот `moduleSlots`, правила D-15–D-21 с числами из данных; Screens/TacticalBattle.md — пуск ловушки и клавишу D.
- **US13-AC4.** EngineRequirements и TacticalBattle.md описывают аномалии: правила D-24–D-33, блок `anomalies`, генерацию, поглощение, пыль, ИИ; Screens/TacticalBattle.md — вид аномалий и правила пересечения.
- **US13-AC5.** TacticalBattle.md описывает `Slots` снимка; Screens/TacticalBattle.md — карточку «Пилот и сборка» (D-34–D-42).

## Non-goals

Документация будущих механик (добавляется в эту же историю при их планировании).

## Invariants

Решения пользователя цитируются из Documentation.md, не перефразируются как новые.

## Решения и согласование

Исполнение всего эпика разрешено текущим поручением пользователя.

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0013-TK-0001-mechanics-documentation](EP-0020-US-0013-TK-0001-mechanics-documentation/EP-0020-US-0013-TK-0001-mechanics-documentation.md) | Документация уклонения и сознания | documentation | `EP-0020-US-0004-TK-0004-view-regression` | US13-AC1, US13-AC2 |
| [EP-0020-US-0013-TK-0002-decoy-documentation](EP-0020-US-0013-TK-0002-decoy-documentation/EP-0020-US-0013-TK-0002-decoy-documentation.md) | Документация ловушек | documentation | `EP-0020-US-0013-TK-0001-mechanics-documentation`, `EP-0020-US-0006-TK-0005-decoy-regression`, `EP-0020-US-0007-TK-0004-decoy-view-regression` | US13-AC2, US13-AC3 |
| [EP-0020-US-0013-TK-0003-anomaly-documentation](EP-0020-US-0013-TK-0003-anomaly-documentation/EP-0020-US-0013-TK-0003-anomaly-documentation.md) | Документация аномалий | documentation | `EP-0020-US-0013-TK-0002-decoy-documentation`, `EP-0020-US-0009-TK-0005-anomaly-regression`, `EP-0020-US-0010-TK-0004-anomaly-view-regression` | US13-AC2, US13-AC4 |
| [EP-0020-US-0013-TK-0004-build-card-documentation](EP-0020-US-0013-TK-0004-build-card-documentation/EP-0020-US-0013-TK-0004-build-card-documentation.md) | Документация слотов и карточки сборки | documentation | `EP-0020-US-0013-TK-0003-anomaly-documentation`, `EP-0020-US-0012-TK-0003-build-card-view-regression` | US13-AC2, US13-AC5 |

## Полнота завершающей истории

По EpicExecutionPrompt §6 история выполняется после всех функциональных историй. TK-0001…TK-0004 также охватывают инвентаризацию влияния итогового diff во всём репозитории, актуализацию README и документов рядом с кодом/тестами/content/tools, совместимость Save20, сохранение исторической точности, проверку ссылок/ID/coverage, ticket-to-SHA и offline Graphify rebuild после итогового кода и текстов. Scope документационных тикетов расширяется на выявленные релевантные документы с записью причины. Инвентаризация ведётся в DocumentationImpact.md этого эпика.

## Исполнение

US13-AC1…5: текущие механики/экраны/снимки и итоговые численные параметры описаны; полная инвентаризация — DocumentationImpact, результаты/калибровка/отчёты и ticket-to-SHA — ExecutionStatus, ограничения — FinalValidation. Исторические исходы не переписываются. Save20 не менялся. Отдельный финальный graph receipt фиксируется TK4/ExecutionStatus. Документация выполнена после 40 функциональных тикетов и общих automated/native проверок.
