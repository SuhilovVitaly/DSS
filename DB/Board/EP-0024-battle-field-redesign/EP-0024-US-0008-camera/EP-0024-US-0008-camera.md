---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0008-camera
title: Камера по фазам
stage: draft
dependencies: [EP-0024-US-0001]
created: 2026-11-12T13:00:00Z
source_request: "В режиме кадрировать камеру по фазам: своя группа в движении, бой в стрельбе и залпе."
current_review: epic
revision: 2
---

# Камера по фазам

## Входное техническое задание

D-04, D-08, D-09, A-08, A-11. Сейчас — `TacticalBattleCamera.cs:31-73` (кадр один раз на бой), `TacticalBattleScreen.cs:111` (`_camera.Update`). Прототип — правки камеры в `DB/Board/EP-0024-battle-field-redesign/Materials/Prototype/src/hooks.diff`.

## User story

Как игрок, я хочу, чтобы камера сама показывала то, что важно в фазе: свою группу при планировании движения и схватку при стрельбе и залпе.

## Dependencies

[EP-0024-US-0001](../EP-0024-US-0001-skin-pack/EP-0024-US-0001-skin-pack.md)

## Completion evidence

- Кадрирование по UiSpec §8.

## Acceptance criteria

- **US8-AC1.** В режиме при смене `(Phase, Turn)` без ручного вида камера кадрирует по UiSpec §8.1; во время полёта и результатов залпа кадр не меняется; после ручного перемещения кадрирование выключено до нового боя; в Classic камера прежняя.

## Non-goals

Ручное управление камерой (как сейчас). Окно «Разведка» и любые панели (D-08).

## Invariants

Прежняя формула зума `Fit`; меняются только точки кадра и поле.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Эпик утверждён пользователем 2026-11-12 ([review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0008-TK-0001-camera](EP-0024-US-0008-TK-0001-camera/EP-0024-US-0008-TK-0001-camera.md) | Кадрирование по фазам | client-ui | `EP-0024-US-0001-TK-0002-tabletop-mode` | US8-AC1 |
