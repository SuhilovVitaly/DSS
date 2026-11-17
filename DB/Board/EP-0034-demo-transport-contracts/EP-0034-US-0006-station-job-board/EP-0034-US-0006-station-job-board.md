---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0006-station-job-board
title: Доска заказов станции
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0032-US-0004-demo-campaign-start]
created: 2026-11-16T20:31:00Z
implementation_status: implemented
---

# Доска заказов станции

## Связь с техническим заданием

Одобренный план, Д4: Контракты перевозки — «общая доска и журнал». [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). История добавлена при проверке достаточности EP-0032…EP-0041 для первой демо 2026-11-16.

## User story

Как игрок, я прихожу на станцию и вижу ограниченный список доступных заказов: пассажиры, грузы, а позже и боевые цели. Набор зависит от роли станции, обновляется со временем и предлагает разные направления, срочность и оплату. Один экран журнала показывает все мои принятые обязательства.

## Зависимости

- [EP-0032-US-0004-demo-campaign-start](../../EP-0032-demo-campaign-continuity/EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)

## Completion evidence

- **AC-01.** Engine формирует предложения детерминированно по seed и роли станции; их число ограничено, у предложения есть срок публикации, обновление идёт по игровому календарю и не зависит от повторного открытия экрана.
- **AC-02.** Оплата предложения объяснима: она зависит от расстояния, срочности и известного риска маршрута; заказы ведут к станциям демо-кластера и выполнимы текущим носителем.
- **AC-03.** Экран Contracts перестаёт быть заглушкой: доска показывает типы заказов EP-0034 и EP-0036, а единый журнал обязательств доступен с карты и со станции.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Не универсальный генератор миссий: только типы заказов демо. Формула оплаты и частота обновления — численные настройки EP-0037. Существующий `ContractsScreen` — заглушка (`DB/Documentation/02-FirstRelease/Screens/Contracts.md`).

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Добавлена агентом при проверке достаточности историй. Отдельное human approval формулировок не заявляется: story stage — draft, plan_status — approved.

- 2026-11-16T20:31:00Z — время фиксации поручения в Board, не доказанное время отправки: «Эпики с 32 по 41 это твоя совместная работа с другим агентом. вы дополняете друг друга. твоя задача пройти по всем этим эпикам, понять их цель, решить достаточны ли эти истории или нет и если нет добавить новые. Цель - подготовить эпики с 32 по 41 к первой демо игры».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась. Если детализация не помещается в пять тикетов, сначала разделить story.
- Implementation: 5/5 tickets published (see Story validation below). Automated story-scoped tests: PASS for own tests, no new regression vs base. Native/manual/product acceptance: NOT RUN / OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Engine формирует предложения детерминированно по seed и роли станции; их число ограничено, у предложения есть срок публикации, обновление идёт по игровому календарю и не зависит от повторного открытия экрана.
- **AC-02.** Оплата предложения объяснима: она зависит от расстояния, срочности и известного риска маршрута; заказы ведут к станциям демо-кластера и выполнимы текущим носителем.
- **AC-03.** Экран Contracts перестаёт быть заглушкой: доска показывает типы заказов EP-0034 и EP-0036, а единый журнал обязательств доступен с карты и со станции.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0032-US-0004-demo-campaign-start](../../EP-0032-demo-campaign-continuity/EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0034-US-0006-TK-0001-contracts](EP-0034-US-0006-TK-0001-contracts/EP-0034-US-0006-TK-0001-contracts.md) | Контракт: Доска заказов станции | contracts | EP-0032-US-0004-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0034-US-0006-TK-0002-content](EP-0034-US-0006-TK-0002-content/EP-0034-US-0006-TK-0002-content.md) | Контент: Доска заказов станции | content-data | EP-0034-US-0006-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0034-US-0006-TK-0003-engine](EP-0034-US-0006-TK-0003-engine/EP-0034-US-0006-TK-0003-engine.md) | Правила: Доска заказов станции | engine | EP-0034-US-0006-TK-0002-content | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0034-US-0006-TK-0005-localized-feedback](EP-0034-US-0006-TK-0005-localized-feedback/EP-0034-US-0006-TK-0005-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0034-US-0006-TK-0003-engine | AC-01, AC-02, AC-03, AC-04 |
| 5. [EP-0034-US-0006-TK-0004-client](EP-0034-US-0006-TK-0004-client/EP-0034-US-0006-TK-0004-client.md) | Экран и причины отказа: Доска заказов станции | client | EP-0034-US-0006-TK-0005-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Story validation (2026-11-17)

Implementation: 5/5 tickets implemented and published on `feature/ep-0034-demo-transport-contracts` (origin). Automated story-scoped tests: own tests PASS; neighbouring regressions show no new failure versus the US-0001 base. Native window, manual playtest and product acceptance: **NOT RUN / OPEN** (headless raster renders of the Contracts screen were inspected, which is not a native check).

| Ticket | Commit(s) |
|---|---|
| EP-0034-US-0006-TK-0001-contracts | `759692d7` |
| EP-0034-US-0006-TK-0002-content | `81e358c1` |
| EP-0034-US-0006-TK-0003-engine | `7aec0af0`, test correction `4c1e3689` |
| EP-0034-US-0006-TK-0005-localized-feedback | `559ce508` |
| EP-0034-US-0006-TK-0004-client | `ec009bf5` |

Result: in a demo session the engine publishes, per calendar day, up to 6 offers on each of the 5 cluster boards (passenger, urgent passenger, cargo; `bounty` reserved for EP-0036) from the named stream `station-jobs/v1:{station}:{bucket}:{slot}`; offers keep frozen terms for 2 days and are saved. The first publication guarantees an ordinary 3-passenger job on the shortest route from the start station. Accepting (`AcceptJobOfferCommand` through the campaign command path) records one `Active` obligation with a calendar deadline from acceptance; duplicates return the receipt, stale/expired/foreign-station/no-capacity offers are refused with stable codes. The Contracts screen shows the docked station's board with explained payout and the journal; from the map (J button, Ctrl+J) it shows the journal in flight.

AC contribution: AC-01 — deterministic per seed and role (twin sessions equal), cap 6, publication bucket/expiry by game calendar, unchanged by repeated snapshots/reopen, paused New Game already has a board (engine tests). AC-02 — payout = ceil((base + load + 3/km × route km) × urgency‰ × risk‰), terms shown on the card; destinations are direct cluster neighbours with known risk; counts/mass bounded by carrier cabins/hold (engine + content tests). AC-03 — placeholder replaced; board types passenger/urgent/cargo shown, `bounty` type and texts ready for EP-0036; journal from station and map (client tests). AC-04 — RU/EN reason texts for every code, pending/confirmed/rejected states, no resubmit on reopen, Save/Load keeps offers/journal/receipts, corrupted board rejects Load atomically (engine + client tests). Native/product evidence for AC-01…AC-04 remains OPEN; delivery, payout and failure of obligations are delivered by US-0002/US-0003/US-0004.

Commands (repository root `D:/DeepSpaceSaga/Dss-Main`, each process ≤ 300000 ms, artifacts in `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0006-20261117/story/`):

- `dotnet test tests/DeepSpaceSaga.Contracts.Tests -c Release --filter "Demo.|ArchitectureTests|Ep0029"` → 29 passed (`contracts.log`).
- `dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --filter "<US-0001 regression-filter.txt>|DeepSpaceSaga.Engine.Tests.Demo."` → 352 passed / 8 failed (`engine-reg.log`); the 8 are exactly the US-0001 baseline set (`engine-failed.txt` vs `us0001-20261117/engine-failed-final.txt`): MasterSeed default scenario, ModuleHealthBootstrap ×2, ScenarioGroupTranslation ×2, LocalSessionBattle ×3. All `DemoEp0034*` and `DemoEp0032*` demo tests pass, including `DemoEp0034Us0006Tk0003Tests` 3/3.
- `dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --filter "<US-0001 client-filter.txt>|DemoEp0034|ContractsScreenTests|MechanicsPanelTests|StationScreenTests|ManualManeuverInputTests|GameSessionDockAutoOpenTests|Locali"` → 297 passed / 32 failed (`client-reg.log`); 30 = the US-0001 baseline set, 2 = `SessionHandleBattleTests.EveryRefusalHasRealLocalizedText…` (EN/RU) newly selected by the `Locali` filter, failing on the missing key `TacticalBattle.Refusal.NoAmmo`, which is absent at base `c7d39b24` too (not touched by this story). Own client tests `DemoEp0034Us0006Tk0002/Tk0004/Tk0005Tests` 3 + 6 + 3 passed.
- Builds: Contracts.Tests, Engine.Tests, Client.Tests (Release) — 0 warnings, 0 errors.

Defects found and fixed during the story run (`4c1e3689`, tests only): US-0001 purity check compared transport records whose new `ImmutableArray` members compare by reference; EP-0032 demo tests read the sibling EP-0033 checkout (`ContentRoot`) — now this checkout.

Assumptions (details in tickets): TK-0001 A-01…A-03 (generic accept command, explicit verdicts, integer terms); TK-0002 A-01…A-03 (baseline numbers for EP-0037, fingerprint unaffected, ordinary goods); TK-0003 A-01…A-05 (2-day offers, explicit dispatch instead of shared partial hooks, bucket-start times and frozen whole-km distance, acceptance only records the obligation, no save-format bump); TK-0005 A-01/A-02; TK-0004 A-01…A-03 (no remote boards in flight, refresh with next snapshot, Ctrl+J). Self-review only; no independent review is claimed.
