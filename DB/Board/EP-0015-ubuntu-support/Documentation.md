---
epic: EP-0015-ubuntu-support
title: "Поддержка Ubuntu"
stage: draft
execution_status: verification_in_progress
created: 2026-10-26
source_request: "Перенести EP-0015 в DB/Board, добавить документационную историю и выполнить полностью по EpicExecutionPrompt, с проверками, отдельными коммитами и push каждого тикета."
current_review: execution-validation
---

# EP-0015 — Поддержка Ubuntu

## Scope and source

Пакет из VData/Planning/EP-0015-ubuntu-support перенесён в канонический Board по прямому разрешению пользователя 2026-10-26. Сохранены 4 исходные истории и 12 тикетов; добавлена US-0005 с двумя завершающими документационными тикетами. Дополнительно заведены SDK8 Client и external-settings contentRoot prerequisites. Всего 5 историй,16 тикетов. Исходная папка больше не является источником задач.

Цель: сборка/тесты на Ubuntu, Linux Skia, комплектные шрифты, одинаковая раскладка, звук, открытие папок, переносимые инструменты, XDG/portable paths и self-contained дистрибутив. Полный объём и AC сохранены в карточках.

## Grounding

- Directory.Build.props: net8.0, TreatWarningsAsErrors=true; global.json допускает более новый стабильный SDK. Локально установлен SDK 10.0.400, что не доказывает проверку .NET 8 SDK.
- Directory.Packages.props содержит SkiaSharp 2.88.8, Silk.NET 2.21.0; Linux-native пакет отсутствует. Состав пакетов проверяется перед реализацией.
- На старте upstream codex/ep-0013-debug-tuning-panel, origin=https://github.com/SuhilovVitaly/DSS; HEAD 5c77fccc9bf46ceb19e41b3bf1b710044d237099 совпал с remote через git ls-remote.
- Посторонние изменения: удаления DB/.obsidian/*, изменение тикета EP-0001 и DB/Documentation/00-Process/AGENTS.md, оставшееся VData/. Они исключаются из коммитов эпика.

## Dependency order and coverage

1. [EP-0015-US-0001-headless-build-and-tests](EP-0015-US-0001-headless-build-and-tests/EP-0015-US-0001-headless-build-and-tests.md)
2. [EP-0015-US-0002-client-rendering](EP-0015-US-0002-client-rendering/EP-0015-US-0002-client-rendering.md)
3. [EP-0015-US-0003-platform-integration](EP-0015-US-0003-platform-integration/EP-0015-US-0003-platform-integration.md)
4. [EP-0015-US-0004-linux-distribution](EP-0015-US-0004-linux-distribution/EP-0015-US-0004-linux-distribution.md)
5. [EP-0015-US-0005-project-documentation](EP-0015-US-0005-project-documentation/EP-0015-US-0005-project-documentation.md)

Тикеты выполняются в dependency order внутри истории; US-0005 — последней. [Индекс и AC coverage](Tickets.md), [результаты исполнения](ExecutionStatus.md).

## Decisions, assumptions and gaps

- D-01: пользователь явно разрешил перенос, документационную историю и автономное исполнение/push draft-карточек. Это не approval человеческой приёмки.
- A-01: подготовка Board — служебный коммит по разделу 2 промта; не заменяет реализацию и проверки тикетов.
- A-02: при отсутствии локальной Ubuntu допускается настоящий Ubuntu 24.04 GitHub Actions runner для headless baseline; native desktop/audio/human gates учитываются отдельно.
- G-01: локально WSL не установлен, docker/podman не найдены. Проверка вне sandbox подтвердила отсутствие WSL. Используется реальный GitHub Actions Ubuntu runner; pending и выполненные gates перечислены в ExecutionStatus.
- G-02: исходные карточки не имели Code context/allowlist; уточнять перед каждым тикетом по текущим исходникам, записывая расширения scope.
- G-03: утверждение исходного TK о зависимости NoDependencies/fontconfig требует проверки официального описания пакета перед выбором. Требование обычного NativeAssets.Linux сохраняется.
- G-04: 44100 Гц sample rate из исходного audio TK не может точно представить весь заявленный диапазон до32767 Гц. Принято техническое решение:44100Hz ниже22050Hz,96000Hz на22050..32767Hz;440Hz/100ms остаётся4410 samples.

## Review log

2026-10-26: перенос и дополнение явно разрешены пользователем. Подготовка — self-review, независимое review и native/human approval не заявляются.

## Execution order correction

SDK8 repairs: US-0001-TK-0001 → US-0001-TK-0004 → US-0001-TK-0002 → US-0002-TK-0001 native Skia → US-0001-TK-0003 check/regression → остальныеUS-0002 → US-0003 → US-0004 → US-0005. Native Skia нужна также Performance raster corpus; первоначальный порядок целых историй заменён реальным ticket DAG.
