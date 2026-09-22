---
epic: EP-0001-trading-system
story: EP-0001-US-0016-market-knowledge
ticket: EP-0001-US-0016-TK-0001-market-knowledge-contract
title: Контракт coarse knowledge без удалённых цен
stage: approved
layer: contracts
depends_on: []
files_touched: 3
serves: [AC-01, AC-03]
created: 2026-09-21T15:11:56Z
revision: 1
---

# Контракт coarse knowledge без удалённых цен

## Why

Client должен получить отдельно от локальной котировки безопасное представление последнего знания о рынках известных станций. Текущий `AuthoritativeSnapshot` имеет только точный `DockedStationTrade`, поэтому без отдельного DTO Engine либо раскроет удалённые цены, либо заставит Client догадываться по world state. Тикет задаёт сериализуемую сетевую границу и не реализует сбор/обновление observations.

Story dependencies US-0004/US-0015 должны быть завершены перед использованием контракта Engine, но сам additive Contracts merge unit не зависит от их production files.

## Decisions

- 2026-09-21T15:11:56Z — пользователь: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0016-market-knowledge\EP-0001-US-0016-market-knowledge.md».
- Других пользовательских решений для этого тикета нет.

## Assumptions

- A-01: Knowledge публикуется отдельным массивом на snapshot, а не в `ObjectMotionSnapshot`: это не смешивает экономическое знание с motion/prediction DTO.
- A-02: Role — готовая player-facing строка; Client не резолвит profile id через Engine content.
- A-03: Availability — наблюдённый boolean. Семантика вычисления принадлежит TK-0002.
- A-04: Stock knowledge содержит только `ItemTypeId` и существующий authoritative enum `StationMarketStockState`; quantity, target/max, price, budget, quote curve и QuoteId отсутствуют по конструкции.
- A-05: `IsStale` — projection field, а `ObservedAtGameTimeMs`/`ObservedMarketRevision` описывают сохранённое observation. Revision имеет тип `ulong`, как монотонный `SnapshotSequence`.
- A-06: Все новые positional параметры добавляются trailing/defaulted; старые конструкторы и JSON без knowledge продолжают работать.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/StationMarketKnowledgeSnapshot.cs` | Файл отсутствует; Contracts — проект DTO без Engine dependency (`Documentation/00-Process/CLAUDE.md:22–38`). | Создать два immutable DTO и JSON converter для default immutable array. |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Snapshot заканчивается trailing fields и имеет только локальный `DockedStationTrade` (`:11–53`, особенно `:29–34`). | Добавить trailing/defaulted `StationMarketKnowledge` после текущих параметров без изменения существующих параметров. |
| `tests/DeepSpaceSaga.Contracts.Tests/StationMarketKnowledgeSnapshotTests.cs` | Файл отсутствует; matching test project — `tests/DeepSpaceSaga.Contracts.Tests`. | Добавить contract/JSON/privacy/backward-compatibility tests только нового DTO и поля snapshot. |

## Public API after the change

```csharp
public sealed record StationMarketKnowledgeSnapshot(
    string StationObjectId,
    string StationRole,
    bool IsAvailable,
    long ObservedAtGameTimeMs,
    ulong ObservedMarketRevision,
    bool IsStale,
    ImmutableArray<StationMarketStockBandSnapshot> StockBands = default);

public sealed record StationMarketStockBandSnapshot(
    string ItemTypeId,
    StationMarketStockState StockState);
```

`AuthoritativeSnapshot` получает последний trailing/defaulted параметр:

```csharp
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<StationMarketKnowledgeSnapshot>))]
ImmutableArray<StationMarketKnowledgeSnapshot> StationMarketKnowledge = default
```

DTO намеренно не содержит `StockQuantity`, `TargetStock`, `MaxStock`, `FreeStockCapacity`, `UnitPriceCredits`, budget/credits, `QuoteId` или quote curve.

## Implementation steps

1. Создать `StationMarketKnowledgeSnapshot.cs` с указанными публичными record-сигнатурами, XML-комментариями о last-known семантике и converter для `StockBands`, чтобы default immutable array сериализовался совместимо с остальными snapshot DTO.
2. Добавить `StationMarketKnowledge` последним параметром `AuthoritativeSnapshot`; не переставлять и не переименовывать существующие поля.
3. Добавить JSON round-trip с двумя stations и всеми `StationMarketStockState`, тест legacy snapshot без нового поля (`IsDefaultOrEmpty`) и reflection/serialized-property test, запрещающий exact price/quantity/budget/quote поля.
4. Проверить, что `IsStale`, observed timestamp/revision и zero revision не теряются при round-trip, а default arrays не требуют специальных действий у старых callers.

## Out of scope

- Вычисление role, availability, stock band или stale.
- Изменение `StationTradeSnapshot`, quote API US-0015, Engine/save/client code.
- Новый scanner/intelligence command или UI.
- Публикация точных удалённых цен/остатков/бюджета.

## Invariants

- Contracts не получает ссылку на Engine и остаётся JSON-serializable (`Documentation/00-Process/CLAUDE.md:22–38, 421–430`).
- `DockedStationTrade` остаётся точной локальной проекцией и non-null только при docking (`src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:29–34`).
- Budget станции не раскрывается даже локальным trade DTO (`src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:15–34`); remote DTO имеет ещё более узкую поверхность.
- Stock band переиспользует authoritative enum, который уже сериализуется строковым именем (`src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:75–93`).
- Additive trailing defaults сохраняют source/JSON compatibility существующих snapshot fixtures.

## Tests

Named tests в `StationMarketKnowledgeSnapshotTests`:

- `Market_knowledge_round_trips_without_exact_market_values`
- `All_stock_bands_serialize_by_name`
- `Authoritative_snapshot_market_knowledge_defaults_to_empty`
- `Legacy_snapshot_json_deserializes_without_market_knowledge`
- `Market_knowledge_contract_has_no_price_quantity_budget_or_quote_fields`

Команды:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~StationMarketKnowledgeSnapshotTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
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

Все имена, типы, поля, privacy exclusions, сериализация и тесты заданы выше. Implementer изменяет ровно два Contracts files и один matching test file; бизнес-правила не требуются.
