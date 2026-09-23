---
epic: EP-0001-trading-system
story: EP-0001-US-0006-repeatable-trading-voyage
title: Повторяемый рейс между двумя рынками
stage: approved
dependencies: [EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0004-seeded-trading-map, EP-0001-US-0014-voyage-lifecycle]
created: 2026-09-21T11:05:34Z
source_request: "сделай тикеты для D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0006-repeatable-trading-voyage\\EP-0001-US-0006-repeatable-trading-voyage.md"
current_review: complete
revision: 2
---

# Повторяемый рейс между двумя рынками

## Входное техническое задание

Точное сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0006-repeatable-trading-voyage\EP-0001-US-0006-repeatable-trading-voyage.md». UTC фиксации контекста: 2026-09-21T11:05:34Z; время отправки сообщения недоступно.

Исходные ссылки: Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:137–147; TradingSystemMvpStories.md:16–18,338–341 в той же папке. Эпик ../Documentation.md:97–103,144,147,152. Сохраняется исходный scope: купить, уйти, долететь, состыковаться, продать и купить обратный груз без перезапуска. Жизненный цикл рейса принадлежит US-0014, топливная модель — US-0009.

Относительные пути ниже — от D:/DeepSpaceSaga/DSS. Ссылки path:line отражают grounding snapshot; планируемые dependency API явно отделены от текущего кода.

## User story

Как игрок, я хочу купить груз, выйти со станции, долететь до другого рынка и продать его, чтобы торговля работала как повторяемое путешествие. После ухода я больше не считаюсь пристыкованным к прежнему рынку и не могу торговать с ним локально. При стыковке с новой станцией мне доступен её рынок с фактическими грузом и балансом. Затем я могу купить обратный груз и повторить цикл в той же сессии. Запоздавшие ответы и окна прежней стоянки не возвращают старую котировку и не мешают новой торговле.

## Acceptance criteria

- AC-01: Два последовательных круга A → B → A в одной сессии содержат реальные Buy, Undock, physical flight, Dock, Sell и обратную покупку. Нельзя подменять рейс переносом координат, созданием cargo или LoadScenario на каждой остановке. Approach сохраняет скорость и не выполняет Dock.
- AC-02: После принятого ухода локальный рынок A недоступен; в полёте DockedStationTrade отсутствует, старый quote/новая команда торговли не меняют состояние. После прибытия рынок и quote bound именно к B, после возвращения — к новой стоянке A. Старый ответ A не становится текущим даже в цикле A → B → A.
- AC-03: На каждом торговом шаге cargo/stock/Credits меняются ровно на ExecutedQuantity/TotalCredits receipt; отказ или повтор CommandId не меняют их второй раз. Partial Sell сохраняет остаток. Рейс не сбрасывает груз/баланс; новые сборы учитываются отдельно, без объявления разницы Credits чистой прибылью.
- AC-04: Уход с блокирующим портовым обязательством не создаёт второй voyage и не уничтожает текущую стоянку. Принятый уход закрывает старое начисление; в полёте старый порт не продолжает выставлять сборы. Новое прибытие создаёт одну новую стоянку. Active voyage/reservation из US-0014 не дублируется при retry; формула и денежная оценка топлива остаются US-0009.
- AC-05: Existing Station → Trade работает при каждой новой стоянке; экран не открывается по stale station event в полёте и не остаётся доступным после потери локального контекста. Cargo и Credits приходят из свежего snapshot. История подтверждённых сделок сохраняется на протяжении сессии, но не используется как актуальная котировка.
- AC-06: Nested modal pause остаётся корректной: закрытие Trade возвращает на текущую Station, успешный Undock возвращает управление полётом через US-0014, отклонённый оставляет Station. Нет лишнего resume при снятии вложенных окон; повторные переходы не создают копий экранов. Сценарий проверяется headless tests и ручным проходом через существующий UI.

## Non-goals

Не реализовать заново Undock/Dock, active voyage persistence, quote calculator, trade executor или генератор карты. Нет телепортации, автоматической стыковки через Approach, изменения скоростей, расхода топлива отдельных engine-команд, расчёта routeFuelCost/COGS/net profit, нового Finance/Trade экрана, balance tuning или десятидневного прогона. Save/Load всей экономики относится к US-0012; здесь повторяемость означает продолжение той же сессии.

## Dependencies

- US-0003: receipt-based atomic trade и quote UI, включая её prerequisites US-0015/0001/0002. Approved tickets не означают реализованный API.
- US-0004: materialized stations/graph; на момент grounding есть только TK-0001/TK-0002, geometry/bootstrap/content не завершены на уровне тикетов.
- US-0014: Undock, states Docked/Undocking/InTransit/Docking/Docked, активный рейс, портовый gate и возврат UI к полёту. Story draft, конкретного runtime API пока нет.

### Required consumer contract — assumption A-01

Это требование потребителя US-0006 к незавершённой US-0014, а не утверждение о текущем API и не правка соседней story. До исполнения TK-0001 dependency должна предоставлять:

- `PlayerCommand(CommandId,ClientSequence,ObjectId,ModuleId,"navigation.undock",TargetObjectId: destinationStationId)` через существующий ReceiveCommand/IGameSessionConnection.SendCommandAsync. Module принадлежит игроку и exposes эту command. Команда не телепортирует и сохраняет скорость/направление при отделении; повтор ID не создаёт новую попытку.
- `AuthoritativeSnapshot.ActiveVoyage` nullable DTO с read-only свойствами `string VoyageId`, `string OriginStationObjectId`, `string DestinationStationObjectId`, `string State`, `long? ReservedFuelKg`. State использует значения US-0014. После завершения рейса ActiveVoyage=null. Не требуется новый тип в US-0006; имя CLR-типа не важно для доступа к этим свойствам.
- ReservedFuelKg null означает, что расчёт US-0009 ещё не подключён; это не бесплатный рейс и не свидетельство готовности топливной модели. Если значение есть, US-0006 проверяет сохранность/недублирование, но не вычисляет норму. Нельзя требовать реализации US-0009 до US-0006: у US-0009 обратная dependency.
- При каждой новой стоянке US-0014 публикует PortFees.FirstPortFeeGameTimeMs как timestamp её начала; это позволяет отличить повторный визит даже при пропуске промежуточных snapshots. DockedStationTrade публикуется только для фактически Docked player и StationObjectId совпадает с DockedStationObjectId. Отказ в уходе имеет terminal Rejected с непустым reason, состояние не меняется. Конкретные тексты/коды отказа определяет US-0014; consumer test не привязывается к новой выдуманной константе.
- US-0014 обрабатывает успешный Undock в существующем UI и сохраняет resume speed. US-0006 защищает station/trade контекст на этом переходе, но не повторяет state machine.

US-0003 consumer API уже задан в её story и TK-0001: `GetTradeQuote(TradeQuoteRequest)` / `IGameSessionConnection.GetTradeQuoteAsync`, PlayerCommand.QuoteId/MarketRevision, CommandResult.TradeReceipt(StationObjectId,ItemTypeId,QuoteId,RequestedQuantity,ExecutedQuantity,TotalCredits,...). Исполнение использует только выданную quote. Несовпадение prerequisite с этим разделом возвращает planning в review до реализации; запрещено скрыто расширять разрешённые файлы.

## Grounding

- EngineRequirements.md:121–129 сохраняет вектор при Undock; :5333–5341 запрещает менять скорость или автоматически стыковаться через Approach.
- Undock пока placeholder: src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:95. Station Trade click:128–129 возвращает OpenTrade без проверки текущей стоянки.
- src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:6–13 определяет только локальный рынок, keyed StationObjectId. Voyage API отсутствует в src/tests по targeted search; его контракт выше — planned dependency.
- TradeModel.Refresh: src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs:108–142 меняет rows по DockedStationTrade, но не хранит отдельную эпоху стоянки. TradeScreen.Refresh:32–42 и Submit:138–145 — точки обновления/отправки. US-0003 уже планирует async quote freshness; нужно сохранить этот механизм и добавить защиту round-trip контекста.
- Journal живёт на session handle: src/DeepSpaceSaga.Client/GameSessionHandle.cs:87; TradeModel.cs:59–81 хранит bounded entries/results независимо от жизни окна. Это не voyage ledger.
- SkiaWindow.OpenStationAsync:885–891 и OpenTradeAsync:911–917 проверяют тип текущего экрана, но не текущую docking identity. NavigateToStationAsync:895–899 может открыть Station после Pop. PollGameSessionAutoTransition:338–351 рассматривает Dialogue/GameSession, но не потерю контекста открытого Trade.
- Pause/resume сосредоточен в SkiaWindow.cs:796–828; ScreenStack.Pop:50–61 активирует предыдущий экран. Нельзя имитировать pause отдельными speed calls внутри Trade.
- Engine snapshot test seam: `CaptureSnapshotForTests(long gameTimeMs=0, SimulationSpeed? speed=null, long? simulationTimeMs=null)` в SimulationEngine.cs:1676; времена можно контролировать раздельно, без sleeps.
- Port fees фильтруют player/docked/due: SimulationEngine.PortFees.cs:15–45. Структура уже содержит отдельный долг и next due; US-0006 не меняет их расчёт.
- Real docking dialogue: Data/Dialogues/station-docking.json:12–13; tests/DeepSpaceSaga.Engine.Tests/DialogueTests.cs:20–30 использует truthful_id → accept_fee → continue. Во время этого диалога docking нельзя подменять тестовым присваиванием.

## Invariants and assumptions

- A-01: отсутствующая граница US-0014 задана выше явно; её реализация — prerequisite, не часть этих тикетов.
- A-02: proof of repeatability — два круга, минимум восемь сделок. Прибыльность не является критерием; receipt arithmetic проверяется точно, доходность проверяет US-0013.
- A-03: Context key — (session handle identity, station ObjectId, PortFees.FirstPortFeeGameTimeMs, локальная epoch пребывания). Epoch увеличивается при потере/смене локальной стоянки и не сбрасывается при возвращении на A. Не путать её с authoritative MarketRevision.
- A-04: При смене стоянки сбрасываются текущая selection/quantity/quote и transient controls; история сделок сохраняется. Поиск/filters можно сохранить; автоматического подтверждения после arrival нет.
- A-05: Engine ticket проверочный: существующий результат обязан обеспечиваться prerequisites. Если тест выявил дефект lifecycle/quotes, исправление принадлежит владельцу dependency; не добавлять production-файлы сверх Code context.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0006-TK-0001-round-trip-engine-proof | Сквозная проверка двух торговых кругов | engine | US-0003, US-0004, US-0014 | AC-01, AC-02, AC-03, AC-04 |
| EP-0001-US-0006-TK-0002-trade-visit-context | Котировка и выбор в контексте текущей стоянки | client | TK-0001 | AC-02, AC-03, AC-05 |
| EP-0001-US-0006-TK-0003-station-trade-navigation | Повторяемый переход Station → Trade → полёт | client | TK-0002 | AC-01, AC-02, AC-05, AC-06 |

Порядок TK-0001 → TK-0002 → TK-0003. Files touched: 2/3/5. Matching tests: Engine.Tests / Client.Tests / Client.Tests. План принят автоматическим workflow; дополнительного подтверждения не требуется.

## Gaps and backlog

- G-01: US-0014 draft без реализации/конкретного API; explicit consumer contract требует согласованной реализации до запуска TK-0001. US-0004 также не завершена. Это внешние implementation gates, не готовый runtime.
- G-02: US-0003 зависит от draft US-0015; готовность котировок проверяется до исполнения, не заменяется локальным unitPrice*quantity.
- G-03: route fuel arithmetic/settlement и денежная оценка — US-0009. До неё nullable reservation позволяет проверить lifecycle; fuel-complete evidence не заявляется. COGS/net profit — US-0010/0011; save continuity — US-0012; долгосрочный баланс — US-0013.
- G-04: requirements-engineer указан в Documentation/00-Process/AGENTS.md, но отсутствует среди доступных skills и в проверенных local skill/plugin paths. Используется явно заданный DSS-StoryBuilder workflow.
- Продуктовых вопросов нет. Если prerequisite API изменится, сначала review consumer contract; нельзя маскировать отсутствие dependency тестовым teleport/mock executor.

## Decision and review log

- 2026-09-21T11:05:34Z — выбран существующий canonical story; epic/story/path IDs согласованы, ticket folders отсутствуют, следующий номер0001. Короткие dependency labels исходного draft раскрыты до canonical story IDs без изменения графа.
- Grounding — обязательные process/architecture/EngineRequirements и релевантные sources прочитаны; отделены текущие API от отсутствующего lifecycle.
- Plan-review — три тикета автоматически приняты; производство кода не входит в текущую задачу.
- Создан TK-0001: engine, два test files; external gates и точный driver contract записаны.
- Создан TK-0002: client, три файла; reuse quote generation, lifetime стоянки и сохранение receipt history.
- Создан TK-0003: client, пять файлов; station/trade navigation guard и headless/modal/ручной evidence. Следующий шаг — validation artifacts.
- Review: timestamp начала стоянки добавлен в consumer/context key для пропущенных промежуточных snapshots; существующие StationScreenTests включены в allowed files вместо лишнего нового test file. File limits сохранены.
- 2026-09-21T11:16:10Z — complete: три canonical tickets; проверены ID пути/frontmatter/file, 11 разделов каждого тикета, matching projects, scopes2/3/5, dependency DAG и coverage AC-01..AC-06. Whitespace всех четырёх Markdown и git diff --check пройдены. Production-код не изменялся; dotnet tests/build и UI smoke не выполнялись в planning. External implementation gates остаются US-0003/0004/0014; незаданных продуктовых вопросов нет.
