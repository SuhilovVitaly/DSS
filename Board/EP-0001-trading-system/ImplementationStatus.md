# EP-0001 — состояние реализации торговой системы

## Сверка реализации — 2026-10-10

Текущая сверка: [ReviewStatus.md](ReviewStatus.md). Этот ранее созданный файл оставлен как исторический аудит 2026-10-05; его NOT_IMPLEMENTED/PARTIAL утверждения о событиях, ledger, continuation, market knowledge и tooling устарели и НЕ являются текущим остатком. Новые F/G и свежие проверки находятся в review 2026-10-10; прежние зелёные результаты не закрывают новые дефекты.

[Текущее устройство](../../Documentation/04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../../Documentation/04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Дата аудита: 2026-10-05. Статус эпика: **НЕ ЗАВЕРШЁН**.

Документ фиксирует результаты проверки Board, текущего кода и тестов для дальнейшего исполнения существующих тикетов. Проверены 16 user story и 74 canonical ticket-файла. Это реестр состояния, а не новая спецификация и не разрешение на изменение production-кода, расширение allowlist или commit/push.

## Основание и границы достоверности

- Требования: [эпик](Documentation.md), story-файлы и canonical ticket-файлы по ссылкам ниже; сохранённые согласованные отклонения учитываются отдельно.
- Аудит относится к рабочему дереву, а не только к коммиту. HEAD при подготовке документа: `902c571dd9fe93eb2ea2ff3491478560f3dd3ec9`.
- Во время проверки параллельно менялась реализация генерации солнечной системы. Перед началом конкретного тикета повторно проверить `git status`, актуальные signatures и зависимости. Не включать чужие изменения в свой scope.
- `stage: approved` / `current_review: complete` в Board означают завершение planning. Это прямо записано в US-0001, строка 99. Корневой Documentation.md всё ещё содержит исторические draft-статусы и старый grounding; они не являются текущей картой реализации.
- Наличие файла или совпадение имени теста само по себе не доказывает выполнение AC. При аудите сопоставлялись контракты, основные runtime/UI пути и проверки; это не исчерпывающее ревью каждой ветви всех 74 тикетов.
- Индекса `graphify-out/graph.json` при аудите не было; выводы основаны на исходниках и тестах напрямую.
- Ручная/native UI-приёмка в этом аудите **NOT RUN**. Отсутствие подтверждения не означает, что такую проверку никогда не проводили; перед закрытием требуется найти существующее evidence либо выполнить её.

## Обозначения

| Статус | Значение |
|---|---|
| CODE_PRESENT | Реализация и автоматические проверки найдены; новых конкретных пробелов в этом аудите не установлено. Не равнозначно полному acceptance/approval. |
| NOT_IMPLEMENTED | Требуемая функциональность тикета не найдена в актуальных контрактах, runtime, контенте или UI. |
| PARTIAL | Есть часть необходимой основы, но существенная часть контракта не реализована. |
| VALIDATION_GAP | Production-основа есть, но предусмотренное подтверждение контракта/пограничных случаев неполно. |
| ACCEPTANCE_PENDING | Код и автоматические проверки есть; обязательная ручная приёмка не подтверждена. |
| REQUIREMENTS_RECONCILIATION | Код соответствует сохранённому согласованному исключению, которое расходится с основным тикетом. Нужна сверка действующего требования, а не автоматическое исправление. |

Все статусы, кроме CODE_PRESENT, входят в открытый остаток работ. Для CODE_PRESENT также остаются обычные требования итоговой приёмки эпика.

## Найденные проблемы и препятствия

### P1 / GAP-01 — отсутствуют события и динамические риски маршрутов

US-0007-TK-0001…0005 и US-0008-TK-0001…0005 не закрыты. Старые station-authored price events и статические risk metadata торговой карты не реализуют каталог восьми событий, их почасовой lifecycle, динамические ограничения маршрутов и проверку сохранения альтернативного пути.

Evidence: [StationTradeSnapshot.cs](../../src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs), record со строки 10, содержит Items и MarketRevision, но не ActiveEvents; [AuthoritativeSnapshot.cs](../../src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs), со строки 11, не содержит TradingRoutes. Не найдены StationMarketEventDefinition.cs, SimulationEngine.MarketEvents.cs и TradingRouteEvaluator.cs, а также соответствующие shipping-каталоги/тесты из тикетов.

Закрытие: выполнить оба набора тикетов по их зависимостям; проверить activation/expiry, save/load, ограничения и сохранение проходимости графа на реальном контенте.

### P1 / GAP-02 — нет полной стоимости и финансового результата рейса

US-0009-TK-0001…0004, US-0010-TK-0001…0005 и US-0011-TK-0001…0005 не закрыты. Наличие nullable ReservedFuelKg не означает, что топливо резервируется или списывается. Расходы рейса, cargo cost basis и итоговая прибыль отсутствуют.

Evidence: [VoyageSnapshot.cs](../../src/DeepSpaceSaga.Contracts/VoyageSnapshot.cs) содержит только необязательный ReservedFuelKg без settlement-контракта; [CargoStackData](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs), строка 477 при проверке, содержит ItemTypeId и Quantity; [TradeExecutionReceipt](../../src/DeepSpaceSaga.Contracts/CommandResult.cs) не содержит RealizedCargoCostCredits/GrossResultCredits. [FinanceScreen.cs](../../src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs), PlaceholderLines со строки 90, выводит заглушки вместо финансов рейса.

Закрытие: реализовать reserve/settle/refund, сохраняемую себестоимость, корректное списание при partial Sell, ledger и authoritative UI. Не использовать разницу PlayerCredits как замену чистой прибыли.

### P1 / GAP-03 — полное восстановление экономики и доказательство баланса отсутствуют

US-0012-TK-0001…0005 и US-0013-TK-0001…0005 не закрыты. Существующие сохранение рынков, receipts и voyage lifecycle дают полезную основу, но не могут сохранять отсутствующие events/fuel/ledger/knowledge. Общие файловые SaveAsync-тесты не подтверждают complete economy continuation.

Evidence: [ScenarioData.cs](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs) не содержит TradingEconomyContinuationData; не найдены предусмотренные MarketContinuation/VoyageContinuation/TradingContinuation partial-файлы и специализированные continuity-тесты. Проект `tools/DeepSpaceSaga.EconomyBalance` и matching test project отсутствуют.

Закрытие: после prerequisites проверить реальные JSON/file round-trip и продолжение на границах времени/сделок/событий/рейсов, затем выполнить 24 комбинации seeds × ship configurations за 10 игровых суток с каноническим отчётом и проверкой повторяемости.

### P1 / GAP-04 — нет знания удалённых рынков

US-0016-TK-0001…0003 не реализованы. Нет DTO/хранилища observations, observed timestamp/revision, stale projection, persistence и market knowledge в Object Info.

Evidence: [AuthoritativeSnapshot.cs](../../src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs) не содержит StationMarketKnowledge; не найдены StationMarketKnowledgeSnapshot.cs и SimulationEngine.MarketKnowledge.cs. [GameSessionScreen.cs](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs), ToObjectInfoPanelData, не передаёт такой DTO в панель.

Закрытие: реализовать три тикета, сохраняя coarse-only remote данные и локальную доступность точной котировки только после стыковки.

### P2 / GAP-05 — US-0014 реализована частично

- TK-0001: VoyageSnapshot, phases и reason codes есть, но предусмотренный VoyageSnapshotTests.cs отсутствует. Проверить эквивалентное покрытие и добавить недостающие contract/JSON/default tests.
- TK-0002: Engine lifecycle, progress, docking reconciliation и persistence есть. Существуют VoyageLifecycleTests, VoyagePersistenceTests и сквозные RepeatableTradingVoyageTests; однако полная матрица acceptance из тикета не подтверждена. Нельзя считать отсутствие точных имён тестов самостоятельным дефектом: сначала сопоставить фактическое покрытие, затем закрыть оставшиеся случаи.
- TK-0003: выбор назначения и отправка TargetObjectId есть. Не показан ETA, причины блокировки выводятся кодами, не реализован выбор первого доступного назначения и fallback выбора по новым options согласно тикету; blocked option нельзя выбрать для просмотра причины.
- TK-0004: GameSession не показывает Voyage/phase, Destination, Progress и понятный Departure blocker.

Evidence: [StationScreen.cs](../../src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs), SelectedDestinationId и render блока DESTINATION, строки 55 и 321; [GameSessionScreen.cs](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs), BuildPanelLines, строка 2154; [VoyageLifecycleTests.cs](../../tests/DeepSpaceSaga.Engine.Tests/VoyageLifecycleTests.cs); [VoyagePersistenceTests.cs](../../tests/DeepSpaceSaga.Engine.Tests/VoyagePersistenceTests.cs).

Закрытие: закончить оставшийся контракт/UI и приёмку, переиспользуя существующий Engine lifecycle и физический proof US-0006.

### P2 / GAP-06 — основной US-0003-TK-0002 расходится с согласованным исключением

[Основной тикет](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0002-atomic-quote-execution/EP-0001-US-0003-TK-0002-atomic-quote-execution.md), строки 30 и 58, требует QuoteRequired для profile-market команд без binding. [Сохранённое решение от 2026-09-22](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0002-atomic-quote-execution/story-20260922-125305.md), строки 3–9, явно разрешает временный unquoted legacy path.

Текущий TryStartTradeCommand в [SimulationEngine.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) отправляет команду без binding в legacy path. Это закрепляет [QuotedTradeExecutionTests.cs](../../tests/DeepSpaceSaga.Engine.Tests/QuotedTradeExecutionTests.cs), тест Unquoted_profile_trade_keeps_legacy_path_until_quote_ui, строка 1040.

Это документированное отклонение, а не подтверждённая случайная регрессия. После реализации US-0015/UI нужно определить действующий end state: сохранить исключение с актуализацией требований либо отдельно реализовать исходный QuoteRequired gate с переходом зависимых callers/tests. Этот отчёт не отменяет ранее согласованное решение.

### P2 / GAP-07 — ручная приёмка не подтверждена

Особенно явно это блокирует US-0006-TK-0003: [Definition of Done](EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0003-station-trade-navigation/EP-0001-US-0006-TK-0003-station-trade-navigation.md), строка 101, оставляет пункт открытым без smoke двух физических кругов A→B→A через существующие окна, docking dialogue и nested pause.

Также ручные проверки прямо предусмотрены US-0001-TK-0004/0005, US-0002-TK-0005 и US-0005-TK-0005. В реестре они отмечены ACCEPTANCE_PENDING. У новых UI-тикетов ручная проверка остаётся частью их будущего DoD.

### P2 / PLAN-01 — перед реализацией требуется адаптировать устаревший grounding

- Ряд тикетов US-0008…0012 ссылается на ActiveVoyageData/ActiveVoyageSnapshot и SimulationEngine.Voyages.cs. Фактически используются VoyageStateData/VoyageSnapshot и SimulationEngine.Voyage.cs. Нельзя создавать параллельную модель только ради старого имени в плане.
- На момент подготовки отчёта CurrentSaveFormatVersion уже равен 11. В старых тикетах встречаются 9/10; не понижать версию и не выбирать новую без проверки merged prerequisite schema и compatibility policy.
- US-0004-TK-0003 имеет stage: draft при существующей реализации; корневой эпик содержит старые статусы. Это несогласованность учёта/approval, не доказательство отсутствия кода.
- У US-0007 и US-0008 в планах различаются имена event/map content paths. Перед интеграцией сверить единый источник данных, не создавать дублирующие каталоги.

Этот отчёт не изменяет canonical ticket scope, версии и approval-статусы. При актуализации плана нужно сохранить согласованные семантику, зависимости и границы файлов.

## Проверки, выполненные при исходном аудите

Команда из D:/DeepSpaceSaga/DSS:

```text
dotnet test DeepSpaceSaga.sln --no-restore --verbosity quiet
```

| Проект | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Contracts.Tests | 105 | 0 | 0 |
| Motion.Tests | 137 | 0 | 0 |
| Engine.Tests | 1155 | 0 | 0 |
| Client.Tests | 1568 | 0 | 0 |
| Всего | 2965 | 0 | 0 |

`git diff --check` завершился успешно; были предупреждения LF/CRLF. Native UI smoke и балансный 10-дневный прогон не выполнялись. Полный formatter не запускался. Эти результаты получены в предыдущем шаге аудита 2026-10-05, а не повторным запуском после создания данного Markdown. Зелёные тесты не являются доказательством выполнения отсутствующих stories.

## Рекомендуемый порядок дальнейшей работы

1. Сверить PLAN-01 и GAP-06 с действующими контрактами/согласованными решениями; определить точный scope следующего тикета.
2. Закрыть оставшиеся TK-0001…0004 US-0014, затем приёмку US-0006-TK-0003. Остальные открытые smoke базовых рынков/полей выполнить как отдельные проверки.
3. US-0007 → US-0008 → US-0009. Внутри stories соблюдать depends_on каждого тикета.
4. US-0010 → US-0011, учитывая fuel/lifecycle dependencies и реальные receipts. Независимая от событий часть US-0010 может выполняться раньше после проверки её prerequisites.
5. US-0016 после готовности US-0004/US-0015; завершить до итоговой US-0012-проверки, поскольку её полный save acceptance включает market knowledge.
6. US-0012 после готовности всех сохраняемых подсистем → US-0013 с полным acceptance corpus.
7. Итоговый сквозной audit AC/DoD, native UI evidence, фиксация фактического статуса Board.

Это порядок по смысловым зависимостям, не замена metadata depends_on. Каждый implementer начинает с Epic → Story → Ticket, сверяет разрешённые файлы и фактическую готовность prerequisites, затем реализует/проверяет один согласованный scope.

## Полный реестр тикетов

Ссылки ведут на исходные задания. Колонка «Остаток / вывод» описывает, что нужно сделать или проверить, а не повторяет всю спецификацию. Все незавершённые тикеты перечислены индивидуально; CODE_PRESENT оставлены для полноты инвентаризации.

### EP-0001-US-0001-station-market-profiles — Пять различающихся локальных рынков

[User Story](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-station-market-profiles.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0001-TK-0001](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0001-market-profile-schema/EP-0001-US-0001-TK-0001-market-profile-schema.md) | Загрузка и валидация профилей рынка | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0001-TK-0002](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0002-profile-market-bootstrap/EP-0001-US-0001-TK-0002-profile-market-bootstrap.md) | Профильный рынок и безопасное восстановление | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0001-TK-0003](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0003-market-catalog-content/EP-0001-US-0001-TK-0003-market-catalog-content.md) | Базовый каталог и локализованные единицы | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0001-TK-0004](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0004-five-market-demo/EP-0001-US-0001-TK-0004-five-market-demo.md) | Пять профилей и демонстрационный сценарий | ACCEPTANCE_PENDING | Код demo есть. Подтвердить native Dock/dialogue → Station → Trade, Buy/Sell/Refuel и несколько профилей; GAP-07. |
| [EP-0001-US-0001-TK-0005](EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0005-profile-trade-presentation/EP-0001-US-0001-TK-0005-profile-trade-presentation.md) | Названия и единицы в существующем Trade | ACCEPTANCE_PENDING | Код presentation есть. Подтвердить пять рынков RU/EN, единицы electronics/rations/cells/Fuel и отсутствие наложений; GAP-07. |

### EP-0001-US-0002-market-replenishment — Восстановление и расходование запасов станций

[User Story](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-market-replenishment.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0002-TK-0001](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0001-market-stock-snapshot/EP-0001-US-0002-TK-0001-market-stock-snapshot.md) | Состояние и вместимость рынка в snapshot | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0002-TK-0002](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0002-market-economy-schema/EP-0001-US-0002-TK-0002-market-economy-schema.md) | Валидируемая конфигурация потоков и сохранения | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0002-TK-0003](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0003-hourly-market-simulation/EP-0001-US-0002-TK-0003-hourly-market-simulation.md) | Почасовая экономика и ограниченный склад | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0002-TK-0004](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0004-market-flow-content/EP-0001-US-0002-TK-0004-market-flow-content.md) | Потоки пяти рынков и локализация состояний | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0002-TK-0005](EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0005-market-state-trade-ui/EP-0001-US-0002-TK-0005-market-state-trade-ui.md) | Обновление и состояния рынка в Trade | ACCEPTANCE_PENDING | Код есть. Подтвердить паузу в Trade, закрытие всех modal, ожидание часа и свежий stock/state при повторном открытии в RU/EN; GAP-07. |

### EP-0001-US-0003-dynamic-market-trading — Атомарное исполнение сделки в существующем Trade

[User Story](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-dynamic-market-trading.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0003-TK-0001](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0001-trade-execution-contract/EP-0001-US-0003-TK-0001-trade-execution-contract.md) | Quote binding и результат сделки | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0003-TK-0002](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0002-atomic-quote-execution/EP-0001-US-0003-TK-0002-atomic-quote-execution.md) | Атомарное исполнение котировки | REQUIREMENTS_RECONCILIATION | Quoted execution есть. Разрешить противоречие QuoteRequired и согласованного unquoted legacy path; GAP-06. |
| [EP-0001-US-0003-TK-0003](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0003-trade-result-texts/EP-0001-US-0003-TK-0003-trade-result-texts.md) | Сообщения котировки и ограничений | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0003-TK-0004](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0004-quoted-trade-controls/EP-0001-US-0003-TK-0004-quoted-trade-controls.md) | Authoritative preview, Max и отправка | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0003-TK-0005](EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0005-confirmed-trade-history/EP-0001-US-0003-TK-0005-confirmed-trade-history.md) | Подтверждённые суммы и partial receipt | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |

### EP-0001-US-0004-seeded-trading-map — Воспроизводимая карта с выбором торгового направления

[User Story](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-seeded-trading-map.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0004-TK-0001](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0001-trading-map-schema/EP-0001-US-0004-TK-0001-trading-map-schema.md) | Схема запроса и сохранённой карты | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0004-TK-0002](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0002-economic-graph/EP-0001-US-0004-TK-0002-economic-graph.md) | Seeded граф и грузовые связи | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0004-TK-0003](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0003-map-geometry/EP-0001-US-0004-TK-0003-map-geometry.md) | Геометрия и классы расстояний | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0004-TK-0004](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0004-map-bootstrap-save/EP-0001-US-0004-TK-0004-map-bootstrap-save.md) | New Game и восстановление сети | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0004-TK-0005](EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0005-trading-scenario-content/EP-0001-US-0004-TK-0005-trading-scenario-content.md) | Три стартовых варианта одной сети | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |

### EP-0001-US-0005-station-resource-fields — Ресурсное окружение экономических районов

[User Story](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-station-resource-fields.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0005-TK-0001](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0001-resource-survey-contract/EP-0001-US-0005-TK-0001-resource-survey-contract.md) | Контракт разрешённого состава | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0005-TK-0002](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0002-seeded-resource-fields/EP-0001-US-0005-TK-0002-seeded-resource-fields.md) | Конфигурация, генерация и сохранение полей | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0005-TK-0003](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0003-resource-field-content/EP-0001-US-0005-TK-0003-resource-field-content.md) | Профили ресурсного окружения | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0005-TK-0004](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0004-structural-resource-survey/EP-0001-US-0005-TK-0004-structural-resource-survey.md) | Сканирование и сохранённые знания | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0005-TK-0005](EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0005-resource-info-panel/EP-0001-US-0005-TK-0005-resource-info-panel.md) | Состав в существующей панели объекта | ACCEPTANCE_PENDING | Код есть. Подтвердить generated asteroid → Unknown → Structural Scan → reveal → save/load и поведение на паузе; GAP-07. |

### EP-0001-US-0006-repeatable-trading-voyage — Повторяемый рейс между двумя рынками

[User Story](EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-repeatable-trading-voyage.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0006-TK-0001](EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0001-round-trip-engine-proof/EP-0001-US-0006-TK-0001-round-trip-engine-proof.md) | Сквозная проверка двух торговых кругов | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0006-TK-0002](EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0002-trade-visit-context/EP-0001-US-0006-TK-0002-trade-visit-context.md) | Котировка и выбор в контексте текущей стоянки | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0006-TK-0003](EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0003-station-trade-navigation/EP-0001-US-0006-TK-0003-station-trade-navigation.md) | Повторяемый переход Station → Trade → полёт | ACCEPTANCE_PENDING | Headless navigation и physical Engine proof есть. Подтвердить два native A→B→A круга, nested pause/history, отсутствие stale market/auto-open; GAP-07. |

### EP-0001-US-0007-temporary-market-events — Временные события меняют торговые возможности

[User Story](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-temporary-market-events.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0007-TK-0001](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0001-market-event-contract/EP-0001-US-0007-TK-0001-market-event-contract.md) | Authoritative проекция активных событий | NOT_IMPLEMENTED | Добавить ActiveEvents, StationMarketEventSnapshot и route-effect DTO с JSON/default/privacy tests; GAP-01. |
| [EP-0001-US-0007-TK-0002](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0002-market-event-catalog/EP-0001-US-0007-TK-0002-market-event-catalog.md) | Схема каталога и save-состояния событий | NOT_IMPLEMENTED | Добавить strict event catalog, восемь definition IDs, validation/fingerprint и persisted event schema; GAP-01, PLAN-01. |
| [EP-0001-US-0007-TK-0003](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0003-market-event-content/EP-0001-US-0007-TK-0003-market-event-content.md) | Восемь событий и локализованные объяснения | NOT_IMPLEMENTED | Добавить shipping-каталог восьми событий, Settings path, RU/EN keys и real-loader tests; GAP-01. |
| [EP-0001-US-0007-TK-0004](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0004-market-event-lifecycle/EP-0001-US-0007-TK-0004-market-event-lifecycle.md) | Seeded lifecycle и экономические эффекты | NOT_IMPLEMENTED | Реализовать seeded hourly activation/expiry, максимум два события, однократные deltas, flow/price effects, revision и save/load; GAP-01. |
| [EP-0001-US-0007-TK-0005](EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0005-trade-event-presentation/EP-0001-US-0007-TK-0005-trade-event-presentation.md) | Причина и длительность события в Trade | NOT_IMPLEMENTED | Добавить badge/tooltip события, причины и authoritative remaining duration в Trade без клиентской симуляции; GAP-01. |

### EP-0001-US-0008-route-risk-and-alternatives — Рискованные маршруты и доступная альтернатива

[User Story](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-route-risk-and-alternatives.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0008-TK-0001](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0001-route-risk-contract/EP-0001-US-0008-TK-0001-route-risk-contract.md) | Контракт риска и доступности маршрута | NOT_IMPLEMENTED | Добавить TradingRouteSnapshot/TradingRoutes, base/effective terms, availability/risk/reasons и JSON tests; GAP-01. |
| [EP-0001-US-0008-TK-0002](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0002-effective-route-evaluator/EP-0001-US-0008-TK-0002-effective-route-evaluator.md) | Effective route и гарантия альтернативы | NOT_IMPLEMENTED | Реализовать effective-route evaluator, композицию modifiers и проверку связности/альтернатив для cargo flows; GAP-01. |
| [EP-0001-US-0008-TK-0003](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0003-route-event-voyage-integration/EP-0001-US-0008-TK-0003-route-event-voyage-integration.md) | События, выбор рейса и snapshot | NOT_IMPLEMENTED | Связать события с edge selection, departure validation, frozen voyage terms и локальной route projection; GAP-01, PLAN-01. |
| [EP-0001-US-0008-TK-0004](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0004-route-choice-presentation/EP-0001-US-0008-TK-0004-route-choice-presentation.md) | Риск и причины в Station screen | NOT_IMPLEMENTED | Показать risk, effective/base ETA и fuel multiplier, причины и доступность; связать выбор с US-0014; GAP-01. |
| [EP-0001-US-0008-TK-0005](EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0005-route-risk-content/EP-0001-US-0008-TK-0005-route-risk-content.md) | Контрольная карта и события маршрутов | NOT_IMPLEMENTED | Подготовить согласованный route-risk content и deterministic integration corpus с blockade/quarantine alternatives; GAP-01, PLAN-01. |

### EP-0001-US-0009-voyage-fuel-cost — Фактический расход топлива торгового рейса

[User Story](EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-voyage-fuel-cost.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0009-TK-0001](EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0001-voyage-fuel-contract/EP-0001-US-0009-TK-0001-voyage-fuel-contract.md) | Контракт reservation и settlement топлива | PARTIAL | Есть nullable ReservedFuelKg в VoyageSnapshot; нет полного projected fuel/cost и settlement-контракта/тестов. Адаптировать к существующему Voyage DTO; GAP-02. |
| [EP-0001-US-0009-TK-0002](EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0002-fuel-accounting-foundation/EP-0001-US-0009-TK-0002-fuel-accounting-foundation.md) | Эффективность и себестоимость топлива в баке | NOT_IMPLEMENTED | Добавить FuelEfficiencyKmPerKg, FuelCostBasisCredits, validation/bootstrap и учёт стоимости при Refuel; GAP-02. |
| [EP-0001-US-0009-TK-0003](EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0003-engine-efficiency-content/EP-0001-US-0009-TK-0003-engine-efficiency-content.md) | Baseline эффективности стартового двигателя | NOT_IMPLEMENTED | Добавить baseline эффективности двигателя в shipping content и real Settings tests; GAP-02. |
| [EP-0001-US-0009-TK-0004](EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0004-voyage-fuel-settlement/EP-0001-US-0009-TK-0004-voyage-fuel-settlement.md) | Authoritative reservation, refund и расход рейса | NOT_IMPLEMENTED | Реализовать atomic reserve, progress projection, terminal consume/refund, durable settlement и continuation tests; GAP-02. |

### EP-0001-US-0010-cargo-cost-and-trade-receipts — Проверяемая себестоимость и результат продажи груза

[User Story](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-cargo-cost-and-trade-receipts.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0010-TK-0001](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0001-cargo-result-contract/EP-0001-US-0010-TK-0001-cargo-result-contract.md) | Себестоимость в authoritative receipt | NOT_IMPLEMENTED | Дополнить TradeExecutionReceipt nullable RealizedCargoCostCredits/GrossResultCredits и compatibility tests; GAP-02. |
| [EP-0001-US-0010-TK-0002](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis/EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis.md) | Сохраняемый cost basis груза | NOT_IMPLEMENTED | Добавить сохраняемые cargo basis/acquisition sources и корректную legacy-unknown семантику; GAP-02, PLAN-01. |
| [EP-0001-US-0010-TK-0003](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0003-weighted-cost-accounting/EP-0001-US-0010-TK-0003-weighted-cost-accounting.md) | Средневзвешенное списание и результат продажи | NOT_IMPLEMENTED | Реализовать weighted add/remove cost, exact full removal, unknown basis и atomic receipt integration; GAP-02. |
| [EP-0001-US-0010-TK-0004](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0004-cargo-result-texts/EP-0001-US-0010-TK-0004-cargo-result-texts.md) | Тексты стоимости и результата груза | NOT_IMPLEMENTED | Добавить RU/EN строки purchase cost, proceeds, known/unknown/partial cargo result и placeholder tests; GAP-02. |
| [EP-0001-US-0010-TK-0005](EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0005-trade-cost-history/EP-0001-US-0010-TK-0005-trade-cost-history.md) | Проверяемый результат в Trade history | NOT_IMPLEMENTED | Показывать receipt-backed стоимость/результат груза в истории, проверять consistency и сохранять unavailable вместо выдуманного нуля; GAP-02. |

### EP-0001-US-0011-net-voyage-profit — Чистая прибыль завершённого рейса

[User Story](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-net-voyage-profit.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0011-TK-0001](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0001-voyage-finance-contract/EP-0001-US-0011-TK-0001-voyage-finance-contract.md) | Контракт финансового результата рейса | NOT_IMPLEMENTED | Добавить VoyageFinanceSnapshot, states, unsold cargo и VoyageFinances contract/tests; GAP-02. |
| [EP-0001-US-0011-TK-0002](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle/EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle.md) | Жизненный цикл и расходы рейсного ledger | NOT_IMPLEMENTED | Создать ledger lifecycle и уникальные postings расходов, paid/assessed/debt, settlement и terminal handling; GAP-02. |
| [EP-0001-US-0011-TK-0003](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0003-voyage-profit-realization/EP-0001-US-0011-TK-0003-voyage-profit-realization.md) | Реализация грузовой прибыли и snapshot | NOT_IMPLEMENTED | Привязать фактический Sell к carried cargo/рейсу, partial realization, COGS и authoritative net profit; GAP-02. |
| [EP-0001-US-0011-TK-0004](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0004-voyage-profit-texts/EP-0001-US-0011-TK-0004-voyage-profit-texts.md) | Локализованные строки рейсовой прибыли | NOT_IMPLEMENTED | Добавить RU/EN Finance/Trade voyage result keys и format tests; GAP-02. |
| [EP-0001-US-0011-TK-0005](EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0005-voyage-profit-presentation/EP-0001-US-0011-TK-0005-voyage-profit-presentation.md) | Сводка в Finance и Trade history | NOT_IMPLEMENTED | Заменить Finance placeholders данными рейса, добавить deduplicated Trade summaries и manual smoke; GAP-02. |

### EP-0001-US-0012-resume-trading-economy — Продолжение торговой игры после сохранения

[User Story](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-resume-trading-economy.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0012-TK-0001](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0001-economy-save-schema/EP-0001-US-0012-TK-0001-economy-save-schema.md) | Версия и миграция экономического save | NOT_IMPLEMENTED | Ввести complete economy manifest/migration и combined configuration identity после merged prerequisites; GAP-03, PLAN-01. |
| [EP-0001-US-0012-TK-0002](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0002-market-state-continuity/EP-0001-US-0012-TK-0002-market-state-continuity.md) | Непрерывность рынка и событий | PARTIAL | Есть сохранение stock/budget/revision/receipts. Нет полного market/event continuation adapter и соответствующей матрицы checkpoints; GAP-03. |
| [EP-0001-US-0012-TK-0003](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0003-voyage-ledger-continuity/EP-0001-US-0012-TK-0003-voyage-ledger-continuity.md) | Непрерывность рейса, топлива и ledger | PARTIAL | Есть voyage lifecycle persistence. Нет согласованного восстановления fuel reservation/settlement, active/closed ledger и durable terminal IDs; GAP-03. |
| [EP-0001-US-0012-TK-0004](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0004-deterministic-economy-continuation/EP-0001-US-0012-TK-0004-deterministic-economy-continuation.md) | Эквивалентность непрерывной и загруженной игры | NOT_IMPLEMENTED | Добавить общий stage/capture/commit coordinator и непрерывный vs JSON-reloaded run по всем экономическим границам; GAP-03. |
| [EP-0001-US-0012-TK-0005](EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0005-local-save-roundtrip/EP-0001-US-0012-TK-0005-local-save-roundtrip.md) | Файловый Save/Load экономической сессии | PARTIAL | Generic SaveAsync/CreateFromSaveFile есть. Нужен полный economy file-roundtrip с continuation, incompatibility и сохранностью slot при failed overwrite; GAP-03. |

### EP-0001-US-0013-economy-balance-evidence — Воспроизводимое подтверждение жизнеспособной торговли

[User Story](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-economy-balance-evidence.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0013-TK-0001](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0001-balance-diagnostic-seam/EP-0001-US-0013-TK-0001-balance-diagnostic-seam.md) | Детерминированный Engine seam для balance tooling | NOT_IMPLEMENTED | Добавить EconomyBalance friend seam и тест explicit hourly stepping/Save-Load equivalence; GAP-03. |
| [EP-0001-US-0013-TK-0002](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0002-balance-run-matrix/EP-0001-US-0013-TK-0002-balance-run-matrix.md) | Headless runner фиксированной balance-матрицы | NOT_IMPLEMENTED | Создать tool/test projects, Engine-backed runner 12 seeds × 2 configs, samples и реальные исполняемые стратегии; GAP-03. |
| [EP-0001-US-0013-TK-0003](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0003-market-health-evaluation/EP-0001-US-0013-TK-0003-market-health-evaluation.md) | Проверка доступности и здоровья складов | NOT_IMPLEMENTED | Реализовать market-health evaluator: bounds, связность, доступные сделки, zero-stock ratio и supply recovery; GAP-03. |
| [EP-0001-US-0013-TK-0004](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0004-strategy-balance-evaluation/EP-0001-US-0013-TK-0004-strategy-balance-evaluation.md) | Проверка маржи, разнообразия и повторных рейсов | NOT_IMPLEMENTED | Реализовать ledger-based margin/dominance/leader-change/long-route/replay evaluator; GAP-03. |
| [EP-0001-US-0013-TK-0005](EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0005-balance-report-cli/EP-0001-US-0013-TK-0005-balance-report-cli.md) | Канонический отчёт и команда release-gate | NOT_IMPLEMENTED | Добавить canonical JSON, strict CLI/exit codes, exact matrix, solution integration и два полных сопоставимых прогона; GAP-03. |

### EP-0001-US-0014-voyage-lifecycle — Жизненный цикл межстанционного рейса

[User Story](EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-voyage-lifecycle.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0014-TK-0001](EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0001-voyage-contract/EP-0001-US-0014-TK-0001-voyage-contract.md) | Snapshot-контракт жизненного цикла рейса | VALIDATION_GAP | DTO/phases/reason codes есть. Проверить и закрыть contract roundtrip/default/null/array/code coverage; VoyageSnapshotTests.cs отсутствует; GAP-05. |
| [EP-0001-US-0014-TK-0002](EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle/EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle.md) | Authoritative lifecycle и продолжение voyage | VALIDATION_GAP | Engine lifecycle есть. Сопоставить покрытие и закрыть недостающие phase/save/invalid-state/progress/abort cases из AC; GAP-05. |
| [EP-0001-US-0014-TK-0003](EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0003-station-route-departure/EP-0001-US-0014-TK-0003-station-route-departure.md) | Выбор назначения и отправление со Station | PARTIAL | Target-aware отправка есть. Доделать ETA, readable reasons, initial/fallback selection и просмотр blocked option; GAP-05. |
| [EP-0001-US-0014-TK-0004](EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0004-voyage-status-presentation/EP-0001-US-0014-TK-0004-voyage-status-presentation.md) | Статус рейса на GameSession | NOT_IMPLEMENTED | Добавить GameSession phase/destination/progress/blocker presentation и tests/render smoke; GAP-05. |

### EP-0001-US-0015-authoritative-market-quotes — Авторитетная котировка и последовательная цена партии

[User Story](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-authoritative-market-quotes.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0015-TK-0001](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-TK-0001-trade-quote-contract/EP-0001-US-0015-TK-0001-trade-quote-contract.md) | Контракт authoritative-котировки | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0015-TK-0002](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-TK-0002-sequential-price-curve/EP-0001-US-0015-TK-0002-sequential-price-curve.md) | Последовательная кривая цены | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0015-TK-0003](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-TK-0003-market-revision-lifecycle/EP-0001-US-0015-TK-0003-market-revision-lifecycle.md) | Жизненный цикл market revision | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0015-TK-0004](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-TK-0004-authoritative-quote-issuer/EP-0001-US-0015-TK-0004-authoritative-quote-issuer.md) | Выдача и валидация котировки | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |
| [EP-0001-US-0015-TK-0005](EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-TK-0005-local-quote-transport/EP-0001-US-0015-TK-0005-local-quote-transport.md) | LocalClient transport котировки | CODE_PRESENT | Реализация и автоматические проверки найдены. Сохранить существующее поведение при дальнейшей интеграции; полный DoD сверяется отдельно. |

### EP-0001-US-0016-market-knowledge — Устаревающее знание удалённых рынков

[User Story](EP-0001-US-0016-market-knowledge/EP-0001-US-0016-market-knowledge.md)

| Тикет | Название | Статус | Остаток / вывод |
|---|---|---|---|
| [EP-0001-US-0016-TK-0001](EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0001-market-knowledge-contract/EP-0001-US-0016-TK-0001-market-knowledge-contract.md) | Контракт coarse knowledge без удалённых цен | NOT_IMPLEMENTED | Добавить coarse market knowledge DTO и поле snapshot, JSON/backward-compatibility/privacy tests; GAP-04. |
| [EP-0001-US-0016-TK-0002](EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0002-authoritative-market-observations/EP-0001-US-0016-TK-0002-authoritative-market-observations.md) | Authoritative observations, stale и Save/Load | NOT_IMPLEMENTED | Реализовать initial/docked observations, stale без удалённого refresh, save/load и atomic validation; GAP-04. |
| [EP-0001-US-0016-TK-0003](EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0003-market-knowledge-presentation/EP-0001-US-0016-TK-0003-market-knowledge-presentation.md) | Market knowledge в существующей Object Info | NOT_IMPLEMENTED | Передавать knowledge соответствующей станции в Object Info; показать role/bands/time/freshness без exact remote prices; GAP-04. |

## Сводка реестра

| Статус | Тикетов |
|---|---:|
| ACCEPTANCE_PENDING | 5 |
| CODE_PRESENT | 27 |
| NOT_IMPLEMENTED | 34 |
| PARTIAL | 5 |
| REQUIREMENTS_RECONCILIATION | 1 |
| VALIDATION_GAP | 2 |
| Всего | 74 |

Числа описывают состояния аудита, а не процент готовности эпика: тикеты различаются по объёму, CODE_PRESENT не заменяет acceptance, а VALIDATION_GAP не означает отсутствия production-кода.

## Как обновлять этот документ после исполнения

Для закрываемого тикета записать дату, фактически выполненные AC/DoD, ссылки на реализацию и evidence, команды/результаты проверок, отдельный статус manual smoke и оставшиеся ограничения. Закрывать GAP только после проверки всех перечисленных contributions; не менять статус по одному build или имени коммита. Пересчитывать сводку при изменении строк. Production changes и canonical Board approvals оформлять отдельно от этого реестра.
