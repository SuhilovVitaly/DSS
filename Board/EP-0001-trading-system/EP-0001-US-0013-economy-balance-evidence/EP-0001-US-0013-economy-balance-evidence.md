---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
title: Воспроизводимое подтверждение жизнеспособной торговли
stage: approved
dependencies: [EP-0001-US-0012-resume-trading-economy]
created: 2026-09-21T14:55:45Z
source_request: "сделай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0013-economy-balance-evidence\\EP-0001-US-0013-economy-balance-evidence.md"
current_review: complete
revision: 1
---

# Воспроизводимое подтверждение жизнеспособной торговли

## Входное техническое задание

Пользователь запросил тикеты для этой истории точным сообщением `сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0013-economy-balance-evidence\EP-0001-US-0013-economy-balance-evidence.md` в `2026-09-21T14:55:45Z`. История закрывает balance-evidence часть эпика: десятидневный прогон на 12 фиксированных seeds и двух конфигурациях корабля, проверку здоровья рынка, маржинальности стратегий, влияния событий, отсутствия доминирующего маршрута и совпадения повторного/Save/Load-прогона (`../Documentation.md:99–104,108–124,854–876`).

## User story

Как автор баланса, я хочу одной воспроизводимой headless-командой проверить торговую карту за десять игровых суток, чтобы выпускать сценарий только при наличии нескольких жизнеспособных стратегий. Проверка проходит 12 фиксированных seeds для стартовой и улучшенной конфигураций корабля. Отчёт показывает здоровье складов и маршрутов, authoritative состав рейсного ledger, маржу и все нарушения с достаточным контекстом для точного повтора. Повторный прогон и ветка с Save/Load дают канонически одинаковый результат.

## Acceptance criteria

- **AC-01 — фиксированная матрица и воспроизводимость.** Одна команда выполняет 24 комбинации: seeds `1,2,3,5,8,13,21,34,55,89,144,233` × `starter`/`cargo-upgrade`, горизонт ровно `10 * GameCalendar.DayMs`, почасовая выборка. `starter` читает фактические параметры стартового корабля; `cargo-upgrade` использует аналитический useful-cargo multiplier `2000 permille` при неизменных fuel efficiency, ценах, скорости и Approach. Два запуска, перестановка входов и эквивалентная Save/Load-ветка дают одинаковый canonical JSON без wall-clock полей.
- **AC-02 — жизнеспособный рынок.** Для каждой комбинации доступны ровно пять торговых станций и связный проходимый граф; у каждой станции на горизонте есть хотя бы одна доступная покупка и продажа. Для каждого обязательного товара доля почасовых samples с `stock == 0` вне активного влияющего события не превышает 25%; stock/budget не выходят за authoritative bounds, ни один обязательный supply flow не исчезает необратимо.
- **AC-03 — диапазоны маршрутов.** На фактически исполняемых стратегиях `netMarginPermille = round(NetProfitCredits * 1000 / CostOfGoodsSoldCredits)` с `AwayFromZero`: безопасный Short попадает в `50..150`, Medium в `150..350`; Long не обязан быть положительным для `starter`, но минимум один Long положителен для `cargo-upgrade`. В расчёте используются authoritative `VoyageFinanceSnapshot` components, а не разница общего Credits или повторение формул Engine в tool.
- **AC-04 — разнообразие и защита от exploit.** В каждом проверяемом состоянии максимальная сопоставимая положительная маржа не превышает `2 ×` медианы; ни один route/item pair не является лучшим во всех normal/event states. Хотя бы одно событие или risk state меняет первое место, не разрывая сеть. Три одинаковых круговых рейса с повторным использованием старой quote/команды не создают прибыль без новых authoritative market changes и не дают двойной posting.
- **AC-05 — диагностируемый результат.** JSON содержит schema/version, matrix parameters, seed, ship configuration, route/item, sampled station/stock/event evidence и ledger breakdown (`GrossSales`, `COGS`, `RouteFuelCost`, `PortFees`, `EventCosts`, passenger result, `NetProfit`). Каждое нарушение имеет stable code, ожидаемое правило и observed values; CLI возвращает `0` только при полном pass и ненулевой код при violation/configuration error.

## Non-goals

- Не добавлять игровой экран, Client-side экономический расчёт, CI pipeline или committed generated report.
- Не менять Approach, физическую скорость/траекторию, production ship modules или содержимое save ради теста.
- Не подбирать автоматически rates/role factors и не объявлять tuning успешным без полного отчёта; найденные коэффициенты меняются отдельным content-data тикетом после evidence.
- Не заменять Engine quote/trade/voyage/ledger правила собственной формулой tool и не измерять производительность.

## Dependencies

- `EP-0001-US-0012-resume-trading-economy` — прямой prerequisite: экономика, карта, events, voyage fuel, ledger и deterministic continuation должны быть реализованы и сохранены.
- Consumer contracts уже зафиксированы upstream planning: `PlayerCommand(..., "navigation.undock", TargetObjectId: ...)`, `SimulationEngine.GetTradeQuote(TradeQuoteRequest)`, `trade.buy`/`trade.sell`, `AuthoritativeSnapshot.ActiveVoyage` (`../EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-repeatable-trading-voyage.md:50–56`) и `VoyageFinanceSnapshot` (`../EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0001-voyage-finance-contract/EP-0001-US-0011-TK-0001-voyage-finance-contract.md:37–78`). Если реализованные signatures расходятся, соответствующий US-0013 тикет возвращается в review, scope молча не расширяется.

## Grounding

- Epic задаёт именно 10 суток, 12 seeds, две конфигурации, margin bands, предел `2×`, максимум 25% zero-stock и тройной round-trip guard: `../Documentation.md:99–104`.
- Rates и role factors остаются tuning parameters до evidence US-0013: `../Documentation.md:108–124`.
- Engine уже имеет public bootstrap из settings/scenario/save: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:98–120`; full save capture: `:720–725`.
- Детерминированный test seam принимает явный `gameTimeMs` и прогоняет общий `BuildSnapshot/AdvanceWorldTo`: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:393–421,1676–1682`; explicit-time save seam находится в `:728–734`.
- `ScenarioLoader` предоставляет public load/serialize для round-trip сравнения: `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:42–60,106–109`.
- Engine уже разрешает internal access для in-repo diagnostic tool `DeepSpaceSaga.Performance`, но не для balance tool: `src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj:14–18`.
- Solution содержит только production и четыре test projects; tooling projects в неё сейчас не включены: `DeepSpaceSaga.sln:6–28`.
- Upstream stories являются planning artifacts и ещё не полностью представлены production code; это execution-order gap, а не открытое продуктовое решение.

## Assumptions

- **A-01:** Hourly samples включают `t=0` и целые часы `1..240`; zero-stock ratio считается по 240 post-boundary samples, чтобы начальный bootstrap не разбавлял дефицит.
- **A-02:** Active influencing event исключает sample из zero-stock denominator только для item/category, на которые event реально влияет; unrelated event не скрывает дефицит.
- **A-03:** `cargo-upgrade` — аналитический профиль `cargoCapacityMultiplierPermille=2000`, `fuelEfficiencyMultiplierPermille=1000`. Он ограничивает размер стратегии, но не мутирует scenario/module content и не обещает доступную игроку upgrade-механику.
- **A-04:** Median считается по положительным сравнимым route/item margins одного state/config после ordinal canonical ordering; при менее чем двух кандидатах результат `insufficient_strategy_diversity`, а не автоматический pass.
- **A-05:** Базовый passive run не совершает сделок. Strategy runs используют независимые клоны того же seed/checkpoint, реальные quote/command/voyage/ledger paths и не влияют друг на друга.
- **A-06:** Полный 24-case report не является обычным unit test и не должен зависеть от wall clock; unit/integration tests используют сокращённую matrix, а acceptance command запускает весь corpus.

## Invariants

- Calendar/economic time отделено от physical motion; diagnostic stepping не меняет Approach/speed (`EngineRequirements.md:5335–5343`; `SimulationEngine.cs:393–421`).
- Authoritative market/route/ledger остаются в Engine; tool только оркестрирует и оценивает опубликованные/сохранённые результаты (`CLAUDE.md:88–112`; `../Documentation.md:101–104`).
- Денежные значения целочисленные; permille rounding — `MidpointRounding.AwayFromZero` (`EngineRequirements.md:5247–5265`).
- Generated evidence сортируется по seed, config, game time, station, route и item ordinal; timestamp, temp path и machine-specific metadata не входят в canonical payload.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0013-TK-0001-balance-diagnostic-seam | Детерминированный Engine seam для balance tooling | engine | US-0012/TK-0004 | AC-01, AC-05 |
| EP-0001-US-0013-TK-0002-balance-run-matrix | Headless runner фиксированной balance-матрицы | tooling | TK-0001 | AC-01, AC-05 |
| EP-0001-US-0013-TK-0003-market-health-evaluation | Проверка доступности и здоровья складов | tooling | TK-0002 | AC-02, AC-05 |
| EP-0001-US-0013-TK-0004-strategy-balance-evaluation | Проверка маржи, разнообразия и повторных рейсов | tooling | TK-0002; US-0011/TK-0003 | AC-03, AC-04, AC-05 |
| EP-0001-US-0013-TK-0005-balance-report-cli | Канонический отчёт и команда release-gate | tooling | TK-0003, TK-0004 | AC-01, AC-02, AC-03, AC-04, AC-05 |

Dependency order: `TK-0001 → TK-0002 → (TK-0003, TK-0004) → TK-0005`.

## Decisions and review log

- `2026-09-21T14:55:45Z` — получено единственное сообщение пользователя: `сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0013-economy-balance-evidence\EP-0001-US-0013-economy-balance-evidence.md`.
- Блокирующих продуктовых вопросов нет; plan и ticket map приняты автоматическим workflow. Технические детали, не заданные входом, записаны как A-01…A-06.
- Созданы TK-0001…TK-0005 в dependency order; вопросов и дополнений пользователя не было.

## Gaps and backlog

- **G-01:** Direct dependency US-0012 имеет готовые planning artifacts, но её production end state и часть транзитивных stories на текущем checkout ещё не реализованы. Каждый US-0013 ticket исполняется только после своих named prerequisites; signature mismatch возвращает ticket в review.
- **G-02:** Фактический полный отчёт ещё не существует — он является результатом выполнения TK-0005. До него нельзя утверждать, что текущие rates/role factors проходят thresholds.
- **G-03:** Если full report выявит tuning violations, менять только подтверждённые content-data coefficients отдельным тикетом/историей с приложенным before/after evidence; automatic optimizer не входит.
- **G-04:** Доступность реального cargo upgrade игроку не входит; аналитический `cargo-upgrade` нужен только для второй balance-конфигурации.
- Нерешённых блокирующих вопросов нет.
