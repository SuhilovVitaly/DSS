---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0001-module-owned-weapon-rules
ticket: EP-0008-US-0001-TK-0001-weapon-contract
title: "Контракт характеристик торпеды"
stage: draft
layer: contracts
depends_on: []
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Контракт характеристик торпеды

STATUS: DRAFT

## Why

Игрок видит параметры установок и понимает шанс сбития. Одинаковые модули обеих сторон работают одинаково, а навыки пока не меняют вероятность. Этот тикет обеспечивает: контракт характеристик торпеды.

## Decisions

Решения Q14,Q17,Q20–Q25,Q41,Q46,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0001-module-owned-weapon-rules.md): AC-0001, AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/CombatSnapshot.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Contracts/WeaponOperatorSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/WeaponOperatorSnapshot.cs:4 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs` | Существует: src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs:1 | Optional numeric факты точности/манёвренности и module/mode/expiry reason для сохраняемого журнала |
| `tests/DeepSpaceSaga.Contracts.Tests/WeaponOperatorSnapshotTests.cs` | Существует: tests/DeepSpaceSaga.Contracts.Tests/WeaponOperatorSnapshotTests.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs` | Существует: tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |

## Dependencies

Шаг **1** [общего порядка](../../Tickets.md).
- Реальный EP6/EP7 runtime и проверенный текущий baseline; нет искусственной зависимости на ещё не созданный файл.
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Добавить RangeKm/Maneuverability и captured maneuverability с ясными именами. Сохранить оператора/skill для отображения, убрать семантику EffectiveRating как влияния skill. В CombatSnapshot определить общий LaunchMode для последующего DTO ПРО; расширить CombatJournalEntry optional module/mode и числовыми фактами, чтобы журнал сохранял источник после удаления снаряда. Различать HP hit100% и шанс сбития; новые API централизовать без второго источника правил.
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

- [ ] `Weapon_parameters_roundtrip`
- [ ] `Operator_skill_is_metadata`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
git diff --check
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
