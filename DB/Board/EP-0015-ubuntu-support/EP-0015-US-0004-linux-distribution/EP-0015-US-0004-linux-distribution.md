---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
title: "Сборка дистрибутива и пользовательские данные на Linux"
stage: draft
dependencies: [EP-0015-US-0002, EP-0015-US-0003]
---

# Сборка дистрибутива и пользовательские данные на Linux

Эпик: [EP-0015 — Поддержка Ubuntu](../Documentation.md)

## User story

Как игрок на Ubuntu, я распаковываю сборку игры, запускаю её одним файлом, и мои сохранения и настройки хранятся в домашнем каталоге, а не рядом с программой.

## Зависимости

EP-0015-US-0002, EP-0015-US-0003

## Критерии приёмки

- **AC-01.** `dotnet publish src/DeepSpaceSaga.Client -c Release -r linux-x64 --self-contained` даёт каталог, который запускается на чистой Ubuntu 24.04 без установленного .NET.
- **AC-02.** Сохранения, `Settings.json`, `DebugTuning/` и `CombatMetrics/` на Linux пишутся в каталоги XDG (`~/.local/share/DeepSpaceSaga`, `~/.config/DeepSpaceSaga`), а не в `AppContext.BaseDirectory`.
- **AC-03.** Существующие сохранения рядом с программой подхватываются при первом запуске: копируются в новый каталог, исходники не удаляются.
- **AC-04.** Документация описывает зависимости Ubuntu, сборку, запуск и расположение файлов.

## Тикеты

- [EP-0015-US-0004-TK-0001-user-data-paths](EP-0015-US-0004-TK-0001-user-data-paths/EP-0015-US-0004-TK-0001-user-data-paths.md) — Пользовательские данные вне каталога программы
- [EP-0015-US-0004-TK-0002-linux-publish](EP-0015-US-0004-TK-0002-linux-publish/EP-0015-US-0004-TK-0002-linux-publish.md) — Публикация linux-x64
- [EP-0015-US-0004-TK-0003-linux-documentation](EP-0015-US-0004-TK-0003-linux-documentation/EP-0015-US-0004-TK-0003-linux-documentation.md) — Документация для Linux
