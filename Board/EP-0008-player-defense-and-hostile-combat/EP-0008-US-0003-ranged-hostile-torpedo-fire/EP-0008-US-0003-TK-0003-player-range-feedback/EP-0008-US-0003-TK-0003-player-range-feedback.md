---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0003-ranged-hostile-torpedo-fire
ticket: EP-0008-US-0003-TK-0003-player-range-feedback
title: "Понятная недоступность торпедного пуска вне дальности"
stage: implemented
layer: client
depends_on: ["EP-0008-US-0003-TK-0002-hostile-fire-scheduler"]
files_touched: 9
serves: ["AC-0001"]
created: 2026-10-07
revision: 1
---

# Понятная недоступность торпедного пуска вне дальности

STATUS: IMPLEMENTED

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

### Execution scope — 2026-10-11

Dependency published as `e33ba130e62ae8dd84439274207b5a48d1ed316d`, remote SHA verified.
Extend scope before edits to shared `Motion/CombatRangeMath.cs` and matching
`Motion.Tests/CombatRangeMathTests.cs`, plus Engine Combat/HostileFire call sites:
extract the existing inclusive range math so Client cannot drift from the Engine
formula. Client uses confirmed snapshot poses/launcher range, while Engine still
validates the launch boundary. Unknown/missing range fails closed.
Update `TorpedoCommandPanelTests`, `DefenseCommandPanelTests`, `TorpedoInspectionTests`
fixtures with explicit range and reuse the panel test fixture for new pending/range
regressions. These fixtures previously omitted the newly required capability.

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

- [x] `Out_of_range_fire_is_disabled`
- [x] `Selection_change_does_not_retarget_pending_fire`
- [x] `Boundary_rejection_clears_only_matching_pending`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Client gates Fire from confirmed snapshot poses and module range through shared
Motion geometry also used by Engine. Inclusive200km/custom50km boundaries agree;
missing/invalid range disables Fire. The launcher reports out-of-range and a matching
server range rejection. Pending target is captured when sending; unrelated or old
CommandIds cannot release the current pending command, including coalesced receipts.
No authoritative busy state is synthesized after rejection.

- Full Client **1770/1770 PASS** (`ep8-us3-tk3-client.trx`).
- Shared Motion range geometry **9/9 PASS** (`ep8-us3-tk3-motion.trx`), including extreme finite coordinates.
- Engine ranged/hostile launch regression **32/32 PASS** (`ep8-us3-tk3-engine.trx`).
- Scoped format/diff PASS; solution build **0 warnings/errors**. Initial new-file CRLF
  findings corrected by scoped formatting. A build overlapping the running Client
  testhost hit a temporary DLL lock; the sequential build after test completion passed.
- Initial UI theory data used integer literals for nullable doubles; corrected to
  double literals before the full passing Client run.

Self-review completed against AC-0001 and US-0003. Native NOT RUN; final corpus
remains OPEN. Unit UI transport tests are supplemented by existing real session/UI
flows in the full Client run; no native window/GPU acceptance is claimed.
