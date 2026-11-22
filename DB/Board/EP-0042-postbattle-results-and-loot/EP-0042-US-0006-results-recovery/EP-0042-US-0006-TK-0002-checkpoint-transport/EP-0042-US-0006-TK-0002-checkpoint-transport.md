---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
ticket: EP-0042-US-0006-TK-0002-checkpoint-transport
title: Долговечный checkpoint и повтор записи
stage: implemented
layer: local-client
depends_on: [EP-0042-US-0006-TK-0001-save-ledger]
files_touched: 3
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Долговечный checkpoint и повтор записи

## Why

Пока идут операции, F5/F9 и эквиваленты Save/Load недоступны. Диспетчер и слой сохранения отклоняют обходы, autosave не записывает промежуточные операции. После завершения и подтверждения записи доступ возвращается. Загрузка сохраняет факт открытия модального окна, подтверждённый исход и текущие строки, взятые/оставленные трофеи, провалы, аресты и RNG cursor. Возврат не повторяет бой, добычу или settlement. Старое открытое EP-0036 сохранение восстанавливается в новом окне с исходными остатками; закрытое не переоткрывается. Ошибка durable записи позволяет повторить тот же checkpoint/receipt безопасно; неуспех записи не выдаётся за подтверждённое сохранение.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-08, D-0042-09](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | TacticalLootCall, ExecuteDemoCampaignAsync, WriteContinuationCheckpointAsync | Сериализовать save с admission/completion и реальными commit receipts, запрещать ручной Save/Load при busy; повторять запись той же операции без повторной мутации. |
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs` | WriteSaveAsync | Проверять активность до захвата снимка/записи; не считать пропуск успешным autosave. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0006Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; существующий transport/gate переносит optional durable receipt и стабильные сообщения busy/ошибки.

## Implementation steps

1. Сериализовать save с admission/completion и реальными commit receipts, запрещать ручной Save/Load при busy; повторять запись той же операции без повторной мутации.
2. Проверять активность до захвата снимка/записи; не считать пропуск успешным autosave.
3. Промежуточное завершение строк может быть видимо в текущей сессии до общего checkpoint, UI показывает незавершённость записи и не выпускает в мир. После окончания последней операции и записи можно разрешить F5/F9. Не перекрыть ошибку записи сменой identity.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Промежуточное завершение строк может быть видимо в текущей сессии до общего checkpoint, UI показывает незавершённость записи и не выпускает в мир. После окончания последней операции и записи можно разрешить F5/F9. Не перекрыть ошибку записи сменой identity.

## Tests

Будущая проверка, не запускалась. Именованный тест: `LootCheckpoint_RetrySameReceiptAndBlockMidBatch`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0006-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~LootCheckpoint_RetrySameReceiptAndBlockMidBatch' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0006-TK-0001-save-ledger](../../EP-0042-US-0006-results-recovery/EP-0042-US-0006-TK-0001-save-ledger/EP-0042-US-0006-TK-0001-save-ledger.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Save и autosave используют одну блокировку с admission и completion. Отдельный Engine-параметр удерживает мир после подтверждения до успешного checkpoint; для этого расширен allowlist на Engine dispatch и связь checkpoint. Штатное правило последнего живого continuation для погибшего учебного носителя применяется и к новому маршруту.

## Исполнение

- LocalClient синхронизирует выдачу и запись checkpoint с ручным сохранением и autosave. После сбоя повторяет сохранение того же состояния. Финальный переход удерживает мировые команды до записи.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли.
