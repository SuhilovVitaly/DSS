---
epic: EP-0014-module-panels
story: EP-0014-US-0002-countermissile-panel
title: "Пусковая ПРО: профили и оценка перехвата"
stage: implementation-complete
dependencies: [EP-0014-US-0004, EP-0014-US-0001]
---

# Пусковая ПРО: профили и оценка перехвата

## Связь с техническим заданием

§5, M01–M06, исходная US2. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как игрок, я вижу профили ПРО, клавиши и текущий выбор, дальность, перезарядку с таймером и разброс в километрах. Мой первый профиль wide, а дальнейший выбор запоминается для каждого аппарата. При наведении на наблюдаемую трассу торпеды я сравниваю время прибытия профилей в точку перехвата и вижу, когда профиль не успевает. Причины невозможности запуска остаются видны при наведении на кнопки.

## Зависимости

EP-0014-US-0004, EP-0014-US-0001. Нужны согласованные общие правила и завершённые результаты предшествующих историй.

## Completion evidence

- **AC-01.** На базовой конфигурации: range30 км, wide15 км/с R1 км и fast25 км/с R.5 км; ядро не показывается. Полная reload12.5 motion-секунд отображается 50 real-секунд при 1× и 25 при 2×.

- **AC-02.** При effective sigma .0666667 на 10 км показывается .67 км; указано, что ПРО сбивает снаряды во всём R. Клавиши приходят из bindings.

- **AC-03.** wide выбирается первоначально, затем выбранный профиль сохраняется независимо по аппарату; unavailable default имеет безопасный fallback к первому.

- **AC-04.** При snap к допущенной трассе профили показывают время до одной согласованной точки перехвата; подтверждённо опаздывающий профиль помечен Недостижимо. Отсутствующий прогноз не заменяется нулём.

- **AC-05.** Render/input подтверждают кольцо с числом, сохранение статуса при hover, reload/disabled/no-operator/destroyed причины и отсутствие launch при запрете. Время, pause и локализация согласованы с US4/US1.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-07: точка и достижимость должны совпадать с live preview и не раскрывать скрытые NPC планы. Условия входа для destroyed наследуют решение review US1.

Тикеты отсутствуют. Будущая нарезка: максимум пять тикетов, один production
layer и matching test project, максимум пять implementation files на тикет.
Engine не вызывается из Render; скрытые NPC данные не используются.
Проверки при реализации имеют process-tree cap300000ms и абсолютные
output paths в D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/<unique-run>/.

## Historical planning review

Draft; human approval отсутствует. Решения и точные сообщения согласования
будут записаны в review log эпика.
## Execution ticket map2026-10-22

Execution authorized through epic-executor; see [current ticket map](../Tickets.md). Earlier planning-only/no-tickets statements above are historical. Stage remains distinct from human acceptance. Final US-0006 must audit and update all impacted project documentation and rebuild Graphify after final code and text.

## Story verification2026-10-22

56/56 affected tests pass, including live observed common-point arrival, unreachable profile, no-track handling, calibrated10km sigma, no core,50/25s reload, shared profile memory and existing countermeasure flows. Common blocked-input paths were verified in US1. Snapshot data and observed trace only; no concealed future endpoint is read. Native/human and independent review remain open.

## Current implementation and acceptance
Implementation is complete under the explicit epic-executor authorization. The earlier draft/no-ticket/approval passages are historical planning records, not current execution restrictions. Canonical tickets and per-ticket publication: [Tickets](../Tickets.md), [ExecutionStatus](../ExecutionStatus.md). Automated and actual native evidence, story coverage and open physical/human/independent gates: [FinalValidation](../FinalValidation.md). No human product approval is synthesized.
