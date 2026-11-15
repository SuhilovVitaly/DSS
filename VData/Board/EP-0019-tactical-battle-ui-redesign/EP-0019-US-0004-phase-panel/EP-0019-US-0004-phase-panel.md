---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
title: Панель фазы
stage: done
dependencies: [EP-0019-US-0002-overlay-layout-shell]
created: 2026-11-08T09:31:05Z
source_request: "Правая постоянная панель: шапка хода и фазы, детали фазы и действие; управление приказами движения и огня, воспроизведением залпа и итогом."
current_review: epic
revision: 1
---

# Панель фазы

## Входное техническое задание

D-03. [DesignSpec §6, §9](../DesignSpec.md#6-правая-колонка--панель-фазы).

## User story

Как игрок, я хочу в одной панели видеть текущую фазу, детали выбранного дрона и главное действие, чтобы всегда понимать, что сейчас нужно сделать.

## Dependencies

[EP-0019-US-0002-overlay-layout-shell](../EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-overlay-layout-shell.md)

## Completion evidence

- Панель фазы соответствует DesignSpec §6 во всех пяти состояниях.
- Все кнопки и клавиши панели выполняют свои действия на реальном бою.

## Acceptance criteria

- **US4-AC1.** Панель `1040…1274` (справа), высота по фазе (`DB` + 120), шапка, разделители и зоны по DesignSpec §6.1.
- **US4-AC2.** Движение: ползунки скорости и поворота отражают и меняют приказ выбранного дрона в пределах `TacticalLimits`; «Прямо» и «Вернуть ИИ»; Enter отправляет движение.
- **US4-AC3.** Стрельба: шанс, дистанция, угол, оружие, броня цели; «Не стрелять» снимает цель выбранного стрелка; ЛКМ по дрону противника назначает цель из списка; Enter отправляет залп.
- **US4-AC4.** Залп: очередь огня с активной строкой по ходу воспроизведения; Enter пропускает полёт. Результаты: итог выстрела и повреждения; «↺ Повтор» и «К движению [Enter]».
- **US4-AC5.** Итог боя: результат, причина, ходы, выстрелы, попадания, потери; «Новый бой» и «Выйти [Esc]» работают как прежние New battle и Exit.

## Non-goals

Карта (US-0005), карточки (US-0003), наведение (US-0006).

## Invariants

Команды через прежние `BuildMoveRequest`/`BuildFireRequest`/`AdvanceSalvo`/`ReplaySalvo`/`ExitAction`; тайминги анимаций EP-0015/EP-0017.

## Решения и согласование

Исполнение draft-карточек разрешено запросом выполнить весь эпик. Использованы записанные варианты по умолчанию; художественное/пользовательское одобрение не приписывается. Итог — [ExecutionStatus](../ExecutionStatus.md).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0004-TK-0001-phase-panel-container-and-header](EP-0019-US-0004-TK-0001-phase-panel-container-and-header/EP-0019-US-0004-TK-0001-phase-panel-container-and-header.md) | Контейнер панели и шапка | client | `EP-0019-US-0002-TK-0004-localization-strings` | US4-AC1 |
| [EP-0019-US-0004-TK-0002-move-details-and-input](EP-0019-US-0004-TK-0002-move-details-and-input/EP-0019-US-0004-TK-0002-move-details-and-input.md) | Детали движения и ввод | client | `EP-0019-US-0004-TK-0001-phase-panel-container-and-header` | US4-AC2 |
| [EP-0019-US-0004-TK-0003-fire-details-and-target-click](EP-0019-US-0004-TK-0003-fire-details-and-target-click/EP-0019-US-0004-TK-0003-fire-details-and-target-click.md) | Детали стрельбы и назначение цели | client | `EP-0019-US-0004-TK-0001-phase-panel-container-and-header` | US4-AC3 |
| [EP-0019-US-0004-TK-0004-salvo-and-results-details](EP-0019-US-0004-TK-0004-salvo-and-results-details/EP-0019-US-0004-TK-0004-salvo-and-results-details.md) | Детали залпа и результатов | client | `EP-0019-US-0004-TK-0001-phase-panel-container-and-header` | US4-AC4 |
| [EP-0019-US-0004-TK-0005-outcome-details](EP-0019-US-0004-TK-0005-outcome-details/EP-0019-US-0004-TK-0005-outcome-details.md) | Итог боя | client | `EP-0019-US-0004-TK-0004-salvo-and-results-details` | US4-AC5 |

## Итоговая проверка

Все тикеты истории реализованы и опубликованы. Критерии сопоставлены проверкам в [ExecutionStatus](../ExecutionStatus.md); текущие результаты заменяют начальную запись blocked. Human/physical input и проверки недоступных ОС остаются отдельными gates.
