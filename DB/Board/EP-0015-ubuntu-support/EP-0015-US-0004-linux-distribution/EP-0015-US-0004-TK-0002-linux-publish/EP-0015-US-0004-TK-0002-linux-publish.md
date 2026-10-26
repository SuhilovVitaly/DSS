---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0002-linux-publish
title: "Публикация linux-x64"
stage: draft
acceptance_status: open
execution_status: implemented
layer: tools
depends_on: [EP-0015-US-0004-TK-0001-user-data-paths]
serves: [AC-01]
created: 2026-10-25
---

# Публикация linux-x64

История: [EP-0015-US-0004-linux-distribution](../EP-0015-US-0004-linux-distribution.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Получить запускаемую сборку для Ubuntu одной командой.

## Шаги

1. Скрипт `tools/publish-linux.sh <out-dir>`: `dotnet publish src/DeepSpaceSaga.Client -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false`.
2. Проверить, что в выводе есть `libSkiaSharp.so`, `libglfw.so.3`, нативная библиотека OpenAL Soft, папки `Data`, `Images`, `UI/Fonts`, `Scenarios`.
3. Выставить право на выполнение у `DeepSpaceSaga.Client` и упаковать в `DeepSpaceSaga-linux-x64.tar.gz` (tar сохраняет права, zip — нет).
4. Положить рядом `deepspacesaga.desktop` и иконку для меню приложений (необязательная установка в `~/.local/share/applications`).

## Зависимости

[EP-0015-US-0004-TK-0001-user-data-paths](../EP-0015-US-0004-TK-0001-user-data-paths/EP-0015-US-0004-TK-0001-user-data-paths.md)

## Definition of Done

- Архив распаковывается и запускается на чистой Ubuntu 24.04 без .NET.
- Пропущенный нативный файл приводит к ошибке скрипта, а не к сломанной сборке.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Code context and scope

tools/publish-linux.sh; tools/linux/install-desktop.sh and deepspacesaga.svg; tools/ci/platform-publish.py; workflow. Native package verification additionally requires Program.cs, UI/SkiaWindow.cs and new UI/NativeStartupEvidence.cs: opt-in absolute DSS_STARTUP_EVIDENCE_PATH captures the actual main-menu GPU frame and round-trips a real session save using production factory, then exits. Normal startup has no probe. Verify archive/execute bits/native inventory and fail a deliberately incomplete package. Clean Ubuntu24.04 container has no dotnet command, read-only mounted install and writable isolated XDG profile; process300s cap, separate from human desktop/audio gates.

## Windows/cross-publish checkpoint

Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-publish/: C#12 build.log zero warnings/errors, format.log scoped formatter exit0; windows-startup.json passed actual production Main/GPU main menu + real factory save/load round-trip under isolated LOCALAPPDATA (14 frames, no actual user-profile writes). windows-startup.png retained. cross-publish.log self-contained linux-x64 exit0; cross-inventory.json and native-dependencies.json verify libSkiaSharp.so, libglfw.so.3, libopenal.so, runtime and content directories. verify-complete.log exit0; verify-incomplete.log deliberate missing libSkiaSharp.so exits1, file restored. Bash syntax and Python AST pass. Linux tar permission/extraction, clean Ubuntu no-runtime/read-only launch pending CI; not claimed from cross-compilation.
