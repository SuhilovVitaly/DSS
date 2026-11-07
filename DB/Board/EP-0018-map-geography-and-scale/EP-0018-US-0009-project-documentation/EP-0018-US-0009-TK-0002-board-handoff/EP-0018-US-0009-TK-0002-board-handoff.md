---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0009-project-documentation
ticket: EP-0018-US-0009-TK-0002-board-handoff
title: "Реестр исполнения и передача эпика без ложной приёмки"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: tooling
test_project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
depends_on: [EP-0018-US-0009-TK-0001-current-map-contracts]
files_touched: 5
serves: [AC-02]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Реестр исполнения и передача эпика без ложной приёмки

## Why
После функциональных проверок синхронизировать текущие описания мира, карты и сохранений, сохранив прежние отчёты как историю.
Поставляемый результат: реестр исполнения и передача эпика без ложной приёмки. Тикет покрывает AC-02 [истории](../EP-0018-US-0009-project-documentation.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| DB/Board/EP-0018-map-geography-and-scale/Documentation.md | Созданный planning epic | После реализации добавить dated execution summary и actual criteria status. |
| DB/Board/EP-0018-map-geography-and-scale/ImplementationContract.md | Планируемый контракт данного эпика | Отметить фактические deviations/допуски без стирания решений. |
| DB/Board/EP-0018-map-geography-and-scale/Tickets.md | Созданный полный ticket index | Обновить actual state и зависимости по реальным outcomes. |
| DB/Board/EP-0018-map-geography-and-scale/ImplementationStatus.md | Будущий новый execution ledger, сейчас не создан | Реальные IDs/результаты/paths/остаток без invented commit SHA. |
| DB/Board/EP-0018-map-geography-and-scale/DocumentationImpact.md | Будущий новый impact inventory, сейчас не создан | File->needed change->actual check inventory для этого эпика. |

## Public API after the change
No API change; Board execution records.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Собрать ledger всех29 тикетов и критериев с реальными evidence; обновление отдельных ticket stage — обязанность соответствующих owning тикетов, не скрытое расширение этого allowlist.
2. Фиксировать NOT RUN/OPEN/FAIL отдельно; эпик не считать принятым только по наличию кода или завершённому planning.
3. Записать deviations от assumptions, actual supported versions и оставшийся backlog; точные publication SHAs только если publication отдельно разрешена.
4. Проверить ID/DAG/coverage/links/counts и cleanup task-owned outputs; evidence только canonical validation run.

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
- AC-02 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Markdown paths/anchors, Board consistency и diff checks проходят; утверждения о runtime опираются на фактическую предыдущую приёмку.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
