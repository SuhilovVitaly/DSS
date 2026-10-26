---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0001-user-data-paths
title: "Пользовательские данные вне каталога программы"
stage: draft
layer: client
depends_on: [EP-0015-US-0004-TK-0004-external-settings-content-root]
serves: [AC-02, AC-03]
created: 2026-10-25
---

# Пользовательские данные вне каталога программы

История: [EP-0015-US-0004-linux-distribution](../EP-0015-US-0004-linux-distribution.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Сейчас всё пишется рядом с программой: `Program.cs:44-47` (`Settings.json`, `Saves`, `DebugTuning`), `CombatMetricsWriter.cs:19`, `DebugTuningFileWriter.cs:32`, `Localization.cs:100`. В `/opt` или `/usr` это недоступно на запись (то же касается `Program Files` на Windows).

## Шаги

1. Добавить `AppPaths` с одним источником правды для путей: `UserData` (сохранения, метрики, отладка), `UserConfig` (настройки), `Content` (`AppContext.BaseDirectory`: Data, Images, Fonts, Scenarios — только чтение).
2. Linux: `UserData` = `$XDG_DATA_HOME/DeepSpaceSaga` или `~/.local/share/DeepSpaceSaga`; `UserConfig` = `$XDG_CONFIG_HOME/DeepSpaceSaga` или `~/.config/DeepSpaceSaga`. Windows: `%LOCALAPPDATA%\DeepSpaceSaga` для обоих.
3. Переносной режим: если рядом с программой лежит файл `portable.txt` — всё как сейчас, в `AppContext.BaseDirectory` (сохраняет текущее поведение для разработки).
4. `Settings.json` из комплекта — шаблон по умолчанию: при отсутствии пользовательского копируется в `UserConfig`.
5. Однократная миграция: если в `UserData/Saves` пусто, а `AppContext.BaseDirectory/Saves` не пуст — скопировать сохранения, ничего не удалять, записать в лог.

## Зависимости

Нет.

## Definition of Done

- На Ubuntu новые сохранения появляются в `~/.local/share/DeepSpaceSaga/Saves`.
- С `portable.txt` поведение совпадает с текущим.
- Тесты `AppPaths`: переменные XDG заданы и не заданы; переносной режим; миграция не перезаписывает существующие файлы.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
