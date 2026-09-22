---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
ticket: EP-0001-US-0012-TK-0001-economy-save-schema
title: Версия и миграция экономического save
stage: approved
layer: engine
depends_on: [EP-0001-US-0002-market-replenishment, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0005-station-resource-fields, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0011-net-voyage-profit]
files_touched: 4
serves: [AC-01, AC-05, AC-06]
created: 2026-09-21T12:53:36Z
revision: 1
---

# Версия и миграция экономического save

## Why

Все prerequisite stories добавляют собственные persisted fragments, но продолжение торговой игры требует одной versioned границы, которая отличает полный economic continuation state от частичного набора optional полей. Тикет задаёт manifest, финальный version bump, строгую shape-validation и консервативную deterministic migration без переоценки уже существующих денег, cargo или топлива.

## Decisions

Пользовательских решений кроме исходного запроса «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0012-resume-trading-economy\EP-0001-US-0012-resume-trading-economy.md» не было.

## Assumptions

- A-01: тикет исполняется после merge всех прямых persistence prerequisites. Новый numeric version равен ровно `SaveFormat.CurrentSaveFormatVersion + 1` в merged HEAD; draft allocations 9/10 не являются подтверждённой историей версий.
- A-02: manifest не дублирует stocks/events/map/voyage/ledger. Он хранит schema version, combined configuration fingerprint и allocator/cursor facts, которых нет в owning DTO.
- A-03: `saveFormatVersion=0` остаётся scenario. Pre-trading saves 1–8 допускаются только по уже поддерживаемой loader policy, с пустыми новыми runtime sections; partial fields новой экономики под старой version отклоняются.
- A-04: prerequisite-era versioned saves мигрируют только при полном наборе mandatory полей своей версии. Migration копирует persisted values и создаёт отсутствующие empty sections/cursors из сохранённых stable ids; никакая current quote/price/basis формула не используется.
- A-05: combined fingerprint — uppercase SHA-256 canonical ordinal payload catalog compatibility, market-profile/economy fingerprint, market-event catalog fingerprint и trading-map rule/schema identity. Mutable state и locale не входят.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | `SaveFormat.CurrentSaveFormatVersion=8`; `GameStateData` уже хранит receipts/economy time/catalog identity (`:7–24,52–78`) | Добавить финальный bump/history comment, trailing `TradingEconomyContinuationData?`, manifest/cursor DTO без копии subsystem state |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | Strict JSON и диапазон version в `:27–35,60–83,129–141`; versioned economy/profile checks в `:184–241` | Вызвать version-aware normalization, проверить manifest shape/required combinations/ordinal uniqueness до runtime load |
| src/DeepSpaceSaga.Engine/Scenario/TradingEconomySaveMigration.cs | Новый файл; общего migration seam нет, а epic gap R03 зафиксирован в `Board/EP-0001-trading-system/Documentation.md:120–128` | Pure migration по явной таблице versions; никаких registry/price computations и side effects |
| tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs | Новый файл; текущие version/fingerprint примеры находятся в `CatalogCompatibilityTests.cs:22–95` и `EconomyTimeContinuityTests.cs:264–319` | Fixtures для current, каждой supported legacy class, partial/corrupt/future version и deterministic repeated migration |

## Public API after the change

Engine scenario/save schema:

```csharp
public sealed record TradingEconomyContinuationData(
    int SchemaVersion,
    string ConfigurationFingerprint,
    long LastProcessedMarketGameTimeMs,
    long NextMarketRevision,
    long NextMarketEventSequence,
    IReadOnlyList<string>? DurableTerminalReceiptIds = null);

// trailing GameStateData field
[property: JsonPropertyName("tradingEconomyContinuation")]
TradingEconomyContinuationData? TradingEconomyContinuation = null;
```

`SchemaVersion` начинает с 1. Все numeric cursors неотрицательны; receipt ids nonblank, distinct и ordinal-sorted. Manifest обязателен для нового save version, запрещён для `saveFormatVersion=0` scenario и для legacy version, которую migration ещё не нормализовала.

Internal pure seam:

```csharp
internal static class TradingEconomySaveMigration
{
    internal static ScenarioFile Normalize(ScenarioFile source);
}
```

## Implementation steps

1. До изменения файлов выписать фактическую merged history `SaveFormat` после prerequisites. В комментарии `SaveFormat` перечислить каждый numeric step; новый current установить на предыдущий +1. Если dependency оставила conflicting/пропущенную version, вернуть тикет в review вместо renumber чужой схемы молча.
2. Добавить manifest DTO и trailing optional field. DTO содержит только cross-cutting compatibility/cursors; materialized map, station state, events, knowledge, voyage, fuel и ledger остаются в dependency-owned DTO.
3. Реализовать явную migration table. Version0 не мигрировать. Versions1–8 без partial new fields оставить pre-trading compatible и нормализовать пустым continuation только на engine save-load path; prerequisite-era versions копируют их mandatory state и получают manifest только из persisted facts. Любой неизвестный/partial combination отвергать с `Save was not modified`.
4. Не вычислять cargo/fuel basis, receipt totals, prices, market revisions, event times или voyage progress заново. Empty означает отсутствие исторического subsystem state, а не нулевую стоимость существующего stack.
5. В loader после JSON deserialize и базовой version проверки вызвать migration, затем validate normalized manifest: fingerprint uppercase 64-hex, schema1, cursors ≥0, receipt IDs distinct/sorted. Current version без manifest и old version с current-only fields отклонять.
6. Тестировать каждый семантический legacy class (0; 1–4; 5–6; 7–8; каждый фактический merged prerequisite version; current; future). Для numeric versions после8 использовать constants/fixtures, зафиксированные в merged history, а не draft story numbers.
7. Проверить повторный `Normalize(Normalize(save))` на structural equality и неизменность stock, tokens, station credits/budget, cargo/fuel basis, receipt totals, event/voyage/ledger payload.

## Out of scope

- Capture/restore runtime market или voyage state — TK-0002/TK-0003.
- Combined fingerprint вычисление из runtime registry и atomic engine commit — TK-0004.
- LocalClient disk I/O — TK-0005.
- Миграция произвольно повреждённого JSON, переоценка legacy cargo/fuel или поддержка future version.
- Изменения Contracts, Client, content JSON, requirements или dependency story files.

## Invariants

- Unknown JSON field, missing mandatory field и semantic invalid value останавливают load: `Documentation/01-Requirements/EngineRequirements.md:254–261`; `ScenarioLoader.cs:27–35`.
- Save является continuation state, а type definitions остаются immutable registry data: `EngineRequirements.md:222–246,3600–3614`.
- `CatalogCompatibility` уже обязателен с version7: `ScenarioLoader.cs:129–141`; новый manifest усиливает, но не заменяет эту проверку.
- Migration никогда не меняет persisted финансовые значения или quantities; epic требует no cargo revaluation: `Board/EP-0001-trading-system/Documentation.md:103,126`.

## Tests

Matching test project: `tests/DeepSpaceSaga.Engine.Tests`.

Named tests:

- `Current_trading_save_requires_complete_manifest`
- `Legacy_version_classes_normalize_without_repricing`
- `Partial_new_economy_state_under_legacy_version_is_rejected`
- `Migration_is_idempotent_and_preserves_financial_payload`
- `Future_or_incompatible_manifest_is_rejected_before_load`

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingEconomySaveSchemaTests"
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

Тикет задаёт точные четыре файла, DTO shape, migration classes, запреты на recomputation, validation order и test matrix. Implementer использует merged `SaveFormat` history только для механической подстановки следующего номера; продуктовых решений или поиска дополнительных files не требуется. Несовместимый prerequisite version graph является явным return-to-review condition.
