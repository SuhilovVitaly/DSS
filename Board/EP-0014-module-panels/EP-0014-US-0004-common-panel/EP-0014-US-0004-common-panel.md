---
epic: EP-0014-module-panels
story: EP-0014-US-0004-common-panel
title: "Общая панель: приоритет, время и состояние модулей"
stage: draft
dependencies: []
---

# Общая панель: приоритет, время и состояние модулей

## Связь с техническим заданием

§7, M01, P1/P7–P11, часть §8 о пределе, исходная US4. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как игрок, я вижу в ограниченном окне прежде всего выбранную панель и оружие, сохраняя сделанные вручную сворачивания. Все длительности и таймеры показывают реальные секунды с пояснением скорости игры и физических секунд. В бою я различаю броню, структуру, отключение, уничтожение и критичность модулей. Число уничтоженных модулей сравнивается с авторитетным пределом моего корабля. Панель сохраняет отзывчивость при неизменных данных.

## Зависимости

none. Самостоятельный результат общей панели; снапшотные добавления планируются в её собственных границах.

## Completion evidence

- **AC-01.** В 1280×720 при масштабе 1.5 приоритет раскрытия: preferred → torpedo → ПРО → Navigation → Engine → Space Control → остальные. Проверяются несколько аппаратов и отсутствие preferred; вручную свёрнутые остаются свёрнутыми.

- **AC-02.** Real seconds = motion seconds / текущий motion multiplier. При 1× коэффициент .25; подсказка у длительности содержит реальные секунды при x<k>; физических: <n>с. После pause с 2× countdown заморожен, справка использует 2×; исходное значение 1×.

- **AC-03.** Строки модулей показывают подписанные полосы Броня/Структура и текущие числа, полные current/max в tooltip. Готов у аппарата не дублируется; disabled/destroyed различимы. Critical icon имеет пояснение, damage indicator остаётся на .8 real-секунды.

- **AC-04.** Destroyed n/L приходит из authoritative snapshot только для своего корабля. Для 9 модулей при fraction .3333 L3, для 6 L2; при n=L−1 предупреждение красное. Клиент не воспроизводит destruction rule.

- **AC-05.** После анализа consumers удаляется только подтверждённо мёртвый P11 код; поведение действующих панелей сохраняется. Все подписи/единицы локализованы.

- **AC-06.** На согласованном пути панели после прогрева при фиксированных данных нет новых managed allocations; реальный Render проверяется отдельно от formatter. Изменения snapshot, курсора, скорости, hover и состояния панели корректно обновляют вывод, без устаревших строк. Граница измерения — открытый G-06.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-06: требуется согласовать границу zero-allocation измерения. P11 требует доказательства отсутствия consumers. Новое authoritative поле предела входит в результат истории; выбор DTO и matching tests уточняются при планировании тикетов.

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
