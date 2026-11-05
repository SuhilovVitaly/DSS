---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
ticket: EP-0015-US-0001-TK-0002-content-definitions
title: content-definitions
stage: done
layer: engine
depends_on: ["EP-0015-US-0001-TK-0001-content-data"]
files_touched: 4
serves: [AC-01, AC-03]
created: 2026-11-05
revision: 3
---

# content-definitions

## Why
Типизированные определения вместо словарей JSON.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs` | new | Records: правила, рамы, пушки (включая необязательный `rangeBonus`), двигатели, щиты, плиты, пилоты (число | [min,max]), компоновки, сценарии с целью, ИИ |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleCatalog.cs` | new | Упорядоченные read-only каталоги |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleConstants.cs` | new | Закрытый список определений Spec из AC-01 со ссылками |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DefinitionsTests.cs` | new | Равенство определений на фикстуре; порядок ключей |

## Public API after the change
`TacticalBattleCatalog`; типы: целые — урон, корпус, броня, навыки, слоты, `piercing`, `defense`, `hitBonus`; остальное `double`. При чтении целое поле, записанное как 1.0, — ошибка данных (как в Python-валидации).

## Implementation steps
1. Records по таблицам Data/README и Modules/*/README.
2. Каталоги и словари сценариев/тактик — `IReadOnlyList` в порядке JSON с индексом по id.
3. JSON-числа читать как double и проверять целость там, где Spec требует целое; bool в числовом поле — ошибка.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01, AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Execution — 2026-11-05

Определения всех данных, ordered immutable каталоги и закрытые константы Spec реализованы в четырёх запланированных файлах. Навык хранит признак диапазона отдельно: [50,50] не превращается в фиксированный навык и сохраняет будущий RNG draw. Catalog копирует входную коллекцию; порядок итерации не зависит от словаря, ID сравниваются Ordinal. Проверены production weapon fixture, optional rangeBonus и отказ int-поля 40.0.

Validation: `dotnet build DeepSpaceSaga.sln --no-restore --nologo -v:minimal -m:1` PASS, 0 warnings/errors; `dotnet test ...Engine.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~TacticalBattle.DefinitionsTests --results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-definitions-01/tests --logger trx` — 3/3 PASS. Scoped format whitespace `--verify-no-changes` и diff check PASS. Evidence: абсолютный каталог `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-definitions-01/` (build.log, tests.log, format-verify.log, tests/*.trx).

Regression selection: полный свежий baseline 5402/5402 — ContentValidation.md, опубликованный TK-0001. В TK-0002 изменены только новые пока не подключённые типы; существующие runtime-пути и прежние тесты не изменены. Эти неизменные наборы не повторялись; текущая сборка и все новые owning tests проверены. Self-review выполнен, independent/native/Linux review не заявляются. SHA и push проверяются Git после создания коммита.
