---
epic: EP-0014-module-panels
story: EP-0014-US-0001-torpedo-panel
title: "Торпедный аппарат: подготовка и обратная связь выстрела"
stage: draft
dependencies: [EP-0014-US-0004]
---

# Торпедный аппарат: подготовка и обратная связь выстрела

## Связь с техническим заданием

§4, M02–M07, P2–P6, исходная US1. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как игрок, я вижу выбранный профиль, его клавишу, радиус и ядро, дальность, урон, бронепробитие и перезарядку своего аппарата. Выбор запоминается отдельно для каждого аппарата на время сессии, а первый выбор использует medium либо первый доступный профиль. В прицеле я сравниваю время полёта и разброс до курсора. Я могу подготовить выстрел во время перезарядки или отключения и получаю понятную причину отказа запуска.

## Зависимости

EP-0014-US-0004. Нужны согласованные общие правила и завершённые результаты предшествующих историй.

## Completion evidence

- **AC-01.** Клавиши профилей берутся из актуальных bindings; отметка выбора видна и вне прицела. Для R9/5/2.5 и CoreFraction .3 ядро 2.7/1.5/.75 км; M07 трактуется по G-04.

- **AC-02.** Первый выбор medium, затем fast сохраняется после выхода/входа; другой аппарат независим, новая сессия начинает с defaults. Неизвестный default даёт первый профиль. При исчезновении профиля после обновления данных не сохраняется невалидный выбор.

- **AC-03.** Для snapshot damage200, penetration20, reload10 motion-секунд панель показывает 200/20%/40 real-секунд при 1× и 20 с при 2×; дробная penetration не теряется. Дальность 70 км видна у переключателя кольца.

- **AC-04.** При sigma .0166667 на 25 км отображается .42 км. На 25 км скорости 6/10/16 дают 16.7/10.0/6.3 real-секунд при 1×; на 80 км при range70 — вне дальности. Без оператора нет выдуманного sigma.

- **AC-05.** Число на reload ring сохраняется при hover: целые секунды, ниже 10 — десятые; при ready число скрыто. Через 3 motion-секунды из 10 при 1× остаётся 28 real-секунд.

- **AC-06.** Reload/disabled/no operator/destroyed видны как причины, заблокированный клик не отправляет launch и даёт красную причину в статусе и у курсора. Вход при reload/disabled работает; правило входа у destroyed открыто в G-02.

- **AC-07.** Проверяется фактический Render и input, включая два аппарата, переключение скорости/паузу и обновлённые snapshot. Тексты RU/EN доступны с момента поставки панели.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-02 (вход у destroyed), G-04 (display core), G-05 (precision). Шесть исходных ticket proposals не утверждены; последующая нарезка — максимум пять тикетов. Новые authoritative сведения входят в результат этой истории, а cleanup вынесен в US5.

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
