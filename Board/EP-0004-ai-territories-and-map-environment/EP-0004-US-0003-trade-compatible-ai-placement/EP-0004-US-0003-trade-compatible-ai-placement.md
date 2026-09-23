---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0003-trade-compatible-ai-placement
title: Территории ИИ вокруг доступной торговой сети
stage: draft
dependencies: [EP-0004-US-0002-moving-ai-territories, EP-0003-US-0004-cluster-map-and-travel-estimates]
created: 2026-09-22T14:40:41Z
source_request: "сделай тикеты для историй в эпиках 2 3 и 4&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0002-procedural-solar-system&#x20;\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0003-station-clusters-and-trade-geography\nD:\\DeepSpaceSaga\\DSS\\Board\\EP-0004-ai-territories-and-map-environment"
current_review: complete
revision: 1
---

# Территории ИИ вокруг доступной торговой сети

## Входное техническое задание

[Документация эпика](../Documentation.md): Раздел «Размещение относительно торговли»; E4-AC-04.
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

Как игрок, я хочу сохранять пригодный для местной торговли старт и пространство для будущих обходных путешествий между районами. Стартовая локальная сеть находится вне областей ИИ, а между кластерами остаётся связная сеть геометрически возможных обходов. Проверка учитывает движение мира на объявленном горизонте, а не только начальный кадр. Если обязательные условия разместить невозможно, новая игра сообщает об этом без частично созданной карты.

## Acceptance criteria

- AC-0001: В отчёте размещения проверены старт, 1, 7, 30, 100 и 365 календарных дней и критические сближения; названы горизонт, настройки и нарушения.
- AC-0002: Старт и местные связи вне областей, межкластерные обходные направления образуют связную сеть; сохранена компактность человеческих кластеров.
- AC-0003: Повторные попытки размещения детерминированы и ограничены; невозможная конфигурация даёт понятную ошибку до публикации мира.

## Non-goals

Геометрическая проверка резервирует пространство и не является автопилотом обхода или доказательством вечной безопасности. Нынешний свободный пролёт не блокируется. Будущие патрули могут изменить оценку риска.

Production-код и выполнение тикетов не входят в текущую planning работу. Соседние формулы торговли, скорость Approach, N-body, патрули/бой, туман войны, реальные эффекты среды не добавляются этой нарезкой.

## Dependencies

EP-0004-US-0002-moving-ai-territories, EP-0003-US-0004-cluster-map-and-travel-estimates. US-0002 даёт движущиеся области, EP-0003/US-0004 — географию стартовой сети и межкластерные направления.
Зависимость означает необходимый результат; статус approved у планового документа сам по себе не доказывает реализацию.

Implementation gates (перед первым тикетом, проверяются по коду/tests, а не stage):
- EP-0004-US-0002-moving-ai-territories: EP-0004-US-0002-TK-0001-territory-radii-contract, EP-0004-US-0002-TK-0002-moving-territory-data, EP-0004-US-0002-TK-0003-territory-rendering.
- EP-0003-US-0004-cluster-map-and-travel-estimates: EP-0003-US-0004-TK-0001-cluster-travel-estimates.

Дополнительные технические зависимости, выявленные при нарезке:

- EP-0004-US-0003-TK-0002-placement-evidence-report → EP-0002-US-0008-TK-0002-system-performance-report: общий tooling/client diagnostic результат, без изменения исходного продуктового dependency списка.

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
| [TK-0001](./EP-0004-US-0003-TK-0001-temporal-placement-validation/EP-0004-US-0003-TK-0001-temporal-placement-validation.md) | Проверка обходов и критических эпох | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](./EP-0004-US-0003-TK-0002-placement-evidence-report/EP-0004-US-0003-TK-0002-placement-evidence-report.md) | Воспроизводимый отчёт геометрического размещения | tooling | TK-0001 + внешние gates; EP-0002-US-0008-TK-0002-system-performance-report | AC-0001, AC-0002, AC-0003 |

Полные ID/depends_on перечислены в frontmatter каждого тикета; внутри истории порядок TK-0001 → TK-0002. served criteria — вклад тикета; полнота каждого AC проверяется объединением всех указанных тикетов истории.

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
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0003-TK-0001-temporal-placement-validation | stage approved; 4 files; engine; AC 1/2/3 |
| 2026-09-22T14:40:41Z | StoryBuilder | Создан EP-0004-US-0003-TK-0002-placement-evidence-report | stage approved; 3 files; tooling; AC 1/2/3 |
