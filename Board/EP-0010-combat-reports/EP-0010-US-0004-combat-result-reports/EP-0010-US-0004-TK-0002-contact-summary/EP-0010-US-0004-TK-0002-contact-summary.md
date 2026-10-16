---
epic: EP-0010-combat-reports
story: EP-0010-US-0004-combat-result-reports
ticket: EP-0010-US-0004-TK-0002-contact-summary
title: "E06–E07 после полного применения удара"
stage: draft
status: implemented
layer: engine
depends_on: ["EP-0010-US-0004-TK-0001-intercept-results"]
files_touched: 4
serves: ["US04-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# E06–E07 после полного применения удара

## Why

Сформировать итог одного контакта после определения module damage и ship death. [История](../EP-0010-US-0004-combat-result-reports.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R10, R11, R12, R16, R21.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:146 — `CombatEventType.Launch` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportDamageTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0004-TK-0001-intercept-results; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. В месте применения контакта сохранить нужные данные до удаления и выпустить summary после всего damage/destroy; не выводить E06 из каждого journal line.
2. Player->enemy: одна E06 с variant Hit/Destroyed. Target player: E07 только если ArmorAfter<Before или StructureAfter<Before; капитан из captured данных до гибели.
3. EpisodeId различает projectiles даже при одном target/time; полная journal детализация и exact death semantics остаются. Уничтожение игрока не ждёт UI.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `LethalHitCreatesOneDestroyedReport` (планируемый сценарий проверки).
- `NonlethalHitCreatesOneHitReport` (планируемый сценарий проверки).
- `ArmorOnlyDamageCreatesCaptainReport` (планируемый сценарий проверки).
- `ZeroDamageDoesNotCreateE07` (планируемый сценарий проверки).
- `SameTimeProjectilesRemainSeparate` (планируемый сценарий проверки).
- `DefeatDoesNotWaitForReportPresentation` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportDamageTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportDamageTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US04-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Сформировать итог одного контакта после определения module damage и ship death. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

One summary emitted after complete contact/destroy. E06 includes zero-effect hits with Hit/Destroyed; E07 requires armor or structure reduction, captures captain before removal. Hidden participants and their launcher IDs are masked. Six summary cases plus report/defeat/wreck/module regressions PASS (60 combined); isolated armor-only case tightened and rerun PASS. Engine compile/scoped format/diff PASS. Defeat stays immediate with final E07 captured.
