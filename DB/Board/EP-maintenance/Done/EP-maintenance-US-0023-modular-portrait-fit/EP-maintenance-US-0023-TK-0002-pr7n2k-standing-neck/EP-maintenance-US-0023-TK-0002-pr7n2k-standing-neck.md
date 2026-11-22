---
epic: EP-maintenance
story: EP-maintenance-US-0023-modular-portrait-fit
ticket: EP-maintenance-US-0023-TK-0002-pr7n2k-standing-neck
title: Исправить высоту головы PR7N2K в стойке
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
serves: [AC2]
---

# Исправить высоту головы PR7N2K в стойке

[История](../EP-maintenance-US-0023-modular-portrait-fit.md)

Голову опустили на 6 игровых пикселей во всех пяти ячейках `threequarter.png`. Слой головы подрезали у воротника по действующей линии шва. В промпт добавили обязательную проверку высоты головы и просвета над воротником до генерации эмоций.

## Проверки

Валидатор W8 подтвердил формат и шов. Сравнили все пять эмоций на GREEN до и после. Атлас пленного, его эталонные слои и общий `settings.json` совпадают побайтно. Проверка выполнена на сохранённых спрайтах.
