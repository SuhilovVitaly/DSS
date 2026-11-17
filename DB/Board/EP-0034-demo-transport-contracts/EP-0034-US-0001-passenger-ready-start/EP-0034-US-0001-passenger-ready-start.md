---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0001-passenger-ready-start
title: Готовый носитель с пассажирскими местами
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start]
created: 2026-11-16T19:49:59Z
implementation_status: implemented
---

# Готовый носитель с пассажирскими местами

## Связь с техническим заданием

Одобренный план, Д4: Контракты пассажирских и грузовых перевозок. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Переиспользовать пассажирские данные, календарь и voyage ledger; закончить доступный игроку цикл. Новая демо-комплектация не меняет старые сохранения EP-0029.

## User story

Как игрок, я начинаю демо на готовом Tetrarch и могу принять небольшой пассажирский заказ уже в первом рейсе. Шесть стартовых членов экипажа и два резервных места сохраняются, а пассажирская вместимость добавлена отдельно.

## Зависимости

- [EP-0032-US-0001-campaign-roundtrip](../../EP-0032-demo-campaign-continuity/EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md)
- [EP-0032-US-0004-demo-campaign-start](../../EP-0032-demo-campaign-continuity/EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)

## Completion evidence

- **AC-01.** В новой демо-игре видны экипаж, резерв и положительное число свободных пассажирских мест.
- **AC-02.** Комплектация допустима по корпусу, массе, размещению и жизнеобеспечению; другие сценарии и существующие saves не получают бесплатного расширения.
- **AC-03.** Проверить физический первый рейс и доступный заказ без предварительной покупки жилья.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

A-02: точное число добавленных мест и допустимая компоновка — результат выбора content до тикетов; минимум один доступный пассажирский оффер.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Пользователь одобрил общий план и поручил материализацию. Детализация этой истории подготовлена агентом в рамках одобренного объёма; отдельное human approval формулировок и допущений не заявляется. Исторически story detail был draft; текущий workflow status указан в frontmatter и новой карте ниже. Это не препятствует завершению запрошенного создания документов и не разрешает реализацию.

- 2026-11-16T19:49:59Z — время фиксации запроса в Board, не доказанное время отправки: «План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась. При будущей подготовке соблюдать предел пяти тикетов на историю; если детализация не помещается, сначала разделить story.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Создание Markdown не закрывает эти gates.


## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** В новой демо-игре видны экипаж, резерв и положительное число свободных пассажирских мест.
- **AC-02.** Комплектация допустима по корпусу, массе, размещению и жизнеобеспечению; другие сценарии и существующие saves не получают бесплатного расширения.
- **AC-03.** Проверить физический первый рейс и доступный заказ без предварительной покупки жилья.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0032-US-0001-campaign-roundtrip](../../EP-0032-demo-campaign-continuity/EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md)
- [EP-0032-US-0004-demo-campaign-start](../../EP-0032-demo-campaign-continuity/EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0034-US-0001-TK-0001-contracts](EP-0034-US-0001-TK-0001-contracts/EP-0034-US-0001-TK-0001-contracts.md) | Контракт: Готовый носитель с пассажирскими местами | contracts | EP-0032-US-0001-TK-0003-client, EP-0032-US-0004-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0034-US-0001-TK-0002-content](EP-0034-US-0001-TK-0002-content/EP-0034-US-0001-TK-0002-content.md) | Контент: Готовый носитель с пассажирскими местами | content-data | EP-0034-US-0001-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0034-US-0001-TK-0003-engine](EP-0034-US-0001-TK-0003-engine/EP-0034-US-0001-TK-0003-engine.md) | Правила: Готовый носитель с пассажирскими местами | engine | EP-0034-US-0001-TK-0002-content | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0034-US-0001-TK-0005-localized-feedback](EP-0034-US-0001-TK-0005-localized-feedback/EP-0034-US-0001-TK-0005-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0034-US-0001-TK-0003-engine | AC-01, AC-02, AC-03, AC-04 |
| 5. [EP-0034-US-0001-TK-0004-client](EP-0034-US-0001-TK-0004-client/EP-0034-US-0001-TK-0004-client.md) | Экран и причины отказа: Готовый носитель с пассажирскими местами | client | EP-0034-US-0001-TK-0005-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Story validation (2026-11-17)

Implementation: 5/5 tickets implemented and published on `feature/ep-0034-demo-transport-contracts` (origin). Automated story-scoped tests: own tests PASS; neighbouring regressions show no new failure versus base `e89980b4`. Native window, manual playtest and product acceptance: **NOT RUN / OPEN** (owner reserved native/manual checks; headless render is not a native check).

| Ticket | Commit(s) |
|---|---|
| EP-0034-US-0001-TK-0001-contracts | `89c2c929` |
| EP-0034-US-0001-TK-0002-content | `af463f4e`, correction `a6cbce6d` |
| EP-0034-US-0001-TK-0003-engine | `1329c059` |
| EP-0034-US-0001-TK-0005-localized-feedback | `bc87d851`, correction `ba14ef23` |
| EP-0034-US-0001-TK-0004-client | `406522f3` |

Result: the demo New Game starts on the Tetrarch from `Data/Ships/demo-starter-carrier.json` (shared starter + `MOD-PLAYER-LIVING-QUARTERS-03`): 12 cabins = 6 crew + 2 reserved + 4 free. Engine projects `DemoCampaignSnapshot.Transport.PassengerCapacity` (derived, never saved) with stable blockers; the Ship screen shows the split and a localized RU/EN availability/blocker line. Tutorial/Default, the shared starter and existing saves receive nothing.

AC contribution: AC-01 — crew, reserve and 4 free cabins in a new demo (engine + Ship screen tests). AC-02 — real catalog module on a free hull cell, placement/overlap validated by loader and engine, 0 W, rations cover a full complement for a day; other scenarios and Load get no extra cabins. AC-03 — first physical flight with 4 passengers: ship moves, exactly 20 rations/day for 10 people; no housing purchase needed. A concrete passenger offer comes from EP-0034-US-0006/US-0002 (not in this story). AC-04 — blockers shown as RU/EN explanations, capacity rejected if smuggled into a save, reopen sends nothing. Native/product evidence for AC-01…AC-04 remains OPEN.

Commands (repository root `D:/DeepSpaceSaga/Dss-Main`, each process ≤ 300000 ms, artifacts in `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0001-20261117/`):

- `dotnet test tests/DeepSpaceSaga.Contracts.Tests -c Release --filter FullyQualifiedName~DemoEp0034Us0001` → 3 passed (`own-contracts.log`); broader `Demo.|Ep0029|ArchitectureTests` → 26 passed (`story-contracts2.log`).
- `dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --filter "DemoEp0034Us0001|DemoEp0032Us0004Tk0002Tests|ScenarioEngineTests|ModuleHealthBootstrapTests.Default_modules_have_structure"` → 77 passed (`own-engine.log`).
- `dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --filter FullyQualifiedName~DemoEp0034Us0001` → 11 passed (`own-client.log`).
- Engine default-scenario regression set (33 classes, `regression-filter.txt`) → 319 passed / 8 failed (`final-engine-default.log`); the same 8 fail on base `e89980b4` (`baseline-engine-default.log`): Demo without `ConfigureDemoCampaign` in raw `LoadScenario` (3), missing `VData/Planning/V4.0/Reference/battles.json` (3), `AllShippedScenariosCovered` lacks Demo, Demo has no SolarSystemMap.
- Client regression set (`client-filter.txt`) → 184 passed / 30 failed (`final-client.log`); identical failing set on base (`baseline-client.log`, `comm` diff empty). `Ep0029Us0009Tk0003Tests` (2 rearm-text failures) also fail on base (`baseline-client-rearm.log`).
- `DemoEp0032*` engine tests read content from the hard-coded sibling checkout `D:/DeepSpaceSaga/DSS` (`DemoEp0032Us0001Tk0002Tests.ContentRoot`); during this run that tree held in-progress EP-0033 content (`market.industrial` initial inventory outside supply/demand), so 15 of them failed for reasons outside this checkout (`final-engine-story.log`). An earlier run in the same session passed 130/130 (`story-engine2.log`). Recorded as an open issue for the epic, not fixed here.
- Builds: Contracts.Tests, Engine.Tests, Client.Tests (Release) — 0 warnings, 0 errors.

Assumptions: A-01 (no command in US-0001; partial hooks untouched to avoid EP-0033 collision), A-02 (DTO names), A-03 (no class mass limit; mass = catalog module mass), A-04 (no persisted state), A-05 (life-support reported, not load-rejected), A-06/A-07 (no pending/stale texts or UI until US-0002). No save-format bump (A-P2 not triggered). Self-review only; no independent review is claimed.
