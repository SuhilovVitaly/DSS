---
epic: EP-0002-procedural-solar-system
story: EP-0002-US-0002-coherent-orbital-world
title: Согласованное движение орбитального мира
stage: draft
dependencies: [EP-0002-US-0001-seeded-system-start]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Согласованное движение орбитального мира

## Входное техническое задание

[Документация эпика](../Documentation.md): Раздел «Орбиты и навигация»; E2-AC-04. Общая концепция: R-02, R-08 и раздел 7.
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

Как игрок, я хочу видеть плавное и согласованное движение планет и сценарных станций по орбитам, чтобы их положение оставалось понятным при изменении скорости игры. Пауза останавливает мир, а ускорение и календарный переход сохраняют согласованность положений, скоростей и привязанного к станции корабля. Свободный корабль продолжает выполнять собственные команды без изменения его физической скорости. Движение поддерживает точность, необходимую будущим компактным группам станций.

## Acceptance criteria

- AC-0001: На одном мире сопоставлены авторитетные и отображаемые положения и скорости при Speed0–4, дробном продвижении и календарном переходе; Солнце остаётся в центре.
- AC-0002: Пристыкованный корабль сохраняет относительное положение, свободный корабль не получает множитель 300; одинаковая конечная эпоха даёт одинаковые орбитальные положения.
- AC-0003: Проверка компактных фазовых смещений подтверждает отсутствие округления, разрушающего заданные малые расстояния на большом радиусе.

## Non-goals

G2-01: до реализации требуется оформить совместимость точных фаз и орбитального времени с разделами 6 и 22 EngineRequirements; конкретный формат здесь не утверждается. Новые правила сближения не вводятся этой историей.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0002-US-0001-seeded-system-start. US-0001 предоставляет созданную систему, которую игрок наблюдает во времени.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0002-US-0001-seeded-system-start: EP-0002-US-0001-TK-0001-system-map-contract, EP-0002-US-0001-TK-0002-generation-input-schema, EP-0002-US-0001-TK-0003-seeded-world-bootstrap, EP-0002-US-0001-TK-0004-default-system-content, EP-0002-US-0001-TK-0005-initial-system-view.

## Grounding

- src/DeepSpaceSaga.Engine/SimulationEngine.cs:197 — public void LoadScenario(ScenarioFile scenario, bool isSave = false): используется единая загрузка runtime; новый stage должен завершиться до публикации.
- src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:54 — public sealed record GameStateData(: общая scenario/save модель; запланированные новые блоки ещё не реализованы.
- src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11 — public sealed record AuthoritativeSnapshot(: immutable session boundary.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:147 — internal bool FitMapView(MapFitMode mode): существующая карта имеет System-fit; это не доказательство доменных кластеров.
- src/DeepSpaceSaga.Engine/RuntimeMotion.cs:10 — текущий At делегирует LinearMotionPredictor; орбитальный runtime является работой этих тикетов.

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
- A-6: Орбитальная точность закрыта additive PhaseOffsetDegrees и явным period/epoch; Undock уже существует. Полное spatial knowledge включается только для нового режима; никаких удалённых live quotes.

## Approved ticket map

План принят автоматическим workflow по прямому запросу пользователя. Это approval объёма тикетов; не утверждение реализации или новый EpicBuilder approval.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](./EP-0002-US-0002-TK-0001-absolute-orbit-math/EP-0002-US-0002-TK-0001-absolute-orbit-math.md) | Общая аналитическая поза и скорость орбиты | motion | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](./EP-0002-US-0002-TK-0002-authoritative-orbit-runtime/EP-0002-US-0002-TK-0002-authoritative-orbit-runtime.md) | Орбиты и пристыкованные корабли в авторитетном мире | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](./EP-0002-US-0002-TK-0003-orbital-client-prediction/EP-0002-US-0002-TK-0003-orbital-client-prediction.md) | Плавный прогноз орбит из клиентского буфера | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002 → TK-0003. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

## Gaps and backlog

- Нерешённых блокирующих продуктовых вопросов для создания тикетов нет.
- Gap поставки: новые map/orbit/cluster/AI файлы и часть EP-0001 APIs пока плановые. Реализация зависит от named gates выше; пропуск dependency запрещён.
- Численные budgets generation/snapshot/save и целевая прибыль неизвестны; отчёты измеряют факты без выдуманного PASS. Отдельное балансное решение остаётся backlog.
- Синхронизация общей концепции и исторических формулировок requirements — отдельная документационная поставка; здесь режимные уточнения и технические assumptions перечислены явно, requirements не изменены.
- Кластеры — EP-0003, территории/поля — EP-0004; сверхбыстрый полёт и N-body вне scope.

## Decision and review log

| UTC | Источник | Действие | Результат |
|---|---|---|---|
| 2026-09-22T14:40:41Z | Точное сообщение в разделе входного задания | Grounding, карта тикетов и assumptions | Автоматический workflow; без дополнительного approval эпика |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0002-US-0002-TK-0001-absolute-orbit-math | stage approved; 3 files; motion; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0002-US-0002-TK-0002-authoritative-orbit-runtime | stage approved; 5 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0002-US-0002-TK-0003-orbital-client-prediction | stage approved; 3 files; client; AC 1/2/3 |
