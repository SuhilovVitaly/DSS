---
epic: EP-0001-trading-system
story: EP-0001-US-0016-market-knowledge
ticket: EP-0001-US-0016-TK-0003-market-knowledge-presentation
title: Market knowledge в существующей Object Info
stage: approved
layer: client
depends_on: [EP-0001-US-0016-TK-0002-authoritative-market-observations]
files_touched: 3
serves: [AC-04]
created: 2026-09-21T15:11:56Z
revision: 1
---

# Market knowledge в существующей Object Info

## Why

Игрок уже выбирает и наводится на станции на tactical map, а Object Info показывает выбранный объект. Добавление remote knowledge туда делает пять рынков сравнимыми без второго торгового или разведывательного экрана. Client только форматирует authoritative DTO; цены, stock bands и stale не пересчитываются.

## Decisions

- 2026-09-21T15:11:56Z — пользователь: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0016-market-knowledge\EP-0001-US-0016-market-knowledge.md».
- Других пользовательских решений для этого тикета нет.

## Assumptions

- A-01: Используется существующая Object Info panel, не новый screen. Hover по-прежнему имеет приоритет над selected object.
- A-02: `GameSessionScreen` сопоставляет knowledge по exact ordinal `StationObjectId`; отсутствие записи означает отсутствие строк, а не fabricated `Unknown` market.
- A-03: Формат: `Role`, `Market` (`Available|Unavailable / FRESH|STALE`), `Observed` (`T+<GameTimeMs> ms`) и три строки `Shortage|Normal|Surplus` со sorted item ids либо `—`.
- A-04: Client не форматирует exact price/quantity/budget и не читает `DockedStationTrade` для Object Info. Точный локальный Trade продолжает существующий путь.
- A-05: Текст `STALE` всегда явный; цветовая кодировка может сопровождать его, но не заменяет текст и не входит в scope.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Snapshot используется для render states; выбранная строка собирается через `ToObjectInfoPanelData` и hover имеет приоритет (`:262–264, 1184, 2083–2109`). | Найти matching authoritative knowledge по object id и передать его только соответствующей Station в panel data. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Панель имеет две существующие строки; formatter выводит только Name/Speed/Direction (`:6–16, 160–179`), text column располагается рядом с изображением (`:278–301`). | Расширить optional panel data и pure formatter market lines без нового screen/layout system. |
| `tests/DeepSpaceSaga.Client.Tests/ObjectInfoPanelTests.cs` | Тестирует pure formatter, geometry и wiring snapshot -> panel (`:56–110, 220–252`). | Добавить DTO fixtures и тесты known/fresh/stale/no-knowledge/privacy/hover priority в существующий matching файл. |

## Public API after the change

Публичный cross-project API не меняется. Внутренний Client DTO расширяется trailing optional полем:

```csharp
public readonly record struct ObjectInfoPanelData(
    string ObjectId,
    string? DisplayName,
    double SpeedKmS,
    double Direction,
    string? RenderObjectType,
    string? Image = null,
    StationMarketKnowledgeSnapshot? MarketKnowledge = null);
```

`ObjectInfoPanel.BuildLines(ObjectInfoPanelData?)` остаётся pure formatting seam.

## Implementation steps

1. При обработке latest snapshot построить lookup `StationObjectId -> StationMarketKnowledgeSnapshot`; не кэшировать его независимо от snapshot sequence.
2. Изменить `ToObjectInfoPanelData`/call sites так, чтобы market DTO прикреплялся только при exact object id match. Player ship, asteroid, unknown station и station без observation получают null.
3. Сохранить первые три строки Name/Speed/Direction. При non-null knowledge добавить Role, Market, Observed и по одной строке для `Shortage`, `Normal`, `Surplus`; item ids сортировать ordinally, пустую группу показывать `—`.
4. Не выводить UnitPrice, StockQuantity, budget, QuoteId или quote curve и не вычислять band/stale/availability из других snapshot fields.
5. Добавить pure formatter и screen wiring tests: fresh/stale, available/unavailable, all bands, station без knowledge, non-station, hover priority и смена snapshot удаляет/заменяет старое knowledge.

## Out of scope

- Новый экран, station list, scanner controls, tooltip price или route overlay.
- Изменение TradeScreen/StationScreen, quote refresh и отправка команд.
- Любой клиентский market calculation или сохранение knowledge.
- Изменение Contracts/Engine.

## Invariants

- Renderer/UI читает только клиентский snapshot state и не обращается к Engine напрямую (`Documentation/00-Process/CLAUDE.md:40–56`; `EngineRequirements.md:411–419`).
- Hover остаётся выше selected object (`GameSessionScreen.cs:2083–2109`).
- Existing Object Info geometry/input semantics сохраняются (`ObjectInfoPanel.cs:223–301`; `ObjectInfoPanelTests.cs:112–218`).
- Exact local data остаётся в `DockedStationTrade`; remote panel использует только TK-0001 DTO.
- Отсутствующие данные показываются отсутствием market lines, не ложным live/zero значением.

## Tests

Named tests в `ObjectInfoPanelTests`:

- `Market_lines_show_role_availability_observed_time_and_freshness`
- `Market_lines_group_sorted_item_ids_by_authoritative_band`
- `Stale_market_is_explicit_text_not_inferred_client_side`
- `Station_without_observation_has_no_market_lines`
- `Non_station_never_receives_station_market_knowledge`
- `Hovered_station_knowledge_has_priority_over_selected_station`
- `New_snapshot_replaces_market_knowledge_without_client_cache`
- `Market_lines_never_show_price_quantity_budget_or_quote`

Команды:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ObjectInfoPanelTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
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

Путь UI, сопоставление DTO, точные labels/format/order, privacy boundary и тестовые сценарии заданы. Implementer изменяет два Client files и один matching test file; дополнительных экранов, assets или решений искать не нужно.
