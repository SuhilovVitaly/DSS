---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
ticket: EP-0015-US-0003-TK-0002-open-folder-fallback
title: "Открытие папки на Linux"
stage: draft
acceptance_status: open
execution_status: implemented
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

## Code context and scope

New UI/ShellLauncher.cs; GameSessionScreen.DebugTuning.cs; GameSession/Controls/DebugTuningPanel.cs; tests/ShellLauncherTests.cs and DebugTuningPanelInputTests.cs. Use ArgumentList for Unix and Windows shell association; inspect asynchronous nonzero launcher exits without blocking rendering. Status carries the attempted absolute directory; errors are logged once per request. Existing folder-test callback remains supported.

## Validation checkpoint

Final self-review found a Windows regression: successful shell activation can reuse Explorer without returning a Process handle. Treating every null handle as failure changed the original Windows behavior. Scope remains ShellLauncher plus the existing tests; accept null only after successful Windows shell activation, retain Unix missing-process failure. API contract: [Process.Start](https://learn.microsoft.com/dotnet/api/system.diagnostics.process.start). Reproduction and corrected run are recorded under D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-shell-reuse/.

2026-10-26: before.trx reproduces the failure (1 failed/4 passed); after.trx passes12/12 launcher and panel-input cases. C#12 compilation zero diagnostics; scoped format exit0. Windows association still propagates exceptions; Unix null handle still fails. This injected launch-result check does not claim physical Explorer or Ubuntu desktop acceptance.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-shell/: shell.trx 11/11 (Unix argument boundaries, Windows association, missing executable, four-scale panel input/status path); tests.log C#12 compilation zero diagnostics; format.log scoped formatter exit0. Unix launcher nonzero stderr is awaited off rendering with a 15-second timeout; path is displayed in localized feedback and failure is logged. Real desktop file-manager opening on Ubuntu remains NOT RUN until a graphical desktop is available.
