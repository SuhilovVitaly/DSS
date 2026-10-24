---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0002-countermeasure-equipped-tetrarch
ticket: EP-0007-US-0002-TK-0001-countermeasure-contract
title: "Состояния аппарата и противоракеты"
stage: draft
layer: contracts
depends_on: ["EP-0007-US-0001-TK-0005-combat-id-compatibility"]
files_touched: 5
serves: ["AC-0001","AC-0002"]
created: 2026-10-04
revision: 1
---

# Состояния аппарата и противоракеты

STATUS: DRAFT

## Why

Как игрок, я начинаю существующий бой на Тетрархе с торпедным и противоракетным аппаратами. Пират имеет такую же комплектацию и двух операторов, но использует только защиту. Новое помещение не сдвигает старые модули. Этот тикет обеспечивает: состояния аппарата и противоракеты.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002 [истории](../EP-0007-US-0002-countermeasure-equipped-tetrarch.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Contracts/SpaceObjectType.cs` | Существует: src/DeepSpaceSaga.Contracts/SpaceObjectType.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Contracts.Tests/CountermeasureSnapshotTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 5 [плана](../../Tickets.md).
- [EP-0007-US-0001-TK-0005-combat-id-compatibility](../../EP-0007-US-0001-assigned-weapon-operators/EP-0007-US-0001-TK-0005-combat-id-compatibility/EP-0007-US-0001-TK-0005-combat-id-compatibility.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

SpaceObjectType.Countermeasure; CountermeasureSnapshot(owner,module,targetTorpedoId,phase,route,trail,launchTime,frozenChanceTenths,ratingBreakdown,missExpiresAt?); DefenseSnapshot(autoEnabled,operator,state,activeProjectileId?,reloadDue?,rangeKm). ObjectMotionSnapshot.Defense проецирует NPC состояние; InstalledModuleSnapshot.Defense — игрока.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Фазы Guiding/MissedCoast; аппарат Ready/Guiding/Reloading/NoOperator с отдельным autoEnabled. Time physical, nullableETA unknown. Сохранить existing torpedo DTO; Countermeasure не Torpedo и не selectable fire target. One-attempt marker в торпедном payload либо explicit combat state; завершение PR не снимает marker.
3. Добавить перечисленные ниже проверки реального поведения. Зафиксировать фактический результат, не объявлять планируемые тесты пройденными.

## Out of scope

Другие production layers, файлы внеallowlist, наступательный AI и новые игровые механики вне эпика. Commit/push не входят. Если требуется шестой файл, сначала отдельное согласованное расширениеscope или новый тикет.

## Invariants

- Числа/формулы/границы берутся из эпика; UI не решает исход боя.
- Physical gameplay time и monotonic effect time не смешиваются (EngineRequirements §2 и временной контракт эпика).
- Snapshot immutable; Client не обращается напрямую кEngine (CLAUDE Architecture).
- Непринятые команды и потеря цели до встречи не потребляют draw. Состояние/attempt/frozenchance переживают SaveLoad.
- Совместимость ID следует loader OrdinalIgnoreCase. Existing ordinary torpedo trails остаются скрытыми.

## Tests

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Планируемые проверки:

- [ ] `Countermeasure_states_roundtrip`
- [ ] `Reload_uses_physical_deadline`
- [ ] `Defender_status_available_on_npc_snapshot`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
git diff --check
```

При scoped lint использовать dotnet format --verify-no-changes --include только изменённых C# файлов. Baseline failure документировать с точной командой; не расширять allowlist молча. Native проверка требуется там, где AC касается реального изображения/input; headless результат её не заменяет.

## Definition of Done

- [ ] Реализованы API/шаги вallowlist, covered критерии наблюдаемы.
- [ ] Named tests и сборка слоя пройдены; baseline ограничения записаны отдельно.
- [ ] Dependencies действительно готовы; нет временных заглушек или скрытой работы.
- [ ] Проверены pause/time domains/idempotency/persistence в применимой части.
- [ ] Diff reviewed; runtime/native статус описан честно.

## Self-containment check

Контракт, ограничения, файлы, зависимости и проверки перечислены здесь; формулы/таблица времени централизованы в связанном эпике. Не требуется восстанавливать смысл коротких ответов из чата. Новое обнаруженное несоответствие оформляется как gap, а не произвольное расширение работ.
