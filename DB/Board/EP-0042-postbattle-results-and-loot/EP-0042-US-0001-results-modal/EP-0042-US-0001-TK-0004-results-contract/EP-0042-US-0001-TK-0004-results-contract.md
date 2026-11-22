---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0001-results-modal
ticket: EP-0042-US-0001-TK-0004-results-contract
title: Контракт результата без обязательной добычи
stage: implemented
layer: contracts
depends_on: []
files_touched: 4
serves: [AC-01, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Контракт результата без обязательной добычи

## Why

После победы, поражения, ничьей и тайм-аута вместо старой кнопки выхода появляется «Результаты боя». Она открывает модальный экран с подтверждённым исходом и потерями. Добыча доступна только при штатном праве на неё. Без права доступны итоги и явное завершение; при гибели своего носителя явное завершение ведёт в существующий экран гибели. Последствия боя применяются один раз.

## Decisions

Точная запись исходных решений: [D-0042-01, D-0042-02](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/WorldEncounterBattleContracts.cs` | WorldEncounterSnapshot | Добавить projectable BattleResultsSnapshot для подтверждённого результата и стадии; все outcome kinds, battle identity, данные об итоговом состоянии и право на добычу. |
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | ILootCampaignCommand / FinishBattleLootCommand | Описать проектируемые действия открытия/закрытия результата отдельно от боевых ходов, сохранив режим legacy loot. |
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | DemoCampaignSnapshot | Подключить опциональную frozen result проекцию к transport snapshot. |
| `tests/DeepSpaceSaga.Contracts.Tests/PostbattleResults/EP0042Us0001Tk0004Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемые OpenBattleResultsCommand и AcknowledgeBattleResultsCommand адресуют BattleId/ExpectedRevision/CommandId; opt-in immutable BattleResultsSnapshot не требует LootSessionSnapshot.

## Implementation steps

1. Добавить projectable BattleResultsSnapshot для подтверждённого результата и стадии; все outcome kinds, battle identity, данные об итоговом состоянии и право на добычу.
2. Описать проектируемые действия открытия/закрытия результата отдельно от боевых ходов, сохранив режим legacy loot.
3. Подключить опциональную frozen result проекцию к transport snapshot.
4. Не выдавать право на лут только по исходу. Представить Victory/Defeat/Draw/Timeout, отсутствие своего carrier и данные старого клиента с null defaults.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Не выдавать право на лут только по исходу. Представить Victory/Defeat/Draw/Timeout, отсутствие своего carrier и данные старого клиента с null defaults.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsContract_FourOutcomesAndLegacyDefaults`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0001-tk0004-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsContract_FourOutcomesAndLegacyDefaults' --results-directory $run
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/WorldEncounterBattleContracts.cs src/DeepSpaceSaga.Contracts/BattleLootContracts.cs src/DeepSpaceSaga.Contracts/DemoCampaign.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: нет новых тикетов перед этой задачей. Точный allowlist и owning test project указаны выше.

## Исполнение

- Добавлены опциональная проекция результата и отдельные команды открытия и подтверждения. Переходы между стадиями реализуются в TK-0005.
- Сборка Contracts Tests прошла без ошибок и предупреждений. Scoped whitespace format и `git diff --check` прошли.
- Тесты запланированы на завершение US-0001.
