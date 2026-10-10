---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0001-module-owned-weapon-rules
ticket: EP-0008-US-0001-TK-0002-defense-contract
title: "Контракт пуска ПРО, времени жизни и preview"
stage: implemented
layer: contracts
depends_on: ["EP-0008-US-0001-TK-0001-weapon-contract"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Контракт пуска ПРО, времени жизни и preview

STATUS: IMPLEMENTED / PUBLICATION BLOCKED — automated checks passed; self-review completed.

## Why

Игрок видит параметры установок и понимает шанс сбития. Одинаковые модули обеих сторон работают одинаково, а навыки пока не меняют вероятность. Этот тикет обеспечивает: контракт пуска про, времени жизни и preview.

## Decisions

Решения Q14,Q17,Q20–Q25,Q41,Q46,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0001-module-owned-weapon-rules.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Contracts/DefenseLaunchPreview.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs:12 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Contracts.Tests/CountermeasureSnapshotTests.cs` | Существует: tests/DeepSpaceSaga.Contracts.Tests/CountermeasureSnapshotTests.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Contracts.Tests/DefenseLaunchPreviewTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **2** [общего порядка](../../Tickets.md).
- [EP-0008-US-0001-TK-0001-weapon-contract](../../EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0001-weapon-contract/EP-0008-US-0001-TK-0001-weapon-contract.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Добавить defense.fire, LaunchMode Auto/Manual, captured Accuracy/targetManeuverability/MaxFlightTimeMs/ExpiresAtMotionTimeMs, per-module preview TargetTorpedoId/ChanceTenths/CanFire/Reason. Состояние нескольких аппаратов публикуется в InstalledModules; singular object Defense не использовать как их полный список.
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

- [x] `Countermeasure_lifetime_and_mode_roundtrip`
- [x] `Preview_is_bound_to_module_and_target`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-07

Implemented in the five planned files. Added `defense.fire`, per-flight mode, captured accuracy/target maneuverability/speed/turn, maximum physical lifetime and absolute expiry. Module DTO carries independent auto/manual ranges and a target-bound `DefenseLaunchPreview`; two modules can publish different eligibility without sharing identity/state. Missing preview/chance stays distinct from a valid zero-percent manual shot. Runtime execution and deadline enforcement remain later ticket responsibilities.

Validation: `dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore` PASS 170/170; `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore` PASS 0 warnings/errors. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include` followed by the five Code context C# paths PASS; `git diff --check` PASS. Tests cover Auto/Manual and Guiding/MissedCoast roundtrip, decimal captured values, unknown ETA, independent module/target identity and missing legacy optional fields.

Self-review found no confirmed defect in the DTO scope. No native behavior changed here; native epic gate remains NOT RUN. Publication is verified from the Git commit carrying this ticket ID, not a guessed future SHA. No user APPROVED claimed.

Publication update: commit `02c89d4a51687ed291a11285a8b704991b42cf20` exists locally. Four push attempts returned GitHub `Internal Server Error`; remote still points to TK-0001 `4233f52`. See [ImplementationStatus](../../ImplementationStatus.md) for exact diagnostics and resumption point. This ticket's publication gate remains open.
