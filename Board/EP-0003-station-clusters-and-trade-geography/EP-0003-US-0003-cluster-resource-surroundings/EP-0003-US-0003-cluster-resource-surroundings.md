---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0003-cluster-resource-surroundings
title: Ресурсное окружение торговых районов
stage: draft
dependencies: [EP-0003-US-0002-distinct-orbital-clusters, EP-0001-US-0005-station-resource-fields]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Ресурсное окружение торговых районов

## Входное техническое задание

[Документация эпика](../Documentation.md): Раздел «Сценарное окружение и ресурсы»; E3-AC-01, E3-AC-06–07. Общая концепция: раздел 8.
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

Как игрок, я хочу видеть ресурсные скопления, соответствующие специализации района, чтобы понимать различия между торговыми кластерами. Лёд, руды и другие допустимые ресурсы используют действующий каталог и связаны с окружением подходящих станций. Положение и состав воспроизводятся по seed и доступны на открытой карте. Ресурсное поле само по себе не пополняет станционный склад.

## Acceptance criteria

- AC-0001: У кластеров показаны ресурсные области и сведения о составе, согласованные с ролями станций и допустимыми товарами.
- AC-0002: Повторный запуск воспроизводит геометрию и состав; движение сохраняет назначенные привязки, а ссылки на объекты и ресурсы разрешаются.
- AC-0003: Просмотр не требует разведки; обычные правила действующих команд сохраняются, автоматического снабжения склада из поля не возникает.

## Non-goals

G3-02: расширение ресурсных полей EP-0001/US-0005 должно сохранять единственное владение их данными. Новый каталог товаров, автоматическая добыча и эффекты среды не входят в историю.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0003-US-0002-distinct-orbital-clusters, EP-0001-US-0005-station-resource-fields. US-0002 даёт роли и расположение районов; EP-0001/US-0005 — результат ресурсных полей, который расширяется на кластерную географию.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0003-US-0002-distinct-orbital-clusters: EP-0003-US-0002-TK-0001-multi-cluster-placement, EP-0003-US-0002-TK-0002-full-cluster-content, EP-0003-US-0002-TK-0003-multi-cluster-overview.
- EP-0001-US-0005-station-resource-fields: EP-0001-US-0005-TK-0001-resource-survey-contract, EP-0001-US-0005-TK-0002-seeded-resource-fields, EP-0001-US-0005-TK-0003-resource-field-content, EP-0001-US-0005-TK-0004-structural-resource-survey, EP-0001-US-0005-TK-0005-resource-info-panel.

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
| [TK-0001](./EP-0003-US-0003-TK-0001-resource-orbit-binding/EP-0003-US-0003-TK-0001-resource-orbit-binding.md) | Орбитальная привязка существующих ресурсных полей | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](./EP-0003-US-0003-TK-0002-cluster-resource-placement/EP-0003-US-0003-TK-0002-cluster-resource-placement.md) | Ресурсы по ролям без дублирования полей | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](./EP-0003-US-0003-TK-0003-cluster-resource-map/EP-0003-US-0003-TK-0003-cluster-resource-map.md) | Известные ресурсы рядом с торговыми районами | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0003-TK-0001-resource-orbit-binding | stage approved; 2 files; contracts; AC 1/2 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0003-TK-0002-cluster-resource-placement | stage approved; 5 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0003-US-0003-TK-0003-cluster-resource-map | stage approved; 4 files; client; AC 1/2/3 |
