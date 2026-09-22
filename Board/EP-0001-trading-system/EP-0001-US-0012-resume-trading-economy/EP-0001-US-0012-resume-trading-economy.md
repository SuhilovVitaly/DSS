---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
title: Продолжение торговой игры после сохранения
stage: approved
dependencies: [EP-0001-US-0002-market-replenishment, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0005-station-resource-fields, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0011-net-voyage-profit]
created: 2026-09-21T12:53:36Z
source_request: "сделай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0012-resume-trading-economy\\EP-0001-US-0012-resume-trading-economy.md"
current_review: complete
revision: 1
---

# Продолжение торговой игры после сохранения

## Входное техническое задание

Точное сообщение пользователя сохранено в `source_request`. Время фиксации grounding: 2026-09-21T12:53:36Z UTC; это не утверждение о времени отправки сообщения. Выбор US-0012 однозначен, ID эпика/story в пути, frontmatter, папке и имени файла совпадают.

Источники: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:125–147,225–235`, `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:14–36,243–253,463–469`, `Documentation/01-Requirements/EngineRequirements.md:210–261,314–348,3608–3623,3990–4006,4063–4074`, решения эпика `../Documentation.md:95–104,120–130,150,156–171`. Все относительные пути ниже считаются от `D:/DeepSpaceSaga/DSS`.

## User story

Как игрок, я хочу сохранить игру во время торговли или рейса и продолжить с тем же экономическим состоянием, чтобы загрузка не меняла выгодность решений. Восстанавливаются материализованная карта и ресурсные поля, склады и бюджеты, market revisions, активные события и их cursors, `masterSeed`, локально известные рыночные данные, active voyage, fuel reservation, финансовый ledger и durable receipts. Следующие экономические интервалы, события и результаты команд совпадают с непрерывной игрой при одинаковых дальнейших действиях. Повторная обработка после загрузки не начисляет производство, сборы, расход топлива, выручку или прибыль второй раз.

## Acceptance criteria

- AC-01: новый save содержит все mutable authoritative данные экономики, предоставленные зависимостями: materialized map/fields, stock, station budget, pending output, market revisions, active event instances/cursors, `masterSeed`, persisted market knowledge, active voyage/progress, fuel reservation/basis/settlement, active и closed voyage ledger, command/terminal receipts. Type definitions и вычисляемые display projections не дублируются.
- AC-02: при checkpoint до и после экономического интервала, сделки и event boundary непрерывный run и run с `CaptureSaveState → JSON → LoadScenario` имеют одинаковое нормализованное состояние; после одинаковых следующих команд и времени они дают одинаковые stock, цены, route availability, voyage, fuel и финансы.
- AC-03: сразу после load, до продвижения времени или новой команды, не происходит скачка stock, budget, market revision, цены, player/station money, active event duration, route terms, voyage progress, reserved fuel или ledger totals.
- AC-04: повтор сохранённого `CommandId`, terminal voyage callback или обработка уже пройденной time boundary возвращает исходный результат либо no-op и не создаёт вторую сделку, production batch, event activation, fuel settlement, fee или ledger entry.
- AC-05: экономическая схема повышает `SaveFormatVersion` на один относительно полностью merged prerequisite schema. Поддерживаемые legacy-классы перечислены и мигрируют только из сохранённых фактов; cargo/fuel basis и уже исполненные суммы не переоцениваются. Partial/corrupt state отклоняется до изменения текущего мира.
- AC-06: несовместимые catalog, market-profile, event/config или trading-map fingerprints отклоняются с понятным сообщением `Save was not modified`; неудачная запись не повреждает предыдущий slot. Совместимый save проходит реальный `SaveAsync → CreateFromSaveFile` round-trip.
- AC-07: station `MarketRevision`, revision/event allocators и last-known timestamps продолжаются монотонно. Ephemeral issued quote не становится исполнимым после новой session: UI запрашивает свежую котировку, а durable executed receipt остаётся доступным для replay.
- AC-08: active/closed ledger, cargo COGS, route fuel cost, port/event/passenger entries и partial-sale result восстанавливаются точно; после продолжения каждая финансовая операция относится к рейсу и учитывается ровно один раз.

## Non-goals

- Не реализовывать заново market flow, quote formula, сделки, генерацию карты/полей, события, voyage lifecycle, fuel accounting, cargo basis или ledger: это implementation prerequisites зависимых историй.
- Не добавлять новый save format UI, торговый экран, экран маршрутов или клиентский расчёт цены/прибыли.
- Не выполнять balance tuning, десятидневный прогон или доказательство интересности экономики — это US-0013.
- Не сохранять transient render state, открытое modal-окно, клиентский preview cache или issued quote payload. После load нужна свежая authoritative quote.
- Не обещать восстановление удалённого market knowledge, которого ещё не предоставляет US-0016; US-0012 сохраняет только knowledge/timestamps, реально введённые prerequisites к моменту реализации.
- Не исправлять произвольно повреждённые saves и не реконструировать неизвестную историческую цену груза/топлива из текущего catalog.

## Dependencies

Прямые зависимости: `EP-0001-US-0002-market-replenishment`, `EP-0001-US-0003-dynamic-market-trading`, `EP-0001-US-0005-station-resource-fields`, `EP-0001-US-0008-route-risk-and-alternatives`, `EP-0001-US-0009-voyage-fuel-cost`, `EP-0001-US-0011-net-voyage-profit`. Транзитивные implementation gates включают `EP-0001-US-0014-voyage-lifecycle` и `EP-0001-US-0015-authoritative-market-quotes`.

На момент planning US-0002…US-0010 представлены approved planning artifacts, US-0011/0014/0015 остаются draft без ticket maps, а соответствующих production APIs в текущем checkout нет. Это не блокирует декомпозицию, но каждый тикет начинает реализацию с проверки exact merged signatures; несовпадение возвращает тикет в review и не расширяет files скрытно.

## Grounding and invariants

- `GeneralSaveState` является authoritative continuation state и обязан хранить runtime metadata, RNG/counters, pending state и всё необходимое для продолжения: `Documentation/01-Requirements/EngineRequirements.md:210–246,3608–3623`.
- Одинаковые time, registry ids, RNG и runtime state после load должны давать тот же authoritative result; resource/RNG/command/event systems требуют save/load continuation test: `EngineRequirements.md:3990–4006,4063–4074`.
- Текущий schema root хранит command receipts, pending commands, economy cursor, motion time и catalog compatibility; текущая версия 8: `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:7–24,52–78`.
- Текущий save уже пишет player/station balances, inventory, events, cargo/fuel, receipts, economy cursor и catalog identity: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:760–833`; future prerequisite fields должны встраиваться в этот один authoritative capture path.
- Load сначала строит и валидирует runtime objects, а затем коммитит под `_worldStateLock`; catalog mismatch отклоняется до commit: `SimulationEngine.cs:196–233,319–359`.
- Loader запрещает unknown JSON fields, future version и неполный versioned state: `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:27–35,60–83,129–141,184–241`.
- Durable command journal ограничен 4096 receipts и восстанавливается вместе с pending commands: `src/DeepSpaceSaga.Engine/SimulationEngine.CommandJournal.cs:8–53`.
- Calendar/economy cursor обрабатывает границы в `(previous,target]`, а load устанавливает `_processedWorldTimeMs`, поэтому повторный snapshot не повторяет boundary: `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:8–51`; `SimulationEngine.cs:350–358`.
- LocalClient захватывает state под engine lock, сериализует во временный файл и atomically replaces slot: `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:158–191`. Save bootstrap загружает текущий registry до `LoadScenario(..., isSave:true)`: `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:23–35`.
- Calendar/economic time не ускоряет physical motion; эта история не меняет speed, Approach или module cycle semantics.

## Assumptions

- A-01: US-0012 добавляет manifest/validation и integration continuity, но не второй shadow copy mutable economy. Stocks/events/map/voyage/ledger остаются в DTO своих owning prerequisites.
- A-02: новый numeric save version выбирается как `merged CurrentSaveFormatVersion + 1` после всех prerequisites; несколько draft tickets, независимо называющих version 9/10, не считаются allocation. TK-0001 фиксирует фактический номер в комментарии и fixtures при реализации.
- A-03: legacy `SaveFormatVersion=0` остаётся New Game scenario. Versioned saves 1–4 без mandatory economy cursor, 5–6 без catalog identity и 7–8 без полного trading state не получают выдуманную незавершённую торговую сессию: они принимаются только существующим loader policy для pre-trading continuation и материализуют пустые новые runtime sections; наличие partial new fields при старой version отклоняется.
- A-04: prerequisite-era versions между 8 и новым current version мигрируют только если их собственные mandatory fields/fingerprints валидны. Отсутствующий новый manifest строится из persisted cursors/ids и empty optional collections; stock, cargo/fuel basis, prices, receipts, voyage/ledger amounts не пересчитываются.
- A-05: combined `ConfigurationFingerprint` — uppercase SHA-256 canonical ordinal payload semantic versions/fingerprints уже предоставленных catalog, market profiles, event catalog и trading-map rules. Он не включает mutable stocks, prices, clocks или cosmetic/localized text.
- A-06: issued quote cache ephemeral и очищается при load. `MarketRevision` и allocator cursor сохраняются; stale pre-save quote нельзя выполнить после load, но выполненная команда replay-ится по durable receipt.
- A-07: capture сортирует все semantically unordered collections ordinal/stable-id before serialization; JSON byte equality используется только там, где порядок является контрактом. Для equivalence сравнивается normalized authoritative state, исключая metadata name и client-only fields.
- A-08: compatibility/shape validation и staged runtime construction завершаются до изменения `_objects`, clocks, allocators, journals или economy state. Ошибка оставляет ранее загруженный engine неизменным.
- A-09: LocalClient не создаёт отдельную экономическую сериализацию; единственный payload — `SimulationEngine.CaptureSaveState()` через `ScenarioLoader.Serialize`.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0012-TK-0001-economy-save-schema | Версия и миграция экономического save | engine | prerequisite stories | AC-01, AC-05, AC-06 |
| EP-0001-US-0012-TK-0002-market-state-continuity | Непрерывность рынка и событий | engine | TK-0001; US-0002/0003/0005/0008/0015 | AC-01, AC-02, AC-03, AC-04, AC-07 |
| EP-0001-US-0012-TK-0003-voyage-ledger-continuity | Непрерывность рейса, топлива и ledger | engine | TK-0001; US-0009/0010/0011/0014 | AC-01, AC-02, AC-03, AC-04, AC-08 |
| EP-0001-US-0012-TK-0004-deterministic-economy-continuation | Эквивалентность непрерывной и загруженной игры | engine | TK-0002, TK-0003 | AC-02, AC-03, AC-04, AC-07, AC-08 |
| EP-0001-US-0012-TK-0005-local-save-roundtrip | Файловый Save/Load экономической сессии | local-client | TK-0004 | AC-03, AC-05, AC-06 |

Implementation files: 4 / 3 / 3 / 3 / 2. Dependency order: TK-0001 → (TK-0002 и TK-0003) → TK-0004 → TK-0005.

## Gaps and backlog

- G-01: US-0011, US-0014 и US-0015 ещё не имеют approved ticket maps; exact ledger/voyage/quote filenames и signatures должны совпасть с contracts, записанными в TK-0002/TK-0003, до implementation.
- G-02: draft dependency tickets независимо резервируют SaveFormat 9/10. US-0012 не угадывает итоговый номер; TK-0001 владеет единственным финальным bump поверх merged prerequisites и удаляет conflicting provisional bumps только в рамках отдельного implementation review.
- G-03: epic R03 требует migration fixtures. В этой story перечислены semantic legacy classes; exact numeric prerequisite versions после 8 фиксируются в TK-0001 после merge истории версий, не по draft allocations.
- G-04: US-0016 выполняется позже и не является зависимостью US-0012. Persistence будущего remote market knowledge добавляется отдельной совместимой schema evolution; здесь не вводится скрытая зависимость.
- G-05: `requirements-engineer`, требуемый repository guide для requirements planning, отсутствует среди доступных skills; применён нормативный DSS-StoryBuilder workflow из текущего `AGENTS.md`.
- Блокирующих вопросов нет. Production code, epic Documentation и requirements не изменялись; tests/build не запускались для planning-only задачи.

## Decision and review log

- 2026-09-21T12:53:36Z — записано точное сообщение пользователя; выбран canonical US-0012, ticket folders отсутствовали, первый свободный номер `0001`.
- Grounding: прочитаны обязательные process/architecture/requirements, эпик, story, source concept/MVP, dependency stories/tickets и текущие code/test surfaces.
- Plan-review: принят split из пяти ticket merge units, каждый с одним layer и максимум пятью implementation files. A-01…A-09 разрешают migration/quote/dependency неопределённости без блокирующего вопроса.
- Созданы TK-0001…TK-0005 в dependency order; production code и соседние dirty changes не редактировались.
- Artifact validation: canonical paths/frontmatter, matching test projects, file limits, served AC coverage и acyclic dependency graph проверены; workflow complete.
