---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0001-linux-build-baseline
title: "Базовый прогон сборки и тестов на Ubuntu"
stage: draft
acceptance_status: open
execution_status: measured
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

## Baseline harness refinement

Первый локальный Engine partition из100 методов достиг300с (exit124): в него попали все data rows corpus/balance. Остальные уже выполненные partitions дали183/183 и151/151; timeout не объявлен PASS. Harness уточнён до точных DisplayName из --list-tests: тяжёлые CorpusTests/CombatBalanceReport theory rows выполняются отдельно, остальные — по100 cases. TRX testName сверяется с полным selected-case набором, zero tests не считаются PASS. Каждая отдельная theory row по-прежнему ограничена300с. Поддержаны --project и --skip-build для ограниченного восстановления, не для скрытого исключения тестов.

Точный DisplayName-фильтр с аргументами проверен локально: ApproachCommandTests.Invalid_target_is_rejected(targetId: null, reason: "missing_target") — ровно1/1 test по TRX. Потребовалось кодирование запятых/кавычек для CLI/MSBuild и escape операторов VSTest: https://github.com/microsoft/vstest/blob/main/docs/filter.md и https://learn.microsoft.com/en-gb/dotnet/core/testing/selective-unit-tests?pivots=xunit.

В CI SDK8 устанавливается в изолированный каталог с проверкой фактически выбранной версии8.0.x: global.json rollForward=latestMajor иначе может выбрать предустановленный более новый SDK. Обоснование install-dir: https://github.com/actions/setup-dotnet/blob/main/README.md#environment-variables.

Локальный исходный Windows baseline SDK10.0.400: Release build0 warnings/errors; Contracts273/273; Motion251/251; Client2322/2322. Логи/TRX: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-baseline/. Это отдельный результат, не сравнительный SDK8 CI.

## Exact-case harness verification

Contracts corpus обнаружил повторный разбор nested JSON/quotes в CLI/MSBuild. Фильтры перенесены в XML RunSettings/RunConfiguration/TestCaseFilter; на CLI передаётся только абсолютный путь. Повторный полный Contracts exact-case прогон:273/273, expected/actual TRX names совпали для всех трёх partitions. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-runsettings-contracts/. Неудачный предыдущий harness-прогон хранится отдельно (20261026-exact-contracts) и не объявлен product/test defect или PASS.

CI40281943899 подтвердил restore0 и build1 на обоих SDK8 runners. Исходные compiler logs не были доступны из-за ошибки upload-artifact: относительный ../ path запрещён самим action. Исправлено на нормализованный абсолютный path через step output; при ненулевом exit последние40 строк command log также выводятся в job log. Это исправление harness, а не сокрытие ошибки сборки. Для baseline использованы read-only GitHub tools/API; отмена промежуточного прогона через credentials была отклонена auto-review и не выполнена, обход не применялся.

## Execution result — baseline measured

COMPLETE только измерение baseline; это не PASS сборки или приёмка истории. На commit13cdb4a8bc3718d1a69583b89e56be18c6866442 оба SDK8.0.427 runners выполнили restore0/build1. Тесты SDK8 NOT RUN из-за compile failure. Ни один src файл в этом тикете не менялся.

| Ошибка | Ubuntu24.04 и Windows2022 | Закрывающий тикет |
|---|---|---|
| TacticalMapDepthRenderer.cs:420 CS0121 | Одинаковая неоднозначность Skia gradient overload | US-0001-TK-0004 |
| GameSessionScreen.cs:1867 CS0121 | Одинаковая неоднозначность Skia gradient overload | US-0001-TK-0004 |
| TradingVoyageFixture.cs:61/72/106 CS8600/CS8604 | Nullable narrowing старого компилятора | US-0001-TK-0002 |
| Все SDK8 test projects | NOT RUN: solution build failed | ПослеTK-0004/TK-0002, затемTK-0003 |
| Local Engine method partitions000/010 | exit124 при300с; остальные12 завершились | Повтор exact-case coverage через harness этогоTK |

CI: https://github.com/SuhilovVitaly/DSS/actions/runs/40282243373 ; Ubuntu job121437891188 и Windows job121437890935, artifact IDs12632920718/12632093914. Job logs сохранены в D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-baseline/ci-121437891188.log и ci-121437890935.log.

Локально SDK10: build0 warnings/errors, Contracts273/273, Motion251/251, Client2322/2322. Engine12 partitions прошли,2timeout. Отдельное exact-case Contracts273/273 с полным совпадением identities. Python AST и scoped git diff --check PASS. Review: self-review. Исходный latestMajorCI40280294407 остаётся отдельным запуском, его результат фиксируется по окончании.

Подготовка Board661f3fe8, harness d026651a/e2cd785c/8eaa3740/13cdb4a8 и новая corrective card3558240c опубликованы и remote verified. Последний коммит содержал только новую карточку: генерация остальных записей прервалась на UTF-8 decode и восстановлена отдельным исправляющим коммитом без переписывания истории. Собственный completion SHA определяется после commit/push через Git.
