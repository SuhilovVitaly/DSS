---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0012-campaign-lifecycle
ticket: EP-0029-US-0012-TK-0003-finish-and-defeat
title: Маршруты завершения, гибели и retry
stage: approved
approval_kind: workflow-only
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0012-TK-0002-owned-client-entry]
files_touched: 5
serves: [AC-02, AC-03, AC-04]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Маршруты завершения, гибели и retry

## Why

Игрок начинает обучение без обхода ownership, а после боя получает сохранённые последствия. Выход в меню и сбой записи не дают скрытую бесплатную перезагрузку имущества. Этот тикет обеспечивает **Маршруты завершения, гибели и retry**. Источники: [история](../EP-0029-US-0012-campaign-lifecycle.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **client**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/SkiaWindow.cs](../../../../../src/DeepSpaceSaga.Client/UI/SkiaWindow.cs) | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Маршруты завершения, гибели и retry» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Маршруты завершения, гибели и retry» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs) | `src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Маршруты завершения, гибели и retry» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs](../../../../../src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs) | `src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Маршруты завершения, гибели и retry» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0003Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0003Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0012Tk0003Tests ниже с независимым oracle. |

## Public API after the change

No API change; согласованный UI lifecycle.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. После terminal outcome сначала authoritative Exit/settlement/checkpoint, затем navigate. При HP0 terminal screen, при alive map, при confirmed mid tutorial abort menu только после checkpoint. EnsureShipDestroyedScreen не должен уничтожать battle UI раньше завершения checkpoint. Pending блокирует второй exit. Storage error показывает Retry/остаться, не выкидывает session; exit app не обещает сохранение при ошибке. Старый manual Load остаётся разрешён.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-02, AC-03, AC-04 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0012-TK-0002-owned-client-entry](../EP-0029-US-0012-TK-0002-owned-client-entry/EP-0029-US-0012-TK-0002-owned-client-entry.md) — завершить до этого тикета.

## Out of scope

Layers кроме client; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0012Tk0003Tests**.

- `Ep0029Us0012Tk0003Tests.ShipDeathCannotNavigateToLiveMap` — terminal route.
- `Ep0029Us0012Tk0003Tests.NoDronesWithLiveShipReturnsMap` — crew/cargo сохранены.
- `Ep0029Us0012Tk0003Tests.AbortToMenuWritesLossesFirst` — checkpoint до dispose.
- `Ep0029Us0012Tk0003Tests.FailedWriteKeepsRetryVisible` — без ложного сообщения saved.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0012Tk0003Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0012-TK-0003" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0003Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 5 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-02, AC-03, AC-04; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

Implemented 2026-11-15. Создание planning-документа не является implementation, build, test или game launch.

## Execution evidence 2026-11-15

Implementation status: **implemented**. Automated tests are **NOT RUN** in this batch по прямому указанию пользователя (no `dotnet test`, no TDD red/green, no validation agent); named tests written and deferred to the final suite run. Единственная проверка батча — компиляция:

```powershell
dotnet build DeepSpaceSaga.sln --no-restore
# -> Build succeeded. 0 Warning(s), 0 Error(s).
```

Изменено (allowlist):
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` — `ExitAsync` больше не считает mapped `Unavailable` успехом при null `GetTacticalBattleStateAsync`: запоминает attempted BattleId (`CheckpointPending`), следующий exit напрямую повторяет `ExitTacticalBattleAsync(new(NewIdentity("exit"), pendingBattleId))` (LocalClient отдаёт cached accepted receipt без re-settlement), успех очищает pending, повторный storage failure оставляет modal открытым с видимым `Error`; metrics на retry не дублируются.
- `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` — `EnsureShipDestroyedScreen` и polling destroyed-перехода не заменяют активный battle screen (`IsSetup:false`); после успешного Close/New выполнения exit/settlement ставят terminal при HP0 (и map при live carrier); confirmed MainMenu во время tutorial battle сначала выполняет `ExitAsync` (settlement + checkpoint), и только при успехе dispose/переход в меню; при провале остаётся battle modal с Retry. Manual Load на terminal screen не блокируется.
- `src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs` — terminal defeat line `ShipDestroyed.Defeat` (без изменения hit-rect Load/MainMenu и без воскрешения корабля повторными событиями).
- `tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0003Tests.cs` — `ShipDeathCannotNavigateToLiveMap`, `NoDronesWithLiveShipReturnsMap`, `AbortToMenuWritesLossesFirst`, `FailedWriteKeepsRetryVisible`.

Board-only correction (traceability): в `US-0012 TK-0001` добавлена секция `Traceability correction 2026-11-15` с фактическими именами тестов и явной пометкой пропущенного destroyed-checkpoint load варианта; production не менялся.

Named-test focus: destroyed player не заменяет battle UI и после exit ведёт на terminal, не на map; только потерянные дроны при живом носителе ведут на map; подтверждённый abort пишет continuation checkpoint (settlement BattleId, revision 1) до dispose; failed write оставляет battle modal, `CheckpointPending`, непустой `Error`, второй exit использует тот же BattleId и после разблокировки пишет ровно один settlement.

Statuses: implementation **implemented**; automated **NOT RUN (deferred to final suite)**; native RU/EN UI gate **OPEN**; product/human acceptance **OPEN**.

