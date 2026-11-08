---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0007-project-documentation
title: Документация проекта
stage: draft
dependencies: [EP-0019-US-0001-reference-and-golden-harness, EP-0019-US-0002-overlay-layout-shell, EP-0019-US-0003-pilot-cards, EP-0019-US-0004-phase-panel, EP-0019-US-0005-map-presentation, EP-0019-US-0006-hover-and-acceptance]
created: 2026-11-08T09:31:05Z
source_request: "Завершающая история по EpicExecutionPrompt: актуализировать документацию экрана, контракта боя и скинов, Board и граф."
current_review: epic
revision: 1
---

# Документация проекта

## Входное техническое задание

[EpicExecutionPrompt.md §6](../../EpicExecutionPrompt.md), [DocumentationSystem.md](../../../Documentation/00-Process/DocumentationSystem.md).

## User story

Как участник проекта, я хочу, чтобы документация описывала действующий интерфейс боя, а не прежний.

## Dependencies

[EP-0019-US-0001-reference-and-golden-harness](../EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-reference-and-golden-harness.md), [EP-0019-US-0002-overlay-layout-shell](../EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-overlay-layout-shell.md), [EP-0019-US-0003-pilot-cards](../EP-0019-US-0003-pilot-cards/EP-0019-US-0003-pilot-cards.md), [EP-0019-US-0004-phase-panel](../EP-0019-US-0004-phase-panel/EP-0019-US-0004-phase-panel.md), [EP-0019-US-0005-map-presentation](../EP-0019-US-0005-map-presentation/EP-0019-US-0005-map-presentation.md), [EP-0019-US-0006-hover-and-acceptance](../EP-0019-US-0006-hover-and-acceptance/EP-0019-US-0006-hover-and-acceptance.md)

## Completion evidence

- Документы экрана, контракта и скинов описывают новый интерфейс.
- Board EP-0019 содержит результаты, SHA и инвентаризацию.

## Acceptance criteria

- **US7-AC1.** `02-FirstRelease/Screens/TacticalBattle.md`, `04-Engineering/TacticalBattle.md`, `04-Engineering/TacticalBattleSkins.md` описывают новый интерфейс, шрифт, портреты, проверки вида и роли `ui.*`.
- **US7-AC2.** Ссылки проверены; в `Documentation` нет скриншотов и отчётов.
- **US7-AC3.** `ExecutionStatus.md` эпика связывает критерии с проверками и SHA.

## Non-goals

Изменение кода.

## Invariants

Документация описывает только действующее поведение.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы-нужно-решение-до-утверждения)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0007-TK-0001-update-contracts-and-docs](EP-0019-US-0007-TK-0001-update-contracts-and-docs/EP-0019-US-0007-TK-0001-update-contracts-and-docs.md) | Итоговая документация | documentation | `EP-0019-US-0006-TK-0002-view-acceptance` | US7-AC1, US7-AC2, US7-AC3 |
