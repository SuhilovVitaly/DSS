---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0003-linux-check-script
title: "Скрипт проверки check-linux.sh"
stage: draft
layer: tools
depends_on: [EP-0015-US-0001-TK-0002-test-portability-fixes, EP-0015-US-0002-TK-0001-skiasharp-linux-native]
serves: [AC-05]
created: 2026-10-25
---

# Скрипт проверки check-linux.sh

История: [EP-0015-US-0001-headless-build-and-tests](../EP-0015-US-0001-headless-build-and-tests.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Одна команда для повторяемой проверки на Linux, без ручного перечисления проектов.

## Шаги

1. `tools/check-linux.sh <results-dir>`: `set -euo pipefail`, проверка версии `dotnet`, `dotnet build -c Release`, затем `dotnet test` по всем тестовым проектам с `--logger trx --results-directory <results-dir>`.
2. Код выхода ненулевой при любой ошибке сборки или теста.
3. Тесты клиента запускаются, только если задан `DISPLAY` или `WAYLAND_DISPLAY`; иначе скрипт явно печатает, что тесты клиента пропущены.
4. Файл хранится с окончаниями LF и правом на выполнение (`git update-index --chmod=+x`).

## Зависимости

[EP-0015-US-0001-TK-0002-test-portability-fixes](../EP-0015-US-0001-TK-0002-test-portability-fixes/EP-0015-US-0001-TK-0002-test-portability-fixes.md)

## Definition of Done

- Скрипт на чистой Ubuntu 24.04 даёт тот же итог, что ручной прогон из TK-0001 после исправлений.
- Без графической сессии скрипт не падает, а сообщает о пропуске тестов клиента.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.
