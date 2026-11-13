---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0009-view-regression
title: Эталонные снимки нового поля и неизменность Classic
stage: draft
dependencies: [EP-0024-US-0004, EP-0024-US-0005, EP-0024-US-0006, EP-0024-US-0007, EP-0024-US-0008, EP-0024-US-0011, EP-0024-US-0013]
created: 2026-11-12T13:00:00Z
source_request: "Завести эталонные снимки скина `tabletop` и проверить, что 15 снимков Classic не изменились."
current_review: epic
revision: 1
---

# Эталонные снимки нового поля и неизменность Classic

## Входное техническое задание

A-13. Снимки — `View/Ep0019Snapshots.cs` (`DSS_EP0019_ACCEPT_SNAPSHOTS=1`), сцены — `View/Ep0019Scenes.cs`, сцены скинов — `View/Ep0022SkinScenesTests.cs`. Эталоны для сверки на глаз — `DB/Board/EP-0024-battle-field-redesign/Materials/Reference`.

## User story

Как разработчик, я хочу ловить случайные изменения нового поля и не задевать Classic.

## Dependencies

[EP-0024-US-0004](../EP-0024-US-0004-painted-clouds/EP-0024-US-0004-painted-clouds.md), [EP-0024-US-0005](../EP-0024-US-0005-move-templates/EP-0024-US-0005-move-templates.md), [EP-0024-US-0006](../EP-0024-US-0006-fire-templates/EP-0024-US-0006-fire-templates.md), [EP-0024-US-0007](../EP-0024-US-0007-salvo-results/EP-0024-US-0007-salvo-results.md), [EP-0024-US-0008](../EP-0024-US-0008-camera/EP-0024-US-0008-camera.md)

## Completion evidence

- Снимки `tabletop` приняты после просмотра; Classic не изменён.

## Acceptance criteria

- **US9-AC1.** Каталог `View/Snapshots/tabletop` содержит снимки 1920×1080 сцен `1-move-no-hover`, `1-anomaly-move-dust`, `2-fire-hover-own`, `2-anomaly-absorption`, `3-salvo-flight`, `4-salvo-results`, `4-decoy-results`, `5-outcome`; принимаются переменной `DSS_EP0024_ACCEPT_SNAPSHOTS=1` только после просмотра каждого PNG и сверки с `Materials/Reference/target` (различия, перечисленные в Reference/README, допустимы); список и причина записаны в ExecutionStatus.
- **US9-AC2.** 15 снимков Classic (`Category=Ep0019View|Ep0020View`) зелёные без пересоздания; `Ep0022SkinScenesTests` включает `tabletop`; геометрия окон справа и тексты совпадают с Classic, а колонка пилотов и панель команд в `tabletop` проверяются пробами UiSpec §11.13 (D-11).
- **US9-AC3.** Снимки `tabletop` показывают узкую колонку, панель команд и стальные окна; сверены с `Materials/Reference/panels` (три экрана и вырезки ×2); пробы UiSpec §11.13 зелёные.

## Non-goals

Изменение сцен и боёв.

## Invariants

Боевые данные сцен не меняются.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Эпик утверждён пользователем 2026-11-12 ([review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0009-TK-0001-snapshots](EP-0024-US-0009-TK-0001-snapshots/EP-0024-US-0009-TK-0001-snapshots.md) | Снимки `tabletop` и проверка Classic | client-tests | `EP-0024-US-0007-TK-0002-torpedo-fx`, `EP-0024-US-0007-TK-0001-salvo`, `EP-0024-US-0008-TK-0001-camera`, `EP-0024-US-0004-TK-0002-clouds-cache`, `EP-0024-US-0005-TK-0001-templates` | US9-AC1, US9-AC2 |
| [EP-0024-US-0009-TK-0002-panel-snapshots](EP-0024-US-0009-TK-0002-panel-snapshots/EP-0024-US-0009-TK-0002-panel-snapshots.md) | Снимки и пробы панелей `tabletop` | client-tests | `EP-0024-US-0009-TK-0001-snapshots`, `EP-0024-US-0011-TK-0003-role-colours`, `EP-0024-US-0013-TK-0002-panel-move`, `EP-0024-US-0013-TK-0003-panel-fire`, `EP-0024-US-0013-TK-0004-panel-salvo` | US9-AC3 |
