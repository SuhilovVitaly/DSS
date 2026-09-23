---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0002-distinct-orbital-clusters
title: Несколько разных торговых районов
stage: draft
dependencies: [EP-0003-US-0001-local-trading-cluster]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Несколько разных торговых районов

## Входное техническое задание

[Документация эпика](../Documentation.md): Разделы «Человеческие поселения», «Торговая сеть и расстояния», «Компактное движение»; E3-AC-01–04.
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

Как игрок, я хочу видеть несколько различающихся человеческих районов, чтобы дальнее путешествие давало доступ к другим ресурсам и потребностям. Базовая настройка создаёт 3–5 кластеров по 10–12 станций в астероидных поясах с устойчивыми названиями и составом. Станции внутри каждого кластера сохраняют взаимные расстояния, а разные кластеры меняют взаимное положение по орбитам. Межкластерные расстояния заметно больше локальных и оцениваются для текущей эпохи.

## Acceptance criteria

- AC-0001: Одинаковый seed воспроизводит состав, специализации, принадлежность поясам и размещение всех кластеров; начальная конфигурация представляет пять профилей в каждом.
- AC-0002: Проверены ориентиры 0,5–2 дня между ближайшими торговыми соседями, 3–7 дней по диаметру и 15–35 дней между соседними кластерами на старте согласно выбранной конфигурации.
- AC-0003: Постоянство внутренних расстояний подтверждено свойствами модели и прогоном; на эпохах 1, 7, 30, 100, 365 дней и критических сближениях группы не распадаются и не сливаются.

## Non-goals

Диапазоны длительности являются начальными балансными настройками и оценкой прямого полёта, а не гарантией ETA для всех пар во все эпохи. Количество станций — следствие состава кластеров, а не отдельный жёсткий лимит. При невозможном размещении недопустима публикация частичного мира.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0003-US-0001-local-trading-cluster. US-0001 предоставляет рабочую единицу размещения — компактный стартовый кластер с торговыми ролями.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0003-US-0001-local-trading-cluster: EP-0003-US-0001-TK-0001-cluster-geography-contract, EP-0003-US-0001-TK-0002-local-cluster-generation, EP-0003-US-0001-TK-0003-local-cluster-content, EP-0003-US-0001-TK-0004-local-cluster-map.

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
| [TK-0001](./EP-0003-US-0002-TK-0001-multi-cluster-placement/EP-0003-US-0002-TK-0001-multi-cluster-placement.md) | Разные орбитальные торговые районы без распада | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](./EP-0003-US-0002-TK-0002-full-cluster-content/EP-0003-US-0002-TK-0002-full-cluster-content.md) | Полная конфигурация 3–5 кластеров | content-data | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](./EP-0003-US-0002-TK-0003-multi-cluster-overview/EP-0003-US-0002-TK-0003-multi-cluster-overview.md) | Обзор нескольких районов одной карты | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002 → TK-0003. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0002-TK-0001-multi-cluster-placement | stage approved; 3 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0002-TK-0002-full-cluster-content | stage approved; 2 files; content-data; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0002-TK-0003-multi-cluster-overview | stage approved; 4 files; client; AC 1/2/3 |
