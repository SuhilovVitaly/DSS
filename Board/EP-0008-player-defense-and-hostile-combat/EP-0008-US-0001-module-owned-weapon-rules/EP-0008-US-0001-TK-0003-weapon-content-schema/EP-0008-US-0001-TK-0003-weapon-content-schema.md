---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0001-module-owned-weapon-rules
ticket: EP-0008-US-0001-TK-0003-weapon-content-schema
title: "Схема и валидация новых параметров оружия"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0001-TK-0002-defense-contract"]
files_touched: 4
serves: ["AC-0001","AC-0002"]
created: 2026-10-07
revision: 1
---

# Схема и валидация новых параметров оружия

STATUS: DRAFT

## Why

Игрок видит параметры установок и понимает шанс сбития. Одинаковые модули обеих сторон работают одинаково, а навыки пока не меняют вероятность. Этот тикет обеспечивает: схема и валидация новых параметров оружия.

## Decisions

Решения Q14,Q17,Q20–Q25,Q41,Q46,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0001-module-owned-weapon-rules.md): AC-0001, AC-0002. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs` | Существует: src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureContentLoaderTests.cs` | Существует: tests/DeepSpaceSaga.Engine.Tests/CountermeasureContentLoaderTests.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs` | Существует: tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |

## Dependencies

Шаг **3** [общего порядка](../../Tickets.md).
- [EP-0008-US-0001-TK-0002-defense-contract](../../EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0002-defense-contract/EP-0008-US-0001-TK-0002-defense-contract.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Ввести torpedoManeuverability, torpedoRangeKm, countermeasureAccuracy, countermeasureManualRangeKm, countermeasureMaxFlightTimeMs; automatic range и reload оставить отдельными. Не переименовывать механически speed/turn в maneuverability. Валидировать конечность, границы и обязательность согласно контракту эпика; удалить зависимость от старых baseRating. Проверить реальные JSON через registry.
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

- [ ] `Weapon_schema_rejects_invalid_values`
- [ ] `Manual_and_auto_ranges_are_independent`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0002 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
