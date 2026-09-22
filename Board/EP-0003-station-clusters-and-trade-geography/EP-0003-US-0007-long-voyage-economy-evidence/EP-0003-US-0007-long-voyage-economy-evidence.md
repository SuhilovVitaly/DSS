---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0007-long-voyage-economy-evidence
title: Проверяемая экономика длительных путешествий
stage: draft
dependencies: [EP-0003-US-0005-local-and-intercluster-voyages, EP-0001-US-0002-market-replenishment, EP-0001-US-0007-temporary-market-events, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0013-economy-balance-evidence]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Проверяемая экономика длительных путешествий

## Входное техническое задание

[Документация эпика](../Documentation.md): Разделы «Сохранение и проверка экономики», «Граница с существующим торговым эпиком»; E3-AC-08.
[Общая концепция](../../../Documentation/02-FirstRelease/Mechanics/SolarSystemMapConcept.md) — продуктовый источник. Это требуемый результат, а не утверждение о готовности runtime.

Исходное описание и три completion evidence сохранены без смены продуктового результата. Запрос тикетов записан 2026-09-22T14:40:41Z UTC; точное время получения сообщения недоступно, это время фиксации при работе. created добавлен при канонизации metadata, не выдаётся за время первоначального создания истории.

Точное сообщение пользователя (HTML-entity сохранена как получена):

```text
сделай тикеты для историй в эпиках 2 3 и 4&#x20;
D:\DeepSpaceSaga\DSS\Board\EP-0002-procedural-solar-system&#x20;
D:\DeepSpaceSaga\DSS\Board\EP-0003-station-clusters-and-trade-geography
D:\DeepSpaceSaga\DSS\Board\EP-0004-ai-territories-and-map-environment
```

## User story

Как разработчик, настраивающий экономику, я хочу сравнивать местные циклы и длительные рейсы на новой карте по воспроизводимому отчёту. Горизонт составляет не менее 100 календарных дней и продлевается, если этого требует полный дальний рейс туда и обратно. Отчёт показывает снабжение, остатки, бюджеты, действие событий, расходы и итог путешествий. По нему можно обнаружить несогласованные часы и дни, дефицит или отсутствие экономического смысла маршрутов.

## Acceptance criteria

- AC-0001: Для объявленных seed и параметров воспроизводятся несколько местных циклов и полный дальний рейс с возвращением на достаточном горизонте.
- AC-0002: В отчёте есть запасы, снабжение, бюджеты, сроки событий, расходы и результаты; условия и недостатки данных названы явно.
- AC-0003: Пороги прежней пятистанционной карты сопоставлены с новыми длительностями; необходимые изменения направлены владельцам EP-0001, а непроверенная прибыль не объявлена доказанной.

## Non-goals

G3-04: численные границы приемлемого баланса и готовность механик EP-0001 требуют проверки перед реализацией. Данная история не утверждает новые формулы цен или гарантированный процент прибыли.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0003-US-0005-local-and-intercluster-voyages, EP-0001-US-0002-market-replenishment, EP-0001-US-0007-temporary-market-events, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0013-economy-balance-evidence. US-0005 даёт действующие рейсы; EP-0001/US-0002, US-0007, US-0008, US-0013 — обновление рынков, события, маршрутные условия и экономическую диагностику.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0003-US-0005-local-and-intercluster-voyages: EP-0003-US-0005-TK-0001-cluster-voyage-integration, EP-0003-US-0005-TK-0002-voyage-user-path.
- EP-0001-US-0002-market-replenishment: EP-0001-US-0002-TK-0001-market-stock-snapshot, EP-0001-US-0002-TK-0002-market-economy-schema, EP-0001-US-0002-TK-0003-hourly-market-simulation, EP-0001-US-0002-TK-0004-market-flow-content, EP-0001-US-0002-TK-0005-market-state-trade-ui.
- EP-0001-US-0007-temporary-market-events: EP-0001-US-0007-TK-0001-market-event-contract, EP-0001-US-0007-TK-0002-market-event-catalog, EP-0001-US-0007-TK-0003-market-event-content, EP-0001-US-0007-TK-0004-market-event-lifecycle, EP-0001-US-0007-TK-0005-trade-event-presentation.
- EP-0001-US-0008-route-risk-and-alternatives: EP-0001-US-0008-TK-0001-route-risk-contract, EP-0001-US-0008-TK-0002-effective-route-evaluator, EP-0001-US-0008-TK-0003-route-event-voyage-integration, EP-0001-US-0008-TK-0004-route-choice-presentation, EP-0001-US-0008-TK-0005-route-risk-content.
- EP-0001-US-0013-economy-balance-evidence: EP-0001-US-0013-TK-0001-balance-diagnostic-seam, EP-0001-US-0013-TK-0002-balance-run-matrix, EP-0001-US-0013-TK-0003-market-health-evaluation, EP-0001-US-0013-TK-0004-strategy-balance-evaluation, EP-0001-US-0013-TK-0005-balance-report-cli.

## Grounding

- src/DeepSpaceSaga.Engine/SimulationEngine.cs:197 — public void LoadScenario(ScenarioFile scenario, bool isSave = false): используется единая загрузка runtime; новый stage должен завершиться до публикации.
- src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:54 — public sealed record GameStateData(: общая scenario/save модель; запланированные новые блоки ещё не реализованы.
- src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11 — public sealed record AuthoritativeSnapshot(: immutable session boundary.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:147 — internal bool FitMapView(MapFitMode mode): существующая карта имеет System-fit; это не доказательство доменных кластеров.
- src/DeepSpaceSaga.Client/Scenarios/MarketProfiles/scenario.json:1 — five-station fixture содержит позиции9900/10100 world около игрока10000; сохранение локальных расстояний имеет приоритет начальной настройки дней.

## Invariants

- `src/DeepSpaceSaga.Contracts/SimulationSpeed.cs:27`: календарный коэффициент300; движение и календарь не взаимозаменяемы.
- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:4`: world units100m, speed km/s, clockwise0up. Отображение не изменяет мир.
- `Documentation/00-Process/CLAUDE.md:34`: render loop не запрашивает Engine; Contracts без graphics, Motion общий.
- `Documentation/01-Requirements/EngineRequirements.md:5335`: Approach с постоянной скоростью; новый map scope не даёт ускорение кораблю.

## Assumptions and resolutions

- A-1: Текущий запрос охватывает все8 существующих историй каждого из3 эпиков. Story/epic stage draft сохранён: approved ticket map означает принятие нарезки автоматическим StoryBuilder workflow, не выдуманное индивидуальное утверждение эпика.
- A-2: Новые технические API, файлы и значения конфигурации ниже — план, а не реализованный код. До dependent тикета обязательны все depends_on, включая EP-0001; статус документа не доказывает поставку.
- A-3: Новый SolarSystem режим уточняет только географию/знания новой карты; legacy сценарии/fixtures/сохранения без блока сохраняют старую политику. Requirements-файлы этим запросом не редактируются.
- A-4: Save compatibility: supported старый мир продолжается без новой генерации; неизвестная версия/повреждённый блок отклоняется атомарно. Следующий номер save выбирается после интеграции предыдущих writers, не фиксируется поверх чужого изменения.
- A-5: Допуски координат1e-6 world и относительный1e-12 — инженерная точность проверок, не gameplay радиус. Бюджеты генерации/snapshot/save и пороги прибыли не заданы; отчёты оставляют их not-assessed. Цель кадров80FPS проверяется на явно указанном оборудовании.
- A-6: Параметры old five-station/hour mode не меняются. MarketProfiles исходные близкие станции — явное исключение balancing neighbour диапазона; сохранять их расстояния. Rigid group допускает разные радиусы при общей angular velocity. Ресурсные данные EP-0001 единственные; формулы экономики не дублируются.

## Approved ticket map

План принят автоматическим workflow по прямому запросу пользователя. Это approval объёма тикетов; не утверждение реализации или новый EpicBuilder approval.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](./EP-0003-US-0007-TK-0001-long-voyage-runner/EP-0003-US-0007-TK-0001-long-voyage-runner.md) | Экономический прогон полной дальней поездки | tooling | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](./EP-0003-US-0007-TK-0002-long-voyage-diagnostics/EP-0003-US-0007-TK-0002-long-voyage-diagnostics.md) | Отчёт снабжения и ограничений длительных маршрутов | tooling | TK-0001 + внешние gates | AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

## Gaps and backlog

- Нерешённых блокирующих продуктовых вопросов для создания тикетов нет.
- Gap поставки: новые map/orbit/cluster/AI файлы и часть EP-0001 APIs пока плановые. Реализация зависит от named gates выше; пропуск dependency запрещён.
- Численные budgets generation/snapshot/save и целевая прибыль неизвестны; отчёты измеряют факты без выдуманного PASS. Отдельное балансное решение остаётся backlog.
- Синхронизация общей концепции и исторических формулировок requirements — отдельная документационная поставка; здесь режимные уточнения и технические assumptions перечислены явно, requirements не изменены.
- Базовые trade/quote/fuel/events/ledger fixes — владельцы EP-0001. Новые пороги прибыли, NPC traders и дипломатия вне scope.

## Decision and review log

| UTC | Источник | Действие | Результат |
|---|---|---|---|
| 2026-09-22T14:40:41Z | Точное сообщение в разделе входного задания | Grounding, карта тикетов и assumptions | Автоматический workflow; без дополнительного approval эпика |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0007-TK-0001-long-voyage-runner | stage approved; 4 files; tooling; AC 1/2 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0007-TK-0002-long-voyage-diagnostics | stage approved; 3 files; tooling; AC 2/3 |
