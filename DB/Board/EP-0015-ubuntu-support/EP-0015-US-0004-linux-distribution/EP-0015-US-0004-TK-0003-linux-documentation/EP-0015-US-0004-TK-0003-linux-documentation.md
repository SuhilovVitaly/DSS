---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0003-linux-documentation
title: "Документация для Linux"
stage: draft
acceptance_status: open
execution_status: implemented
layer: docs
depends_on: [EP-0015-US-0004-TK-0002-linux-publish]
serves: [AC-04]
created: 2026-10-25
---

# Документация для Linux

> Текущий сводный результат, checkpoints и открытые gates: [FinalValidation](../../FinalValidation.md); публикация: [PublicationStatus](../../PublicationStatus.md). Записи отдельных прогонов ниже — история evidence, а не обещание текущего PASS.

История: [EP-0015-US-0004-linux-distribution](../EP-0015-US-0004-linux-distribution.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Зафиксировать всё, что нужно знать для сборки и запуска на Ubuntu.

## Шаги

1. Зависимости Ubuntu: `dotnet-sdk-8.0` (для сборки), `libgl1`, `libx11-6`, `libxrandr2`, `libxi6`, `libxcursor1`, `libfontconfig1`, `libasound2t64`/`libpulse0`, `xdg-utils`; для скриптов — `powershell`.
2. Сборка и тесты (`tools/check-linux.sh`), публикация (`tools/publish-linux.sh`).
3. Где лежат сохранения, настройки, метрики и файлы отладки на Linux и Windows; переносной режим `portable.txt`.
4. Известные ограничения: Wayland работает через XWayland; проверено на Ubuntu 22.04 и 24.04 x64; arm64 не проверялся.

## Зависимости

[EP-0015-US-0004-TK-0002-linux-publish](../EP-0015-US-0004-TK-0002-linux-publish/EP-0015-US-0004-TK-0002-linux-publish.md)

## Definition of Done

- Новый человек по документу собирает, тестирует и запускает игру на чистой Ubuntu без дополнительных вопросов.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Code context and scope

New DB/Documentation/06-Tooling/Ubuntu.md and documentation README navigation. Commands verified against actual check/publish/asset interfaces; dependency names grounded in official Microsoft/Ubuntu docs. Workflow scope extended to Ubuntu22.04 matrix so the source-card target is verified rather than stated as already tested. Current execution counts/results remain in Board only; native Wayland, physical audio/input and arm64 are not inferred from Xvfb CI.

Validation: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-linux-docs/links.json: zero broken links in Ubuntu guide/navigation. Guide distinguishes source build, release launch, Xvfb and physical acceptance; records absolute-only XDG, migration marker/no overwrite, managed contentRoot, portable requirements and optional desktop re-registration after moving install. Official .NET/PowerShell dependency guidance linked. Actual Ubuntu22.04 execution added to CI matrix and remains pending, not described as already passed.
