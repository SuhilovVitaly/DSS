---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
ticket: EP-0042-US-0006-TK-0001-save-ledger
title: Снимок завершённых состояний и совместимость
stage: draft
layer: engine
depends_on: [EP-0042-US-0005-TK-0002-capacity-exit-rule, EP-0042-US-0001-TK-0005-outcome-lifecycle]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Снимок завершённых состояний и совместимость

## Why

Пока идут операции, F5/F9 и эквиваленты Save/Load недоступны. Диспетчер и слой сохранения отклоняют обходы, autosave не записывает промежуточные операции. После завершения и подтверждения записи доступ возвращается. Загрузка сохраняет факт открытия модального окна, подтверждённый исход и текущие строки, взятые/оставленные трофеи, провалы, аресты и RNG cursor. Возврат не повторяет бой, добычу или settlement. Старое открытое EP-0036 сохранение восстанавливается в новом окне с исходными остатками; закрытое не переоткрывается. Ошибка durable записи позволяет повторить тот же checkpoint/receipt безопасно; неуспех записи не выдаётся за подтверждённое сохранение.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-08, D-0042-09](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/BattleLootData.cs` | Validate; Map, Sources, DrawCount | Дополнить долговечные состояния ExtractedWaiting, сохранять их один draw и исключить Running. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | Battle loot и encounter sections | Выделить опциональную запись результата без добычи; использовать прочитанные immutable outcome/units и добавить совместимый признак стадии при необходимости. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | ValidateBattleLootForLoad | Проверить стабильную связь BattleId/источник/количество, legacy open/closed и settled non-victory; запретить смешение с чужим боем. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | CaptureSaveStateCore, LoadScenario | Сохранение допустимо только со stable ledger, восстановление результата non-victory не требует активной боевой сессии. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0006Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемые optional save members со строгой валидацией. Если текущая версия не допускает добавлений, явно оформить миграцию до реализации, сохранив чтение входного Save22.

## Implementation steps

1. Дополнить долговечные состояния ExtractedWaiting, сохранять их один draw и исключить Running.
2. Выделить опциональную запись результата без добычи; использовать прочитанные immutable outcome/units и добавить совместимый признак стадии при необходимости.
3. Проверить стабильную связь BattleId/источник/количество, legacy open/closed и settled non-victory; запретить смешение с чужим боем.
4. Сохранение допустимо только со stable ledger, восстановление результата non-victory не требует активной боевой сессии.
5. Не записывать промежуточный timer state. Чтение старых состояний сохраняет их форму и смысл. Допущение crash policy: после падения приложения доступен последний подтверждённый checkpoint; недолговечная незавершённая пачка не восстанавливается. Отдельно проверить отсутствие нового броска при чтении уже завершённой попытки.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Не записывать промежуточный timer state. Чтение старых состояний сохраняет их форму и смысл. Допущение crash policy: после падения приложения доступен последний подтверждённый checkpoint; недолговечная незавершённая пачка не восстанавливается. Отдельно проверить отсутствие нового броска при чтении уже завершённой попытки.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsSave_LegacyOpenClosedAndExtractedWaiting`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0006-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsSave_LegacyOpenClosedAndExtractedWaiting' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/BattleLootData.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0005-TK-0002-capacity-exit-rule](../../EP-0042-US-0005-safe-exit/EP-0042-US-0005-TK-0002-capacity-exit-rule/EP-0042-US-0005-TK-0002-capacity-exit-rule.md), [EP-0042-US-0001-TK-0005-outcome-lifecycle](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0005-outcome-lifecycle/EP-0042-US-0001-TK-0005-outcome-lifecycle.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Save22 получает опциональную стадию, BattleId и расход торпед. Для победы с лутом карта берётся из существующего ledger, для остальных исходов сохраняется одна замороженная карта. Проверки сопоставляют собственный итог с settlement и историю встречи с итогом. Добавлены `BattleResultsData.cs`, файлы восстановления боя и pending encounter, чтобы сохранить режим подтверждения на обоих маршрутах.

## Исполнение

- Save22 сохраняет только подтверждённый исход и стадию. Войны и таймеры не сериализуются как активное выполнение. Проверены обязательные привязки к собственному settlement, ledger и истории встречи.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли. Legacy и roundtrip назначены на границу US-0006.
