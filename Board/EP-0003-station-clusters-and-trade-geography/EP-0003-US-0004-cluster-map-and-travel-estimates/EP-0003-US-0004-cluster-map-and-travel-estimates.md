---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0004-cluster-map-and-travel-estimates
title: Выбор станции и направления по карте
stage: draft
dependencies: [EP-0003-US-0002-distinct-orbital-clusters, EP-0003-US-0003-cluster-resource-surroundings, EP-0002-US-0006-known-system-map, EP-0001-US-0016-market-knowledge]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Выбор станции и направления по карте

## Входное техническое задание

[Документация эпика](../Documentation.md): Раздел «Отображение и доступные сведения»; E3-AC-03, E3-AC-06.
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

Как игрок, я хочу находить торговый район, приблизить его и выбрать конкретную станцию с понятными сведениями для поездки. Карта показывает название кластера, специализации станций, ресурсы, актуальные расстояния и явно обозначенную оценку прямого полёта. При движении районов оценки обновляются, а плановое торговое направление отличается от подтверждённого манёвра. Известность всей географии не превращает удалённые цены в свежую котировку.

## Acceptance criteria

- AC-0001: С общего обзора можно перейти к любому кластеру и выбрать каждую из его станций; видны принадлежность кластеру, профиль, расстояние и оценка.
- AC-0002: Изменение эпохи изменяет межкластерные оценки согласно положению объектов; масштаб камеры не меняет расчёт.
- AC-0003: Подтверждённый маршрут и предполагаемое направление визуально различаются; состояние рыночных знаний и возможность торговли соответствуют правилам EP-0001.

## Non-goals

Экономический кластер не подменяется экранной агрегацией маркеров. Новый торговый экран и собственный расчёт котировок не требуются; точные решения по композиции UI остаются последующему планированию.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0003-US-0002-distinct-orbital-clusters, EP-0003-US-0003-cluster-resource-surroundings, EP-0002-US-0006-known-system-map, EP-0001-US-0016-market-knowledge. US-0002–0003 дают районы и ресурсы, EP-0002/US-0006 — общий обзор, EP-0001/US-0016 — границу актуальности знаний о рынках.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0003-US-0002-distinct-orbital-clusters: EP-0003-US-0002-TK-0001-multi-cluster-placement, EP-0003-US-0002-TK-0002-full-cluster-content, EP-0003-US-0002-TK-0003-multi-cluster-overview.
- EP-0003-US-0003-cluster-resource-surroundings: EP-0003-US-0003-TK-0001-resource-orbit-binding, EP-0003-US-0003-TK-0002-cluster-resource-placement, EP-0003-US-0003-TK-0003-cluster-resource-map.
- EP-0002-US-0006-known-system-map: EP-0002-US-0006-TK-0001-known-map-projection, EP-0002-US-0006-TK-0002-system-map-navigation.
- EP-0001-US-0016-market-knowledge: EP-0001-US-0016-TK-0001-market-knowledge-contract, EP-0001-US-0016-TK-0002-authoritative-market-observations, EP-0001-US-0016-TK-0003-market-knowledge-presentation.

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
| [TK-0001](./EP-0003-US-0004-TK-0001-cluster-travel-estimates/EP-0003-US-0004-TK-0001-cluster-travel-estimates.md) | Актуальные расстояния и честная оценка прямого полёта | client | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0004-TK-0001-cluster-travel-estimates | stage approved; 5 files; client; AC 1/2/3 |
