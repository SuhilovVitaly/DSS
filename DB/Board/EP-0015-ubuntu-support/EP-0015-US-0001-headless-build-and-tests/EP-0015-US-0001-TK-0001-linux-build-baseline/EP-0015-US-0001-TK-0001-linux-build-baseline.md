---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0001-linux-build-baseline
title: "Базовый прогон сборки и тестов на Ubuntu"
stage: draft
layer: infra
depends_on: []
serves: [AC-01, AC-02]
created: 2026-10-25
---

# Базовый прогон сборки и тестов на Ubuntu

История: [EP-0015-US-0001-headless-build-and-tests](../EP-0015-US-0001-headless-build-and-tests.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Первый честный замер: что собирается и что падает на Ubuntu без каких-либо правок.

## Шаги

1. Ubuntu 24.04 x64, .NET 8 SDK (`global.json`: 8.0.100, `rollForward: latestMajor`).
2. `dotnet restore` → `dotnet build -c Release` → `dotnet test` по каждому тестовому проекту отдельно.
3. Сравнить число пройденных, упавших и пропущенных тестов с Windows-прогоном на том же коммите.
4. Каждое расхождение записать в таблицу «тест → причина → тикет эпика, который его закрывает».

## Зависимости

Нет.

## Definition of Done

- Таблица расхождений приложена к тикету; для каждого упавшего теста указан закрывающий тикет.
- Ни один файл в `src/` не изменён в этом тикете.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Scope update 2026-10-26

Локальная Ubuntu отсутствует: WSL не установлен (проверено вне sandbox), docker/podman не найдены. Для получения настоящего baseline scope расширен на минимальный GitHub Actions harness. Production src не меняется. CI сравнивает тот же commit на Ubuntu 24.04 и Windows 2022 с SDK 8.0.x; native desktop/audio это не доказывает.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| .github/workflows/ubuntu-support.yml | Отсутствовал | Matrix Ubuntu24.04/Windows2022, .NET8, upload evidence |
| tools/ci/platform-baseline.py | Отсутствовал | Restore/build, VSTest exact method partitions, 300s process-tree cap, JSON/TRX/logs |
| DB/Board/EP-0015-ubuntu-support/ExecutionStatus.md | Подготовка | Фактические результаты и blockers |

## Assumptions and validation

- CI output использует абсолютный sibling artifacts/validation/ep-0015-ubuntu-support/<run> относительно checkout; это Linux/hosted-runner аналог локального D:/DeepSpaceSaga/artifacts/validation. Локальные диагностические файлы остаются только в заданном D: root.
- Каждый partition задаёт точные FullyQualifiedName из --list-tests; theory rows выполняются вместе. Ненулевой exit, отсутствие TRX и zero selected tests не считаются PASS. Никаких silent platform skips.
- Harness публикуется отдельным коммитом этого тикета, после выполнения CI результаты фиксируются следующим коммитом того же тикета. До получения baseline следующий тикет не начинается.
