---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0006-skins
title: Карточка с командами во всех скинах боя
stage: done
dependencies: [EP-0023-US-0002, EP-0023-US-0003, EP-0023-US-0004, EP-0023-US-0005]
created: 2026-11-11T14:30:00Z
source_request: "Новые элементы карточки (шкалы, иконки, разделитель, кнопки шага, строки целей, ловушки и уклонения в карточке, индикатор прокрутки) получают роли скина и выглядят в classic, contrast и Xenon Star (D-04)."
current_review: epic
revision: 1
---

# Карточка с командами во всех скинах боя

## Входное техническое задание

D-04. Контракт скинов — `DB/Documentation/04-Engineering/TacticalBattleSkins.md` (раздел «Поверхности активного боя»); паки — `src/DeepSpaceSaga.Client/Skins/contrast/skin.json`, `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` (роли `ui.pilot-card*`, `ui.bar.segments`, `ui.slider.*`, `ui.button*`); чтение ролей — `TacticalBattleSkinRenderScope`.

## User story

Как игрок со своим скином боя, я хочу, чтобы новая карточка выглядела в стиле выбранного скина.

## Dependencies

[EP-0023-US-0002](../EP-0023-US-0002-move-in-card/EP-0023-US-0002-move-in-card.md), [EP-0023-US-0003](../EP-0023-US-0003-fire-in-card/EP-0023-US-0003-fire-in-card.md), [EP-0023-US-0004](../EP-0023-US-0004-salvo-in-card/EP-0023-US-0004-salvo-in-card.md), [EP-0023-US-0005](../EP-0023-US-0005-compact-panel/EP-0023-US-0005-compact-panel.md)

## Completion evidence

- Роли UiSpec §9 читаются кодом карточки с запасными значениями classic.
- contrast и Xenon Star задают эти роли; сцены скинов EP-0022 проходят.

## Acceptance criteria

- **US6-AC1.** Код карточки читает роли UiSpec §9 через `TacticalBattleSkinRenderScope` с запасными значениями classic; без пака (или с удалённым паком) вид совпадает с classic.
- **US6-AC2.** `contrast/skin.json` и `xenon-star/skin.json` задают роли UiSpec §9 (цвета и при наличии — `.art`); сцены `Ep0022SkinScenesTests` для всех трёх скинов проходят с пробами новой карточки; контраст значений над шкалами в Xenon Star не ниже 4,5:1 к фону карточки.
- **US6-AC3.** Контракт скинов и `Skins/README.md` перечисляют новые роли и их запасные значения.

## Non-goals

Новые растровые ресурсы скинов; изменение формата манифеста.

## Invariants

Поведение боя не зависит от скина (`ClassicAndContrastShareIdenticalPlaybackFacts`).

## Решения и согласование

Решения пользователя — [D-01–D-04](../Documentation.md#решения-пользователя), допущения — [A-01–A-13](../Documentation.md#технические-допущения). Исполнение авторизовано запросом пользователя от 2026-11-11. Результаты и оставшиеся проверки указаны в тикетах.

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0023-US-0006-TK-0001-skin-roles](EP-0023-US-0006-TK-0001-skin-roles/EP-0023-US-0006-TK-0001-skin-roles.md) | Роли скина для новой карточки и значения в паках | client-ui | `EP-0023-US-0005-TK-0002-remove-panel-controls` | US6-AC1, US6-AC3 |
| [EP-0023-US-0006-TK-0002-skin-scenes](EP-0023-US-0006-TK-0002-skin-scenes/EP-0023-US-0006-TK-0002-skin-scenes.md) | Сцены скинов с новой карточкой | client-tests | `EP-0023-US-0006-TK-0001-skin-roles` | US6-AC2 |
