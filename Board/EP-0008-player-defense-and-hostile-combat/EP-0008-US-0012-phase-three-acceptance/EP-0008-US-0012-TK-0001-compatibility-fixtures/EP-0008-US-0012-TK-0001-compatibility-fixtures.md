---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0012-phase-three-acceptance
ticket: EP-0008-US-0012-TK-0001-compatibility-fixtures
title: "Адаптация существующих проверок к новой фазе"
stage: draft
layer: validation
depends_on: ["EP-0008-US-0011-TK-0004-load-ui-and-transport"]
files_touched: "inventory-required"
serves: ["AC-0002"]
created: 2026-10-07
revision: 1
---

# Адаптация существующих проверок к новой фазе

STATUS: DRAFT

## Why

Поставка проверяется на настоящем сценарии и нескольких враждебных кораблях. Автоматические проверки, нативное окно и результаты review фиксируются отдельно. Этот тикет обеспечивает: адаптация существующих проверок к новой фазе.

## Decisions

Решения Q01–Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0012-phase-three-acceptance.md): AC-0002. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Расширенный scope является явным исключением A12: test-only, documentation или generated tooling по назначению этого тикета; приложить точный список фактических файлов перед commit.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Contracts.Tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Motion.Tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/` | Набор файлов: точная execution-инвентаризация перед изменением; исключение A12 | Только результат и шаги этой карточки |

## Dependencies

Шаг **39** [общего порядка](../../Tickets.md).
- [EP-0008-US-0011-TK-0004-load-ui-and-transport](../../EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0004-load-ui-and-transport/EP-0008-US-0011-TK-0004-load-ui-and-transport.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Новый gameplay API не вводится. Результат — проверяемые артефакты, актуальное покрытие или документация в заявленном scope.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Только test-source/fixtures matching этих проектов: инвентаризировать ожидания old ratings,одного ПРО,два crew, passive Enemy, unlimited fire, поддержка save<16. Сохранить старые economy/motion тесты осмысленными: нужные сценарии строятся текущим production serializer либо явно NewGame fixture, не обходят save gate. Обновлять expected only по принятому контракту, добавить rejection old saves. Итоговый перечень реально изменённых файлов записать до commit; расширенный test-only scope является явным исключением из лимита5.
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

- [ ] `Existing_suite_preserves_noncombat_assertions`
- [ ] `Old_save_tests_assert_explicit_rejection`
- [ ] `Current_save_tests_use_complete_current_schema`

Команды из корня DSS после реализации:

```powershell
dotnet test DeepSpaceSaga.sln --no-restore
dotnet build DeepSpaceSaga.sln --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0002 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
