---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0001-results-modal
ticket: EP-0042-US-0001-TK-0001-entry-label
title: Кнопка «Результаты боя»
stage: draft
layer: client
depends_on: []
files_touched: 4
serves: [AC-01, AC-02]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Кнопка «Результаты боя»

## Why

После победы, поражения, ничьей и тайм-аута вместо старой кнопки выхода появляется «Результаты боя». Она открывает модальный экран с подтверждённым исходом и потерями. Окончание боя не открывает окно автоматически. Клик по юниту или обломкам на тактической карте не запускает лут. Esc, клик снаружи, горячие клавиши и события диспетчера не пропускают обязательный экран.

## Decisions

Точная запись исходных решений: [D-0042-01, D-0042-02](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs` | DrawOutcome | Заменить кнопки выхода в терминальном состоянии на событие открытия результатов. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | OnKeyDown, OnMouseDown | Обработать вход по кнопке, убрать прежние прямые пути выхода и ввод карты после окончания. |
| `src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs` | enum ScreenEvent | Добавить событие OpenBattleResults. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0001Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемое внутреннее действие OpenBattleResults. Для тактической кнопки использовать ключ TacticalBattle.Results, определяемый тикетом US-0002/TK-0004.

## Implementation steps

1. Заменить кнопки выхода в терминальном состоянии на событие открытия результатов.
2. Обработать вход по кнопке, убрать прежние прямые пути выхода и ввод карты после окончания.
3. Добавить ScreenEvent.OpenBattleResults. Использовать TacticalBattle.Results вместо TacticalBattle.ToMap только на тактическом экране.
4. Во всех терминальных состояниях публиковать ровно одно действие открытия. Клик, клавиатура и assisted activation проходят одну проверку. Тест не требует открыть игровое окно.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Во всех терминальных состояниях публиковать ровно одно действие открытия. Клик, клавиатура и assisted activation проходят одну проверку. Тест не требует открыть игровое окно.

## Tests

Будущая проверка, не запускалась. Именованный тест: `TerminalOutcome_OpensResultsWithoutExiting`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0001-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~TerminalOutcome_OpensResultsWithoutExiting' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: нет новых тикетов перед этой задачей. Точный allowlist и owning test project указаны выше.
