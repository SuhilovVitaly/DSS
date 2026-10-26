---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
ticket: EP-0015-US-0003-TK-0002-open-folder-fallback
title: "Открытие папки на Linux"
stage: draft
layer: client
depends_on: []
serves: [AC-03]
created: 2026-10-25
---

# Открытие папки на Linux

История: [EP-0015-US-0003-platform-integration](../EP-0015-US-0003-platform-integration.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

`GameSessionScreen.DebugTuning.cs:101` открывает каталог через `Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true })`. На Linux .NET вызывает `xdg-open`; если его нет, летит исключение.

## Шаги

1. Вынести открытие папки в метод `ShellLauncher.OpenDirectory(string path)`.
2. Windows — как сейчас; Linux — `xdg-open <path>`; macOS — `open <path>`.
3. Любая ошибка запуска → сообщение в статусной строке «Не удалось открыть папку: <путь>» и запись в лог, без исключения наружу.

## Зависимости

Нет.

## Definition of Done

- На Ubuntu с рабочим столом папка открывается в файловом менеджере.
- Без `xdg-utils` клиент не падает и показывает путь.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
