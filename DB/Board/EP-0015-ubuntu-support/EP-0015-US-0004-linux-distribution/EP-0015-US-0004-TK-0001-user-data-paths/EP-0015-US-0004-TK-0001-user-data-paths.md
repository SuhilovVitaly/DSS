---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0001-user-data-paths
title: "Пользовательские данные вне каталога программы"
stage: draft
acceptance_status: open
execution_status: implemented
layer: client
depends_on: [EP-0015-US-0004-TK-0004-external-settings-content-root]
serves: [AC-02, AC-03]
created: 2026-10-25
---

# Пользовательские данные вне каталога программы

> Текущий сводный результат, checkpoints и открытые gates: [FinalValidation](../../FinalValidation.md); публикация: [PublicationStatus](../../PublicationStatus.md). Записи отдельных прогонов ниже — история evidence, а не обещание текущего PASS.

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

## Code context and scope

New Client/AppPaths.cs; Program.cs; Localization.cs; UI/SkiaWindow.cs; CombatMetricsWriter.cs; DebugTuningFileWriter.cs; UI/InterfaceLog.cs; GameSessionScreen.DebugTuning.cs; new AppPathsTests.cs plus Client.Tests.csproj/portable.txt; Performance native layout harness. More than5 files necessary to route all existing settings/data writes through one source. Content assets remain AppContext.BaseDirectory; optional Engine contentRoot prerequisite published c7e76bd7. Tests use an explicit portable marker and isolated path override; no validation writes real user settings. Initialize config/migration before startup logging, atomic config writes, preserve existing saves and one-time migration marker.

## Windows validation checkpoint

D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-app-paths/: app-paths-final.trx39/39; client-full.trx2346/2346, no failures/skips; format.log scoped formatter exit0. Initial five path-test failures were mixed slash expectations on Windows, corrected by normalizing expected full paths. Program now sets CWD to read-only content so existing relative image/font lookups work when started from a desktop launcher; all settings/save/log/diagnostic writes use absolute user paths. This final Main adjustment compiled with zero warnings/errors in build-final.log; native-windows/layout.json captures six real screens successfully using isolated portable override. Actual user profiles were not modified by validation. Linux XDG/portable validation pending CI.
