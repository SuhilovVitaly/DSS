---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
ticket: EP-0001-US-0012-TK-0005-local-save-roundtrip
title: Файловый Save/Load экономической сессии
stage: approved
layer: local-client
depends_on: [EP-0001-US-0012-TK-0004-deterministic-economy-continuation]
files_touched: 2
serves: [AC-03, AC-05, AC-06]
created: 2026-09-21T12:53:36Z
revision: 1
---

# Файловый Save/Load экономической сессии

## Why

Engine round-trip недостаточен, если реальный gateway теряет payload, пишет partial file или загружает save не через current registry/fingerprint validation. Тикет проверяет публичный `SaveAsync → CreateFromSaveFile` путь на полном торговом state и сохраняет atomic replacement semantics при ошибке или отмене.

## Decisions

Пользовательских решений кроме исходного запроса «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0012-resume-trading-economy\EP-0001-US-0012-resume-trading-economy.md» не было.

## Assumptions

- A-01: LocalClient не знает экономическую схему и не создаёт второй serializer; он пишет полный `CaptureSaveState` через `ScenarioLoader.Serialize`.
- A-02: `CreateFromSaveFile` загружает current registry/settings и делегирует compatibility/atomic restore Engine. Ошибка construction не изменяет уже работающую connection; session swap остаётся задачей Client.
- A-03: production change в gateway допускается только если integration regression доказывает потерю/гонку full state. Нельзя добавлять список экономических полей в LocalClient.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs | `CreateFromSaveFile` делегирует Engine (`:43–55`); `SaveAsync` capture/serialize/temp/atomic move под gate (`:158–191`) | Сохранить generic path; исправить только доказанную потерю complete payload, cancellation/rename ordering или full-state race без экономической логики |
| tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs | Проверяет наличие/парсинг slots и concurrent writes (`:107–239`), но не полный trading continuation | Добавить real settings/save-directory round-trip, loaded snapshot/continuation comparison, incompatible file and overwrite-failure preservation |

## Public API after the change

No API change. Existing signatures remain:

```csharp
public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default);
public static LocalGameSessionConnection CreateFromSaveFile(
    string settingsPath, string savePath, string? saveDirectory = null);
```

No LocalClient DTO or economic serializer is introduced.

## Implementation steps

1. Build a full prerequisite-backed trading fixture through real `Settings.json` and a materialized scenario/save: non-default stock/budget/revision, active event, map/fields, cargo/fuel basis, active voyage/reservation and ledger/receipts. Do not hand-copy JSON property names beyond the Engine ScenarioData constructors.
2. Start `LocalGameSessionConnection(engine, tempSaveDirectory)`, freeze at Speed0 after fixture setup, call `SaveAsync("economy-roundtrip")`, dispose, then load via `CreateFromSaveFile(settingsPath, path, tempSaveDirectory)`.
3. Read the first authoritative snapshot and/or resave loaded connection to a second slot. Compare Engine-normalized continuation evidence exposed by public snapshots/serialized ScenarioData: no immediate stock/price/money/revision/event/voyage/fuel/ledger jump; version and fingerprints present.
4. Continue loaded connection with one fresh quote/command and one time advance to show the actual background loop resumes from restored cursors rather than bootstrap defaults. Assert pre-save issued quote is rejected and durable executed receipt does not double execute.
5. Place a known-good old slot, then induce cancellation/write failure before `File.Move`; verify old file bytes remain parseable and unchanged and no temp file remains. Preserve existing `_saveGate` serialization for concurrent writes.
6. Write a save with tampered catalog/config fingerprint and assert `CreateFromSaveFile` reports the Engine diagnostic containing `Save was not modified`. The original active connection/saved good slot remains usable.
7. If all tests pass without production change, commit only the regression test; `LocalGameSessionConnection.cs` stays untouched and remains an allowed diagnostic file. If it fails, change only generic capture/write/load sequencing in that file.

## Out of scope

- Client screen swap/error rendering, Save/Load UI, localization and modal state.
- Any Engine schema, migration, market/voyage logic or test helper outside allowed files.
- Network session implementation, cloud saves, compression, encryption or autosave.
- Performance benchmarks; correctness and atomicity only.

## Invariants

- Engine/Client communicate through `IGameSessionConnection`; LocalClient is replaceable and must not own game rules: `Documentation/00-Process/CLAUDE.md:22–56`.
- Existing save gate covers capture, serialization, write and rename; failed writes remove temp: `LocalGameSessionConnection.cs:158–191`.
- Save bootstrap loads registry before `LoadScenario(..., isSave:true)`: `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:23–35`.
- Disk write occurs only on explicit save command: `Documentation/01-Requirements/EngineRequirements.md:263–265`.
- No direct Client reference to Engine is introduced; test project may exercise LocalClient integration through existing references.

## Tests

Matching integration test project: `tests/DeepSpaceSaga.Client.Tests`.

Named tests in `LocalSessionIntegrationTests`:

- `Economic_session_save_and_create_from_save_preserve_full_continuation`
- `Loaded_economic_session_continues_without_bootstrap_or_duplicate_effect`
- `Cancelled_economic_save_preserves_previous_slot_and_removes_temp`
- `Incompatible_economic_save_is_rejected_without_replacing_good_slot`
- `Concurrent_economic_saves_each_produce_complete_valid_state`

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalSessionIntegrationTests"
dotnet build D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Тикет использует ровно gateway и его existing integration test file. Fixture, public workflow, continuation evidence, failure modes and permitted fallback edit заданны; LocalClient не должен искать или перечислять economic fields и не принимает продуктовых решений.
