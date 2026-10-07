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

STATUS: DRAFT

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
| `Documentation/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `Board/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `README.md` | На baseline отсутствует; обновлять только если появится, не создавать автоматически | Только результат и шаги этой карточки |
| `src/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tools/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |

## Dependencies

Шаг **43** [общего порядка](../../Tickets.md).
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
