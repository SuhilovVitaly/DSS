---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0003-linux-check-script
title: "Скрипт проверки check-linux.sh"
stage: draft
execution_status: implemented-ci-validation-pending
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

## Code context and scope update 2026-10-26

Разрешены tools/check-linux.sh (entrypoint), tools/ci/platform-baseline.py (существующий проверенный exact-case harness; явный skip Client только без DISPLAY/WAYLAND_DISPLAY), .github/workflows/ubuntu-support.yml (реальный Linux запуск entrypoint подXvfb; Windows baseline остаётся). Python3 — явная зависимость инструмента, обычная дляUbuntu; бизнес-логика и src не меняются. Every test process сохраняет cap300с, точныеcases/TRX identities и ненулевойexit при ошибке. Для headless skip записываются причина/NOT RUN в summary и stdout. Все результаты — в переданный абсолютный каталог с уникальным run дочерним каталогом. Наличие виртуального дисплея не доказывает human/XWayland/audio приёмку.

## Execution result 2026-10-26

tools/check-linux.sh добавлен сset-euo-pipefail, проверкойLinux/.NET8+/Python3, абсолютным results-dir и уникальным дочерним run. Используется проверенный exact-case Python harness: buildRelease, каждый test process≤300с, TRX identity coverage, ненулевойexit приfailure. БезDISPLAY/WAYLAND_DISPLAY Client явноNOT RUN вstdout иsummary; остальныеprojects выполняются. ПодDISPLAY/WaylandClient выполняется. CIUbuntu вызываетentrypoint подXvfb, Windows сохраняеттотжеharness.

Python AST/Python LF assertion иbash-n/--help PASS. SandboxMSYS named-object denied; повтор внеsandbox с300с cap прошёл. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-check-linux/ (bash-syntax.log, bash-help.log). Native/human acceptance не заявлена. РеальныйUbuntu script/corpus подтверждаетсяCIпослеpush; результаты добавляются вfinal evidence. Executable100755 иLF проверяютсяGit. Self-review иgit diff --checkPASS.

## Bounded-suite follow-up scope

Large Engine/Economy batches can exceed300s even when individual exact cases fit. platform-baseline.py now records an invalid timed-out batch attempt and recursively bisects its unique display-name selection; only completed leaf partitions contribute suite coverage. Singleton timeouts still fail, no test skipped and no timeout increased. Names/multiplicity assertions retained. This is validation orchestration, not an Engine performance claim.

Validation: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-adaptive-runner/adaptive-verification.json: 273 real Contracts cases passed in six leaf partitions, exact multiplicity/coverage equal discovery. Three parent timeouts were explicitly injected without starting VSTest, to exercise recursive orchestration; these are not real performance measurements. All leaf TRX are actual VSTest runs. A singleton timeout still produces failure.
