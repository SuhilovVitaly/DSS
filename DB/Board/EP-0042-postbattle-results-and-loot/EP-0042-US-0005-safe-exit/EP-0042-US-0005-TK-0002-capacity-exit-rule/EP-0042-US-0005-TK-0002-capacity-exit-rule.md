---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0005-safe-exit
ticket: EP-0042-US-0005-TK-0002-capacity-exit-rule
title: Каюты и подтверждаемый список оставляемого
stage: draft
layer: engine
depends_on: [EP-0042-US-0004-TK-0002-cargo-completion, EP-0042-US-0005-TK-0001-capacity-contract]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Каюты и подтверждаемый список оставляемого

## Why

Футер указывает живому носителю свободный трюм и число свободных кают для пленных рядом, включая явный ноль. Каюты берутся из общей ёмкости с действующими резервациями, без клиентского пересчёта; успешный арест обновляет значение. «На карту» недоступна пока хотя бы одна операция идёт, а также пока окончательный результат ещё не подтверждён записью. После этого единственный выход — явная кнопка и, когда остаётся добыча или живой неарестованный пилот, подтверждение со списком оставляемого. Подтверждение окончательно закрывает сбор и исключает дальнейшую выдачу; отмена сохраняет окно и состояние. В окне нет интерфейса выброса/управления грузом. Мёртвому носителю нельзя показывать выдуманное наличие ресурсов.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-09, D-0042-11, D-0042-15](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | BuildBattleLootProjection:153–190 | Публиковать вычисленный freeCabins для парного footer readout; отражать ExtractedWaiting, остатки расходников, неарестованных живых и актуальные причины. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs` | GetAvailablePrisonerCabins:10, PrepareArrestPilot | Сохранить существующий пул общих кают и причину при отсутствии места; правка только при необходимости общего read seam, без пересчёта в UI. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | PrepareFinishBattleLoot | Проверить незавершённые операции, сохранить confirm guard и финальное закрытие/исключение повторных выдач. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | ExecuteDemoCampaign | Устойчиво отклонять прямые конфликтующие команды; проверка выхода не зависит от UI. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0005Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемое использование FreePrisonerCabins из TK-0001 и авторитетных HasUnclaimed/причин отказа; без расширения API ареста.

## Implementation steps

1. Публиковать вычисленный freeCabins для парного footer readout; отражать ExtractedWaiting, остатки расходников, неарестованных живых и актуальные причины.
2. Сохранить существующий пул общих кают и причину при отсутствии места; правка только при необходимости общего read seam, без пересчёта в UI.
3. Проверить незавершённые операции, сохранить confirm guard и финальное закрытие/исключение повторных выдач.
4. Устойчиво отклонять прямые конфликтующие команды; проверка выхода не зависит от UI.
5. Успешный арест уменьшает свободные места, повтор ID ничего не меняет; учитываются занятые, зарезервированные и сломанные каюты. Нехватка места не скрывает список оставляемого.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Успешный арест уменьшает свободные места, повтор ID ничего не меняет; учитываются занятые, зарезервированные и сломанные каюты. Нехватка места не скрывает список оставляемого.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsExit_LeftoversAndSharedCabinCount`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0005-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsExit_LeftoversAndSharedCabinCount' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0004-TK-0002-cargo-completion](../../EP-0042-US-0004-parallel-bulk/EP-0042-US-0004-TK-0002-cargo-completion/EP-0042-US-0004-TK-0002-cargo-completion.md), [EP-0042-US-0005-TK-0001-capacity-contract](../../EP-0042-US-0005-safe-exit/EP-0042-US-0005-TK-0001-capacity-contract/EP-0042-US-0005-TK-0001-capacity-contract.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

`SimulationEngine.BattleResults.cs` публикует пару показателей и для живого собственного носителя после исходов без права на лут. Отдельный справочник кают не вводится. Gate операций перенесён в общий Engine-переход закрытия ledger.

## Исполнение

- Счётчик использует общие исправные каюты, пассажирские резервы и реальных пленных. Закрытие ledger требует окончания операций и подтверждения при остатках. Управляемое окно не принимает команду выброса.
- Повторная сборка Engine Tests, scoped whitespace format и `git diff --check` прошли.
