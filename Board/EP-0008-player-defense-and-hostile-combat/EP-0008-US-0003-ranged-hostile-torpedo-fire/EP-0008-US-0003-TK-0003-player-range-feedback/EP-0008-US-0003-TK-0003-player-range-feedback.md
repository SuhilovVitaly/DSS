---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0003-ranged-hostile-torpedo-fire
ticket: EP-0008-US-0003-TK-0003-player-range-feedback
title: "Понятная недоступность торпедного пуска вне дальности"
stage: draft
layer: client
depends_on: ["EP-0008-US-0003-TK-0002-hostile-fire-scheduler"]
files_touched: 3
serves: ["AC-0001"]
created: 2026-10-07
revision: 1
---

# Понятная недоступность торпедного пуска вне дальности

STATUS: DRAFT

## Why

Каждый враждебный вооружённый корабль запускает торпеду по игроку в радиусе своего аппарата. Игрок подчиняется той же дальности, но управление чужими модулями через player command остаётся запрещено. Этот тикет обеспечивает: понятная недоступность торпедного пуска вне дальности.

## Decisions

Решения Q02–Q04,Q17,Q41,Q44 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0003-ranged-hostile-torpedo-fire.md): AC-0001. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/RangedTorpedoUiTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **10** [общего порядка](../../Tickets.md).
- [EP-0008-US-0003-TK-0002-hostile-fire-scheduler](../../EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0002-hostile-fire-scheduler/EP-0008-US-0003-TK-0002-hostile-fire-scheduler.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Кнопка торпедного Fire учитывает authoritative range и target; UI rejected result обрабатывается по CommandId, без ложного busy. Пояснение out-of-range и сравнение200km должны согласоваться с Engine; пограничная серверная проверка остаётся обязательной.
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

- [ ] `Out_of_range_fire_is_disabled`
- [ ] `Selection_change_does_not_retarget_pending_fire`
- [ ] `Boundary_rejection_clears_only_matching_pending`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
