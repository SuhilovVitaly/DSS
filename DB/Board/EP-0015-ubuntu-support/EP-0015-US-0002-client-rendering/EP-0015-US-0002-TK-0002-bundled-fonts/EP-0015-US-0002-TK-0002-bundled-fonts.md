---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0002-bundled-fonts
title: "Шрифты в комплекте клиента"
stage: draft
layer: client
depends_on: [EP-0015-US-0002-TK-0001-skiasharp-linux-native]
serves: [AC-02]
created: 2026-10-25
---

# Шрифты в комплекте клиента

История: [EP-0015-US-0002-client-rendering](../EP-0015-US-0002-client-rendering.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Код запрашивает системные шрифты: `SKTypeface.FromFamilyName("Segoe UI")` (2 места), `"Consolas"` (3), `"Verdana"` (4). На Ubuntu их нет, Skia молча подставляет шрифт по умолчанию, и раскладка плывёт.

## Шаги

1. Взять свободные шрифты с лицензией OFL: **Inter** (вместо Segoe UI, Regular и Bold), **JetBrains Mono** (вместо Consolas), **Noto Sans** (вместо Verdana, Regular, Bold, Italic). Положить в `src/DeepSpaceSaga.Client/UI/Fonts/` вместе с файлами лицензий; правило копирования `UI\Fonts\**\*.ttf;*.otf;*.txt` уже есть в `.csproj`.
2. Добавить единый класс `UiFonts` (рядом с `XenonStyle.cs`): лениво загружает `SKTypeface.FromFile` по роли (`Ui`, `UiBold`, `Mono`, `Body`, `BodyItalic`, `BodyWeight(int)`), кэширует, при отсутствии файла пишет в лог и падает назад на `SKTypeface.Default`.
3. Заменить все вызовы `FromFamilyName` в файлах: `ImageButton.cs`, `MenuStyle.cs`, `XenonStyle.cs`, `CommandsPanel.cs`, `DebugTuningPanel.cs`, `GameSessionScreen.cs`, `ObjectLabelRenderer.cs`, `TradePainter.cs`.
4. `humaroid.regular.otf` уже грузится из файла — не трогать.

## Зависимости

[EP-0015-US-0002-TK-0001-skiasharp-linux-native](../EP-0015-US-0002-TK-0001-skiasharp-linux-native/EP-0015-US-0002-TK-0001-skiasharp-linux-native.md)

## Definition of Done

- `git grep FromFamilyName -- src` не находит вызовов.
- На Windows и Ubuntu интерфейс рисуется одними и теми же шрифтами.
- Удаление файла шрифта не роняет клиент: в логе есть предупреждение, текст рисуется шрифтом по умолчанию.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
