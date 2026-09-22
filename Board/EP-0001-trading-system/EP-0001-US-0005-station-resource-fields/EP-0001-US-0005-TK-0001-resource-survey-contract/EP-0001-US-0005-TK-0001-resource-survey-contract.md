---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
ticket: EP-0001-US-0005-TK-0001-resource-survey-contract
title: Контракт разрешённого состава
stage: approved
layer: contracts
depends_on: []
files_touched: 2
serves: [AC-04, AC-06]
created: 2026-09-21T09:56:58Z
revision: 1
---

# Контракт разрешённого состава

## Why

Client должен различать известный астероид с неизвестным составом и результат разрешённого survey, не получая скрытых ресурсов. End state: additive DTO и JSON roundtrip всех допустимых состояний.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj. Относительные пути — от D:/DeepSpaceSaga/DSS.

## Decisions

2026-09-21T09:56:58Z — зафиксирован запрос: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0005-station-resource-fields\EP-0001-US-0005-station-resource-fields.md». Иных технических решений пользователь не давал.

## Assumptions

DTO относится только к generated resource asteroids. null сохраняет старые snapshots; null не означает разрешение сканировать старый произвольный объект. Fraction — характеристика состава, не cargo/yield. Ice/Silicate/Iron остаются единственными base composition values.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs | :11 record, последний optional NavigationTargetObjectId; survey metadata отсутствует | Дописать optional Survey, DTO и reason constants ниже |
| tests/DeepSpaceSaga.Contracts.Tests/ResourceSurveySnapshotTests.cs | Новый файл; JSON pattern StationTradeSnapshotTests.cs | Legacy/unknown/revealed DTO roundtrip и default immutable array |

## Public API after the change

Namespace DeepSpaceSaga.Contracts; все типы можно поместить в существующий ObjectMotionSnapshot.cs:
```csharp
// В конце ObjectMotionSnapshot:
AsteroidSurveySnapshot? Survey = null

public sealed record ResourceFractionSnapshot(string ItemTypeId, int Permille);
public sealed record AsteroidSurveySnapshot(long MassKg, bool CompositionKnown,
    bool CanStructuralScan, string? CompositionType = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ResourceFractionSnapshot>))]
    ImmutableArray<ResourceFractionSnapshot> Resources = default);
```
`ResourceSurveyReasonCodes` static constants:
`UnsupportedTarget="resource_survey_unsupported"`, `TargetNotIdentified="target_not_identified"`, `AlreadyKnown="target_already_structurally_identified"`, `OutOfRange="target_out_of_range"`, `TargetLost="target_lost"`, `ScanFailed="scan_failed"`, `InvalidTime="resource_survey_invalid_time"`.
Existing Busy/ModuleUnavailable/MissingTarget/UnknownTarget reason constants переиспользуются.

Unknown: CompositionKnown=false, CompositionType=null, Resources empty. Known: CompositionKnown=true, CompositionType один из3, Resources positive integer fractions, sum1000; CanStructuralScan=false. CanStructuralScan — разрешённость target/range и отсутствие текущей попытки для target; availability выбранного модуля Client проверяет отдельно; Engine валидирует всё заново. MassKg допустима в обоих состояниях: постоянный generated asteroid уже IsKnown=true по требованиям.

## Implementation steps

1. Добавить optional Survey в конец record, preserving positional/default compatibility.
2. Добавить public records/константы и immutable array converter. Не добавлять actual hidden field kind, variant ID, owner profile или raw generated data в snapshot.
3. XML comments явно отделяют coarse CompositionType от resource fractions и technical motion от player knowledge. Contract DTO не считает деньги/массу/cargo и не выполняет validation мира.
4. Написать named serialization tests, включая with-copy ObjectMotionSnapshot: Survey сохраняется при изменении X/Y/SpeedKmS, как требует prediction pipeline.

## Out of scope

Генератор, scanner execution, новые base composition types, Engine refs, UI, save schema.

## Invariants

Contracts не имеет зависимостей на Engine/Client (Documentation/00-Process/CLAUDE.md:45–56). Permanent asteroid composition скрыт до survey (EngineRequirements.md:1558–1574). Motion fields и правила Approach не меняются (:5337–5341).

## Tests

ResourceSurveySnapshotTests:
- `Legacy_motion_snapshot_roundtrips_with_null_survey` — AC-06.
- `Unrevealed_survey_contains_mass_but_no_composition_or_resources` — AC-04/06, fixture отражает producer contract.
- `Revealed_survey_roundtrips_exact_resource_fractions` — AC-06.
- `Default_resource_array_serializes_as_empty` и `Motion_with_copy_preserves_survey` — AC-06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps только в двух файлах; served criteria покрыты named tests на уровне DTO.
- Matching tests/build/format проходят либо конкретный исходный failure записан отдельно.
- Public API/defaults/invariants и Out of scope соблюдены; нет скрытых files/assumptions/вопросов.
- Unknown и known payload проверяются по JSON; реальный knowledge gate реализует TK-0004.

## Self-containment check

Полные DTO/defaults/коды и семантика раскрытия заданы. Для реализации не нужен выбор mineral model или поиск новых Engine API.
