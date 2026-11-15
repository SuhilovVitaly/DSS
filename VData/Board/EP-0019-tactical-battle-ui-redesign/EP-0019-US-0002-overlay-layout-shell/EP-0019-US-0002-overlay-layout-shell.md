---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0002-overlay-layout-shell
title: Каркас экрана: карта под панелями
stage: done
dependencies: [EP-0019-US-0001-reference-and-golden-harness]
created: 2026-11-08T09:31:05Z
source_request: "Перевести экран боя на пространство макета 1280×720 с токенами и примитивами DesignSpec, карту на весь экран и камеру по фазам; убрать старую правую панель."
current_review: epic
revision: 1
---

# Каркас экрана: карта под панелями

## Входное техническое задание

D-01, D-02, D-03. [DesignSpec §1–3, §7, §10](../DesignSpec.md). Текущая компоновка — `TacticalBattleScreen.cs:93-228`, камера — `TacticalBattleCamera.cs`.

## User story

Как игрок, я хочу видеть поле боя на весь экран и компактные панели поверх него, чтобы карте доставалось как можно больше места.

## Dependencies

[EP-0019-US-0001-reference-and-golden-harness](../EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-reference-and-golden-harness.md)

## Completion evidence

- Карта рисуется на весь экран; панелей старого вида нет; клавиши 0, Esc, Enter, Tab, стрелки работают.
- Камера в движении вписывает поле с носителями, в остальных фазах — дроны, в безопасной области 202…Wd−246.
- Все новые строки есть в RU и EN.

## Acceptance criteria

- **US2-AC1.** Масштаб `S = H/720`; правая колонка привязана к правому краю; при 1920×1080 координаты элементов равны координатам DesignSpec ×1,5.
- **US2-AC2.** Токены и примитивы `BattleDesign` повторяют DesignSpec §2–3 (цвета, радиусы, толщины, размеры шрифтов, `FitSize`, форматирование чисел) и покрыты тестами.
- **US2-AC3.** Карта (фон `MapBg`, сетка 26, линейка 100 у.е.) занимает весь экран; вкладки, флажки видимости и кнопки старой панели удалены; клики по прямоугольникам панелей не доходят до карты.
- **US2-AC4.** Камера по DesignSpec §8.4 даёт масштаб и центр по формуле на снимках seed 1 (допуск 0,1 %); колесо и ПКМ работают как раньше; `0` возвращает вписанный вид.
- **US2-AC5.** Ключи всех строк DesignSpec §5–7 есть в `Russian.json` и `English.json`; `LocalizationCoverageTests` проходят.

## Non-goals

Содержимое панелей и карты (US-0003–US-0005).

## Invariants

Команды движения и огня формируются прежними `BuildMoveRequest`/`BuildFireRequest`. Подтверждение выхода, пауза мира и блокировка мировых клавиш сохраняются.

## Решения и согласование

Исполнение draft-карточек разрешено запросом выполнить весь эпик. Использованы записанные варианты по умолчанию; художественное/пользовательское одобрение не приписывается. Итог — [ExecutionStatus](../ExecutionStatus.md).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0002-TK-0001-design-tokens-and-primitives](EP-0019-US-0002-TK-0001-design-tokens-and-primitives/EP-0019-US-0002-TK-0001-design-tokens-and-primitives.md) | Токены и примитивы макета | client | `EP-0019-US-0001-TK-0003-scene-harness` | US2-AC1, US2-AC2 |
| [EP-0019-US-0002-TK-0002-full-screen-map-shell](EP-0019-US-0002-TK-0002-full-screen-map-shell/EP-0019-US-0002-TK-0002-full-screen-map-shell.md) | Карта на весь экран и удаление старой панели | client | `EP-0019-US-0002-TK-0001-design-tokens-and-primitives` | US2-AC1, US2-AC3 |
| [EP-0019-US-0002-TK-0003-phase-camera-fit](EP-0019-US-0002-TK-0003-phase-camera-fit/EP-0019-US-0002-TK-0003-phase-camera-fit.md) | Камера по фазам | client | `EP-0019-US-0002-TK-0002-full-screen-map-shell` | US2-AC4 |
| [EP-0019-US-0002-TK-0004-localization-strings](EP-0019-US-0002-TK-0004-localization-strings/EP-0019-US-0002-TK-0004-localization-strings.md) | Строки RU/EN нового интерфейса | content-data | `EP-0019-US-0002-TK-0002-full-screen-map-shell` | US2-AC5 |

## Итоговая проверка

Все тикеты истории реализованы и опубликованы. Критерии сопоставлены проверкам в [ExecutionStatus](../ExecutionStatus.md); текущие результаты заменяют начальную запись blocked. Human/physical input и проверки недоступных ОС остаются отдельными gates.
