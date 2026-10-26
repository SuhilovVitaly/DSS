---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0003-platform-integration
ticket: EP-0015-US-0003-TK-0003-powershell-scripts
title: "Служебные скрипты PowerShell"
stage: draft
layer: tools
depends_on: []
serves: [AC-04]
created: 2026-10-25
---

# Служебные скрипты PowerShell

История: [EP-0015-US-0003-platform-integration](../EP-0015-US-0003-platform-integration.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

В репозитории два скрипта `.ps1`. На Ubuntu они работают только при установленном PowerShell 7.

## Шаги

1. Проверить оба скрипта под `pwsh` 7 на Ubuntu: разделители путей, вызовы Windows-утилит, регистр имён файлов.
2. Непереносимые места исправить (`Join-Path`, без `cmd.exe` и путей с `\`).
3. В шапку каждого скрипта добавить комментарий: назначение, требование `pwsh 7+`, пример запуска на Linux.

## Зависимости

Нет.

## Definition of Done

- Оба скрипта выполняются под `pwsh` на Ubuntu и дают тот же результат, что на Windows.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
