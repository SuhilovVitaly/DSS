---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0011-phase-three-save-continuation
ticket: EP-0008-US-0011-TK-0002-save-combat-validation
title: "Полная схема и preflight боя новой фазы"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0011-TK-0001-new-save-version-gate"]
files_touched: 9
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Полная схема и preflight боя новой фазы

STATUS: DRAFT

## Why

Сохранение новой игры восстанавливает обе установки, активные снаряды и поведение противников. Старые сохранения явно отклоняются, а стартовые сценарии остаются доступными. Этот тикет обеспечивает: полная схема и preflight боя новой фазы.

## Decisions

Решения Q14,Q25,Q28,Q42,Q49 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0011-phase-three-save-continuation.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs` | Существует: src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs` | Существует: src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs` | Создаётся предыдущим тикетом EP-0008-US-0011-TK-0001-new-save-version-gate | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:11 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveSchemaTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

Execution scope expansion before implementation: `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` and `src/DeepSpaceSaga.Engine/SimulationEngine.cs` supply the required version2 defense/version1 hostile payload to the current writer at the same time validation becomes mandatory. No invalid intermediate writer is published. Regression requires staged hostile field restore in this ticket too: emitting the new cursor without restoring it changes complete save state after Load, breaking existing economy continuity assertions. Implement capture plus validated atomic restore together; TK-0003 remains dedicated real-file pursuit/orbit/guiding/miss/reload continuation proof and resulting repairs.

Before producer repair, add `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs`: explicit zero damage in the countermeasure launch journal fact, consistent with current phase-three journal validation and non-damaging countermeasure semantics.

Before updating legacy regression, add `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs`: reject legacy-derived torpedoes in current-format save, while preserving historical internal-fixture migration/impact/operator assertions.

## Dependencies

Шаг **37** [общего порядка](../../Tickets.md).
- [EP-0008-US-0011-TK-0001-new-save-version-gate](../EP-0008-US-0011-TK-0001-new-save-version-gate/EP-0008-US-0011-TK-0001-new-save-version-gate.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Сохранить/валидировать версии payloads, captured поля/абсолютные deadlines, launch mode, hostility provenance, stable IDs, reserved attempts и AI state. Проверка actual formula соответствует frozen fields, skill metadata не участвует. Не принимать expiry before launch, impossible phase deadlines, duplicate module assignment/IDs или divergent clock cursor. Existing economy/map v15 schema остаётся целой.
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

- [x] `Malformed_frozen_fields_rejected_atomically`
- [x] `Invalid_deadline_and_ai_phase_rejected`
- [x] `Case_variant_ids_normalize_consistently`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
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

Implemented version2 defense payload and version1 hostile capture/atomic staged restore. Current-format validation requires captured parameters/provenance, valid physical TTL and miss deadlines, launch mode, zero countermeasure damage, sequence high-water marks, attempts, consistent AI segment/pose/cursor, canonical owner/module/target IDs. Zero-chance manual flight remains valid; skill metadata does not change the frozen formula. Orphan enemy torpedo remains manually eligible after owner destruction and Load.

Regression exposed newly emitted AI cursor being reset on Load: capture and staged restore now ship together. Legacy internal fixture keeps its migration/impact/operator checks under historical version15; current save rejects legacy-derived projectile provenance.

Validation: initial full Engine1884/1903 identified cursor continuity and legacy expectation; after restore Engine1902/1903 left only historical-version fixture mismatch. Final affected Engine202/202 (34 new schema cases plus operator and continuity regressions) PASS. Full Client1843/1843 PASS. Solution build, scoped whitespace verification and diff check PASS. Self-review against AC-0002/0003 performed; actual-file multi-phase continuation belongs to TK-0003. Native acceptance and final corpus OPEN. Own review is not user APPROVED. Separate ticket commit/push follows; remote SHA recorded by next step.
