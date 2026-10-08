---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0011-phase-three-save-continuation
ticket: EP-0008-US-0011-TK-0004-load-ui-and-transport
title: "Файловая загрузка и понятное сообщение несовместимости"
stage: draft
layer: client
depends_on: ["EP-0008-US-0011-TK-0003-save-runtime-continuation"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Файловая загрузка и понятное сообщение несовместимости

STATUS: DRAFT

## Why

Сохранение новой игры восстанавливает обе установки, активные снаряды и поведение противников. Старые сохранения явно отклоняются, а стартовые сценарии остаются доступными. Этот тикет обеспечивает: файловая загрузка и понятное сообщение несовместимости.

## Decisions

Решения Q14,Q25,Q28,Q42,Q49 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0011-phase-three-save-continuation.md): AC-0001, AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/SessionConnectionLoader.cs` | Существует: src/DeepSpaceSaga.Client/SessionConnectionLoader.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Существует: src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:31 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Создаётся предыдущим тикетом EP-0008-US-0007-TK-0002-panel-command-routing | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/SaveReliabilityTests.cs` | Существует: tests/DeepSpaceSaga.Client.Tests/SaveReliabilityTests.cs:9 (baseline 6907c58) | Только результат и шаги этой карточки |

## Dependencies

Шаг **38** [общего порядка](../../Tickets.md).
- [EP-0008-US-0011-TK-0003-save-runtime-continuation](../../EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0003-save-runtime-continuation/EP-0008-US-0011-TK-0003-save-runtime-continuation.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Реальный save slot/new session roundtrip и rejected old save как локализованная recoverable error. Восстановить две панели/selection/TTL/frozen values и AI info; сбросить pending/previews/effect watermark. Нельзя объявлять Load успехом пока factory/validation не завершены.
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

- [ ] `Real_file_roundtrip_restores_both_panels`
- [ ] `Old_save_error_keeps_current_session`
- [ ] `Load_has_no_old_effect_or_pending_replay`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
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
