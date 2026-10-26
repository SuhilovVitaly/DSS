---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0001-headless-build-and-tests
ticket: EP-0015-US-0001-TK-0002-test-portability-fixes
title: "Исправление непереносимых тестов"
stage: draft
execution_status: implemented-local-validation
layer: tests
depends_on: [EP-0015-US-0001-TK-0004-sdk8-client-compilation]
serves: [AC-02, AC-03, AC-04]
created: 2026-10-25
---

# Исправление непереносимых тестов

История: [EP-0015-US-0001-headless-build-and-tests](../EP-0015-US-0001-headless-build-and-tests.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Исправить тесты, которые падают на Linux из-за допущений Windows: регистр путей, разделители, перевод строк, имена исполняемых файлов.

## Шаги

1. Источник списка — таблица из TK-0001.
2. Пути строить только через `Path.Combine`/`Path.Join`; не сравнивать пути как строки с `\`.
3. Если тест сравнивает текст с эталоном — нормализовать `\r\n` → `\n` перед сравнением (в репозитории нет `.gitattributes`, поэтому окончания строк зависят от настроек git у разработчика).
4. Добавить `.gitattributes` с `* text=auto` и явными `binary` для `*.png *.jpg *.otf *.ttf *.trx`.

## Зависимости

[EP-0015-US-0001-TK-0001-linux-build-baseline](../EP-0015-US-0001-TK-0001-linux-build-baseline/EP-0015-US-0001-TK-0001-linux-build-baseline.md)

## Definition of Done

- Все тесты из таблицы TK-0001 с причиной «переносимость» проходят на Linux и на Windows.
- `.gitattributes` добавлен; повторный checkout на Windows и Linux не даёт изменённых файлов в `git status`.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Scope update and Code context 2026-10-26

SDK8 baseline выявил CS8600 L61 и CS8604 L72/L106 в tests/DeepSpaceSaga.Engine.Tests/TradingVoyageFixture.cs: Where(id is not null) не сужает string? sequence в старом компиляторе. Разрешено безопасное явное nullable narrowing (OfType<string>) без изменения соседей/порядка и без null-forgiving обхода. Этот файл и .gitattributes составляют подтверждённый initial allowlist. Новые platform failures добавлять с обоснованием перед правкой. Layer tests, production Engine не меняется. Проверки: SDK8 solution build, trading voyage tests, cross-platform exact-case corpus/count/identity comparison; все skips учитывать явно.

## Execution result 2026-10-26

Where(nullable) заменён на OfType<string>, порядок сохранён, neighbors! удалён. .gitattributes: text=auto, shellLF, binary для изображений/шрифтов/TRX. Полная C#12 Release solution build0 warnings/errors; RepeatableTradingVoyageTests + VoyageSaveLoadContinuityTests + TradingContinuationReviewTests:41/41. Scoped whitespace format, git check-attr и diff check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-portability/ (solution-csharp12-build.log, trading.log, trading.trx, format.log). Команды ограничены300с на процесс. Self-review: null элементы отфильтрованы как прежде, соседи и assertions не изменены.

SDK8 cross-platform count/identity comparison выполняется послеpush и сохраняется в итоговом evidence; этот локальный результат не объявлен Linux PASS. Новые подтверждённые platform failures исправляются дополнительными коммитами этого тикета без переписывания истории. Чистый повторный checkout проверяется CI, посторонние исходные изменения исключены из commit.

## Additional SDK8 finding 2026-10-26

CI40283423491 наобеихОС показалCS1998 вtests/DeepSpaceSaga.Client.Tests/GameSessionNavigationTests.cs:105: Navigation_trajectory_projection_shows_wait_turn_then_straight_to_target — полностьюсинхронныйтест, нообъявленasyncTask. Scopeрасширеннатестовыйфайл: заменитьсигнатурунаvoid, сохраниввсеassertions/геометрию; warningнеподавлять. РанееисправленныеCS0121/CS8600/CS8604 исчезлиизSDK8CI. НовыйcompilerfindingзакрываетсядополнительнымкоммитомэтогоTK.

SDK8 CS1998 follow-up: synchronous navigation signature repaired; GameSessionNavigationTests36/36 onC#12. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-portability-navigation/navigation.log иnavigation.trx. Assertions unchanged; independentSDK8CIconfirmation follows separatepush. Currentworktree also contains uncommittedfontwork, excluded fromthisfixcommit.

## Linux CI follow-up scope

Run40288631755: Windows Client passed, Linux Client exposed Windows-specific rename-lock assumptions in LocalSessionIntegrationTests.Failed_economic_overwrite; EconomyBalance BalanceReportTests.Atomic_replace_failure has the same assumption. Unix permits rename over an open file. Scope extended to those two test files: retain exact previous-byte and temporary-file assertions; inject a real Unix directory write-permission failure on non-Windows, preserve Windows sharing-lock case, always restore permissions. No production save behavior changed. Harness scope extends EconomyBalance exact cases to singleton partitions because its first100-case batch exceeds300s on both OS; timeout cap remains300s.

Windows follow-up verification: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-unix-save-portability/client-save.trx and economy-save.trx each1/1; scoped format.log exit0. Unix permission branches compile with platform guards and await actual CI execution. Existing byte preservation, temp cleanup and later successful save assertions retained.
