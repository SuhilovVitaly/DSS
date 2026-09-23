---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0001-local-trading-cluster
title: Рабочая торговля в стартовом кластере
stage: draft
dependencies: [EP-0002-US-0005-orbital-station-visit, EP-0001-US-0001-station-market-profiles, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0004-seeded-trading-map]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Рабочая торговля в стартовом кластере

## Входное техническое задание

[Документация эпика](../Documentation.md): Разделы «Человеческие поселения», «Торговая сеть и расстояния», «Сценарное окружение и ресурсы»; E3-AC-01–04.
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

Как игрок, я хочу начинать игру в компактном кластере с несколькими торговыми направлениями, чтобы первая полезная сделка не требовала дальней экспедиции. В базовой конфигурации кластер содержит 10–12 станций в астероидном поясе и представляет все пять экономических профилей. Сценарные станции входят в этот состав без дублирования и сохраняют свои особые данные. Сеть предоставляет потребителей основной продукции, минимум два независимых цикла, минимум два потенциальных направления от станции и стартовое направление с возможной обратной загрузкой.

## Acceptance criteria

- AC-0001: Во всех текущих сценариях стартовый кластер содержит 10–12 станций и пять профилей; сохранены сценарные ссылки, явные склады и назначение старта, а радиус до Солнца остаётся в пределах 50–75 дней.
- AC-0002: На карте и при посещении доступны стартовый рынок и соседние рынки; показаны потенциальные потоки продукции и обратная загрузка без требования производства у транзита.
- AC-0003: Связность, два независимых цикла и направления проверены по ролям; компактность и исходные локальные расстояния соответствуют объявленной конфигурации и сохраняются при движении.

## Non-goals

G3-01: EP-0001/US-0004 описывает пять неподвижных станций и пороги в часах; перед реализацией необходимо согласовать расширение её контракта для этого режима. Оно не считается уже выполненным или автоматически утверждённым. Базовые сделки остаются у EP-0001.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0002-US-0005-orbital-station-visit, EP-0001-US-0001-station-market-profiles, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0004-seeded-trading-map. EP-0002/US-0005 даёт посещаемые орбитальные станции; EP-0001/US-0001, US-0003 и US-0004 — профили, сделки и экономические связи для расширения географии.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0002-US-0005-orbital-station-visit: EP-0002-US-0005-TK-0001-orbital-synchronization, EP-0002-US-0005-TK-0002-orbital-docking-departure.
- EP-0001-US-0001-station-market-profiles: EP-0001-US-0001-TK-0001-market-profile-schema, EP-0001-US-0001-TK-0002-profile-market-bootstrap, EP-0001-US-0001-TK-0003-market-catalog-content, EP-0001-US-0001-TK-0004-five-market-demo, EP-0001-US-0001-TK-0005-profile-trade-presentation.
- EP-0001-US-0003-dynamic-market-trading: EP-0001-US-0003-TK-0001-trade-execution-contract, EP-0001-US-0003-TK-0002-atomic-quote-execution, EP-0001-US-0003-TK-0003-trade-result-texts, EP-0001-US-0003-TK-0004-quoted-trade-controls, EP-0001-US-0003-TK-0005-confirmed-trade-history.
- EP-0001-US-0004-seeded-trading-map: EP-0001-US-0004-TK-0001-trading-map-schema, EP-0001-US-0004-TK-0002-economic-graph.

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
| [TK-0001](./EP-0003-US-0001-TK-0001-cluster-geography-contract/EP-0003-US-0001-TK-0001-cluster-geography-contract.md) | Доменные кластеры и экономические связи карты | contracts | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](./EP-0003-US-0001-TK-0002-local-cluster-generation/EP-0003-US-0001-TK-0002-local-cluster-generation.md) | Стартовый кластер из экономических ролей | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](./EP-0003-US-0001-TK-0003-local-cluster-content/EP-0003-US-0001-TK-0003-local-cluster-content.md) | Настройки первой торговой группы | content-data | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0004](./EP-0003-US-0001-TK-0004-local-cluster-map/EP-0003-US-0001-TK-0004-local-cluster-map.md) | Стартовый район и доступные соседние станции | client | TK-0003 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002 → TK-0003 → TK-0004. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0001-TK-0001-cluster-geography-contract | stage approved; 3 files; contracts; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0001-TK-0002-local-cluster-generation | stage approved; 5 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0001-TK-0003-local-cluster-content | stage approved; 2 files; content-data; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0001-TK-0004-local-cluster-map | stage approved; 5 files; client; AC 1/2/3 |
