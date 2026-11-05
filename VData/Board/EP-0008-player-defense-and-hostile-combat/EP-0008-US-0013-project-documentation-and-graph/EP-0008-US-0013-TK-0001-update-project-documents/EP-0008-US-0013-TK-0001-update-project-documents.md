---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0013-project-documentation-and-graph
ticket: EP-0008-US-0013-TK-0001-update-project-documents
title: "Обновить релевантные документы во всех папках"
stage: draft
layer: documentation
depends_on: ["EP-0008-US-0012-TK-0004-epic-review"]
files_touched: "inventory-required"
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Обновить релевантные документы во всех папках

STATUS: IMPLEMENTED — documentation reviewed; Graphify follows in TK-0002

## Why

Последняя история синхронизирует документы с фактическим результатом третьей фазы. Она охватывает Documentation и релевантные материалы в других папках, затем обновляет навигационный граф. Этот тикет обеспечивает: обновить релевантные документы во всех папках.

## Decisions

Решения Обязательная завершающая история по запросу пользователя и EpicExecutionPrompt из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0013-project-documentation-and-graph.md): AC-0001, AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Расширенный scope является явным исключением A12: test-only, documentation или generated tooling по назначению этого тикета; приложить точный список фактических файлов перед commit.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `DB/Board/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `README.md` | На baseline отсутствует; обновлять только если появится, не создавать автоматически | Только результат и шаги этой карточки |
| `src/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tools/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |

## Dependencies

Шаг **44** [общего порядка](../../Tickets.md).
- [EP-0008-US-0012-TK-0004-epic-review](../../EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0004-epic-review/EP-0008-US-0012-TK-0004-epic-review.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Новый gameplay API не вводится. Результат — проверяемые артефакты, актуальное покрытие или документация в заявленном scope.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Scope только релевантные Markdown/руководства/схемные описания/XML-doc comments и текстовые specifications, не произвольный production refactor. Выполнить полный impact audit по final diff: source-of-truth, first release, Tetrarch/crew/loadout/панели/карта/сохранения/поражение/AI, старые EP6/7 summary links, tooling и инструкции рядом с реализацией. Сначала обновить канонические контракты; перенести реальное evidence в engineering report. В карточке execution зафиксировать точный перечень. Обновить итоговое состояние Board, ссылки и счётчики.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Разрешён только обозначенный класс файлов/результат; общий охват проекта не является production-refactor заданием.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [ ] `Affected_document_inventory_has_no_unreviewed_rows`
- [ ] `Documentation_matches_actual_schema_and_behavior`
- [ ] `Historical_reports_are_not_rewritten_as_new_evidence`

Команды из корня DSS после реализации:

```powershell
git diff --check
# Проверить Markdown-ссылки и metadata/DAG всего изменённого Board-пакета.
# Для tooling: Graphify по актуальному SKILL.md после code/docs, с фактическим отчётом.
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution inventory before edits — 2026-10-12

Baseline d13afd05e5a26d53492fcc98a96744cdb7ab7976 published and remote verified. This scope reconciles current EP-0008 rules, per-scenario loadout, Save16, Defeat and truthful acceptance/publication status. Dated evidence is retained. New files: Defeat.md screen contract and PhaseThreeCombatDocumentation.md impact inventory. No runtime changes. Exact scope:

- `DB/Documentation/01-Requirements/EngineRequirements.md`
- `DB/Documentation/01-Requirements/FirstReleaseRequirements.md`
- `DB/Documentation/00-Process/CLAUDE.md`
- `DB/Documentation/README.md`
- `DB/Documentation/02-FirstRelease/Mechanics/TetrarchClass.md`
- `DB/Documentation/02-FirstRelease/Mechanics/CrewAndHabitation.md`
- `DB/Documentation/02-FirstRelease/Mechanics/PassengerContracts.md`
- `DB/Documentation/02-FirstRelease/Mechanics/CommandPanels.md`
- `DB/Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md`
- `DB/Documentation/02-FirstRelease/Screens/GameSession.md`
- `DB/Documentation/02-FirstRelease/Screens/Save.md`
- `DB/Documentation/02-FirstRelease/Screens/Load.md`
- `DB/Documentation/02-FirstRelease/Screens/Defeat.md`
- `DB/Documentation/03-Design/TacticalMapSpecification.md`
- `DB/Documentation/06-Tooling/GameSessionScreenUI.md`
- `DB/Documentation/04-Engineering/BasicTorpedoCombat.md`
- `DB/Documentation/04-Engineering/CountermeasureCombat.md`
- `DB/Documentation/04-Engineering/AiMapEnvironment.md`
- `DB/Documentation/04-Engineering/TradingAndSolarSystem.md`
- `DB/Documentation/04-Engineering/PhaseThreeCombatDocumentation.md`
- `tools/DeepSpaceSaga.Performance/README.md`
- `DB/Board/EP-0006-basic-torpedo-combat/Documentation.md`
- `DB/Board/EP-0006-basic-torpedo-combat/Tickets.md`
- `DB/Board/EP-0007-countermeasure-combat/Documentation.md`
- `DB/Board/EP-0007-countermeasure-combat/Tickets.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/Documentation.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/PublicationStatus.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/Tickets.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-module-owned-weapon-rules.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-dual-defense-loadout.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-ranged-hostile-torpedo-fire.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-hostile-pursuit-and-orbit.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-manual-countermeasure-fire.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-lifetime-and-auto-arbitration.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-independent-defense-panels.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-manual-preview-and-map.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-authoritative-player-defeat.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-defeat-screen-and-recovery.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-phase-three-save-continuation.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-phase-three-acceptance.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-project-documentation-and-graph.md`
- `DB/Board/EP-0008-player-defense-and-hostile-combat/EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-TK-0001-update-project-documents/EP-0008-US-0013-TK-0001-update-project-documents.md`

Root AGENTS/CLAUDE are compatibility links; no root README exists. Asset workflows, ItemCatalogSchema and unrelated dated reports do not change. No relevant adjacent src/tests Markdown requires an edit. Graphify artifacts are owned by TK-0002 after this publication.

## Validation and result — 2026-10-12

43 scoped Markdown files synchronized, including two new durable documents. Canonical EngineRequirements updated before summaries. Relative links resolve after Windows path normalization;13 stories/45 unique tickets and dependency DAG checked, publication hashes are HEAD ancestors. Self-review and git diff --check PASS. No runtime change or repeated tests. Native/FPS/human gaps remain explicitly OPEN. Scope inventory: [PhaseThreeCombatDocumentation](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatDocumentation.md). Separate commit/push follows; own SHA verified through Git.
