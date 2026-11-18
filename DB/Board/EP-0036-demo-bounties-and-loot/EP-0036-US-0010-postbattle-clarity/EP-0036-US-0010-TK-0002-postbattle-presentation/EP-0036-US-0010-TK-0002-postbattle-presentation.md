---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0010-postbattle-clarity
ticket: EP-0036-US-0010-TK-0002-postbattle-presentation
title: Самоуничтожение, вреки и подсказка сбора на карте
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0010-TK-0001-selfdestruct-log, EP-0036-US-0007-TK-0004-postbattle-map]
files_touched: 6
serves: [AC-01, AC-02]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Самоуничтожение, вреки и подсказка сбора на карте

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

[EP-0036-US-0010-postbattle-clarity](../EP-0036-US-0010-postbattle-clarity.md). Самоуничтожение, вреки и подсказка сбора на карте. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs` | Destroyed→Log.CarrierDestroyed:41. | SelfDestructed→Log.CarrierSelfDestructed. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Plates.cs` | Подписи юнитов. | Метка врека дрона/носителя по frozen source kind US7. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | OverlayClick, US7 TK4 LootOpen ветка. | Баннер-подсказка и кнопка «Завершить сбор». |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Log.CarrierDestroyed существует. | RU ключи ниже. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | То же. | EN ключи ниже. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0010Tk0002Tests.cs` | Новый. | Named tests ниже. |

Разрешены 6 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Ключи: Log.CarrierSelfDestructed, Loot.WreckDrone, Loot.WreckCarrier, Loot.Hint ("Победа. Нажмите на выведенный из строя дрон или врек, чтобы осмотреть добычу; подлетать не нужно"), Loot.Finish. Кнопка «Завершить сбор» вызывает тот же confirm flow FinishBattleLoot US7 TK4, не отдельный путь выхода.

## Implementation steps

1. BattleLogFormatter: SelfDestructed отображается отдельной строкой «Носитель X самоуничтожился — крыло потеряно», в той же вкладке журнала.
2. На карте в LootOpen подписывать вреки по frozen source kind (drone wreck / carrier wreck), Disabled-дрон — как доступный для осмотра; уничтоженные предметы не раскрывать.
3. Пока LootOpen и нет открытого окна — баннер Loot.Hint и кнопка Loot.Finish; кнопка и Escape идут в единственный confirm flow US7 TK4. В бою (до победы) баннер не показывается.
4. Load в LootOpen восстанавливает те же метки/баннер без команд.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0010Tk0002Tests.SelfDestructLogLineDistinct`
- `DemoEp0036Us0010Tk0002Tests.WreckLabelsByFrozenKind`
- `DemoEp0036Us0010Tk0002Tests.HintOnlyInLootOpen`
- `DemoEp0036Us0010Tk0002Tests.FinishButtonUsesConfirmFlow`
- `DemoEp0036Us0010Tk0002Tests.RuEnKeyParity`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0010-TK-0002-postbattle-presentation/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0010Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Scope correction
Loot marker renderer is BattleMapRenderer.cs and hint/finish owner is BattleLootWindow.cs. Also retain the existing journal in LootOpen overlay and route its click/wheel via TacticalBattleScreen.Loot.cs/Screen.cs so the new self-destruct explanation remains inspectable after victory.

## Execution
Distinct RU/EN self-destruct journal text, frozen wreck-kind labels, victory inspection hint (no approach needed), Finish collection through existing confirmation flow. Loot overlay retains existing scrollable battle journal. Client/test build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us10-tk2-build.log); tests deferred to US10 boundary.
