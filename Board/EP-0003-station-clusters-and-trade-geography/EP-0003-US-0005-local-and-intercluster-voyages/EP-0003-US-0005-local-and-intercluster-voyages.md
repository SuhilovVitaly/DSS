---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0005-local-and-intercluster-voyages
title: Местный цикл и дальний рейс с возвращением
stage: draft
dependencies: [EP-0003-US-0004-cluster-map-and-travel-estimates, EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0011-net-voyage-profit, EP-0001-US-0014-voyage-lifecycle]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Местный цикл и дальний рейс с возвращением

## Входное техническое задание

[Документация эпика](../Documentation.md): Разделы «Торговая сеть и расстояния», «Компактное движение», «Сохранение и проверка экономики»; E3-AC-05.
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

Как игрок, я хочу выполнить местный торговый цикл, а затем отправиться в другой кластер и вернуться с грузом. На новой географии работают подготовка рейса, уход, перелёт, сближение, синхронизация, стыковка и сделки через действующие правила игры. Движение станций не лишает предусмотренные направления возможности завершить путешествие. Я вижу фактические расходы и результат без обещания гарантированной прибыли.

## Acceptance criteria

- AC-0001: Демонстрационный прогон завершает местный цикл и полный межкластерный рейс туда и обратно, фиксируя посещения, сделки и смены состояния рейса.
- AC-0002: Сближение со станциями работает на новых расстояниях без изменения скорости Approach; фактическое время отделено от начальной оценки по прямой.
- AC-0003: Расходы и финансовый результат получены из торговли EP-0001; нет второго расчёта цены, топлива или прибыли внутри географии.

## Non-goals

G3-03: готовность перечисленных торговых возможностей проверяется по реализации, а не статусу планирования. Исправления базовой торговли остаются в EP-0001. Рассматриваются предусмотренные демонстрационные рейсы, не гарантированная достижимость любой цели.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0003-US-0004-cluster-map-and-travel-estimates, EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0011-net-voyage-profit, EP-0001-US-0014-voyage-lifecycle. US-0004 даёт выбор направлений; EP-0001/US-0006, US-0009, US-0011, US-0014 предоставляют повторяемое путешествие, топливные расходы, финансовый итог и lifecycle рейса.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0003-US-0004-cluster-map-and-travel-estimates: EP-0003-US-0004-TK-0001-cluster-travel-estimates.
- EP-0001-US-0006-repeatable-trading-voyage: EP-0001-US-0006-TK-0001-round-trip-engine-proof, EP-0001-US-0006-TK-0002-trade-visit-context, EP-0001-US-0006-TK-0003-station-trade-navigation.
- EP-0001-US-0009-voyage-fuel-cost: EP-0001-US-0009-TK-0001-voyage-fuel-contract, EP-0001-US-0009-TK-0002-fuel-accounting-foundation, EP-0001-US-0009-TK-0003-engine-efficiency-content, EP-0001-US-0009-TK-0004-voyage-fuel-settlement.
- EP-0001-US-0011-net-voyage-profit: EP-0001-US-0011-TK-0001-voyage-finance-contract, EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle, EP-0001-US-0011-TK-0003-voyage-profit-realization, EP-0001-US-0011-TK-0004-voyage-profit-texts, EP-0001-US-0011-TK-0005-voyage-profit-presentation.
- EP-0001-US-0014-voyage-lifecycle: EP-0001-US-0014-TK-0001-voyage-contract, EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle, EP-0001-US-0014-TK-0003-station-route-departure, EP-0001-US-0014-TK-0004-voyage-status-presentation.

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
| [TK-0001](./EP-0003-US-0005-TK-0001-cluster-voyage-integration/EP-0003-US-0005-TK-0001-cluster-voyage-integration.md) | Действующий местный и межкластерный рейс | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](./EP-0003-US-0005-TK-0002-voyage-user-path/EP-0003-US-0005-TK-0002-voyage-user-path.md) | Пользовательский путь поездки между кластерами | client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0005-TK-0001-cluster-voyage-integration | stage approved; 3 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0005-TK-0002-voyage-user-path | stage approved; 3 files; client; AC 1/2/3 |
