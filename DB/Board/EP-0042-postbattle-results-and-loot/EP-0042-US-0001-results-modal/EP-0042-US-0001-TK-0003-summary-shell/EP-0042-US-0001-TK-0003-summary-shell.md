---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0001-results-modal
ticket: EP-0042-US-0001-TK-0003-summary-shell
title: Модаль с подтверждённым итогом
stage: draft
layer: client
depends_on: [EP-0042-US-0001-TK-0002-mandatory-route]
files_touched: 3
serves: [AC-01, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Модаль с подтверждённым итогом

## Why

После победы, поражения, ничьей и тайм-аута вместо старой кнопки выхода появляется «Результаты боя». Она открывает модальный экран с подтверждённым исходом и потерями. Добыча доступна только при штатном праве на неё. Без права доступны итоги и явное завершение; при гибели своего носителя явное завершение ведёт в существующий экран гибели. Последствия боя применяются один раз.

## Decisions

Точная запись исходных решений: [D-0042-01, D-0042-02](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | новый | Собрать модальный контейнер, итог, ход/потери/расход, кнопку «На карту» и область дочернего списка. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | ExitAsync, State | Передавать immutable outcome отчёта и сведения о праве на лут без двойного settlement. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0001Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемый BattleResultsScreen получает подтверждённый snapshot, связь с Flow и действие явного выхода.

## Implementation steps

1. Собрать модальный контейнер, итог, ход/потери/расход, кнопку «На карту» и область дочернего списка.
2. Передавать immutable outcome отчёта и сведения о праве на лут без двойного settlement.
3. У ничьей, тайм-аута и поражения нет collect/arrest. Сценарии диагностического боя не получают мировую добычу из одного названия Victory. Результаты не берутся с предыдущей встречи.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

У ничьей, тайм-аута и поражения нет collect/arrest. Сценарии диагностического боя не получают мировую добычу из одного названия Victory. Результаты не берутся с предыдущей встречи.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResults_AllOutcomes_RenderCorrectLedger`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0001-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResults_AllOutcomes_RenderCorrectLedger' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0002-mandatory-route](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0002-mandatory-route/EP-0042-US-0001-TK-0002-mandatory-route.md). Точный allowlist и owning test project указаны выше.
