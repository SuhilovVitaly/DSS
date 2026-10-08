---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0012-phase-three-acceptance
ticket: EP-0008-US-0012-TK-0004-epic-review
title: "Итоговое review контрактов и границ"
stage: draft
layer: validation
depends_on: ["EP-0008-US-0012-TK-0003-native-ui-acceptance"]
files_touched: 2
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Итоговое review контрактов и границ

STATUS: DRAFT

## Why

Поставка проверяется на настоящем сценарии и нескольких враждебных кораблях. Автоматические проверки, нативное окно и результаты review фиксируются отдельно. Этот тикет обеспечивает: итоговое review контрактов и границ.

## Decisions

Решения Q01–Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0012-phase-three-acceptance.md): AC-0001, AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `Board/EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `Documentation/04-Engineering/PhaseThreeCombatAcceptance.md` | Создаётся предыдущим тикетом EP-0008-US-0012-TK-0003-native-ui-acceptance | Только результат и шаги этой карточки |

## Dependencies

Шаг **42** [общего порядка](../../Tickets.md).
- [EP-0008-US-0012-TK-0003-native-ui-acceptance](../../EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0003-native-ui-acceptance/EP-0008-US-0012-TK-0003-native-ui-acceptance.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Новый gameplay API не вводится. Результат — проверяемые артефакты, актуальное покрытие или документация в заявленном scope.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Проследить Decisions→AC→tickets→actual tests, review всех production changes, versions, invariants, ownership, boundaries, native gates. Подтверждённые findings исправляются в исходных тикетах/отдельных correction tickets и повторно проверяются; эта карточка не даёт права поставить APPROVED за пользователя. Записать ticket→SHA→push при будущем выполнении через EpicExecutionPrompt.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Другие production layers не реализуются в этой карточке; необходимое расширение оформляется до изменения.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [ ] `Decision_coverage_is_complete`
- [ ] `All_required_gates_have_factual_status`

Команды из корня DSS после реализации:

```powershell
dotnet test DeepSpaceSaga.sln --no-restore
dotnet build DeepSpaceSaga.sln --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0002, AC-0003, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
