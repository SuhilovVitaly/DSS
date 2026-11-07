---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0009-project-documentation
ticket: EP-0018-US-0009-TK-0003-documentation-navigation
title: "Навигация и связь прежних эпиков с новой картой"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: tooling
test_project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
depends_on: [EP-0018-US-0009-TK-0002-board-handoff]
files_touched: 5
serves: [AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Навигация и связь прежних эпиков с новой картой

## Why
После функциональных проверок синхронизировать текущие описания мира, карты и сохранений, сохранив прежние отчёты как историю.
Поставляемый результат: навигация и связь прежних эпиков с новой картой. Тикет покрывает AC-03 [истории](../EP-0018-US-0009-project-documentation.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| DB/Documentation/README.md | Текущие ссылки на solar concept/engineering | Обновить указатели в рамках existing documentation architecture. |
| tools/DeepSpaceSaga.Performance/README.md | Существующий tooling guide | Добавить точные v2 report/native commands, caps/output/cleanup rules. |
| DB/Board/EP-0002-procedural-solar-system/Documentation.md | Исторический эпик карты/орбит | Датированный supersession link кEP-0018 без удаления historical approvals/results. |
| DB/Board/EP-0003-station-clusters-and-trade-geography/Documentation.md | Исторический эпик кластеров | Датированно указать заменённые spacing/preserved-station правила. |
| DB/Board/EP-0004-ai-territories-and-map-environment/Documentation.md | Исторический эпикAI/среды | Датированная integration note v2 и ссылка на актуальное placement evidence. |

## Public API after the change
No API change; documentation-only.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
Final accuracy clarification: `DB/Documentation/02-FirstRelease/Screens/Settings.md` must distinguish renderer constructor/tool UI2 from ordinary SkiaWindow persisted-setting validation, which still accepts only the menu's original set. This is a documentation correction, not a host behaviour change; refresh the affected semantic source and graph hash after it.

Final scope (2026-11-08): initial five files plus `DB/Documentation/06-Tooling/Graphify.md`, `DB/Documentation/06-Tooling/GraphifyRebuild.md`, `tools/graphify/rebuild.py`, and the documentation-only UI-scale summary comment in `tests/DeepSpaceSaga.Client.Tests/GameSessionUiScaleTests.cs`. Rebuild includes canonical EP18 and map-history EP2/3/4, selective fresh agent semantics with exact SHA/L# and full offline AST; publication ledgers/this final ticket are excluded to avoid circular hashes. Existing historical graph outputs remain untouched. Current guide examples must use canonical checkout and workspace artifact paths, never old sibling checkout/root output examples. Board/impact/ledger and actual post-commit receipt are updated after evidence, without inventing own SHA.

1. Добавить навигацию только после фактической поставки; старые acceptance даты/FAIL результаты оставить историей.
2. Описать новые tooling flags с реальными параметрами и воспроизводимыми examples, process-tree cap300000ms и абсолютными paths.
3. Проверить ссылки на текущие контракты и ledger; самостоятельные старые EpicReview paths не объявлять новыми authoritative tasks.
4. Не перестраивать граф/не запускать игру ради Markdown. Если будущий execution workflow требует graph evidence, выделить явный ограниченный ticket до такого запуска.

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
- AC-03 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
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

Updated current navigation, strict v2 raster/native commands, exact caps/absolute
artifact paths, explicit v1 legacy tool modes, current Graphify scope and meaningful
UI-scale comment. EP2/3/4 carry dated supersession/integration links; historical
results/approvals unchanged. Deleted historical Agents links are text, current
installed skill links valid. Settings host/menu versus renderer-only UI2 clarified.

Offline graph744code/664Markdown,15fresh semantic sources (one delegated Graphify
extraction agent per skill; not independent code review),24semantic nodes/27edges.
20422derived nodes/55930edges/690communities;1408source and18stable export hashes
verified. All required v2 files and query path present. Counts, source manifest,
semantics, raw extraction, diagnostics, report and community HTML retained at
D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261108-graph/.
quality-audit.json/verify_graph.py prove current hashes/endpoint integrity. Token
usage unmeasured, no external API or vision extraction. Parser warning and graph
pruning/collapse limitations recorded, not silently called complete coverage.

Changed Markdown62/links427,29tickets/38dependency nodes,0errors; navigation-audit.json
in20261108-documentation. Graph caches/temporary authoring script removed; slots0,
processes complete, noDSS/artifacts/root temp clones/worktrees. Current source hashes
verified after cleanup. C# change is comment only; no gameplay tests rerun for prose.
Actual previous regression/native failures remain explicit in ImplementationStatus.
Final own SHA/push check is recorded only after commit by publication receipt.
