---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0012-campaign-lifecycle
ticket: EP-0029-US-0012-TK-0001-checkpoint-transport
title: Автосохранение результата без потери сессии
stage: approved
approval_kind: workflow-only
layer: local-client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0006-TK-0003-post-battle-state, EP-0029-US-0007-TK-0003-catalog-compatibility, EP-0029-US-0008-TK-0004-rearm-client-handle, EP-0029-US-0010-TK-0005-battle-locale, EP-0029-US-0011-TK-0003-tutorial-instructions]
files_touched: 4
serves: [AC-02, AC-04]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: implemented
---

# Автосохранение результата без потери сессии

## Why

Игрок начинает обучение без обхода ownership, а после боя получает сохранённые последствия. Выход в меню и сбой записи не дают скрытую бесплатную перезагрузку имущества. Этот тикет обеспечивает **Автосохранение результата без потери сессии**. Источники: [история](../EP-0029-US-0012-campaign-lifecycle.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **local-client**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs](../../../../../src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs) | `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Автосохранение результата без потери сессии» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs](../../../../../src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs) | `src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Автосохранение результата без потери сессии» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs](../../../../../src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs) | `src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Автосохранение результата без потери сессии» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0001Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0001Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0012Tk0001Tests ниже с независимым oracle. |

## Public API after the change

Существующий ExitTacticalBattleAsync завершает storage phase; IOError failure различим от Engine reject. Новый public API не требуется.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. После ExitTacticalBattle и подтверждённого settlement сохранять expanded Save21 в отдельный campaign continuation slot с устойчивым CampaignId, atomic temp+replace. Не вызывать Save при active battle. Если outcome committed, а файл не записан, retry Exit/Finalize повторяет запись того же settlement receipt без применения потерь заново. При cancellation после commit вернуть recoverable storage failure, не терять возможность retry. Не затирать пользовательские ручные slots. Slot виден в списке Load и dead state тоже доступен как terminal continuation.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-02, AC-04 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0006-TK-0003-post-battle-state](../../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-TK-0003-post-battle-state/EP-0029-US-0006-TK-0003-post-battle-state.md) — завершить до этого тикета.
- [EP-0029-US-0007-TK-0003-catalog-compatibility](../../EP-0029-US-0007-save-continuity/EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md) — завершить до этого тикета.
- [EP-0029-US-0008-TK-0004-rearm-client-handle](../../EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-TK-0004-rearm-client-handle/EP-0029-US-0008-TK-0004-rearm-client-handle.md) — завершить до этого тикета.
- [EP-0029-US-0010-TK-0005-battle-locale](../../EP-0029-US-0010-tactical-presentation/EP-0029-US-0010-TK-0005-battle-locale/EP-0029-US-0010-TK-0005-battle-locale.md) — завершить до этого тикета.
- [EP-0029-US-0011-TK-0003-tutorial-instructions](../../EP-0029-US-0011-starter-loadout/EP-0029-US-0011-TK-0003-tutorial-instructions/EP-0029-US-0011-TK-0003-tutorial-instructions.md) — завершить до этого тикета.

## Out of scope

Layers кроме local-client; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0012Tk0001Tests**.

- `Ep0029Us0012Tk0001Tests.CheckpointAfterBattleContainsActualAmmo` — save читается новым engine.
- `Ep0029Us0012Tk0001Tests.DiskFailureKeepsSessionAndRetryId` — retry не удваивает ущерб.
- `Ep0029Us0012Tk0001Tests.ManualSlotsUntouched` — только выделенный continuation.
- `Ep0029Us0012Tk0001Tests.DestroyedCheckpointLoadsAsDestroyed` — нет воскресения.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0012Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0012-TK-0001" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0001Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 4 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-02, AC-04; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

Implemented (2026-11-15) — see the execution subsection below. Создание planning-документа не является implementation, build, test или game launch.

## Remediation/Execution 2026-11-15

Implementation status: **implemented** (automated). Human/product acceptance remains **OPEN**; no native UI gate applies to this ticket (no UI change).

Scope actually changed (4 allowlisted production/test files + this Board ticket):
- `src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs` — reserved `ContinuationSlotId = "campaign-continuation"` and `ContinuationDisplayName = "Campaign Continuation"`; the file name stays `campaign-continuation.json`.
- `src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs` — `ListSlots` maps the continuation slot id to the fixed display name; all other slots keep the file-stem display name.
- `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` — `ExitTacticalBattleAsync` now owns the storage phase: accepted owned-exit receipts (Settlement != null) are cached by BattleId plus a checkpointed set; the post-commit world is written through the existing atomic `SaveAsync(ContinuationSlotId, ct)`; a cached BattleId retries only the write and never re-calls the engine; storage failures (`IOException`, `UnauthorizedAccessException`, post-commit cancellation, disposed connection) are wrapped as `IOException("campaign checkpoint failed: ...")` after caching so the Client's `SendTactical` maps them to `Unavailable`; no save directory skips the write and marks the receipt checkpointed; diagnostic exits and rejections pass through unchanged.
- `tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0001Tests.cs` — new named tests (real ClientRoot `Settings.json` + Tutorial scenario through `LocalGameSessionConnection`, temp save directory, CarrierOperations snapshots): `ExitWritesContinuationCheckpoint`, `CheckpointFailureKeepsRetryWithoutResettlement`, `RetryAfterSuccessReturnsCachedSettlement`, `ManualSaveDuringBattleRejected`, `ContinuationSlotIsListedAndLoadable`, `DuplicateExitAfterReloadIsNoBattle`.

Actual commands and results (artifacts in `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-057-us0012tk0001/`):

```powershell
# TDD red before production changes: 6 failed / 0 passed — checkpoint absent, no IOException, no cached receipt.
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0012Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-057-us0012tk0001" --logger "trx;LogFileName=focused-red.trx"
# -> Failed: 6, Passed: 0 (focused-red.trx / focused-red.log)

# Focused named tests after implementation: 6 passed / 0 failed.
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0012Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-057-us0012tk0001" --logger "trx;LogFileName=focused.trx"
# -> Passed: 6, Failed: 0 (focused.trx / focused.log)

# Guards: SaveSlotRepositoryTests, SaveSlotsTests, LocalSessionIntegrationTests save/load subset (28 tests).
# -> Passed: 26, Failed: 2 — exactly the known baseline reds
#    Economic_session_save_and_create_from_save_preserve_full_continuation(inFlight: true/false),
#    Assert.Equal expected 20 actual 21; both already Failed in baseline-20261115/client-baseline.trx. (guards.trx / guards.log)

dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
# -> Build succeeded. 0 Warning(s), 0 Error(s). (build-localclient.log)

dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0012Tk0001Tests.cs
# -> exit 0, no changes required. (format.log)
```

Evidence mapping: checkpoint exists + display name + expanded carrier state after exit (`focused.trx`, `ExitWritesContinuationCheckpoint`); failed write keeps the cached accepted receipt, one carrier revision increment and a retry without re-settlement (`CheckpointFailureKeepsRetryWithoutResettlement`); repeated exit returns the same receipt and revision (`RetryAfterSuccessReturnsCachedSettlement`); manual save during battle still rejected and no manual file (`ManualSaveDuringBattleRejected`); continuation slot listable and loadable through `CreateFromSaveFile` without re-granting the manifest (`ContinuationSlotIsListedAndLoadable`); old BattleId on a reloaded session is `NoBattle` and the file is byte/timestamp unchanged (`DuplicateExitAfterReloadIsNoBattle`). No full-suite claim; known baseline reds and other EP-0019-era Client.Tests failures are unchanged. Native/human acceptance: OPEN.

