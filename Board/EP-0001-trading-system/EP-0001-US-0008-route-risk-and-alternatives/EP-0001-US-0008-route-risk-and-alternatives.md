---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
title: Рискованные маршруты и доступная альтернатива
stage: approved
dependencies: [EP-0001-US-0004-seeded-trading-map, EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0007-temporary-market-events]
created: 2026-09-21T11:10:35Z
source_request: "сделай тикеты для D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0008-route-risk-and-alternatives\\EP-0001-US-0008-route-risk-and-alternatives.md"
current_review: complete
revision: 2
---

# Рискованные маршруты и доступная альтернатива

## Входное техническое задание

Точное сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0008-route-risk-and-alternatives\EP-0001-US-0008-route-risk-and-alternatives.md». UTC фиксации контекста: 2026-09-21T11:10:35Z; время отправки сообщения недоступно.

Исходный объём: Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:149–161 и TradingSystemMvpStories.md:227–253,368–384,459–461. Эпик уточняет authoritative границы в ../Documentation.md:97–103 и dependency order в :156–172. Все относительные paths ниже считаются от D:/DeepSpaceSaga/DSS; path:line описывает grounding snapshot, будущие dependency API явно обозначены как планируемые.

## User story

Как игрок, я хочу перед уходом со станции различать безопасное короткое направление и более рискованные варианты, чтобы выбирать условия рейса осознанно. Для каждого доступного направления я вижу authoritative класс расстояния, оценку времени, fuel multiplier, риск и понятную причину временного ограничения. Пиратская блокада и карантин меняют только экономические свойства логических рёбер: доступность, travel estimate, fuel multiplier или рыночный эффект; они не меняют физическую скорость и правила Approach. Недоступный вариант нельзя начать, но остаётся доступная цепочка рёбер, по которой снабжение можно продолжить без перезапуска. После завершения события значения возвращаются к базовым параметрам карты.

## Acceptance criteria

- AC-01: На одной материализованной карте есть Short <=2 игровых часов, Medium >2–6 и Long >6 по reference ship; безопасный короткий и рискованный вариант отличаются минимум по двум authoritative параметрам из travel estimate, fuel multiplier, availability и market effect.
- AC-02: Snapshot публикует только Engine-рассчитанные направления от текущей пристыкованной станции: origin/destination, distance class, base/effective travel estimate, base/effective fuel multiplier, risk, availability, active event IDs и понятную причину. Legacy snapshot без массива остаётся валиден; точные удалённые цены не раскрываются.
- AC-03: Активная pirate blockade или quarantine детерминированно меняет заданные рёбра. `Unavailable` нельзя выбрать или начать; `Restricted` использует effective travel estimate при старте рейса. Fuel multiplier доступен следующей US-0009, но эта история не списывает топливо.
- AC-04: Перед применением закрытия Engine проверяет результирующий граф. Все пять станций остаются связными доступными рёбрами, а endpoints каждого базового cargo flow остаются достижимы. Невалидный candidate не активируется и не маскируется клиентом; порядок fallback-кандидатов детерминирован.
- AC-05: В существующем Station screen маршрутные строки показывают безопасный/рискованный статус, время, fuel multiplier и причину события. Недоступный вариант disabled, а минимум один рабочий вариант остаётся видимым. Клиент не пересчитывает эффекты и не создаёт новый экран маршрутов/рынка.
- AC-06: После expiry базовые availability/time/fuel восстанавливаются ровно один раз. Save/Load активного события и рейса через зависимости US-0007/US-0006 сохраняет те же effective условия; одинаковые seed/config/время дают те же эффекты и альтернативу.

## Non-goals

Нет боя, пиратских столкновений, радиационного урона, collision/obstacle routing, изменения SpeedKmS, перепланирования Approach, нового экрана маршрутов, раскрытия удалённых точных цен, расчёта прибыльности, балансных коэффициентов или списания route fuel. US-0007 владеет жизненным циклом восьми событий и рыночными price/stock effects; US-0006/0014 — состояниями рейса; US-0009 — резервированием/списанием топлива; US-0013 — численным балансом; US-0016 — удалённым знанием рынка.

## Dependencies

- EP-0001-US-0004-seeded-trading-map должна предоставить materialized `TradingMapStateData` и `TradingMapEdgeData(FromStationObjectId, ToStationObjectId, DistanceKm, TravelEstimateGameTimeMs, DistanceClass, FuelMultiplierPermille, RiskProfileId)` из её TK-0001, а также связный граф/cargo flows из TK-0002. На момент grounding созданы только planning artifacts TK-0001/TK-0002; schema ещё отсутствует в production `GameStateData` (`src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:52–73`).
- EP-0001-US-0007-temporary-market-events должна предоставить lifecycle активных событий, player-facing description, stable event ID, priority, start/duration и нормализованные route effects. Текущая schema умеет сохранять только price factors (`ScenarioData.cs:245–289`), runtime применяет их по времени (`SimulationEngine.cs:1608–1630`), но trigger/lifecycle отсутствует (`ScenarioData.cs:156–160`, `SimulationEngine.cs:1420–1428`).
- EP-0001-US-0006-repeatable-trading-voyage транзитивно через US-0014 должна предоставить authoritative выбор destination и активный voyage. Сейчас Undock остаётся placeholder (`src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:83–96`), а EngineRequirements.md:5076–5081 прямо фиксирует отсутствие Undock.
- Эти dependencies являются implementation gates. Если их фактические имена типов/файлов отличаются от contracts, записанных в тикетах, текущий тикет возвращается в review; запрещено молча расширять US-0008 или редактировать соседнюю story.

## Grounding

- Engine является владельцем механики; UI читает snapshot и отправляет команды только через публичную границу: Documentation/01-Requirements/EngineRequirements.md:362–419; Documentation/00-Process/CLAUDE.md:21–54.
- Settings и тематические JSON должны пройти строгую syntax/semantic validation до старта: EngineRequirements.md:224–260. RNG обязан быть named, independent и persisted: :314–348.
- Эпик определяет логический граф, Short/Medium/Long и базовые edge metadata; риск не меняет физическое движение: ../Documentation.md:97–100. Concept запрещает отдельное окно маршрутов и требует использовать station/map data: TradingSystemConcept.md:175–205.
- Текущий `AuthoritativeSnapshot` заканчивается optional route-arrival/simulation fields и не содержит экономических направлений: src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11–57. `BuildSnapshot` формируется в Engine и уже добавляет docked trade projection: src/DeepSpaceSaga.Engine/SimulationEngine.cs:510–534.
- `StationTradeSnapshot` публикуется только при фактической стыковке и не раскрывает скрытый budget: src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:6–21; аналогичный locality gate применяется к route choices.
- Station screen получает только SnapshotBuffer/session, Undock не реализован, а layout имеет фиксированные button rows: src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:29–96; StationLayout.cs:22–59. UI-тесты уже живут в `StationScreenTests`: tests/DeepSpaceSaga.Client.Tests/StationScreenTests.cs:10–20.
- Календарное и физическое время разделены; экономические boundaries сопоставляются движению, не ускоряя циклы: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:12–51. Effective travel меняет только voyage schedule зависимости, не motion clock.

## Invariants and assumptions

- A-01: Базовое ребро не мутируется. Evaluator формирует immutable effective view на `GameTimeMs`; после expiry отсутствие modifiers автоматически возвращает base values.
- A-02: Route availability имеет три значения: `Available`, `Restricted`, `Unavailable`. `Restricted` можно выбрать, `Unavailable` нельзя. Риск имеет `Safe` или `Elevated`; UI не выводит вероятность боя, потому что боевой модели нет.
- A-03: Permille modifiers положительны; travel/fuel factors комбинируются в порядке `(Priority desc, StartedGameTimeMs, EventId ordinal)` через checked integer/decimal arithmetic с `MidpointRounding.AwayFromZero`. Availability использует самый строгий уровень, reason/events сохраняют тот же deterministic order.
- A-04: US-0007 передаёт в US-0008 только уже активные нормализованные edge modifiers: eventId/type/priority/start, canonical unordered endpoints, availability, travel/fuel permille и player text. Scheduling, station price factors и лимит двух событий остаются US-0007.
- A-05: Проверка альтернативы требует связности доступного подграфа всех пяти станций. Это сильнее минимального «один сосед», но прямо обеспечивает достижимость endpoints всех cargo flows; `Restricted` считается проходимым.
- A-06: Event scheduler US-0007 перебирает seed-derived candidates в стабильном порядке и активирует первый, который проходит `CanApply`; если ни один не проходит, событие не стартует в этот interval. Нельзя silently downgrade `Unavailable` до `Restricted`.
- A-07: Snapshot содержит только исходящие рёбра текущей docked station, отсортированные по destination ID. Это даёт выбор без бесплатного глобального рынка; active voyage использует собственную проекцию US-0006/0014.
- A-08: Effective travel фиксируется в создаваемом voyage при успешном старте. Уже начатый рейс не ускоряется/останавливается из-за последующего event expiry. Fuel multiplier фиксируется рядом для US-0009, но fuel не списывается здесь.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0008-TK-0001-route-risk-contract | Контракт риска и доступности маршрута | contracts | US-0004 materialized edge contract | AC-02, AC-05 |
| EP-0001-US-0008-TK-0002-effective-route-evaluator | Effective route и гарантия альтернативы | engine | TK-0001; US-0004 graph; US-0007 normalized modifiers | AC-01, AC-03, AC-04, AC-06 |
| EP-0001-US-0008-TK-0003-route-event-voyage-integration | События, выбор рейса и snapshot | engine | TK-0002; US-0006/0014 voyage; US-0007 lifecycle | AC-02, AC-03, AC-04, AC-06 |
| EP-0001-US-0008-TK-0004-route-choice-presentation | Риск и причины в Station screen | client | TK-0003; US-0006/0014 destination control | AC-01, AC-02, AC-05 |
| EP-0001-US-0008-TK-0005-route-risk-content | Контрольная карта и события маршрутов | content-data | TK-0003, TK-0004; US-0004/0007 shipped content | AC-01, AC-03, AC-04, AC-05, AC-06 |

Dependency order: TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. Implementation file counts: 3/2/5/4/3. Matching tests: Contracts.Tests, Engine.Tests, Engine.Tests, Client.Tests, Client.Tests.

## Gaps and backlog

- G-01: US-0004 имеет только два из пяти запланированных тикетов; materialized geometry/bootstrap/content отсутствуют даже как завершённая ticket map. До их реализации TK-0002/0005 закрыть нельзя.
- G-02: US-0006, US-0007 и US-0014 пока draft без тикетов. Их expected contracts в этой story — integration gates, не утверждение, что production API существует.
- G-03: Точная формула/коэффициенты blockade/quarantine относятся к US-0013. TK-0005 задаёт только проверочный baseline, достаточный для различия параметров и альтернативы; balance sign-off не заявляется.
- G-04: Эта story не резервирует новый SaveFormatVersion: persistence событий/active voyage принадлежит dependencies. Если US-0008 добавит собственное сохраняемое состояние вопреки assumptions, требуется возврат в review и отдельное compatibility решение.
- G-05: `fuelMultiplier` здесь наблюдаем и фиксируется в voyage, но фактическое `routeFuelCost` остаётся US-0009; отдельные engine-команды топливо не расходуют (TradingSystemMvpStories.md:403–415).
- G-06: В памяти найден старый объединённый planning run по item catalog/dynamic trading; он подтверждает только то, что route/dynamic-event execution тогда оставались открытыми. Текущие files и epic проверены заново.
- Блокирующих продуктовых вопросов нет. Открытые dependency implementation gaps не скрыты.

## Decision and review log

- 2026-09-21T11:10:35Z — точное сообщение пользователя и выбранный canonical path записаны выше; IDs path/folder/frontmatter совпадают, исходных ticket folders не было.
- Grounding: обязательные process/architecture/requirements, эпик, story, dependency stories, source concept/MVP и code/test surface проверены. Production-код и requirements не изменялись.
- Plan-review: пять тикетов приняты автоматическим workflow; отдельное подтверждение пользователя не требуется.
- Созданы TK-0001…TK-0005 в dependency order; layer/test project, file limits, dependency gates и coverage записаны в каждом.
- 2026-09-21T11:10:35Z — complete: canonical story и пять ticket artifacts подготовлены; implementation и dotnet-команды не выполнялись.
