---
epic: EP-0014-module-panels
story: EP-0014-US-0003-engine-panel
title: "Двигатель: скорость, лимиты и время манёвра"
stage: draft
dependencies: [EP-0014-US-0004]
---

# Двигатель: скорость, лимиты и время манёвра

## Связь с техническим заданием

§6, M08, исходная US3. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как игрок, я вижу текущую скорость относительно максимальной, ускорение и скорость поворота двигателя. Панель даёт справочное время разгона до максимума и разворота на 90°, чтобы я мог планировать ручное управление. Лимиты обновляются по принятому состоянию, включая debug tuning. Отключённый или уничтоженный двигатель явно помечен, а его лимиты приглушены.

## Зависимости

EP-0014-US-0004. Нужны согласованные общие правила и завершённые результаты предшествующих историй.

## Completion evidence

- **AC-01.** Для лимитов 5 км/с, .6 км/с² и 20°/с видны подписи, числа и полоса скорости. При текущих 3.0 км/с разгон 13 с и поворот 18 с при 1×; при 3.2 км/с разгон 12 с. При 2× времена вдвое меньше; pause сохраняет последнюю скорость для справки.

- **AC-02.** Новая принятая ревизия лимитов/скорости отражается в следующем отображённом кадре; UI не обращается синхронно к Engine.

- **AC-03.** Disabled даёт приглушённые лимиты и Отключён Nс, destroyed — Уничтожен. При нулевом ускорении/повороте нет NaN/infinity или выдуманной длительности.

- **AC-04.** Render и изменение snapshot демонстрируют результат на RU/EN; кнопки ручного управления сохраняют действующий schema2 контракт.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-03: исправление числового примера является явной арифметической нормализацией черновика. Поведение при скорости выше лимита не должно давать отрицательное время; presentation policy уточняется при детализации.

Тикеты отсутствуют. Будущая нарезка: максимум пять тикетов, один production
layer и matching test project, максимум пять implementation files на тикет.
Engine не вызывается из Render; скрытые NPC данные не используются.
Проверки при реализации имеют process-tree cap300000ms и абсолютные
output paths в D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/<unique-run>/.

## Review

Draft; human approval отсутствует. Решения и точные сообщения согласования
будут записаны в review log эпика.
## Execution ticket map2026-10-22

Execution authorized through epic-executor; see [current ticket map](../Tickets.md). Earlier planning-only/no-tickets statements above are historical. Stage remains distinct from human acceptance. Final US-0006 must audit and update all impacted project documentation and rebuild Graphify after final code and text.
