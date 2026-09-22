---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0001-hostile-ai-bases
title: Различимые враждебные базы ИИ
stage: draft
dependencies: [EP-0002-US-0006-known-system-map, EP-0003-US-0002-distinct-orbital-clusters]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Различимые враждебные базы ИИ

## Входное техническое задание

[Документация эпика](../Documentation.md): Раздел «Принадлежность и размещение баз»; E4-AC-01–03, E4-AC-05. Общая концепция: R-05, R-09.
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

Как игрок, я хочу отличать базы ИИ от человеческих станций и просматривать их принадлежность на карте. Базы могут находиться на планетах и в пространстве между человеческими районами, при этом человеческих планетных поселений нет. Планетная база следует за планетой, а свободная — за своей орбитой. Враждебная база доступна для выбора, но попытки торговать или стыковаться с ней отклоняются правилами игры.

## Acceptance criteria

- AC-0001: Созданные базы известны с начала игры, выбираются и показывают принадлежность; научно-военная человеческая станция не принимается за базу ИИ.
- AC-0002: Повторение исходных условий воспроизводит ID, типы и размещение; родительские ссылки разрешаются, движение сохраняет привязку.
- AC-0003: Торговля и стыковка отклоняются авторитетно, включая попытку команды в обход скрытой кнопки; человеческие станции сохраняют доступность.

## Non-goals

Количество баз и варианты типов остаются настройками. Отношения человеческих фракций друг с другом, дипломатия, захват и бой не добавляются; общий враг не означает союз между всеми людьми.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0002-US-0006-known-system-map, EP-0003-US-0002-distinct-orbital-clusters. EP-0002/US-0006 даёт известную движущуюся карту, EP-0003/US-0002 — человеческие районы, относительно которых размещаются базы.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0002-US-0006-known-system-map: EP-0002-US-0006-TK-0001-known-map-projection, EP-0002-US-0006-TK-0002-system-map-navigation.
- EP-0003-US-0002-distinct-orbital-clusters: EP-0003-US-0002-TK-0001-multi-cluster-placement, EP-0003-US-0002-TK-0002-full-cluster-content, EP-0003-US-0002-TK-0003-multi-cluster-overview.

## Grounding

- src/DeepSpaceSaga.Engine/SimulationEngine.cs:197 — public void LoadScenario(ScenarioFile scenario, bool isSave = false): используется единая загрузка runtime; новый stage должен завершиться до публикации.
- src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:54 — public sealed record GameStateData(: общая scenario/save модель; запланированные новые блоки ещё не реализованы.
- src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11 — public sealed record AuthoritativeSnapshot(: immutable session boundary.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:147 — internal bool FitMapView(MapFitMode mode): существующая карта имеет System-fit; это не доказательство доменных кластеров.
- src/DeepSpaceSaga.Engine/Dialogue/DialogueEffectTransaction.cs:14 — Prepare собирает detached transaction и принимает validateDock callback; запрет AI нужно проверять и здесь.

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
- A-6: Количество/радиусы/плотности явно заданы в content тикетах как начальная настройка. Владение Ai проверяется авторитетно на всех docking/trade путях. Проверка обходов ограничена365d и включает критические эпохи; это не автопилот/вечная безопасность.

## Approved ticket map

План принят автоматическим workflow по прямому запросу пользователя. Это approval объёма тикетов; не утверждение реализации или новый EpicBuilder approval.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](./EP-0004-US-0001-TK-0001-ai-base-contract/EP-0004-US-0001-TK-0001-ai-base-contract.md) | Принадлежность баз и привязки объектов ИИ | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](./EP-0004-US-0001-TK-0002-seeded-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases.md) | Воспроизводимые планетные и свободные базы | engine | TK-0001 + внешние gates | AC-0001, AC-0002 |
| [TK-0003](./EP-0004-US-0001-TK-0003-authoritative-hostile-access/EP-0004-US-0001-TK-0003-authoritative-hostile-access.md) | Авторитетный запрет стыковки и торговли с ИИ | engine | TK-0002 + внешние gates | AC-0003 |
| [TK-0004](./EP-0004-US-0001-TK-0004-ai-base-content/EP-0004-US-0001-TK-0004-ai-base-content.md) | Начальные настройки баз ИИ | content-data | TK-0003 + внешние gates | AC-0001, AC-0002 |
| [TK-0005](./EP-0004-US-0001-TK-0005-ai-base-presentation/EP-0004-US-0001-TK-0005-ai-base-presentation.md) | Выбор базы и различимая принадлежность | client | TK-0004 + внешние gates | AC-0001, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

## Gaps and backlog

- Нерешённых блокирующих продуктовых вопросов для создания тикетов нет.
- Gap поставки: новые map/orbit/cluster/AI файлы и часть EP-0001 APIs пока плановые. Реализация зависит от named gates выше; пропуск dependency запрещён.
- Численные budgets generation/snapshot/save и целевая прибыль неизвестны; отчёты измеряют факты без выдуманного PASS. Отдельное балансное решение остаётся backlog.
- Синхронизация общей концепции и исторических формулировок requirements — отдельная документационная поставка; здесь режимные уточнения и технические assumptions перечислены явно, requirements не изменены.
- Патрули/перехват/бой — вторая стадия; эффекты радиации/пыли, exploration/salvage/награды, захват и дипломатия — backlog.

## Decision and review log

| UTC | Источник | Действие | Результат |
|---|---|---|---|
| 2026-09-22T14:40:41Z | Точное сообщение в разделе входного задания | Grounding, карта тикетов и assumptions | Автоматический workflow; без дополнительного approval эпика |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0001-TK-0001-ai-base-contract | stage approved; 3 files; contracts; AC 1/2 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0001-TK-0002-seeded-ai-bases | stage approved; 5 files; engine; AC 1/2 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0001-TK-0003-authoritative-hostile-access | stage approved; 4 files; engine; AC 3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0001-TK-0004-ai-base-content | stage approved; 2 files; content-data; AC 1/2 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0001-TK-0005-ai-base-presentation | stage approved; 4 files; client; AC 1/3 |
