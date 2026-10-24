---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0010-defeat-screen-and-recovery
ticket: EP-0008-US-0010-TK-0001-defeated-session-save-guard
title: "Защита файлового сохранения и session lifecycle"
stage: draft
layer: engine-localclient
depends_on: ["EP-0008-US-0009-TK-0003-defeat-clock-boundary"]
files_touched: 2
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Защита файлового сохранения и session lifecycle

STATUS: IMPLEMENTED — self-reviewed; publication recorded by the next execution entry

## Why

Игрок получает понятное окно поражения с двумя действиями. Загрузка старого по времени, но совместимого сохранения до гибели создаёт нормальную живую сессию; выход возвращает главное меню. Этот тикет обеспечивает: защита файлового сохранения и session lifecycle.

## Decisions

Решения Q27–Q28,Q49 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0010-defeat-screen-and-recovery.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | Существует: src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:13 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/DefeatedSessionSaveTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **33** [общего порядка](../../Tickets.md).
- [EP-0008-US-0009-TK-0003-defeat-clock-boundary](../../EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-TK-0003-defeat-clock-boundary/EP-0008-US-0009-TK-0003-defeat-clock-boundary.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Проверить central Engine guard через actual SaveAsync/saveGate/file path. Захват после defeat не пишет/не заменяет слот; queued captures после death также rejected. Сохранение атомарного pre-death snapshot, захваченного до death, не считать post-death capture. Existing file bytes/temporary cleanup сохранены. Если test project не ссылается на LocalClient, использовать существующий matching transport test project при актуализации карточки.
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

- [x] `Save_after_defeat_keeps_existing_bytes`
- [x] `Queued_postdeath_capture_is_rejected`
- [x] `Fresh_session_has_no_terminal_state`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency `bd10a68f565846c57a973c9d4bf07517a970f78c` published/remote verified. Existing LocalSession SaveAsync already captures inside saveGate and before all filesystem writes; central terminal Engine guard therefore supplies the required production behavior. Kept that ordering and documented the distinction between queued post-death capture and completing an immutable pre-death capture.

Four real transport/file regressions verify unchanged existing slot bytes, no directory creation for rejected new slots, deterministic queued capture behind the actual saveGate rejected after a lethal hit, no temp files, disposal and creating a fresh Running connection from a real pre-death save through CreateFromSaveFile, followed by a successful new file save. No mocked engine/file transport. Affected Engine17/17 PASS (DefeatedSessionSave, LocalWorldSaveRoundtrip, FullMapLocalLoad), dependent projects build0warnings/errors; scoped format normalized new-file line endings then verification/diffPASS. Own review complete. Next is the defeat screen and actual window navigation integration; native remains OPEN.
