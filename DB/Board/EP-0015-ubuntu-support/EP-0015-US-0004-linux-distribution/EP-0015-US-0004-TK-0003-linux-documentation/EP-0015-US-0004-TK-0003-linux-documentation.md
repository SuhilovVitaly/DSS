---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0003-linux-documentation
title: "Документация для Linux"
stage: draft
layer: docs
depends_on: [EP-0015-US-0004-TK-0002-linux-publish]
serves: [AC-04]
created: 2026-10-25
---

# Документация для Linux

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
