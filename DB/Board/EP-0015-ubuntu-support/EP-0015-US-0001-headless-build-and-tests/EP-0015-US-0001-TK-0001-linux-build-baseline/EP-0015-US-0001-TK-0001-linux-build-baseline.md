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
