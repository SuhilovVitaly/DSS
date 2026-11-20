---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0005-safe-exit
ticket: EP-0042-US-0005-TK-0003-footer-confirmation
title: Футер, предупреждение и единственное действие выхода
stage: draft
layer: client
depends_on: [EP-0042-US-0001-TK-0002-mandatory-route, EP-0042-US-0004-TK-0003-bulk-action, EP-0042-US-0005-TK-0002-capacity-exit-rule]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Футер, предупреждение и единственное действие выхода

## Why

Футер указывает живому носителю свободный трюм и число свободных кают для пленных рядом, включая явный ноль. Каюты берутся из общей ёмкости с действующими резервациями, без клиентского пересчёта; успешный арест обновляет значение. «На карту» недоступна пока хотя бы одна операция идёт, а также пока окончательный результат ещё не подтверждён записью. После этого единственный выход — явная кнопка и, когда остаётся добыча или живой неарестованный пилот, подтверждение со списком оставляемого. Подтверждение окончательно закрывает сбор и исключает дальнейшую выдачу; отмена сохраняет окно и состояние. В окне нет интерфейса выброса/управления грузом. Мёртвому носителю нельзя показывать выдуманное наличие ресурсов.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-09, D-0042-11, D-0042-15](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | будущий футер модали | Вывести authoritative кг и свободные каюты, отдельную подтверждаемую кнопку «На карту». Убрать доступ в выброс груза, показать блокировку выхода/запись. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs` | legacy footer/Discard/Jettison | Перенести нужное подтверждение и удалить прежний вход в управление трюмом из результатов. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | ExitAsync, ConfirmLootDiscard | Прокинуть утверждённую команду выхода и возврат из подтверждения, без дублирования. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs` | будущая строка пилота | Показывать существующую причину при нуле, корректно обновлять счётчик по свежей ревизии. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0005Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Внутренний footer view model потребляет authoritative counts; никаких клиентских объединений occupants.

## Implementation steps

1. Вывести authoritative кг и свободные каюты, отдельную подтверждаемую кнопку «На карту». Убрать доступ в выброс груза, показать блокировку выхода/запись.
2. Перенести нужное подтверждение и удалить прежний вход в управление трюмом из результатов.
3. Прокинуть утверждённую команду выхода и возврат из подтверждения, без дублирования.
4. Показывать существующую причину при нуле, корректно обновлять счётчик по свежей ревизии.
5. Подтверждение перечисляет остатки, включая ExtractedWaiting и живых пилотов; Escape внутри подтверждения может отменить его, но не завершает бой и не выходит на карту. При нуле показывать 0, при уничтожении судна — неприменимость.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Подтверждение перечисляет остатки, включая ExtractedWaiting и живых пилотов; Escape внутри подтверждения может отменить его, но не завершает бой и не выходит на карту. При нуле показывать 0, при уничтожении судна — неприменимость.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsFooter_CabinsRefreshAndNoJettison`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0005-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsFooter_CabinsRefreshAndNoJettison' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0002-mandatory-route](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0002-mandatory-route/EP-0042-US-0001-TK-0002-mandatory-route.md), [EP-0042-US-0004-TK-0003-bulk-action](../../EP-0042-US-0004-parallel-bulk/EP-0042-US-0004-TK-0003-bulk-action/EP-0042-US-0004-TK-0003-bulk-action.md), [EP-0042-US-0005-TK-0002-capacity-exit-rule](../../EP-0042-US-0005-safe-exit/EP-0042-US-0005-TK-0002-capacity-exit-rule/EP-0042-US-0005-TK-0002-capacity-exit-rule.md). Точный allowlist и owning test project указаны выше.

## Исполнение

- Футер показывает ёмкость живого носителя из Engine. Перед закрытием выводится прокручиваемый список несобранного, включая живых пилотов. Уйти после предупреждения можно явной кнопкой. Остаться сохраняет состояние.
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли.
