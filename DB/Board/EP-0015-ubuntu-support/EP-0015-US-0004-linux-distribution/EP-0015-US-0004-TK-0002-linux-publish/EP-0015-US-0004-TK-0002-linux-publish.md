---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0002-linux-publish
title: "Публикация linux-x64"
stage: draft
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
