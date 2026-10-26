---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
title: "Сборка и тесты без клиента на Ubuntu"
stage: draft
dependencies: []
---

# Сборка и тесты без клиента на Ubuntu

Эпик: [EP-0015 — Поддержка Ubuntu](../Documentation.md)

## User story

Как разработчик, я клонирую репозиторий на Ubuntu 24.04 (или 22.04) с .NET 8 SDK, выполняю `dotnet build` и `dotnet test` для движка, общих библиотек и инструментов и получаю тот же результат, что на Windows.

## Зависимости

Нет.

## Критерии приёмки

- **AC-01.** `dotnet build DeepSpaceSaga.sln -c Release` на чистой Ubuntu 24.04 x64 завершается без ошибок и предупреждений (`TreatWarningsAsErrors=true` остаётся включённым).
- **AC-02.** `dotnet test` проходит для `DeepSpaceSaga.Engine.Tests`, `DeepSpaceSaga.Motion.Tests`, `DeepSpaceSaga.Contracts.Tests`, `DeepSpaceSaga.EconomyBalance.Tests`: число пройденных тестов совпадает с прогоном на Windows на том же коммите.
- **AC-03.** Тесты, которые на Linux падают по причинам среды (окно, звук, шрифты), не отключаются молча. Каждый такой тест либо исправлен, либо помечен явной причиной и ссылкой на тикет этого эпика.
- **AC-04.** Тесты из `tools/DeepSpaceSaga.Performance.Tests`, которые запускают `dotnet` и `git` как процессы, проходят на Linux.
- **AC-05.** Скрипт `tools/check-linux.sh` воспроизводит проверку одной командой и пишет результаты (`.trx`) в каталог, переданный аргументом.

## Тикеты

- [EP-0015-US-0001-TK-0001-linux-build-baseline](EP-0015-US-0001-TK-0001-linux-build-baseline/EP-0015-US-0001-TK-0001-linux-build-baseline.md) — Базовый прогон сборки и тестов на Ubuntu
- [EP-0015-US-0001-TK-0002-test-portability-fixes](EP-0015-US-0001-TK-0002-test-portability-fixes/EP-0015-US-0001-TK-0002-test-portability-fixes.md) — Исправление непереносимых тестов
- [EP-0015-US-0001-TK-0003-linux-check-script](EP-0015-US-0001-TK-0003-linux-check-script/EP-0015-US-0001-TK-0003-linux-check-script.md) — Скрипт проверки check-linux.sh
