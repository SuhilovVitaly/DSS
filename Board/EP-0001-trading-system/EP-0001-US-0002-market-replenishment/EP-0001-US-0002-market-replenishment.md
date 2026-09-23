---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
title: Восстановление и расходование запасов станций
stage: approved
dependencies: [EP-0001-US-0001-station-market-profiles]
created: 2026-09-21T08:59:06Z
source_request: "сделай следующую стори"
current_review: complete
revision: 1
---

# Восстановление и расходование запасов станций

## Входное техническое задание

Точное сообщение пользователя: «сделай следующую стори». 2026-09-21T08:59:06Z — время регистрации запроса в planning log, не время его отправки. A-01: выбрана US-0002, следующая после подготовленной US-0001 по порядку эпика Documentation.md:158; менять порядок или создавать новую задачу не требуется.

Исходные ссылки: Concept:82–96; MVP:48–102, 189–199, 447–449. Полные документы — Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md и TradingSystemMvpStories.md. Продуктовые уточнения — Board/EP-0001-trading-system/Documentation.md:93–95, 108–118. Все пути к исходникам и требованиям в этом файле относительны D:/DeepSpaceSaga/DSS.

Исходная история: «Как игрок, я хочу видеть, как станция производит и потребляет товары со временем, чтобы выбирать между ожиданием пополнения и перелётом к другому рынку. Запасы остаются в допустимых пределах, а рынок может переходить между избытком, нормой и дефицитом. За одинаковый промежуток игрового времени рынок приходит к одинаковому состоянию при обычном ходе, ускорении и разрешённом ручном продвижении. Пока торговое окно открыто, обычное течение экономики остановлено вместе с симуляцией».

Исходные completion evidence сохранены: наблюдаемое изменение запасов и состояния в Trade, повторная покупка после пополнения; одинаковое состояние за одинаковое игровое время всеми разрешёнными способами; отсутствие отрицательных/переполненных остатков и двойного выпуска при нехватке inputs и заполненном складе. Ограничения: часовой экономический шаг, конфигурируемые rates/targets/caps, один источник выпуска, Fuel отдельно от cargo flows, без полной ECS-фабрики и ускорения физического движения.

## User story

Игрок закрывает Trade, даёт пройти игровому времени и при повторном открытии видит произведённые или израсходованные товары. Экран явно показывает дефицит, норму или избыток, а пополнение позволяет повторить покупку. При полном складе и нехватке сырья экономика сохраняет корректное состояние, без потери уже произведённого и двойного начисления. Одинаковое игровое время даёт одинаковый рынок при обычном ходе, ускорении и существующем перемещении между районами станции.

## Acceptance criteria

- AC-01: optional economy-конфигурация профиля задаёт часовые output/input rates, targetStock, пороги состояния и восстановление бюджета; ссылки и значения валидируются до запуска. Отсутствие economy сохраняет US-0001 bootstrap-only behavior.
- AC-02: на границах целых GameCalendar.HourMs профиль производит/потребляет по одному разу; shortage inputs не создаёт выпуск, Transit потребляет доступное без отрицательных остатков. Известные rates пяти ролей берутся из эпика.
- AC-03: каждый cargo stock экономики остаётся в [0, 2×targetStock] при production и Sell. Completed recipe output сверх вместимости сохраняется в PendingOutput и выгружается однократно после освобождения места. Profile и recipe production никогда не дублируют один выпуск/расход.
- AC-04: одинаковый seed, набор команд и конечный GameTimeMs дают одинаковые stock/budget/pending при разных допустимых разбиениях интервала; повторный snapshot на границе не повторяет эффекты. Speed0 не продвигает рынок от real time, ручной TravelStation остаётся явным разрешённым исключением.
- AC-05: доступный торговый бюджет ограничен maxBudget=2×scaled profile InitialCredits и восстанавливается не более floor(maxBudget/24) за игровые сутки. Sell ограничен бюджетом и свободным складом; executed quantity/остаток партии наблюдаемы. Credits станции не раскрывается UI.
- AC-06: существующий Trade показывает authoritative stock, target, max и Shortage/Normal/Surplus, различает складской и денежный лимиты; новая поставка после закрытия/ожидания/открытия доступна для покупки. Нет клиентской симуляции или нового экрана.
- AC-07: new save сохраняет торговый бюджет и PendingOutput; после load граница не применяется повторно, ни одна произведённая единица не пропадает. Изменение economy fingerprint отклоняет несовместимое сохранение; статические legacy сценарии продолжают работать.

## Non-goals

Динамические цены, QuoteId/MarketRevision и price curve (US-0015/0003); генерация/перелёты/Undock; fuel replenishment и потребление топлива; события, удалённое знание, ledger; полноценная ECS и общий pending-output framework для корабельных фабрик. Не меняются календарный множитель, Motion, команды двигателя, портовые ставки и правила модальных окон. Полные миграции/вся persistence экономики — US-0012, баланс десятидневных прогонов — US-0013.

## Dependencies

Обязательная implementation prerequisite — EP-0001-US-0001-station-market-profiles и её TK-0001…0005. На начало grounding она подготовлена как planning, не реализована: файлов StationMarketProfileDefinition.cs и Data/Markets/station-market-profiles.json в текущем checkout нет; подтверждён поиск src/DeepSpaceSaga.Engine по *Market* и базовый TypeDataPaths в EngineContentLoader.cs:540–549.

US-0002 описывает следующий слой поверх точного планового API US-0001, не выдаёт его за существующий код. Перед реализацией проверить, что prerequisite merged и signatures совпадают. Файлы US-0001, epic Documentation.md и requirements в этой задаче не изменяются.

При финальной проверке появились параллельные незавершённые изменения US-0001: StationMarketProfileDefinition.cs:8–18 и registry.StationMarketProfiles в GameDataRegistry.cs:51 совпадают с запланированными dependency signatures; LoadStationMarketProfiles находится в EngineContentLoader.cs:127. Shipping profiles JSON ещё отсутствует. Эти production-изменения сделаны вне данной planning-задачи и не редактировались здесь. Полное завершение US-0001 по-прежнему является prerequisite; ранние path:line ниже относятся к снимку начала grounding.

## Grounding

| Проверенный факт / ограничение | Evidence |
|---|---|
| Settings validation до запуска, strict JSON | Documentation/01-Requirements/EngineRequirements.md:224–261; src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:10–13 |
| Модальные окна держат Speed0 до закрытия последнего | Documentation/01-Requirements/EngineRequirements.md:1121–1133 |
| Имеющийся календарный проход обрабатывает production/meal/fee/deadline в (previous,target] | src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:12–51 |
| Recipe inputs списываются при старте, output выдаётся при deadline; текущая выдача не ограничена stock cap | src/DeepSpaceSaga.Engine/SimulationEngine.ProductionTime.cs:13–62 |
| Невместившийся произведённый output должен сохраняться, новый цикл ждёт выгрузки | Documentation/01-Requirements/EngineRequirements.md:3844–3868 |
| Calendar и motion имеют разные множители | src/DeepSpaceSaga.Engine/SimulationClock.cs:51–60 |
| TravelStation требует docked pause, прибавляет один игровой час, receipt idempotent | src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs:31–54 |
| Тесты могут инъецировать clock и сравнивать интервалы без реальных sleep | tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs:13–43; SimulationEngine.cs:123, 1580 |
| Save/load устанавливает календарный cursor на сохранённое время | src/DeepSpaceSaga.Engine/SimulationEngine.cs:318–339 |
| Snapshot сейчас содержит StockQuantity/MaxSellableQuantity, без market band/target | src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:23–40 |
| TradeModel считает station-limit денежным, Engine Sell ограничивает budget | src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs:26–31; SimulationEngine.cs:1988–1998 |
| Credits увеличивают Buy/Refuel, портовые сборы и docking dialogue | SimulationEngine.cs:1957,2040; SimulationEngine.PortFees.cs:42–45; Dialogue/DialogueEffectTransaction.cs:53 (в src/DeepSpaceSaga.Engine) |
| US-0001 планирует immutable profiles, profile fingerprint и save v8 | ../EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0001-market-profile-schema/EP-0001-US-0001-TK-0001-market-profile-schema.md:47–59; TK-0002-profile-market-bootstrap, Public API after the change |

Тесты/игра в grounding не запускались. Состояние production-кода не менялось.

## Invariants

- Engine — единственный владелец рынка; Client использует DTO (Documentation/00-Process/CLAUDE.md:22–56).
- GameTimeMs управляет экономикой; SimulationTimeMs/MotionTimeMs остаются физическим временем (SimulationClock.cs:51–60).
- Inputs списываются один раз; завершённый recipe output не уничтожается при заполнении склада (EngineRequirements.md:3844–3868).
- Деньги/коэффициенты — целые/fixed-point/decimal, rounding AwayFromZero (EngineRequirements.md:5263–5265).
- Explicit inventory не перезаписывается defaults (EngineRequirements.md:5178, 5315–5317). Для opted-in economy явное значение за лимитом отклоняется до старта, а не обрезается.
- FuelTank не участвует в cargo hourly flow и обратно не продаётся (EngineRequirements.md:5128).
- Нет изменения старой формулы цены и module-driven consumed-resource factors (EngineRequirements.md:5227,5243–5265).
- Ошибка загрузки не заменяет действующий мир (SimulationEngine.cs:230–232).

## Assumptions

- A-01: следующая история — US-0002 по установленному порядку.
- A-02: economy — optional opt-in расширение профиля, старый профиль без поля остаётся статическим. schemaVersion профилей остаётся 1, fingerprint добавляет непустой economy payload; для economy=null старый fingerprint byte-for-byte сохраняется.
- A-03: productionSource = Profile или Modules на станцию. Profile выполняет один общий часовой batch из hourlyInputs/hourlyOutputs; весь batch требует всех inputs и вместимости всех outputs. Modules использует только существующие recipes, hourlyInputs/Outputs пусты. Независимое consumption для Transit/Modules bounded min(stock,rate); совпадение consumption с recipe inputs отвергается при load.
- A-04: Profile несовместим с активными/незавершёнными producingModules. Modules не начисляет профильный выпуск. Это явная диагностика неверной конфигурации, не молчаливое отключение существующего производства.
- A-05: rates в таблице эпика — на станцию за час и здесь не масштабируются размером; размер масштабирует targets и initial/max budget. target округляется AwayFromZero, maxStock=2×уже округлённый target.
- A-06: default Shortage строго ниже 0.5×target, Surplus строго выше 1.5×target; равные границы Normal. Пороги 500/1500 permille находятся в JSON.
- A-07: торговый budget — доступная для закупок часть существующего Credits. Отдельный MarketBudgetCredits ограничен cap; Credits сохраняет полную выручку/сборы без списания избытка. Sell списывает оба; Buy/Refuel увеличивают Credits полностью, budget до cap; сборы продолжают пополнять Credits как прежде. Часовое восстановление сначала делает доступным имеющийся резерв Credits, недостающую часть покрывает предусмотренное бюджетное пополнение. Точная формула в TK-0003.
- A-08: дневной refill=floor(maxBudget/24), распределён по 24 часовым границам без дробных credits/накопления пропущенного refill при cap. Не путать «/24 за день» с «/24 каждый час».
- A-09: рынок обновляется после completed recipes, до meal/fee/deadline; pending разгружается перед стартом следующего recipe и после почасового изменения. Общий cursor делает одинаковые timestamps idempotent.
- A-10: лишь минимальная persistence новых полей и save v9 входит здесь. Несовместимая новая экономика старого profile save не мигрирует автоматически.

## Approved ticket map

План принят автоматическим workflow без запроса отдельного approval. Все тикеты planning-only.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0002-TK-0001-market-stock-snapshot | Состояние и вместимость рынка в snapshot | contracts | US-0001 | AC-03, AC-05, AC-06 |
| EP-0001-US-0002-TK-0002-market-economy-schema | Валидируемая конфигурация потоков и сохранения | engine | US-0001 | AC-01, AC-03, AC-05, AC-07 |
| EP-0001-US-0002-TK-0003-hourly-market-simulation | Почасовая экономика и ограниченный склад | engine | TK-0001, TK-0002 | AC-02, AC-03, AC-04, AC-05, AC-07 |
| EP-0001-US-0002-TK-0004-market-flow-content | Потоки пяти рынков и локализация состояний | content-data | TK-0003 | AC-01, AC-02, AC-05, AC-06 |
| EP-0001-US-0002-TK-0005-market-state-trade-ui | Обновление и состояния рынка в Trade | client | TK-0001, TK-0003, TK-0004 | AC-03, AC-04, AC-05, AC-06 |

Порядок: US-0001 → (TK-0001, TK-0002) → TK-0003 → TK-0004 → TK-0005; допустим последовательный TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. Implementation files: 2 / 5 / 5 / 4 / 3. Matching test project указан в каждом тикете.

## Gaps and backlog

- G-01: US-0001 ещё не реализована; это известная prerequisite, не причина угадывать текущие API или выполнять её в planning-задаче.
- G-02: численные rates/thresholds/targets — tuning до US-0013. Silicon не имеет производителя в baseline, Transit не имеет replenishment товаров. Конечный stock и последующий дефицит здесь ожидаемы; устойчивость всей экономики не заявляется.
- G-03: полные migrations, profile evolution, ledger/graph/events — US-0012; повреждённый/несовместимый save отвергается без изменения мира.
- G-04: общий ECS factory state machine не реализуется; bounded station recipes сохраняют PendingOutput в уже существующей модели.
- G-05: дано явное бюджетное assumption A-07, чтобы maxBudget не уничтожало собранные деньги и не блокировало docking/port fees.
- G-06: requirements-engineer отсутствует в доступных/ранее проверенных локальных skills; применяется явно предоставленный StoryBuilder.
- G-07: полная совместимость прогнозируемых prerequisite API проверяется при реализации после US-0001; номера текущего кода относятся к grounding 2026-09-21.
- Блокирующих вопросов нет. Build/runtime/tests не выполнялись; точные команды в тикетах.

## Decision and review log

| UTC | Event / exact user message | Result |
|---|---|---|
| 2026-09-21T08:59:06Z | «сделай следующую стори» | D-01: продолжить planning следующей истории; выбрана US-0002 по Documentation.md:158. |
| 2026-09-21T08:59:06Z | Проверены обязательные источники, story/epic IDs, текущая экономика и planning US-0001. Story-файл существует, ticket-папок нет. | Grounding завершён; первый свободный номер 0001. |
| 2026-09-21T08:59:06Z | A-01…A-10 записаны, пять тикетов приняты автоматическим workflow. | plan-review → создание, без новых требований к пользователю. |

| 2026-09-21T09:00:04.5761941Z | Создан TK-0001, contracts, 2 файла. | approved planning. |
| 2026-09-21T09:01:41.0804927Z | Создан TK-0002, engine, 5 файлов. | approved planning. |
| 2026-09-21T09:04:04.7560971Z | Создан TK-0003, engine, 5 файлов. | approved planning; clock/pending/budget invariants и tests записаны. |
| 2026-09-21T09:06:14.6423220Z | Создан TK-0004, content-data, 4 файла. | approved planning; baseline пяти ролей и locale keys зафиксированы. |
| 2026-09-21T09:07:45.1289144Z | Создан TK-0005, client, 3 файла. | approved planning; все тикеты готовы к структурной проверке. |
| 2026-09-21T09:09:40.6371433Z | Проверены 5 ticket folders/files, metadata, 11 обязательных разделов, matching test projects, layer scope, лимиты 2/5/5/4/3, покрытие AC-01…AC-07 и зависимости на существующие planning artifacts без циклов. | complete; код/тесты/build не выполнялись. |

| 2026-09-21T09:10:32.3877383Z | Обнаружены параллельные изменения US-0001; новые profile signatures проверены и соответствуют плану. | Чужие production-изменения не затронуты; prerequisite остаётся обязательной. |
