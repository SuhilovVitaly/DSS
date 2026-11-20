---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0003-timed-loot
ticket: EP-0042-US-0003-TK-0001-operation-contract
title: Команды и снимок операций по строкам
stage: draft
layer: contracts
depends_on: []
files_touched: 2
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Команды и снимок операций по строкам

## Why

Ручной старт по одному источнику и строке: расходники 3 секунды, модули 5, лом 10. Реальное монотонное время движется при паузе игрового мира; часы игры стоят. Каждый модуль/лом использует свой существующий шанс, один бросок и видимый итог. Блокировать повторный старт Running/Failed/Collected; при техническом повторе сохранять ту же identity. Для Running справа от кнопки вертикальная шкала от низа к верху. Для терминального результата строка остаётся, а прогресс прекращается. Арест не входит в 3/5/10 и не запускается автоматически.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-08, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | ILootCampaignCommand, LootSessionSnapshot, LootAttemptStates | Описать ключ BattleId/ObjectId/kind/entryId, режим single/all, операцию, дедлайн или elapsed, durable состояния и команды start/advance/claim с idempotent identity. |
| `tests/DeepSpaceSaga.Contracts.Tests/PostbattleResults/EP0042Us0003Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемые StartLootOperationsCommand(CommandId, ExpectedRevision, BattleId, Selector), AdvanceLootOperationsCommand(CommandId, ExpectedRevision, BattleId), ClaimExtractedLootCommand(CommandId, ExpectedRevision, BattleId, RowKey); snapshots OperationId, RowKey, state, DurationMs и timing. Совместимые additions/defaults; точные имена могут следовать существующему каталогу.

## Implementation steps

1. Описать ключ BattleId/ObjectId/kind/entryId, режим single/all, операцию, дедлайн или elapsed, durable состояния и команды start/advance/claim с idempotent identity.
2. Клиент не передаёт RNG, цену, шанс или абсолютное время завершения. Контракт представляет Running/Failed/Collected/ExtractedWaiting отдельно и поддерживает nullable/empty поля старых снимков. Текущий API ареста оставлен.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Клиент не передаёт RNG, цену, шанс или абсолютное время завершения. Контракт представляет Running/Failed/Collected/ExtractedWaiting отдельно и поддерживает nullable/empty поля старых снимков. Текущий API ареста оставлен.

## Tests

Будущая проверка, не запускалась. Именованный тест: `LootOperationContract_LegacyAndNewSnapshotRoundTrip`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0003-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter 'FullyQualifiedName~LootOperationContract_LegacyAndNewSnapshotRoundTrip' --results-directory $run
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/BattleLootContracts.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: нет новых тикетов перед этой задачей. Точный allowlist и owning test project указаны выше.
