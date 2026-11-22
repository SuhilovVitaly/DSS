---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0001-results-modal
ticket: EP-0042-US-0001-TK-0005-outcome-lifecycle
title: Подтверждённый итог и однократный учёт вылета
stage: implemented
layer: engine
depends_on: [EP-0042-US-0001-TK-0004-results-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Подтверждённый итог и однократный учёт вылета

## Why

После победы, поражения, ничьей и тайм-аута вместо старой кнопки выхода появляется «Результаты боя». Она открывает модальный экран с подтверждённым исходом и потерями. Окончание боя не открывает окно автоматически. Клик по юниту или обломкам на тактической карте не запускает лут. Esc, клик снаружи, горячие клавиши и события диспетчера не пропускают обязательный экран. Добыча доступна только при штатном праве на неё. Без права доступны итоги и явное завершение; при гибели своего носителя явное завершение ведёт в существующий экран гибели. Последствия боя применяются один раз.

## Decisions

Точная запись исходных решений: [D-0042-01, D-0042-02](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs` | новый | Рассчитать и удержать immutable подтверждённый итог по BattleId. Принять только корректное открытие и явное завершение, не начинать timer или RNG из одного запроса открыть. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | ExecuteTactical, ExitTacticalBattle | Гарантировать единственный переход из terminal battle и существующий settlement, исключить release до явного завершения результата. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | ExecuteDemoCampaign, BuildDemoCampaignProjection | Подключить авторитетный read/status и commands. Сохранить victory-only entitlement, gate мира и существующие storage semantics. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | BeginPostBattleLoot, PrepareFinishBattleLoot | Развести frozen право на лут и показ модального окна, не генерировать предметы повторно. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0001Tk0005Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Реализация проектируемого WorldEncounterBattleContracts API TK-0004. Состояния OutcomeReady, Presented, Released с однократным действием по BattleId.

## Implementation steps

1. Рассчитать и удержать immutable подтверждённый итог по BattleId. Принять только корректное открытие и явное завершение, не начинать timer или RNG из одного запроса открыть.
2. Гарантировать единственный переход из terminal battle и существующий settlement, исключить release до явного завершения результата.
3. Подключить авторитетный read/status и commands. Сохранить victory-only entitlement, gate мира и существующие storage semantics.
4. Развести frozen право на лут и показ модального окна, не генерировать предметы повторно.
5. Потери, расход и гибель носителя берутся из подтверждённого боя и переносятся однажды. Мир паузится до release. Поведение текущей debug ветки сохраняется, если baseline включает debug loot. Стойкий save non-victory дополняет US-0006.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Потери, расход и гибель носителя берутся из подтверждённого боя и переносятся однажды. Мир паузится до release. Поведение текущей debug ветки сохраняется, если baseline включает debug loot. Стойкий save non-victory дополняет US-0006.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsLifecycle_SettlesOnceAndNeverAwardsDefeat`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0001-tk0005-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsLifecycle_SettlesOnceAndNeverAwardsDefeat' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0004-results-contract](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0004-results-contract/EP-0042-US-0001-TK-0004-results-contract.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Текущий `ExecuteTactical` сразу замораживает выигранную встречу. Расширяем allowlist на контракты запуска и `SimulationEngine.WorldEncounterBattle.cs`, чтобы новый клиент мог включить обязательное подтверждение при старте обоих типов боя. Legacy-вызовы сохраняют прежний маршрут. Боевые действия по-прежнему возвращают settlement при завершении. Именованный тест проверяет обе ветки выдачи по реальной встрече.

## Исполнение

- Обязательный режим выбирается новым клиентом при старте боя. Движок замораживает исход и однократно рассчитывает потери. Выход до подтверждения отклоняется.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли. Именованные тесты запланированы на завершение US-0001. Восстановление стадий из сохранения остаётся задачей US-0006.
