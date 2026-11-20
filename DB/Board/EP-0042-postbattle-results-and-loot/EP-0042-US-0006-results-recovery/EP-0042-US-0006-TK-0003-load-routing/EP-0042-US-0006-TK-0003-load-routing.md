---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
ticket: EP-0042-US-0006-TK-0003-load-routing
title: Блокировка сохранения и маршрутизация после Load
stage: draft
layer: client
depends_on: [EP-0042-US-0001-TK-0002-mandatory-route, EP-0042-US-0005-TK-0003-footer-confirmation, EP-0042-US-0006-TK-0002-checkpoint-transport]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Блокировка сохранения и маршрутизация после Load

## Why

Пока идут операции, F5/F9 и эквиваленты Save/Load недоступны. Диспетчер и слой сохранения отклоняют обходы, autosave не записывает промежуточные операции. После завершения и подтверждения записи доступ возвращается. Загрузка сохраняет факт открытия модального окна, подтверждённый исход и текущие строки, взятые/оставленные трофеи, провалы, аресты и RNG cursor. Возврат не повторяет бой, добычу или settlement. Старое открытое EP-0036 сохранение восстанавливается в новом окне с исходными остатками; закрытое не переоткрывается. Ошибка durable записи позволяет повторить тот же checkpoint/receipt безопасно; неуспех записи не выдаётся за подтверждённое сохранение.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-08, D-0042-09](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | RecoverBattleFlowAsync, dispatcher F5/F9 | Защитить все интерактивные маршруты Save/Load во время busy; recover pre-results или opened modal по сохранённой стадии. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | AdoptRecoveredLoot, ExitAsync | Принимать только согласованный battle id и durable стадию; не делать новый старт боя при загрузке результатов. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | будущий контейнер модали | Восстановить результат и строки, разрешить F5/F9 после завершения, при ошибке записи показать существующий повтор и остаться в модали. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0006Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; внутренний recover flow использует existing session projection; optional result DTO потребуется только если выявит TK-0001.

## Implementation steps

1. Защитить все интерактивные маршруты Save/Load во время busy; recover pre-results или opened modal по сохранённой стадии.
2. Принимать только согласованный battle id и durable стадию; не делать новый старт боя при загрузке результатов.
3. Восстановить результат и строки, разрешить F5/F9 после завершения, при ошибке записи показать существующий повтор и остаться в модали.
4. Legacy open восстанавливается модалью, closed не перезапускается. Смена сессии не переносит старые UI операции или подтверждение. Мониторные проверки проводит владелец отдельно.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Legacy open восстанавливается модалью, closed не перезапускается. Смена сессии не переносит старые UI операции или подтверждение. Мониторные проверки проводит владелец отдельно.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsLoad_OpensModalWithoutRestartingBattle`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0006-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsLoad_OpensModalWithoutRestartingBattle' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0002-mandatory-route](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0002-mandatory-route/EP-0042-US-0001-TK-0002-mandatory-route.md), [EP-0042-US-0005-TK-0003-footer-confirmation](../../EP-0042-US-0005-safe-exit/EP-0042-US-0005-TK-0003-footer-confirmation/EP-0042-US-0005-TK-0003-footer-confirmation.md), [EP-0042-US-0006-TK-0002-checkpoint-transport](../../EP-0042-US-0006-results-recovery/EP-0042-US-0006-TK-0002-checkpoint-transport/EP-0042-US-0006-TK-0002-checkpoint-transport.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Диспетчер удерживает ввод модали на время сохранения или смены сессии. Восстановленный Open открывает окно напрямую, AwaitingOpen возвращает в терминальную тактическую панель. Legacy open проходит только подтверждение стадии без повторной выдачи. Признак режима pending encounter хранится в совместимой проекции Contracts. Новый тест использует настоящий headless диспетчер и файл слота.

## Исполнение

- Загрузка сохраняет модальную стадию. Legacy open открывается в новой модали. При активном таймере или незаписанном результате закрыты F5/F9 и прямые события. Во время допустимой навигации вводу не даётся стартовать другую транзакцию.
- Сборка Client Tests и diff прошли. Scoped whitespace прошёл для новых и изменённых production-файлов и нового теста. Отдельный прежний формат строк fixture BattleNavigationTests зафиксирован в US-0001/TK-0002.
