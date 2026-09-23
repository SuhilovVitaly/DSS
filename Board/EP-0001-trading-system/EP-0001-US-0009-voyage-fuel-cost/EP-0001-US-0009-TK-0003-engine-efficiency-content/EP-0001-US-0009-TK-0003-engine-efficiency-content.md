---
epic: EP-0001-trading-system
story: EP-0001-US-0009-voyage-fuel-cost
ticket: EP-0001-US-0009-TK-0003-engine-efficiency-content
title: Baseline эффективности стартового двигателя
stage: approved
layer: content-data
depends_on: [EP-0001-US-0009-TK-0002-fuel-accounting-foundation]
files_touched: 2
serves: [AC-01]
created: 2026-09-21T12:38:06Z
revision: 1
---

# Baseline эффективности стартового двигателя

## Why

Дать shipped `module.engine.basic` конкретную положительную эффективность, чтобы voyage formula имела data-driven вход во всех стартовых сценариях. End state: packaged content задаёт 10 km/kg, проходит строгую загрузку и не меняет speed/capacity/command parameters.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Decisions

Пользователь не задавал числовое значение. Baseline `10 km/kg` — безопасный явно записанный tuning assumption A-03; итоговый balance approval принадлежит US-0013.

## Assumptions

Все Default/Docked/Undocked используют один type `module.engine.basic`, поэтому изменение единственного module JSON распространяется без дублирования scenario data. Content test проверяет значение и production bootstrap, но не реализует voyage.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json` | `module.engine.basic` задаёт speed/inertia/capacity, efficiency отсутствует (`:2–18`) | Добавить `"fuelEfficiencyKmPerKg": 10`, остальное не менять |
| `tests/DeepSpaceSaga.Client.Tests/VoyageFuelContentTests.cs` | Новый файл; packaged Settings boot pattern есть в `LocalSessionIntegrationTests.cs:436–472` | Exact JSON assertion и boot smoke через production Settings |

## Public API after the change

No API change. Content fragment:

```json
{
  "typeId": "module.engine.basic",
  "fuelCapacityKg": 1000,
  "fuelEfficiencyKmPerKg": 10
}
```

## Implementation steps

1. Добавить единственное новое поле к `module.engine.basic`; сохранить остальные значения и формат массива.
2. Test через `JsonDocument` находит ровно один `module.engine.basic` и проверяет integer `10`, не duplicate/string/fraction.
3. Boot smoke создаёт Engine через repository `src/DeepSpaceSaga.Client/Settings.json` и получает initial snapshot без `ContentException`; не использует копию test-only module JSON.
4. Test фиксирует неизменность `fuelCapacityKg=1000`, `maxSpeedMps=4000`, inertia/turn parameters, чтобы tuning fuel efficiency не менял physical ship motion.

## Out of scope

Изменения scenario, tank amount, prices, route distances/multipliers, balance report, Contracts, Engine calculation и UI.

## Invariants

- Engine content data остаётся источником параметров модулей: `Documentation/00-Process/CLAUDE.md:56–68`.
- Стартовый engine capacity/fuel определены требованиями: `EngineRequirements.md:4928–4954`.
- Approach speed неизменна: `EngineRequirements.md:5335–5343`; новый field не участвует в motion.
- Два files, production layer content-data, matching `DeepSpaceSaga.Client.Tests`.

## Tests

`VoyageFuelContentTests`:

- `Packaged_basic_engine_declares_ten_km_per_kg_efficiency` (AC-01).
- `Packaged_settings_boots_with_engine_efficiency_content` (AC-01).
- `Fuel_efficiency_content_does_not_change_engine_motion_or_capacity_values` (AC-01).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~VoyageFuelContentTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только два разрешённых файла; production JSON содержит одно integer поле со значением 10.
- AC-01 покрыт exact content и real Settings boot tests; build/format проходят либо baseline failure записано отдельно.
- Capacity, speed, turn/inertia и scenario fuel не изменены.
- Нет скрытого balance claim, assumptions или работы вне scope.

## Self-containment check

Путь, точное поле/значение, неизменяемые соседние параметры и commands заданы. Implementer не выбирает число и не редактирует сценарии.
