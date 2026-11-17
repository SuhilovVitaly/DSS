---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0010-postbattle-clarity
ticket: EP-0036-US-0010-TK-0001-selfdestruct-log
title: Запись самоуничтожения носителя в журнале боя
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0007-TK-0002-carrier-selfdestruct]
files_touched: 3
serves: [AC-01]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Запись самоуничтожения носителя в журнале боя

## Why

[EP-0036-US-0010-postbattle-clarity](../EP-0036-US-0010-postbattle-clarity.md). Запись самоуничтожения носителя в журнале боя. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | enum TacticalBattleLogKind:11, последний член Moved=17. | Append-only SelfDestructed=18; существующие значения не менять. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | Add(TacticalBattleLogKind.Destroyed…):167/208 для обычной гибели. | Одна запись SelfDestructed в selfdestruct-пути US7 TK2. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0010Tk0001Tests.cs` | Новый. | Named tests ниже. |

Разрешены 3 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

TacticalBattleLogKind.SelfDestructed = 18 (append-only; contracts enum добавляется в этом engine-тикете, т.к. единственный потребитель — engine log). Запись содержит target=carrier id и unit=Describe(carrier). Outcome reason остаётся TacticalCarrierDestroyed (US7 TK2), отдельный TacticalOutcomeReason не вводится.

## Implementation steps

1. В точке самоуничтожения US7 TK2 (после перехода Hull=0 по правилу собственного крыла) добавить ровно одну запись SelfDestructed вместо обычной Destroyed для этого носителя.
2. Повторный tick/повтор хода/Load не добавляют вторую запись; обычная гибель носителя от огня по-прежнему пишет Destroyed.
3. Проверить сериализацию snapshot нового значения enum и что старые логи без него читаются.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0010Tk0001Tests.SelfDestructWritesSingleLogEntry`
- `DemoEp0036Us0010Tk0001Tests.ShotDownCarrierStillDestroyedKind`
- `DemoEp0036Us0010Tk0001Tests.RepeatTickNoSecondEntry`
- `DemoEp0036Us0010Tk0001Tests.EnumValuesAppendOnly`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0010-TK-0001-selfdestruct-log/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0010Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.
