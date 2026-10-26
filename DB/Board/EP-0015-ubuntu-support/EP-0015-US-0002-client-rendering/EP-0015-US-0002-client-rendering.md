---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
title: "Клиент запускается и выглядит как на Windows"
stage: draft
dependencies: [EP-0015-US-0001]
---

# Клиент запускается и выглядит как на Windows

Эпик: [EP-0015 — Поддержка Ubuntu](../Documentation.md)

## User story

Как игрок на Ubuntu, я запускаю клиент, вижу главное меню и тактическую карту, а текст, колонки и панели расположены так же, как на Windows.

## Зависимости

EP-0015-US-0001

## Критерии приёмки

- **AC-01.** Клиент стартует на Ubuntu 24.04 (X11 и Wayland через XWayland) без `DllNotFoundException`/`TypeInitializationException` от SkiaSharp.
- **AC-02.** Во всём интерфейсе используются шрифты из папки `UI/Fonts` клиента; системные `Segoe UI`, `Consolas`, `Verdana` больше не запрашиваются.
- **AC-03.** Скриншоты главного меню, тактической карты с панелью модулей и экрана торговли на Windows и Ubuntu совпадают по раскладке: ширина колонок и переносы строк одинаковы, отличаются только сглаживанием.
- **AC-04.** Тесты `DeepSpaceSaga.Client.Tests` проходят на Ubuntu с графической сессией.

## Тикеты

- [EP-0015-US-0002-TK-0001-skiasharp-linux-native](EP-0015-US-0002-TK-0001-skiasharp-linux-native/EP-0015-US-0002-TK-0001-skiasharp-linux-native.md) — Нативная библиотека SkiaSharp для Linux
- [EP-0015-US-0002-TK-0002-bundled-fonts](EP-0015-US-0002-TK-0002-bundled-fonts/EP-0015-US-0002-TK-0002-bundled-fonts.md) — Шрифты в комплекте клиента
- [EP-0015-US-0002-TK-0003-layout-verification](EP-0015-US-0002-TK-0003-layout-verification/EP-0015-US-0002-TK-0003-layout-verification.md) — Проверка раскладки после смены шрифтов
