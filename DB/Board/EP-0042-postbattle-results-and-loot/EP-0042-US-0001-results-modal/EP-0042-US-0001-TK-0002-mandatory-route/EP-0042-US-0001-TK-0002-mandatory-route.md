---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0001-results-modal
ticket: EP-0042-US-0001-TK-0002-mandatory-route
title: Модальный маршрут без обходов
stage: draft
layer: client
depends_on: [EP-0042-US-0001-TK-0001-entry-label, EP-0042-US-0001-TK-0005-outcome-lifecycle]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Модальный маршрут без обходов

## Why

После победы, поражения, ничьей и тайм-аута вместо старой кнопки выхода появляется «Результаты боя». Она открывает модальный экран с подтверждённым исходом и потерями. Окончание боя не открывает окно автоматически. Клик по юниту или обломкам на тактической карте не запускает лут. Esc, клик снаружи, горячие клавиши и события диспетчера не пропускают обязательный экран. Добыча доступна только при штатном праве на неё. Без права доступны итоги и явное завершение; при гибели своего носителя явное завершение ведёт в существующий экран гибели. Последствия боя применяются один раз.

## Decisions

Точная запись исходных решений: [D-0042-01, D-0042-02](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | диспетчер ScreenEvent и CloseTacticalBattle | Открыть дочернюю модаль и заблокировать прямой pop/переход в карту, действия мира и фоновые жесты. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | ExitAsync | Разделить показ результатов, подтверждение выхода и существующий вызов settlement. Повтор не продублирует эффект. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Loot.cs` | ObserveLoot, LootClick | Убрать hit-test источников тактической карты как путь в лут. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | DrawOverlay, ObserveLoot | Сохранить панель исхода до явного открытия; перенести визуализацию лута в новый modal. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0001Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемое состояние клиента OutcomeReady → ResultsModal → Released; изменение состояния через маршрутизатор.

## Implementation steps

1. Открыть дочернюю модаль и заблокировать прямой pop/переход в карту, действия мира и фоновые жесты.
2. Разделить показ результатов, подтверждение выхода и существующий вызов settlement. Повтор не продублирует эффект.
3. Убрать hit-test источников тактической карты как путь в лут.
4. Пока ждём явное открытие, оставить панель исхода и журнал, убрать прежнюю ветку отрисовки активного лута поверх тактики.
5. На F5/F9 действуют правила US-0006. Уничтожение носителя не перехватывает обязательный экран, но сохраняет существующий конечный destination. Внутренний UI teardown при завершении процесса не считается игровым выходом.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

На F5/F9 действуют правила US-0006. Уничтожение носителя не перехватывает обязательный экран, но сохраняет существующий конечный destination. Внутренний UI teardown при завершении процесса не считается игровым выходом.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResults_NoMapBypass_DeathRoutesAfterExplicitExit`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0001-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResults_NoMapBypass_DeathRoutesAfterExplicitExit' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Loot.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0001-entry-label](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0001-entry-label/EP-0042-US-0001-TK-0001-entry-label.md), [EP-0042-US-0001-TK-0005-outcome-lifecycle](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0005-outcome-lifecycle/EP-0042-US-0001-TK-0005-outcome-lifecycle.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Дочернее окно заменяет тактический экран в том же слоте modal stack. Дополнительно затронуты `IScreen.cs`, `BattleResultsScreen.cs` и LocalClient для отдельного события подтверждения и сохранения последствий собственного вылета до навигации. `SimulationEngine.BattleResults.cs` публикует SourceShipId для отличия отладочного боя от собственного вылета. Уточняются прежние headless expectations для кнопки New Battle после терминального исхода.

## Исполнение

- Игра открывает модаль через единственный переход в активном тактическом слоте. Подтверждение закрывает этот слот и запускает обычную маршрутизацию гибели/возврата. LocalClient использует имеющуюся durable запись для выхода из собственного боя.
- Сборка Client Tests и `git diff --check` прошли. Scoped whitespace выявил прежнее форматирование строк 144–145 в BattleNavigationTests.cs вне изменённого блока. Именованные тесты запланированы на завершение истории.
