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

## Scope and code context

Both existing PS scripts; tools/DeepSpaceSaga.PortraitAssets/Program.cs and new PortableAssetScripts.cs; new tools/ci/platform-assets.ps1; Ubuntu workflow. System.Drawing is unsupported on Linux; reuse the existing Skia-based PortraitAssets tool and bundled font roles, with no new imaging dependency. Preserve anatomy/alpha/collar guards, manifest sizing/install, previews and contact sheet. Add explicit output/preview directories so verification writes only under artifacts. Verify representative real inputs on both OS and compare decoded image metrics/hashes.

## Windows validation checkpoint

Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-assets/: build.log C#12 zero warnings/errors, format.log scoped formatter exit0; windows/summary.json confirms both scripts execute under PowerShell7.6.5. Inputs: real engine.accelerate.png and CHR-20260915-123931-52F226.png; output portrait retains all six outer-alpha probes, continuous-neck checks and three costume-collar checks. Contact sheet and three-costume preview inspected visually without gaps/cropping. PNG SHA256 recorded for cross-OS comparison. Linux execution pending CI; no System.Drawing remains in either wrapper.
