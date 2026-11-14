---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0005-reliable-first-battle
title: Надёжный первый бой
stage: approved
dependencies: [EP-0028-US-0001-hint-framework]
created: 2026-11-14T15:04:43Z
source_request: "US-0005 Надёжный первый бой: winnability check/seed; forced Tabletop skin for tutorial."
current_review: complete
revision: 1
---

# Надёжный первый бой

[Эпик](../Documentation.md) · [Тикеты](../Tickets.md)

## Входное техническое задание

D-03 [эпика](../Documentation.md#решения-пользователя-обязательные): бой обучения всегда в Tabletop. Из дизайна: «прощающий» первый бой — бой обучения (сценарий `initial`, комплектация по умолчанию, seed 1) должен выигрываться и при игре по подсказкам, и чистым Enter; если нет — выбрать другой seed (навык gameplay-balance-check).

## User story

Как новый игрок, я всегда вижу бой обучения в оформлении Tabletop, под которое написаны подсказки, независимо от выбранного в Настройках скина; мой выбор скина для обычных боёв не меняется. Первый бой прощает ошибки: и если я следую подсказкам, и если просто жму Enter, «Скиталец» побеждает, поэтому первое впечатление — успех, а не разочарование.

## Acceptance criteria

- **AC-01.** Бой обучения создаётся со скином `tabletop` при наличии пакета; `gameSettings.tacticalBattleSkinId` не меняется; бой F8 использует предпочтение; без пакета — предпочтительный скин и запись в InterfaceLog (A-07).
- **AC-02.** Детерминированный тест: бой обучения на зафиксированном seed выигрывается стороной игрока и при «чистом Enter», и при политике «по подсказкам»; seed зафиксирован в коде и в тестах.

## Non-goals

Изменение баланса, правил или данных `scenarios.json`; изменение Engine (D-07).

## Invariants

- Исход боя — функция Engine от seed и приказов; скин на исход не влияет.
- Seed передаётся только через `StartTacticalBattleRequest` клиента (`SkiaWindow.cs:1107`).

## Dependencies

US-0001 (сессия обучения в `CreateTacticalBattleView`).

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0028-US-0005-TK-0001-forced-tabletop/EP-0028-US-0005-TK-0001-forced-tabletop.md) | Tabletop для боя обучения | client | US-0001-TK-0004 | AC-01 |
| [TK-0002](EP-0028-US-0005-TK-0002-winnable-battle/EP-0028-US-0005-TK-0002-winnable-battle.md) | Выигрываемый первый бой | client | TK-0001 | AC-02 |

Карта принята автоматическим workflow.

## Gaps and backlog

G-04 эпика закрывается TK-0002. Если ни один seed в диапазоне поиска не даёт победу обеим политикам — блокер для пользователя (изменение баланса вне объёма).

## Completion evidence

`TutorialFlowTests` с тестом скина и `TutorialBattleWinnabilityTests` зелёные; seed записан в «Журнал» TK-0002.

## Журнал

2026-11-14T15:04:43Z — история создана по утверждённому плану.
