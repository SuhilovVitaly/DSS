---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0027-ep24-uispec-drift
title: UiSpec EP-0024 расходится с кодом после исправлений
stage: draft
implementation_status: not_started
severity: minor
layer: docs
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC27]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# UiSpec EP-0024 расходится с кодом после исправлений

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

UiSpec EP-0024 описывает действующее поведение Tabletop или явно отмечает пункты, заменённые исправлениями EP-maintenance, со ссылками на них.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024 UiSpec; EP-maintenance 91dc15bc, 09a7c2ae, 42c8082b, 63db8c1e. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Позднейшие исправления изменили поведение, а UiSpec не обновлён: (1) §7.1 / D-06 — надписи «промах» и «поглощено» в Tabletop отключены (`BattleMapRenderer.Fire.cs:151`, 91dc15bc); (2) §2.3 — при `UnitScale = 0.6` и `Inflate` после привязки к сетке пиксели арта нецелые (09a7c2ae, US-0015-TK-0001), тест `Real_sheet_renders_with_integer_pixels…` проверяет только масштаб 1; (3) §8.2 — перекадрирование при смене фазы убрано (42c8082b); (4) §3.5 / §11.5 — золотые рамки повреждений убраны, в узкой карточке полоски вместо квадратов (91dc15bc, 63db8c1e).

**Сценарий.** Исполнитель, сверяющийся с UiSpec, вернёт удалённое поведение или сочтёт текущее дефектом.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `DB/Board/EP-0024-battle-field-redesign/UiSpec.md` | §2.3, §3.5, §7.1, §8.2, §11.5. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleCamera.cs:33-43` | Масштаб и камера при смене фазы. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Перенести решения исправлений в UiSpec или пометить пункты как заменённые со ссылками на тикеты EP-maintenance. Решить, нужны ли целые пиксели при UnitScale ≠ 1; если да — отдельное исправление.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Ссылки UiSpec проверены, перечисленные пункты совпадают с кодом на текущем main.

## Definition of Done

- AC27 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
