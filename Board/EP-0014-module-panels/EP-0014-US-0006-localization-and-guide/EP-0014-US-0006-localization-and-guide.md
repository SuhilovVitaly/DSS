---
epic: EP-0014-module-panels
story: EP-0014-US-0006-localization-and-guide
title: "Панели на русском и английском и актуальное руководство"
stage: draft
dependencies: [EP-0014-US-0001, EP-0014-US-0002, EP-0014-US-0003, EP-0014-US-0004, EP-0014-US-0005]
---

# Панели на русском и английском и актуальное руководство

## Связь с техническим заданием

§7 о локализации, §9 о документации, исходная US6. Полный источник, текущие факты и D/A/G см. в [Documentation.md](../Documentation.md).
Требования ниже — целевой результат; это не утверждение о наличии реализации.

## User story

Как игрок, я получаю все подписи, единицы и подсказки панелей на выбранном русском или английском языке. Названия клавиш соответствуют моим текущим привязкам. Как разработчик, я нахожу согласованное описание панелей, времени и памяти профилей в актуальном руководстве. Из руководства понятны источник чисел и границы клиентской оценки.

## Зависимости

EP-0014-US-0001, EP-0014-US-0002, EP-0014-US-0003, EP-0014-US-0004, EP-0014-US-0005. Нужны согласованные общие правила и завершённые результаты предшествующих историй.

## Completion evidence

- **AC-01.** RU/EN покрывают весь набор Combat.Panel.* для новых панелей, включая R, sigma, ядро, маркер выбора, причины, состояния, здоровье, critical tooltip и единицы. Проверка намеренного отсутствующего ключа выявляет пробел.

- **AC-02.** В коде panel presentation нет заданных нелокализованных символов/аббревиатур; семантическая проверка ограничена этим кодом, а не всеми строками проекта. Клавиши отображаются из schema2 bindings.

- **AC-03.** PointAimCombat.md актуально описывает аппараты, ПРО, двигатель, M01 и M03; при необходимости связанные текущие контракты обновлены согласованно. В Documentation нет копии task history или generated evidence.

- **AC-04.** Демонстрация RU/EN и links review подтверждают соответствие фактически реализованным панелям; истории приёмки остаются в Board. Само наличие документации не закрывает native/human acceptance.

Это план будущих наблюдений, а не выполненная проверка. Automated/render,
native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Нормативные документы не меняются во время текущего planning. Последующая поставка документации должна описывать реализованный контракт. Сквозная локализация не откладывается целиком до этой истории.

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
