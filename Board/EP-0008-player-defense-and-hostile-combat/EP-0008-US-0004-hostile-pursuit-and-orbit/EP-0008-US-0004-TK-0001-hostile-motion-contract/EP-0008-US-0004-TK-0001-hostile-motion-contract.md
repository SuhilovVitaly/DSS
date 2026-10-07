---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0004-hostile-pursuit-and-orbit
ticket: EP-0008-US-0004-TK-0001-hostile-motion-contract
title: "Контракт подтверждённого движения и фаз противника"
stage: draft
layer: contracts
depends_on: ["EP-0008-US-0003-TK-0003-player-range-feedback"]
files_touched: 3
serves: ["AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Контракт подтверждённого движения и фаз противника

STATUS: DRAFT

## Why

Противник начинает сближение после снятия стартовой паузы. Он управляет двигателем, выходит на движущуюся вместе с игроком орбиту радиусом половины дальности оружия и возвращается к преследованию, если удержание невозможно. Этот тикет обеспечивает: контракт подтверждённого движения и фаз противника.

## Decisions

Решения Q18–Q20,Q39–Q45,Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0004-hostile-pursuit-and-orbit.md): AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/HostileCombatSnapshot.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:14 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Contracts.Tests/HostileCombatSnapshotTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **11** [общего порядка](../../Tickets.md).
- [EP-0008-US-0003-TK-0003-player-range-feedback](../../EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0003-player-range-feedback/EP-0008-US-0003-TK-0003-player-range-feedback.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Определить фазу, target player ID, controlling engine/weapon module IDs, desired radius, retained clockwise choice, versioned deterministic segment/epoch данных движения. Snapshot должен позволять Motion предсказывать ровно подтверждённый сегмент без решений AI в Client.
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

- [ ] `Hostile_state_and_motion_segment_roundtrip`
- [ ] `Orbit_direction_is_explicit`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0002, AC-0003, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
