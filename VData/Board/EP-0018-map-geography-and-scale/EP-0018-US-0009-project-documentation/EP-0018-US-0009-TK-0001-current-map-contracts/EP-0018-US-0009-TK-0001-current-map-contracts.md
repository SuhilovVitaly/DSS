---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0009-project-documentation
ticket: EP-0018-US-0009-TK-0001-current-map-contracts
title: "Действующие правила географии, карты и сохранений"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: tooling
test_project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
depends_on: [EP-0018-US-0008-TK-0003-native-map-evidence]
files_touched: 5
serves: [AC-01]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Действующие правила географии, карты и сохранений

## Why
После функциональных проверок синхронизировать текущие описания мира, карты и сохранений, сохранив прежние отчёты как историю.
Поставляемый результат: действующие правила географии, карты и сохранений. Тикет покрывает AC-01 [истории](../EP-0018-US-0009-project-documentation.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| DB/Documentation/01-Requirements/EngineRequirements.md | Разделы Время/Мир/Авторитет | Согласовать только затронутые новые контракты, сохраняя единицы и ownership. |
| DB/Documentation/02-FirstRelease/Mechanics/SolarSystemMapConcept.md | Краткая текущая seeded map концепция | Обновить min/nearest/levels и legacy/new-world behavior. |
| DB/Documentation/04-Engineering/TradingAndSolarSystem.md | Текущие gen1 ranges/материализация | Описать новый pipeline, frozen generation scale vs current ETA, resources. |
| DB/Documentation/04-Engineering/SaveFormat.md | Save20 materialized world contract | Точные поддержанные версии geography и optional descriptor по фактическому коду. |
| DB/Documentation/03-Design/TacticalMapSpecification.md | Существующая world tactical map спецификация | Согласовать Local/Region/Cluster/System, focus/selection/orbits; не менять tactical battle camera. |

## Public API after the change
No API change; documentation-only, layer tooling по Board convention.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
Final impact scope (2026-11-08): besides the five initial owners, update `DB/Documentation/01-Requirements/FirstReleaseRequirements.md`, `02-FirstRelease/Screens/GameSession.md`, `02-FirstRelease/Screens/Settings.md`, `02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md`, `04-Engineering/AiMapEnvironment.md`, `04-Engineering/DebugTuningPanel.md`, `06-Tooling/GameSessionScreenUI.md` (all under DB/Documentation). These summaries need current scale/navigation/ownership boundaries. VData copies remain noncanonical historical material. Validation history stays in Board/artifacts.

1. После US-0008 сверить итоговую реализацию с этим Board; новые обещания не добавлять без evidence.
2. Обновить5 owning Markdown: rules/units/versions/local zoom/LOD/navigation/ETA boundaries.
3. Указать реальные sources и наблюдаемые ограничения, отделить native/human/FPS остаток. Не хранить логи/screenshots/JSON в Documentation.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
Документационный тикет: production/test C# не меняются. Layer tooling используется для проверки документации; owning test project указан как область регрессий, запуск build/unit tests для Markdown не требуется.
- Проверить каждое утверждение по итоговому code diff и retained evidence.
- Проверить все локальные Markdown paths и якоря; ссылки на будущие результаты не выдавать за существующие файлы.
- Проверить Board IDs/counts/dependency DAG и coverage; запуск git diff --check.
- Сравнить allowlist с фактическим diff, исторические даты и результаты не переписывать.
Автоматические и ручные проверки документировать раздельно. Новых unit tests для Markdown не создавать.

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Markdown paths/anchors, Board consistency и diff checks проходят; утверждения о runtime опираются на фактическую предыдущую приёмку.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Scope исполнения — 2026-11-07

Прямое поручение исполнить эпик по разделу6 EpicExecutionPrompt.md расширяет
первоначальные allowlists документационных тикетов. История выполняется последней.
В TK-0001 входят все релевантные текущие Markdown-контракты, включая screen/mechanics
карты, settings, architecture/config/scenario/save guides, найденные итоговым diff.
TK-0002 ведёт DocumentationImpact.md: полный inventory по всему репозиторию
(document → change → required update → observed check), включая README, документы
возле src/tests/tools/content/assets и значимые комментарии. TK-0003 актуализирует
найденные вне Documentation руководства/README и navigation, затем обновляет
offline граф через tools/graphify/rebuild.py; output/semantic/cache только в
абсолютном workspace artifacts/validation/ep-0018-map-geography-and-scale/<run>/.
Новые scope paths определяются inventory перед изменением и записываются в карточке.
Граф строится после финального кода/текстов; fresh hashes/coverage/limitations
проверяются, исторический src/graphify-out не заменяется. Пять файлов исходного
planning не ограничивают обязательный inventory/документацию всего проекта.


## Execution — 2026-11-08

Updated12 current curated Markdown owners: frozen v2 geography/formula, v1/v2 Save20 ingress, bounded materialization/resources/AI boundaries, camera scale/detail/navigation/ruler vs current ETA, UI2 renderer vs unchanged Settings menu. No run history or generated outputs added to Documentation. Contract/source self-review completed; no new gameplay promises or human approval claimed. Link/anchor and Markdown-only audit:13 changed files/84links,29tickets/38dependency nodes,0errors; retained D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261108-documentation/contracts-audit.json and audit.py. git diff --check PASS. Markdown-only change; prior automated/native outcomes are referenced, not rerun for prose.
