---
epic: EP-0010-combat-reports
story: EP-0010-US-0012-documentation-and-handoff
ticket: EP-0010-US-0012-TK-0001-project-documentation
title: "Синхронизация спецификации, экрана и статуса эпика"
stage: implemented
status: implemented
layer: documentation
depends_on: ["EP-0010-US-0011-TK-0006-hover-cleanup"]
files_touched: 79
serves: ["US12-AC1"]
created: 2026-10-16
revision: 2
validation_status: documented-acceptance-open
---

# Синхронизация спецификации, экрана и статуса эпика

## Why

Завершить поставку проверяемым документальным handoff. [История](../EP-0010-US-0012-documentation-and-handoff.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R04, R05, R06, R07, R08, R09, R10, R11, R12, R13, R14, R15, R16, R17, R18, R19, R20, R21, R22, R23, R24.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Первоначальные 5 файлов из таблицы; фактический scope расширен в Execution scope amendment ниже; Board evidence не разрешает скрытые изменения production. Layer `documentation`; matching coverage `Markdown link and traceability audit`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md` | DB/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md:1 — существующий файл, проверить актуальный diff перед execution | Изменение только для шагов этого тикета; соседний scope запрещён |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | DB/Documentation/01-Requirements/EngineRequirements.md:1 — существующий файл, проверить актуальный diff перед execution | Изменение только для шагов этого тикета; соседний scope запрещён |
| `DB/Documentation/02-FirstRelease/Screens/GameSession.md` | DB/Documentation/02-FirstRelease/Screens/GameSession.md:1 — существующий файл, проверить актуальный diff перед execution | Изменение только для шагов этого тикета; соседний scope запрещён |
| `DB/Board/EP-0010-combat-reports/ImplementationStatus.md` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `DB/Board/EP-0010-combat-reports/Documentation.md` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Дополнительные документы и Graphify разрешены Execution scope amendment ниже. Зависимости: EP-0010-US-0011-TK-0003-native-visual-and-performance; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Сверить фактический API, save childVersion и текущую baseline с контрактом; изменить текст только на основании реализации и evidence.
2. Обновить screen/EngineRequirements ссылки без дублирования и без представления планируемого поведения как текущего.
3. Заполнить ticket-to-evidence статусы, причины открытых native/FPS/human gates; проверить Markdown links и ID. Graphify rebuild и отдельная публикация включены execution scope amendment ниже.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `Relative Markdown links resolve` (планируемый сценарий проверки).
- `EveryR01ThroughR24HasStoryAndTicket` (планируемый сценарий проверки).
- `ImplementationStatusDistinguishesHeadlessNativeAndHuman` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
git diff --check
# Проверить relative Markdown links и R/AC coverage по Tickets.md; build/test не требуются для prose-only diff.
```

## Definition of Done

- Шаги выполнены в документированном расширенном scope; код runtime в финальной истории не меняется.
- Критерии US12-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Завершить поставку проверяемым документальным handoff. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution scope amendment — 2026-10-16

Причина: исходные пять файлов и запрет Graphify не обеспечивают §6 EpicExecutionPrompt.md; пользователь явно разрешил расширение. До реализации scope расширен: все подтверждённо затронутые документы Documentation, корневые руководства, связанные Board, README рядом с src/tests/tools/data, значимые контрактные комментарии; src/graphify-out и связанные manifest/report/tooling документы. Точный перечень фактически изменённых файлов записать в итоговую инвентаризацию до выполнения этой истории.

Дополнительные шаги: составить инвентаризацию document/change/update/check; устранить stale summaries с сохранением датированной истории; проверить все ссылки, ID, coverage/counts; перестроить Graphify и проверить целостность/полноту относительно исходников; записать ограничения AST/semantic extraction; сверить опубликованные SHA через Git. Отдельный commit и немедленный push обязательны. Эта поправка имеет приоритет над прежними planning-only запретами.

## Execution result —2026-10-16

Expanded documentation inventory applied across canonical requirements, screen, engineering, tooling/README, related EP9 and all EP10 cards. Status distinguishes headless PASS from native/human/FPS OPEN; no autonomous game launches allowed. Ticket publication table uses real verified SHAs and leaves own final SHA to Git post-commit check. Link/ID/DAG audit and fresh Graphify extraction/export validated before separate final commit/push. See DocumentationInventory.md, DocumentationAudit.json, FinalReview.md and ImplementationStatus.md in epic root.

Actual final US12 commit scope:79files (including19Graphify output artifacts). The original five-file Code context is historical and superseded by the explicit execution scope amendment/inventory. No runtime C# changes in this ticket.
