---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0011-steel-panels
title: Стальная тема панелей
stage: done
dependencies: [EP-0024-US-0001]
created: 2026-11-12T17:00:00Z
source_request: "В скине tabletop окна и кнопки боя — тёмная воронёная сталь с тёмными кромками, как на эталоне Reference/panels."
current_review: epic
revision: 1
---

# Стальная тема панелей

## Входное техническое задание

D-11, A-17. Роли панелей читают `BattleDesign.Round/PanelBox/Button/Tag/BoxText/Bar` (`BattleDesign.cs:96-169`), блоки карточки (`PilotCardFireBlock.cs:80-149`, `PilotCardMoveBlock.cs:36`), `BuildCard.cs:34,67`, `HoverCard.cs:87-116`, `PhasePanel.Salvo.cs:56,84`, `PhasePanel.Outcome.cs:16`. Роли и картинки — в `Materials/Assets/skin` (US-0001 копирует пакет). Эталон — `Materials/Reference/panels/*-windows-x2.png`.

## User story

Как игрок, я хочу, чтобы в скине «Tabletop» окна боя были в той же тёмной стали, что колонка пилотов, с тёмными кромками, чтобы экран смотрелся одним целым.

## Dependencies

[EP-0024-US-0001](../EP-0024-US-0001-skin-pack/EP-0024-US-0001-skin-pack.md)

## Completion evidence

- Окно фазы, «Пилот и сборка», итог выстрела, «Залп завершён», карточки наведения и итог боя — в стали по UiSpec §11.2–§11.3, §11.10.

## Acceptance criteria

- **US11-AC1.** Пакет `tabletop` содержит роли и картинки UiSpec §11.2, загружается без предупреждений, укладывается в лимиты; контракт скинов описывает новые роли.
- **US11-AC2.** В `tabletop` плиты, кнопки, ярлыки и поля — с прямыми углами, тёмной рамкой, тёмным скосом и тенью по §11.3; светлых рамок нет (кроме выбора и главной кнопки); в Classic, Contrast и Xenon Star вид и `Layout.Rects` прежние.
- **US11-AC3.** В `tabletop` полосы корпуса и брони в «Пилот и сборка», наведении и итоге выстрела — `#5C9440` и `#5A7CA8`, имя пилота — Ink; в Classic — прежние Cyan и Purple.

## Non-goals

Геометрия и состав правых окон. Колонка пилотов (US-0012, US-0013).

## Invariants

Тексты, команды и `Layout.Rects` окон прежние.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Панели добавлены по прямому запросу пользователя (D-11, [review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0011-TK-0001-steel-roles](EP-0024-US-0011-TK-0001-steel-roles/EP-0024-US-0011-TK-0001-steel-roles.md) | Роли и текстуры стальной темы в пакете | client-data | `EP-0024-US-0001-TK-0001-pack-files` | US11-AC1 |
| [EP-0024-US-0011-TK-0002-steel-frames](EP-0024-US-0011-TK-0002-steel-frames/EP-0024-US-0011-TK-0002-steel-frames.md) | Прямые углы, тёмный скос и тень плит | client-ui | `EP-0024-US-0011-TK-0001-steel-roles`, `EP-0024-US-0001-TK-0002-tabletop-mode` | US11-AC2 |
| [EP-0024-US-0011-TK-0003-role-colours](EP-0024-US-0011-TK-0003-role-colours/EP-0024-US-0011-TK-0003-role-colours.md) | Цвета полос и имени в правых окнах из ролей | client-ui | `EP-0024-US-0011-TK-0001-steel-roles` | US11-AC3 |


## Итог исполнения

DONE по автоматическим проверкам и просмотренным кадрам. Критерии и границы приёмки — [AcceptanceCoverage](../AcceptanceCoverage.md), проверки и SHA — [ExecutionStatus](../ExecutionStatus.md). Физический ввод и owner acceptance отдельно, не заменены автоматизацией.
