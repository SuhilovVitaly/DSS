---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0003-layout-verification
title: "Проверка раскладки после смены шрифтов"
stage: draft
layer: client
depends_on: [EP-0015-US-0002-TK-0002-bundled-fonts]
serves: [AC-03, AC-04]
created: 2026-10-25
---

# Проверка раскладки после смены шрифтов

История: [EP-0015-US-0002-client-rendering](../EP-0015-US-0002-client-rendering.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Inter и JetBrains Mono по ширине отличаются от Segoe UI и Consolas. Нужно найти и поправить места, где текст обрезается или колонки разъезжаются.

## Шаги

1. Снять скриншоты на Windows до и после TK-0002: главное меню, сохранение/загрузка, тактическая карта с панелью торпедного аппарата и панелью отладки (F12), торговля.
2. Поправить жёстко заданные ширины колонок и размеры шрифта там, где текст перестал помещаться.
3. Повторить те же скриншоты на Ubuntu и сравнить с Windows.
4. Прогнать `DeepSpaceSaga.Client.Tests` на обеих ОС; тесты с ожиданиями ширины текста обновить осознанно, с пояснением в тикете.

## Зависимости

[EP-0015-US-0002-TK-0002-bundled-fonts](../EP-0015-US-0002-TK-0002-bundled-fonts/EP-0015-US-0002-TK-0002-bundled-fonts.md)

## Definition of Done

- Набор скриншотов Windows/Ubuntu приложен к тикету; обрезанного текста нет.
- `DeepSpaceSaga.Client.Tests` проходит на обеих ОС.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
