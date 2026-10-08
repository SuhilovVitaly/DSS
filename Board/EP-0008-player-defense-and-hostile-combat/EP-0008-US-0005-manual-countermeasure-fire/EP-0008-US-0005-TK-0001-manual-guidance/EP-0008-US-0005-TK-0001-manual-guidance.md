---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0005-manual-countermeasure-fire
ticket: EP-0008-US-0005-TK-0001-manual-guidance
title: "План и преследование без гарантированной встречи"
stage: draft
layer: motion
depends_on: ["EP-0008-US-0004-TK-0004-combat-path-integration"]
files_touched: 2
serves: ["AC-0003"]
created: 2026-10-07
revision: 1
---

# План и преследование без гарантированной встречи

STATUS: DRAFT

## Why

Игрок выбирает вражескую торпеду и запускает ПРО с конкретного готового аппарата на расстоянии до200км. Он может рисковать при нулевом шансе или неизвестной встрече, повторять попытки и запускать две ПРО по одной цели. Этот тикет обеспечивает: план и преследование без гарантированной встречи.

## Decisions

Решения Q05–Q11,Q16,Q25–Q26,Q29,Q31,Q35,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0005-manual-countermeasure-fire.md): AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/CountermeasureGuidanceMath.cs` | Существует: src/DeepSpaceSaga.Motion/CountermeasureGuidanceMath.cs:5 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Motion.Tests/ManualCountermeasureGuidanceTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **15** [общего порядка](../../Tickets.md).
- [EP-0008-US-0004-TK-0004-combat-path-integration](../../EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0004-combat-path-integration/EP-0008-US-0004-TK-0004-combat-path-integration.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Добавить manual planning с horizon=remaining lifetime, учитывающее полный curved target path, без mandatory hull-contact deadline. При отсутствии встречи создать bounded pursuit segment с unknown ETA, не null spawn и не ложный крест. Наличие геометрической встречи после столкновения цели с препятствием не выдавать как гарантированный прогноз. Сохранить auto строгую достижимость до hull и lifetime.
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

- [ ] `Manual_unreachable_target_still_has_flight`
- [ ] `Manual_horizon_uses_captured_lifetime`
- [ ] `Auto_requires_contact_before_hull`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
