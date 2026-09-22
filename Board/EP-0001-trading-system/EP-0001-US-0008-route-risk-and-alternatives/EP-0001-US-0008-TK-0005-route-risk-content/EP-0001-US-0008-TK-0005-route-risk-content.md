---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
ticket: EP-0001-US-0008-TK-0005-route-risk-content
title: Контрольная карта и события маршрутов
stage: approved
layer: content-data
depends_on: [EP-0001-US-0008-TK-0003-route-event-voyage-integration, EP-0001-US-0008-TK-0004-route-choice-presentation]
files_touched: 3
serves: [AC-01, AC-03, AC-04, AC-05, AC-06]
created: 2026-09-21T11:10:35Z
revision: 1
---

# Контрольная карта и события маршрутов

## Why

Дать shipped content воспроизводимый безопасный маршрут, рискованную альтернативу и реальные blockade/quarantine route effects. End state: строгая загрузка content и integration test доказывают различие минимум по двум параметрам, доступную альтернативу, expiry и одинаковый результат для одинакового seed.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj. Все пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

Пользователь не задавал коэффициенты. Ниже фиксируется configurable verification baseline, а не окончательный баланс; численные диапазоны утверждает US-0013.

## Assumptions

US-0004 создаёт общий thematic map JSON, US-0007 — общий event JSON и подключает оба через существующий Settings/dependency path. Этот тикет не добавляет новые settings/scenario fields. Для baseline: safe=1000 fuel permille; elevated=1300. Blockade делает targeted edge `Restricted` с travel1500/fuel1250; quarantine делает targeted edge `Unavailable` без дополнительных multipliers. Candidate pairs перечислены в stable order, а Engine отбрасывает разрывающий граф candidate.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Markets/trading-map.json | Планируемый shipped content EP-0001-US-0004; сейчас production schema/файл отсутствуют, TK-0001/TK-0002 задают RiskProfiles/Templates/Links | Назначить `risk.safe`/`risk.elevated`, edge roles и baseline multipliers для всех templates |
| src/DeepSpaceSaga.Client/Data/Markets/market-events.json | Планируемый shipped content EP-0001-US-0007; текущие shipped scenarios не содержат событий (`ScenarioData.cs:156–160`) | Route effect blocks для pirate blockade/quarantine, stable candidate endpoints и player text |
| tests/DeepSpaceSaga.Client.Tests/RouteRiskContentTests.cs | Новый файл; packaged-content integration pattern — `LocalSessionIntegrationTests.cs:436–472`, market content pattern — `StationMarketCatalogContentTests.cs:10` | Strict parse, template/seed corpus, public local-session snapshot and expiry checks |

## Public API after the change

No API change. JSON использует dependency schemas без новых полей этого тикета:

- map risks: `riskProfileId`, `fuelMultiplierPermille`; links reference one profile.
- event route effect: dependency US-0007 fields for availability, travel/fuel permille, ordered candidate endpoint pairs and player-facing description.

Exact baseline:

- `risk.safe`: fuel 1000; минимум одно Short edge от start station.
- `risk.elevated`: fuel 1300; минимум одно Medium/Long edge, не единственный incident edge станции.
- `event.pirate-blockade`: `Restricted`, travel 1500, fuel 1250, текст причины «Пиратская блокада: рейс дольше и дороже».
- `event.quarantine`: `Unavailable`, travel/fuel 1000, текст «Карантин: направление временно закрыто».

## Implementation steps

1. Во всех map templates назначить ровно один safe short start edge и минимум один elevated edge так, чтобы base safe/risky различались по travel class/estimate и fuel multiplier. Не менять роли, coordinates, cargo flows или reference speed US-0004.
2. Для blockade/quarantine добавить минимум по три canonical candidate edge pairs в stable ordinal order. Quarantine candidates не должны быть bridges; negative fixture теста создаёт bridge отдельно, production content обязан иметь хотя бы один допустимый candidate для каждого template.
3. Player text явно называет событие и последствие; тексты не обещают бой, damage, точную прибыль или fuel charge. Остальные шесть event definitions и их market effects не менять.
4. Strict content tests проверяют IDs/refs/positive permille, все templates, Short/Medium/Long, safe/elevated distinction, отсутствие candidate на неизвестном/самозамкнутом edge и `CanApply` хотя бы для одного candidate каждого события/template.
5. Для seeds 0..255 и `ulong.MaxValue` материализовать map/event schedule через реальный loader. При одинаковом seed результат route snapshot до/во время/после события идентичен; во время события graph connected и Station screen model имеет enabled alternative. Разные seeds не обязаны отличаться попарно.
6. Public local-session integration использует временный scenario с фиксированным masterSeed, не добавляя четвёртый shipped scenario. Проверить exact expiry boundary: до конца modifier активен, на границе duration отсутствует и base values восстановлены. Default_500 и legacy scenario без trading map остаются без route rows/events.

## Out of scope

Изменение schema/Settings, новые события сверх восьми, окончательный баланс/маржа, global price knowledge, voyage/fuel code, новый UI, сценарий QA в репозитории и performance artifacts.

## Invariants

- Тематический content валидируется до старта: EngineRequirements.md:224–260.
- Восемь событий и альтернативный путь обязательны: TradingSystemMvpStories.md:227–253,368–384.
- Route risk размещён на graph edges, не в физическом пространстве: TradingSystemMvpStories.md:377–384; ../Documentation.md:99–100.
- Default_500 не становится экономической картой: TradingSystemMvpStories.md:819–832.
- Три allowed files, content-data layer, matching Client.Tests.

## Tests

`RouteRiskContentTests`:

- `Every_template_has_short_medium_long_safe_and_elevated_routes` (AC-01).
- `Safe_short_and_elevated_route_differ_in_travel_and_fuel` (AC-01).
- `Blockade_and_quarantine_candidates_resolve_and_preserve_an_alternative` (AC-03/04).
- `Packaged_event_reason_is_visible_in_station_route_rows` (AC-05).
- `Same_seed_replays_candidate_effective_values_and_expiry` (AC-06).
- `Expiry_boundary_restores_exact_base_values` (AC-06).
- `Default_500_and_legacy_scenario_do_not_gain_routes_or_events`.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~RouteRiskContentTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в трёх allowed files; dependency schemas/Settings/scenarios не изменены.
- AC-01/03/04/05/06 покрыты named strict/content/local-session tests на всех templates и seed corpus.
- Named tests/build/format проходят либо известные missing portrait assets записаны отдельно; их нельзя считать успехом route tests.
- Coefficients остаются data-driven baseline, graph connected, expiry exact, legacy/Default_500 unchanged.
- Нет незаписанных assumptions или hidden content; результат проверяем JSON diff и deterministic tests.

## Self-containment check

Files, exact baseline values/texts, candidate rules, seed corpus, expiry boundary, legacy behavior и commands заданы. Implementer не должен выбирать коэффициенты или создавать дополнительный scenario.
